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

        // ====================================================================================
        // КОМАНДА: DEGISN (было "degis" в LISP)
        // ====================================================================================
        // НАЗНАЧЕНИЕ: Массовая замена одного значения на другое в выбранном поле
        // (POZ, ADET2, ADET, CAP, ARALIK, BOY, TIP, A, B, C, D, E, F, R, GC) у всех
        // выбранных блоков RL-POS, где текущее значение поля точно совпадает со старым.
        // ПЕРЕВЕДЕНО ИЗ: (defun c:degis (/) ...) — QUANTITY2.LSP, строки ~1035-1109
        [CommandMethod("DEGISN")]
        public static void ChangeAttributeMassCommand()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            if (doc == null) return;
            Editor ed = doc.Editor;

            PromptSelectionOptions selOpts = new PromptSelectionOptions
            {
                MessageForAdding = "\nDegisiklik uygulanacak pozlari seciniz: "
            };
            PromptSelectionResult selRes = ed.GetSelection(selOpts, RlPosFilter());
            if (selRes.Status != PromptStatus.OK) return;

            PromptStringOptions fieldOpts = new PromptStringOptions(
                "\nDegisecek bilgi (POZ, ADET2, ADET, CAP, ARALIK, BOY, TIP, A, B, C, D, E, F, R, GC): ")
            { AllowSpaces = false };
            PromptResult fieldRes = ed.GetString(fieldOpts);
            if (fieldRes.Status != PromptStatus.OK) return;
            string degisBilgi = (fieldRes.StringResult ?? "").ToUpperInvariant();
            if (degisBilgi.Length == 0) return;

            PromptStringOptions oldOpts = new PromptStringOptions("\nEski deger: ") { AllowSpaces = true };
            PromptResult oldRes = ed.GetString(oldOpts);
            if (oldRes.Status != PromptStatus.OK) return;
            string eskiDeger = oldRes.StringResult ?? "";

            PromptStringOptions newOpts = new PromptStringOptions("\nYeni deger: ") { AllowSpaces = true };
            PromptResult newRes = ed.GetString(newOpts);
            if (newRes.Status != PromptStatus.OK) return;
            string yeniDeger = newRes.StringResult ?? "";

            int changed = 0;
            foreach (ObjectId id in selRes.Value.GetObjectIds())
            {
                var attrs = BlockHelper.GetAttributes(id);
                string tb = attrs.TryGetValue("TB", out string tbv) ? tbv : "";
                string adetCarp = PozHelper.GetAdetCarpi(tb);
                string adet = PozHelper.GetAdet(tb);
                string cap = PozHelper.GetCap(tb);
                string aralik = PozHelper.GetAralik(tb);

                var updates = new Dictionary<string, string>();

                switch (degisBilgi)
                {
                    case "POZ":
                        if (attrs.TryGetValue("POZ", out string poz) && poz == eskiDeger)
                        { updates["POZ"] = yeniDeger; changed++; }
                        break;
                    case "ADET2":
                        if (adetCarp == eskiDeger)
                        {
                            string newAdetCarp = yeniDeger;
                            string carpiIsareti = newAdetCarp.Length > 0 ? "x" : "";
                            string boluIsareti = aralik.Length > 0 ? "/" : "";
                            updates["TB"] = newAdetCarp + carpiIsareti + adet + PozHelper.Fi + cap + boluIsareti + aralik;
                            changed++;
                        }
                        break;
                    case "ADET":
                        if (adet == eskiDeger)
                        {
                            string carpiIsareti = adetCarp.Length > 0 ? "x" : "";
                            string boluIsareti = aralik.Length > 0 ? "/" : "";
                            updates["TB"] = adetCarp + carpiIsareti + yeniDeger + PozHelper.Fi + cap + boluIsareti + aralik;
                            changed++;
                        }
                        break;
                    case "CAP":
                        if (cap == eskiDeger)
                        {
                            string carpiIsareti = adetCarp.Length > 0 ? "x" : "";
                            string boluIsareti = aralik.Length > 0 ? "/" : "";
                            updates["TB"] = adetCarp + carpiIsareti + adet + PozHelper.Fi + yeniDeger + boluIsareti + aralik;
                            changed++;
                        }
                        break;
                    case "ARALIK":
                        if (aralik == eskiDeger)
                        {
                            string carpiIsareti = adetCarp.Length > 0 ? "x" : "";
                            string boluIsareti = yeniDeger.Length > 0 ? "/" : "";
                            updates["TB"] = adetCarp + carpiIsareti + adet + PozHelper.Fi + cap + boluIsareti + yeniDeger;
                            updates["ARALIK"] = yeniDeger;
                            changed++;
                        }
                        break;
                    case "BOY":
                    case "TIP":
                    case "A":
                    case "B":
                    case "C":
                    case "D":
                    case "E":
                    case "F":
                    case "R":
                    case "GC":
                        if (attrs.TryGetValue(degisBilgi, out string cur) && cur == eskiDeger)
                        { updates[degisBilgi] = yeniDeger; changed++; }
                        break;
                }

                if (updates.Count > 0)
                    BlockHelper.SetAttributes(id, updates);
                PozHelper.RepositionShapeText(id);
            }

            ed.WriteMessage($"\n{changed} poz degistirildi.");
        }

        // ====================================================================================
        // КОМАНДА: TDDKN (было "tddk" в LISP)
        // ====================================================================================
        // НАЗНАЧЕНИЕ: Копирует ВСЕ атрибуты (по совпадению тега) из одного эталонного блока
        // в произвольное число целевых блоков (не только RL-POS). Работает циклически -
        // после каждого выбора снова просит выбрать следующую партию блоков, пока
        // пользователь не нажмёт Enter без выбора.
        // ПЕРЕВЕДЕНО ИЗ: (defun c:tddk (/) ...) — QUANTITY2.LSP, строки ~1113-1137
        [CommandMethod("TDDKN")]
        public static void CopyAttributesCommand()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            if (doc == null) return;
            Editor ed = doc.Editor;

            PromptEntityOptions entOpts = new PromptEntityOptions("\nOkunacak Poz'u sec: ");
            entOpts.SetRejectMessage("\nBlok secmelisiniz.");
            entOpts.AddAllowedClass(typeof(BlockReference), false);
            PromptEntityResult entRes = ed.GetEntity(entOpts);
            if (entRes.Status != PromptStatus.OK) return;

            Dictionary<string, string> sourceAttrs = BlockHelper.GetAttributes(entRes.ObjectId);
            if (sourceAttrs.Count == 0)
            {
                ed.WriteMessage("\nSecilen blokta attribute yok.");
                return;
            }

            TypedValue[] filterList = [new TypedValue((int)DxfCode.Start, "INSERT")];
            SelectionFilter anyInsertFilter = new SelectionFilter(filterList);

            while (true)
            {
                PromptSelectionOptions selOpts = new PromptSelectionOptions
                {
                    MessageForAdding = "\nDegistirilecek Pozlari sec: "
                };
                PromptSelectionResult selRes = ed.GetSelection(selOpts, anyInsertFilter);
                if (selRes.Status != PromptStatus.OK) break;

                foreach (ObjectId id in selRes.Value.GetObjectIds())
                {
                    BlockHelper.SetAttributes(id, sourceAttrs);
                    if (IsBlockNamed(id, "RL-POS")) PozHelper.RepositionShapeText(id);
                }
            }
        }

        // ====================================================================================
        // КОМАНДА: TDDUN (было "tddu" в LISP)
        // ====================================================================================
        // НАЗНАЧЕНИЕ: Применяет данные эталонной позиции (взятой с блока или введённой
        // с клавиатуры) ко всем выбранным блокам RL-POS с ТЕМ ЖЕ номером POZ. У каждой
        // цели сохраняется собственное количество/множитель/шаг в TB, меняется только
        // диаметр (cap) и остальные поля (BOY/TIP/A-F/R/POZ).
        // ПЕРЕВЕДЕНО ИЗ: (defun c:tddu (/) ...) — QUANTITY2.LSP, строки ~954-1033
        [CommandMethod("TDDUN")]
        public static void ApplyReferenceToMatchingPozCommand()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            if (doc == null) return;
            Editor ed = doc.Editor;

            string bilgiPoz, bilgiCap, bilgiBoy, bilgiTip, bilgiA, bilgiB, bilgiC, bilgiD, bilgiE, bilgiF, bilgiR;

            PromptEntityOptions entOpts = new PromptEntityOptions("\nOkunacak Poz'u sec (Enter = klavyeden gir): ");
            entOpts.SetRejectMessage("\nBlok secmelisiniz.");
            entOpts.AddAllowedClass(typeof(BlockReference), false);
            entOpts.AllowNone = true;
            PromptEntityResult entRes = ed.GetEntity(entOpts);

            if (entRes.Status != PromptStatus.OK)
            {
                bilgiPoz = ReadLine(ed, "\nPoz: ");
                bilgiCap = ReadLine(ed, "\nCap: ");
                bilgiBoy = ReadLine(ed, "\nBoy: ");
                if (!bilgiBoy.StartsWith("L", StringComparison.OrdinalIgnoreCase)) bilgiBoy = "L=" + bilgiBoy;
                bilgiTip = ReadLine(ed, "\nTip: ");
                bilgiA = ReadLine(ed, "\nA: ");
                bilgiB = ReadLine(ed, "\nB: ");
                bilgiC = ReadLine(ed, "\nC: ");
                bilgiD = ReadLine(ed, "\nD: ");
                bilgiE = ReadLine(ed, "\nE: ");
                bilgiF = ReadLine(ed, "\nF: ");
                bilgiR = ReadLine(ed, "\nR: ");
            }
            else
            {
                var srcAttrs = BlockHelper.GetAttributes(entRes.ObjectId);
                string srcTb = srcAttrs.TryGetValue("TB", out string tbv) ? tbv : "";
                bilgiPoz = srcAttrs.TryGetValue("POZ", out string p) ? p : "";
                bilgiCap = PozHelper.GetCap(srcTb);
                bilgiBoy = srcAttrs.TryGetValue("BOY", out string boy) ? boy : "";
                bilgiTip = srcAttrs.TryGetValue("TIP", out string tip) ? tip : "";
                bilgiA = srcAttrs.TryGetValue("A", out string a) ? a : "";
                bilgiB = srcAttrs.TryGetValue("B", out string b) ? b : "";
                bilgiC = srcAttrs.TryGetValue("C", out string c) ? c : "";
                bilgiD = srcAttrs.TryGetValue("D", out string d) ? d : "";
                bilgiE = srcAttrs.TryGetValue("E", out string e) ? e : "";
                bilgiF = srcAttrs.TryGetValue("F", out string f) ? f : "";
                bilgiR = srcAttrs.TryGetValue("R", out string r) ? r : "";
            }

            PromptSelectionOptions selOpts = new PromptSelectionOptions
            {
                MessageForAdding = "\nBu poza benzetilecek diger pozlari sec: "
            };
            PromptSelectionResult selRes = ed.GetSelection(selOpts, RlPosFilter());
            if (selRes.Status != PromptStatus.OK) return;

            int changed = 0;
            foreach (ObjectId id in selRes.Value.GetObjectIds())
            {
                var attrs = BlockHelper.GetAttributes(id);
                if (!attrs.TryGetValue("POZ", out string poz2) || poz2 != bilgiPoz) continue;

                string tb2 = attrs.TryGetValue("TB", out string tbv2) ? tbv2 : "";
                string aralik2 = PozHelper.GetAralik(tb2);
                string boluIsareti = aralik2.Length > 0 ? "/" : "";
                int fiIndex = tb2.IndexOf(PozHelper.Fi, StringComparison.OrdinalIgnoreCase);
                string prefix = fiIndex >= 0 ? tb2.Substring(0, fiIndex) : tb2;
                string newTb = prefix + PozHelper.Fi + bilgiCap + boluIsareti + aralik2;

                BlockHelper.SetAttributes(id, new Dictionary<string, string>
                {
                    ["POZ"] = bilgiPoz,
                    ["TB"] = newTb,
                    ["BOY"] = bilgiBoy,
                    ["TIP"] = bilgiTip,
                    ["A"] = bilgiA,
                    ["B"] = bilgiB,
                    ["C"] = bilgiC,
                    ["D"] = bilgiD,
                    ["E"] = bilgiE,
                    ["F"] = bilgiF,
                    ["R"] = bilgiR
                });
                PozHelper.RepositionShapeText(id);
                changed++;
            }

            ed.WriteMessage($"\n{changed} poz guncellendi.");
        }

        private static string ReadLine(Editor ed, string prompt)
        {
            PromptStringOptions opts = new PromptStringOptions(prompt) { AllowSpaces = true };
            PromptResult res = ed.GetString(opts);
            return res.Status == PromptStatus.OK ? (res.StringResult ?? "") : "";
        }

        // Аналог (= (cdr (assoc 2 (entget en))) "RL-POS") - проверяет имя блока по ObjectId
        private static bool IsBlockNamed(ObjectId blockId, string name)
        {
            if (blockId.IsNull) return false;
            using (Transaction tr = blockId.Database.TransactionManager.StartTransaction())
            {
                BlockReference blkRef = tr.GetObject(blockId, OpenMode.ForRead) as BlockReference;
                bool result = blkRef != null && string.Equals(blkRef.Name, name, StringComparison.OrdinalIgnoreCase);
                tr.Commit();
                return result;
            }
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
