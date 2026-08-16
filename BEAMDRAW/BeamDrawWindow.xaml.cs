// ====================================================================================
// ФАЙЛ: BeamDrawWindow.xaml.cs
// НАЗНАЧЕНИЕ: "code-behind" для BeamDrawWindow.xaml - здесь вся ЛОГИКА окна (что
//             происходит по нажатию кнопок, как читаются/пишутся текстовые поля).
//             Сама разметка (что где расположено) - в соседнем файле BeamDrawWindow.xaml.
// ====================================================================================
// Для новичка: "partial class" значит, что класс BeamDrawWindow на самом деле собран
// из ДВУХ файлов - этого (.xaml.cs, наш C#-код) и BeamDrawWindow.xaml (разметка,
// которую WPF на этапе сборки тоже превращает в C#-код). "partial" разрешает так
// разделить один класс на несколько файлов - это стандартный приём WPF во всех
// файлах *.xaml.cs в этом Solution (см. PoseEditWindow.xaml.cs).
// ====================================================================================
#nullable disable
using System; // Базовые типы (Convert, и т.д.)
using System.Globalization; // CultureInfo.InvariantCulture - см. комментарий ниже.
using System.Linq; // Sum(), даёт удобные методы для списков (например Spans.Sum(...)).
using System.Windows; // Базовые классы WPF: Window, RoutedEventArgs и т.д.
using System.Windows.Controls; // DataGrid, TextBox и другие элементы управления.

namespace BEAMDRAW
{
    public partial class BeamDrawWindow : Window
    {
        // Поле класса - хранит "текущую" балку, с которой работает окно, пока оно
        // открыто. private значит "видно только внутри этого класса", readonly -
        // "можно присвоить только один раз, в конструкторе, дальше не меняется".
        private readonly Beam _beam;

        // Какой язык интерфейса сейчас выбран - false = русский, true = английский.
        // Не readonly, потому что меняется при каждом клике по кнопке RU/EN
        // (см. LangRu_Checked/LangEn_Checked ниже).
        private bool _useEnglish;

        // Конструктор - специальный метод, который вызывается при "new BeamDrawWindow(...)".
        // Сюда передаём балку (Beam), с которой окно будет работать - либо новую
        // (для черчения "с нуля"), либо уже заполненную (если потом добавим
        // сохранение/загрузку).
        public BeamDrawWindow(Beam beam)
        {
            // InitializeComponent() - метод, который генерирует сам WPF по разметке
            // BeamDrawWindow.xaml. Обязательно вызывается первым в конструкторе любого
            // WPF-окна - без него все элементы (txtBeamName, dgSpans и т.д.) останутся
            // пустыми ссылками (null) и что дальше что ни делай - будет ошибка.
            InitializeComponent();

            _beam = beam;

            // DataContext - "источник данных по умолчанию" для всего окна и его детей.
            // Именно благодаря этой строке работает "ItemsSource="{Binding Spans}""
            // в XAML: WPF берёт DataContext (тут - _beam), находит в нём свойство Spans
            // и подключает его к таблице DataGrid.
            DataContext = _beam;

            // Если у балки ещё нет ни одного пролёта - сразу добавляем один, чтобы
            // пользователь не открывал пустое окно с пустой таблицей.
            if (_beam.Spans.Count == 0)
                _beam.Spans.Add(_beam.CreateDefaultSpan());

            // Заполняем текстовые поля "по умолчанию" текущими значениями балки -
            // иначе после открытия окна они были бы просто пустыми.
            LoadDefaultsIntoFields();

            // rbLangRu.IsChecked = true - выставляем ТУТ, кодом, а не в XAML, потому что
            // _beam к этому моменту уже присвоен (см. большой комментарий у rbLangRu в
            // BeamDrawWindow.xaml - если поставить IsChecked="True" прямо в разметке,
            // событие Checked сработает ДО присвоения _beam и упадёт с
            // NullReferenceException). Это само по себе вызовет LangRu_Checked ->
            // SetLanguage(false) -> UpdateTotalLength() - отдельно вызывать их не нужно.
            rbLangRu.IsChecked = true;
        }

        // ================================================================================
        // ПЕРЕКЛЮЧАТЕЛЬ ЯЗЫКА
        // ================================================================================
        // Checked="LangRu_Checked" / "LangEn_Checked" в XAML - эти два метода вызываются,
        // когда пользователь кликает по кнопке RU или EN (это RadioButton - см.
        // комментарий у rbLangRu/rbLangEn в XAML). Оба просто вызывают один и тот же
        // SetLanguage(...) с разным значением.
        private void LangRu_Checked(object sender, RoutedEventArgs e) => SetLanguage(false);
        private void LangEn_Checked(object sender, RoutedEventArgs e) => SetLanguage(true);

        // Переключает ВЕСЬ текст окна между русским и английским. Мы не используем
        // "настоящую" WPF-локализацию (файлы .resx на каждый язык, сателлитные сборки) -
        // для маленького окна это была бы overkill-сложность; вместо этого просто
        // держим пары "русский текст / английский текст" прямо тут и присваиваем нужную
        // половину каждому элементу по его x:Name (см. BeamDrawWindow.xaml - у каждого
        // Label/GroupBox/Button/DataGridColumn есть x:Name именно для этого).
        private void SetLanguage(bool english)
        {
            _useEnglish = english;

            Title = english ? "BEAMDRAW - Beam/Girder Data Entry" : "BEAMDRAW - Ввод данных балки/ригеля";

            gbBeam.Header = english ? "Beam" : "Балка";
            lblBeamName.Content = english ? "Name:" : "Название:";

            gbSection.Header = english ? "Default section && cover" : "Сечение и привязки по умолчанию";
            lblB.Content = english ? "B, m:" : "B, м:";
            lblH.Content = english ? "H, m:" : "H, м:";
            lblAs.Content = english ? "as, m:" : "as, м:";
            lblAi.Content = english ? "ai, m:" : "ai, м:";
            lblDH.Content = english ? "dH, m:" : "dH, м:";
            btnApplySection.Content = english ? "Apply to all" : "Применить ко всем";

            gbStirrup.Header = english ? "Stirrups (whole beam)" : "Хомуты (на всю балку)";
            lblStirrupDiameter.Content = english ? "⌀, mm:" : "⌀, мм:";
            lblStirrupGeneral.Content = english ? "mid-span step, m:" : "шаг в пролёте, м:";
            lblStirrupSupport.Content = english ? "support step, m:" : "шаг у опоры, м:";
            lblStirrupZone.Content = english ? "support zone length, m:" : "длина зоны у опоры, м:";

            gbRebar.Header = english ? "Default reinforcement" : "Арматура по умолчанию";
            lblDefBottom.Content = english ? "Bottom As1: count x ⌀,mm" : "Низ As1: кол-во x ⌀,мм";
            lblDefTop.Content = english ? "Top As1: count x ⌀,mm" : "Верх As1: кол-во x ⌀,мм";
            btnApplyRebar.Content = english ? "Apply to all" : "Применить ко всем";

            colLength.Header = english ? "L, m" : "L, м";
            colAs.Header = "as, " + (english ? "m" : "м");
            colAi.Header = "ai, " + (english ? "m" : "м");
            colDH.Header = "dH, " + (english ? "m" : "м");
            colB.Header = english ? "B, m" : "B, м";
            colH.Header = english ? "H, m" : "H, м";
            colBottomAs1Count.Header = english ? "Bottom As1 count" : "Низ As1 кол-во";
            colBottomAs1Diameter.Header = english ? "Bottom As1 ⌀,mm" : "Низ As1 ⌀,мм";
            colBottomAs2Count.Header = english ? "Bottom As2 count" : "Низ As2 кол-во";
            colBottomAs2Diameter.Header = english ? "Bottom As2 ⌀,mm" : "Низ As2 ⌀,мм";
            colTopAs1Count.Header = english ? "Top As1 count" : "Верх As1 кол-во";
            colTopAs1Diameter.Header = english ? "Top As1 ⌀,mm" : "Верх As1 ⌀,мм";
            colTopAs2Count.Header = english ? "Top As2 count" : "Верх As2 кол-во";
            colTopAs2Diameter.Header = english ? "Top As2 ⌀,mm" : "Верх As2 ⌀,мм";

            // DisplayMemberPath - какое поле объекта SectionTypeOption показывать в
            // выпадающем списке (см. подробный комментарий в XAML рядом с colSectionType).
            colSectionType.Header = english ? "Section type" : "Тип сечения";
            colSectionType.DisplayMemberPath = english ? "DisplayEn" : "DisplayRu";

            btnAddSpan.Content = english ? "Add span" : "Добавить пролёт";
            btnRemoveSpan.Content = english ? "Remove span" : "Удалить пролёт";
            btnCancel.Content = english ? "Cancel" : "Отмена";

            // Суммарная длина зависит от языка ТОЛЬКО подписью ("Total length"/
            // "Суммарная длина") - само число не меняется, поэтому просто пересчитываем
            // заново тем же методом, который и так знает про _useEnglish.
            UpdateTotalLength();
        }

        // Переносит значения по умолчанию из объекта _beam в текстовые поля окна.
        // ToStringInvariant() - наш собственный маленький помощник ниже в этом файле,
        // который всегда печатает число с точкой (12.5), а не с запятой (12,5) - это
        // важно, потому что в русской Windows по умолчанию разделитель дробной части -
        // запятая, а нам нужен единообразный формат для парсинга обратно в double.
        private void LoadDefaultsIntoFields()
        {
            txtBeamName.Text = _beam.Name;

            txtDefaultB.Text = ToStringInvariant(_beam.DefaultB);
            txtDefaultH.Text = ToStringInvariant(_beam.DefaultH);
            txtDefaultAs.Text = ToStringInvariant(_beam.DefaultAs);
            txtDefaultAi.Text = ToStringInvariant(_beam.DefaultAi);
            txtDefaultDH.Text = ToStringInvariant(_beam.DefaultDH);

            txtStirrupDiameter.Text = ToStringInvariant(_beam.StirrupDiameter);
            txtStirrupSpacingGeneral.Text = ToStringInvariant(_beam.StirrupSpacingGeneral);
            txtStirrupSpacingSupport.Text = ToStringInvariant(_beam.StirrupSpacingSupport);
            txtStirrupZoneLength.Text = ToStringInvariant(_beam.StirrupSupportZoneLength);

            txtDefBottomAs1Count.Text = _beam.DefaultBottomAs1Count.ToString(CultureInfo.InvariantCulture);
            txtDefBottomAs1Diameter.Text = ToStringInvariant(_beam.DefaultBottomAs1Diameter);
            txtDefTopAs1Count.Text = _beam.DefaultTopAs1Count.ToString(CultureInfo.InvariantCulture);
            txtDefTopAs1Diameter.Text = ToStringInvariant(_beam.DefaultTopAs1Diameter);
        }

        // ================================================================================
        // КНОПКА "Применить ко всем" (сечение/привязки) - аналог CommandButton1-4 из
        // старого Excel-инструмента: взять одно введённое значение и раздать его во ВСЕ
        // строки таблицы пролётов.
        // ================================================================================
        // "object sender, RoutedEventArgs e" - стандартная "подпись" (сигнатура) для
        // любого обработчика событий кнопки в WPF. sender - какая именно кнопка была
        // нажата (нам тут не нужно), e - подробности события (тоже не нужно) - но
        // параметры обязательно должны быть именно такими, иначе XAML не сможет
        // подключить Click="btnApplySection_Click" к этому методу.
        private void btnApplySection_Click(object sender, RoutedEventArgs e)
        {
            // ParseD(...) - наш помощник ниже: читает текст из TextBox и превращает
            // в double, а если там мусор/пусто - возвращает 0 вместо падения программы.
            _beam.DefaultB = ParseD(txtDefaultB.Text);
            _beam.DefaultH = ParseD(txtDefaultH.Text);
            _beam.DefaultAs = ParseD(txtDefaultAs.Text);
            _beam.DefaultAi = ParseD(txtDefaultAi.Text);
            _beam.DefaultDH = ParseD(txtDefaultDH.Text);

            // "foreach (var span in _beam.Spans)" - проходим ПО ОЧЕРЕДИ по каждому
            // пролёту в списке и меняем у него B/H/As/Ai/DH на новые значения.
            foreach (var span in _beam.Spans)
            {
                span.B = _beam.DefaultB;
                span.H = _beam.DefaultH;
                span.As = _beam.DefaultAs;
                span.Ai = _beam.DefaultAi;
                span.DH = _beam.DefaultDH;
            }

            // Items.Refresh() - говорит таблице "перерисуй ячейки заново", потому что
            // мы поменяли значения в объектах BeamSpan напрямую в C#, а не через саму
            // таблицу - DataGrid сам об этом не узнал бы без явной подсказки.
            dgSpans.Items.Refresh();
            UpdateTotalLength();
        }

        // Кнопка "Применить ко всем" для арматуры по умолчанию (аналог CommandButton5-8).
        private void btnApplyRebar_Click(object sender, RoutedEventArgs e)
        {
            _beam.DefaultBottomAs1Count = (int)ParseD(txtDefBottomAs1Count.Text);
            _beam.DefaultBottomAs1Diameter = ParseD(txtDefBottomAs1Diameter.Text);
            _beam.DefaultTopAs1Count = (int)ParseD(txtDefTopAs1Count.Text);
            _beam.DefaultTopAs1Diameter = ParseD(txtDefTopAs1Diameter.Text);

            foreach (var span in _beam.Spans)
            {
                span.BottomAs1Count = _beam.DefaultBottomAs1Count;
                span.BottomAs1Diameter = _beam.DefaultBottomAs1Diameter;
                span.TopAs1Count = _beam.DefaultTopAs1Count;
                span.TopAs1Diameter = _beam.DefaultTopAs1Diameter;
            }

            dgSpans.Items.Refresh();
        }

        // ================================================================================
        // Добавить / удалить пролёт
        // ================================================================================
        private void btnAddSpan_Click(object sender, RoutedEventArgs e)
        {
            // Сначала считываем актуальные значения "по умолчанию" из полей ввода -
            // так новый пролёт получит именно то, что сейчас видит пользователь в
            // полях "по умолчанию", даже если он их поменял, но ещё не нажал
            // "Применить ко всем".
            _beam.DefaultB = ParseD(txtDefaultB.Text);
            _beam.DefaultH = ParseD(txtDefaultH.Text);
            _beam.DefaultAs = ParseD(txtDefaultAs.Text);
            _beam.DefaultAi = ParseD(txtDefaultAi.Text);
            _beam.DefaultDH = ParseD(txtDefaultDH.Text);

            // .Add(...) на ObservableCollection сам добавляет строку в таблицу на
            // экране - никакого Items.Refresh() тут не нужно (в отличие от изменения
            // значений внутри уже существующих строк выше).
            _beam.Spans.Add(_beam.CreateDefaultSpan());
            UpdateTotalLength();
        }

        private void btnRemoveSpan_Click(object sender, RoutedEventArgs e)
        {
            // dgSpans.SelectedItems - какие строки таблицы сейчас выделены мышкой.
            // ".Cast<BeamSpan>().ToList()" - превращаем в обычный список BeamSpan,
            // чтобы можно было спокойно удалять элементы (менять исходную коллекцию
            // "на ходу", пока по ней же идёт перебор - плохая идея и приводит к ошибке,
            // поэтому сначала копируем список выделенных строк отдельно).
            var selected = dgSpans.SelectedItems.Cast<BeamSpan>().ToList();
            foreach (var span in selected)
                _beam.Spans.Remove(span);

            UpdateTotalLength();
        }

        // Пересчитывает и показывает суммарную длину балки (сумма Length по всем
        // пролётам) - вызывается после любого изменения, которое может повлиять на
        // длину: добавление/удаление пролёта, редактирование ячейки "L, м".
        private void UpdateTotalLength()
        {
            double total = _beam.Spans.Sum(s => s.Length);
            lblTotalLength.Content = _useEnglish
                ? $"Total length: {total:F2} m"
                : $"Суммарная длина: {total:F2} м";
        }

        // Событие DataGrid, которое срабатывает, когда пользователь заканчивает
        // редактировать ячейку (например, нажал Enter или кликнул в другую ячейку).
        // Нам это нужно только для того, чтобы обновить "Суммарная длина" сразу же,
        // а не только при следующем добавлении/удалении пролёта.
        private void dgSpans_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {
            // Dispatcher.BeginInvoke откладывает пересчёт на "чуть позже" (после того,
            // как WPF реально запишет новое значение в BeamSpan) - если пересчитать
            // прямо здесь, мы бы ещё видели СТАРОЕ значение ячейки.
            Dispatcher.BeginInvoke(new Action(UpdateTotalLength));
        }

        // ================================================================================
        // OK / Отмена
        // ================================================================================
        private void btnOk_Click(object sender, RoutedEventArgs e)
        {
            _beam.Name = txtBeamName.Text;
            _beam.StirrupDiameter = ParseD(txtStirrupDiameter.Text);
            _beam.StirrupSpacingGeneral = ParseD(txtStirrupSpacingGeneral.Text);
            _beam.StirrupSpacingSupport = ParseD(txtStirrupSpacingSupport.Text);
            _beam.StirrupSupportZoneLength = ParseD(txtStirrupZoneLength.Text);

            // DialogResult = true - стандартный способ WPF сказать "пользователь нажал
            // OK, а не закрыл окно крестиком/Esc". Тот, кто открыл это окно (в нашем
            // случае - команда BEAMDRAW в Commands.cs) проверяет именно это значение,
            // чтобы понять, продолжать ли работу с введёнными данными.
            DialogResult = true;
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }

        // ================================================================================
        // Маленькие помощники для перевода текста <-> число
        // ================================================================================
        // ParseD ("parse double") - безопасно превращает текст из TextBox в число.
        // CultureInfo.InvariantCulture - "нейтральная" культура, где разделитель
        // дробной части всегда точка (.) - не зависит от языка Windows у пользователя.
        // Если пользователь напечатал что-то нечисловое (или оставил поле пустым) -
        // double.TryParse вернёт false, и мы просто возвращаем 0 вместо падения окна.
        private static double ParseD(string text)
        {
            return double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out double value)
                ? value
                : 0;
        }

        private static string ToStringInvariant(double value)
        {
            return value.ToString(CultureInfo.InvariantCulture);
        }
    }
}
