// ====================================================================================
// ФАЙЛ: PozHelper.cs
// НАЗНАЧЕНИЕ: Общая инфраструктура для команд редактирования блоков RL-POS,
//             портированных из Temp/Command/QUANTITY2.LSP и Temp/Command/POSEDIT.LSP.
// ====================================================================================
// Содержит:
//   1. Разбор строки TB (например "3x20%%C10/100") на составные части —
//      аналог poz_edit_adet_carpi / poz_edit_adet / poz_edit_cap / poz_edit_aralik
//   2. RepositionShapeText — аналог poz_sekil_topla: автоматически подставляет
//      BOY/NOT рядом с TB и скрывает пустые атрибуты
// ====================================================================================

#nullable disable
using System;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;

namespace PoseEdit2026
{
    public static class PozHelper
    {
        // "%%C" - управляющий код AutoCAD для символа диаметра (Ø). В LISP это (fi_) -> fi
        public const string Fi = "%%C";

        // ====================================================================================
        // РАЗБОР СТРОКИ TB (сырые подстроки, БЕЗ вычисления выражений - аналог poz_edit_* из POSEDIT.LSP)
        // ====================================================================================

        // poz_edit_adet_carpi: множитель БЕЗ символа "x" (например "3" в "3x20%%C10" - вызывающий
        // код сам дописывает "x" при сборке строки, как и в оригинале)
        public static string GetAdetCarpi(string tb)
        {
            if (string.IsNullOrEmpty(tb)) return "";
            int fiIndex = tb.IndexOf(Fi, StringComparison.OrdinalIgnoreCase);
            if (fiIndex < 0) return "";
            string adetKismi = tb.Substring(0, fiIndex);
            int lastX = adetKismi.ToUpperInvariant().LastIndexOf('X');
            if (adetKismi.Length > 0 && lastX >= 0)
                return adetKismi.Substring(0, lastX);
            return "";
        }

        // poz_edit_adet: количество стержней (без множителя), например "20" в "3x20%%C10"
        public static string GetAdet(string tb)
        {
            if (string.IsNullOrEmpty(tb)) return "";
            int fiIndex = tb.IndexOf(Fi, StringComparison.OrdinalIgnoreCase);
            if (fiIndex < 0) return "";
            string adetKismi = tb.Substring(0, fiIndex);
            if (adetKismi.Length == 0) return "";
            int lastX = adetKismi.ToUpperInvariant().LastIndexOf('X');
            if (lastX < 0) return adetKismi;
            return adetKismi.Substring(lastX + 1);
        }

        // poz_edit_cap: диаметр между "%%C" и "/" (или до конца строки), например "10" в "20%%C10/100"
        public static string GetCap(string tb)
        {
            if (string.IsNullOrEmpty(tb)) return "";
            int fiIndex = tb.IndexOf(Fi, StringComparison.OrdinalIgnoreCase);
            if (fiIndex < 0) return "";
            int start = fiIndex + Fi.Length;
            int slashIndex = tb.IndexOf('/', start);
            if (slashIndex < 0) slashIndex = tb.Length;
            if (slashIndex < start) return "";
            return tb.Substring(start, slashIndex - start);
        }

        // poz_edit_aralik: шаг после "/", например "100" в "20%%C10/100"
        public static string GetAralik(string tb)
        {
            if (string.IsNullOrEmpty(tb)) return "";
            int slashIndex = tb.IndexOf('/');
            if (slashIndex < 0 || slashIndex >= tb.Length - 1) return "";
            return tb.Substring(slashIndex + 1);
        }

        // ====================================================================================
        // ФУНКЦИЯ: RepositionShapeText (аналог poz_sekil_topla из QUANTITY2.LSP, строки ~365-435)
        // ====================================================================================
        // НАЗНАЧЕНИЕ: После изменения текста TB автоматически:
        //   1. Скрывает атрибуты TB/BOY/NOT, если они пустые
        //   2. Если BOY/NOT находятся близко к "штатной" позиции рядом с TB (в радиусе
        //      Etki_daire = 20*масштаб) - подтягивает их точно на штатное место
        // ПРИМЕЧАНИЕ: TB_GEN/BOY_GEN - эвристическая оценка ширины текста в единицах чертежа
        // (как в оригинале). Коэффициент "ScaleFactor" ActiveX-объекта не имеет прямого
        // аналога в .NET API AttributeReference, поэтому принят равным 1.0 - это влияет
        // только на точность автоподстановки (не критично, при необходимости текст можно
        // подвинуть вручную), но не на инженерные данные позиции.
        public static void RepositionShapeText(ObjectId blockId)
        {
            if (blockId.IsNull) return;

            using (Transaction tr = blockId.Database.TransactionManager.StartTransaction())
            {
                BlockReference blkRef = tr.GetObject(blockId, OpenMode.ForWrite) as BlockReference;
                if (blkRef == null) { tr.Commit(); return; }

                AttributeReference tbAtt = FindAttr(tr, blkRef, "TB");
                AttributeReference boyAtt = FindAttr(tr, blkRef, "BOY");
                AttributeReference notAtt = FindAttr(tr, blkRef, "NOT");

                if (tbAtt != null) tbAtt.Invisible = tbAtt.TextString.Length < 2;
                if (boyAtt != null) boyAtt.Invisible = string.IsNullOrEmpty(boyAtt.TextString) || boyAtt.TextString == " ";
                if (notAtt != null) notAtt.Invisible = string.IsNullOrEmpty(notAtt.TextString) || notAtt.TextString == " ";

                if (tbAtt == null)
                {
                    tr.Commit();
                    return;
                }

                Point3d insBase = blkRef.Position;
                double insAng = blkRef.Rotation;
                double insScale = blkRef.ScaleFactors.X;
                double etkiDaire = 20.0 * insScale;

                const double scaleFactorHeuristic = 1.0; // см. комментарий выше

                double tbGen = (0.05 * tbAtt.Height) + (0.65 * tbAtt.Height * scaleFactorHeuristic * tbAtt.TextString.Length);
                double boyGen = boyAtt != null
                    ? (0.55 * boyAtt.Height) + (0.75 * boyAtt.Height * scaleFactorHeuristic * boyAtt.TextString.Length)
                    : 0.0;

                Point3d p10 = tbAtt.Position;

                Point3d p11 = Polar(insBase, insAng, Math.Abs(insScale) * 45.0);
                p11 = Polar(p11, insAng + 0.5 * Math.PI, Math.Abs(insScale) * 10.0);

                double distP10P11 = p10.DistanceTo(p11);
                if (distP10P11 > 0 && distP10P11 < etkiDaire)
                {
                    MoveAttr(tbAtt, p11);
                    p10 = tbAtt.Position;
                }

                if (boyAtt != null)
                {
                    Point3d p20 = boyAtt.Position;

                    Point3d p21 = Polar(p10, insAng, tbGen);
                    Point3d p22 = Polar(p10, insAng + 0.5 * Math.PI, 45.0 * insScale);
                    Point3d p23 = Polar(p10, insAng + 1.5 * Math.PI, 45.0 * insScale);

                    bool nearAngle = AnglesEqual(insAng, AngleTo(p10, p20), 0.10);
                    double distP20P21 = p20.DistanceTo(p21);
                    double distP20P22 = p20.DistanceTo(p22);
                    double distP20P23 = p20.DistanceTo(p23);

                    if ((distP20P21 > 0 && distP20P21 < etkiDaire) || nearAngle)
                        MoveAttr(boyAtt, p21);
                    if (distP20P22 > 0 && distP20P22 < etkiDaire)
                        MoveAttr(boyAtt, p22);
                    if (distP20P23 > 0 && distP20P23 < etkiDaire)
                        MoveAttr(boyAtt, p23);

                    if (notAtt != null)
                    {
                        Point3d p30 = notAtt.Position;
                        Point3d p31 = (distP20P21 > 0 && distP20P21 < etkiDaire) || nearAngle
                            ? Polar(p10, insAng, tbGen + boyGen)
                            : Polar(p10, insAng, tbGen);
                        Point3d p32 = Polar(p10, insAng + 0.5 * Math.PI, 90.0 * insScale);
                        Point3d p33 = Polar(p10, insAng + 1.5 * Math.PI, 90.0 * insScale);

                        bool nearAngleNot = AnglesEqual(insAng, AngleTo(p10, p30), 0.10);
                        double distP30P31 = p30.DistanceTo(p31);
                        double distP30P32 = p30.DistanceTo(p32);
                        double distP30P33 = p30.DistanceTo(p33);

                        if ((distP30P31 > 0 && distP30P31 < etkiDaire) || nearAngleNot)
                            MoveAttr(notAtt, p31);
                        if (distP30P32 > 0 && distP30P32 < etkiDaire)
                            MoveAttr(notAtt, p32);
                        if (distP30P33 > 0 && distP30P33 < etkiDaire)
                            MoveAttr(notAtt, p33);
                    }
                }

                tr.Commit();
            }
        }

        // ====================================================================================
        // ФУНКЦИЯ: MoveAttrTo (публичная обёртка над MoveAttr для прямого позиционирования)
        // ====================================================================================
        // НАЗНАЧЕНИЕ: Принудительно ставит указанный атрибут (по тегу) блока в заданную точку.
        // Используется командами TDD1N/TDD2N/TDD3N, которые (в отличие от RepositionShapeText)
        // не проверяют "радиус притяжения" - переставляют текст безусловно по жёсткой схеме.
        public static void MoveAttrTo(ObjectId blockId, string tag, Point3d newPosition)
        {
            if (blockId.IsNull) return;
            using (Transaction tr = blockId.Database.TransactionManager.StartTransaction())
            {
                BlockReference blkRef = tr.GetObject(blockId, OpenMode.ForWrite) as BlockReference;
                if (blkRef == null) { tr.Commit(); return; }

                AttributeReference attr = FindAttr(tr, blkRef, tag);
                if (attr != null) MoveAttr(attr, newPosition);

                tr.Commit();
            }
        }

        // Публичная обёртка для polar() - используется командами TDD1N/TDD2N/TDD3N
        public static Point3d PolarPoint(Point3d basePt, double angle, double distance) => Polar(basePt, angle, distance);

        // Публичный доступ к базовой геометрии блока (аналог entget: assoc 10/50/41)
        public static (Point3d Position, double Rotation, double ScaleX) GetBlockGeometry(ObjectId blockId)
        {
            using (Transaction tr = blockId.Database.TransactionManager.StartTransaction())
            {
                BlockReference blkRef = tr.GetObject(blockId, OpenMode.ForRead) as BlockReference;
                var result = blkRef != null
                    ? (blkRef.Position, blkRef.Rotation, blkRef.ScaleFactors.X)
                    : (Point3d.Origin, 0.0, 1.0);
                tr.Commit();
                return result;
            }
        }

        private static AttributeReference FindAttr(Transaction tr, BlockReference blkRef, string tag)
        {
            foreach (ObjectId attId in blkRef.AttributeCollection)
            {
                AttributeReference attRef = tr.GetObject(attId, OpenMode.ForWrite) as AttributeReference;
                if (attRef != null && string.Equals(attRef.Tag, tag, StringComparison.OrdinalIgnoreCase))
                    return attRef;
            }
            return null;
        }

        // Аналог (vla-move attrib old new) - сдвигает Position и AlignmentPoint на одну и ту же дельту
        private static void MoveAttr(AttributeReference attr, Point3d newPosition)
        {
            Vector3d delta = newPosition - attr.Position;
            attr.Position = attr.Position + delta;
            attr.AlignmentPoint = attr.AlignmentPoint + delta;
        }

        private static Point3d Polar(Point3d basePt, double angle, double distance)
        {
            return new Point3d(
                basePt.X + distance * Math.Cos(angle),
                basePt.Y + distance * Math.Sin(angle),
                basePt.Z);
        }

        private static double AngleTo(Point3d from, Point3d to)
        {
            return Math.Atan2(to.Y - from.Y, to.X - from.X);
        }

        private static bool AnglesEqual(double a, double b, double tolerance)
        {
            return Math.Abs(a - b) < tolerance;
        }
    }
}
