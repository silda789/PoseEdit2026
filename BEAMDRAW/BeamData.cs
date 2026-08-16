// ====================================================================================
// ФАЙЛ: BeamData.cs
// НАЗНАЧЕНИЕ: Модель данных балки/ригеля для BEAMDRAW - то, что раньше вводилось в
//             широкую таблицу Excel (лист Folha1 в BEAMDRAW.xlsm, см. BEAMDRAW\Temp).
//             Один Beam = одна балка, состоящая из нескольких пролётов (BeamSpan).
// ====================================================================================
// СХЕМА ДАННЫХ ВЗЯТА ИЗ СТАРОГО EXCEL-ИНСТРУМЕНТА (BEAMDRAW\Temp\BEAMDRAW.xlsm,
// BEAMDRAW\Temp\screenshots): у балки есть общие ("на всю балку") хомуты и параметры
// сечения по умолчанию, и у каждого пролёта - своя длина, привязки, арматура.
// Точная методика построения чертежа (как из этих чисел получить сам чертёж - шаг
// хомутов у опор/в пролёте, крюки, анкеровка) пока не задана - будет описана позже.
// ====================================================================================
#nullable disable
using System.Collections.Generic; // Даёт нам List<T> - список элементов одного типа.
using System.Collections.ObjectModel; // Даёт нам ObservableCollection<T> - см. ниже.

namespace BEAMDRAW
{
    // "public class BeamSpan" - описываем НОВЫЙ ТИП данных под названием BeamSpan.
    // Это как "чертёж" одной строки таблицы: когда мы напишем "new BeamSpan()",
    // C# создаст в памяти объект с такими полями (Length, As, Ai и т.д.).
    // Один пролёт балки - одна строка в таблице данных.
    public class BeamSpan
    {
        // "public double Length { get; set; }" - это "свойство" (property).
        // double - тип данных "дробное число" (например 3.5, 12.75).
        // { get; set; } - автоматически создаёт и "прочитать значение", и "записать
        // новое значение". Это позволяет WPF-таблице (DataGrid) напрямую читать и
        // менять эти поля, когда пользователь печатает в ячейке.
        //
        // "L" - длина пролёта, м (в старом Excel - колонка Li).
        public double Length { get; set; }

        // "as" / "ai" - привязки/отступы у левой и правой опоры пролёта, м.
        public double As { get; set; }
        public double Ai { get; set; }

        // "dH" - перепад высоты сечения (для уступов балки), м. Обычно 0.
        public double DH { get; set; }

        // Сечение пролёта B x H, м. По умолчанию берётся из Beam.DefaultB/DefaultH.
        public double B { get; set; }
        public double H { get; set; }

        // Нижняя арматура (As1/As2 в старом Excel) - до двух групп стержней.
        // int - тип данных "целое число" (без дробной части) - тут это количество
        // стержней, дробным оно быть не может.
        public int BottomAs1Count { get; set; }
        public double BottomAs1Diameter { get; set; }
        public int BottomAs2Count { get; set; }
        public double BottomAs2Diameter { get; set; }

        // Верхняя арматура - до двух групп стержней.
        public int TopAs1Count { get; set; }
        public double TopAs1Diameter { get; set; }
        public int TopAs2Count { get; set; }
        public double TopAs2Diameter { get; set; }

        // Тип сечения (см. BeamSpan.SectionTypes) - индекс в списке типов ниже.
        public string SectionType { get; set; }

        // "static readonly List<string>" - это ОБЩИЙ для всех BeamSpan список (не у
        // каждого пролёта свой, а один на весь проект), и его нельзя поменять после
        // создания (readonly = "только для чтения" после инициализации).
        // Список типов сечения - переписан со скриншота старого Excel-инструмента
        // (легенда "Section type" в BEAMDRAW\Temp\screenshots). Предварительный список,
        // уточним, когда будет описана методика черчения.
        public static readonly List<string> SectionTypes = new List<string>
        {
            "Slab - Slab / Плита - плита",
            "Without slab - Slab / Без плиты - плита",
            "Slab - Without slab / Плита - без плиты",
            "Without slab - Without slab / Без плиты - без плиты",
            "Inverted: Slab - Slab / Перевёрнутое: плита - плита",
            "Inverted: Without slab - Slab / Перевёрнутое: без плиты - плита",
            "Inverted: Slab - Without slab / Перевёрнутое: плита - без плиты",
        };
    }

    // Балка целиком: имя/номер, общие ("на всю балку") хомуты и параметры сечения по
    // умолчанию, и список пролётов.
    public class Beam
    {
        // "= "BEAM 1"" после свойства - это значение "по умолчанию": оно записывается
        // сразу при создании объекта (new Beam()), ещё до того, как мы явно что-то
        // присвоим сами. Так поля не остаются "пустыми"/нулевыми без причины.
        // Название/номер балки, например "BEAM B 1".
        public string Name { get; set; } = "BEAM 1";

        // Хомуты - общие на всю балку (в старом Excel это тоже было одно значение на
        // всю балку, не на пролёт): диаметр, шаг "в пролёте" и шаг "у опоры" на длине
        // StirrupSupportZoneLength от каждой опоры.
        public double StirrupDiameter { get; set; } = 8;
        public double StirrupSpacingGeneral { get; set; } = 0.200;
        public double StirrupSpacingSupport { get; set; } = 0.150;
        public double StirrupSupportZoneLength { get; set; } = 1.20;

        // Значения по умолчанию для новых пролётов и для кнопок "Применить ко всем".
        public double DefaultB { get; set; } = 0.25;
        public double DefaultH { get; set; } = 0.60;
        public double DefaultAs { get; set; } = 0.25;
        public double DefaultAi { get; set; } = 0.25;
        public double DefaultDH { get; set; } = 0.00;

        public int DefaultBottomAs1Count { get; set; } = 2;
        public double DefaultBottomAs1Diameter { get; set; } = 12;
        public int DefaultBottomAs2Count { get; set; }
        public double DefaultBottomAs2Diameter { get; set; }

        public int DefaultTopAs1Count { get; set; } = 2;
        public double DefaultTopAs1Diameter { get; set; } = 16;
        public int DefaultTopAs2Count { get; set; }
        public double DefaultTopAs2Diameter { get; set; }

        // ObservableCollection<T> - это "список, который умеет сообщать об изменениях".
        // Обычный List<T> молчит, когда мы добавляем/удаляем элементы - WPF-таблица
        // (DataGrid) об этом не узнает и не обновит строки на экране. ObservableCollection
        // сама "оповещает" таблицу при Add()/Remove(), и строка сразу появляется/исчезает
        // на экране без дополнительного кода. Поэтому список пролётов делаем именно таким.
        public ObservableCollection<BeamSpan> Spans { get; set; } = new ObservableCollection<BeamSpan>();

        // Создаёт новый пролёт, заполненный текущими значениями по умолчанию -
        // используется и кнопкой "Добавить пролёт", и при первом открытии окна.
        public BeamSpan CreateDefaultSpan()
        {
            return new BeamSpan
            {
                Length = 3.00,
                As = DefaultAs,
                Ai = DefaultAi,
                DH = DefaultDH,
                B = DefaultB,
                H = DefaultH,
                BottomAs1Count = DefaultBottomAs1Count,
                BottomAs1Diameter = DefaultBottomAs1Diameter,
                BottomAs2Count = DefaultBottomAs2Count,
                BottomAs2Diameter = DefaultBottomAs2Diameter,
                TopAs1Count = DefaultTopAs1Count,
                TopAs1Diameter = DefaultTopAs1Diameter,
                TopAs2Count = DefaultTopAs2Count,
                TopAs2Diameter = DefaultTopAs2Diameter,
                SectionType = BeamSpan.SectionTypes[0],
            };
        }
    }
}
