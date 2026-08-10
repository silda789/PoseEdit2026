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
using System.Linq;
using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;

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

        // ====================================================================================
        // КОМАНДЫ: TDD1N / TDD2N / TDD3N (было "tdd1"/"tdd2"/"tdd3" в LISP)
        // ====================================================================================
        // НАЗНАЧЕНИЕ: Принудительно переставляет TB/BOY/NOT выбранных блоков RL-POS/RL-POS2
        // по одной из трёх жёстких схем компоновки (в отличие от RepositionShapeText/
        // poz_sekil_topla, здесь нет проверки "радиуса притяжения" - позиция ставится всегда).
        // ПЕРЕВЕДЕНО ИЗ: (defun c:tdd1/2/3 (/) ...) — QUANTITY2.LSP, строки ~210-289
        //
        // ПРИМЕЧАНИЕ: оригинал перед перестановкой отражает ("_mirror") блоки с отрицательным
        // масштабом (зеркальная вставка), чтобы текст не оказался "задом наперёд". Эта версия
        // такие блоки пропускает и предупреждает пользователя - безопаснее, чем воспроизводить
        // вызов команды MIRROR через межпроцессный интерфейс без возможности проверить его в AutoCAD.
        [CommandMethod("TDD1N")]
        public static void RearrangeScheme1Command() => RearrangeByScheme(1);

        [CommandMethod("TDD2N")]
        public static void RearrangeScheme2Command() => RearrangeByScheme(2);

        [CommandMethod("TDD3N")]
        public static void RearrangeScheme3Command() => RearrangeByScheme(3);

        private static void RearrangeByScheme(int scheme)
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            if (doc == null) return;
            Editor ed = doc.Editor;

            TypedValue[] filterList =
            [
                new TypedValue((int)DxfCode.Start, "INSERT"),
                new TypedValue(-4, "<OR"),
                new TypedValue((int)DxfCode.BlockName, "RL-POS"),
                new TypedValue((int)DxfCode.BlockName, "RL-POS2"),
                new TypedValue(-4, "OR>")
            ];
            PromptSelectionOptions selOpts = new PromptSelectionOptions
            {
                MessageForAdding = "\nPozisyonlari degisecek attributeleri seciniz: "
            };
            PromptSelectionResult selRes = ed.GetSelection(selOpts, new SelectionFilter(filterList));
            if (selRes.Status != PromptStatus.OK) return;

            int skipped = 0;
            foreach (ObjectId id in selRes.Value.GetObjectIds())
            {
                var (insBase, insAng, insScale) = PozHelper.GetBlockGeometry(id);
                if (insScale < 0) { skipped++; continue; }

                Point3d p11, p21, p31;
                switch (scheme)
                {
                    case 1:
                        p11 = PozHelper.PolarPoint(insBase, insAng, Math.Abs(insScale) * 45.0);
                        p11 = PozHelper.PolarPoint(p11, insAng + 0.5 * Math.PI, Math.Abs(insScale) * 10.0);
                        p21 = PozHelper.PolarPoint(p11, insAng + 1.5 * Math.PI, 45.0 * insScale);
                        p31 = PozHelper.PolarPoint(p21, insAng + 1.5 * Math.PI, 45.0 * insScale);
                        break;
                    case 2:
                        p11 = PozHelper.PolarPoint(insBase, insAng + 1.5 * Math.PI, 45.0 * insScale);
                        p11 = PozHelper.PolarPoint(p11, insAng + Math.PI, 30.0 * insScale);
                        p21 = PozHelper.PolarPoint(p11, insAng + 1.5 * Math.PI, 45.0 * insScale);
                        p31 = PozHelper.PolarPoint(p21, insAng + 1.5 * Math.PI, 45.0 * insScale);
                        break;
                    default: // 3
                        p11 = PozHelper.PolarPoint(insBase, insAng, Math.Abs(insScale) * 45.0);
                        p11 = PozHelper.PolarPoint(p11, insAng + 0.5 * Math.PI, Math.Abs(insScale) * 10.0);
                        p21 = PozHelper.PolarPoint(p11, insAng + 0.5 * Math.PI, 45.0 * insScale);
                        p31 = PozHelper.PolarPoint(p21, insAng + 0.5 * Math.PI, 45.0 * insScale);
                        break;
                }

                PozHelper.MoveAttrTo(id, "TB", p11);
                PozHelper.MoveAttrTo(id, "BOY", p21);
                PozHelper.MoveAttrTo(id, "NOT", p31);
            }

            if (skipped > 0)
                ed.WriteMessage($"\n{skipped} blok atlandi (negatif olcek/ayna - once elle duzeltin).");
        }

        // ====================================================================================
        // КОМАНДА: TDDHN (было "tddh" в LISP)
        // ====================================================================================
        // НАЗНАЧЕНИЕ: Выводит содержимое error.txt (файл ошибок, который создаёт RQT при
        // проверке метража) в чертёж построчно как текстовые примитивы, начиная с указанной
        // точки и опускаясь вниз.
        // ПЕРЕВЕДЕНО ИЗ: (defun c:tddh (/ pn1) ...) — QUANTITY2.LSP, строки ~1754-1769
        [CommandMethod("TDDHN")]
        public static void PrintErrorLogCommand()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            if (doc == null) return;
            Editor ed = doc.Editor;
            Database db = doc.Database;

            string errorFile = System.IO.Path.Combine(QuantityTableGenerator.GetClientPath(), "error.txt");
            if (!System.IO.File.Exists(errorFile))
            {
                ed.WriteMessage("\nerror.txt bulunamadi. Once RQT calistirin.");
                return;
            }

            double birim = QuantityTableGenerator.GetUnits();
            double olcek = QuantityTableGenerator.GetScale();
            double yuk = 0.0020 * olcek * birim;
            double kacma = 0.0010 * olcek * birim;

            PromptPointOptions ptOpts = new PromptPointOptions(
                "\nMetraj hata dosyasini yazdirmak istediginiz noktayi gosteriniz: ");
            PromptPointResult ptRes = ed.GetPoint(ptOpts);
            if (ptRes.Status != PromptStatus.OK) return;

            string[] lines = System.IO.File.ReadAllLines(errorFile);
            Point3d pn1 = ptRes.Value;

            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                BlockTable bt = tr.GetObject(db.BlockTableId, OpenMode.ForRead) as BlockTable;
                BlockTableRecord ms = tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite) as BlockTableRecord;

                foreach (string line in lines)
                {
                    pn1 = PozHelper.PolarPoint(pn1, 1.5 * Math.PI, yuk + 2.5 * kacma);
                    if (string.IsNullOrEmpty(line)) continue;

                    DBText txt = new DBText
                    {
                        Position = pn1,
                        Height = yuk,
                        TextString = line
                    };
                    ms.AppendEntity(txt);
                    tr.AddNewlyCreatedDBObject(txt, true);
                }

                tr.Commit();
            }
        }

        // ====================================================================================
        // КОМАНДА: DIEZN (было "diez" в LISP)
        // ====================================================================================
        // НАЗНАЧЕНИЕ: Находит все блоки RL-POS/RL-POS2 во всём чертеже, у которых POZ, TB
        // или размеры A-F/R содержат символ "#" (признак незавершённой/проблемной позиции),
        // и рисует от каждой стрелку к точке, указанной пользователем.
        // ПЕРЕВЕДЕНО ИЗ: (defun c:diez (/) ...) — Temp/Command/DIEZ.LSP
        [CommandMethod("DIEZN")]
        public static void MarkHashPositionsCommand()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            if (doc == null) return;
            Editor ed = doc.Editor;
            Database db = doc.Database;

            double hk = 1000.0 * db.Dimscale;

            PromptSelectionResult selRes = ed.SelectAll(RlPosOrPos2Filter());
            if (selRes.Status != PromptStatus.OK)
            {
                ed.WriteMessage("\nCizimde RL-POS/RL-POS2 blogu bulunamadi.");
                return;
            }

            var hashIds = new List<ObjectId>();
            foreach (ObjectId id in selRes.Value.GetObjectIds())
            {
                if (ContainsHash(BlockHelper.GetAttributes(id))) hashIds.Add(id);
            }

            if (hashIds.Count == 0)
            {
                ed.WriteMessage("\nDiez isaretli bir poz bulunmuyor...");
                return;
            }

            PromptPointOptions ptOpts = new PromptPointOptions(
                $"\n{hashIds.Count}.Adet eleman bulundu. Bulunan pozlara simdi isaretleyeceginiz noktadan oklar cizilecek. Noktayi gosteriniz: ");
            PromptPointResult ptRes = ed.GetPoint(ptOpts);
            if (ptRes.Status != PromptStatus.OK) return;
            Point3d pt3 = ptRes.Value;

            DrawArrowsToPoint(db, hashIds, pt3, hk);
        }

        // Аналог (strcat POZ TB A B C D E F R) содержит "#"
        private static bool ContainsHash(Dictionary<string, string> attrs)
        {
            string[] tags = ["POZ", "TB", "A", "B", "C", "D", "E", "F", "R"];
            foreach (string tag in tags)
                if (attrs.TryGetValue(tag, out string v) && v != null && v.Contains('#'))
                    return true;
            return false;
        }

        private static SelectionFilter RlPosOrPos2Filter()
        {
            TypedValue[] filterList =
            [
                new TypedValue((int)DxfCode.Start, "INSERT"),
                new TypedValue(-4, "<OR"),
                new TypedValue((int)DxfCode.BlockName, "RL-POS"),
                new TypedValue((int)DxfCode.BlockName, "RL-POS2"),
                new TypedValue(-4, "OR>")
            ];
            return new SelectionFilter(filterList);
        }

        // Рисует "стрелку" (полилиния с переменной шириной) от каждой позиции к общей точке.
        // Аналог (command "pline" pt1 "w" 0 (* hk 0.080) pt2 "w" 0 0 pt3 "") на слое "ren.arrow"
        private static void DrawArrowsToPoint(Database db, List<ObjectId> ids, Point3d target, double hk)
        {
            int oldOsmode = (int)Application.GetSystemVariable("OSMODE");
            Application.SetSystemVariable("OSMODE", 0);
            try
            {
                using (Transaction tr = db.TransactionManager.StartTransaction())
                {
                    EnsureLayer(tr, db, "ren.arrow");
                    BlockTable bt = tr.GetObject(db.BlockTableId, OpenMode.ForRead) as BlockTable;
                    BlockTableRecord ms = tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite) as BlockTableRecord;

                    foreach (ObjectId id in ids)
                    {
                        BlockReference blkRef = tr.GetObject(id, OpenMode.ForRead) as BlockReference;
                        if (blkRef == null) continue;
                        Point3d pt1 = blkRef.Position;
                        double aci = Math.Atan2(target.Y - pt1.Y, target.X - pt1.X);
                        Point3d pt2 = PozHelper.PolarPoint(pt1, aci, hk * 0.240);

                        Polyline pl = new Polyline();
                        pl.AddVertexAt(0, new Point2d(pt1.X, pt1.Y), 0, 0, hk * 0.080);
                        pl.AddVertexAt(1, new Point2d(pt2.X, pt2.Y), 0, 0, 0);
                        pl.AddVertexAt(2, new Point2d(target.X, target.Y), 0, 0, 0);
                        pl.Layer = "ren.arrow";

                        ms.AppendEntity(pl);
                        tr.AddNewlyCreatedDBObject(pl, true);
                    }

                    tr.Commit();
                }
            }
            finally
            {
                Application.SetSystemVariable("OSMODE", oldOsmode);
            }
        }

        private static void EnsureLayer(Transaction tr, Database db, string layerName)
        {
            LayerTable lt = tr.GetObject(db.LayerTableId, OpenMode.ForRead) as LayerTable;
            if (lt.Has(layerName)) return;
            lt.UpgradeOpen();
            LayerTableRecord ltr = new LayerTableRecord { Name = layerName };
            lt.Add(ltr);
            tr.AddNewlyCreatedDBObject(ltr, true);
        }

        // ====================================================================================
        // КОМАНДА: PPPN (было "ppp" в LISP)
        // ====================================================================================
        // НАЗНАЧЕНИЕ: Из указанной вручную выборки блоков находит все с заданным номером POZ
        // и рисует от каждого стрелку к общей точке.
        // ПЕРЕВЕДЕНО ИЗ: (defun c:ppp (/) ...) — QUANTITY2.LSP, строки ~699-728
        [CommandMethod("PPPN")]
        public static void DrawArrowsToPozCommand()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            if (doc == null) return;
            Editor ed = doc.Editor;
            Database db = doc.Database;

            double hk = 0.05 * QuantityTableGenerator.GetScale() * QuantityTableGenerator.GetUnits();

            string pozNo = ReadLine(ed, "\nPoz no girin: ");
            if (string.IsNullOrEmpty(pozNo)) return;

            PromptSelectionOptions selOpts = new PromptSelectionOptions
            {
                MessageForAdding = "\nAranacak pozlari seciniz: "
            };
            PromptSelectionResult selRes = ed.GetSelection(selOpts, RlPosOrPos2Filter());
            if (selRes.Status != PromptStatus.OK) return;

            PromptPointOptions ptOpts = new PromptPointOptions(
                "\nBulunan pozlara simdi isaretleyeceginiz noktadan oklar cizilecek. Noktayi gosteriniz: ");
            PromptPointResult ptRes = ed.GetPoint(ptOpts);
            if (ptRes.Status != PromptStatus.OK) return;

            var matchIds = new List<ObjectId>();
            foreach (ObjectId id in selRes.Value.GetObjectIds())
            {
                var attrs = BlockHelper.GetAttributes(id);
                if (attrs.TryGetValue("POZ", out string p) && p == pozNo) matchIds.Add(id);
            }

            DrawArrowsToPoint(db, matchIds, ptRes.Value, hk);
            ed.WriteMessage($"\n{matchIds.Count} poz isaretlendi.");
        }

        // ====================================================================================
        // КОМАНДА: PPP2N (было "ppp2" в LISP)
        // ====================================================================================
        // НАЗНАЧЕНИЕ: Находит все блоки с заданным POZ в выборке, по очереди приближает
        // (zoom) к каждому и показывает все атрибуты, спрашивая продолжать ли поиск.
        // Если и искомый, и найденный POZ начинаются с "#" - предлагает удалить элемент.
        // ПЕРЕВЕДЕНО ИЗ: (defun c:ppp2 (/) ...) — QUANTITY2.LSP, строки ~730-780
        [CommandMethod("PPP2N")]
        public static void FindAndReviewPozCommand()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            if (doc == null) return;
            Editor ed = doc.Editor;
            Database db = doc.Database;

            double birim = QuantityTableGenerator.GetUnits();

            string pozNo = ReadLine(ed, "\nPoz no girin: ");
            if (string.IsNullOrEmpty(pozNo)) return;

            PromptSelectionOptions selOpts = new PromptSelectionOptions
            {
                MessageForAdding = "\nAranacak pozlari seciniz: "
            };
            PromptSelectionResult selRes = ed.GetSelection(selOpts, RlPosOrPos2Filter());
            if (selRes.Status != PromptStatus.OK) return;

            ObjectId[] ids = selRes.Value.GetObjectIds();
            var matches = new List<ObjectId>();
            foreach (ObjectId id in ids)
            {
                var attrs = BlockHelper.GetAttributes(id);
                if (attrs.TryGetValue("POZ", out string p) && p == pozNo) matches.Add(id);
            }

            if (matches.Count == 0)
            {
                ed.WriteMessage("\nArama tamamlandi ... ama bisey bulamadik..!");
                return;
            }

            bool searchIsHash = pozNo.StartsWith("#", StringComparison.Ordinal);
            int sira = 0;
            foreach (ObjectId id in matches)
            {
                sira++;
                var attrs = BlockHelper.GetAttributes(id);
                using (Transaction tr = db.TransactionManager.StartTransaction())
                {
                    BlockReference blkRef = tr.GetObject(id, OpenMode.ForRead) as BlockReference;
                    if (blkRef != null)
                    {
                        Point3d pt1 = blkRef.Position;
                        double halfW = 3.0 * birim;
                        ZoomToPoint(ed, pt1, halfW);
                    }
                    tr.Commit();
                }

                string info = string.Join(" , ", attrs.Select(kv => $"{kv.Key}: {kv.Value}"));
                string x = ReadLine(ed, $"\nNo:{sira}/{matches.Count} - {info} Aramaya devam edilsin mi?..<E>...");
                if (string.Equals(x, "h", StringComparison.OrdinalIgnoreCase)) break;

                if (searchIsHash)
                {
                    string y = ReadLine(ed, "\nGecerli eleman silinsin mi? ");
                    if (string.Equals(y, "e", StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(y, "E", StringComparison.Ordinal))
                    {
                        using (Transaction tr = db.TransactionManager.StartTransaction())
                        {
                            Entity ent = tr.GetObject(id, OpenMode.ForWrite) as Entity;
                            ent?.Erase();
                            tr.Commit();
                        }
                    }
                }
            }

            ed.WriteMessage("\nArama tamamlandi ... ");
        }

        // Аналог (command "zoom" "w" p1 p2) - зумирует на квадрат вокруг точки
        private static void ZoomToPoint(Editor ed, Point3d center, double halfWidth)
        {
            ViewTableRecord view = ed.GetCurrentView();
            view.CenterPoint = new Point2d(center.X, center.Y);
            view.Height = halfWidth * 2.0;
            view.Width = halfWidth * 2.0;
            ed.SetCurrentView(view);
        }

        // ====================================================================================
        // КОМАНДА: POZVERN (было "pozver" в LISP)
        // ====================================================================================
        // НАЗНАЧЕНИЕ: Автоматически присваивает номера POZ выбранным блокам RL-POS: одинаковые
        // по геометрии (диаметр/тип/BOY/A-F/R) позиции получают один и тот же номер.
        // Если пользователь ничего не выбрал на первом запросе - переходит в режим "сохранить
        // уже пронумерованные (TIK=0) позиции", подбирая новым позициям номера уже
        // существующих идентичных, а остаток нумерует заново начиная со следующего номера.
        // ПЕРЕВЕДЕНО ИЗ: (defun c:pozver (/) ...) — Temp/Command/QUANTITY_COUNT.LSP
        [CommandMethod("POZVERN")]
        public static void AutoNumberPositionsCommand()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            if (doc == null) return;
            Editor ed = doc.Editor;

            PromptSelectionOptions selOpts = new PromptSelectionOptions
            {
                MessageForAdding = "\nOtomatik pozlandirilacak pozlari sec (Enter = TIK=0 pozlari koru): "
            };
            PromptSelectionResult selRes = ed.GetSelection(selOpts, RlPosFilter());

            List<ObjectId> eset;
            int sonPoz = 0;

            if (selRes.Status != PromptStatus.OK)
            {
                ed.WriteMessage("\nDaha oncden pozlandirilmis pozlar kalacak, edit yapilmis pozlar pozlandirilacak. Simdi tekrar sec...");
                PromptSelectionResult selAllRes = ed.GetSelection(selOpts, RlPosFilter());
                if (selAllRes.Status != PromptStatus.OK) return;

                var esetOnce = new List<ObjectId>();
                eset = new List<ObjectId>();
                foreach (ObjectId id in selAllRes.Value.GetObjectIds())
                {
                    var a = BlockHelper.GetAttributes(id);
                    string tik = a.TryGetValue("TIK", out string tv) ? tv : "";
                    if (tik == "0") esetOnce.Add(id); else eset.Add(id);
                }

                foreach (ObjectId id in esetOnce)
                {
                    var a = BlockHelper.GetAttributes(id);
                    if (int.TryParse(a.TryGetValue("POZ", out string p) ? p : "0", out int pv))
                        sonPoz = Math.Max(sonPoz, pv);
                }

                for (int i = eset.Count - 1; i >= 0; i--)
                {
                    ShapeKey key = MakeShapeKey(BlockHelper.GetAttributes(eset[i]));
                    foreach (ObjectId onceId in esetOnce)
                    {
                        if (!key.Equals(MakeShapeKey(BlockHelper.GetAttributes(onceId)))) continue;

                        var onceAttrs = BlockHelper.GetAttributes(onceId);
                        string poz = onceAttrs.TryGetValue("POZ", out string pz) ? pz : "";
                        BlockHelper.SetAttributes(eset[i], new Dictionary<string, string> { ["POZ"] = poz, ["TIK"] = "0" });
                        eset.RemoveAt(i);
                        break;
                    }
                    if (eset.Count < 1) break;
                }
            }
            else
            {
                eset = new List<ObjectId>(selRes.Value.GetObjectIds());
            }

            if (eset.Count == 0)
            {
                ed.WriteMessage("\nPoz verme islemi tamamlandi... ");
                return;
            }

            var uniqueKeys = new List<ShapeKey>();
            foreach (ObjectId id in eset)
            {
                ShapeKey key = MakeShapeKey(BlockHelper.GetAttributes(id));
                if (!uniqueKeys.Contains(key)) uniqueKeys.Add(key);
            }
            uniqueKeys = uniqueKeys
                .OrderBy(k => int.TryParse(k.Cap, out int c) ? c : 0)
                .ThenBy(k => QuantityTableGenerator.ParseBoyInt(k.Boy))
                .ToList();

            foreach (ObjectId id in eset)
            {
                ShapeKey key = MakeShapeKey(BlockHelper.GetAttributes(id));
                int idx = uniqueKeys.IndexOf(key);
                if (idx < 0) continue;
                int newPoz = sonPoz + idx + 1;
                BlockHelper.SetAttributes(id, new Dictionary<string, string>
                {
                    ["POZ"] = newPoz.ToString(),
                    ["TIK"] = "0"
                });
            }

            ed.Regen();
            ed.WriteMessage("\nPoz verme islemi tamamlandi... ");
        }

        // Ключ идентичности геометрии позиции (аналог poz_icin_oku): совпадение этих полей
        // означает "это одна и та же форма арматуры" для целей автонумерации
        private readonly struct ShapeKey : IEquatable<ShapeKey>
        {
            public readonly string Cap, Tip, Boy, A, B, C, D, E, F, R;

            public ShapeKey(string cap, string tip, string boy, string a, string b, string c, string d, string e, string f, string r)
            {
                Cap = cap; Tip = tip; Boy = boy; A = a; B = b; C = c; D = d; E = e; F = f; R = r;
            }

            public bool Equals(ShapeKey other) =>
                Cap == other.Cap && Tip == other.Tip && Boy == other.Boy &&
                A == other.A && B == other.B && C == other.C && D == other.D &&
                E == other.E && F == other.F && R == other.R;

            public override bool Equals(object obj) => obj is ShapeKey other && Equals(other);

            public override int GetHashCode()
            {
                unchecked
                {
                    int hash = 17;
                    foreach (string s in new[] { Cap, Tip, Boy, A, B, C, D, E, F, R })
                        hash = hash * 31 + (s?.GetHashCode() ?? 0);
                    return hash;
                }
            }
        }

        private static ShapeKey MakeShapeKey(Dictionary<string, string> attrs)
        {
            string tb = attrs.TryGetValue("TB", out string tbv) ? tbv : "";
            string rawCap = PozHelper.GetCap(tb);
            string cap = int.TryParse(rawCap, out int capN) ? capN.ToString() : rawCap;
            string Get(string tag) => attrs.TryGetValue(tag, out string v) ? v : "";
            return new ShapeKey(cap, Get("TIP"), Get("BOY"), Get("A"), Get("B"), Get("C"), Get("D"), Get("E"), Get("F"), Get("R"));
        }

        // ====================================================================================
        // КОМАНДА: 77N (было "77" в LISP)
        // ====================================================================================
        // НАЗНАЧЕНИЕ: Создаёт связанный блок RL-POS2 (выноска на сечении), поля которого
        // ссылаются на POZ/TB/NOT выбранной позиции через ACAD_FIELD - при изменении
        // исходной позиции текст выноски обновится автоматически (UPDATEFIELD).
        // ПЕРЕВЕДЕНО ИЗ: (defun c:77 (/ en blk atts att pn id olcu aci p71 text) ...)
        //                — QUANTITY2.LSP, строки ~898-952
        [CommandMethod("77N")]
        public static void CreateLinkedCalloutCommand()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            if (doc == null) return;
            Editor ed = doc.Editor;
            Database db = doc.Database;

            PromptEntityOptions entOpts = new PromptEntityOptions("\nOkunacak Poz'u sec: ");
            entOpts.SetRejectMessage("\nBlok secmelisiniz.");
            entOpts.AddAllowedClass(typeof(BlockReference), false);
            PromptEntityResult entRes = ed.GetEntity(entOpts);
            if (entRes.Status != PromptStatus.OK) return;

            double insScale = (QuantityTableGenerator.GetScale() / QuantityTableGenerator.GetUnits()) * 100.0;

            long idPoz = 0, idTb = 0, idNot = 0;
            double aci = 0;

            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                BlockReference blkRef = tr.GetObject(entRes.ObjectId, OpenMode.ForRead) as BlockReference;
                if (blkRef == null) { tr.Commit(); return; }

                foreach (ObjectId attId in blkRef.AttributeCollection)
                {
                    AttributeReference attRef = tr.GetObject(attId, OpenMode.ForRead) as AttributeReference;
                    if (attRef == null) continue;
                    if (string.Equals(attRef.Tag, "POZ", StringComparison.OrdinalIgnoreCase))
                    {
                        idPoz = attRef.ObjectId.Handle.Value;
                        aci = attRef.Rotation;
                    }
                    else if (string.Equals(attRef.Tag, "TB", StringComparison.OrdinalIgnoreCase))
                        idTb = attRef.ObjectId.Handle.Value;
                    else if (string.Equals(attRef.Tag, "NOT", StringComparison.OrdinalIgnoreCase))
                        idNot = attRef.ObjectId.Handle.Value;
                }
                tr.Commit();
            }

            if (idPoz == 0 || idTb == 0)
            {
                ed.WriteMessage("\nSecilen blokta POZ/TB attribute bulunamadi.");
                return;
            }

            ed.WriteMessage($"\nAci: {aci * 180.0 / Math.PI:0}");
            PromptPointOptions ptOpts = new PromptPointOptions("\nYerlesim Noktasi: ");
            PromptPointResult ptRes = ed.GetPoint(ptOpts);
            if (ptRes.Status != PromptStatus.OK) return;
            Point3d pt1 = ptRes.Value;

            string text1 = $"%<\\AcObjProp Object(%<\\_ObjId {idPoz}>%).TextString>%";
            string text2 = $"%<\\AcObjProp Object(%<\\_ObjId {idTb}>%).TextString>%";
            string text3 = idNot != 0 ? $"%<\\AcObjProp Object(%<\\_ObjId {idNot}>%).TextString>%" : "";

            string tempFile = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "RL-POS2.dwg");
            try { ExtractEmbeddedResource("PoseEdit2026.Resources.RL-POS2.dwg", tempFile); }
            catch (System.Exception ex) { ed.WriteMessage($"\nRL-POS2.dwg cikartilamadi: {ex.Message}"); return; }

            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                ObjectId btrId;
                BlockTable bt = tr.GetObject(db.BlockTableId, OpenMode.ForRead) as BlockTable;
                if (bt.Has("RL-POS2"))
                {
                    btrId = bt["RL-POS2"];
                }
                else
                {
                    Database sourceDb = new Database(false, true);
                    sourceDb.ReadDwgFile(tempFile, FileOpenMode.OpenForReadAndAllShare, true, null);
                    btrId = db.Insert("RL-POS2", sourceDb, false);
                    sourceDb.Dispose();
                }

                BlockTableRecord ms = tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite) as BlockTableRecord;
                BlockTableRecord btr = tr.GetObject(btrId, OpenMode.ForRead) as BlockTableRecord;

                using (BlockReference newRef = new BlockReference(pt1, btrId))
                {
                    newRef.ScaleFactors = new Scale3d(insScale, insScale, insScale);
                    newRef.Rotation = aci;
                    newRef.Layer = "ren.mtr.Pos";
                    ms.AppendEntity(newRef);
                    tr.AddNewlyCreatedDBObject(newRef, true);

                    foreach (ObjectId defId in btr)
                    {
                        AttributeDefinition attDef = tr.GetObject(defId, OpenMode.ForRead) as AttributeDefinition;
                        if (attDef == null || attDef.Constant) continue;

                        using (AttributeReference newAttr = new AttributeReference())
                        {
                            newAttr.SetAttributeFromBlock(attDef, newRef.BlockTransform);
                            string tagUp = attDef.Tag.ToUpperInvariant();
                            if (tagUp == "POZ") newAttr.TextString = text1;
                            else if (tagUp == "TB") newAttr.TextString = text2;
                            else if (tagUp == "NOT") newAttr.TextString = text3;
                            newRef.AttributeCollection.AppendAttribute(newAttr);
                            tr.AddNewlyCreatedDBObject(newAttr, true);
                        }
                    }
                }

                tr.Commit();
            }

            ed.WriteMessage("\n77N: bagli poz olusturuldu (alanlari guncellemek icin gerekirse UPDATEFIELD calistirin).");
        }

        // ====================================================================================
        // КОМАНДА: PZGN (было "pzg" в LISP)
        // ====================================================================================
        // НАЗНАЧЕНИЕ: Переопределяет определение блока RL-POS из текущего встроенного шаблона
        // (Resources/RL-POS.dwg) и досинхронизирует все блоки RL-POS в чертеже с новым
        // набором атрибутов (аналог команды ATTSYNC): добавляет новые атрибуты определения,
        // удаляет отсутствующие, СОХРАНЯЕТ значения совпадающих тегов. После синхронизации
        // восстанавливает прежнее расположение TB/BOY/NOT (ATTSYNC иначе сбросил бы их
        // позицию на дефолтную из определения блока) и пересчитывает раскладку.
        // ПЕРЕВЕДЕНО ИЗ: (defun c:pzg (/) ...) — QUANTITY2.LSP, строки ~72-110
        //
        // ВНИМАНИЕ: это операция массового изменения по ВСЕМ блокам RL-POS в чертеже -
        // сохраните чертёж перед запуском.
        [CommandMethod("PZGN")]
        public static void SyncBlockDefinitionCommand()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            if (doc == null) return;
            Editor ed = doc.Editor;
            Database db = doc.Database;

            string confirm = ReadLine(ed,
                "\nBu islem cizimdeki TUM RL-POS bloklarini guncel sablonla senkronize edecek. " +
                "Once cizimi kaydedin. Devam edilsin mi? (Evet/Hayir): ");
            if (!string.Equals(confirm, "Evet", StringComparison.OrdinalIgnoreCase)) return;

            PromptSelectionResult selRes = ed.SelectAll(RlPosFilter());
            if (selRes.Status != PromptStatus.OK)
            {
                ed.WriteMessage("\nGuncellenecek RL-POS blogu bulunamadi.");
                return;
            }
            ObjectId[] ids = selRes.Value.GetObjectIds();

            var oldPositions = new Dictionary<ObjectId, (Point3d Tb, Point3d Boy, Point3d Not)>();
            foreach (ObjectId id in ids)
            {
                using (Transaction tr = db.TransactionManager.StartTransaction())
                {
                    BlockReference blkRef = tr.GetObject(id, OpenMode.ForRead) as BlockReference;
                    Point3d tb = Point3d.Origin, boy = Point3d.Origin, nott = Point3d.Origin;
                    if (blkRef != null)
                    {
                        foreach (ObjectId attId in blkRef.AttributeCollection)
                        {
                            AttributeReference attRef = tr.GetObject(attId, OpenMode.ForRead) as AttributeReference;
                            if (attRef == null) continue;
                            if (string.Equals(attRef.Tag, "TB", StringComparison.OrdinalIgnoreCase)) tb = attRef.Position;
                            else if (string.Equals(attRef.Tag, "BOY", StringComparison.OrdinalIgnoreCase)) boy = attRef.Position;
                            else if (string.Equals(attRef.Tag, "NOT", StringComparison.OrdinalIgnoreCase)) nott = attRef.Position;
                        }
                    }
                    tr.Commit();
                    oldPositions[id] = (tb, boy, nott);
                }
            }

            string tempFile = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "RL-POS_sync.dwg");
            try { ExtractEmbeddedResource("PoseEdit2026.Resources.RL-POS.dwg", tempFile); }
            catch (System.Exception ex) { ed.WriteMessage($"\nRL-POS.dwg cikartilamadi: {ex.Message}"); return; }

            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                Database sourceDb = new Database(false, true);
                sourceDb.ReadDwgFile(tempFile, FileOpenMode.OpenForReadAndAllShare, true, null);
                db.Insert("RL-POS", sourceDb, false);
                sourceDb.Dispose();
                tr.Commit();
            }

            int synced = 0;
            foreach (ObjectId id in ids)
            {
                SyncAttributesToDefinition(db, id);

                var (oldTb, oldBoy, oldNot) = oldPositions[id];
                PozHelper.MoveAttrTo(id, "TB", oldTb);
                PozHelper.MoveAttrTo(id, "BOY", oldBoy);
                PozHelper.MoveAttrTo(id, "NOT", oldNot);

                var attrs = BlockHelper.GetAttributes(id);
                string tb = attrs.TryGetValue("TB", out string tbv) ? tbv : "";
                BlockHelper.SetAttributes(id, new Dictionary<string, string> { ["ARALIK"] = PozHelper.GetAralik(tb) });

                PozHelper.RepositionShapeText(id);
                synced++;
            }

            ed.WriteMessage($"\n{synced} RL-POS blogu guncel sablonla senkronize edildi.");
        }

        // Аналог ATTSYNC для одного блока: пересоздаёт атрибуты по актуальному определению,
        // сохраняя значения тегов, которые совпадают со старыми
        private static void SyncAttributesToDefinition(Database db, ObjectId blockRefId)
        {
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                BlockReference blkRef = tr.GetObject(blockRefId, OpenMode.ForWrite) as BlockReference;
                if (blkRef == null) { tr.Commit(); return; }

                BlockTableRecord btr = tr.GetObject(blkRef.BlockTableRecord, OpenMode.ForRead) as BlockTableRecord;

                var existingValues = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                var oldAttrIds = new List<ObjectId>();
                foreach (ObjectId attId in blkRef.AttributeCollection)
                {
                    AttributeReference attRef = tr.GetObject(attId, OpenMode.ForRead) as AttributeReference;
                    if (attRef == null) continue;
                    existingValues[attRef.Tag] = attRef.TextString;
                    oldAttrIds.Add(attId);
                }

                foreach (ObjectId id in oldAttrIds)
                {
                    AttributeReference attRef = tr.GetObject(id, OpenMode.ForWrite) as AttributeReference;
                    attRef?.Erase();
                }

                foreach (ObjectId defId in btr)
                {
                    AttributeDefinition attDef = tr.GetObject(defId, OpenMode.ForRead) as AttributeDefinition;
                    if (attDef == null || attDef.Constant) continue;

                    using (AttributeReference newAttr = new AttributeReference())
                    {
                        newAttr.SetAttributeFromBlock(attDef, blkRef.BlockTransform);
                        newAttr.TextString = existingValues.TryGetValue(attDef.Tag, out string oldVal) ? oldVal : attDef.TextString;
                        blkRef.AttributeCollection.AppendAttribute(newAttr);
                        tr.AddNewlyCreatedDBObject(newAttr, true);
                    }
                }

                tr.Commit();
            }
        }

        // Извлекает встроенный DWG-ресурс во временный файл (аналог логики в Commands.cs)
        private static void ExtractEmbeddedResource(string resourceName, string destPath)
        {
            var asm = System.Reflection.Assembly.GetExecutingAssembly();
            using (var stream = asm.GetManifestResourceStream(resourceName))
            {
                if (stream == null) throw new InvalidOperationException($"Resource not found: {resourceName}");
                using (var file = System.IO.File.Create(destPath))
                {
                    stream.CopyTo(file);
                }
            }
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
