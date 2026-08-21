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
    }
}
