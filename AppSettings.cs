#nullable disable

using System;

namespace PoseEdit2026
{
    /// <summary>
    /// Класс для хранения настроек приложения
    /// Настройки сохраняются в памяти и доступны из любого места программы
    /// </summary>
    public static class AppSettings
    {
        // Единицы измерения: 1 (м), 100 (см), 1000 (мм)
        private static double _drawingUnit = 1000.0;
        
        // Масштаб чертежа (например: 50, 100, 200)
        private static double _sheetScale = 50.0;
        
        // Язык таблиц: "rus", "eng", "re"
        private static string _tableLanguage = "rus";
        
        // Настройки размещения таблиц: [Yerlesim_1, Yerlesim_2, Yerlesim_3]
        private static string[] _tablePlacement = { "0", "0", "1" };
        
        // Имя проекта
        private static string _projectName = "";

        // Свойства с публичным доступом
        public static double DrawingUnit
        {
            get => _drawingUnit;
            set => _drawingUnit = value;
        }

        public static double SheetScale
        {
            get => _sheetScale;
            set => _sheetScale = value;
        }

        public static string TableLanguage
        {
            get => _tableLanguage;
            set => _tableLanguage = value;
        }

        public static string[] TablePlacement
        {
            get => _tablePlacement;
            set => _tablePlacement = value ?? new string[] { "0", "0", "1" };
        }

        public static string ProjectName
        {
            get => _projectName;
            set => _projectName = value ?? "";
        }

        /// <summary>
        /// Загружает настройки из файлов (для совместимости со старым кодом)
        /// </summary>
        public static void LoadFromFiles()
        {
            // Эта функция может быть вызвана при старте, если нужно загрузить сохраненные настройки
            // Пока оставляем значения по умолчанию
        }

        /// <summary>
        /// Сохраняет настройки в файлы (для совместимости со старым кодом)
        /// </summary>
        public static void SaveToFiles()
        {
            // Эта функция может быть вызвана при закрытии, если нужно сохранить настройки
            // Пока не реализовано, так как настройки хранятся в памяти
        }
    }
}

