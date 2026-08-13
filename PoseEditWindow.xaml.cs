#nullable disable
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.ApplicationServices;
using System;
using System.Collections.Generic;
using System.Linq; // Для Enumerable.Range и других LINQ методов
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using App = Autodesk.AutoCAD.ApplicationServices.Application;

namespace PoseEdit2026
{
    public partial class PoseEditWindow : Window
    {
        private ObjectId _currentBlockId;
        // Флаг, чтобы не запускать события, пока окно грузится
        private bool _isLoading = true;

        public PoseEditWindow(ObjectId blockId)
        {
            InitializeComponent();
            _currentBlockId = blockId;

            // 1. Сначала заполняем списки
            InitLists();

            // 2. Инициализируем настройки (убрано, так как настройки теперь в отдельном окне)
            // InitSettings(); // УДАЛЕНО: настройки теперь открываются в отдельном окне SettingsWindow

            // 3. Подписываемся на события для автоматического обновления полей
            SetupEventHandlers();

            // 4. Только потом разрешаем событиям работать
            _isLoading = false;

            // 5. Загружаем данные из блока
            LoadDataFromBlock();
        }

        // ====================================================================================
        // МЕТОД: InitLists (Инициализация списков)
        // НАЗНАЧЕНИЕ: Заполняет все ComboBox в окне начальными значениями
        // ====================================================================================
        // Этот метод вызывается при создании окна (в конструкторе)
        // Он заполняет выпадающие списки (ComboBox) данными, из которых пользователь может выбрать
        private void InitLists()
        {
            try
            {
                // ====================================================================================
                // СПИСОК 1: cmbShapeNumber (Номер формы арматуры)
                // ====================================================================================
                // Заполняем список номерами форм от 00 до 93
                // "D2" означает формат с двумя цифрами (00, 01, 02, ..., 93)
                // ВНИМАНИЕ: В папке Resources есть только 94 картинки (от Shape_00.png до Shape_93.png)
                // C# 12 / .NET 8: Используем Enumerable.Range для более читаемого кода
                cmbShapeNumber.Items.Clear(); // Очищаем список перед заполнением
                var shapeNumbers = Enumerable.Range(0, 94) // Генерируем числа от 0 до 93 (94 элемента)
                    .Select(i => i.ToString("D2"))          // Преобразуем в строку с ведущим нулем (00, 01, ...)
                    .ToArray();                             // Преобразуем в массив
                foreach (var number in shapeNumbers)
                {
                    cmbShapeNumber.Items.Add(number); // Добавляем номер в список
                }

                // ====================================================================================
                // СПИСОК 2: cmbMaterial (Материал арматуры)
                // ====================================================================================
                // Заполняем список материалами арматуры согласно стандартам
                // A500C, A240, A-I, A-III - это марки стали для арматуры
                cmbMaterial.Items.Clear(); // Очищаем список перед заполнением
                
                // C# 12: Используем collection expression для более компактного кода
                // Вместо нескольких Add() можно использовать один массив и цикл
                string[] materials = ["A500C", "A240", "A-I", "A-III"]; // Массив материалов
                foreach (var material in materials)
                {
                    cmbMaterial.Items.Add(material);
                }

                // ====================================================================================
                // СПИСОК 3: cmbNoteList (Список примечаний/позиций)
                // ====================================================================================
                // Заполняем список примечаниями, которые указывают расположение арматуры
                // Например: "внутр." = внутренняя, "внешн." = внешняя, "нижн." = нижняя и т.д.
                cmbNoteList.Items.Clear(); // Очищаем список перед заполнением
                
                // C# 12: Используем collection expression для более компактного кода
                // Все примечания в одном массиве, затем добавляем через цикл
                string[] noteList = [
                    // Основные позиции (расположение арматуры)
                    "внутр.",      // Внутренняя арматура
                    "внешн.",      // Внешняя арматура
                    "нижн.",       // Нижняя арматура
                    "верхн.",      // Верхняя арматура
                    "Центр",       // Центральная арматура
                    "Хомут",       // Хомут (поперечная арматура)
                    "Шпилька",     // Шпилька (вертикальная арматура)
                    
                    // Дополнительные позиции (первый ряд)
                    "внутр. доп.",           // Внутренняя дополнительная
                    "внешн. доп.",           // Внешняя дополнительная
                    "внутр. доп. 2-й ряд",   // Внутренняя дополнительная, 2-й ряд
                    "внешн. доп. 2-й ряд",   // Внешняя дополнительная, 2-й ряд
                    
                    // Дополнительные позиции (верх/низ)
                    "нижн. доп.",           // Нижняя дополнительная
                    "верхн. доп.",          // Верхняя дополнительная
                    "нижн. доп. 2-й ряд",   // Нижняя дополнительная, 2-й ряд
                    "верхн. доп. 2-й ряд",  // Верхняя дополнительная, 2-й ряд
                    
                    // Специальные позиции
                    "боковая стержень",     // Боковая стержневая арматура
                    "доп.",                 // Дополнительная (общее)
                    
                    // Плотность арматуры (количество стержней на квадратный метр)
                    "1 шт/м2",   // 1 стержень на квадратный метр
                    "2 шт/м2",   // 2 стержня на квадратный метр
                    "4 шт/м2",   // 4 стержня на квадратный метр
                    "6 шт/м2",   // 6 стержней на квадратный метр
                    "9 шт/м2",   // 9 стержней на квадратный метр
                    "10 шт/м2",  // 10 стержней на квадратный метр
                    "12 шт/м2"   // 12 стержней на квадратный метр
                ];
                
                // Добавляем все элементы из массива в ComboBox
                foreach (var note in noteList)
                {
                    cmbNoteList.Items.Add(note);
                }

                // ====================================================================================
                // СПИСОК 4: cmbProjectName (Имя проекта)
                // ====================================================================================
                // УДАЛЕНО: cmbProjectName теперь находится в отдельном окне SettingsWindow
                // Инициализация списка проектов происходит в SettingsWindow.xaml.cs
            }
            catch (Exception ex)
            {
                // Если произошла ошибка при заполнении списков, показываем сообщение пользователю
                MessageBox.Show("Ошибка при инициализации списков: " + ex.Message);
            }
        }

        // ====================================================================================
        // МЕТОД: InitSettings (Инициализация настроек)
        // НАЗНАЧЕНИЕ: УДАЛЕН - настройки теперь инициализируются в отдельном окне SettingsWindow
        // ====================================================================================
        // Этот метод больше не нужен, так как настройки теперь открываются в отдельном окне.
        // Инициализация настроек происходит в SettingsWindow.xaml.cs в методе LoadSettings().
        // ====================================================================================

        // ====================================================================================
        // МЕТОД: SetupEventHandlers (Настройка обработчиков событий)
        // НАЗНАЧЕНИЕ: Подписывается на события для автоматического обновления полей
        // ====================================================================================
        // Этот метод настраивает автоматическое обновление:
        // 1. Когда пользователь выбирает значение в cmbNoteList, оно записывается в txtNote
        // 2. Когда изменяются значения в TextBox, автоматически формируется строка в txtPoseImage
        private void SetupEventHandlers()
        {
            // ====================================================================================
            // ОБРАБОТЧИК 1: Выбор значения в списке примечаний (cmbNoteList)
            // ====================================================================================
            // Когда пользователь выбирает значение из выпадающего списка (например, "Хомут"),
            // это значение автоматически записывается в текстовое поле txtNote
            cmbNoteList.SelectionChanged += (sender, e) =>
            {
                // Проверяем, что окно уже загружено (не во время инициализации)
                if (_isLoading) return;

                // Если выбрано какое-то значение
                if (cmbNoteList.SelectedItem != null)
                {
                    // Записываем выбранное значение в txtNote
                    txtNote.Text = cmbNoteList.SelectedItem.ToString();
                }
            };

            // ====================================================================================
            // ОБРАБОТЧИКИ 2-7: Изменение значений в TextBox для формирования Pose Image
            // ====================================================================================
            // Когда пользователь изменяет любое из этих полей, автоматически обновляется
            // строка в txtPoseImage по формату: (txtPose) txtItemMult x txtItem Ø txtDiameter / txtSpace L= txtLength
            // Пример: (5) 2x5Ø20/200 L=1625

            // Подписываемся на событие TextChanged для каждого поля
            txtPose.TextChanged += (s, e) => UpdatePoseImage();
            txtItemMult.TextChanged += (s, e) => UpdatePoseImage();
            txtItem.TextChanged += (s, e) => UpdatePoseImage();
            txtDiameter.TextChanged += (s, e) => UpdatePoseImage();
            txtSpace.TextChanged += (s, e) => UpdatePoseImage();
            txtLength.TextChanged += (s, e) => UpdatePoseImage();
        }

        // ====================================================================================
        // МЕТОД: UpdatePoseImage (Обновление изображения позиции)
        // НАЗНАЧЕНИЕ: Формирует строку для txtPoseImage на основе значений из других полей
        // ====================================================================================
        // Этот метод собирает значения из разных TextBox и формирует строку по формату:
        // (txtPose) txtItemMult x txtItem Ø txtDiameter / txtSpace L= txtLength
        //
        // ПРИМЕРЫ:
        //   Если txtPose = "5", txtItemMult = "2", txtItem = "5", txtDiameter = "20", 
        //   txtSpace = "200", txtLength = "L=1625"
        //   Результат: "(5) 2x5Ø20/200 L=1625"
        //
        // ЛОГИКА:
        //   - Если поле пустое, оно не включается в строку
        //   - txtPose всегда в скобках: (значение)
        //   - txtItemMult с префиксом "x": значение x
        //   - txtItem без префикса: значение
        //   - txtDiameter с символом диаметра: Øзначение
        //   - txtSpace с префиксом "/": /значение
        //   - txtLength как есть (уже содержит "L="): L=значение
        //
        // ПАРАМЕТРЫ:
        //   force - если true, обновляет даже во время загрузки (для принудительного обновления)
        private void UpdatePoseImage(bool force = false)
        {
            // Проверяем, что окно уже загружено (не во время инициализации)
            // Если force = true, пропускаем эту проверку (для принудительного обновления)
            if (!force && _isLoading) return;

            try
            {
                // Создаем список частей строки
                List<string> parts = new List<string>();

                // ====================================================================================
                // ЧАСТЬ 1: txtPose в скобках
                // ====================================================================================
                // Если поле txtPose не пустое, добавляем его значение в скобках
                if (!string.IsNullOrWhiteSpace(txtPose.Text))
                {
                    parts.Add("(" + txtPose.Text.Trim() + ")");
                }

                // ====================================================================================
                // ЧАСТЬ 2: txtItemMult с префиксом "x"
                // ====================================================================================
                // Если поле txtItemMult не пустое, добавляем его значение с префиксом "x"
                // Например: если txtItemMult = "2", то получится "2x"
                if (!string.IsNullOrWhiteSpace(txtItemMult.Text))
                {
                    parts.Add(txtItemMult.Text.Trim() + "x");
                }

                // ====================================================================================
                // ЧАСТЬ 3: txtItem (текущее значение)
                // ====================================================================================
                // Если поле txtItem не пустое, добавляем его значение
                if (!string.IsNullOrWhiteSpace(txtItem.Text))
                {
                    parts.Add(txtItem.Text.Trim());
                }

                // ====================================================================================
                // ЧАСТЬ 4: txtDiameter с символом диаметра "Ø"
                // ====================================================================================
                // Если поле txtDiameter не пустое, добавляем его значение с символом диаметра
                // Символ "Ø" - это Unicode символ диаметра (можно также использовать "⌀")
                if (!string.IsNullOrWhiteSpace(txtDiameter.Text))
                {
                    parts.Add("Ø" + txtDiameter.Text.Trim());
                }

                // ====================================================================================
                // ЧАСТЬ 5: txtSpace с префиксом "/"
                // ====================================================================================
                // Если поле txtSpace не пустое, добавляем его значение с префиксом "/"
                // Например: если txtSpace = "200", то получится "/200"
                if (!string.IsNullOrWhiteSpace(txtSpace.Text))
                {
                    parts.Add("/" + txtSpace.Text.Trim());
                }

                // ====================================================================================
                // ЧАСТЬ 6: txtLength (как есть, уже содержит "L=")
                // ====================================================================================
                // Если поле txtLength не пустое, добавляем его значение как есть
                // Обычно txtLength уже содержит "L=" в начале (например, "L=1625")
                if (!string.IsNullOrWhiteSpace(txtLength.Text))
                {
                    // Если txtLength не содержит "L=", добавляем его
                    string lengthValue = txtLength.Text.Trim();
                    if (!lengthValue.StartsWith("L="))
                    {
                        parts.Add("L=" + lengthValue);
                    }
                    else
                    {
                        parts.Add(lengthValue);
                    }
                }

                // ====================================================================================
                // СОБИРАЕМ ВСЕ ЧАСТИ В ОДНУ СТРОКУ
                // ====================================================================================
                // Объединяем все части через пробел
                // string.Join(" ", parts) - объединяет элементы списка через пробел
                string result = string.Join(" ", parts);

                // Записываем результат в txtPoseImage
                txtPoseImage.Text = result;
            }
            catch (Exception ex)
            {
                // Если произошла ошибка, просто не обновляем поле (чтобы не мешать пользователю)
                // В реальном приложении можно добавить логирование ошибок
                System.Diagnostics.Debug.WriteLine("Ошибка при обновлении PoseImage: " + ex.Message);
            }
        }

        private void LoadDataFromBlock()
        {
            try
            {
                // Защита: если ID блока пустой (чего быть не должно, но вдруг)
                if (_currentBlockId.IsNull) return;

                var attributes = BlockHelper.GetAttributes(_currentBlockId);
                // Защита: если BlockHelper вернул null (хотя мы это исправили)
                if (attributes == null) return;

                if (attributes.ContainsKey("POZ")) txtPose.Text = attributes["POZ"];
                if (attributes.ContainsKey("BOY")) txtLength.Text = attributes["BOY"];

                // ТИП И КАРТИНКА
                if (attributes.ContainsKey("TIP"))
                {
                    txtType.Text = attributes["TIP"];

                    // Безопасная установка значения в ComboBox
                    string tip = attributes["TIP"];
                    if (cmbShapeNumber.Items.Contains(tip))
                    {
                        cmbShapeNumber.Text = tip;
                    }
                }

                // РАЗМЕРЫ
                if (attributes.ContainsKey("A")) SetValueOrTag(txtA, attributes["A"]);
                if (attributes.ContainsKey("B")) SetValueOrTag(txtB, attributes["B"]);
                if (attributes.ContainsKey("C")) SetValueOrTag(txtC, attributes["C"]);
                if (attributes.ContainsKey("D")) SetValueOrTag(txtD, attributes["D"]);
                if (attributes.ContainsKey("E")) SetValueOrTag(txtE, attributes["E"]);
                if (attributes.ContainsKey("F")) SetValueOrTag(txtF, attributes["F"]);
                if (attributes.ContainsKey("R")) SetValueOrTag(txtR, attributes["R"]);

                // ПАРСИНГ TB
                if (attributes.ContainsKey("TB")) ParseTB(attributes["TB"]);

                // Загрузка ARALIK (SPACE) - шаг арматуры
                if (attributes.ContainsKey("ARALIK"))
                {
                    txtSpace.Text = attributes["ARALIK"];
                }

                // Загрузка значения Note из атрибута (если есть)
                if (attributes.ContainsKey("NOTE"))
                {
                    string noteValue = attributes["NOTE"];
                    txtNote.Text = noteValue;
                    
                    // Пытаемся найти это значение в списке и выбрать его
                    if (cmbNoteList.Items.Contains(noteValue))
                    {
                        cmbNoteList.SelectedItem = noteValue;
                    }
                }

                // ====================================================================================
                // ОБНОВЛЕНИЕ txtPoseImage ПОСЛЕ ЗАГРУЗКИ ДАННЫХ
                // ====================================================================================
                // После загрузки всех данных из блока обновляем строку в txtPoseImage
                // Вызываем принудительно (force = true), чтобы обновить даже во время загрузки
                UpdatePoseImage(true);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error in LoadDataFromBlock: " + ex.Message);
            }
        }

        private void SetValueOrTag(TextBox box, string val)
        {
            // Защита: если textBox не загрузился из XAML
            if (box == null) return;
            if (val == null) val = "";

            if (val.StartsWith("%<\\AcObjProp"))
            {
                box.Tag = val;
                box.Text = "FIELD";
                box.Background = Brushes.LightYellow;
            }
            else
            {
                box.Text = val;
                box.Tag = null;
                box.Background = Brushes.White;
            }
        }

        // ====================================================================================
        // МЕТОД: ParseTB (Парсинг строки TB)
        // НАЗНАЧЕНИЕ: Разбирает строку TB и заполняет поля txtItemMult, txtItem, txtDiameter, txtSpace, txtNote
        // ====================================================================================
        // Формат строки TB: "2x20Ø10/100 L=2260 Хомут"
        //   - 2x - txtItemMult x
        //   - 20 - txtItem
        //   - Ø10 - txtDiameter
        //   - /100 - txtSpace (шаг)
        //   - L=2260 - txtLength
        //   - Хомут - txtNote (примечание)
        private void ParseTB(string tbText)
        {
            if (string.IsNullOrEmpty(tbText)) return;
            // Проверка на наличие элементов интерфейса
            if (txtItemMult == null || txtItem == null || txtDiameter == null) return;

            // ====================================================================================
            // ШАГ 1: ИЗВЛЕКАЕМ NOTE (ПРИМЕЧАНИЕ) ИЗ КОНЦА СТРОКИ
            // ====================================================================================
            // Note находится в самом конце строки после "L=..."
            // Например: "2x20Ø10/100 L=2260 Хомут" -> Note = "Хомут"
            string noteValue = "";
            string tbTextWithoutNote = tbText;

            // Ищем "L=" в строке
            int lIndex = tbText.IndexOf("L=");
            if (lIndex > 0)
            {
                // Берем часть после "L="
                string afterL = tbText.Substring(lIndex + 2);
                // Ищем пробел после длины (это начало Note)
                int spaceAfterLength = afterL.IndexOf(' ');
                if (spaceAfterLength > 0)
                {
                    // Извлекаем Note (всё после пробела)
                    noteValue = afterL.Substring(spaceAfterLength + 1).Trim();
                }
                // "L=..." (и всё, что после) не относится к диаметру/шагу - отрезаем в любом случае,
                // даже если Note нет (иначе "10Ø22 L=2350" без Note парсится как диаметр "22 L=2350")
                tbTextWithoutNote = tbText.Substring(0, lIndex).TrimEnd();
            }
            
            // Если Note найден, записываем его
            if (!string.IsNullOrWhiteSpace(noteValue))
            {
                txtNote.Text = noteValue;
                // Пытаемся найти это значение в списке и выбрать его
                if (cmbNoteList.Items.Contains(noteValue))
                {
                    cmbNoteList.SelectedItem = noteValue;
                }
            }

            // ====================================================================================
            // ШАГ 2: ПАРСИМ ОСТАЛЬНУЮ ЧАСТЬ СТРОКИ (без Note)
            // ====================================================================================
            int diameterIndex = tbTextWithoutNote.IndexOf("Ø");
            if (diameterIndex == -1) diameterIndex = tbTextWithoutNote.IndexOf("%%C");
            if (diameterIndex == -1) diameterIndex = tbTextWithoutNote.IndexOf("%%c");

            if (diameterIndex > -1)
            {
                string leftPart = tbTextWithoutNote.Substring(0, diameterIndex);
                if (leftPart.ToLower().Contains("x"))
                {
                    string[] parts = leftPart.ToLower().Split('x');
                    if (parts.Length > 0) txtItemMult.Text = parts[0];
                    if (parts.Length > 1) txtItem.Text = parts[1];
                }
                else
                {
                    txtItemMult.Text = "";
                    txtItem.Text = leftPart;
                }

                int symLength = (tbTextWithoutNote.Contains("%%C") || tbTextWithoutNote.Contains("%%c")) ? 3 : 1;
                string rightPart = tbTextWithoutNote.Substring(diameterIndex + symLength);

                if (rightPart.Contains("/"))
                {
                    string[] dParts = rightPart.Split('/');
                    if (dParts.Length > 0) txtDiameter.Text = dParts[0];
                    // Парсим шаг арматуры (SPACE) из строки TB после символа "/"
                    // Например: "20/100" -> txtDiameter = "20", txtSpace = "100"
                    if (dParts.Length > 1) 
                    {
                        // Убираем "L=" и Note из конца, если они есть
                        string spaceValue = dParts[1];
                        // Удаляем всё после пробела (это может быть "L=..." или Note)
                        int spaceIndex = spaceValue.IndexOf(' ');
                        if (spaceIndex > 0)
                        {
                            spaceValue = spaceValue.Substring(0, spaceIndex);
                        }
                        txtSpace.Text = spaceValue.Trim();
                    }
                }
                else
                {
                    txtDiameter.Text = rightPart;
                }
            }
        }

        // --- КАРТИНКИ (Самое опасное место) ---
        private void UpdateShapeImage()
        {
            // 1. Проверяем, загружена ли форма
            if (_isLoading) return;
            // 2. Проверяем, выбран ли элемент
            if (cmbShapeNumber.SelectedItem == null) return;
            // 3. Проверяем, существует ли Image (imgPreview)
            if (imgPreview == null) return;

            string code = cmbShapeNumber.SelectedItem.ToString();
            
            // ====================================================================================
            // ПРОВЕРКА: Существует ли картинка для данного номера формы
            // ====================================================================================
            // В папке Resources есть только картинки от Shape_00.png до Shape_93.png (94 картинки)
            // Если номер формы больше 93 или равен "99" (не распознано), картинки нет
            // Пытаемся преобразовать код в число для проверки
            if (int.TryParse(code, out int shapeNumber))
            {
                // Если номер больше 93, картинки нет - очищаем изображение
                if (shapeNumber > 93)
                {
                    imgPreview.Source = null;
                    return; // Выходим, не пытаясь загрузить несуществующую картинку
                }
            }

            string assemblyName = "PoseEdit2026";
            string uriPath = $"pack://application:,,,/{assemblyName};component/Resources/Shape_{code}.png";

            try
            {
                BitmapImage bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.UriSource = new Uri(uriPath);
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.EndInit();
                imgPreview.Source = bitmap;
            }
            catch
            {
                // Тихо игнорируем отсутствие картинки, чтобы не крашить программу
                imgPreview.Source = null;
            }
        }

        private void cmbShapeNumber_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_isLoading) return;
            UpdateShapeImage();

            if (cmbShapeNumber.SelectedItem != null && txtType != null)
            {
                // Временно отключаем событие текста, чтобы не зациклить
                _isLoading = true;
                txtType.Text = cmbShapeNumber.SelectedItem.ToString();
                _isLoading = false;
            }
        }

        private void txtType_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_isLoading) return;
            if (txtType == null || cmbShapeNumber == null) return;

            string currentType = txtType.Text.Trim();
            if (cmbShapeNumber.Items.Contains(currentType))
            {
                if (cmbShapeNumber.Text != currentType)
                {
                    _isLoading = true; // Блокируем зацикливание
                    cmbShapeNumber.Text = currentType;
                    _isLoading = false;

                    // Обновляем картинку вручную, т.к. SelectionChanged может не сработать в режиме блока
                    UpdateShapeImage();
                }
            }
        }

        // --- КНОПКИ ---

        private void btnUpdate_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // ====================================================================================
                // ФОРМИРОВАНИЕ СТРОКИ TB (Текст блока) ПО ФОРМАТУ: 2x20Ø10/100 L=2260 Хомут
                // ====================================================================================
                // Формат строки: txtItemMult x txtItem Ø txtDiameter / txtSpace L= txtLength Note
                // Пример: "2x20Ø10/100 L=2260 Хомут"
                string tbResult = "";
                
                // Часть 1: txtItemMult x (если есть)
                if (!string.IsNullOrEmpty(txtItemMult.Text)) 
                    tbResult += txtItemMult.Text + "x";
                
                // Часть 2: txtItem Ø txtDiameter
                tbResult += txtItem.Text + "Ø" + txtDiameter.Text;

                // Часть 3: / txtSpace (шаг арматуры)
                if (!string.IsNullOrWhiteSpace(txtSpace.Text))
                {
                    tbResult += "/" + txtSpace.Text.Trim();
                }

                // Часть 5: Note (примечание) в конце строки
                if (!string.IsNullOrWhiteSpace(txtNote.Text))
                {
                    tbResult += " " + txtNote.Text.Trim();
                }

                // ====================================================================================
                // СОХРАНЕНИЕ АТРИБУТОВ В БЛОК
                // ====================================================================================
                Dictionary<string, string> newValues = new Dictionary<string, string>();

                // Основные атрибуты
                newValues["POZ"] = txtPose.Text;
                newValues["TB"] = tbResult;
                // BOY - отдельный атрибут (как везде в LegacyCommands.cs/PozHelper.cs), а не часть
                // строки TB - иначе ParseTB при следующем открытии путает длину с диаметром
                newValues["BOY"] = txtLength.Text.Trim();
                newValues["TIP"] = txtType.Text;

                // Сохраняем ARALIK (SPACE) - шаг арматуры отдельным атрибутом
                if (!string.IsNullOrWhiteSpace(txtSpace.Text))
                {
                    newValues["ARALIK"] = txtSpace.Text.Trim();
                }
                
                // Сохраняем значение Note (примечание) отдельным атрибутом (для удобства)
                if (!string.IsNullOrWhiteSpace(txtNote.Text))
                {
                    newValues["NOTE"] = txtNote.Text.Trim();
                }

                // Размеры арматуры (A, B, C, D, E, F, R)
                newValues["A"] = GetValueOrTag(txtA);
                newValues["B"] = GetValueOrTag(txtB);
                newValues["C"] = GetValueOrTag(txtC);
                newValues["D"] = GetValueOrTag(txtD);
                newValues["E"] = GetValueOrTag(txtE);
                newValues["F"] = GetValueOrTag(txtF);
                newValues["R"] = GetValueOrTag(txtR);

                BlockHelper.SetAttributes(_currentBlockId, newValues);
                PozHelper.RepositionShapeText(_currentBlockId);

                Document doc = App.DocumentManager.MdiActiveDocument;
                doc.Editor.Command("_.UPDATEFIELD", _currentBlockId, "");

                this.DialogResult = true;
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error updating: " + ex.Message);
            }
        }

        private void btnDiscard_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }

        private void btnCalc_Click(object sender, RoutedEventArgs e)
        {
            double a = ParseDouble(txtA.Text);
            double b = ParseDouble(txtB.Text);
            double c = ParseDouble(txtC.Text);
            double d = ParseDouble(txtD.Text);
            double e_val = ParseDouble(txtE.Text);
            double f = ParseDouble(txtF.Text);

            string type = txtType.Text.Trim();
            double total = 0;

            switch (type)
            {
                case "00": total = a; break;
                case "11": case "15": total = a + b; break;
                case "21": case "25": case "41": total = a + b + c; break;
                case "31": case "38": total = a + b + c + d + e_val; break;
                default: total = a + b + c + d + e_val + f; break;
            }

            txtLength.Text = "L=" + total.ToString("0");
        }

        private void LinkDimensionToTextBox(TextBox targetBox)
        {
            this.Hide();
            try
            {
                Editor ed = App.DocumentManager.MdiActiveDocument.Editor;
                PromptEntityOptions opt = new PromptEntityOptions("\nSelect Line or Dimension:");
                opt.SetRejectMessage("\nInvalid object.");
                opt.AddAllowedClass(typeof(Line), true);
                opt.AddAllowedClass(typeof(Polyline), true);
                opt.AddAllowedClass(typeof(Dimension), true);

                PromptEntityResult res = ed.GetEntity(opt);

                if (res.Status == PromptStatus.OK)
                {
                    ObjectId objId = res.ObjectId;
                    string prop = "Length";
                    double val = 0;

                    using (Transaction tr = objId.Database.TransactionManager.StartTransaction())
                    {
                        Entity ent = tr.GetObject(objId, OpenMode.ForRead) as Entity;
                        if (ent is Line l) val = l.Length;
                        else if (ent is Polyline p) val = p.Length;
                        else if (ent is Dimension d)
                        {
                            prop = "Measurement";
                            try { val = (double)ent.GetType().GetProperty("Measurement").GetValue(ent); } catch { }
                        }
                        tr.Commit();
                    }

                    string fieldCode = string.Format("%<\\AcObjProp Object(%<\\_ObjId {0}>%).{1} \\f \"%lu2%pr0\">%", objId.OldIdPtr, prop);

                    targetBox.Text = val.ToString("0");
                    targetBox.Tag = fieldCode;
                    targetBox.Background = Brushes.LightYellow;
                }
            }
            finally
            {
                this.ShowDialog();
            }
        }

        private void btnLinkA_Click(object sender, RoutedEventArgs e) => LinkDimensionToTextBox(txtA);
        private void btnLinkB_Click(object sender, RoutedEventArgs e) => LinkDimensionToTextBox(txtB);
        private void btnLinkC_Click(object sender, RoutedEventArgs e) => LinkDimensionToTextBox(txtC);
        private void btnLinkD_Click(object sender, RoutedEventArgs e) => LinkDimensionToTextBox(txtD);
        private void btnLinkE_Click(object sender, RoutedEventArgs e) => LinkDimensionToTextBox(txtE);
        private void btnLinkF_Click(object sender, RoutedEventArgs e) => LinkDimensionToTextBox(txtF);
        private void btnLinkR_Click(object sender, RoutedEventArgs e) => LinkDimensionToTextBox(txtR);
        private void btnLinkItem_Click(object sender, RoutedEventArgs e) => LinkDimensionToTextBox(txtItem);

        private void txtDimension_GotFocus(object sender, RoutedEventArgs e)
        {
            if (sender is TextBox box)
            {
                box.Tag = null;
                box.Background = Brushes.White;
            }
        }

        private void btnDetermination_Click(object sender, RoutedEventArgs e)
        {
            this.Hide();
            try
            {
                Editor ed = App.DocumentManager.MdiActiveDocument.Editor;
                PromptEntityOptions opt = new PromptEntityOptions("\nSelect polyline:");
                opt.SetRejectMessage("\nInvalid object.");
                opt.AddAllowedClass(typeof(Polyline), true);
                opt.AddAllowedClass(typeof(Line), true);

                PromptEntityResult res = ed.GetEntity(opt);
                if (res.Status == PromptStatus.OK)
                {
                    var result = RebarRecognizer.Recognize(res.ObjectId);
                    txtType.Text = result.Type;
                    txtLength.Text = result.Length;
                    txtA.Text = result.A;
                    txtB.Text = result.B;
                    txtC.Text = result.C;
                    txtD.Text = result.D;
                    txtE.Text = result.E;
                    txtF.Text = result.F;
                    txtR.Text = result.R;

                    // Запоминаем связь с этой полилинией: если её потом изменят (длину/угол),
                    // REGEN/REGENALL и TDDBN пересчитают значения этой позиции автоматически.
                    if (result.Type != "99")
                        RebarRecognizer.SetLinkedPolyline(_currentBlockId, res.ObjectId);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error: " + ex.Message);
            }
            finally
            {
                this.ShowDialog();
            }
        }

        // Обновляет все блоки RL-POS в чертеже с тем же номером позиции (POZ).
        // Применяет текущие значения формы (TIP, A-F, R, TB) ко всем совпадающим блокам.
        private void btnUpdateAll_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string targetPoz = txtPose.Text.Trim();
                if (string.IsNullOrEmpty(targetPoz))
                {
                    MessageBox.Show("POZ is empty. Fill in the position number first.", "UpdateAll", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Собираем атрибуты из формы (аналогично btnUpdate)
                string tbResult = "";
                if (!string.IsNullOrEmpty(txtItemMult.Text)) tbResult += txtItemMult.Text + "x";
                tbResult += txtItem.Text + "Ø" + txtDiameter.Text;
                if (!string.IsNullOrWhiteSpace(txtSpace.Text)) tbResult += "/" + txtSpace.Text.Trim();
                if (!string.IsNullOrWhiteSpace(txtNote.Text)) tbResult += " " + txtNote.Text.Trim();

                var newValues = new Dictionary<string, string>
                {
                    ["POZ"]  = targetPoz,
                    ["TB"]   = tbResult,
                    // BOY - отдельный атрибут, не часть строки TB (см. btnUpdate_Click)
                    ["BOY"]  = txtLength.Text.Trim(),
                    ["TIP"]  = txtType.Text,
                    ["A"]    = GetValueOrTag(txtA),
                    ["B"]    = GetValueOrTag(txtB),
                    ["C"]    = GetValueOrTag(txtC),
                    ["D"]    = GetValueOrTag(txtD),
                    ["E"]    = GetValueOrTag(txtE),
                    ["F"]    = GetValueOrTag(txtF),
                    ["R"]    = GetValueOrTag(txtR)
                };
                if (!string.IsNullOrWhiteSpace(txtSpace.Text)) newValues["ARALIK"] = txtSpace.Text.Trim();
                if (!string.IsNullOrWhiteSpace(txtNote.Text))  newValues["NOTE"]   = txtNote.Text.Trim();

                // Ищем все блоки RL-POS и RL-POS2 в чертеже
                Document doc = App.DocumentManager.MdiActiveDocument;
                Database db  = doc.Database;
                int updated  = 0;
                var updatedIds = new List<ObjectId>();

                using (Transaction tr = db.TransactionManager.StartTransaction())
                {
                    BlockTable bt = tr.GetObject(db.BlockTableId, OpenMode.ForRead) as BlockTable;
                    BlockTableRecord ms = tr.GetObject(db.CurrentSpaceId, OpenMode.ForRead) as BlockTableRecord;

                    foreach (ObjectId id in ms)
                    {
                        DBObject obj = tr.GetObject(id, OpenMode.ForRead);
                        if (obj is not BlockReference blk) continue;

                        string bname = blk.Name;
                        if (bname != "RL-POS" && bname != "RL-POS2") continue;

                        // Проверяем POZ у этого блока
                        var attrs = BlockHelper.GetAttributes(id);
                        if (!attrs.ContainsKey("POZ") || attrs["POZ"] != targetPoz) continue;

                        BlockHelper.SetAttributes(id, newValues);
                        updatedIds.Add(id);
                        updated++;
                    }
                    tr.Commit();
                }

                // Расставляем TB/BOY/NOT по местам у каждого обновлённого блока (отдельно от
                // основной транзакции выше - RepositionShapeText открывает свою собственную)
                foreach (ObjectId id in updatedIds) PozHelper.RepositionShapeText(id);

                MessageBox.Show($"Updated {updated} block(s) with POZ = {targetPoz}.", "UpdateAll", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error in UpdateAll: " + ex.Message);
            }
        }

        // Позволяет выбрать другой блок RL-POS/RL-POS2 в чертеже и загрузить его данные в форму.
        private void btnRead_Click(object sender, RoutedEventArgs e)
        {
            this.Hide();
            try
            {
                Editor ed = App.DocumentManager.MdiActiveDocument.Editor;

                var filterList = new Autodesk.AutoCAD.DatabaseServices.TypedValue[]
                {
                    new Autodesk.AutoCAD.DatabaseServices.TypedValue((int)DxfCode.Start, "INSERT"),
                    new Autodesk.AutoCAD.DatabaseServices.TypedValue((int)DxfCode.BlockName, "RL-POS,RL-POS2")
                };
                var filter = new SelectionFilter(filterList);

                PromptSelectionOptions selOpts = new PromptSelectionOptions();
                selOpts.MessageForAdding = "\nSelect RL-POS block to read:";
                selOpts.SingleOnly = true;
                PromptSelectionResult res = ed.GetSelection(selOpts, filter);

                if (res.Status == PromptStatus.OK)
                {
                    _currentBlockId = res.Value[0].ObjectId;
                    _isLoading = true;
                    LoadDataFromBlock();
                    _isLoading = false;

                    // LoadDataFromBlock ставит cmbShapeNumber.Text, пока _isLoading=true, поэтому
                    // SelectionChanged (который обновляет картинку) сам себя блокирует своей же
                    // проверкой _isLoading - обновляем картинку вручную здесь
                    UpdateShapeImage();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error in Read: " + ex.Message);
            }
            finally
            {
                this.ShowDialog();
            }
        }

        // ====================================================================================
        // ОБРАБОТЧИК: btnSettings_Click
        // ====================================================================================
        // НАЗНАЧЕНИЕ: Открывает отдельное окно настроек
        // ====================================================================================
        // При нажатии кнопки "Settings" открывается отдельное окно SettingsWindow,
        // где пользователь может изменить все настройки приложения.
        // Настройки сохраняются в класс AppSettings, который доступен из любого места программы.
        // ====================================================================================
        private void btnSettings_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Создаем новое окно настроек
                SettingsWindow settingsWindow = new SettingsWindow();
                
                // Показываем окно как модальное (блокирует главное окно, пока не закроется)
                // ShowDialog() возвращает bool? (true/false/null):
                //   - true = окно закрыто с сохранением (DialogResult = true)
                //   - false = окно закрыто без сохранения (DialogResult = false)
                //   - null = окно закрыто крестиком
                bool? result = settingsWindow.ShowDialog();
                
                // Если настройки были сохранены (result == true), можно выполнить дополнительные действия
                // Например, обновить какие-то элементы интерфейса, если они зависят от настроек
                if (result == true)
                {
                    // Настройки сохранены в AppSettings, можно их использовать
                    // Здесь можно добавить логику обновления интерфейса, если нужно
                    // Например, если настройки влияют на отображение данных в главном окне
                }
            }
            catch (Exception ex)
            {
                // Если произошла ошибка при открытии окна настроек, показываем сообщение
                MessageBox.Show($"Error opening settings window: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private string GetValueOrTag(TextBox box)
        {
            if (box == null) return "0";
            if (box.Tag != null && box.Tag.ToString().Length > 0) return box.Tag.ToString();
            return box.Text;
        }

        private double ParseDouble(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return 0;
            text = text.Replace(".", ",");
            if (double.TryParse(text, out double res)) return res;
            return 0;
        }
    }
}