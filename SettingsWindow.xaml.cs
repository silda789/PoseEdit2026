// ====================================================================================
// ФАЙЛ: SettingsWindow.xaml.cs
// НАЗНАЧЕНИЕ: Окно настроек приложения (отдельное от главного окна)
// ====================================================================================
// Это окно содержит все настройки приложения:
//   - Масштаб чертежа (Sheet Scale)
//   - Единицы измерения (Drawing Unit)
//   - Язык таблиц (Quantity Table Language)
//   - Имя проекта (Project Name)
//   - Размещение таблиц (Table Placement)
//
// ВАЖНО: Настройки сохраняются в класс AppSettings, который доступен из любого места программы.
// ====================================================================================

#nullable disable
using System;
using System.Linq; // Для LINQ методов
using System.Windows;
using System.Windows.Controls;
using PoseEdit2026; // Для доступа к AppSettings

namespace PoseEdit2026
{
    // ====================================================================================
    // КЛАСС: SettingsWindow
    // ====================================================================================
    // Это окно настроек, которое открывается отдельно при нажатии кнопки "Settings"
    // в главном окне PoseEditWindow.
    //
    // ПРИНЦИП РАБОТЫ:
    //   1. При открытии окна загружаются текущие настройки из AppSettings
    //   2. Пользователь изменяет настройки
    //   3. При нажатии "Save Settings" настройки сохраняются в AppSettings
    //   4. Настройки доступны из любого места программы через AppSettings
    // ====================================================================================
    public partial class SettingsWindow : Window
    {
        // ====================================================================================
        // КОНСТРУКТОР
        // ====================================================================================
        // Инициализирует окно и загружает текущие настройки
        // ====================================================================================
        public SettingsWindow()
        {
            // InitializeComponent() - это метод, который загружает XAML разметку
            // Он создает все элементы интерфейса (TextBox, ComboBox, Button и т.д.)
            InitializeComponent();
            
            // Загружаем текущие настройки из AppSettings в интерфейс
            LoadSettings();
        }

        // ====================================================================================
        // МЕТОД: LoadSettings
        // ====================================================================================
        // НАЗНАЧЕНИЕ: Загружает текущие настройки из AppSettings и отображает их в интерфейсе
        // ====================================================================================
        private void LoadSettings()
        {
            try
            {
                // Загружаем масштаб чертежа
                if (txtSheetScale != null)
                {
                    txtSheetScale.Text = AppSettings.SheetScale.ToString();
                }

                // Устанавливаем единицы измерения
                // AppSettings.DrawingUnit может быть: 1 (м), 100 (см), 1000 (мм)
                if (rbMeter != null && rbCentimeter != null && rbMillimeter != null)
                {
                    switch (AppSettings.DrawingUnit)
                    {
                        case 1:
                            rbMeter.IsChecked = true;
                            break;
                        case 100:
                            rbCentimeter.IsChecked = true;
                            break;
                        case 1000:
                        default:
                            rbMillimeter.IsChecked = true;
                            break;
                    }
                }

                // Устанавливаем язык таблиц
                // AppSettings.TableLanguage может быть: "rus", "eng", "re"
                if (rbRus != null && rbEng != null && rbRusEng != null)
                {
                    switch (AppSettings.TableLanguage)
                    {
                        case "eng":
                            rbEng.IsChecked = true;
                            break;
                        case "re":
                            rbRusEng.IsChecked = true;
                            break;
                        case "rus":
                        default:
                            rbRus.IsChecked = true;
                            break;
                    }
                }

                // Устанавливаем размещение таблиц
                // AppSettings.TablePlacement - это массив из 3 элементов ["0", "0", "1"]
                if (rbPlacement1 != null && rbPlacement2 != null && rbPlacement3 != null)
                {
                    string[] placement = AppSettings.TablePlacement;
                    if (placement != null && placement.Length >= 3)
                    {
                        // Проверяем, какой элемент массива равен "1" (активен)
                        if (placement[0] == "1") rbPlacement1.IsChecked = true;
                        else if (placement[1] == "1") rbPlacement2.IsChecked = true;
                        else if (placement[2] == "1") rbPlacement3.IsChecked = true;
                        else rbPlacement3.IsChecked = true; // По умолчанию
                    }
                    else
                    {
                        rbPlacement3.IsChecked = true;
                    }
                }

                // Устанавливаем имя проекта
                if (cmbProjectName != null && !string.IsNullOrEmpty(AppSettings.ProjectName))
                {
                    cmbProjectName.Text = AppSettings.ProjectName;
                    
                    // Добавляем имя проекта в список, если его там еще нет
                    if (!cmbProjectName.Items.Contains(AppSettings.ProjectName))
                    {
                        cmbProjectName.Items.Add(AppSettings.ProjectName);
                    }
                }
            }
            catch (Exception ex)
            {
                // Если произошла ошибка при загрузке настроек, показываем сообщение
                MessageBox.Show($"Error loading settings: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // ====================================================================================
        // ОБРАБОТЧИК: btnSaveSettings_Click
        // ====================================================================================
        // НАЗНАЧЕНИЕ: Сохраняет все настройки из интерфейса в AppSettings
        // ====================================================================================
        private void btnSaveSettings_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Сохраняем масштаб чертежа
                // Пытаемся преобразовать текст из TextBox в число (double)
                if (txtSheetScale != null && double.TryParse(txtSheetScale.Text, out double scale))
                {
                    AppSettings.SheetScale = scale;
                }

                // Сохраняем единицы измерения
                // Проверяем, какая RadioButton выбрана
                if (rbMeter != null && rbCentimeter != null && rbMillimeter != null)
                {
                    if (rbMeter.IsChecked == true) AppSettings.DrawingUnit = 1;      // Метры
                    else if (rbCentimeter.IsChecked == true) AppSettings.DrawingUnit = 100;  // Сантиметры
                    else if (rbMillimeter.IsChecked == true) AppSettings.DrawingUnit = 1000; // Миллиметры
                }

                // Сохраняем язык таблиц
                if (rbRus != null && rbEng != null && rbRusEng != null)
                {
                    if (rbRus.IsChecked == true) AppSettings.TableLanguage = "rus";        // Русский
                    else if (rbEng.IsChecked == true) AppSettings.TableLanguage = "eng";   // Английский
                    else if (rbRusEng.IsChecked == true) AppSettings.TableLanguage = "re"; // Русский+Английский
                }

                // Сохраняем размещение таблиц
                // C# 12: Используем collection expression для инициализации массива
                string[] placement = ["0", "0", "0"]; // По умолчанию все отключены
                if (rbPlacement1 != null && rbPlacement1.IsChecked == true) placement[0] = "1";
                else if (rbPlacement2 != null && rbPlacement2.IsChecked == true) placement[1] = "1";
                else if (rbPlacement3 != null && rbPlacement3.IsChecked == true) placement[2] = "1";
                AppSettings.TablePlacement = placement;

                // Сохраняем имя проекта
                if (cmbProjectName != null)
                {
                    AppSettings.ProjectName = cmbProjectName.Text ?? ""; // Если null, сохраняем пустую строку
                }

                // Сохраняем в файл на диске
                AppSettings.SaveToFiles();

                // Показываем сообщение об успешном сохранении
                MessageBox.Show("Settings saved successfully!", "Settings", MessageBoxButton.OK, MessageBoxImage.Information);

                // Закрываем окно (DialogResult = true означает "настройки сохранены")
                this.DialogResult = true;
                this.Close();
            }
            catch (Exception ex)
            {
                // Если произошла ошибка, показываем сообщение
                MessageBox.Show($"Error saving settings: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // ====================================================================================
        // ОБРАБОТЧИК: btnCancel_Click
        // ====================================================================================
        // НАЗНАЧЕНИЕ: Закрывает окно без сохранения изменений
        // ====================================================================================
        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            // Просто закрываем окно без сохранения (DialogResult = false)
            this.DialogResult = false;
            this.Close();
        }

        // ====================================================================================
        // ОБРАБОТЧИК: rbUnit_Checked
        // ====================================================================================
        // НАЗНАЧЕНИЕ: Обновляет настройки единиц измерения при изменении RadioButton
        // ====================================================================================
        private void rbUnit_Checked(object sender, RoutedEventArgs e)
        {
            // Обновляем настройки сразу при изменении (без необходимости нажимать "Save")
            if (rbMeter != null && rbCentimeter != null && rbMillimeter != null)
            {
                if (rbMeter.IsChecked == true) AppSettings.DrawingUnit = 1;
                else if (rbCentimeter.IsChecked == true) AppSettings.DrawingUnit = 100;
                else if (rbMillimeter.IsChecked == true) AppSettings.DrawingUnit = 1000;
            }
        }

        // ====================================================================================
        // ОБРАБОТЧИК: rbLanguage_Checked
        // ====================================================================================
        // НАЗНАЧЕНИЕ: Обновляет настройки языка таблиц при изменении RadioButton
        // ====================================================================================
        private void rbLanguage_Checked(object sender, RoutedEventArgs e)
        {
            // Обновляем настройки сразу при изменении
            if (rbRus != null && rbEng != null && rbRusEng != null)
            {
                if (rbRus.IsChecked == true) AppSettings.TableLanguage = "rus";
                else if (rbEng.IsChecked == true) AppSettings.TableLanguage = "eng";
                else if (rbRusEng.IsChecked == true) AppSettings.TableLanguage = "re";
            }
        }

        // ====================================================================================
        // ОБРАБОТЧИК: rbPlacement_Checked
        // ====================================================================================
        // НАЗНАЧЕНИЕ: Обновляет настройки размещения таблиц при изменении RadioButton
        // ====================================================================================
        private void rbPlacement_Checked(object sender, RoutedEventArgs e)
        {
            // Обновляем настройки сразу при изменении
            // C# 12: Используем collection expression для инициализации массива
            string[] placement = ["0", "0", "0"]; // По умолчанию все отключены
            if (rbPlacement1 != null && rbPlacement1.IsChecked == true) placement[0] = "1";
            else if (rbPlacement2 != null && rbPlacement2.IsChecked == true) placement[1] = "1";
            else if (rbPlacement3 != null && rbPlacement3.IsChecked == true) placement[2] = "1";
            AppSettings.TablePlacement = placement;
        }

        // ====================================================================================
        // ОБРАБОТЧИК: btnAddProject_Click
        // ====================================================================================
        // НАЗНАЧЕНИЕ: Добавляет имя проекта в список проектов
        // ====================================================================================
        private void btnAddProject_Click(object sender, RoutedEventArgs e)
        {
            if (cmbProjectName == null) return;

            // Получаем имя проекта из TextBox (обрезаем пробелы)
            string projectName = cmbProjectName.Text?.Trim();
            
            // Проверяем, что имя проекта не пустое
            if (string.IsNullOrEmpty(projectName))
            {
                MessageBox.Show("Please enter a project name.", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Проверяем, нет ли уже такого проекта в списке
            if (!cmbProjectName.Items.Contains(projectName))
            {
                // Если проекта нет в списке - добавляем его
                cmbProjectName.Items.Add(projectName);
                cmbProjectName.Text = projectName; // Устанавливаем выбранный проект
                AppSettings.ProjectName = projectName; // Сохраняем в настройки
                MessageBox.Show($"Project '{projectName}' added to list.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                // Если проект уже есть в списке - просто выбираем его
                cmbProjectName.Text = projectName;
                AppSettings.ProjectName = projectName;
            }
        }

        // ====================================================================================
        // ОБРАБОТЧИК: cmbProjectName_SelectionChanged
        // ====================================================================================
        // НАЗНАЧЕНИЕ: Обновляет настройки имени проекта при изменении выбора в ComboBox
        // ====================================================================================
        private void cmbProjectName_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Обновляем настройки сразу при изменении
            if (cmbProjectName != null && !string.IsNullOrEmpty(cmbProjectName.Text))
            {
                AppSettings.ProjectName = cmbProjectName.Text;
            }
        }
    }
}

