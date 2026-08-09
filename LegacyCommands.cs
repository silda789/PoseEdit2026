// ====================================================================================
// ФАЙЛ: LegacyCommands.cs
// НАЗНАЧЕНИЕ: Портированные команды массового редактирования блоков RL-POS,
//             переведённые с AutoLISP (Temp/Command/QUANTITY2.LSP и др.) на C#.
// ====================================================================================
// ВАЖНО ПРО ИМЕНА КОМАНД: старый AutoLISP (POSEDIT.LSP и файлы в Temp/Command/) может
// быть загружен в той же сессии AutoCAD, что и этот плагин. Чтобы не было конфликта
// имён команд, каждая новая C#-команда получает суффикс "N" (по аналогии с тем, как
// старая LISP-команда "ee" в этом проекте была заменена на "EEN"):
//
//   Старая LISP-команда   ->  Новая C#-команда
//   adet                  ->  ADETN
//   adet2                 ->  ADET2N
//   cap                   ->  CAPN
//   aralik                ->  ARALIKN
//   grup                  ->  GRUPN
//
// Остальные команды из этого файла (degis, tddk, tddu, tdd1/2/3, pzg, 77, 77b,
// pzredef, diez, ppp, ppp2, pozver, tddh) портируются по этой же схеме в следующих
// коммитах этой же серии.
// ====================================================================================

#nullable disable
using System;
using System.Collections.Generic;
using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;

namespace PoseEdit2026
{
    public static class LegacyCommands
    {
        // Общий фильтр выбора: блоки RL-POS (INSERT с именем "RL-POS")
        private static SelectionFilter RlPosFilter()
        {
            TypedValue[] filterList =
            [
                new TypedValue((int)DxfCode.Start, "INSERT"),
                new TypedValue((int)DxfCode.BlockName, "RL-POS")
            ];
            return new SelectionFilter(filterList);
        }

        // ====================================================================================
        // КОМАНДА: ADETN (было "adet" в LISP)
        // ====================================================================================
        // НАЗНАЧЕНИЕ: Меняет количество стержней (adet) в TB у выбранных блоков RL-POS,
        // сохраняя множитель, диаметр и шаг без изменений.
        // ПЕРЕВЕДЕНО ИЗ: (defun c:adet (/) ...) — QUANTITY2.LSP, строки ~445-463
        [CommandMethod("ADETN")]
        public static void ChangeAdetCommand()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            if (doc == null) return;
            Editor ed = doc.Editor;

            PromptSelectionOptions selOpts = new PromptSelectionOptions
            {
                MessageForAdding = "\nAdeti degisecek pozlari seciniz: "
            };
            PromptSelectionResult selRes = ed.GetSelection(selOpts, RlPosFilter());
            if (selRes.Status != PromptStatus.OK) return;

            PromptIntegerOptions intOpts = new PromptIntegerOptions("\nDonati adeti: ")
            {
                AllowNegative = false,
                AllowZero = false
            };
            PromptIntegerResult intRes = ed.GetInteger(intOpts);
            if (intRes.Status != PromptStatus.OK) return;
            string adet = intRes.Value.ToString();

            foreach (ObjectId id in selRes.Value.GetObjectIds())
            {
                var attrs = BlockHelper.GetAttributes(id);
                string tb = attrs.TryGetValue("TB", out string v) ? v : "";
                string adetCarp = PozHelper.GetAdetCarpi(tb);
                string cap = PozHelper.GetCap(tb);
                string aralik = PozHelper.GetAralik(tb);
                string adetCarpPart = adetCarp.Length > 0 ? adetCarp + "x" : "";
                string aralikPart = aralik.Length > 0 ? "/" + aralik : "";
                string newTb = adetCarpPart + adet + PozHelper.Fi + cap + aralikPart;

                BlockHelper.SetAttributes(id, new Dictionary<string, string> { ["TB"] = newTb });
                PozHelper.RepositionShapeText(id);
            }

            ed.WriteMessage($"\n{selRes.Value.Count} poz guncellendi.");
        }

        // ====================================================================================
        // КОМАНДА: ADET2N (было "adet2" в LISP)
        // ====================================================================================
        // НАЗНАЧЕНИЕ: Меняет множитель количества (например "3x" перед количеством) у
        // выбранных блоков RL-POS, сохраняя остальную часть TB (после первого "x") без изменений.
        // ПЕРЕВЕДЕНО ИЗ: (defun c:adet2 (/) ...) — QUANTITY2.LSP, строки ~465-483
        [CommandMethod("ADET2N")]
        public static void ChangeAdetCarpiCommand()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            if (doc == null) return;
            Editor ed = doc.Editor;

            PromptSelectionOptions selOpts = new PromptSelectionOptions
            {
                MessageForAdding = "\nAdeti carpani degisecek pozlari seciniz: "
            };
            PromptSelectionResult selRes = ed.GetSelection(selOpts, RlPosFilter());
            if (selRes.Status != PromptStatus.OK) return;

            PromptIntegerOptions intOpts = new PromptIntegerOptions("\nDonati adeti carpani: ")
            {
                AllowNegative = false
            };
            PromptIntegerResult intRes = ed.GetInteger(intOpts);
            if (intRes.Status != PromptStatus.OK) return;

            string adetCarpPrefix = intRes.Value > 1 ? intRes.Value + "x" : "";

            foreach (ObjectId id in selRes.Value.GetObjectIds())
            {
                var attrs = BlockHelper.GetAttributes(id);
                string tb = attrs.TryGetValue("TB", out string v) ? v : "";

                // Отбрасываем всё до первого "x" включительно (старый множитель), как в LISP
                int xPos = tb.ToUpperInvariant().IndexOf('X');
                string tbSag = xPos >= 0 ? tb.Substring(xPos + 1) : tb;
                string newTb = adetCarpPrefix + tbSag;

                BlockHelper.SetAttributes(id, new Dictionary<string, string> { ["TB"] = newTb });
                PozHelper.RepositionShapeText(id);
            }

            ed.WriteMessage($"\n{selRes.Value.Count} poz guncellendi.");
        }

        // ====================================================================================
        // КОМАНДА: CAPN (было "cap" в LISP)
        // ====================================================================================
        // НАЗНАЧЕНИЕ: Меняет диаметр стержня (cap) в TB, сохраняя количество/множитель/шаг.
        // Дополнительно выставляет TIK=1 (флаг "изменено вручную"), как в оригинале.
        // ПЕРЕВЕДЕНО ИЗ: (defun c:cap (/ adet) ...) — QUANTITY2.LSP, строки ~485-504
        [CommandMethod("CAPN")]
        public static void ChangeCapCommand()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            if (doc == null) return;
            Editor ed = doc.Editor;

            PromptSelectionOptions selOpts = new PromptSelectionOptions
            {
                MessageForAdding = "\nCapi degisecek pozlari seciniz: "
            };
            PromptSelectionResult selRes = ed.GetSelection(selOpts, RlPosFilter());
            if (selRes.Status != PromptStatus.OK) return;

            PromptIntegerOptions intOpts = new PromptIntegerOptions("\nDonati capi: ")
            {
                AllowNegative = false,
                AllowZero = false
            };
            PromptIntegerResult intRes = ed.GetInteger(intOpts);
            if (intRes.Status != PromptStatus.OK) return;
            string cap = intRes.Value.ToString();

            foreach (ObjectId id in selRes.Value.GetObjectIds())
            {
                var attrs = BlockHelper.GetAttributes(id);
                string tb = attrs.TryGetValue("TB", out string v) ? v : "";
                string adetCarp = PozHelper.GetAdetCarpi(tb);
                string adet = PozHelper.GetAdet(tb);
                string aralik = PozHelper.GetAralik(tb);
                string adetCarpPart = adetCarp.Length > 0 ? adetCarp + "x" : "";
                string aralikPart = aralik.Length > 0 ? "/" + aralik : "";
                string newTb = adetCarpPart + adet + PozHelper.Fi + cap + aralikPart;

                BlockHelper.SetAttributes(id, new Dictionary<string, string>
                {
                    ["TB"] = newTb,
                    ["TIK"] = "1"
                });
                PozHelper.RepositionShapeText(id);
            }

            ed.WriteMessage($"\n{selRes.Value.Count} poz guncellendi.");
        }

        // ====================================================================================
        // КОМАНДА: ARALIKN (было "aralik" в LISP)
        // ====================================================================================
        // НАЗНАЧЕНИЕ: Меняет шаг арматуры (aralik). Если у TB нет привязанного поля
        // (ACAD_FIELD) - автоматически пересчитывает количество стержней пропорционально
        // изменению шага (старое_кол-во * старый_шаг / новый_шаг), как в оригинале.
        // ПЕРЕВЕДЕНО ИЗ: (defun c:aralik (/ adet) ...) — QUANTITY2.LSP, строки ~545-572
        [CommandMethod("ARALIKN")]
        public static void ChangeAralikCommand()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            if (doc == null) return;
            Editor ed = doc.Editor;

            PromptSelectionOptions selOpts = new PromptSelectionOptions
            {
                MessageForAdding = "\nAraligi degisecek pozlari seciniz: "
            };
            PromptSelectionResult selRes = ed.GetSelection(selOpts, RlPosFilter());
            if (selRes.Status != PromptStatus.OK) return;

            PromptIntegerOptions intOpts = new PromptIntegerOptions("\nDonati araligi: ")
            {
                AllowNegative = false,
                AllowZero = false
            };
            PromptIntegerResult intRes = ed.GetInteger(intOpts);
            if (intRes.Status != PromptStatus.OK) return;
            int aralikYeni = intRes.Value;

            foreach (ObjectId id in selRes.Value.GetObjectIds())
            {
                var attrs = BlockHelper.GetAttributes(id);
                string tb = attrs.TryGetValue("TB", out string v) ? v : "";
                string adetCarp = PozHelper.GetAdetCarpi(tb);
                string adetEski = PozHelper.GetAdet(tb);
                string cap = PozHelper.GetCap(tb);
                string aralikEski = PozHelper.GetAralik(tb);

                bool hasField = AttributeHasField(id, "TB");
                string adetYeni;
                if (!hasField && int.TryParse(adetEski, out int adetEskiN) && int.TryParse(aralikEski, out int aralikEskiN) && aralikYeni != 0)
                {
                    int calc = (int)Math.Floor(0.5 + (double)(adetEskiN * aralikEskiN) / aralikYeni);
                    adetYeni = Math.Max(1, calc).ToString();
                }
                else
                {
                    adetYeni = adetEski;
                }

                string adetCarpPart = adetCarp.Length > 0 ? adetCarp + "x" : "";
                string newTb = adetCarpPart + adetYeni + PozHelper.Fi + cap + "/" + aralikYeni;

                BlockHelper.SetAttributes(id, new Dictionary<string, string>
                {
                    ["TB"] = newTb,
                    ["ARALIK"] = aralikYeni.ToString()
                });
                PozHelper.RepositionShapeText(id);
            }

            ed.WriteMessage($"\n{selRes.Value.Count} poz guncellendi.");
        }

        // ====================================================================================
        // КОМАНДА: GRUPN (было "grup" в LISP)
        // ====================================================================================
        // НАЗНАЧЕНИЕ: Меняет групповой множитель (GC) у выбранных блоков RL-POS.
        // ПЕРЕВЕДЕНО ИЗ: (defun c:grup (/) ...) — QUANTITY2.LSP, строки ~865-874
        [CommandMethod("GRUPN")]
        public static void ChangeGrupCommand()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            if (doc == null) return;
            Editor ed = doc.Editor;

            PromptSelectionOptions selOpts = new PromptSelectionOptions
            {
                MessageForAdding = "\nGrup carpani degisecek pozlari seciniz: "
            };
            PromptSelectionResult selRes = ed.GetSelection(selOpts, RlPosFilter());
            if (selRes.Status != PromptStatus.OK) return;

            PromptIntegerOptions intOpts = new PromptIntegerOptions("\nGrup carpani: ");
            PromptIntegerResult intRes = ed.GetInteger(intOpts);
            if (intRes.Status != PromptStatus.OK) return;
            string gcDeger = intRes.Value.ToString();

            foreach (ObjectId id in selRes.Value.GetObjectIds())
            {
                BlockHelper.SetAttributes(id, new Dictionary<string, string> { ["GC"] = gcDeger });
                PozHelper.RepositionShapeText(id);
            }

            ed.WriteMessage($"\n{selRes.Value.Count} poz guncellendi.");
        }

        // Аналог att_field_varmi: проверяет, привязано ли к атрибуту поле ACAD_FIELD
        private static bool AttributeHasField(ObjectId blockId, string tag)
        {
            if (blockId.IsNull) return false;
            using (Transaction tr = blockId.Database.TransactionManager.StartTransaction())
            {
                BlockReference blkRef = tr.GetObject(blockId, OpenMode.ForRead) as BlockReference;
                if (blkRef == null) { tr.Commit(); return false; }

                foreach (ObjectId attId in blkRef.AttributeCollection)
                {
                    AttributeReference attRef = tr.GetObject(attId, OpenMode.ForRead) as AttributeReference;
                    if (attRef == null || !string.Equals(attRef.Tag, tag, StringComparison.OrdinalIgnoreCase)) continue;

                    ObjectId extDictId = attRef.ExtensionDictionary;
                    if (extDictId.IsNull) { tr.Commit(); return false; }

                    DBDictionary extDict = tr.GetObject(extDictId, OpenMode.ForRead) as DBDictionary;
                    bool hasField = extDict != null && extDict.Contains("ACAD_FIELD");
                    tr.Commit();
                    return hasField;
                }
                tr.Commit();
            }
            return false;
        }
    }
}
