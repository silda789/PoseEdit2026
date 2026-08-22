// ====================================================================================
// ФАЙЛ: RoutineCommands.cs
// НАЗНАЧЕНИЕ: Мелкие вспомогательные команды для рутинных операций в AutoCAD, которые
//             не связаны напрямую с редактированием блоков RL-POS (в отличие от
//             Commands.cs / LegacyCommands.cs). Новые "быстрые" команды добавляются сюда.
// ====================================================================================
#nullable disable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;

namespace PoseEdit2026
{
    public static class RoutineCommands
    {
        // Шаблон имени листа: необязательный текстовый префикс, число, необязательный суффикс.
        // Например "55" -> префикс "", номер 55, суффикс "". "Лист 55" -> префикс "Лист ", номер 55.
        // Используется только LAYSHIFT'ом - LAYRENUM больше не парсит старое имя вообще
        // (см. комментарий у LAYRENUM про CTAB-поле на штампе).
        private static readonly Regex NumberedLayoutPattern = new Regex(@"^(\D*)(\d+)(\D*)$");

        // ====================================================================================
        // КОМАНДА: LAYSHIFT — сдвинуть номера всех листов (Layout) на константу
        // ====================================================================================
        // НАЗНАЧЕНИЕ: когда в чертеже ~100-200 листов, пронумерованных, например, от 55 до 199,
        // и нужно перенумеровать их все разом (например в 66-211, сдвиг +11), не открывая
        // каждый лист вручную через "Rename Layout".
        //
        // КАК РАБОТАЕТ:
        // 1. Собираем все листы, кроме "Model", у которых имя содержит число
        //    (например "55", "Лист 55", "55_Разрез").
        // 2. Спрашиваем у пользователя число сдвига (может быть отрицательным - для уменьшения
        //    номеров).
        // 3. Переименовываем каждый лист: новый номер = старый номер + сдвиг; префикс/суффикс
        //    и ширина числа (ведущие нули) не трогаются.
        //
        // ВАЖНАЯ ДЕТАЛЬ: AutoCAD не разрешает двум листам иметь одинаковое имя одновременно.
        // Если сдвиг положительный (номера растут) - переименовываем от САМОГО БОЛЬШОГО номера
        // к самому маленькому: тогда новое имя листа никогда временно не совпадёт с именем ещё
        // не переименованного листа. Если сдвиг отрицательный - наоборот, от меньшего к большему.
        [CommandMethod("LAYSHIFT")]
        public static void ShiftLayoutNumbersCommand()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            if (doc == null) return;
            Editor ed = doc.Editor;
            Database db = doc.Database;

            PromptIntegerOptions shiftOpts = new PromptIntegerOptions(
                "\nShift layout numbers by (e.g. 11, or -5): ")
            {
                AllowNegative = true,
                AllowZero = false
            };
            PromptIntegerResult shiftRes = ed.GetInteger(shiftOpts);
            if (shiftRes.Status != PromptStatus.OK) return;
            int shift = shiftRes.Value;

            // Запоминаем активный лист, чтобы вернуться на него (или на его новое имя) в конце -
            // тот же паттерн save/restore, что используется во всём проекте (см. CLAUDE.md).
            string originalActiveLayout = LayoutManager.Current.CurrentLayout;

            using (doc.LockDocument())
            {
                var items = new List<(string OldName, string Prefix, int Number, string Suffix)>();

                using (Transaction tr = db.TransactionManager.StartTransaction())
                {
                    DBDictionary layoutDict = (DBDictionary)tr.GetObject(db.LayoutDictionaryId, OpenMode.ForRead);
                    foreach (DBDictionaryEntry entry in layoutDict)
                    {
                        Layout layout = tr.GetObject(entry.Value, OpenMode.ForRead) as Layout;
                        if (layout == null || layout.ModelType) continue; // пропускаем "Model"

                        Match m = NumberedLayoutPattern.Match(entry.Key);
                        if (!m.Success) continue; // в имени нет числа - пропускаем

                        items.Add((entry.Key, m.Groups[1].Value, int.Parse(m.Groups[2].Value), m.Groups[3].Value));
                    }
                    tr.Commit();
                }

                if (items.Count == 0)
                {
                    ed.WriteMessage("\nNo layouts with a number in their name found.");
                    return;
                }

                // Порядок переименования, исключающий временное совпадение имён (см. выше).
                IEnumerable<(string OldName, string Prefix, int Number, string Suffix)> ordered =
                    shift > 0 ? items.OrderByDescending(i => i.Number) : items.OrderBy(i => i.Number);

                // Переключаемся на "Model" на время переименования - так текущий активный лист
                // никогда не совпадёт с листом, который мы в данный момент переименовываем.
                LayoutManager.Current.CurrentLayout = "Model";

                var renameMap = new Dictionary<string, string>();
                int renamed = 0;
                int skipped = 0;
                try
                {
                    foreach (var it in ordered)
                    {
                        int newNumber = it.Number + shift;
                        if (newNumber <= 0)
                        {
                            ed.WriteMessage($"\nSkipped layout '{it.OldName}': new number {newNumber} <= 0.");
                            skipped++;
                            continue;
                        }

                        // Сохраняем ширину числа с ведущими нулями (например "055" -> "066", не "66").
                        int digitWidth = it.OldName.Length - it.Prefix.Length - it.Suffix.Length;
                        string newNumberText = newNumber.ToString().PadLeft(digitWidth, '0');
                        string newName = $"{it.Prefix}{newNumberText}{it.Suffix}";

                        if (newName == it.OldName) continue;

                        try
                        {
                            LayoutManager.Current.RenameLayout(it.OldName, newName);
                            renameMap[it.OldName] = newName;
                            renamed++;
                        }
                        catch (System.Exception ex)
                        {
                            ed.WriteMessage($"\nFailed to rename '{it.OldName}' -> '{newName}': {ex.Message}");
                            skipped++;
                        }
                    }
                }
                finally
                {
                    // Возвращаемся на исходный активный лист - или на его новое имя, если он
                    // сам оказался среди перенумерованных.
                    string restoreTo = renameMap.TryGetValue(originalActiveLayout, out string mapped)
                        ? mapped
                        : originalActiveLayout;
                    try { LayoutManager.Current.CurrentLayout = restoreTo; }
                    catch (System.Exception) { /* лист удалён/недоступен - остаёмся на Model */ }
                }

                ed.WriteMessage($"\nDone. Layouts renamed: {renamed}, skipped: {skipped}.");
            }
        }

        // ====================================================================================
        // КОМАНДА: LAYRENUM — перенумеровать листы (Layout) по порядку, начиная с заданного числа
        // ====================================================================================
        // НАЗНАЧЕНИЕ: в отличие от LAYSHIFT (который добавляет константу к каждому старому
        // номеру), эта команда просто раздаёт номера подряд, по порядку вкладок в AutoCAD:
        // задал начальное число 55 - первый лист по порядку станет "55", следующий "56", потом
        // "57", "58", "59" и т.д. Старые номера листов при этом не важны вообще - используется
        // только порядок, в котором вкладки идут в самом AutoCAD (Layout.TabOrder).
        //
        // КАК РАБОТАЕТ:
        // 1. Собираем ВСЕ листы, кроме "Model", сортируем по порядку вкладок (как они идут
        //    внизу экрана в AutoCAD слева направо) - имя листа при отборе не важно вообще,
        //    берутся абсолютно все, независимо от того, что там сейчас написано.
        // 2. Спрашиваем начальное число.
        // 3. Присваиваем номера по порядку: 1-й лист = начальное число, 2-й = +1, и т.д. Новое
        //    имя - ЧИСТОЕ число, без всякого префикса/суффикса от старого имени (просто "55",
        //    "56", ...).
        //
        // ВАЖНО (изменено 2026-08-20 по просьбе пользователя, после бага с молчаливым
        // пропуском - см. историю ниже): раньше команда сохраняла префикс/суффикс старого
        // имени (например "Лист 55" -> "Лист 56"). Пользователь объяснил реальную причину,
        // почему это не нужно: на штампе номер листа - это AutoCAD Field, завязанный на
        // системную переменную CTAB (имя ТЕКУЩЕГО активного листа) - то есть штамп напрямую
        // показывает буквальное имя вкладки Layout. Значит листу нужно ЧИСТОЕ числовое имя
        // без текста, независимо от того, как он назывался раньше ("Layout1", "Лист 55",
        // что угодно) - отсюда и решение брать вообще ВСЕ листы (не только те, что уже
        // содержат число) и переименовывать их в голые числа.
        //
        // ВАЖНАЯ ДЕТАЛЬ: при сплошной перенумерации (не сдвиге) новый номер листа очень часто
        // совпадает со СТАРЫМ именем какого-то другого листа где-то в середине списка - здесь
        // нет гарантии порядка, как в LAYSHIFT. Поэтому переименование идёт в ДВА ПРОХОДА:
        // сначала все листы получают временные уникальные имена (гарантированно ни с чем не
        // совпадающие), потом только со второго прохода - настоящие итоговые номера.
        //
        // ИСТОРИЯ БАГА (2026-08-20, реальный тест: 10 Layouts - 5 форматов A4-A0, вставленных
        // в чертёж ДВАЖДЫ): раньше отбор шёл через regex, требовавший РОВНО ОДНО число во
        // всём имени - копии листов, переименованные самим AutoCAD в "Layout1 (2)" и т.п.
        // (два числа в имени), молча выбрасывались, отсюда "переименовано 5 из 10" без
        // единого сообщения. Сначала это было исправлено более мягким regex'ом, отдельным от
        // LAYSHIFT - но раз теперь имя листа вообще не сохраняется (см. выше), сама проблема
        // с распознаванием старого имени отпала: команда больше не парсит старое имя вообще,
        // просто берёт все листы по порядку вкладок.
        [CommandMethod("LAYRENUM")]
        public static void RenumberLayoutsSequentiallyCommand()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            if (doc == null) return;
            Editor ed = doc.Editor;
            Database db = doc.Database;

            PromptIntegerOptions startOpts = new PromptIntegerOptions(
                "\nStart layout renumbering from: ")
            {
                AllowNegative = false,
                AllowZero = true
            };
            PromptIntegerResult startRes = ed.GetInteger(startOpts);
            if (startRes.Status != PromptStatus.OK) return;
            int start = startRes.Value;

            string originalActiveLayout = LayoutManager.Current.CurrentLayout;

            using (doc.LockDocument())
            {
                var items = new List<(string OldName, int TabOrder)>();

                using (Transaction tr = db.TransactionManager.StartTransaction())
                {
                    DBDictionary layoutDict = (DBDictionary)tr.GetObject(db.LayoutDictionaryId, OpenMode.ForRead);
                    foreach (DBDictionaryEntry entry in layoutDict)
                    {
                        Layout layout = tr.GetObject(entry.Value, OpenMode.ForRead) as Layout;
                        if (layout == null || layout.ModelType) continue; // пропускаем "Model"

                        items.Add((entry.Key, layout.TabOrder));
                    }
                    tr.Commit();
                }

                if (items.Count == 0)
                {
                    ed.WriteMessage("\nNo layouts found (besides Model).");
                    return;
                }

                // Порядок как в самом AutoCAD (порядок вкладок), а не по старому имени -
                // именно так пользователь листает листы по очереди.
                var ordered = items.OrderBy(i => i.TabOrder).ToList();

                // Новое имя - ЧИСТОЕ число, без префикса/суффикса от старого имени (см.
                // комментарий у команды выше про CTAB-поле на штампе).
                var finalNames = new Dictionary<string, string>();
                for (int idx = 0; idx < ordered.Count; idx++)
                {
                    var it = ordered[idx];
                    int newNumber = start + idx;
                    finalNames[it.OldName] = newNumber.ToString();
                }

                LayoutManager.Current.CurrentLayout = "Model";

                var tempNames = new Dictionary<string, string>();
                int renamed = 0;
                int skipped = 0;
                try
                {
                    // Фаза 1: временные уникальные имена - чтобы ни одно новое имя случайно не
                    // совпало с ещё не переименованным старым именем.
                    foreach (var it in ordered)
                    {
                        string tempName = $"__LAYRENUM_TMP_{Guid.NewGuid():N}";
                        try
                        {
                            LayoutManager.Current.RenameLayout(it.OldName, tempName);
                            tempNames[it.OldName] = tempName;
                        }
                        catch (System.Exception ex)
                        {
                            ed.WriteMessage($"\nFailed to start renaming '{it.OldName}': {ex.Message}");
                            skipped++;
                        }
                    }

                    // Фаза 2: настоящие итоговые имена.
                    foreach (var it in ordered)
                    {
                        if (!tempNames.TryGetValue(it.OldName, out string tempName)) continue;
                        string finalName = finalNames[it.OldName];
                        try
                        {
                            LayoutManager.Current.RenameLayout(tempName, finalName);
                            renamed++;
                        }
                        catch (System.Exception ex)
                        {
                            ed.WriteMessage($"\nLayout '{it.OldName}' is stuck with temp name '{tempName}' - failed to rename to '{finalName}': {ex.Message}");
                            skipped++;
                        }
                    }
                }
                finally
                {
                    string restoreTo = finalNames.TryGetValue(originalActiveLayout, out string mapped)
                        ? mapped
                        : originalActiveLayout;
                    try { LayoutManager.Current.CurrentLayout = restoreTo; }
                    catch (System.Exception) { /* лист удалён/недоступен - остаёмся на Model */ }
                }

                ed.WriteMessage($"\nDone. Layouts renamed: {renamed}, skipped: {skipped}, starting from {start}.");
            }
        }

        // ====================================================================================
        // КОМАНДА: VPLOCKALL — заблокировать масштаб всех видовых экранов на всех листах
        // ====================================================================================
        // НАЗНАЧЕНИЕ: проходит по всем листам (Layout, кроме "Model"). На каждом листе
        // находит все видовые экраны (Viewport) и для каждого из них проверяет свойство
        // Locked (это то же самое, что команда AutoCAD "VPLOCK" или пункт правой кнопки
        // мыши на границе видового экрана "Display Locked" -> "Yes"). Если видовой
        // экран ещё НЕ заблокирован - блокирует. Уже заблокированные не трогает (чтобы
        // не создавать лишних записей в истории отмены - Undo).
        //
        // ПОЧЕМУ НЕ ФИЛЬТРУЕМ ПО Viewport.Number: у каждого листа есть один служебный
        // видовой экран - тот, что представляет сам лист бумаги (не то, что пользователь
        // считает "видовым экраном" на чертеже), обычно с Number == 1. Первая версия
        // этой команды пропускала такие (Number <= 1) - но оказалось, что
        // Viewport.Number надёжен ТОЛЬКО для видовых экранов АКТИВНОГО на момент запуска
        // листа; на всех остальных (неактивных) листах Number может быть "протухшим"
        // (например -1) у ЛЮБОГО видового экрана, включая настоящие - из-за этого
        // фильтр по Number отсекал почти все видовые экраны на чертеже с сотней+ листов,
        // и команда молча ничего не блокировала (баг найден пользователем 2026-08-16 -
        // протестировал на реальном чертеже с ~125 листами, ни один экран не
        // заблокировался). Раз различить "настоящий" и "служебный" видовой экран
        // надёжно нельзя без захода в каждый лист по очереди, а блокировка служебного
        // видового экрана листа безвредна (пользователь его никогда не видит и не
        // выделяет - это не отдельный объект на экране в виде листа), просто
        // обрабатываем ВСЕ найденные Viewport-объекты подряд.
        [CommandMethod("VPLOCKALL")]
        public static void LockAllViewportsCommand()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            if (doc == null) return;
            Editor ed = doc.Editor;
            Database db = doc.Database;

            using (doc.LockDocument())
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                DBDictionary layoutDict = (DBDictionary)tr.GetObject(db.LayoutDictionaryId, OpenMode.ForRead);

                int totalViewports = 0;
                int alreadyLocked = 0;
                int newlyLocked = 0;

                // Обходим все листы...
                foreach (DBDictionaryEntry entry in layoutDict)
                {
                    Layout layout = tr.GetObject(entry.Value, OpenMode.ForRead) as Layout;
                    if (layout == null || layout.ModelType) continue; // пропускаем "Model"

                    // Layout.BlockTableRecordId - это "содержимое" листа (все объекты,
                    // нарисованные на этом конкретном листе, включая видовые экраны) -
                    // тот же приём, что и с обычным ModelSpace, просто для другого листа.
                    BlockTableRecord layoutBtr = (BlockTableRecord)tr.GetObject(layout.BlockTableRecordId, OpenMode.ForRead);

                    // ...и все объекты внутри каждого листа, отбирая среди них только
                    // видовые экраны (остальное - линии, тексты, штампы и т.п. - нам
                    // тут не нужно, "as Viewport" вернёт null для всего, что не Viewport,
                    // и мы просто пропустим такой объект через "continue").
                    foreach (ObjectId entId in layoutBtr)
                    {
                        Viewport vp = tr.GetObject(entId, OpenMode.ForRead) as Viewport;
                        if (vp == null) continue;

                        totalViewports++;

                        if (vp.Locked)
                        {
                            alreadyLocked++;
                            continue;
                        }

                        // UpgradeOpen() - "переоткрывает" уже открытый на чтение (ForRead)
                        // объект на запись (ForWrite), не читая его из базы заново. Делаем
                        // это только для видовых экранов, которые ДЕЙСТВИТЕЛЬНО нужно
                        // менять - так уже заблокированные видовые экраны вообще не
                        // трогаются транзакцией.
                        vp.UpgradeOpen();
                        vp.Locked = true;
                        newlyLocked++;
                    }
                }

                tr.Commit();

                ed.WriteMessage($"\nVPLOCKALL: total viewports {totalViewports}, already locked {alreadyLocked}, locked now {newlyLocked}.");
            }
        }

        // ====================================================================================
        // КОМАНДА: PZSHAPECHECK — диагностика: что реально лежит в PZ_00..PZ_95.dwg, и
        // распознаёт ли их RebarRecognizer (мозговой штурм по улучшению Determination)
        // ====================================================================================
        // ВРЕМЕННАЯ диагностическая команда (2026-08-21), см. память сессии
        // "project_determination_recognition" - RebarRecognizer.cs (кнопка "Determination"
        // в EEN) распознаёт только ~10-20% реальных эскизов, которые чертит пользователь.
        // Прежде чем чинить/переделывать сам алгоритм, нужны факты: какие из ~95 known
        // "эталонных форм" (шаблоны PZ_00..PZ_95.dwg, уже встроены в проект - используются
        // PZREDEFN) вообще распознаются, и что именно распознаётся неверно.
        //
        // ВАЖНО: команда НЕ трогает текущий открытый чертёж вообще - каждый PZ_XX.dwg
        // открывается как ОТДЕЛЬНАЯ, не подключенная к документу Database (тот же приём
        // чтения, что и в PZREDEFN перед db.Insert - см. LegacyCommands.cs), поэтому её
        // можно гонять сколько угодно раз без всякого риска для активного чертежа и без
        // необходимости что-либо сохранять перед запуском.
        //
        // ЧТО ДЕЛАЕТ, для каждого PZ_00..PZ_95 (96 файлов):
        //   1. Извлекает embedded DWG-ресурс во временный файл.
        //   2. Открывает его как отдельную Database, читает Model Space.
        //   3. Ищет там объекты, которые умеет читать RebarRecognizer.GetPointsFromEntity -
        //      Line/Polyline/Polyline2d ("кандидаты" на "эскиз формы"). Остальные объекты
        //      (текст, атрибуты, штриховка, размерные линии и т.п.) в кандидаты не идут,
        //      только считаются для общей картины.
        //   4. Если кандидат РОВНО ОДИН - вызывает RebarRecognizer.Recognize() прямо на
        //      нём (Recognize() открывает свою транзакцию через entityId.Database, поэтому
        //      прекрасно работает и на "чужой", не активной Database, не только на
        //      объектах текущего документа) и печатает, какой Type/A-F/R получился.
        //   5. Если кандидатов 0 или больше 1 - печатает это отдельно: значит, форма в
        //      этом шаблоне ЛИБО не нарисована одной линией/полилинией (несколько
        //      несоединённых отрезков, или её вообще нет как отдельного объекта), и
        //      Recognize() до неё в принципе не достучится без склейки сначала.
        //
        // ВАЖНО: команда НЕ пытается сама судить "правильный это Type или нет" - только
        // печатает голые факты (что нашла, что вернул Recognize). Понять, для какого PZ_XX
        // какой Type ДОЛЖЕН получиться - следующий шаг мозгового штурма, не эта команда.
        [CommandMethod("PZSHAPECHECK")]
        public static void CheckStandardShapeRecognitionCommand()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            if (doc == null) return;
            Editor ed = doc.Editor;

            int candidateOne = 0;
            int candidateZero = 0;
            int candidateMany = 0;
            int missingTemplate = 0;
            int recognizedSomeType = 0;
            int notRecognized99 = 0;

            for (int i = 0; i <= 95; i++)
            {
                string code = i.ToString("D2");
                string resourceName = $"PoseEdit2026.Resources.Standard.PZ_{code}.dwg";
                string tempFile = Path.Combine(Path.GetTempPath(), "PZSHAPECHECK_" + code + ".dwg");

                try
                {
                    LegacyCommands.ExtractEmbeddedResource(resourceName, tempFile);
                }
                catch (System.Exception)
                {
                    ed.WriteMessage($"\nPZ_{code}: template not found, skipped.");
                    missingTemplate++;
                    continue;
                }

                Database sourceDb = new Database(false, true);
                try
                {
                    sourceDb.ReadDwgFile(tempFile, FileOpenMode.OpenForReadAndAllShare, true, null);

                    using (Transaction tr = sourceDb.TransactionManager.StartTransaction())
                    {
                        BlockTable bt = (BlockTable)tr.GetObject(sourceDb.BlockTableId, OpenMode.ForRead);
                        BlockTableRecord ms = (BlockTableRecord)tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForRead);

                        var candidates = new List<ObjectId>();
                        int otherEntities = 0;
                        foreach (ObjectId id in ms)
                        {
                            Entity ent = tr.GetObject(id, OpenMode.ForRead) as Entity;
                            if (ent is Line || ent is Polyline || ent is Polyline2d)
                                candidates.Add(id);
                            else
                                otherEntities++;
                        }

                        if (candidates.Count == 0)
                        {
                            candidateZero++;
                            ed.WriteMessage($"\nPZ_{code}: 0 candidates ({otherEntities} other objects) - not testable as-is.");
                        }
                        else if (candidates.Count > 1)
                        {
                            candidateMany++;
                            ed.WriteMessage($"\nPZ_{code}: {candidates.Count} candidates (ambiguous - not joined into one line/polyline).");
                        }
                        else
                        {
                            candidateOne++;
                            RebarResult result = RebarRecognizer.Recognize(candidates[0]);
                            if (result.Type == "99")
                            {
                                notRecognized99++;
                                ed.WriteMessage($"\nPZ_{code}: NOT RECOGNIZED (99).");
                            }
                            else
                            {
                                recognizedSomeType++;
                                ed.WriteMessage($"\nPZ_{code}: Type={result.Type} A={result.A} B={result.B} C={result.C} D={result.D} E={result.E} F={result.F} R={result.R}");
                            }
                        }

                        tr.Commit();
                    }
                }
                catch (System.Exception ex)
                {
                    ed.WriteMessage($"\nPZ_{code}: error reading template - {ex.Message}");
                    missingTemplate++;
                }
                finally
                {
                    sourceDb.Dispose();
                    try { File.Delete(tempFile); } catch (System.Exception) { /* временный файл, не критично */ }
                }
            }

            ed.WriteMessage("\n\n=== PZSHAPECHECK summary ===");
            ed.WriteMessage($"\nExactly 1 candidate found: {candidateOne} (of which recognized: {recognizedSomeType}, NOT recognized/99: {notRecognized99})");
            ed.WriteMessage($"\n0 candidates (not testable): {candidateZero}");
            ed.WriteMessage($"\nMultiple candidates (ambiguous): {candidateMany}");
            ed.WriteMessage($"\nTemplate missing/unreadable: {missingTemplate}");
        }

        // ====================================================================================
        // КОМАНДА: PZSHAPEDETAIL — диагностика: дамп ВСЕХ объектов Model Space одного
        // конкретного PZ_XX.dwg (слой, тип, координаты) - продолжение PZSHAPECHECK
        // ====================================================================================
        // ВРЕМЕННАЯ диагностическая команда (2026-08-21), тот же мозговой штурм что и у
        // PZSHAPECHECK. PZSHAPECHECK показал: у 92 из 96 шаблонов НЕСКОЛЬКО отдельных
        // Line/Polyline объектов (не одна цельная полилиния) - от 2 до 68 штук. Нужно
        // понять, можно ли среди них по СЛОЮ отличить именно контур формы арматуры от
        // размерных линий/выносок/штриховки - для этого печатаем слой+тип+координаты
        // КАЖДОГО объекта одного шаблона (не только кандидатов, всех, включая текст,
        // атрибуты и т.п.), чтобы увидеть реальную структуру.
        [CommandMethod("PZSHAPEDETAIL")]
        public static void ShowStandardShapeDetailCommand()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            if (doc == null) return;
            Editor ed = doc.Editor;

            PromptStringOptions codeOpts = new PromptStringOptions("\nPZ_XX code (naprimer 21): ") { AllowSpaces = false };
            PromptResult codeRes = ed.GetString(codeOpts);
            if (codeRes.Status != PromptStatus.OK) return;

            string code = codeRes.StringResult.Trim();
            if (int.TryParse(code, out int codeNum))
                code = codeNum.ToString("D2");

            string resourceName = $"PoseEdit2026.Resources.Standard.PZ_{code}.dwg";
            string tempFile = Path.Combine(Path.GetTempPath(), "PZSHAPEDETAIL_" + code + ".dwg");

            try
            {
                LegacyCommands.ExtractEmbeddedResource(resourceName, tempFile);
            }
            catch (System.Exception ex)
            {
                ed.WriteMessage($"\nPZ_{code}: template not found - {ex.Message}");
                return;
            }

            Database sourceDb = new Database(false, true);
            try
            {
                sourceDb.ReadDwgFile(tempFile, FileOpenMode.OpenForReadAndAllShare, true, null);

                using (Transaction tr = sourceDb.TransactionManager.StartTransaction())
                {
                    BlockTable bt = (BlockTable)tr.GetObject(sourceDb.BlockTableId, OpenMode.ForRead);
                    BlockTableRecord ms = (BlockTableRecord)tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForRead);

                    ed.WriteMessage($"\n=== PZ_{code} Model Space contents ===");
                    foreach (ObjectId id in ms)
                    {
                        Entity ent = tr.GetObject(id, OpenMode.ForRead) as Entity;
                        if (ent == null) continue;

                        string kind = ent.GetType().Name;
                        string layer = ent.Layer;

                        if (ent is Line line)
                        {
                            ed.WriteMessage($"\n  Line   layer='{layer}'  ({line.StartPoint.X:F1},{line.StartPoint.Y:F1}) -> ({line.EndPoint.X:F1},{line.EndPoint.Y:F1})");
                        }
                        else if (ent is Polyline poly)
                        {
                            ed.WriteMessage($"\n  Polyline layer='{layer}'  {poly.NumberOfVertices} verts, closed={poly.Closed}");
                        }
                        else if (ent is Polyline2d)
                        {
                            ed.WriteMessage($"\n  Polyline2d layer='{layer}'");
                        }
                        else
                        {
                            ed.WriteMessage($"\n  {kind,-10} layer='{layer}'");
                        }
                    }

                    tr.Commit();
                }
            }
            catch (System.Exception ex)
            {
                ed.WriteMessage($"\nPZ_{code}: error reading template - {ex.Message}");
            }
            finally
            {
                sourceDb.Dispose();
                try { File.Delete(tempFile); } catch (System.Exception) { /* временный файл, не критично */ }
            }
        }

        // ====================================================================================
        // КОМАНДА: PLDETCHECK — диагностика: прогнать Recognize() по ВСЕМ полилиниям в
        // текущем чертеже, проверив у каждой наличие дуговых сегментов (bulge)
        // ====================================================================================
        // ВРЕМЕННАЯ диагностическая команда (2026-08-21), продолжение мозгового штурма по
        // Determination. Пользователь склеил (JOIN) все отдельные примитивы из PZ-эскизов
        // в цельные полилинии в одном тестовом чертеже - но там, где раньше был скругленный
        // угол (фаска/радиус), JOIN превращает его не в острую вершину, а в ДУГОВОЙ сегмент
        // (bulge) между двумя новыми точками. RebarRecognizer.GetPointsFromEntity берёт
        // только координаты вершин (poly.GetPoint3dAt) и НИКАК не учитывает bulge - то есть
        // для таких скруглённых углов количество точек и все углы/длины между ними будут
        // посчитаны неверно. Эта команда проверяет гипотезу на реальных данных: для каждой
        // полилинии в текущем пространстве (Model Space) печатает - есть ли в ней хоть один
        // ненулевой bulge, сколько вершин, и что вернул Recognize() (Type или 99).
        //
        // НЕ ТРОГАЕТ чертёж - только читает (ForRead), ничего не меняет и не сохраняет.
        [CommandMethod("PLDETCHECK")]
        public static void CheckAllPolylinesRecognitionCommand()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            if (doc == null) return;
            Editor ed = doc.Editor;
            Database db = doc.Database;

            int total = 0;
            int withBulge = 0;
            int withoutBulge = 0;
            int recognizedWithBulge = 0;
            int recognizedWithoutBulge = 0;

            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                BlockTableRecord ms = (BlockTableRecord)tr.GetObject(db.CurrentSpaceId, OpenMode.ForRead);

                foreach (ObjectId id in ms)
                {
                    Entity ent = tr.GetObject(id, OpenMode.ForRead) as Entity;
                    if (!(ent is Polyline poly)) continue; // считаем только LWPOLYLINE - это то, чем реально рисуют в EEN

                    total++;

                    bool hasBulge = false;
                    for (int i = 0; i < poly.NumberOfVertices; i++)
                    {
                        // GetBulgeAt(i) - "изгиб" сегмента, начинающегося в вершине i.
                        // 0 = прямой отрезок, любое другое значение = дуговой сегмент.
                        if (Math.Abs(poly.GetBulgeAt(i)) > 0.0001)
                        {
                            hasBulge = true;
                            break;
                        }
                    }

                    RebarResult result = RebarRecognizer.Recognize(id);
                    bool recognized = result.Type != "99";

                    if (hasBulge)
                    {
                        withBulge++;
                        if (recognized) recognizedWithBulge++;
                    }
                    else
                    {
                        withoutBulge++;
                        if (recognized) recognizedWithoutBulge++;
                    }

                    Point3d firstPt = poly.NumberOfVertices > 0 ? poly.GetPoint3dAt(0) : Point3d.Origin;
                    ed.WriteMessage($"\nHandle={ent.Handle}  at=({firstPt.X:F0},{firstPt.Y:F0})  verts={poly.NumberOfVertices}  bulge={(hasBulge ? "YES" : "no")}  -> {(recognized ? $"Type={result.Type}" : "NOT RECOGNIZED (99)")}");

                    // Для НЕ распознанных без дуги (без дуги - значит вершины честные, не
                    // артефакт скругления) сразу печатаем ВСЕ координаты вершин - чтобы можно
                    // было трассировать RebarRecognizer вручную по реальным цифрам, не гоняя
                    // туда-сюда за каждым отдельным случаем.
                    if (!recognized && !hasBulge)
                    {
                        for (int i = 0; i < poly.NumberOfVertices; i++)
                        {
                            Point3d v = poly.GetPoint3dAt(i);
                            ed.WriteMessage($"\n    P{i + 1}=({v.X:F1},{v.Y:F1})");
                        }
                    }
                }

                tr.Commit();
            }

            ed.WriteMessage("\n\n=== PLDETCHECK summary ===");
            ed.WriteMessage($"\nTotal polylines checked: {total}");
            ed.WriteMessage($"\nWith bulge (arc segments): {withBulge}  - recognized: {recognizedWithBulge} ({(withBulge > 0 ? 100.0 * recognizedWithBulge / withBulge : 0):F0}%)");
            ed.WriteMessage($"\nWithout bulge (all straight): {withoutBulge}  - recognized: {recognizedWithoutBulge} ({(withoutBulge > 0 ? 100.0 * recognizedWithoutBulge / withoutBulge : 0):F0}%)");
        }

        // ====================================================================================
        // КОМАНДА: ZOOMHANDLE — приблизить вид и подсветить объект по его Handle
        // ====================================================================================
        // ВРЕМЕННАЯ диагностическая команда (2026-08-21), продолжение мозгового штурма по
        // Determination - PLDETCHECK печатает Handle каждого проблемного объекта (например
        // "3A3AD"), но без этой команды пользователю пришлось бы искать полилинию по
        // координатам вручную. Тут - вводишь Handle как есть (без "0x", просто hex-строка),
        // команда находит объект, приближает вид к нему и выделяет (implied selection),
        // чтобы сразу было видно, какая это форма.
        [CommandMethod("ZOOMHANDLE")]
        public static void ZoomToHandleCommand()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            if (doc == null) return;
            Editor ed = doc.Editor;
            Database db = doc.Database;

            PromptStringOptions opts = new PromptStringOptions("\nHandle (naprimer 3A3AD): ") { AllowSpaces = false };
            PromptResult res = ed.GetString(opts);
            if (res.Status != PromptStatus.OK) return;

            string handleText = res.StringResult.Trim();
            long handleValue;
            try
            {
                // Convert.ToInt64(text, 16) - переводит шестнадцатеричную строку (как в
                // AutoCAD-Handle'ах) в обычное число, без нужды возиться с NumberStyles.
                handleValue = Convert.ToInt64(handleText, 16);
            }
            catch (System.Exception)
            {
                ed.WriteMessage($"\nInvalid handle format: '{handleText}'.");
                return;
            }

            ObjectId id;
            try
            {
                id = db.GetObjectId(false, new Handle(handleValue), 0);
            }
            catch (System.Exception ex)
            {
                ed.WriteMessage($"\nHandle '{handleText}' not found in this drawing: {ex.Message}");
                return;
            }

            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                Entity ent = tr.GetObject(id, OpenMode.ForRead) as Entity;
                if (ent == null)
                {
                    ed.WriteMessage($"\nHandle '{handleText}' is not a drawable entity.");
                    tr.Commit();
                    return;
                }

                Extents3d ext = ent.GeometricExtents;

                // Небольшой отступ вокруг объекта (10% от размера), чтобы он не упирался
                // ровно в края экрана - иначе на маленьких формах совсем не видно контекста.
                double dx = (ext.MaxPoint.X - ext.MinPoint.X) * 0.1 + 1.0;
                double dy = (ext.MaxPoint.Y - ext.MinPoint.Y) * 0.1 + 1.0;
                Extents3d padded = new Extents3d(
                    new Point3d(ext.MinPoint.X - dx, ext.MinPoint.Y - dy, 0),
                    new Point3d(ext.MaxPoint.X + dx, ext.MaxPoint.Y + dy, 0));

                // Editor.Zoom(Extents3d) в этой версии API отсутствует - зумим тем же
                // способом, что и обычный пользователь: команда ZOOM с опцией "Window".
                ed.Command("_.ZOOM", "_W", padded.MinPoint, padded.MaxPoint);
                ed.SetImpliedSelection(new ObjectId[] { id });

                tr.Commit();
            }

            ed.WriteMessage($"\nZoomed to and selected handle '{handleText}'.");
        }

        // ====================================================================================
        // КОМАНДА: PZSTDFIX — батч-правка слоёв/текстового стиля во всех PZ_XX.dwg
        // ====================================================================================
        // НАЗНАЧЕНИЕ: пользователь переименовал слои с префиксом "ren." на "posedit." по
        // всему C#-коду (RebarRecognizer.cs/LegacyCommands.cs/QuantityTableGenerator.cs), но
        // это не трогает сами ЭТАЛОННЫЕ DWG-файлы в Resources/Standard/PZ_01.dwg..PZ_93.dwg -
        // они хранят СВОИ СОБСТВЕННЫЕ таблицы слоёв/текстовых стилей внутри бинарного DWG,
        // отдельно от того, что делает C#-код при вставке. Эта команда правит все 93 файла
        // "не открывая" их в редакторе AutoCAD - через side-database (Database(false,true) +
        // ReadDwgFile), тот же приём, что и у PZREDEFN/PZSHAPECHECK выше, только тут мы не
        // просто ЧИТАЕМ шаблон, а ПРАВИМ и СОХРАНЯЕМ обратно на диск.
        //
        // ЧТО ПРАВИТ в каждом файле:
        //   1. Слои: ren.mtr.bar/hidden/lenght/tb/text -> posedit.mtr.bar/hidden/lenght/tb/text
        //      (переименование через LayerTableRecord.Name - опечатка "lenght" вместо
        //      "length" сохранена намеренно, раз она уже есть в реальных слоях - меняем
        //      только префикс, не исправляем чужую опечатку).
        //   2. Текстовый стиль "ren Gost.common" -> "posedit.ISOCPEUR": имя, шрифт (GOST
        //      Common -> ISOCPEUR), ширину (0.7 -> 0.9), уклон (10° -> 0°).
        //
        // ВАЖНО (исправлено 2026-08-22 - первая версия ошибочно ставила SHX-файл): "ISOCPEUR"
        // - это Windows TrueType-шрифт (см. "ISOCPEUR (windows fonts)" от пользователя), а
        // НЕ файл фигур AutoCAD isocpeur.shx - хотя стандартный AutoCAD и правда поставляется
        // с одноимённым .shx, тут нужен именно установленный в Windows TTF. Для TrueType-
        // шрифта в API это задаётся через TextStyleTableRecord.Font (свойство типа
        // FontDescriptor из Autodesk.AutoCAD.GraphicsInterface), а не через FileName (FileName
        // - только для .shx/.shp) - FileName для такого стиля обнуляется. Точный API проверен
        // рефлексией по реальной сборке AcDbMgd.dll (метода "SetFont" в этой версии нет).
        //
        // ВАЖНО: имя текстового стиля ищется НЕ строго по символам, а по "нормализованному"
        // сравнению (без точек/пробелов/подчёркиваний, без учёта регистра) и проверяется
        // ОБА варианта - старое имя "ren Gost.common" И уже переименованное "posedit.ISOCPEUR" -
        // так команду можно безопасно перезапускать повторно (например, чтобы досчитать
        // шрифт после того, как имя уже переименовано прошлым запуском), а не только один раз.
        // Слои и стиль, которые НЕ нашлись вообще ни разу за весь проход, отдельно
        // перечисляются в конце - сигнал, что имя реально другое и нужно уточнить у
        // пользователя, а не молча гадать дальше.
        //
        // БЕЗОПАСНОСТЬ: каждый файл сохраняется через временный "*.dwg.tmp" рядом, и только
        // после успешного SaveAs + Dispose оригинал заменяется (File.Delete + File.Move) -
        // если что-то упадёт посреди записи, оригинальный файл не пострадает. Ошибка на
        // одном файле не останавливает обработку остальных (try/catch на файл, не на всю
        // команду) - в конце печатается сводка.
        [CommandMethod("PZSTDFIX")]
        public static void FixStandardTemplateLayersAndStyleCommand()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            if (doc == null) return;
            Editor ed = doc.Editor;

            PromptStringOptions folderOpts = new PromptStringOptions(
                "\nFolder with PZ_XX.dwg templates: ")
            {
                DefaultValue = @"C:\Users\durum\Documents\GitHub\PoseEdit2026\Resources\Standard",
                UseDefaultValue = true,
                AllowSpaces = true
            };
            PromptResult folderRes = ed.GetString(folderOpts);
            if (folderRes.Status != PromptStatus.OK) return;
            string folder = folderRes.StringResult;

            if (!Directory.Exists(folder))
            {
                ed.WriteMessage($"\nFolder not found: {folder}");
                return;
            }

            // Старое имя -> новое имя. "lenght" - существующая опечатка в реальных слоях,
            // сохранена намеренно (меняем только префикс "ren." -> "posedit.").
            Dictionary<string, string> layerRenames = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["ren.mtr.bar"] = "posedit.mtr.bar",
                ["ren.mtr.hidden"] = "posedit.mtr.hidden",
                ["ren.mtr.lenght"] = "posedit.mtr.lenght",
                ["ren.mtr.tb"] = "posedit.mtr.tb",
                ["ren.mtr.text"] = "posedit.mtr.text",
            };

            const string oldStyleName = "ren Gost.common";
            const string newStyleName = "posedit.ISOCPEUR";
            // "ISOCPEUR" здесь - Windows TrueType-шрифт (устанавливается в системе), НЕ файл
            // фигур AutoCAD isocpeur.shx - см. TextStyleTableRecord.SetFont ниже.
            const string newStyleTypeface = "ISOCPEUR";
            const double newWidthFactor = 0.9;
            const double newObliqueDeg = 0.0;

            string[] files = Directory.GetFiles(folder, "PZ_*.dwg");
            Array.Sort(files, StringComparer.OrdinalIgnoreCase);

            int filesOk = 0, filesFailed = 0, totalLayerRenames = 0, filesWithStyleFixed = 0, entitiesFixed = 0;
            Dictionary<string, int> layerFoundCounts = layerRenames.Keys.ToDictionary(k => k, k => 0);

            foreach (string path in files)
            {
                string fileName = Path.GetFileName(path);
                string tempPath = path + ".tmp";
                try
                {
                    using (Database db = new Database(false, true))
                    {
                        db.ReadDwgFile(path, FileOpenMode.OpenForReadAndAllShare, true, null);

                        using (Transaction tr = db.TransactionManager.StartTransaction())
                        {
                            // --- Слои ---
                            LayerTable lt = (LayerTable)tr.GetObject(db.LayerTableId, OpenMode.ForRead);
                            foreach (KeyValuePair<string, string> rename in layerRenames)
                            {
                                if (!lt.Has(rename.Key)) continue;
                                layerFoundCounts[rename.Key]++;

                                if (lt.Has(rename.Value)) continue; // целевое имя уже занято - пропускаем, не переименовываем

                                LayerTableRecord ltr = (LayerTableRecord)tr.GetObject(lt[rename.Key], OpenMode.ForWrite);
                                ltr.Name = rename.Value;
                                totalLayerRenames++;
                            }

                            // --- Текстовый стиль (нормализованный поиск по имени) ---
                            // Проверяем ОБА варианта имени - старое "ren Gost.common" и уже
                            // переименованное "posedit.ISOCPEUR" - чтобы повторный запуск (после
                            // того как первый раз переименовал, но поставил неверный шрифт)
                            // тоже находил и доправлял стиль, а не считал его "не найденным".
                            TextStyleTable stt = (TextStyleTable)tr.GetObject(db.TextStyleTableId, OpenMode.ForRead);
                            ObjectId styleId = ObjectId.Null;
                            string normOld = NormalizeStyleName(oldStyleName);
                            string normNew = NormalizeStyleName(newStyleName);
                            foreach (ObjectId id in stt)
                            {
                                TextStyleTableRecord candidate = (TextStyleTableRecord)tr.GetObject(id, OpenMode.ForRead);
                                string norm = NormalizeStyleName(candidate.Name);
                                if (norm == normOld || norm == normNew)
                                {
                                    styleId = id;
                                    break;
                                }
                            }
                            if (!styleId.IsNull)
                            {
                                TextStyleTableRecord sttr = (TextStyleTableRecord)tr.GetObject(styleId, OpenMode.ForWrite);
                                if (!stt.Has(newStyleName)) sttr.Name = newStyleName;
                                // TrueType (Windows) шрифт - НЕ файл фигур .shx/.shp, поэтому
                                // FileName обнуляется, а typeface задаётся через свойство Font
                                // (FontDescriptor) - в этой версии API нет метода SetFont,
                                // только settable-свойство (проверено рефлексией на AcDbMgd.dll).
                                sttr.FileName = "";
                                sttr.Font = new Autodesk.AutoCAD.GraphicsInterface.FontDescriptor(
                                    newStyleTypeface, false, false, 0, 0);
                                sttr.XScale = newWidthFactor;
                                sttr.ObliquingAngle = newObliqueDeg * Math.PI / 180.0;
                                filesWithStyleFixed++;

                                // ВАЖНО (обнаружено пользователем 2026-08-22 - "визуально не
                                // поменялся, в свойствах 0.6"): смена XScale/ObliquingAngle у
                                // самого TextStyleTableRecord НЕ откатывается на уже созданные
                                // AttributeDefinition/DBText, которые на него ссылаются - у
                                // каждого такого объекта своё СОБСТВЕННОЕ значение WidthFactor/
                                // Oblique, скопированное со стиля один раз в момент создания, а
                                // не живая ссылка на стиль. Нужно пройтись по всем блокам файла
                                // и поправить каждый найденный объект с этим TextStyleId отдельно.
                                BlockTable bt2 = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);
                                foreach (ObjectId btrId in bt2)
                                {
                                    BlockTableRecord btr2 = (BlockTableRecord)tr.GetObject(btrId, OpenMode.ForRead);
                                    foreach (ObjectId entId in btr2)
                                    {
                                        DBObject obj = tr.GetObject(entId, OpenMode.ForRead);
                                        if (obj is AttributeDefinition attDef && attDef.TextStyleId == styleId)
                                        {
                                            attDef.UpgradeOpen();
                                            attDef.WidthFactor = newWidthFactor;
                                            attDef.Oblique = newObliqueDeg * Math.PI / 180.0;
                                            entitiesFixed++;
                                        }
                                        else if (obj is DBText dbText && dbText.TextStyleId == styleId)
                                        {
                                            dbText.UpgradeOpen();
                                            dbText.WidthFactor = newWidthFactor;
                                            dbText.Oblique = newObliqueDeg * Math.PI / 180.0;
                                            entitiesFixed++;
                                        }
                                    }
                                }
                            }

                            tr.Commit();
                        }

                        db.SaveAs(tempPath, DwgVersion.Current);
                    }

                    File.Delete(path);
                    File.Move(tempPath, path);
                    filesOk++;
                }
                catch (System.Exception ex)
                {
                    filesFailed++;
                    ed.WriteMessage($"\n{fileName}: ERROR {ex.Message}");
                    if (File.Exists(tempPath)) { try { File.Delete(tempPath); } catch { } }
                }
            }

            ed.WriteMessage($"\nPZSTDFIX: {files.Length} files found, {filesOk} saved OK, {filesFailed} failed, " +
                             $"{totalLayerRenames} layer renames total, {filesWithStyleFixed} files had the text style fixed, " +
                             $"{entitiesFixed} attribute/text objects had WidthFactor/Oblique fixed.");

            foreach (KeyValuePair<string, int> kvp in layerFoundCounts)
            {
                if (kvp.Value == 0)
                    ed.WriteMessage($"\nLayer '{kvp.Key}' was never found in any file - check the exact name.");
            }
            if (filesWithStyleFixed == 0)
                ed.WriteMessage($"\nText style '{oldStyleName}' was never found in any file - check the exact name.");
        }

        // Сравнение имён текстовых стилей без учёта точек/пробелов/подчёркиваний/регистра -
        // так реальное имя в DWG может слегка отличаться по пунктуации от того, что
        // продиктовал пользователь, и всё равно найдётся.
        private static string NormalizeStyleName(string name)
        {
            return (name ?? "").Replace(".", "").Replace(" ", "").Replace("_", "").ToLowerInvariant();
        }
    }
}
