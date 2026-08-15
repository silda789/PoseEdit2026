// ====================================================================================
// ФАЙЛ: RoutineCommands.cs
// НАЗНАЧЕНИЕ: Мелкие вспомогательные команды для рутинных операций в AutoCAD, которые
//             не связаны напрямую с редактированием блоков RL-POS (в отличие от
//             Commands.cs / LegacyCommands.cs). Новые "быстрые" команды добавляются сюда.
// ====================================================================================
#nullable disable
using System;
using System.Collections.Generic;
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
                "\nNa skolko sdvinut nomera listov (naprimer 11, ili -5): ")
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
                    ed.WriteMessage("\nListov s chislom v imeni ne naideno.");
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
                            ed.WriteMessage($"\nPropushen list '{it.OldName}': novyi nomer {newNumber} <= 0.");
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
                            ed.WriteMessage($"\nNe udalos pereimenovat '{it.OldName}' -> '{newName}': {ex.Message}");
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

                ed.WriteMessage($"\nGotovo. Pereimenovano listov: {renamed}, propushcheno: {skipped}.");
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
        // 1. Собираем все листы, кроме "Model", у которых имя содержит число, сортируем по
        //    порядку вкладок (как они идут внизу экрана в AutoCAD слева направо).
        // 2. Спрашиваем начальное число.
        // 3. Присваиваем номера по порядку: 1-й лист = начальное число, 2-й = +1, и т.д.
        //    Префикс/суффикс имени (если был, например "Лист 55" -> префикс "Лист ") сохраняется
        //    у каждого листа свой, без ведущих нулей (просто "55", "56", ...).
        //
        // ВАЖНАЯ ДЕТАЛЬ: при сплошной перенумерации (не сдвиге) новый номер листа очень часто
        // совпадает со СТАРЫМ номером какого-то другого листа где-то в середине списка - здесь
        // нет гарантии порядка, как в LAYSHIFT. Поэтому переименование идёт в ДВА ПРОХОДА:
        // сначала все листы получают временные уникальные имена (гарантированно ни с чем не
        // совпадающие), потом только со второго прохода - настоящие итоговые номера.
        [CommandMethod("LAYRENUM")]
        public static void RenumberLayoutsSequentiallyCommand()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            if (doc == null) return;
            Editor ed = doc.Editor;
            Database db = doc.Database;

            PromptIntegerOptions startOpts = new PromptIntegerOptions(
                "\nS kakogo nomera nachat perenumeratsiyu listov: ")
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
                var items = new List<(string OldName, string Prefix, string Suffix, int TabOrder)>();

                using (Transaction tr = db.TransactionManager.StartTransaction())
                {
                    DBDictionary layoutDict = (DBDictionary)tr.GetObject(db.LayoutDictionaryId, OpenMode.ForRead);
                    foreach (DBDictionaryEntry entry in layoutDict)
                    {
                        Layout layout = tr.GetObject(entry.Value, OpenMode.ForRead) as Layout;
                        if (layout == null || layout.ModelType) continue; // пропускаем "Model"

                        Match m = NumberedLayoutPattern.Match(entry.Key);
                        if (!m.Success) continue; // в имени нет числа - пропускаем

                        items.Add((entry.Key, m.Groups[1].Value, m.Groups[3].Value, layout.TabOrder));
                    }
                    tr.Commit();
                }

                if (items.Count == 0)
                {
                    ed.WriteMessage("\nListov s chislom v imeni ne naideno.");
                    return;
                }

                // Порядок как в самом AutoCAD (порядок вкладок), а не по старому номеру -
                // именно так пользователь листает листы по очереди.
                var ordered = items.OrderBy(i => i.TabOrder).ToList();

                var finalNames = new Dictionary<string, string>();
                for (int idx = 0; idx < ordered.Count; idx++)
                {
                    var it = ordered[idx];
                    int newNumber = start + idx;
                    finalNames[it.OldName] = $"{it.Prefix}{newNumber}{it.Suffix}";
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
                            ed.WriteMessage($"\nNe udalos nachat pereimenovanie '{it.OldName}': {ex.Message}");
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
                            ed.WriteMessage($"\nList '{it.OldName}' ostalsya s vremennym imenem '{tempName}' - pereimenovat v '{finalName}' ne udalos: {ex.Message}");
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

                ed.WriteMessage($"\nGotovo. Pereimenovano listov: {renamed}, propushcheno: {skipped}, nachinaya s {start}.");
            }
        }
    }
}
