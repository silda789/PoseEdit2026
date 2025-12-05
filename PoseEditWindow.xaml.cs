// #nullable disable — эта директива говорит компилятору: 
// "Не ругайся, если я где-то могу получить null (пустое значение)".
// В .NET 8 проверки очень строгие, и без этой строки будет куча желтых предупреждений.
#nullable disable

using AutoCAD2024Final;
// --- ПРОСТРАНСТВА ИМЕН AUTOCAD ---
using Autodesk.AutoCAD.DatabaseServices; // Работа с базой чертежа (Transaction, ObjectId)
using Autodesk.AutoCAD.EditorInput; // Ввод пользователя (выбор объектов, клики)
using System;
using System.Collections.Generic;
using System.Windows; // Главное пространство имен WPF (Window, MessageBox, RoutedEventArgs)
using System.Windows.Controls; // Элементы управления (Button, TextBox, ComboBox)
using System.Windows.Media; // Графика WPF (Brushes, Colors)
using System.Windows.Media.Imaging; // Работа с изображениями (BitmapImage)
// Создаем псевдоним "App", чтобы не писать длинное Autodesk...Application каждый раз
using App = Autodesk.AutoCAD.ApplicationServices.Application;

namespace PoseEdit2026
{
    // public partial class:
    // "partial" (частичный) означает, что код этого класса разделен на два файла.
    // 1. Этот файл (.cs) — наша логика.
    // 2. Скрытый файл (.g.i.cs) — генерируется автоматически из XAML, там создаются кнопки.
    // Наследуемся от Window, так как это отдельное окно WPF.
    public partial class PoseEditWindow : Window
    {
        // Поле класса для хранения ID блока.
        // ObjectId — это уникальный "паспорт" объекта в базе AutoCAD.
        // Храним его здесь, чтобы видеть во всех методах (кнопках) этого окна.
        private ObjectId _currentBlockId;

        // --- КОНСТРУКТОР ---
        // Этот метод вызывается первым при создании окна: new PoseEditWindow(id)
        public PoseEditWindow(ObjectId blockId)
        {
            // InitializeComponent() — самый важный метод.
            // Он берет твой XAML (разметку) и превращает её в реальные кнопки в памяти.
            // Если его удалить, окно будет пустым.
            InitializeComponent();

            _currentBlockId = blockId; // Запоминаем ID блока

            InitLists();        // 1. Заполняем выпадающие списки
            LoadDataFromBlock(); // 2. Читаем данные из блока в поля
        }

        // Метод для заполнения списков начальными данными
        private void InitLists()
        {
            // Очищаем ComboBox перед заполнением, чтобы не дублировать, если метод вызовут дважды
            cmbShapeNumber.Items.Clear();

            // Цикл от 0 до 99
            for (int i = 0; i <= 99; i++)
            {
                // i.ToString("D2") — форматирование числа.
                // "D" (Decimal), "2" (знака).
                // 5 превратится в "05", 10 останется "10".
                cmbShapeNumber.Items.Add(i.ToString("D2"));
            }
        }

        // --- ЧТЕНИЕ ДАННЫХ ИЗ БЛОКА ---
        private void LoadDataFromBlock()
        {
            try
            {
                // Вызываем наш помощник BlockHelper.
                // Он возвращает Словарь (Dictionary) — это как таблица из двух колонок:
                // Ключ ("A") | Значение ("1200")
                var attributes = BlockHelper.GetAttributes(_currentBlockId);

                // Заполняем простые поля
                // ContainsKey проверяет, есть ли такой атрибут в блоке, чтобы не было ошибки
                if (attributes.ContainsKey("POZ")) txtPose.Text = attributes["POZ"];
                if (attributes.ContainsKey("BOY")) txtLength.Text = attributes["BOY"];

                // В WPF пока не делали комбобокс материалов, но если он есть в XAML:
                // if (attributes.ContainsKey("MALZEME")) cmbMaterial.Text = attributes["MALZEME"];

                // --- Логика ТИПА и КАРТИНКИ ---
                if (attributes.ContainsKey("TIP"))
                {
                    txtType.Text = attributes["TIP"];

                    // Синхронизация: если в блоке записан тип "21", 
                    // мы должны выбрать "21" в выпадающем списке.
                    if (cmbShapeNumber.Items.Contains(attributes["TIP"]))
                    {
                        // Присвоение значения свойству Text у ComboBox автоматически
                        // запустит событие SelectionChanged, и картинка обновится сама!
                        cmbShapeNumber.Text = attributes["TIP"];
                    }
                }

                // --- Заполнение размеров с поддержкой "Умных полей" (Field) ---
                // Мы используем метод SetValueOrTag (описан ниже), который проверяет:
                // это просто текст "1000" или формула "%<\AcObjProp..."?
                if (attributes.ContainsKey("A")) SetValueOrTag(txtA, attributes["A"]);
                if (attributes.ContainsKey("B")) SetValueOrTag(txtB, attributes["B"]);
                if (attributes.ContainsKey("C")) SetValueOrTag(txtC, attributes["C"]);
                if (attributes.ContainsKey("D")) SetValueOrTag(txtD, attributes["D"]);
                if (attributes.ContainsKey("E")) SetValueOrTag(txtE, attributes["E"]);
                if (attributes.ContainsKey("R")) SetValueOrTag(txtR, attributes["R"]);

                // --- Разбор строки TB ---
                // Если есть строка вида "2x5Ø12/200", её нужно порезать на части
                if (attributes.ContainsKey("TB")) ParseTB(attributes["TB"]);
            }
            catch (Exception ex)
            {
                // В WPF MessageBox находится в пространстве System.Windows
                MessageBox.Show("Error loading data: " + ex.Message);
            }
        }

        // Вспомогательный метод для "Умных полей"
        private void SetValueOrTag(TextBox box, string val)
        {
            // Если строка начинается с кода поля AutoCAD...
            if (val.StartsWith("%<\\AcObjProp"))
            {
                // ...мы прячем формулу в свойство Tag (скрытый карман элемента)
                box.Tag = val;
                // А пользователю показываем слово FIELD (или можно попытаться вычислить значение)
                box.Text = "FIELD";
                // И красим фон в желтый, чтобы было видно связь.
                // В WPF цвета — это "Кисти" (Brushes).
                box.Background = Brushes.LightYellow;
            }
            else
            {
                // Если это обычный текст — просто показываем его
                box.Text = val;
                box.Tag = null; // Очищаем скрытый карман
                box.Background = Brushes.White;
            }
        }

        // Парсер строки описания (TB)
        private void ParseTB(string tbText)
        {
            if (string.IsNullOrEmpty(tbText)) return;

            // Ищем индекс (позицию) символа диаметра
            int diameterIndex = tbText.IndexOf("Ø");
            if (diameterIndex == -1) diameterIndex = tbText.IndexOf("%%C"); // Код для автокада
            if (diameterIndex == -1) diameterIndex = tbText.IndexOf("%%c");

            if (diameterIndex > -1)
            {
                // Берем левую часть (Количество)
                string leftPart = tbText.Substring(0, diameterIndex);

                // Если есть 'x', значит есть множитель групп
                if (leftPart.ToLower().Contains("x"))
                {
                    string[] parts = leftPart.ToLower().Split('x');
                    txtItemMult.Text = parts[0];
                    txtItem.Text = parts[1];
                }
                else
                {
                    txtItemMult.Text = "";
                    txtItem.Text = leftPart;
                }

                // Определяем длину символа (Ø = 1, %%C = 3)
                int symLength = (tbText.Contains("%%C") || tbText.Contains("%%c")) ? 3 : 1;

                // Берем правую часть (Диаметр и Шаг)
                string rightPart = tbText.Substring(diameterIndex + symLength);

                if (rightPart.Contains("/"))
                {
                    string[] dParts = rightPart.Split('/');
                    txtDiameter.Text = dParts[0];
                    // txtSpace.Text = dParts[1]; // Если есть поле шага, раскомментируй
                }
                else
                {
                    txtDiameter.Text = rightPart;
                }
            }
        }

        // --- РАБОТА С КАРТИНКАМИ В WPF ---
        // Это самая сложная часть в WPF по сравнению с WinForms.
        // Здесь картинки загружаются через URI (Uniform Resource Identifier).
        private void UpdateShapeImage()
        {
            // Если в списке ничего не выбрано — выходим
            if (cmbShapeNumber.SelectedItem == null) return;

            string code = cmbShapeNumber.SelectedItem.ToString();

            // ВАЖНО: Имя сборки должно совпадать с названием проекта!
            string assemblyName = "PoseEdit2026";

            // Формируем путь к ресурсу внутри DLL.
            // pack://application:,,,/ — это магическое заклинание WPF, говорящее "ищи внутри текущего приложения".
            // component/ — обязательная часть пути.
            string uriPath = $"pack://application:,,,/{assemblyName};component/Resources/Shape_{code}.png";

            try
            {
                // Создаем новый объект картинки
                BitmapImage bitmap = new BitmapImage();
                bitmap.BeginInit(); // Начинаем инициализацию
                bitmap.UriSource = new Uri(uriPath); // Указываем путь

                // CacheOption.OnLoad заставляет WPF загрузить картинку в память сразу же.
                // Это важно, чтобы не блокировать файл.
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.EndInit(); // Заканчиваем инициализацию

                // Присваиваем картинку элементу Image в XAML (imgPreview)
                imgPreview.Source = bitmap;
            }
            catch
            {
                // Если картинки нет (файл не найден) — очищаем Image
                imgPreview.Source = null;
            }
        }

        // Событие: Выбор в списке изменился
        // (WPF использует SelectionChangedEventArgs, а не EventArgs)
        private void cmbShapeNumber_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateShapeImage(); // Обновляем картинку

            // Синхронизация: пишем выбранный номер в текстовое поле Type
            if (cmbShapeNumber.SelectedItem != null)
                txtType.Text = cmbShapeNumber.SelectedItem.ToString();
        }

        // Событие: Текст в поле Type изменился
        private void txtType_TextChanged(object sender, TextChangedEventArgs e)
        {
            string currentType = txtType.Text.Trim();
            // Если такой тип есть в списке, выбираем его (это обновит картинку)
            if (cmbShapeNumber.Items.Contains(currentType))
            {
                if (cmbShapeNumber.Text != currentType)
                    cmbShapeNumber.Text = currentType;
            }
        }

        // --- КНОПКИ УПРАВЛЕНИЯ ---

        // Кнопка UPDATE POSE (Сохранить)
        private void btnUpdate_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // 1. Собираем строку TB
                string tbResult = "";
                if (!string.IsNullOrEmpty(txtItemMult.Text)) tbResult += txtItemMult.Text + "x";
                tbResult += txtItem.Text + "Ø" + txtDiameter.Text;

                // 2. Создаем словарь для записи
                Dictionary<string, string> newValues = new Dictionary<string, string>();
                newValues["POZ"] = txtPose.Text;
                newValues["TB"] = tbResult;
                newValues["BOY"] = txtLength.Text;
                newValues["TIP"] = txtType.Text;

                // 3. Собираем размеры.
                // Используем GetValueOrTag, чтобы если там формула поля, записать формулу, а не текст "FIELD".
                newValues["A"] = GetValueOrTag(txtA);
                newValues["B"] = GetValueOrTag(txtB);
                newValues["C"] = GetValueOrTag(txtC);
                newValues["D"] = GetValueOrTag(txtD);
                newValues["E"] = GetValueOrTag(txtE);
                newValues["R"] = GetValueOrTag(txtR);

                // 4. Пишем в блок
                BlockHelper.SetAttributes(_currentBlockId, newValues);

                // 5. Обновляем поля в AutoCAD (чтобы пересчитались формулы)
                Document doc = App.DocumentManager.MdiActiveDocument;
                doc.Editor.Command("_.UPDATEFIELD", _currentBlockId, "");

                // 6. Закрываем окно с результатом "Успех"
                // В WPF DialogResult — это bool? (Nullable bool).
                this.DialogResult = true;
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error updating: " + ex.Message);
            }
        }

        // Кнопка DISCARD (Отмена)
        private void btnDiscard_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false; // "Неудача/Отмена"
            this.Close();
        }

        // Кнопка CALCULATION (Расчет)
        private void btnCalc_Click(object sender, RoutedEventArgs e)
        {
            // Парсим значения (ParseDouble превращает текст в число, учитывая запятые)
            double a = ParseDouble(txtA.Text);
            double b = ParseDouble(txtB.Text);
            double c = ParseDouble(txtC.Text);

            // Здесь должна быть логика switch(type), как мы писали раньше.
            // Для примера простое сложение:
            double total = a + b + c; // Допиши сюда остальные слагаемые

            txtLength.Text = "L=" + total.ToString("0");
        }

        // --- ЛОГИКА LINK (Связь с размерами) ---
        // В WPF с модальными окнами (ShowDialog) есть нюанс:
        // Нельзя просто кликнуть в Автокад. Окно нужно Скрывать (Hide), а потом Показывать (Show).
        private void LinkDimensionToTextBox(TextBox targetBox)
        {
            this.Hide(); // 1. Прячем окно WPF

            try
            {
                Editor ed = App.DocumentManager.MdiActiveDocument.Editor;

                // 2. Запрашиваем выбор объекта
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

                    // 3. Читаем реальное значение объекта
                    using (Transaction tr = objId.Database.TransactionManager.StartTransaction())
                    {
                        Entity ent = tr.GetObject(objId, OpenMode.ForRead) as Entity;
                        if (ent is Line l) val = l.Length;
                        else if (ent is Polyline p) val = p.Length;
                        else if (ent is Dimension d)
                        {
                            prop = "Measurement"; // У размеров длина — это Measurement
                            // В .NET 8 использование dynamic или Reflection может быть сложнее, 
                            // упрощенный пример:
                            try { val = (double)ent.GetType().GetProperty("Measurement").GetValue(ent); } catch { }
                        }
                        tr.Commit();
                    }

                    // 4. Формируем код Поля (Field Expression)
                    // %lu2%pr0 — десятичный формат, точность 0 знаков.
                    string fieldCode = string.Format("%<\\AcObjProp Object(%<\\_ObjId {0}>%).{1} \\f \"%lu2%pr0\">%", objId.OldIdPtr, prop);

                    // 5. Записываем данные в TextBox
                    targetBox.Text = val.ToString("0"); // Число (видим)
                    targetBox.Tag = fieldCode;          // Формула (скрыта)
                    targetBox.Background = Brushes.LightYellow; // Красим фон
                }
            }
            finally
            {
                // Блок finally выполняется всегда, даже если была ошибка.
                // Это гарантирует, что окно вернется на экран.
                this.ShowDialog();
            }
        }

        // Обработчики нажатий Link (стрелочные функции => для краткости)
        private void btnLinkA_Click(object sender, RoutedEventArgs e) => LinkDimensionToTextBox(txtA);
        private void btnLinkB_Click(object sender, RoutedEventArgs e) => LinkDimensionToTextBox(txtB);
        private void btnLinkC_Click(object sender, RoutedEventArgs e) => LinkDimensionToTextBox(txtC);
        private void btnLinkD_Click(object sender, RoutedEventArgs e) => LinkDimensionToTextBox(txtD);
        private void btnLinkE_Click(object sender, RoutedEventArgs e) => LinkDimensionToTextBox(txtE);
        private void btnLinkR_Click(object sender, RoutedEventArgs e) => LinkDimensionToTextBox(txtR);

        // Обработчик фокуса: если пользователь кликнул в желтое поле и начал писать,
        // мы сбрасываем связь (удаляем формулу), так как он вводит ручное значение.
        private void txtDimension_GotFocus(object sender, RoutedEventArgs e)
        {
            if (sender is TextBox box)
            {
                box.Tag = null; // Удаляем формулу
                box.Background = Brushes.White; // Возвращаем белый цвет
            }
        }

        // Кнопка DETERMINATION (Распознавание)
        private void btnDetermination_Click(object sender, RoutedEventArgs e)
        {
            this.Hide(); // Скрываем окно
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
                    // Вызываем наш "Мозг" (RebarRecognizer)
                    var result = RebarRecognizer.Recognize(res.ObjectId);

                    // Заполняем поля
                    txtType.Text = result.Type;
                    txtLength.Text = result.Length;
                    txtA.Text = result.A;
                    txtB.Text = result.B;
                    txtC.Text = result.C;
                    txtD.Text = result.D;
                    txtE.Text = result.E;
                    txtR.Text = result.R;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error: " + ex.Message);
            }
            finally
            {
                this.ShowDialog(); // Возвращаем окно
            }
        }

        // Заглушки для кнопок, которые мы еще не реализовали
        private void btnUpdateAll_Click(object sender, RoutedEventArgs e) { MessageBox.Show("Not implemented yet"); }
        private void btnRead_Click(object sender, RoutedEventArgs e) { MessageBox.Show("Not implemented yet"); }

        // --- ВСПОМОГАТЕЛЬНЫЕ МЕТОДЫ (HELPERS) ---

        // Метод выбора: вернуть Tag (формулу) или Text (значение)
        private string GetValueOrTag(TextBox box)
        {
            // Если Tag не пустой — берем его
            if (box.Tag != null && box.Tag.ToString().Length > 0) return box.Tag.ToString();
            // Иначе берем текст
            return box.Text;
        }

        // Безопасный парсинг числа из строки
        private double ParseDouble(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return 0;
            // Заменяем точку на запятую (или наоборот), чтобы не зависеть от региональных настроек Windows
            text = text.Replace(".", ",");
            if (double.TryParse(text, out double res)) return res;
            return 0;
        }
    }
}