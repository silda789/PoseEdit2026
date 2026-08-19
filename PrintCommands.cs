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

                    Extents3d? frame = FindLargestClosedPolylineExtents(tr, layoutBtr);
                    if (frame == null)
                    {
                        skipped.Add($"{name}: zamknutaya polyliniya (ramka lista) ne naidena.");
                        continue;
                    }

                    double widthMm = frame.Value.MaxPoint.X - frame.Value.MinPoint.X;
                    double heightMm = frame.Value.MaxPoint.Y - frame.Value.MinPoint.Y;

                    string mediaName = FindMatchingMedia(layout, widthMm, heightMm, out bool rotate);
                    if (mediaName == null)
                    {
                        double shortSide = Math.Min(widthMm, heightMm);
                        double longSide = Math.Max(widthMm, heightMm);
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
            // ШАГ 3: печать. doc.LockDocument() - обязательная блокировка документа перед
            // тем, как менять/использовать его через код (тот же приём, что и во всех
            // остальных командах этого проекта, которые правят чертёж программно).
            //
            // BuildPlotInfo переключает текущий лист (LayoutManager.Current.CurrentLayout)
            // для каждого печатаемого листа по очереди - запоминаем, какой лист был активен
            // ДО запуска команды, чтобы вернуть пользователя туда же после печати (тот же
            // приём save/restore, что и в RoutineCommands.LAYSHIFT/LAYRENUM).
            //
            // BACKGROUNDPLOT - принудительно ставим в 0 (= "печатать только на переднем
            // плане, последовательно") на время печати. Если у пользователя эта переменная
            // была не 0 (AutoCAD по умолчанию часто ставит 2 = "фоновая печать разрешена
            // для Plot и Publish"), AutoCAD может пытаться параллельно фоново обработать
            // наш пакетный PlotEngine - это прямо названо "критичным требованием" в
            // официальном примере Autodesk по программному плоттингу, и раньше было упущено
            // при портировании - могло быть одной из причин "зависания"/медленной печати.
            int originalBackgroundPlot = (short)Application.GetSystemVariable("BACKGROUNDPLOT");
            Application.SetSystemVariable("BACKGROUNDPLOT", 0);
            string originalActiveLayout = LayoutManager.Current.CurrentLayout;

            using (doc.LockDocument())
            try
            {
                PlotEngine pe = PlotFactory.CreatePublishEngine();
                using (pe)
                {
                    PlotProgressDialog ppd = new PlotProgressDialog(false, sheets.Count, true);
                    using (ppd)
                    {
                        ppd.set_PlotMsgString(PlotMessageIndex.DialogTitle, "MAGICPRINT");
                        ppd.set_PlotMsgString(PlotMessageIndex.CancelJobButtonMessage, "Cancel Job");
                        ppd.set_PlotMsgString(PlotMessageIndex.CancelSheetButtonMessage, "Cancel Sheet");
                        ppd.set_PlotMsgString(PlotMessageIndex.SheetSetProgressCaption, "Sheet Set Progress");
                        ppd.set_PlotMsgString(PlotMessageIndex.SheetProgressCaption, "Sheet Progress");
                        ppd.LowerPlotProgressRange = 0;
                        ppd.UpperPlotProgressRange = 100;
                        ppd.PlotProgressPos = 0;
                        ppd.OnBeginPlot();
                        ppd.IsVisible = true;

                        pe.BeginPlot(ppd, null);

                        // BeginDocument вызывается ОДИН РАЗ на весь пакет листов - именно
                        // это и превращает несколько отдельных страниц в ОДИН общий
                        // многостраничный PDF-файл, а не в отдельные файлы по одному на
                        // лист. PlotInfo первого листа передаётся сюда как "образец" -
                        // реальные настройки каждой страницы задаются в BeginPage ниже.
                        PlotInfo firstInfo = BuildPlotInfo(sheets[0]);
                        pe.BeginDocument(firstInfo, doc.Name, null, 1, true, outputPdf);

                        int printed = 0;
                        for (int i = 0; i < sheets.Count; i++)
                        {
                            PlotInfo pi = i == 0 ? firstInfo : BuildPlotInfo(sheets[i]);

                            ppd.OnBeginSheet();
                            ppd.LowerSheetProgressRange = 0;
                            ppd.UpperSheetProgressRange = 100;
                            ppd.SheetProgressPos = 0;

                            // "true" третьим параметром = "это последняя страница?" -
                            // важно передавать true ТОЛЬКО на самом последнем листе,
                            // иначе PDF получится оборванным/только с одной страницей.
                            bool isLast = i == sheets.Count - 1;
                            PlotPageInfo ppi = new PlotPageInfo();
                            pe.BeginPage(ppi, pi, isLast, null);
                            pe.BeginGenerateGraphics(null);
                            pe.EndGenerateGraphics(null);
                            pe.EndPage(null);

                            ppd.SheetProgressPos = 100;
                            ppd.OnEndSheet();
                            printed++;
                        }

                        pe.EndDocument(null);
                        ppd.PlotProgressPos = 100;
                        ppd.OnEndPlot();
                        pe.EndPlot(null);

                        ed.WriteMessage($"\nMAGICPRINT: napechatano listov {printed} v '{outputPdf}'.");
                    }
                }
            }
            finally
            {
                // Возвращаем пользователя на тот лист, что был активен до запуска команды -
                // BuildPlotInfo переключал текущий лист много раз подряд во время печати.
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

                // НЕ центрировать и НЕ смещать - левый нижний угол рамки должен точно
                // совпасть с левым нижним углом печатной области бумаги (Printable Area
                // = 0 со всех сторон, как и просили - при условии, что сама бумага в PC3
                // действительно без полей, см. комментарий в начале файла).
                psv.SetPlotCentered(ps, false);
                psv.SetPlotOrigin(ps, new Point2d(0, 0));

                // Если рамка "лежит" горизонтально (шире, чем выше), а стандартный размер
                // бумаги в PC3 определён как "портретный" (короткая сторона по X) -
                // поворачиваем бумагу на 90°, чтобы содержимое поместилось правильной
                // стороной, а не обрезалось.
                double width = sheet.WindowMax.X - sheet.WindowMin.X;
                double height = sheet.WindowMax.Y - sheet.WindowMin.Y;
                psv.SetPlotRotation(ps, sheet.Rotate ? (width > height ? PlotRotation.Degrees090 : PlotRotation.Degrees000)
                                                       : PlotRotation.Degrees000);

                pi.OverrideSettings = ps;

                PlotInfoValidator piv = new PlotInfoValidator { MediaMatchingPolicy = MatchingPolicy.MatchEnabled };
                piv.Validate(pi);

                tr.Commit();
                return pi;
            }
        }

        // ====================================================================================
        // Поиск рамки листа: самая большая (по площади) ЗАМКНУТАЯ полилиния на листе.
        // ====================================================================================
        private static Extents3d? FindLargestClosedPolylineExtents(Transaction tr, BlockTableRecord layoutBtr)
        {
            Extents3d? best = null;
            double bestArea = -1;

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
                if (area <= bestArea) continue;

                bestArea = area;
                best = ext;
            }

            return best;
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

            // Точного стандартного совпадения нет - пробуем найти вручную добавленный
            // custom-размер по договорённому имени "CUSTOM <short>x<long>mm" (см. большой
            // комментарий в начале файла про то, как его создать в мастере AutoCAD).
            string customName = $"CUSTOM {shortSide:F0}x{longSide:F0}mm";
            string customFound = FindMediaNameExact(mediaNames, customName);
            if (customFound != null)
            {
                rotate = widthMm > heightMm;
                return customFound;
            }

            return null;
        }

        // Сравнение "без учёта пробелов/подчёркиваний/регистра" - разные версии/локализации
        // AutoCAD хранят имена размеров бумаги то с пробелами, то с подчёркиваниями.
        private static string Normalize(string s) =>
            s.Replace(" ", "").Replace("_", "").ToUpperInvariant();

        private static string FindMediaNameContaining(System.Collections.Specialized.StringCollection names, params string[] mustContainAll)
        {
            foreach (string name in names)
            {
                string normalized = Normalize(name);
                if (mustContainAll.All(part => normalized.Contains(Normalize(part))))
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
