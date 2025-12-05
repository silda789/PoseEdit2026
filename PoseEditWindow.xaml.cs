using System;
using System.Collections.Generic;
using System.Windows; // База WPF
using System.Windows.Controls;
using System.Windows.Media; // Для цветов (Brush)
using System.Windows.Media.Imaging; // Для картинок (BitmapImage)

// AutoCAD
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using App = Autodesk.AutoCAD.ApplicationServices.Application;

namespace PoseEdit2026
{
    // Interaction logic for PoseEditWindow.xaml
    public partial class PoseEditWindow : Window
    {
        private ObjectId _currentBlockId;

        public PoseEditWindow(ObjectId blockId)
        {
            // Метод, который "читает" XAML и строит окно
            InitializeComponent();

            _currentBlockId = blockId;

            InitLists();
            LoadDataFromBlock();
        }

        private void InitLists()
        {
            // Заполняем комбобокс с номерами 00..99
            cmbShapeNumber.Items.Clear();
            for (int i = 0; i <= 99; i++)
            {
                cmbShapeNumber.Items.Add(i.ToString("D2"));
            }
        }

        // --- ЛОГИКА ЗАГРУЗКИ КАРТИНКИ В WPF ---
        private void UpdateShapeImage()
        {
            if (cmbShapeNumber.SelectedItem == null) return;

            string code = cmbShapeNumber.SelectedItem.ToString();

            // ВАЖНО: Здесь должно быть имя сборки (имя проекта)
            string assemblyName = "PoseEdit2026";

            // Путь: pack://application:,,,/PoseEdit2026;component/Resources/Shape_01.png
            string uriPath = $"pack://application:,,,/{assemblyName};component/Resources/Shape_{code}.png";

            try
            {
                // Пытаемся создать картинку по ссылке
                BitmapImage bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.UriSource = new Uri(uriPath);
                bitmap.CacheOption = BitmapCacheOption.OnLoad; // Грузим в память сразу
                bitmap.EndInit();

                imgPreview.Source = bitmap; // Назначаем картинку элементу Image в XAML
            }
            catch
            {
                // Если картинки нет - очищаем
                imgPreview.Source = null;
            }
        }

        // Событие выбора в списке
        private void cmbShapeNumber_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateShapeImage();

            // Синхронизация с текстовым полем
            if (cmbShapeNumber.SelectedItem != null)
                txtType.Text = cmbShapeNumber.SelectedItem.ToString();
        }

        // ... Сюда нужно перенести методы LoadDataFromBlock, btnUpdate_Click и т.д.
        // Логика 1-в-1 как была в WinForms, только меняем:
        // - MessageBox.Show(...) на System.Windows.MessageBox.Show(...)
        // - Color.Yellow на Brushes.LightYellow (в WPF цвета - это Кисти/Brushes)
        // - TextBox.BackColor на TextBox.Background
    }
}