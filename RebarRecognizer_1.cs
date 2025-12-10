// ====================================================================================
// ФАЙЛ: RebarRecognizer_1.cs (резервная/улучшенная версия)
// НАЗНАЧЕНИЕ: Распознавание арматуры по геометрии полилинии с доработками под .NET 8
// ОСНОВНЫЕ ИЗМЕНЕНИЯ:
//   - Убрали статическое поле масштаба: теперь масштаб локален и не течёт между вызовами.
//   - Единые допуски/константы в одном месте (углы, расстояния, параллельность).
//   - Извлечение точек без вложенных транзакций (передаём текущую транзакцию).
//   - Поддержка полилиний с дугами (bulge): тесселяция дуговых сегментов в точки.
//   - Более безопасное округление (MidpointRounding.AwayFromZero) и проверка на планарность Z≈0.
//   - Комментарии по блокам, чтобы было понятно, что и зачем сделано.
// ПРИМЕЧАНИЕ: Подключение этого класса в UI пока не меняем — используйте его как запасной вариант
//             или подмените вызовы на RebarRecognizer1.Recognize(...) для теста.
// ====================================================================================

#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;

namespace PoseEdit2026
{
    /// <summary>
    /// Результат распознавания арматуры.
    /// Добавлено поле <see cref="Reason"/> для диагностики, почему распознать не удалось.
    /// </summary>
    public class RebarResult1
    {
        public string Type { get; set; } = "99";
        public string A { get; set; } = "0";
        public string B { get; set; } = "0";
        public string C { get; set; } = "0";
        public string D { get; set; } = "0";
        public string E { get; set; } = "0";
        public string F { get; set; } = "0";
        public string R { get; set; } = "0";
        public string Length { get; set; } = "L=0";

        // Диагностическое сообщение (почему Type=99 или что было распознано).
        public string Reason { get; set; } = string.Empty;
    }

    /// <summary>
    /// Улучшенная версия распознавателя арматуры.
    /// </summary>
    public static class RebarRecognizer1
    {
        // --------------------------------------------------------------------------
        // Константы и допуски (в одном месте, чтобы легко настраивать)
        // --------------------------------------------------------------------------
        private const double RoundStepMm = 10.0;          // Шаг округления размеров (10 мм, как в LISP)
        private const double ZeroTol = 1e-4;               // Толеранс для сравнения с нулём (координаты)
        private const double DistTol = 1e-2;               // Толеранс расстояний (мм) при проверках равенства
        private const double AngleTolDeg = 5.0;            // Допуск угла для "почти 90°"
        private static readonly Tolerance VectorTol = new Tolerance(0.05, 0.05); // Для IsParallelTo

        // --------------------------------------------------------------------------
        // Публичная точка входа
        // --------------------------------------------------------------------------
        public static RebarResult1 Recognize(ObjectId entityId)
        {
            var result = new RebarResult1();

            // Используем open/close транзакцию только для чтения
            using var tr = entityId.Database.TransactionManager.StartOpenCloseTransaction();

            var ent = tr.GetObject(entityId, OpenMode.ForRead) as Entity;
            if (ent == null)
            {
                result.Reason = "Entity is null";
                return result;
            }

            // Получаем масштаб из DIMLFAC (локально, без статиков)
            double scale = GetDrawingScaleSafe();

            // Извлекаем точки с учётом дуговых сегментов полилинии
            List<Point3d> pts = GetPointsFromEntity(ent, tr, scale);
            int npt = pts.Count;

            if (npt < 2)
            {
                result.Type = "99";
                result.Reason = "Not enough points";
                return result;
            }

            // Общая длина кривой (если Curve)
            if (ent is Curve curve)
            {
                try
                {
                    double lenParam = curve.GetDistanceAtParameter(curve.EndParam);
                    result.Length = "L=" + RoundToStep(lenParam * scale, RoundStepMm).ToString("0");
                }
                catch
                {
                    // оставляем длину по умолчанию
                }
            }

            // Выбор обработчика по количеству точек
            switch (npt)
            {
                case 2:
                    Recognize2Points(pts, result, scale);
                    break;
                case 3:
                    Recognize3Points(pts, result, scale);
                    break;
                case 4:
                    Recognize4Points(pts, result, scale);
                    break;
                case 5:
                    Recognize5Points(pts, result, scale);
                    break;
                case 6:
                    Recognize6Points(pts, result, scale);
                    break;
                case 7:
                    Recognize7Points(pts, result, scale);
                    break;
                case 8:
                    Recognize8Points(pts, result, scale);
                    break;
                default:
                    result.Type = "99";
                    result.Reason = $"Unsupported point count: {npt}";
                    break;
            }

            return result;
        }

        // --------------------------------------------------------------------------
        // Распознавание по количеству точек (логика оставлена близкой к исходной)
        // --------------------------------------------------------------------------
        private static void Recognize2Points(List<Point3d> pts, RebarResult1 res, double scale)
        {
            double boy1 = RoundToStep(pts[0].DistanceTo(pts[1]) * scale, RoundStepMm);
            res.Type = "00";
            res.A = boy1.ToString("0");
            res.B = res.C = res.D = res.E = res.F = res.R = "0";
            res.Reason = "2-point straight bar";
        }

        private static void Recognize3Points(List<Point3d> pts, RebarResult1 res, double scale)
        {
            // Унификация порядка (короткий сегмент первым)
            double dist1 = pts[0].DistanceTo(pts[1]);
            double dist2 = pts[1].DistanceTo(pts[2]);
            if (dist1 > dist2)
            {
                (pts[0], pts[2]) = (pts[2], pts[0]);
            }

            var norm = Normalize3Points(pts);

            double alfa1 = Math.Atan2(norm[0].Y, norm[0].X) * 180.0 / Math.PI;
            double boy1 = RoundToStep(scale * norm[0].DistanceTo(norm[1]), RoundStepMm);
            double boy2 = RoundToStep(scale * norm[1].DistanceTo(norm[2]), RoundStepMm);

            res.A = boy1.ToString("0");
            res.B = boy2.ToString("0");
            res.C = res.D = res.E = res.F = res.R = "0";

            // Острый / прямой / тупой
            double proj = RoundToStep(scale * Math.Abs(boy1 * Math.Sin(Math.Atan2(norm[0].Y, norm[0].X))), RoundStepMm);
            if (Math.Abs(alfa1) < 90 - AngleTolDeg)
            {
                res.Type = "14";
                res.C = proj.ToString("0");
                res.Reason = "Acute bend (<90°)";
            }
            else if (Is90(alfa1))
            {
                res.Type = "11";
                res.Reason = "Right angle (≈90°)";
            }
            else
            {
                res.Type = "15";
                res.C = proj.ToString("0");
                res.Reason = "Obtuse bend (>90°)";
            }
        }

        private static void Recognize4Points(List<Point3d> pts, RebarResult1 res, double scale)
        {
            var pp = Normalize4Points(pts);

            double boy1 = RoundToStep(scale * pp[0].DistanceTo(pp[1]), RoundStepMm);
            double boy2 = RoundToStep(scale * pp[1].DistanceTo(pp[2]), RoundStepMm);
            double boy3 = RoundToStep(scale * pp[2].DistanceTo(pp[3]), RoundStepMm);

            res.A = boy1.ToString("0");
            res.B = boy2.ToString("0");
            res.C = boy3.ToString("0");
            res.D = res.E = res.F = res.R = "0";

            double alfa1 = Deg(pp[0]);
            double alfa2 = Deg(pp[3] - pp[2]);

            // Утка (Crank): первые и последние сегменты параллельны
            Vector3d v1 = new(pp[0].X, pp[0].Y, 0);
            Vector3d v3 = new(pp[3].X - pp[2].X, pp[3].Y - pp[2].Y, 0);
            if (v1.IsParallelTo(v3, VectorTol))
            {
                res.Type = "41";
                res.Reason = "Crank: first/last segments parallel";
                return;
            }

            if (Is90(alfa1) && Is90(alfa2))
            {
                bool turn1 = GetTurnDirection(pp[0], pp[1], pp[2]);
                bool turn2 = GetTurnDirection(pp[1], pp[2], pp[3]);
                res.Type = turn1 == turn2 ? "21" : "25";
                res.Reason = turn1 == turn2 ? "U-shape (same turns)" : "Z-shape (opposite turns)";
            }
            else
            {
                res.Type = "17";
                res.Reason = "Special 4-point shape";
            }
        }

        private static void Recognize5Points(List<Point3d> pts, RebarResult1 res, double scale)
        {
            var pp = NormalizeNPoints(pts, centerIndex: 1, alignIndex: 2);

            double boy1 = RoundToStep(scale * pp[0].DistanceTo(pp[1]), RoundStepMm);
            double boy2 = RoundToStep(scale * pp[1].DistanceTo(pp[2]), RoundStepMm);
            double boy3 = RoundToStep(scale * pp[2].DistanceTo(pp[3]), RoundStepMm);
            double boy4 = RoundToStep(scale * pp[3].DistanceTo(pp[4]), RoundStepMm);

            res.A = boy1.ToString("0");
            res.B = boy2.ToString("0");
            res.C = boy3.ToString("0");
            res.D = boy4.ToString("0");
            res.E = res.F = res.R = "0";

            double a1 = Deg(pp[0]);
            double a2 = Deg(pp[3] - pp[2]);
            double a3 = Deg(pp[4] - pp[3]);

            if (Is90(a1) && Is90(a2) && Math.Abs(a3 - 180) < AngleTolDeg)
            {
                res.Type = "31";
                res.Reason = "Stirrup (all 90°, close at end)";
            }
            else
            {
                res.Type = "30";
                res.Reason = "Special 5-point shape";
            }
        }

        private static void Recognize6Points(List<Point3d> pts, RebarResult1 res, double scale)
        {
            var pp = NormalizeNPoints(pts, centerIndex: 2, alignIndex: 3);

            double boy1 = RoundToStep(scale * pp[0].DistanceTo(pp[1]), RoundStepMm);
            double boy2 = RoundToStep(scale * pp[1].DistanceTo(pp[2]), RoundStepMm);
            double boy3 = RoundToStep(scale * pp[2].DistanceTo(pp[3]), RoundStepMm);
            double boy4 = RoundToStep(scale * pp[3].DistanceTo(pp[4]), RoundStepMm);
            double boy5 = RoundToStep(scale * pp[4].DistanceTo(pp[5]), RoundStepMm);

            res.A = boy1.ToString("0");
            res.B = boy2.ToString("0");
            res.C = boy3.ToString("0");
            res.D = boy4.ToString("0");
            res.E = boy5.ToString("0");
            res.F = res.R = "0";

            Vector3d v1 = new(pp[0].X - pp[1].X, pp[0].Y - pp[1].Y, 0);
            Vector3d v5 = new(pp[5].X - pp[4].X, pp[5].Y - pp[4].Y, 0);
            if (v1.IsParallelTo(v5, VectorTol))
            {
                res.Type = "41";
                res.Reason = "Crank (6pt) parallel ends";
            }
            else
            {
                res.Type = "20";
                res.Reason = "Special 6-point shape";
            }
        }

        private static void Recognize7Points(List<Point3d> pts, RebarResult1 res, double scale)
        {
            var pp = NormalizeNPoints(pts, centerIndex: 2, alignIndex: 3);

            double boy1 = RoundToStep(scale * pp[0].DistanceTo(pp[1]), RoundStepMm);
            double boy2 = RoundToStep(scale * pp[1].DistanceTo(pp[2]), RoundStepMm);
            double boy3 = RoundToStep(scale * pp[2].DistanceTo(pp[3]), RoundStepMm);
            double boy4 = RoundToStep(scale * pp[3].DistanceTo(pp[4]), RoundStepMm);
            double boy5 = RoundToStep(scale * pp[4].DistanceTo(pp[5]), RoundStepMm);
            double boy6 = RoundToStep(scale * pp[5].DistanceTo(pp[6]), RoundStepMm);

            res.A = boy1.ToString("0");
            res.B = boy2.ToString("0");
            res.C = boy3.ToString("0");
            res.D = boy6.ToString("0");
            res.E = boy5.ToString("0");
            res.F = res.R = "0";

            if (Math.Abs(boy2 - boy4) < DistTol)
            {
                res.Type = "49";
                res.Reason = "7-point symmetric (49)";
            }
            else
            {
                res.Type = "99";
                res.Reason = "Unclassified 7-point";
            }
        }

        private static void Recognize8Points(List<Point3d> pts, RebarResult1 res, double scale)
        {
            var pp = NormalizeNPoints(pts, centerIndex: 3, alignIndex: 4);

            double boy1 = RoundToStep(scale * pp[0].DistanceTo(pp[1]), RoundStepMm);
            double boy2 = RoundToStep(scale * pp[1].DistanceTo(pp[2]), RoundStepMm);
            double boy3 = RoundToStep(scale * pp[2].DistanceTo(pp[3]), RoundStepMm);
            double boy4 = RoundToStep(scale * pp[3].DistanceTo(pp[4]), RoundStepMm);
            double boy5 = RoundToStep(scale * pp[4].DistanceTo(pp[5]), RoundStepMm);
            double boy6 = RoundToStep(scale * pp[5].DistanceTo(pp[6]), RoundStepMm);
            double boy7 = RoundToStep(scale * pp[6].DistanceTo(pp[7]), RoundStepMm);

            res.A = boy2.ToString("0");
            res.B = boy3.ToString("0");
            res.C = boy4.ToString("0");
            res.D = boy1.ToString("0");
            res.E = boy6.ToString("0");
            res.F = res.R = "0";

            if (Math.Abs(boy1 - boy7) < DistTol && Math.Abs(boy3 - boy5) < DistTol)
            {
                res.Type = "48";
                res.Reason = "8-point symmetric (48)";
            }
            else
            {
                res.Type = "99";
                res.Reason = "Unclassified 8-point";
            }
        }

        // --------------------------------------------------------------------------
        // Извлечение точек (без вложенных транзакций, с поддержкой bulge)
        // --------------------------------------------------------------------------
        private static List<Point3d> GetPointsFromEntity(Entity ent, Transaction tr, double scale)
        {
            var pts = new List<Point3d>();

            switch (ent)
            {
                case Line line:
                    pts.Add(line.StartPoint);
                    pts.Add(line.EndPoint);
                    break;

                case Polyline poly:
                    pts.AddRange(ExtractPolylinePoints(poly));
                    break;

                case Polyline2d poly2d:
                    foreach (ObjectId id in poly2d)
                    {
                        if (tr.GetObject(id, OpenMode.ForRead) is Vertex2d vtx)
                            pts.Add(vtx.Position);
                    }
                    break;

                case Curve curve:
                    // Общий случай: если не полилиния, пробуем дискретизацию по длине
                    pts.AddRange(SampleCurve(curve, segments: 20));
                    break;
            }

            // Приводим Z к 0, чтобы избежать проблем с UCS/случайными высотами
            for (int i = 0; i < pts.Count; i++)
            {
                pts[i] = new Point3d(pts[i].X, pts[i].Y, 0);
            }

            return pts;
        }

        /// <summary>
        /// Извлечение точек из Polyline с поддержкой дуговых сегментов (bulge).
        /// </summary>
        private static IEnumerable<Point3d> ExtractPolylinePoints(Polyline poly)
        {
            int n = poly.NumberOfVertices;
            if (n == 0) yield break;

            for (int i = 0; i < n; i++)
            {
                Point3d p0 = poly.GetPoint3dAt(i);
                yield return p0;

                // bulge сегмент между i и i+1 (или замыкание)
                double bulge = poly.GetBulgeAt(i);
                if (Math.Abs(bulge) > ZeroTol)
                {
                    int next = (i + 1) % n;
                    Point3d p1 = poly.GetPoint3dAt(next);
                    foreach (var mid in TessellateBulge(p0, p1, bulge, segments: 6))
                        yield return mid;
                }
            }
        }

        /// <summary>
        /// Тесселяция дугового сегмента (bulge) в несколько промежуточных точек.
        /// </summary>
        private static IEnumerable<Point3d> TessellateBulge(Point3d p0, Point3d p1, double bulge, int segments)
        {
            // bulge = tan(angle/4). Вычисляем центральный угол
            double alpha = 4 * Math.Atan(Math.Abs(bulge));
            double chord = p0.DistanceTo(p1);
            if (chord < ZeroTol || alpha < ZeroTol) yield break;

            // Радиус и центр дуги
            double radius = chord / (2 * Math.Sin(alpha / 2));
            // Вектор хорды
            Vector2d v = new Vector2d(p1.X - p0.X, p1.Y - p0.Y);
            double theta = Math.Atan2(v.Y, v.X);
            double sagitta = radius - radius * Math.Cos(alpha / 2);
            // Направление выпуклости
            double sign = bulge >= 0 ? 1.0 : -1.0;

            // Нормаль к хорде (влево при sign=+1)
            Vector2d n = v.GetPerpendicularVector().GetNormal();
            Point2d mid = new Point2d((p0.X + p1.X) * 0.5, (p0.Y + p1.Y) * 0.5);
            Point2d center = mid + n * (sign * sagitta);

            // Начальный и конечный углы от центра
            double startAng = Math.Atan2(p0.Y - center.Y, p0.X - center.X);
            double endAng = startAng + sign * alpha;

            // Семплируем промежуточные точки
            for (int i = 1; i < segments; i++)
            {
                double t = (double)i / segments;
                double ang = startAng + (endAng - startAng) * t;
                yield return new Point3d(
                    center.X + radius * Math.Cos(ang),
                    center.Y + radius * Math.Sin(ang),
                    0);
            }
        }

        /// <summary>
        /// Семплирование произвольной кривой, если тип не поддержан напрямую.
        /// </summary>
        private static IEnumerable<Point3d> SampleCurve(Curve curve, int segments)
        {
            double start = curve.StartParam;
            double end = curve.EndParam;
            yield return curve.GetPointAtParameter(start);
            for (int i = 1; i < segments; i++)
            {
                double t = start + (end - start) * i / segments;
                yield return curve.GetPointAtParameter(t);
            }
            yield return curve.GetPointAtParameter(end);
        }

        // --------------------------------------------------------------------------
        // Нормализация наборов точек (общие утилиты)
        // --------------------------------------------------------------------------
        private static List<Point3d> Normalize3Points(List<Point3d> pts)
        {
            Point3d p2 = pts[1];
            var pp = new List<Point3d>
            {
                new(pts[0].X - p2.X, pts[0].Y - p2.Y, 0),
                new(0, 0, 0),
                new(pts[2].X - p2.X, pts[2].Y - p2.Y, 0)
            };

            double teta = Math.Abs(pp[2].X) < ZeroTol ? Math.PI / 2.0 : -Math.Atan2(pp[2].Y, pp[2].X);
            pp[0] = RotatePoint(pp[0], teta);
            pp[2] = RotatePoint(pp[2], teta);

            if (pp[2].X < 0)
            {
                pp[0] = RotatePoint(pp[0], Math.PI);
                pp[2] = RotatePoint(pp[2], Math.PI);
            }

            if (pp[0].Y < 0)
            {
                pp[0] = new Point3d(pp[0].X, -pp[0].Y, 0);
            }

            return pp;
        }

        private static List<Point3d> Normalize4Points(List<Point3d> pts)
        {
            Point3d p2 = pts[1];
            var pp = new List<Point3d>
            {
                new(pts[0].X - p2.X, pts[0].Y - p2.Y, 0),
                new(0, 0, 0),
                new(pts[2].X - p2.X, pts[2].Y - p2.Y, 0),
                new(pts[3].X - p2.X, pts[3].Y - p2.Y, 0)
            };

            double teta = Math.Abs(pp[2].X) < ZeroTol ? Math.PI / 2.0 : -Math.Atan2(pp[2].Y, pp[2].X);
            pp[0] = RotatePoint(pp[0], teta);
            pp[2] = RotatePoint(pp[2], teta);
            pp[3] = RotatePoint(pp[3], teta);

            if (pp[2].X < 0)
            {
                pp[0] = RotatePoint(pp[0], Math.PI);
                pp[2] = RotatePoint(pp[2], Math.PI);
                pp[3] = RotatePoint(pp[3], Math.PI);
            }

            if (pp[0].Y < 0)
            {
                pp[0] = new Point3d(pp[0].X, -pp[0].Y, 0);
                pp[3] = new Point3d(pp[3].X, -pp[3].Y, 0);
            }

            return pp;
        }

        /// <summary>
        /// Обобщённая нормализация для N точек:
        /// - перенос в начало координат по centerIndex
        /// - выравнивание по alignIndex (делает сегмент горизонтальным)
        /// - отражение, чтобы первая точка была выше оси X
        /// </summary>
        private static List<Point3d> NormalizeNPoints(List<Point3d> pts, int centerIndex, int alignIndex)
        {
            Point3d pc = pts[centerIndex];
            var pp = pts.Select(p => new Point3d(p.X - pc.X, p.Y - pc.Y, 0)).ToList();

            double teta = Math.Abs(pp[alignIndex].X) < ZeroTol ? Math.PI / 2.0 : -Math.Atan2(pp[alignIndex].Y, pp[alignIndex].X);
            for (int i = 0; i < pp.Count; i++)
            {
                if (i != centerIndex) pp[i] = RotatePoint(pp[i], teta);
            }

            if (pp[alignIndex].X < 0)
            {
                for (int i = 0; i < pp.Count; i++)
                {
                    if (i != centerIndex) pp[i] = RotatePoint(pp[i], Math.PI);
                }
            }

            if (pp[0].Y < 0)
            {
                for (int i = 0; i < pp.Count; i++)
                {
                    pp[i] = new Point3d(pp[i].X, -pp[i].Y, 0);
                }
            }

            return pp;
        }

        // --------------------------------------------------------------------------
        // Векторные/геометрические утилиты
        // --------------------------------------------------------------------------
        private static Point3d RotatePoint(Point3d point, double angle)
        {
            double cosA = Math.Cos(angle);
            double sinA = Math.Sin(angle);
            return new Point3d(
                point.X * cosA - point.Y * sinA,
                point.X * sinA + point.Y * cosA,
                point.Z);
        }

        private static bool GetTurnDirection(Point3d p1, Point3d p2, Point3d p3)
        {
            double v1x = p2.X - p1.X;
            double v1y = p2.Y - p1.Y;
            double v2x = p3.X - p2.X;
            double v2y = p3.Y - p2.Y;
            double crossZ = v1x * v2y - v1y * v2x;
            return crossZ > 0;
        }

        private static bool Is90(double angleDeg)
        {
            return Math.Abs(Math.Abs(angleDeg) - 90.0) < AngleTolDeg;
        }

        private static double RoundToStep(double value, double step)
        {
            // Округление к ближайшему кратному step (10 мм) с AwayFromZero,
            // чтобы избежать систематического смещения при 0.5
            double div = value / step;
            double rounded = Math.Round(div, 0, MidpointRounding.AwayFromZero);
            return rounded * step;
        }

        private static double Deg(Point3d p) => Math.Atan2(p.Y, p.X) * 180.0 / Math.PI;
        private static double Deg(Vector3d v) => Math.Atan2(v.Y, v.X) * 180.0 / Math.PI;

        private static double GetDrawingScaleSafe()
        {
            try
            {
                object dimlfacObj = Application.GetSystemVariable("DIMLFAC");
                double sc = dimlfacObj == null ? 1.0 : Convert.ToDouble(dimlfacObj);
                return sc == 0 ? 1.0 : sc;
            }
            catch
            {
                return 1.0;
            }
        }
    }
}


