using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;

namespace AutoCAD2024Final
{
    public class RebarResult
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
    }

    public static class RebarRecognizer
    {
        private const double SAYI = 10.0; // Округление
        private static double _scale = 1.0;

        public static RebarResult Recognize(ObjectId entityId)
        {
            RebarResult result = new RebarResult();

            using (Transaction tr = entityId.Database.TransactionManager.StartTransaction())
            {
                Curve curve = tr.GetObject(entityId, OpenMode.ForRead) as Curve;
                if (curve == null) return result;

                _scale = curve.Database.Dimlfac;
                if (_scale == 0) _scale = 1.0;

                // 1. Получаем точки
                List<Point3d> pts = GetPointsFromCurve(curve);
                int npt = pts.Count;

                // 2. Общая длина
                double lenParam = curve.GetDistanceAtParameter(curve.EndParam);
                result.Length = "L=" + Round(lenParam * _scale).ToString("0");

                // 3. Анализ по количеству сегментов
                switch (npt)
                {
                    case 2: // 1 сегмент (Прямая)
                        result.Type = "00";
                        result.A = Round(pts[0].DistanceTo(pts[1]) * _scale).ToString("0");
                        break;

                    case 3: // 2 сегмента (Г-образные)
                        Analyze3Points(pts, result);
                        break;

                    case 4: // 3 сегмента (П, Z, Утки) - Твои красные "Crank" здесь
                        Analyze4Points(pts, result);
                        break;

                    case 5: // 4 сегмента (Хомуты, Стульчики) - Твои красные "Chair" здесь
                        Analyze5Points(pts, result);
                        break;

                    case 6: // 5 сегментов (Шляпы/Омега) - Твоя красная "Шляпа" здесь
                        Analyze6Points(pts, result);
                        break;

                    default:
                        result.Type = "99";
                        break;
                }
                tr.Commit();
            }
            return result;
        }

        // ====================================================================================
        // ЛОГИКА: 3 ТОЧКИ (Г-ОБРАЗНЫЕ)
        // ====================================================================================
        private static void Analyze3Points(List<Point3d> pts, RebarResult res)
        {
            // Векторы
            Vector3d v1 = pts[1] - pts[0];
            Vector3d v2 = pts[2] - pts[1];

            // Угол между векторами (0..180)
            double angle = v1.GetAngleTo(v2) * (180.0 / Math.PI);

            double len1 = Round(v1.Length * _scale);
            double len2 = Round(v2.Length * _scale);

            res.A = len1.ToString("0");
            res.B = len2.ToString("0");

            // Логика
            if (Is90(angle))
            {
                res.Type = "11"; // Г-шка 90
            }
            else if (angle > 90) // Тупой угол (выпрямленная)
            {
                // Стандарт BS 8666 для тупого угла - это Тип 15
                res.Type = "15";
                // Для типа 15 часто нужно указывать не саму длину, а проекции,
                // но пока оставим фактические длины A и B.
            }
            else // Острый угол (<90)
            {
                res.Type = "12"; // Или 13
            }
        }

        // ====================================================================================
        // ЛОГИКА: 4 ТОЧКИ (П, Z, УТКИ) - ВАЖНО ДЛЯ ТЕБЯ
        // ====================================================================================
        private static void Analyze4Points(List<Point3d> pts, RebarResult res)
        {
            Vector3d v1 = pts[1] - pts[0]; // Сегмент A
            Vector3d v2 = pts[2] - pts[1]; // Сегмент B (наклонный)
            Vector3d v3 = pts[3] - pts[2]; // Сегмент C

            double a = Round(v1.Length * _scale);
            double b = Round(v2.Length * _scale);
            double c = Round(v3.Length * _scale);

            res.A = a.ToString("0");
            res.B = b.ToString("0");
            res.C = c.ToString("0");

            // Углы поворота
            double angle1 = v1.GetAngleTo(v2) * (180.0 / Math.PI);
            double angle2 = v2.GetAngleTo(v3) * (180.0 / Math.PI);

            // Проверка на параллельность A и C (Это ключевой признак УТКИ / CRANK)
            bool isParallel = v1.IsParallelTo(v3, new Tolerance(0.05, 0.05)); // Допуск

            // 1. УТКА (CRANK) - Тип 41
            // Если первый и последний сегмент параллельны (или почти параллельны)
            if (isParallel)
            {
                res.Type = "41"; // Crank
                return;
            }

            // 2. П-ОБРАЗНАЯ (Тип 21)
            // Углы около 90, повороты в одну сторону
            bool turn1 = GetTurnDirection(pts[0], pts[1], pts[2]);
            bool turn2 = GetTurnDirection(pts[1], pts[2], pts[3]);

            if (Is90(angle1) && Is90(angle2))
            {
                if (turn1 == turn2) res.Type = "21"; // П-шка (повороты в одну сторону)
                else res.Type = "25";                // Z-ка (повороты в разные стороны)
            }
            // 3. Другие случаи (например, тупые углы, как на твоем скрине слева внизу)
            else
            {
                // Если это выглядит как Z (разные повороты), но углы не 90 -> Тип 18 или спец Crank
                if (turn1 != turn2) res.Type = "41"; // Всё равно считаем Уткой
                else res.Type = "99";
            }
        }

        // ====================================================================================
        // ЛОГИКА: 5 ТОЧЕК (СТУЛЬЧИКИ, ХОМУТЫ)
        // ====================================================================================
        private static void Analyze5Points(List<Point3d> pts, RebarResult res)
        {
            Vector3d v1 = pts[1] - pts[0];
            Vector3d v2 = pts[2] - pts[1];
            Vector3d v3 = pts[3] - pts[2];
            Vector3d v4 = pts[4] - pts[3];

            res.A = Round(v1.Length * _scale).ToString("0");
            res.B = Round(v2.Length * _scale).ToString("0");
            res.C = Round(v3.Length * _scale).ToString("0");
            res.D = Round(v4.Length * _scale).ToString("0");

            // Направления поворотов
            bool t1 = GetTurnDirection(pts[0], pts[1], pts[2]);
            bool t2 = GetTurnDirection(pts[1], pts[2], pts[3]);
            bool t3 = GetTurnDirection(pts[2], pts[3], pts[4]);

            // 1. ХОМУТ (Квадрат) - Тип 31
            // Все повороты в одну сторону (t1==t2==t3)
            if (t1 == t2 && t2 == t3)
            {
                res.Type = "31";
                return;
            }

            // 2. СТУЛЬЧИК (Chair) - Тип 38
            // Форма: Нога -> Высота -> Верх -> Высота
            // Повороты: Лево -> Право -> Право (или наоборот: Л, Л, П)
            // На скрине у тебя: L-shape вверх, потом L-shape вниз.
            // Это значит: поворот1 != поворот2, а поворот2 == поворот3
            // Или: поворот1 == поворот2, а поворот2 != поворот3

            if (t1 != t2 || t2 != t3)
            {
                res.Type = "38"; // Chair
                return;
            }

            res.Type = "99";
        }

        // ====================================================================================
        // ЛОГИКА: 6 ТОЧЕК (ШЛЯПА / OMEGA)
        // ====================================================================================
        private static void Analyze6Points(List<Point3d> pts, RebarResult res)
        {
            Vector3d v1 = pts[1] - pts[0];
            Vector3d v2 = pts[2] - pts[1];
            Vector3d v3 = pts[3] - pts[2];
            Vector3d v4 = pts[4] - pts[3];
            Vector3d v5 = pts[5] - pts[4];

            res.A = Round(v1.Length * _scale).ToString("0");
            res.B = Round(v2.Length * _scale).ToString("0");
            res.C = Round(v3.Length * _scale).ToString("0");
            res.D = Round(v4.Length * _scale).ToString("0");
            res.E = Round(v5.Length * _scale).ToString("0");

            // Проверяем "Шляпу" (Тип 26 или спец)
            // Схема: Вправо -> Вверх -> Вправо -> Вниз -> Вправо
            // Повороты: Лево -> Право -> Право -> Лево

            bool t1 = GetTurnDirection(pts[0], pts[1], pts[2]);
            bool t2 = GetTurnDirection(pts[1], pts[2], pts[3]);
            bool t3 = GetTurnDirection(pts[2], pts[3], pts[4]);
            bool t4 = GetTurnDirection(pts[3], pts[4], pts[5]);

            // Если "ноги" (A и E) параллельны (лежат на одной прямой или просто параллельны)
            if (v1.IsParallelTo(v5, new Tolerance(0.05, 0.05)))
            {
                res.Type = "26"; // Это код для П-шки с ногами наружу в некоторых стандартах
                // Или можно вернуть "Hat" если у тебя есть такой код
                return;
            }

            res.Type = "99";
        }

        // =================================================================================
        // ВСПОМОГАТЕЛЬНЫЕ МЕТОДЫ (Векторная алгебра вместо Лисповского вращения)
        // =================================================================================

        private static List<Point3d> GetPointsFromCurve(Curve curve)
        {
            List<Point3d> pts = new List<Point3d>();
            if (curve is Line line) { pts.Add(line.StartPoint); pts.Add(line.EndPoint); }
            else if (curve is Polyline poly)
            {
                for (int i = 0; i < poly.NumberOfVertices; i++) pts.Add(poly.GetPoint3dAt(i));
            }
            return pts;
        }

        // Округление до 10 мм (или 5)
        private static double Round(double value) => Math.Floor(value / SAYI + 0.5) * SAYI;

        // Проверка на 90 градусов (с допуском 5 градусов, т.к. рисуют криво)
        private static bool Is90(double angleDeg) => Math.Abs(angleDeg - 90.0) < 5.0;

        // Определение направления поворота (Влево/Вправо)
        // Используем Z-компоненту векторного произведения (Cross Product)
        private static bool GetTurnDirection(Point3d p1, Point3d p2, Point3d p3)
        {
            // Вектор 1 (входящий)
            double v1x = p2.X - p1.X;
            double v1y = p2.Y - p1.Y;

            // Вектор 2 (исходящий)
            double v2x = p3.X - p2.X;
            double v2y = p3.Y - p2.Y;

            // Z = x1*y2 - x2*y1
            double crossZ = v1x * v2y - v1y * v2x;

            return crossZ > 0; // True = Лево, False = Право
        }
    }
}