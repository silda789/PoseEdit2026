// ====================================================================================
// ФАЙЛ: LayerSettingsWindow.xaml.cs
// НАЗНАЧЕНИЕ: Окно для настройки создания стандартных слоев в AutoCAD
// ====================================================================================
// Это окно позволяет пользователю:
//   - Указать префикс для стандартных слоев (например: "std", "ren", "model")
//   - Выбрать, какие стандартные слои создавать
//   - Все параметры слоев (цвет, тип линии, вес) уже заданы в стандарте LAYERS-Model.pdf
//
// ПРИНЦИП РАБОТЫ:
//   1. Загружаются все стандартные слои из StandardLayers.GetStandardLayers()
//   2. Пользователь указывает префикс (например: "std")
//   3. Пользователь выбирает слои для создания (чекбоксы)
//   4. При нажатии "Create Layers" создается объект LayerSettings с выбранными слоями
//   5. LayerSettings передается в LayerCreator для создания слоев
// ====================================================================================

#nullable disable

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using Autodesk.AutoCAD.Colors;
using Autodesk.AutoCAD.DatabaseServices;

namespace PoseEdit2026
{
    // ====================================================================================
    // КЛАСС: LayerItemViewModel
    // ====================================================================================
    // НАЗНАЧЕНИЕ: ViewModel для отображения слоя в списке
    // ====================================================================================
    public class LayerItemViewModel : INotifyPropertyChanged
    {
        private bool _isSelected = true;
        private LayerDefinition _layerDef;

        public LayerItemViewModel(LayerDefinition layerDef)
        {
            _layerDef = layerDef ?? throw new ArgumentNullException(nameof(layerDef));
        }

        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                if (_isSelected != value)
                {
                    _isSelected = value;
                    OnPropertyChanged();
                }
            }
        }

        public string Name => _layerDef.Name;
        public short ColorIndex => _layerDef.ColorIndex;
        public string Linetype => _layerDef.Linetype;
        public string LineWeightDisplay => _layerDef.LineWeightMM.HasValue 
            ? $"{_layerDef.LineWeightMM.Value:F2} mm" 
            : "ByLayer";
        public string Description => _layerDef.Description;

        public LayerDefinition LayerDefinition => _layerDef;

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    // ====================================================================================
    // КЛАСС: LayerSettingsWindow
    // ====================================================================================
    // Окно для настройки создания стандартных слоев
    // ====================================================================================
    public partial class LayerSettingsWindow : Window
    {
        private ObservableCollection<LayerItemViewModel> _layerItems;

        // ====================================================================================
        // КОНСТРУКТОР
        // ====================================================================================
        public LayerSettingsWindow()
        {
            InitializeComponent();
            
            // Загружаем стандартные слои
            LoadStandardLayers();
            
            // Обновляем счетчик выбранных слоев
            UpdateSelectedCount();
        }

        // ====================================================================================
        // МЕТОД: LoadStandardLayers
        // ====================================================================================
        // НАЗНАЧЕНИЕ: Загружает стандартные слои и отображает их в списке
        // ====================================================================================
        private void LoadStandardLayers()
        {
            // Получаем список стандартных слоев
            List<LayerDefinition> standardLayers = StandardLayers.GetStandardLayers();
            
            // Создаем коллекцию ViewModel для отображения
            _layerItems = new ObservableCollection<LayerItemViewModel>();
            
            foreach (LayerDefinition layerDef in standardLayers)
            {
                LayerItemViewModel item = new LayerItemViewModel(layerDef);
                item.PropertyChanged += (s, e) =>
                {
                    if (e.PropertyName == nameof(LayerItemViewModel.IsSelected))
                    {
                        UpdateSelectedCount();
                    }
                };
                _layerItems.Add(item);
            }
            
            // Привязываем коллекцию к ItemsControl
            itemsControlLayers.ItemsSource = _layerItems;
        }

        // ====================================================================================
        // МЕТОД: UpdateSelectedCount
        // ====================================================================================
        // НАЗНАЧЕНИЕ: Обновляет счетчик выбранных слоев
        // ====================================================================================
        private void UpdateSelectedCount()
        {
            if (_layerItems == null) return;
            
            int selectedCount = _layerItems.Count(item => item.IsSelected);
            txtSelectedCount.Text = $"{selectedCount} of {_layerItems.Count} layers selected";
        }

        // ====================================================================================
        // МЕТОД: GetSettings
        // ====================================================================================
        // НАЗНАЧЕНИЕ: Создает объект LayerSettings на основе выбранных слоев и префикса
        //
        // ВОЗВРАЩАЕТ:
        //   LayerSettings - настройки для создания слоев или null, если данные невалидны
        // ====================================================================================
        public LayerSettings GetSettings()
        {
            try
            {
                if (_layerItems == null || _layerItems.Count == 0)
                {
                    MessageBox.Show("No layers available.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return null;
                }

                // Получаем выбранные слои
                List<LayerDefinition> selectedLayers = _layerItems
                    .Where(item => item.IsSelected)
                    .Select(item => item.LayerDefinition)
                    .ToList();

                if (selectedLayers.Count == 0)
                {
                    MessageBox.Show("Please select at least one layer to create.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return null;
                }

                // Проверяем префикс (может быть пустым)
                string prefix = txtPrefix?.Text?.Trim() ?? "";

                // Создаем настройки
                LayerSettings settings = new LayerSettings
                {
                    LayerDefinitions = selectedLayers,
                    Prefix = prefix,
                    SkipExisting = chkSkipExisting?.IsChecked == true
                };

                return settings;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error creating settings: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return null;
            }
        }

        // ====================================================================================
        // ОБРАБОТЧИК: btnOK_Click
        // ====================================================================================
        // НАЗНАЧЕНИЕ: Сохраняет настройки и закрывает окно
        // ====================================================================================
        private void btnOK_Click(object sender, RoutedEventArgs e)
        {
            LayerSettings settings = GetSettings();
            if (settings != null)
            {
                this.DialogResult = true;
                this.Close();
            }
        }

        // ====================================================================================
        // ОБРАБОТЧИК: btnCancel_Click
        // ====================================================================================
        // НАЗНАЧЕНИЕ: Закрывает окно без сохранения
        // ====================================================================================
        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }

        // ====================================================================================
        // ОБРАБОТЧИК: btnSelectAll_Click
        // ====================================================================================
        // НАЗНАЧЕНИЕ: Выбирает все слои
        // ====================================================================================
        private void btnSelectAll_Click(object sender, RoutedEventArgs e)
        {
            if (_layerItems == null) return;
            
            foreach (LayerItemViewModel item in _layerItems)
            {
                item.IsSelected = true;
            }
        }

        // ====================================================================================
        // ОБРАБОТЧИК: btnDeselectAll_Click
        // ====================================================================================
        // НАЗНАЧЕНИЕ: Снимает выбор со всех слоев
        // ====================================================================================
        private void btnDeselectAll_Click(object sender, RoutedEventArgs e)
        {
            if (_layerItems == null) return;
            
            foreach (LayerItemViewModel item in _layerItems)
            {
                item.IsSelected = false;
            }
        }

        // ====================================================================================
        // ОБРАБОТЧИК: txtPrefix_TextChanged
        // ====================================================================================
        // НАЗНАЧЕНИЕ: Обновляет предварительный просмотр при изменении префикса
        // ====================================================================================
        private void txtPrefix_TextChanged(object sender, TextChangedEventArgs e)
        {
            // Можно добавить валидацию префикса или предварительный просмотр
            // Пока просто обновляем счетчик
            UpdateSelectedCount();
        }
    }
}

