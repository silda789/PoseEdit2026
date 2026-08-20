// ====================================================================================
// ФАЙЛ: PrintCommands.cs
// НАЗНАЧЕНИЕ: команда MAGICPRINT - "распечатать всё" одним нажатием: находит рамку
//             листа на каждом Layout чертежа, подбирает подходящий формат бумаги
//             (A4/A3/A2/A1/A0 или заранее созданный вручную нестандартный размер) и
//             печатает ВСЕ листы в ОДИН многостраничный PDF через "AutoCAD PDF
//             (High Quality Print).pc3", 1:1 к бумаге, без полей.
// ====================================================================================
// ВАЖНОЕ ОГРАНИЧЕНИЕ (проверено 2026-08-19, до написания этого файла): у AutoCAD НЕТ
// официального .NET/COM API для программного ДОБАВЛЕНИЯ новых Custom Paper Sizes в
// .pc3-драйвер - это подтверждают и форумы Autodesk, и официальная документация.
// Поэтому эта команда только ПОДБИРАЕТ и ИСПОЛЬЗУЕТ уже существующие размеры бумаги
// (стандартные ISO, или нестандартные - если пользователь один раз вручную добавил их
// через мастер AutoCAD: Файл -> Диспетчер плоттеров -> "AutoCAD PDF (High Quality
// Print).pc3" -> правой кнопкой "Правка" -> вкладка "Device and Document Settings" ->
// "Custom Paper Sizes" -> "Add"). Если нужного размера ещё нет - команда НЕ падает и не
// печатает криво, а чётко пропускает этот лист и говорит, что размер нужно добавить.
//
// ВАЖНО ПРО ИМЕНА НЕСТАНДАРТНЫХ РАЗМЕРОВ: чтобы команда могла найти вручную созданный
// custom-размер, ему нужно дать ИМЕННО такое имя в мастере AutoCAD:
//     CUSTOM <короткая_сторона>x<длинная_сторона>mm
// например для листа 594x1050 мм - имя "CUSTOM 594x1050mm". Короткая сторона - всегда
// первой. При создании такого размера в мастере ОБЯЗАТЕЛЬНО поставьте все 4 поля
// "Printable Area" (margins) в 0 - иначе поле "Printable Area = 0" не будет выполнено
// для этого листа (это тоже нельзя исправить программно - см. ограничение выше).
// ====================================================================================
#nullable disable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.PlottingServices;
using Autodesk.AutoCAD.Runtime;

namespace PoseEdit2026
{
    public static class PrintCommands
    {
        // Имя плоттера - точно как оно называется в списке плоттеров AutoCAD
        // (Файл -> Печать -> поле "Принтер/плоттер"). Если у пользователя плоттер
        // называется иначе (например, локализованное имя) - придётся поменять эту
        // строку под конкретную установку AutoCAD.
        private const string PlotterName = "AutoCAD PDF (High Quality Print).pc3";

        // Допуск при сравнении размеров рамки со стандартными форматами, в мм.
        // Небольшая погрешность округления при черчении - нормально (доли мм), но
        // допуск НАМЕРЕННО строгий, а не "на все случаи": слишком широкий допуск тихо
        // "прощает" настоящие ошибки в размере рамки (например 210x294 вместо 210x297 -
        // реальный случай 2026-08-19, оказался опечаткой при черчении, а не погрешностью -
        // с широким допуском такая ошибка осталась бы незамеченной и ушла бы в печать).
        // Лучше явно пропустить подозрительный лист (см. "propuscheno listov" в отчёте
        // команды) и дать пользователю самому проверить рамку, чем угадать неправильно.
        private const double SizeTolerance = 1.0;

        // Стандартные листы ISO серии A, короткая сторона x длинная сторона, в мм.
        // (Label, Short, Long) - "A4" самый маленький, "A0" самый большой.
        private static readonly (string Label, double Short, double Long)[] IsoSizes =
        {
            ("A4", 210, 297),
            ("A3", 297, 420),
            ("A2", 420, 594),
            ("A1", 594, 841),
            ("A0", 841, 1189),
        };

        // ====================================================================================
        // КОМАНДА: MAGICPRINT
        // ====================================================================================
        [CommandMethod("MAGICPRINT")]
        public static void MagicPrintCommand()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            if (doc == null) return;
            Editor ed = doc.Editor;
            Database db = doc.Database;

            // Проверяем, что прямо сейчас не идёт другая печать (иначе PlotEngine
            // откажется работать) - тот же приём, что и в официальном примере Autodesk.
            if (PlotFactory.ProcessPlotState != ProcessPlotState.NotPlotting)
            {
                ed.WriteMessage("\nMAGICPRINT: another plot is already in progress.");
                return;
            }

            // ================================================================================
            // ШАГ 1: собираем список листов для печати - для каждого Layout (кроме "Model")
            // ищем самую большую замкнутую полилинию (рамку листа), измеряем её размер и
            // подбираем под неё имя бумаги в PC3. Листы без рамки/без подходящего размера -
            // пропускаем и подробно объясняем почему, не прерывая всю печать целиком.
            // ================================================================================
            var sheets = new List<SheetPlan>();
            var skipped = new List<string>();

            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                DBDictionary layoutDict = (DBDictionary)tr.GetObject(db.LayoutDictionaryId, OpenMode.ForRead);

                // DBDictionary не гарантирует порядок "как на вкладках" - сортируем по
                // Layout.TabOrder (тот же приём, что и в RoutineCommands.LAYRENUM), чтобы
                // страницы в итоговом PDF шли в привычном пользователю порядке вкладок.
                var layoutEntries = new List<(string Name, Layout Layout)>();
                foreach (DBDictionaryEntry entry in layoutDict)
                {
                    Layout layout = tr.GetObject(entry.Value, OpenMode.ForRead) as Layout;
                    if (layout == null || layout.ModelType) continue;
                    layoutEntries.Add((entry.Key, layout));
                }
                layoutEntries = layoutEntries.OrderBy(e => e.Layout.TabOrder).ToList();

                foreach (var (name, layout) in layoutEntries)
                {
                    BlockTableRecord layoutBtr = (BlockTableRecord)tr.GetObject(layout.BlockTableRecordId, OpenMode.ForRead);

                    // Список ВСЕХ замкнутых полилиний на листе (не только самая большая) -
                    // отсортирован по убыванию площади. На листе может быть несколько
                    // замкнутых контуров (сама рамка, ячейки штампа и т.п.) - раньше метод
                    // слепо брал САМУЮ БОЛЬШУЮ по площади и считал её рамкой, даже если её
                    // размер не совпадал ни с одним реальным форматом бумаги.
                    List<Extents3d> candidates = FindClosedPolylineCandidates(tr, layoutBtr);
                    if (candidates.Count == 0)
                    {
                        skipped.Add($"{name}: zamknutaya polyliniya (ramka lista) ne naidena.");
                        continue;
                    }

                    // ВАЖНО (изменено 2026-08-19 по просьбе пользователя): теперь ищем среди
                    // ВСЕХ замкнутых полилиний именно ту, чей размер СОВПАДАЕТ с реальным
                    // форматом бумаги (ISO или вручную добавленный custom) - а не просто берём
                    // самую большую и потом проверяем её размер. Перебираем от большей к
                    // меньшей (см. сортировку в FindClosedPolylineCandidates), первое
                    // совпадение и есть искомая рамка листа.
                    Extents3d? frame = null;
                    string mediaName = null;
                    bool rotate = false;
                    foreach (Extents3d candidate in candidates)
                    {
                        double candWidthMm = candidate.MaxPoint.X - candidate.MinPoint.X;
                        double candHeightMm = candidate.MaxPoint.Y - candidate.MinPoint.Y;
                        string candMedia = FindMatchingMedia(layout, candWidthMm, candHeightMm, out bool candRotate);
                        if (candMedia == null) continue;

                        frame = candidate;
                        mediaName = candMedia;
                        rotate = candRotate;
                        break;
                    }

                    if (frame == null)
                    {
                        // В сообщении о пропуске показываем размер САМОЙ БОЛЬШОЙ замкнутой
                        // полилинии - скорее всего это и есть рамка, просто её размер не
                        // совпал ни с одним известным форматом (или это опечатка при черчении).
                        Extents3d biggest = candidates[0];
                        double shortSide = Math.Min(biggest.MaxPoint.X - biggest.MinPoint.X, biggest.MaxPoint.Y - biggest.MinPoint.Y);
                        double longSide = Math.Max(biggest.MaxPoint.X - biggest.MinPoint.X, biggest.MaxPoint.Y - biggest.MinPoint.Y);
                        skipped.Add($"{name}: razmer {shortSide:F0}x{longSide:F0} mm ne naiden sredi " +
                                    $"standartnyh ili vruchnuyu dobavlennyh razmerov PC3 (ozhidalos imya " +
                                    $"'CUSTOM {shortSide:F0}x{longSide:F0}mm').");
                        continue;
                    }

                    sheets.Add(new SheetPlan
                    {
                        LayoutId = layout.ObjectId,
                        LayoutName = name,
                        WindowMin = new Point2d(frame.Value.MinPoint.X, frame.Value.MinPoint.Y),
                        WindowMax = new Point2d(frame.Value.MaxPoint.X, frame.Value.MaxPoint.Y),
                        MediaName = mediaName,
                        Rotate = rotate,
                    });
                }

                tr.Commit();
            }

            if (skipped.Count > 0)
            {
                ed.WriteMessage($"\nMAGICPRINT: propuscheno listov {skipped.Count}:");
                foreach (string s in skipped)
                    ed.WriteMessage("\n  - " + s);
            }

            if (sheets.Count == 0)
            {
                ed.WriteMessage("\nMAGICPRINT: net listov, podhodyaschih dlya pechati. Otmena.");
                return;
            }

            // ================================================================================
            // ШАГ 2: куда сохранить итоговый PDF - рядом с самим чертежом, под тем же именем.
            // ================================================================================
            string dwgPath = doc.Name;
            string outputPdf = Path.Combine(
                Path.GetDirectoryName(dwgPath) ?? "",
                Path.GetFileNameWithoutExtension(dwgPath) + ".pdf");

            // ================================================================================
            // ШАГ 3: печать.
            //
            // ВАЖНО (архитектура изменена 2026-08-20 - собственный PlotEngine-цикл заменён
            // на штатный AutoCAD Publish): своя ручная печать через PlotFactory.
            // CreatePublishEngine + BeginPage-цикл (как в официальном примере Autodesk
            // "Driving a multi-sheet AutoCAD plot") оказалась РЕАЛЬНО БАГОВАННОЙ у самого
            // AutoCAD PDF-драйвера при печати НЕСКОЛЬКИХ РАЗНЫХ размеров/поворотов подряд в
            // одном пакете: подтверждено множеством тестов на реальном чертеже (10 листов,
            // 2026-08-19/20) - первые 1-2 листа после любого "скачка вверх" по размеру
            // печатались верно, а следующие 2-3 листа поменьше - с обрезанным содержимым
            // (только угол штампа), независимо от поворота/ориентации. Ни смена порядка
            // вызовов SetPlotRotation/SetPlotWindowArea/SetPlotOrigin, ни центрирование
            // вместо ручного origin, ни подготовка всех PlotInfo заранее до начала печати -
            // ничего из этого не исправило сам баг (только исправляло сопутствующие крэши).
            //
            // Вместо самодельного цикла используем ТОТ ЖЕ механизм, что и штатная команда
            // AutoCAD "Файл -> Publish" (Publish/PUBLISH) - она как раз создана именно для
            // печати наборов листов разных форматов в один PDF и годами проверена в бою,
            // в отличие от нашего PlotEngine-цикла, у которого не было ни одного прецедента
            // использования в этом проекте. Механизм - DSD (Drawing Set Description): текстовый
            // файл-список "какой лист из какого чертежа печатать", который передаётся в
            // Application.Publisher.PublishExecute. Официальный пример Autodesk ("Publish
            // Layouts (.NET)", help.autodesk.com) показывает точно этот путь.
            //
            // Каждый DsdEntry.Nps (Named Page Setup) оставляем пустым - это означает
            // "использовать те настройки печати, что уже сохранены прямо в самом Layout" -
            // а мы их туда только что записали через BuildPlotInfo ниже (шаг "Apply to
            // Layout", добавленный раньше в этом же файле) - то есть вся работа по подбору
            // окна печати/бумаги/поворота (ШАГ 1 выше) никуда не пропадает, она просто
            // применяется через сохранённые настройки Layout'а, а не через наш собственный
            // PlotInfo.OverrideSettings.
            int originalBackgroundPlot = (short)Application.GetSystemVariable("BACKGROUNDPLOT");
            Application.SetSystemVariable("BACKGROUNDPLOT", 0);
            string originalActiveLayout = LayoutManager.Current.CurrentLayout;
            string dsdPath = null;

            try
            {
                // Сохраняем итоговые настройки печати (Window/бумага/поворот) прямо в каждый
                // Layout - возвращаемый PlotInfo больше не используется (печать теперь идёт
                // через Publish, не через наш PlotEngine), но сама запись в Layout ("Apply to
                // Layout") - это именно то, что потом читает DsdEntry.Nps="" ниже.
                foreach (SheetPlan sheet in sheets)
                    BuildPlotInfo(sheet);

                using (DsdEntryCollection dsdEntries = new DsdEntryCollection())
                {
                    foreach (SheetPlan sheet in sheets)
                    {
                        DsdEntry entry = new DsdEntry
                        {
                            DwgName = dwgPath,
                            Layout = sheet.LayoutName,
                            Title = sheet.LayoutName,
                            Nps = "",           // "" = взять сохранённые настройки самого Layout'а
                            NpsSourceDwg = "",
                        };
                        dsdEntries.Add(entry);
                    }

                    using (DsdData dsdData = new DsdData())
                    {
                        dsdData.DestinationName = outputPdf;
                        dsdData.ProjectPath = Path.GetDirectoryName(dwgPath) ?? "";
                        dsdData.SheetType = SheetType.MultiPdf; // один общий многостраничный PDF
                        dsdData.SetDsdEntryCollection(dsdEntries);
                        dsdData.LogFilePath = Path.Combine(Path.GetTempPath(), "MAGICPRINT_publish_log.txt");

                        dsdPath = Path.Combine(Path.GetTempPath(), "MAGICPRINT_" + Guid.NewGuid().ToString("N") + ".dsd");
                        dsdData.WriteDsd(dsdPath);

                        // Официальный пример Autodesk перечитывает только что записанный DSD
                        // обратно через отдельный DsdData/ReadDsd перед PublishExecute - тот
                        // же приём повторён здесь, не своя придумка.
                        using (DsdData dsdToPublish = new DsdData())
                        {
                            dsdToPublish.ReadDsd(dsdPath);
                            PlotConfig plotConfig = PlotConfigManager.SetCurrentConfig(PlotterName);
                            Application.Publisher.PublishExecute(dsdToPublish, plotConfig);
                        }
                    }
                }

                ed.WriteMessage($"\nMAGICPRINT: otpravleno na pechat {sheets.Count} listov v '{outputPdf}'.");
            }
            finally
            {
                if (dsdPath != null)
                {
                    try { File.Delete(dsdPath); }
                    catch (System.Exception) { /* временный файл, не критично если не удалился */ }
                }

                // Возвращаем пользователя на тот лист, что был активен до запуска команды -
                // BuildPlotInfo переключал текущий лист по очереди при подготовке каждого
                // листа к печати.
                try { LayoutManager.Current.CurrentLayout = originalActiveLayout; }
                catch (System.Exception) { /* лист удалён/недоступен - остаёмся где есть */ }

                // Возвращаем BACKGROUNDPLOT в то значение, что было до команды - не наше
                // дело менять пользовательские настройки насовсем, только на время печати.
                Application.SetSystemVariable("BACKGROUNDPLOT", originalBackgroundPlot);
            }
        }

        // Собирает и валидирует PlotInfo для одного листа - вынесено в отдельный метод,
        // потому что вызывается по одному разу на каждый лист внутри цикла печати выше.
        //
        // ВАЖНО (баг найден и исправлен 2026-08-19, третий крэш подряд после фиксов
        // FindMatchingMedia и SetPlotType/SetPlotWindowArea): PlotInfoValidator.Validate
        // требует, чтобы ПРОВЕРЯЕМЫЙ лист был ТЕКУЩИМ активным листом чертежа (вкладкой) -
        // иначе падает с "Autodesk.AutoCAD.Runtime.Exception: eLayoutNotCurrent". При
        // печати НЕСКОЛЬКИХ листов подряд (наш случай - MAGICPRINT перебирает их все)
        // перед КАЖДЫМ вызовом Validate нужно переключать LayoutManager.Current.CurrentLayout
        // на этот конкретный лист - подтверждено официальным примером Autodesk "Driving a
        // multi-sheet AutoCAD plot" (keanw.com), не только моя догадка.
        private static PlotInfo BuildPlotInfo(SheetPlan sheet)
        {
            LayoutManager.Current.CurrentLayout = sheet.LayoutName;

            using (Transaction tr = sheet.LayoutId.Database.TransactionManager.StartTransaction())
            {
                Layout layout = (Layout)tr.GetObject(sheet.LayoutId, OpenMode.ForRead);

                PlotInfo pi = new PlotInfo { Layout = sheet.LayoutId };

                PlotSettings ps = new PlotSettings(layout.ModelType);
                ps.CopyFrom(layout);

                PlotSettingsValidator psv = PlotSettingsValidator.Current;

                // Один вызов сразу задаёт и плоттер, и размер бумаги (по имени, которое мы
                // подобрали в FindMatchingMedia) - psv сам проверяет, что такой плоттер и
                // такой размер существуют, и бросит исключение, если нет.
                psv.SetPlotConfigurationName(ps, PlotterName, sheet.MediaName);
                psv.SetPlotPaperUnits(ps, PlotPaperUnit.Millimeters);

                // Поворот листа - ставим СРАЗУ после плоттера/бумаги, ДО окна печати.
                //
                // ВАЖНО (баг найден и исправлен 2026-08-19, ПОСЛЕ третьего крэша, уже на
                // этапе визуальной проверки а не крэша): на всех листах, где rotate=true
                // (A3/A2/A1/A0 - альбомная рамка на портретную бумагу), в PDF печаталась
                // только часть штампа, а не вся рамка/содержимое - только у A4
                // (единственный без поворота) печать выходила полностью верной. Раньше
                // SetPlotRotation вызывался ПОСЛЕДНИМ, уже после SetPlotWindowArea/
                // SetPlotOrigin/SetPlotCentered - похоже, окно и origin, заданные ДО того,
                // как повернуть бумагу, интерпретировались в старой (неповёрнутой) системе
                // координат бумаги и переставали совпадать с реальной after-поворота
                // печатной областью. Перенесено в начало - как и оба предыдущих бага в
                // этом файле, дело оказалось в порядке вызовов этого API, не в логике.
                double width = sheet.WindowMax.X - sheet.WindowMin.X;
                double height = sheet.WindowMax.Y - sheet.WindowMin.Y;
                psv.SetPlotRotation(ps, sheet.Rotate && width > height ? PlotRotation.Degrees090 : PlotRotation.Degrees000);

                // PlotType.Window - печатаем не "всё, что есть на листе", а именно
                // прямоугольник рамки, которую нашли (WindowMin/WindowMax) - так на
                // печать не попадёт ничего за пределами рамки листа (штампы соседних
                // листов, служебные пометки и т.п.).
                //
                // ВАЖНО (баг найден и исправлен 2026-08-19, второй крэш подряд после
                // фикса FindMatchingMedia): порядок вызовов ОБЯЗАТЕЛЕН именно такой -
                // SetPlotWindowArea ДО SetPlotType(Window), а не после. У меня было
                // наоборот - SetPlotType(ps, Window) падал с
                // "Autodesk.AutoCAD.Runtime.Exception: eInvalidInput", потому что на
                // момент вызова окно печати ещё не задано (у ps, скопированного из
                // layout.CopyFrom выше, окно печати - "пустое"/вырожденное, раз лист
                // раньше печатался не через PlotType.Window) - подтверждено на форумах
                // Autodesk, не только моя догадка.
                psv.SetPlotWindowArea(ps, new Extents2d(sheet.WindowMin, sheet.WindowMax));
                psv.SetPlotType(ps, Autodesk.AutoCAD.DatabaseServices.PlotType.Window);

                // Масштаб 1:1 - рамка листа печатается в натуральную величину бумаги
                // (пользователь выбрал этот вариант явно, а не "вписать в лист").
                psv.SetUseStandardScale(ps, true);
                psv.SetStdScaleType(ps, StdScaleType.StdScale1To1);

                // ЦЕНТРИРОВАТЬ, не смещать вручную через (0,0).
                //
                // ВАЖНО (диагностика 2026-08-19, после двух безрезультатных попыток - смены
                // порядка вызовов и проверки Validate()): DIAG2 подтвердил, что после
                // Validate() PlotWindowArea/PlotRotation/PlotOrigin(0,0)/PlotCentered=false -
                // всё остаётся ТОЧНО таким, каким мы задали, но PDF всё равно обрезан на
                // повёрнутых листах (A3/A2/A1/A0), и только у A4 (без поворота) всё верно.
                // Похоже, при PlotRotation != Degrees000 связка "origin=(0,0) + не
                // центрировано" не работает так, как ожидается - origin(0,0) интерпретируется
                // не в системе координат уже повёрнутой бумаги. Раз найденное окно печати по
                // размеру ровно совпадает с бумагой (мы сами подбираем бумагу под размер
                // рамки), центрирование должно давать ТОТ ЖЕ результат без полей - но без
                // завязки на origin(0,0), который, похоже, и ломается при повороте.
                psv.SetPlotCentered(ps, true);

                pi.OverrideSettings = ps;

                PlotInfoValidator piv = new PlotInfoValidator { MediaMatchingPolicy = MatchingPolicy.MatchEnabled };
                piv.Validate(pi);

                // "Apply to Layout" - то же самое, что кнопка "Apply to Layout" в обычном
                // диалоге печати AutoCAD (Файл -> Печать -> Window -> "Apply to Layout"):
                // сохраняем итоговые (уже провалидированные psv/piv) настройки печати прямо
                // в сам объект Layout чертежа, а не только используем их "на лету" для
                // текущей печати. Без этого шага следующая РУЧНАЯ печать этого листа
                // (Ctrl+P) в диалоге показывала бы старые/пустые настройки Window, а не
                // тот прямоугольник рамки, который MAGICPRINT только что нашёл и напечатал -
                // Layout нужно открыть на запись (UpgradeOpen), он был открыт ForRead выше.
                layout.UpgradeOpen();
                layout.CopyFrom(ps);

                tr.Commit();
                return pi;
            }
        }

        // ====================================================================================
        // Поиск ВСЕХ замкнутых полилиний на листе (не только самой большой) - отсортированы
        // по убыванию площади. Раньше здесь искалась только САМАЯ БОЛЬШАЯ полилиния, и её
        // размер считался рамкой листа безусловно; теперь вызывающий код (Step 1 выше)
        // перебирает весь список и берёт первую, чей размер реально совпал с известным
        // форматом бумаги - на листе может быть несколько замкнутых контуров (сама рамка,
        // ячейки штампа и т.п.), и самый большой по площади не всегда и есть настоящая рамка.
        // ====================================================================================
        private static List<Extents3d> FindClosedPolylineCandidates(Transaction tr, BlockTableRecord layoutBtr)
        {
            var candidates = new List<(Extents3d Ext, double Area)>();

            foreach (ObjectId id in layoutBtr)
            {
                // "as Polyline" - облегчённая полилиния (LWPOLYLINE), самый частый тип в
                // современных чертежах. Старые 2D/3D-полилинии (Polyline2d/Polyline3d) тут
                // намеренно не проверяются - если рамки в старых чертежах ими начерчены,
                // этот метод их не найдёт (можно добавить позже, если понадобится).
                Polyline pl = tr.GetObject(id, OpenMode.ForRead) as Polyline;
                if (pl == null || !pl.Closed) continue;

                Extents3d ext = pl.GeometricExtents;
                double area = (ext.MaxPoint.X - ext.MinPoint.X) * (ext.MaxPoint.Y - ext.MinPoint.Y);
                candidates.Add((ext, area));
            }

            return candidates.OrderByDescending(c => c.Area).Select(c => c.Ext).ToList();
        }

        // ====================================================================================
        // Подбор имени бумаги в PC3 под измеренный размер рамки (в мм).
        // ====================================================================================
        // Возвращает имя канонического размера бумаги, если нашли подходящий, иначе null.
        // "rotate" (out) - true, если для точного совпадения с найденным ISO-размером рамку
        // нужно повернуть (то есть совпадение нашлось по перевёрнутым width/height).
        //
        // ВАЖНО (баг найден и исправлен 2026-08-19): раньше тут был "PlotSettings(false)" -
        // ПУСТОЙ, ни к какому реальному листу не привязанный объект, и следующая строка
        // "SetPlotConfigurationName(tempPs, PlotterName, null)" на нём падала с
        // "Autodesk.AutoCAD.Runtime.Exception: eInvalidInput" (поймано пользователем на
        // реальном тесте) - похоже, "null" в качестве имени бумаги означает "оставь как
        // было", а у пустого PlotSettings просто нет предыдущего валидного значения, чтобы
        // "оставить". Исправлено: сначала копируем НАСТОЯЩИЕ настройки существующего листа
        // (layout.CopyFrom) - точно так же, как уже делает BuildPlotInfo ниже - тогда у
        // tempPs есть валидное "текущее" имя бумаги, и null ("не менять его") больше не
        // ломается.
        private static string FindMatchingMedia(Layout layout, double widthMm, double heightMm, out bool rotate)
        {
            rotate = false;
            double shortSide = Math.Min(widthMm, heightMm);
            double longSide = Math.Max(widthMm, heightMm);

            PlotSettingsValidator psv = PlotSettingsValidator.Current;

            // Список "канонических" имён бумаги, которые ДЕЙСТВИТЕЛЬНО существуют в этом
            // PC3 прямо сейчас (стандартные + всё, что пользователь когда-либо добавил
            // вручную через мастер) - вместо того чтобы гадать точную строку названия "вслепую".
            PlotSettings tempPs = new PlotSettings(layout.ModelType);
            tempPs.CopyFrom(layout);
            psv.SetPlotConfigurationName(tempPs, PlotterName, null);
            System.Collections.Specialized.StringCollection mediaNames = psv.GetCanonicalMediaNameList(tempPs);

            // Ищем точный стандартный ISO-размер (в пределах SizeTolerance).
            foreach (var (label, isoShort, isoLong) in IsoSizes)
            {
                bool matches = Math.Abs(shortSide - isoShort) <= SizeTolerance &&
                               Math.Abs(longSide - isoLong) <= SizeTolerance;
                if (!matches) continue;

                string found = FindMediaNameContaining(mediaNames, "full_bleed", label)
                            ?? FindMediaNameContaining(mediaNames, "iso", label);
                if (found != null)
                {
                    rotate = widthMm > heightMm; // рамка лежит "на боку" относительно портретного листа
                    return found;
                }
            }

            // Точного стандартного совпадения нет - сначала проверяем, нет ли уже
            // вручную добавленного (через мастер AutoCAD, старым способом) размера по
            // договорённому имени "CUSTOM <short>x<long>mm".
            string customName = $"CUSTOM {shortSide:F0}x{longSide:F0}mm";
            string customFound = FindMediaNameExact(mediaNames, customName);
            if (customFound != null)
            {
                rotate = widthMm > heightMm;
                return customFound;
            }

            // Совсем ничего подходящего нет - регистрируем размер САМИ, прямо в .pmp-файл
            // плоттера (см. EnsureCustomMediaSize) - у реальных чертежей десятки
            // уникальных нестандартных длин (балки/ригели разной длины), вручную
            // добавлять каждую через мастер было бы неприемлемо медленно (2026-08-20,
            // жалоба пользователя - "18 листов пропущено" на реальном чертеже). Размер
            // создаётся в ЕГО СОБСТВЕННОЙ (уже правильной) ориентации рамки - поворот
            // бумаги не нужен, раз сама бумага того же размера, что и рамка, без
            // привязки к портретному/альбомному канону ISO.
            string autoName = EnsureCustomMediaSize(layout, widthMm, heightMm);
            if (autoName != null)
            {
                rotate = false;
                return autoName;
            }

            return null;
        }

        // ====================================================================================
        // Автоматическая регистрация нестандартного размера бумаги ПРЯМО в .pmp-файл
        // плоттера (без мастера AutoCAD "Диспетчер плоттеров").
        // ====================================================================================
        // ВАЖНО (найдено и подтверждено 2026-08-20): современные .pc3/.pmp файлы AutoCAD
        // (проверено на реальных файлах 2024/2026) оказались обычным ЧИТАЕМЫМ JSON
        // (заголовок "PIAFILEVERSION_3.0,json"), а не бинарным форматом - старый
        // комментарий в начале файла про "нет .NET/COM API для программного добавления
        // custom paper size" по-прежнему верен ЧЕРЕЗ ОФИЦИАЛЬНЫЙ API, но сам файл
        // можно читать/писать напрямую как обычный текст, в обход этого ограничения.
        // Пользовательские размеры бумаги хранятся в СВЯЗАННОМ .pmp-файле (путь - прямо
        // в .pc3, поле meta.user_defined_model_pathname), в разделе "udm" (User Defined
        // Model), в двух синхронных списках:
        //  - data.udm.media.description - сами размеры (ширина/высота/область печати),
        //  - data.udm.media.size - их "человеческие" и канонические имена, ссылающиеся
        //    на запись выше по полю "media_description_name". Именно поле "name" из
        //    ЭТОГО списка - тот самый канонический "UserDefinedMetric (WxH MM)", который
        //    видит GetCanonicalMediaNameList и принимает SetPlotConfigurationName.
        // Формат обеих записей подобран ТОЧНО по образцу существующих записей, которые
        // сам AutoCAD создал ранее (когда пользователь добавлял размеры вручную через
        // мастер в рамках этой же сессии отладки) - не придуман с нуля.
        //
        // Правим файл ТОЧЕЧНОЙ ВСТАВКОЙ новых записей (не полным перепарсингом JSON),
        // чтобы гарантированно не задеть/не переформатировать остальные сотни уже
        // существующих записей - это системный файл плоттера, общий для всего AutoCAD,
        // а не только для этого плагина, поэтому перед первой правкой всегда делаем
        // резервную копию (<файл>.magicprint_backup, только если её ещё нет - чтобы не
        // затереть чистый оригинал повторными запусками).
        //
        // Возвращает канонический "name" размера, который можно сразу передавать в
        // SetPlotConfigurationName - или null, если что-то пошло не так (файл плоттера
        // не найден, неожиданная структура и т.п.) - тогда лист просто пропускается, как
        // раньше, а не падает с необработанным исключением.
        private static string EnsureCustomMediaSize(Layout layout, double widthMm, double heightMm)
        {
            string canonicalName = $"UserDefinedMetric ({widthMm:F2} x {heightMm:F2}MM)";
            Editor diagEd = Application.DocumentManager.MdiActiveDocument.Editor;

            try
            {
                // ВАЖНО (исправлено 2026-08-20, после реального теста): HostApplicationServices.
                // FindFile с FindFileHint.Default НЕ годится для поиска .pc3 - на реальном
                // тесте бросал "Autodesk.AutoCAD.Runtime.Exception: eFilerError" вместо того
                // чтобы просто не найти файл. .pc3-файлы лежат не там, где ищет Default (это
                // хинт для файлов чертежа - шрифты, xref и т.п.), а в отдельной папке
                // плоттеров, путь к которой даёт HostApplicationServices.RoamableRootFolder
                // (например "...\AutoCAD 2024\R24.3\enu") + "\Plotters" - подтверждено на
                // реальных .pc3-файлах этой же машины при исследовании их формата.
                string pc3Path = Path.Combine(HostApplicationServices.Current.RoamableRootFolder, "Plotters", PlotterName);
                if (!File.Exists(pc3Path))
                {
                    diagEd.WriteMessage($"\nMAGICPRINT DIAG3: .pc3 ne naiden po puti '{pc3Path}'.");
                    return null;
                }

                string pc3Text = File.ReadAllText(pc3Path);
                System.Text.RegularExpressions.Match pmpMatch = System.Text.RegularExpressions.Regex.Match(
                    pc3Text, "\"user_defined_model_pathname\"\\s*:\\s*\"([^\"]*)\"");
                if (!pmpMatch.Success)
                {
                    diagEd.WriteMessage($"\nMAGICPRINT DIAG3: user_defined_model_pathname ne naiden v pc3 '{pc3Path}'.");
                    return null;
                }

                string pmpPath = pmpMatch.Groups[1].Value.Replace("\\\\", "\\");
                if (string.IsNullOrEmpty(pmpPath) || !File.Exists(pmpPath))
                {
                    diagEd.WriteMessage($"\nMAGICPRINT DIAG3: pmp fail ne naiden ('{pmpPath}').");
                    return null;
                }

                string text = File.ReadAllText(pmpPath);

                // Уже добавляли этот же размер раньше (в этом или прошлом запуске) -
                // ничего делать не нужно, просто используем существующую запись.
                if (text.Contains($"\"name\" : \"{canonicalName}\""))
                    return canonicalName;

                int udmIdx = text.IndexOf("\"udm\"", StringComparison.Ordinal);
                if (udmIdx < 0)
                {
                    diagEd.WriteMessage($"\nMAGICPRINT DIAG3: 'udm' ne naiden v pmp '{pmpPath}'.");
                    return null;
                }

                string afterUdm = text.Substring(udmIdx);
                System.Text.RegularExpressions.Match descMatch = System.Text.RegularExpressions.Regex.Match(
                    afterUdm, "\"description\"\\s*:\\s*\\{");
                System.Text.RegularExpressions.Match sizeMatch = System.Text.RegularExpressions.Regex.Match(
                    afterUdm, "\"size\"\\s*:\\s*\\{");
                if (!descMatch.Success || !sizeMatch.Success)
                {
                    diagEd.WriteMessage($"\nMAGICPRINT DIAG3: description/size ne naideny v udm-sektsii '{pmpPath}'.");
                    return null;
                }

                int descInsertPos = udmIdx + descMatch.Index + descMatch.Length;
                int sizeInsertPos = udmIdx + sizeMatch.Index + sizeMatch.Length;

                bool landscape = widthMm > heightMm;
                double area = widthMm * heightMm;
                string orientation = landscape ? "Landscape" : "Portrait";
                string entryKey = $"MP_{widthMm:F0}x{heightMm:F0}";
                string descName = $"UserDefinedMetric {orientation} {widthMm:F2}W x {heightMm:F2}H - " +
                                   $"(0, 0) x ({widthMm:F0}, {heightMm:F0}) ={area:F0} MM";

                string descEntry =
                    $"\n     \"{entryKey}\" : \n     {{\n" +
                    "      \"caps_type\" : 2,\n" +
                    "      \"dimensional\" : true,\n" +
                    $"      \"media_bounds_urx\" : {widthMm:F1},\n" +
                    $"      \"media_bounds_ury\" : {heightMm:F1},\n" +
                    $"      \"name\" : \"{descName}\",\n" +
                    $"      \"printable_area\" : {area:F1},\n" +
                    "      \"printable_bounds_llx\" : 0.0,\n" +
                    "      \"printable_bounds_lly\" : 0.0,\n" +
                    $"      \"printable_bounds_urx\" : {widthMm:F1},\n" +
                    $"      \"printable_bounds_ury\" : {heightMm:F1}\n" +
                    "     },";

                string sizeEntry =
                    $"\n     \"{entryKey}\" : \n     {{\n" +
                    "      \"caps_type\" : 2,\n" +
                    $"      \"landscape_mode\" : {(landscape ? "true" : "false")},\n" +
                    $"      \"localized_name\" : \"MAGICPRINT ({widthMm:F2} x {heightMm:F2} MM)\",\n" +
                    $"      \"media_description_name\" : \"{descName}\",\n" +
                    "      \"media_group\" : 15,\n" +
                    $"      \"name\" : \"{canonicalName}\"\n" +
                    "     },";

                // Вставляем СНАЧАЛА в более позднюю по тексту позицию (size), потом в
                // более раннюю (description) - иначе первая же вставка сдвинула бы уже
                // вычисленный индекс для второй.
                string afterSize = text.Substring(0, sizeInsertPos) + sizeEntry + text.Substring(sizeInsertPos);
                string finalText = afterSize.Substring(0, descInsertPos) + descEntry + afterSize.Substring(descInsertPos);

                string backupPath = pmpPath + ".magicprint_backup";
                if (!File.Exists(backupPath))
                    File.Copy(pmpPath, backupPath);

                File.WriteAllText(pmpPath, finalText);
                return canonicalName;
            }
            catch (System.Exception ex)
            {
                // Любая проблема с чтением/правкой системного файла плоттера - не валим
                // всю команду, просто не добавляем размер (лист будет пропущен, как
                // и раньше, до этой автоматизации).
                diagEd.WriteMessage($"\nMAGICPRINT DIAG3: isklyuchenie - {ex.GetType().Name}: {ex.Message}");
                return null;
            }
        }

        // Сравнение "без учёта пробелов/подчёркиваний/регистра" - разные версии/локализации
        // AutoCAD хранят имена размеров бумаги то с пробелами, то с подчёркиваниями.
        private static string Normalize(string s) =>
            s.Replace(" ", "").Replace("_", "").ToUpperInvariant();

        // Разбивает имя бумаги на отдельные "слова" по любому разделителю (пробел,
        // подчёркивание, скобки, точка) - используется вместо простого IndexOf/Contains
        // для поиска размера по ЦЕЛОМУ слову, а не по случайному вхождению подстроки.
        private static string[] Tokenize(string s) =>
            System.Text.RegularExpressions.Regex.Split(s.ToUpperInvariant(), @"[^A-Z0-9]+")
                .Where(t => t.Length > 0).ToArray();

        // ВАЖНО (баг найден и исправлен 2026-08-19, найден через диагностику после
        // визуальной проверки PDF, не крэш): лист "A0" (841x1189мм) подобрал бумагу
        // "ISO_full_bleed_2A0_(1189.00_x_1682.00_MM)" - это 2A0 (ВДВОЕ больше A0!), а не
        // A0. Причина - старая реализация искала размер через Normalize(name).Contains(...),
        // то есть просто "содержит подстроку без пробелов/подчёркиваний" - а "2A0"
        // ТОЖЕ содержит подстроку "A0" в конце. Из-за этого окно печати 841x1189 попадало
        // на бумагу 1189x1682 - совсем другого размера, отсюда и пустой лист. Теперь ищем
        // размер как ОТДЕЛЬНЫЙ токен ("2A0" и "A0" - разные, не совпадающие токены), а не
        // как подстроку где угодно внутри имени.
        private static string FindMediaNameContaining(System.Collections.Specialized.StringCollection names, params string[] mustContainAllTokens)
        {
            string[] required = mustContainAllTokens.SelectMany(Tokenize).ToArray();
            foreach (string name in names)
            {
                string[] tokens = Tokenize(name);
                if (required.All(r => tokens.Contains(r)))
                    return name;
            }
            return null;
        }

        private static string FindMediaNameExact(System.Collections.Specialized.StringCollection names, string expected)
        {
            string normalizedExpected = Normalize(expected);
            foreach (string name in names)
                if (Normalize(name) == normalizedExpected) return name;
            return null;
        }

        // Описание одного листа, который будем печатать - собрано на шаге 1, используется
        // на шаге 3 (BuildPlotInfo вызывается по этой структуре, не по самому Layout).
        private class SheetPlan
        {
            public ObjectId LayoutId;
            public string LayoutName;
            public Point2d WindowMin;
            public Point2d WindowMax;
            public string MediaName;
            public bool Rotate;
        }
    }
}
