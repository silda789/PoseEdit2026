// ====================================================================================
// ФАЙЛ: BeamDrawer.cs
// НАЗНАЧЕНИЕ: черчение "скелета" балки в AutoCAD - только геометрия, которую можно
//             однозначно вычислить из уже собранных данных (длины пролётов, сечения,
//             оси/опоры), БЕЗ арматуры (хомутов, крюков, анкеровки) - для неё ещё нет
//             методики (см. CLAUDE.md / память сессии - пользователь опишет её сам).
//
// ВАЖНО: это ПЕРВАЯ версия, ещё не проверенная в реальном AutoCAD (только компилятор
// подтвердил, что код синтаксически верный). Схематично, не в масштабе по высоте H -
// как и в старом Excel-инструменте (BEAMDRAW\Temp\screenshots), высота "ленты" балки
// на чертеже одна и та же для всех пролётов, реальные B x H просто подписаны текстом.
// ====================================================================================
#nullable disable
using System.Collections.Generic; // List<T> - список чисел (позиции осей).
using Autodesk.AutoCAD.DatabaseServices; // Transaction, BlockTableRecord, Line, Circle, Solid, DBText - "кирпичики" чертежа.
using Autodesk.AutoCAD.Geometry; // Point3d - точка в пространстве (X, Y, Z).

namespace BEAMDRAW
{
    public static class BeamDrawer
    {
        // "const" - число, которое никогда не меняется во время работы программы (в
        // отличие от обычной переменной). Все размеры тут - в МИЛЛИМЕТРАХ (мм), потому
        // что окно ввода данных собирает всё в МЕТРАХ (м) - см. BeamData.cs - а чертежи
        // в этом офисе обычно ведутся в мм (как и остальной PoseEdit2026), поэтому при
        // черчении переводим м -> мм умножением на 1000 (см. ниже, span.Length * 1000.0).

        // Высота "ленты" балки на чертеже - СХЕМАТИЧНАЯ, не в масштабе с реальным H
        // (в старом Excel-инструменте лента тоже была одной высоты для всех пролётов,
        // а настоящие B x H просто подписывались текстом снизу - см. скриншоты).
        private const double BandHeight = 400;

        // Ширина опоры (колонны/стены), которую рисуем на каждой оси.
        private const double SupportWidth = 300;

        // На сколько опора выступает выше и ниже ленты балки - просто чтобы было видно,
        // что это опора, а не часть самой балки.
        private const double SupportAboveBand = 200;
        private const double SupportBelowBand = 800;

        // Радиус кружка с номером оси, и зазор между низом опоры и этим кружком.
        private const double AxisBubbleRadius = 150;
        private const double AxisLeaderGap = 300;

        // Зазор между низом опоры и линией размера (расстояния пролёта), высота текста,
        // и длина маленьких вертикальных "усиков" на концах линии размера.
        private const double DimLineGap = 900;
        private const double TextHeight = 150;
        private const double DimTickLength = 100;

        // ================================================================================
        // ГЛАВНЫЙ МЕТОД: чертит балку целиком.
        // ================================================================================
        // db - база чертежа, куда добавляем объекты (линии, окружности и т.д.).
        // beam - данные балки (из окна BeamDrawWindow).
        // insertionPoint - точка, куда пользователь указал "начать чертить" (левый нижний
        // угол ленты балки, ось №1).
        public static void DrawSkeleton(Database db, Beam beam, Point3d insertionPoint)
        {
            // Transaction - "пакет изменений" базы чертежа: либо применяются ВСЕ
            // изменения сразу (Commit), либо ни одного (если что-то пошло не так и
            // Commit не вызван). Это стандартный способ менять чертёж AutoCAD из кода -
            // используется во всех командах PoseEdit2026 (см. Commands.cs, LegacyCommands.cs).
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                // BlockTable - "оглавление" всех блоков чертежа, ModelSpace - это то
                // самое основное пространство модели, где обычно и чертят.
                BlockTable bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);
                BlockTableRecord ms = (BlockTableRecord)tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite);

                double baseX = insertionPoint.X;
                double baseY = insertionPoint.Y;
                double bandBottom = baseY;
                double bandTop = baseY + BandHeight;

                // Считаем X-координаты всех осей (границ пролётов), в мм, от baseX.
                // axisX[0] = 0 (ось 1, самое начало балки), axisX[1] = длина 1-го
                // пролёта, axisX[2] = длина 1-го + 2-го, и т.д. - axisX[i] = позиция
                // ОСИ №(i+1). Если пролётов N, то осей всегда N+1.
                var axisX = new List<double> { 0 };
                foreach (var span in beam.Spans)
                    axisX.Add(axisX[axisX.Count - 1] + span.Length * 1000.0);

                double totalLength = axisX[axisX.Count - 1];

                // --- Верхняя и нижняя линии ленты балки, на всю длину сразу (лента
                // идёт непрерывно через опоры - опоры рисуются поверх неё) ---
                AddEntity(tr, ms, new Line(new Point3d(baseX, bandBottom, 0), new Point3d(baseX + totalLength, bandBottom, 0)));
                AddEntity(tr, ms, new Line(new Point3d(baseX, bandTop, 0), new Point3d(baseX + totalLength, bandTop, 0)));

                // --- Название балки, подписываем над лентой слева ---
                AddEntity(tr, ms, MakeText(beam.Name, new Point3d(baseX, bandTop + TextHeight, 0), TextHeight * 1.5));

                // --- Опоры и номера осей: один цикл по всем осям (их N+1 на N пролётов) ---
                for (int i = 0; i < axisX.Count; i++)
                {
                    double x = baseX + axisX[i];
                    DrawSupport(tr, ms, x, bandBottom, bandTop);
                    DrawAxisBubble(tr, ms, x, bandBottom - SupportBelowBand, i + 1);
                }

                // --- Размерная линия и подпись сечения B x H под каждым пролётом ---
                double dimY = bandBottom - DimLineGap;
                for (int i = 0; i < beam.Spans.Count; i++)
                {
                    double xStart = baseX + axisX[i];
                    double xEnd = baseX + axisX[i + 1];
                    DrawSpanDimension(tr, ms, xStart, xEnd, dimY, beam.Spans[i]);
                }

                // Commit() - "сохранить все изменения из этой транзакции в чертёж
                // насовсем". Без этого вызова все new Line/new Circle/... выше
                // останутся не более чем объектами в памяти - на самом чертеже ничего
                // не появится.
                tr.Commit();
            }
        }

        // Рисует одну опору (колонну/стену) как закрашенный прямоугольник (Solid -
        // "2D SOLID" в терминах AutoCAD, простейший способ сделать закрашенную
        // область без возни с штриховкой Hatch, которую сложнее настроить правильно
        // "вслепую", без проверки в реальном AutoCAD).
        //
        // ВАЖНО про порядок точек у Solid: это НЕ порядок "по кругу" (как рисовал бы
        // обычный прямоугольник), а порядок "зигзагом" - 1-я и 2-я точки образуют одну
        // сторону, 3-я и 4-я - противоположную. Перепутав порядок, получим не
        // прямоугольник, а перекрученную "бабочку".
        private static void DrawSupport(Transaction tr, BlockTableRecord ms, double axisX, double bandBottom, double bandTop)
        {
            double left = axisX - SupportWidth / 2.0;
            double right = axisX + SupportWidth / 2.0;
            double bottom = bandBottom - SupportBelowBand;
            double top = bandTop + SupportAboveBand;

            Solid support = new Solid(
                new Point3d(left, bottom, 0),   // 1: низ-лево
                new Point3d(right, bottom, 0),  // 2: низ-право
                new Point3d(left, top, 0),      // 3: верх-лево
                new Point3d(right, top, 0));    // 4: верх-право
            AddEntity(tr, ms, support);
        }

        // Рисует кружок с номером оси и короткую вертикальную "выноску" от опоры до кружка.
        private static void DrawAxisBubble(Transaction tr, BlockTableRecord ms, double axisX, double supportBottomY, int axisNumber)
        {
            double bubbleCenterY = supportBottomY - AxisLeaderGap - AxisBubbleRadius;

            // Выноска - просто вертикальная линия от низа опоры до верха кружка.
            AddEntity(tr, ms, new Line(
                new Point3d(axisX, supportBottomY, 0),
                new Point3d(axisX, bubbleCenterY + AxisBubbleRadius, 0)));

            AddEntity(tr, ms, new Circle(new Point3d(axisX, bubbleCenterY, 0), Vector3d.ZAxis, AxisBubbleRadius));

            // Номер оси по центру кружка. AttachmentPoint = MiddleCenter выравнивает
            // текст точно по центру Position (а не "от левого нижнего угла", как текст
            // ведёт себя по умолчанию) - иначе цифра оказалась бы смещена от центра кружка.
            DBText text = MakeText(axisNumber.ToString(), new Point3d(axisX, bubbleCenterY, 0), AxisBubbleRadius * 1.1);
            text.HorizontalMode = TextHorizontalMode.TextCenter;
            text.VerticalMode = TextVerticalMode.TextVerticalMid;
            text.AlignmentPoint = text.Position;
            AddEntity(tr, ms, text);
        }

        // Рисует размерную линию одного пролёта (с "усиками" на концах вместо стрелок -
        // не полагаемся на текущий размерный стиль DIMSTYLE чертежа пользователя,
        // который неизвестно как настроен) и подпись длины + сечения B x H под ней.
        private static void DrawSpanDimension(Transaction tr, BlockTableRecord ms, double xStart, double xEnd, double y, BeamSpan span)
        {
            AddEntity(tr, ms, new Line(new Point3d(xStart, y, 0), new Point3d(xEnd, y, 0)));

            // Маленькие вертикальные "усики" на обоих концах размерной линии.
            AddEntity(tr, ms, new Line(new Point3d(xStart, y - DimTickLength, 0), new Point3d(xStart, y + DimTickLength, 0)));
            AddEntity(tr, ms, new Line(new Point3d(xEnd, y - DimTickLength, 0), new Point3d(xEnd, y + DimTickLength, 0)));

            double midX = (xStart + xEnd) / 2.0;

            // "L=3.00" над размерной линией, "0.25 x 0.60" под ней - как в скриншотах
            // старого инструмента (BEAMDRAW\Temp\screenshots).
            DBText lengthText = MakeText($"L={span.Length:F2}", new Point3d(midX, y + TextHeight * 0.3, 0), TextHeight);
            lengthText.HorizontalMode = TextHorizontalMode.TextCenter;
            lengthText.AlignmentPoint = lengthText.Position;
            AddEntity(tr, ms, lengthText);

            DBText sectionText = MakeText($"{span.B:F2} x {span.H:F2}", new Point3d(midX, y - TextHeight * 1.5, 0), TextHeight);
            sectionText.HorizontalMode = TextHorizontalMode.TextCenter;
            sectionText.AlignmentPoint = sectionText.Position;
            AddEntity(tr, ms, sectionText);
        }

        // Маленький помощник - создаёт обычный однострочный текст (DBText) с заданной
        // строкой, точкой вставки и высотой шрифта. Используется во всех местах выше,
        // чтобы не повторять одни и те же 3 строки кода по многу раз.
        private static DBText MakeText(string value, Point3d position, double height)
        {
            return new DBText
            {
                TextString = value,
                Position = position,
                Height = height,
            };
        }

        // Помощник: добавляет готовый объект (Entity) в чертёж. Оба вызова обязательны
        // и всегда идут парой:
        //   1. ms.AppendEntity(entity) - "физически" добавляет объект в ModelSpace.
        //   2. tr.AddNewlyCreatedDBObject(entity, true) - говорит транзакции "этот
        //      объект новый, включи его при Commit()". Без этого вызова объект либо не
        //      попадёт в чертёж, либо вызовет ошибку - это обязательный ритуал AutoCAD
        //      .NET API при создании ЛЮБОГО нового объекта в транзакции.
        private static void AddEntity(Transaction tr, BlockTableRecord ms, Entity entity)
        {
            ms.AppendEntity(entity);
            tr.AddNewlyCreatedDBObject(entity, true);
        }
    }
}
