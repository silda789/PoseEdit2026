// ====================================================================================
// ФАЙЛ: AppSettings.cs
// НАЗНАЧЕНИЕ: Класс для хранения глобальных настроек приложения
// ====================================================================================
// Этот класс хранит настройки приложения в памяти во время работы программы.
// Настройки доступны из любого места программы через статические свойства.
//
// ЧТО ХРАНИТСЯ:
//   - Единицы измерения (метры, сантиметры, миллиметры)
//   - Масштаб чертежа (1:50, 1:100, 1:200 и т.д.)
//   - Язык таблиц (русский, английский, русский+английский)
//   - Размещение таблиц (3 варианта размещения)
//   - Имя проекта
//
// КАК ИСПОЛЬЗОВАТЬ:
//   // Чтение настройки
//   double scale = AppSettings.SheetScale;
//
//   // Запись настройки
//   AppSettings.SheetScale = 100.0;
//
// ВАЖНО:
//   Настройки хранятся только в памяти (RAM). При закрытии программы они теряются.
//   Если нужно сохранить настройки между сеансами, используйте методы LoadFromFiles() и SaveToFiles()
//   (сейчас они не реализованы, но могут быть добавлены в будущем).
// ====================================================================================

#nullable disable

using System;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace PoseEdit2026
{
    // ====================================================================================
    // КЛАСС: AppSettings
    // ====================================================================================
    // "static" означает, что нам не нужно создавать объект этого класса.
    // Все свойства и методы доступны напрямую: AppSettings.SheetScale = 100;
    //
    // ПАТТЕРН "Singleton" (Одиночка):
    //   Этот класс использует паттерн Singleton - в программе может быть только один
    //   экземпляр настроек. Но так как класс static, экземпляров вообще не может быть.
    //   Это самый простой способ реализации Singleton.
    // ====================================================================================
    public static class AppSettings
    {
        // ====================================================================================
        // ПРИВАТНЫЕ ПОЛЯ (Хранилище данных)
        // ====================================================================================
        // Эти поля хранят реальные значения настроек.
        // Они приватные (private), чтобы нельзя было случайно изменить их напрямую.
        // Доступ к ним идет только через публичные свойства (get/set).
        
        // Единицы измерения: 1 (метры), 100 (сантиметры), 1000 (миллиметры)
        // По умолчанию: 1000 мм (миллиметры) - стандарт для чертежей в AutoCAD
        private static double _drawingUnit = 1000.0;
        
        // Масштаб чертежа (например: 50, 100, 200 означает масштабы 1:50, 1:100, 1:200)
        // По умолчанию: 50 (масштаб 1:50)
        private static double _sheetScale = 50.0;
        
        // Язык таблиц: "rus" (русский), "eng" (английский), "re" (русский+английский)
        // По умолчанию: "rus" (русский язык)
        private static string _tableLanguage = "rus";
        
        // Настройки размещения таблиц: массив из 3 элементов [Yerlesim_1, Yerlesim_2, Yerlesim_3]
        // Каждый элемент может быть "0" (отключено) или "1" (включено)
        // По умолчанию: ["0", "0", "1"] - активна только третья схема размещения
        // C# 12: Используем collection expression для инициализации массива
        private static string[] _tablePlacement = ["0", "0", "1"];
        
        // Имя проекта (например: "ЖК Солнечный", "Офисный центр" и т.д.)
        // По умолчанию: пустая строка (проект не задан)
        private static string _projectName = "";

        // ====================================================================================
        // ПУБЛИЧНЫЕ СВОЙСТВА (Интерфейс доступа к данным)
        // ====================================================================================
        // Свойства (Properties) - это способ контролировать доступ к приватным полям.
        // В get мы можем добавить проверки, форматирование, логирование и т.д.
        // В set мы можем добавить валидацию (проверку корректности значения).
        //
        // Синтаксис "=>" (стрелка) - это сокращенная форма записи свойств (expression-bodied members).
        // Это эквивалентно:
        //   public static double DrawingUnit
        //   {
        //       get { return _drawingUnit; }
        //       set { _drawingUnit = value; }
        //   }

        /// <summary>
        /// Единицы измерения чертежа
        /// Значения: 1 (метры), 100 (сантиметры), 1000 (миллиметры)
        /// </summary>
        /// <example>
        /// AppSettings.DrawingUnit = 1000; // Устанавливаем миллиметры
        /// double unit = AppSettings.DrawingUnit; // Читаем текущее значение
        /// </example>
        public static double DrawingUnit
        {
            get => _drawingUnit; // Возвращаем значение из приватного поля
            set => _drawingUnit = value; // Сохраняем значение в приватное поле
        }

        /// <summary>
        /// Масштаб чертежа (1:scale)
        /// Примеры: 50 означает масштаб 1:50, 100 означает масштаб 1:100
        /// </summary>
        /// <example>
        /// AppSettings.SheetScale = 100; // Устанавливаем масштаб 1:100
        /// </example>
        public static double SheetScale
        {
            get => _sheetScale;
            set => _sheetScale = value;
        }

        /// <summary>
        /// Язык таблиц спецификации
        /// Значения: "rus" (русский), "eng" (английский), "re" (русский+английский)
        /// </summary>
        /// <example>
        /// AppSettings.TableLanguage = "eng"; // Устанавливаем английский язык
        /// </example>
        public static string TableLanguage
        {
            get => _tableLanguage;
            set => _tableLanguage = value;
        }

        /// <summary>
        /// Настройки размещения таблиц
        /// Массив из 3 элементов, каждый может быть "0" (отключено) или "1" (включено)
        /// [Yerlesim_1, Yerlesim_2, Yerlesim_3]
        /// </summary>
        /// <example>
        /// AppSettings.TablePlacement = ["1", "0", "0"]; // Включаем только первую схему
        /// </example>
        public static string[] TablePlacement
        {
            get => _tablePlacement;
            // C# 12: Используем collection expression в setter
            // Если передан null, используем значения по умолчанию
            set => _tablePlacement = value ?? ["0", "0", "1"];
        }

        /// <summary>
        /// Имя текущего проекта
        /// </summary>
        /// <example>
        /// AppSettings.ProjectName = "ЖК Солнечный";
        /// string project = AppSettings.ProjectName; // "ЖК Солнечный"
        /// </example>
        public static string ProjectName
        {
            get => _projectName;
            // Если передан null, используем пустую строку (чтобы избежать ошибок)
            set => _projectName = value ?? "";
        }

        // ====================================================================================
        // МЕТОДЫ ДЛЯ РАБОТЫ С ФАЙЛАМИ (для будущего расширения)
        // ====================================================================================
        // Эти методы пока не реализованы, но могут быть использованы в будущем для:
        //   - Сохранения настроек в XML/JSON файл
        //   - Загрузки настроек из файла при старте программы
        //   - Синхронизации настроек между разными компьютерами

        /// <summary>
        /// Загружает настройки из файлов (для совместимости со старым кодом)
        /// </summary>
        /// <remarks>
        /// ВАЖНО: Метод пока не реализован. Настройки используются из памяти.
        /// В будущем здесь может быть:
        ///   - Чтение из XML файла
        ///   - Чтение из JSON файла
        ///   - Чтение из базы данных
        ///   - Чтение из реестра Windows
        /// </remarks>
        /// <example>
        /// // Вызов при старте программы
        /// AppSettings.LoadFromFiles();
        /// </example>
        private static string SettingsFilePath
        {
            get
            {
                string dir = Path.GetDirectoryName(
                    System.Reflection.Assembly.GetExecutingAssembly().Location) ?? "";
                return Path.Combine(dir, "PoseEdit2026.settings.json");
            }
        }

        /// <summary>Загружает настройки из файла JSON рядом с DLL.</summary>
        public static void LoadFromFiles()
        {
            try
            {
                string path = SettingsFilePath;
                if (!File.Exists(path)) return;
                string json = File.ReadAllText(path, Encoding.UTF8);

                double? du = ReadJsonDouble(json, "DrawingUnit");
                double? ss = ReadJsonDouble(json, "SheetScale");
                string tl  = ReadJsonString(json, "TableLanguage");
                string pn  = ReadJsonString(json, "ProjectName");
                string[] tp = ReadJsonStringArray(json, "TablePlacement");

                if (du.HasValue) _drawingUnit   = du.Value;
                if (ss.HasValue) _sheetScale     = ss.Value;
                if (tl != null)  _tableLanguage  = tl;
                if (pn != null)  _projectName    = pn;
                if (tp != null)  _tablePlacement = tp;
            }
            catch { }
        }

        /// <summary>Сохраняет настройки в файл JSON рядом с DLL.</summary>
        public static void SaveToFiles()
        {
            try
            {
                string p0 = _tablePlacement != null && _tablePlacement.Length > 0 ? _tablePlacement[0] : "0";
                string p1 = _tablePlacement != null && _tablePlacement.Length > 1 ? _tablePlacement[1] : "0";
                string p2 = _tablePlacement != null && _tablePlacement.Length > 2 ? _tablePlacement[2] : "1";

                var sb = new StringBuilder();
                sb.AppendLine("{");
                sb.AppendLine("  \"DrawingUnit\": " + _drawingUnit.ToString("R", CultureInfo.InvariantCulture) + ",");
                sb.AppendLine("  \"SheetScale\": " + _sheetScale.ToString("R", CultureInfo.InvariantCulture) + ",");
                sb.AppendLine("  \"TableLanguage\": \"" + JsonEsc(_tableLanguage) + "\",");
                sb.AppendLine("  \"TablePlacement\": [\"" + p0 + "\",\"" + p1 + "\",\"" + p2 + "\"],");
                sb.AppendLine("  \"ProjectName\": \"" + JsonEsc(_projectName) + "\"");
                sb.Append("}");
                File.WriteAllText(SettingsFilePath, sb.ToString(), Encoding.UTF8);
            }
            catch { }
        }

        private static string JsonEsc(string s)
            => (s ?? "").Replace("\\", "\\\\").Replace("\"", "\\\"");

        private static double? ReadJsonDouble(string json, string key)
        {
            var m = Regex.Match(json, "\"" + key + "\"\\s*:\\s*([0-9\\.eE+\\-]+)");
            if (m.Success && double.TryParse(m.Groups[1].Value,
                NumberStyles.Float, CultureInfo.InvariantCulture, out double v))
                return v;
            return null;
        }

        private static string ReadJsonString(string json, string key)
        {
            var m = Regex.Match(json, "\"" + key + "\"\\s*:\\s*\"((?:[^\"\\\\]|\\\\.)*)\"");
            if (m.Success) return m.Groups[1].Value.Replace("\\\"", "\"").Replace("\\\\", "\\");
            return null;
        }

        private static string[] ReadJsonStringArray(string json, string key)
        {
            var m = Regex.Match(json, "\"" + key + "\"\\s*:\\s*\\[([^\\]]*)\\]");
            if (!m.Success) return null;
            var items = Regex.Matches(m.Groups[1].Value, "\"([^\"]*)\"");
            if (items.Count == 0) return null;
            var result = new string[items.Count];
            for (int i = 0; i < items.Count; i++) result[i] = items[i].Groups[1].Value;
            return result;
        }
    }
}



