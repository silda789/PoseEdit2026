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
    }
}
