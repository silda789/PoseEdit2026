    #nullable disable

using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Colors;
using Autodesk.AutoCAD.Runtime;

namespace PoseEdit2026
{
    /// <summary>
    /// Класс для генерации таблиц спецификации арматуры на основе блоков RL-POS
    /// Переписан с LISP файла QUANTITY.LSP
    /// </summary>
    public static class QuantityTableGenerator
    {
        // Константы для структуры данных (соответствуют LISP assoc индексам)
        private const int INDEX_CNTR = 0;      // Счетчик
        private const int INDEX_POZ = 1;      // Позиция
        private const int INDEX_ADET = 2;     // Количество
        private const int INDEX_CAP = 3;      // Диаметр
        private const int INDEX_ARALIK = 4;   // Шаг
        private const int INDEX_BOY = 5;      // Длина (BOY)
        private const int INDEX_TIP = 6;      // Тип (форма арматуры)
        private const int INDEX_A = 7;        // Размер A
        private const int INDEX_B = 8;        // Размер B
        private const int INDEX_C = 9;        // Размер C
        private const int INDEX_D = 10;       // Размер D
        private const int INDEX_E = 11;       // Размер E
        private const int INDEX_F = 12;       // Размер F
        private const int INDEX_R = 13;       // Радиус R
        private const int INDEX_BOY_INT = 14; // Длина как число
        private const int INDEX_MALZEME = 15; // Материал

        /// <summary>
        /// Структура данных для хранения информации о позиции арматуры
        /// </summary>
        public class RebarPositionInfo
        {
            public int Cntr { get; set; }           // 0
            public string Poz { get; set; }         // 1
            public string Adet { get; set; }        // 2
            public string Cap { get; set; }         // 3
            public string Aralik { get; set; }      // 4
            public string Boy { get; set; }         // 5
            public string Tip { get; set; }         // 6
            public string A { get; set; }           // 7
            public string B { get; set; }           // 8
            public string C { get; set; }           // 9
            public string D { get; set; }           // 10
            public string E { get; set; }           // 11
            public string F { get; set; }           // 12
            public string R { get; set; }           // 13
            public int BoyInt { get; set; }         // 14
            public string Malzeme { get; set; }     // 15
        }

        /// <summary>
        /// Структура данных для информации о диаметре
        /// </summary>
        public class DiameterInfo
        {
            public string Cap { get; set; }         // Диаметр
            public double TotalLength { get; set; } // Общая длина всех стержней этого диаметра
        }

        /// <summary>
        /// Главная команда RQTN (было "RQT" в LISP - переименовано, чтобы не сталкиваться
        /// с (defun c:RQT ...) из Temp/Command/QUANTITY.LSP, если старый LISP загружен
        /// в той же сессии AutoCAD, что и этот плагин)
        /// </summary>
        [CommandMethod("RQTN")]
        public static void CreateQuantityTables()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            if (doc == null) return;

            Editor ed = doc.Editor;
            Database db = doc.Database;

            try
            {
                // Сохраняем настройки
                object oldOsMode = Application.GetSystemVariable("OSMODE");
                object oldDimZin = Application.GetSystemVariable("DIMZIN");
                object oldClayer = Application.GetSystemVariable("CLAYER");

                // Настройка среды
                Application.SetSystemVariable("CMDECHO", 0);
                Application.SetSystemVariable("DIMZIN", 1);
                Application.SetSystemVariable("ATTREQ", 0);

                // Создаем слои для таблиц
                CreateMetrajLayers(db);

                // Получаем масштаб и единицы измерения
                double birim = GetUnits();
                double olcek = GetScale() * birim * 0.0100;
                double olcuCarp = 1.0 / birim;

                // Выбор блоков RL-POS
                // C# 12: Используем новый синтаксис коллекций (collection expressions)
                TypedValue[] filterList = [
                    new TypedValue((int)DxfCode.Start, "INSERT"),
                    new TypedValue((int)DxfCode.BlockName, "RL-POS")
                ];
                SelectionFilter filter = new SelectionFilter(filterList);

                PromptSelectionOptions selOpts = new PromptSelectionOptions();
                selOpts.MessageForAdding = "\nSelect RL-POS blocks: ";

                PromptSelectionResult selRes = ed.GetSelection(selOpts, filter);
                if (selRes.Status != PromptStatus.OK)
                {
                    ed.WriteMessage("\nNo blocks selected.");
                    return;
                }

                SelectionSet eset = selRes.Value;

                // Собираем данные из блоков
                List<RebarPositionInfo> toplamBilgi = CollectRebarData(eset);

                // Проверка на ошибки
                ValidateData(toplamBilgi);

                // Группируем данные
                toplamBilgi = GroupRebarData(toplamBilgi);

                // Создаем список диаметров
                List<DiameterInfo> capListe = CreateDiameterList(toplamBilgi);

                // Запрашиваем точку размещения таблиц
                Application.SetSystemVariable("OSMODE", 0);
                PromptPointOptions ptOpts = new PromptPointOptions("\nPlacement point for tables: ");
                PromptPointResult ptRes = ed.GetPoint(ptOpts);

                if (ptRes.Status != PromptStatus.OK)
                {
                    return;
                }

                Point3d yerlesim = ptRes.Value;

                // Определяем язык таблиц и рисуем
                string tableLanguage = GetTableLanguage();
                switch (tableLanguage)
                {
                    case "eng":
                        DrawTablesEnglish(yerlesim, capListe, toplamBilgi, olcek);
                        break;
                    case "re":
                        DrawTablesRussianEnglish(yerlesim, capListe, toplamBilgi, olcek);
                        break;
                    default:
                        DrawTablesRussian(yerlesim, capListe, toplamBilgi, olcek);
                        break;
                }

                // Восстанавливаем настройки
                if (oldOsMode != null) Application.SetSystemVariable("OSMODE", oldOsMode);
                if (oldClayer != null) Application.SetSystemVariable("CLAYER", oldClayer);
                if (oldDimZin != null) Application.SetSystemVariable("DIMZIN", oldDimZin);
                Application.SetSystemVariable("CMDECHO", 1);

                ed.WriteMessage("\nTables created successfully.");
            }
            catch (System.Exception ex)
            {
                ed.WriteMessage($"\nError: {ex.Message}");
            }
        }

        // ====================================================================================
        // ВСПОМОГАТЕЛЬНЫЕ МЕТОДЫ
        // ====================================================================================

        /// <summary>
        /// Собирает данные из всех выбранных блоков RL-POS
        /// Аналог функции RENAISSANCE_toplam_bilgi_olustur
        /// </summary>
        private static List<RebarPositionInfo> CollectRebarData(SelectionSet eset)
        {
            List<RebarPositionInfo> result = new List<RebarPositionInfo>();

            // Получаем Database из активного документа, а не из SelectionSet
            Document doc = Application.DocumentManager.MdiActiveDocument;
            if (doc == null) return result;
            
            Database db = doc.Database;
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                for (int i = 0; i < eset.Count; i++)
                {
                    ObjectId objId = eset[i].ObjectId;
                    BlockReference blkRef = tr.GetObject(objId, OpenMode.ForRead) as BlockReference;

                    if (blkRef == null) continue;

                    // Читаем атрибуты блока
                    var attributes = BlockHelper.GetAttributes(objId);

                    // Получаем групповой множитель (GC)
                    string grupCarpani = attributes.ContainsKey("GC") ? attributes["GC"] : "1";
                    if (string.IsNullOrEmpty(grupCarpani)) grupCarpani = "1";
                    int gcDeger = int.Parse(grupCarpani);

                    // Парсим строку TB (например: "1xSØ12/200")
                    string tb = attributes.ContainsKey("TB") ? attributes["TB"] : "";
                    int adet = ParseAdetFromTB(tb) * gcDeger;

                    RebarPositionInfo info = new RebarPositionInfo
                    {
                        Cntr = i,
                        Poz = attributes.ContainsKey("POZ") ? attributes["POZ"] : "",
                        Adet = adet.ToString(),
                        Cap = ParseCapFromTB(tb),
                        Aralik = ParseAralikFromTB(tb),
                        Boy = attributes.ContainsKey("BOY") ? attributes["BOY"] : "",
                        Tip = attributes.ContainsKey("TIP") ? attributes["TIP"] : "00",
                        A = attributes.ContainsKey("A") ? attributes["A"] : "0",
                        B = attributes.ContainsKey("B") ? attributes["B"] : "0",
                        C = attributes.ContainsKey("C") ? attributes["C"] : "0",
                        D = attributes.ContainsKey("D") ? attributes["D"] : "0",
                        E = attributes.ContainsKey("E") ? attributes["E"] : "0",
                        F = attributes.ContainsKey("F") ? attributes["F"] : "0",
                        R = attributes.ContainsKey("R") ? attributes["R"] : "0",
                        BoyInt = ParseBoyInt(attributes.ContainsKey("BOY") ? attributes["BOY"] : ""),
                        Malzeme = attributes.ContainsKey("MALZEME") ? attributes["MALZEME"] : ""
                    };

                    result.Add(info);
                }

                tr.Commit();
            }

            // Сортируем: сначала по позиции, потом по диаметру, потом по длине
            result = result.OrderBy(x => int.TryParse(x.Poz, out int poz) ? poz : 9999)
                          .ThenBy(x => int.TryParse(x.Cap, out int cap) ? cap : 9999)
                          .ThenBy(x => x.BoyInt)
                          .ToList();

            return result;
        }

        /// <summary>
        /// Группирует одинаковые позиции (объединяет количество)
        /// Аналог функции RENAISSANCE_toplam_bilgi_derle
        /// </summary>
        private static List<RebarPositionInfo> GroupRebarData(List<RebarPositionInfo> data)
        {
            List<RebarPositionInfo> result = new List<RebarPositionInfo>();

            int i = 0;
            while (i < data.Count)
            {
                int totalAdet = int.TryParse(data[i].Adet, out int a0) ? a0 : 0;

                // Складываем количество со всеми подряд идущими дубликатами (та же
                // Poz/Cap/Boy), а не только с одним соседним элементом
                int j = i + 1;
                while (j < data.Count &&
                       data[j].Poz == data[i].Poz &&
                       data[j].Cap == data[i].Cap &&
                       data[j].Boy == data[i].Boy)
                {
                    totalAdet += int.TryParse(data[j].Adet, out int aj) ? aj : 0;
                    j++;
                }

                data[i].Adet = totalAdet.ToString();
                result.Add(data[i]);
                i = j;
            }

            return result;
        }

        /// <summary>
        /// Создает список диаметров с суммарными длинами
        /// Аналог функции cap_bilgi_olustur
        /// </summary>
        private static List<DiameterInfo> CreateDiameterList(List<RebarPositionInfo> data)
        {
            // Создаем список для каждого элемента
            List<DiameterInfo> tempList = new List<DiameterInfo>();

            foreach (var item in data)
            {
                int adet = int.TryParse(item.Adet, out int a) ? a : 0;
                double toplamBoy = adet * item.BoyInt;

                tempList.Add(new DiameterInfo
                {
                    Cap = item.Cap,
                    TotalLength = toplamBoy
                });
            }

            // Сортируем по диаметру
            tempList = tempList.OrderBy(x => int.TryParse(x.Cap, out int cap) ? cap : 9999).ToList();

            // Группируем одинаковые диаметры и суммируем длины (складываем весь подряд идущий
            // "забег" одинаковых диаметров, а не только один соседний элемент)
            List<DiameterInfo> result = new List<DiameterInfo>();
            int i = 0;
            while (i < tempList.Count)
            {
                double totalLength = tempList[i].TotalLength;
                int j = i + 1;
                while (j < tempList.Count && tempList[j].Cap == tempList[i].Cap)
                {
                    totalLength += tempList[j].TotalLength;
                    j++;
                }

                tempList[i].TotalLength = totalLength;
                result.Add(tempList[i]);
                i = j;
            }

            return result;
        }

        // ====================================================================================
        // ПАРСИНГ ДАННЫХ ИЗ СТРОКИ TB
        // ====================================================================================

        private static int ParseAdetFromTB(string tb)
        {
            if (string.IsNullOrEmpty(tb)) return 0;

            // Ищем символ диаметра
            int fiIndex = tb.IndexOf("Ø");
            if (fiIndex == -1) fiIndex = tb.IndexOf("%%C");
            if (fiIndex == -1) fiIndex = tb.IndexOf("%%c");

            if (fiIndex > 0)
            {
                string leftPart = tb.Substring(0, fiIndex).ToUpper();
                if (leftPart.Contains("X"))
                {
                    string[] parts = leftPart.Split('X');
                    if (parts.Length == 2 && int.TryParse(parts[1], out int adet))
                    {
                        return adet;
                    }
                }
                else
                {
                    if (int.TryParse(leftPart, out int adet))
                    {
                        return adet;
                    }
                }
            }

            return 0;
        }

        private static string ParseCapFromTB(string tb)
        {
            if (string.IsNullOrEmpty(tb)) return "0";

            int fiIndex = tb.IndexOf("Ø");
            int fiLength = 1;
            if (fiIndex == -1)
            {
                fiIndex = tb.IndexOf("%%C");
                fiLength = 3;
            }
            if (fiIndex == -1)
            {
                fiIndex = tb.IndexOf("%%c");
                fiLength = 3;
            }

            if (fiIndex >= 0)
            {
                int start = fiIndex + fiLength;
                int slashIndex = tb.IndexOf("/", start);
                int end = slashIndex >= 0 ? slashIndex : tb.Length;

                // На случай старых TB, где после диаметра ещё шёл "L=..."/заметка через пробел
                // (до фикса TB/BOY они хранились вместе) - обрезаем и по первому пробелу тоже
                int spaceIndex = tb.IndexOf(' ', start);
                if (spaceIndex >= 0 && spaceIndex < end) end = spaceIndex;

                if (end > start)
                {
                    string capStr = tb.Substring(start, end - start).Trim();
                    if (capStr.Length > 0) return capStr;
                }
            }

            return "0";
        }

        private static string ParseAralikFromTB(string tb)
        {
            if (string.IsNullOrEmpty(tb)) return "";

            int slashIndex = tb.IndexOf("/");
            if (slashIndex >= 0 && slashIndex < tb.Length - 1)
            {
                return tb.Substring(slashIndex + 1).Trim();
            }

            return "";
        }

        internal static int ParseBoyInt(string boy)
        {
            if (string.IsNullOrEmpty(boy)) return 0;

            // Убираем "L=" и скобки (но НЕ "~" - см. ниже)
            boy = boy.Replace("L=", "").Replace("l=", "").Replace("(", "").Replace(")", "").Trim();

            // Диапазон "мин~макс" (так форматирует PozHelper.ComputeBendLength/TDDBN, когда
            // размеры A-F заданы диапазоном) - берём среднее, как в LISP boy_oku_isle:
            // (* 0.5 (+ min max)). ВАЖНО: раньше "~" просто вырезался без замены разделителем,
            // и "1200~1300" превращалось в "12001300" (12 миллионов мм вместо ~1250).
            if (boy.Contains('~'))
            {
                string[] parts = boy.Split('~');
                if (parts.Length == 2 &&
                    int.TryParse(parts[0].Trim(), out int minV) &&
                    int.TryParse(parts[1].Trim(), out int maxV))
                {
                    return (int)Math.Round((minV + maxV) / 2.0);
                }
                return 0;
            }

            if (int.TryParse(boy, out int result))
            {
                return result;
            }

            return 0;
        }

        // ====================================================================================
        // НАСТРОЙКИ И ВСПОМОГАТЕЛЬНЫЕ ФУНКЦИИ
        // ====================================================================================

        /// <summary>
        /// Получает путь к папке настроек (client_path)
        /// В LISP это глобальная переменная, здесь мы используем путь к папке с программой
        /// Сначала проверяем папку Temp (для разработки), потом Standard рядом с DLL
        /// </summary>
        internal static string GetClientPath()
        {
            // 1. Проверяем папку Temp рядом с DLL (для разработки и тестирования)
            string assemblyDir = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
            string tempPath = Path.Combine(Path.GetDirectoryName(assemblyDir) ?? assemblyDir, "Temp");
            if (!Directory.Exists(tempPath))
            {
                // Альтернативный путь: ищем Temp рядом с исходниками проекта
                string projectDir = Path.GetDirectoryName(assemblyDir);
                while (projectDir != null)
                {
                    string candidate = Path.Combine(projectDir, "Temp");
                    if (Directory.Exists(candidate)) { tempPath = candidate; break; }
                    projectDir = Path.GetDirectoryName(projectDir);
                }
            }
            if (Directory.Exists(tempPath))
            {
                if (File.Exists(Path.Combine(tempPath, "UNIT.TXT")) ||
                    File.Exists(Path.Combine(tempPath, "TABLO_DILI.TXT")))
                {
                    return tempPath + Path.DirectorySeparatorChar;
                }
            }

            // 2. Пытаемся найти папку Standard рядом с DLL
            string assemblyPath = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
            string standardPath = Path.Combine(assemblyPath, "Standard");
            
            if (Directory.Exists(standardPath))
            {
                return assemblyPath + Path.DirectorySeparatorChar;
            }

            // 3. Альтернативный путь - папка пользователя
            string userPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                "PoseEdit2026", "Standard");
            
            if (Directory.Exists(userPath))
            {
                return Path.GetDirectoryName(userPath) + Path.DirectorySeparatorChar;
            }

            // 4. Если ничего не найдено, возвращаем путь к DLL
            return assemblyPath + Path.DirectorySeparatorChar;
        }

        /// <summary>
        /// Получает путь к папке Standard с файлами настроек
        /// </summary>
        private static string GetStandardPath()
        {
            string clientPath = GetClientPath();
            string standardPath = Path.Combine(clientPath, "Standard");
            
            // Если папка Standard не существует, но файлы есть в корне clientPath
            if (!Directory.Exists(standardPath) && 
                (File.Exists(Path.Combine(clientPath, "UNIT.TXT")) ||
                 File.Exists(Path.Combine(clientPath, "TABLO_DILI.TXT"))))
            {
                return clientPath; // Файлы в корневой папке
            }
            
            return standardPath;
        }

        /// <summary>
        /// Читает единицы измерения из AppSettings (приоритет) или из файла UNIT.TXT
        /// </summary>
        internal static double GetUnits()
        {
            // Сначала проверяем настройки из окна (AppSettings)
            if (AppSettings.DrawingUnit > 0)
            {
                return AppSettings.DrawingUnit;
            }

            // Если в AppSettings нет, читаем из файла (для совместимости)
            try
            {
                string standardPath = GetStandardPath();
                string unitFile = Path.Combine(standardPath, "UNIT.TXT");
                
                if (!File.Exists(unitFile))
                {
                    unitFile = Path.Combine(standardPath, "UNIT.txt");
                }
                
                if (File.Exists(unitFile))
                {
                    string[] lines = File.ReadAllLines(unitFile);
                    foreach (string line in lines)
                    {
                        string trimmed = line.Trim();
                        if (!string.IsNullOrEmpty(trimmed) && double.TryParse(trimmed, out double result))
                        {
                            return result;
                        }
                    }
                }
            }
            catch { }

            // Значение по умолчанию: 1000 (мм)
            return 1000.0;
        }

        /// <summary>
        /// Читает масштаб чертежа из AppSettings (приоритет) или из файла UNIT.TXT
        /// </summary>
        internal static double GetScale()
        {
            // Сначала проверяем настройки из окна (AppSettings)
            if (AppSettings.SheetScale > 0)
            {
                return AppSettings.SheetScale;
            }

            // Если в AppSettings нет, читаем из файла (для совместимости)
            try
            {
                string standardPath = GetStandardPath();
                string unitFile = Path.Combine(standardPath, "UNIT.TXT");
                
                if (!File.Exists(unitFile))
                {
                    unitFile = Path.Combine(standardPath, "UNIT.txt");
                }
                
                if (File.Exists(unitFile))
                {
                    string[] lines = File.ReadAllLines(unitFile);
                    int foundCount = 0;
                    foreach (string line in lines)
                    {
                        string trimmed = line.Trim();
                        if (!string.IsNullOrEmpty(trimmed))
                        {
                            foundCount++;
                            if (foundCount == 2 && double.TryParse(trimmed, out double result))
                            {
                                return result;
                            }
                        }
                    }
                }
            }
            catch { }

            // Значение по умолчанию: 50
            return 50.0;
        }

        /// <summary>
        /// Читает язык таблиц из AppSettings (приоритет) или из файла TABLO_DILI.TXT
        /// </summary>
        private static string GetTableLanguage()
        {
            // Сначала проверяем настройки из окна (AppSettings)
            if (!string.IsNullOrEmpty(AppSettings.TableLanguage))
            {
                return AppSettings.TableLanguage;
            }

            // Если в AppSettings нет, читаем из файла (для совместимости)
            try
            {
                string standardPath = GetStandardPath();
                string langFile = Path.Combine(standardPath, "TABLO_DILI.TXT");
                
                if (!File.Exists(langFile))
                {
                    langFile = Path.Combine(standardPath, "tablo_dili.txt");
                }
                
                if (File.Exists(langFile))
                {
                    string[] lines = File.ReadAllLines(langFile);
                    foreach (string line in lines)
                    {
                        string trimmed = line.Trim();
                        if (!string.IsNullOrEmpty(trimmed))
                        {
                            return trimmed.ToLower();
                        }
                    }
                }
            }
            catch { }

            // По умолчанию русский
            return "rus";
        }

        /// <summary>
        /// Читает настройки размещения таблиц из AppSettings (приоритет) или из файла Tablo_Tipi.TXT
        /// </summary>
        private static string[] GetTablePlacementSettings()
        {
            // Сначала проверяем настройки из окна (AppSettings)
            if (AppSettings.TablePlacement != null && AppSettings.TablePlacement.Length >= 3)
            {
                return AppSettings.TablePlacement;
            }

            // Если в AppSettings нет, читаем из файла (для совместимости)
            try
            {
                string standardPath = GetStandardPath();
                string placementFile = Path.Combine(standardPath, "Tablo_Tipi.TXT");
                
                if (!File.Exists(placementFile))
                {
                    placementFile = Path.Combine(standardPath, "tablo_tipi.txt");
                }
                
                if (File.Exists(placementFile))
                {
                    string[] lines = File.ReadAllLines(placementFile);
                    // C# 12: Используем collection expression для инициализации списка
                    List<string> settings = [];
                    
                    foreach (string line in lines)
                    {
                        string trimmed = line.Trim();
                        if (!string.IsNullOrEmpty(trimmed) && settings.Count < 3)
                        {
                            settings.Add(trimmed);
                        }
                    }
                    
                    if (settings.Count == 3)
                    {
                        return settings.ToArray();
                    }
                }
            }
            catch { }

            // Настройки по умолчанию: третья схема размещения активна
            // C# 12: Используем collection expression для инициализации массива
            return ["0", "0", "1"];
        }

        /// <summary>
        /// Создает слои для таблиц метража
        /// Аналог функции RENAISSANCE_metraj_layer_olustur
        /// </summary>
        private static void CreateMetrajLayers(Database db)
        {
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                LayerTable lt = tr.GetObject(db.LayerTableId, OpenMode.ForRead) as LayerTable;

                // C# 12: Используем collection expression для инициализации массива
                string[] layers = [
                    "posedit.mtr.layer_l1",    // Основные линии таблиц
                    "posedit.mtr.layer_t1",    // Текст таблиц
                    "posedit.mtr.bar"          // Слой для таблиц
                ];

                foreach (string layerName in layers)
                {
                    if (!lt.Has(layerName))
                    {
                        lt.UpgradeOpen();
                        LayerTableRecord newLayer = new LayerTableRecord();
                        newLayer.Name = layerName;
                        newLayer.Color = Color.FromColorIndex(ColorMethod.ByAci, 7); // Белый/Черный
                        lt.Add(newLayer);
                        tr.AddNewlyCreatedDBObject(newLayer, true);
                        lt.DowngradeOpen();
                    }
                }

                tr.Commit();
            }
        }

        /// <summary>
        /// Проверяет данные на ошибки
        /// Аналог функции hata_kontrol
        /// </summary>
        private static void ValidateData(List<RebarPositionInfo> data)
        {
            if (data == null || data.Count == 0) return;

            string errorFile = Path.Combine(GetClientPath(), "error.txt");
            System.IO.StreamWriter writer = null;

            try
            {
                writer = new System.IO.StreamWriter(errorFile, false);

                // Проверка 1: Одинаковые позиции с разными диаметрами
                for (int i = 0; i < data.Count - 1; i++)
                {
                    if (data[i].Poz == data[i + 1].Poz && data[i].Cap != data[i + 1].Cap)
                    {
                        string msg = $"\n {data[i].Poz}.NoPoz: Cap hatasi var. Duzeltip yeniden metraj yapin....! ";
                        Application.DocumentManager.MdiActiveDocument.Editor.WriteMessage(msg);
                        writer.WriteLine(msg);
                    }

                    // Проверка 2: Одинаковые позиции с разными длинами
                    if (data[i].Poz == data[i + 1].Poz && data[i].Boy != data[i + 1].Boy)
                    {
                        string msg = $"\n {data[i].Poz}.NoPoz: Toplam boy hatasi var. Duzeltip yeniden metraj yapin....! ";
                        Application.DocumentManager.MdiActiveDocument.Editor.WriteMessage(msg);
                        writer.WriteLine(msg);
                    }

                    // Проверка 3: Одинаковые позиции с разными типами
                    if (data[i].Poz == data[i + 1].Poz && data[i].Tip != data[i + 1].Tip)
                    {
                        string msg = $"\n {data[i].Poz}.NoPoz: Tip hatasi var. Duzeltip yeniden metraj yapin....! ";
                        Application.DocumentManager.MdiActiveDocument.Editor.WriteMessage(msg);
                        writer.WriteLine(msg);
                    }

                    // Проверка 4: Одинаковые позиции с разными размерами (A, B, C, D, E, F, R)
                    if (data[i].Poz == data[i + 1].Poz &&
                        (data[i].A != data[i + 1].A || data[i].B != data[i + 1].B ||
                         data[i].C != data[i + 1].C || data[i].D != data[i + 1].D ||
                         data[i].E != data[i + 1].E || data[i].F != data[i + 1].F ||
                         data[i].R != data[i + 1].R))
                    {
                        string msg = $"\n {data[i].Poz}.NoPoz: Bukum boylarinda hata var. Duzeltip yeniden metraj yapin....! ";
                        Application.DocumentManager.MdiActiveDocument.Editor.WriteMessage(msg);
                        writer.WriteLine(msg);
                    }
                }

                // Проверка 5: Индивидуальные проверки
                foreach (var item in data)
                {
                    // Проверка на нулевую длину
                    if (item.BoyInt == 0)
                    {
                        string msg = $"\n {item.Poz}.NoPoz: Toplam boy L=0 ! ";
                        Application.DocumentManager.MdiActiveDocument.Editor.WriteMessage(msg);
                        writer.WriteLine(msg);
                    }

                    // Проверка на максимальную длину (11.70 м)
                    double olcuCarp = 1.0 / GetUnits();
                    if (item.BoyInt * olcuCarp > 11.70 + 0.001)
                    {
                        string msg = $"\n {item.Poz}.NoPoz: Bukum boylariyla hesaplanan boy 11.70 m yi geciyor ! ";
                        Application.DocumentManager.MdiActiveDocument.Editor.WriteMessage(msg);
                        writer.WriteLine(msg);
                    }
                }
            }
            catch (System.Exception ex)
            {
                Application.DocumentManager.MdiActiveDocument.Editor.WriteMessage($"\nError writing validation log: {ex.Message}");
            }
            finally
            {
                writer?.Close();
            }
        }

        // ====================================================================================
        // РИСОВАНИЕ ТАБЛИЦ — вспомогательные функции
        // ====================================================================================

        // Вес арматуры кг/м. Аналог LISP: (defun cp_ag (cp) (* 0.00616537558266997 cp cp))
        private static double CpAg(double diameterMm)
            => 0.00616537558266997 * diameterMm * diameterMm;

        // Сдвиг точки в направлении angle (радианы) на расстояние dist.
        // Аналог LISP: (polar pt angle dist)
        private static Point3d Polar(Point3d pt, double angle, double dist)
            => new Point3d(pt.X + dist * Math.Cos(angle), pt.Y + dist * Math.Sin(angle), pt.Z);

        // Рисует линию от pt1 в направлении angle на длину dist.
        // lineType: 1 → "posedit.mtr.layer_l1", иначе → "Defpoints".
        // Аналог LISP: REN_cizgi
        private static void RenLine(BlockTableRecord space, Transaction tr,
            Point3d pt1, double angle, double dist, int lineType)
        {
            Line line = new Line(pt1, Polar(pt1, angle, dist));
            line.Layer = lineType == 1 ? "posedit.mtr.layer_l1" : "Defpoints";
            space.AppendEntity(line);
            tr.AddNewlyCreatedDBObject(line, true);
        }

        // Возвращает ObjectId текстового стиля: "ren Gost.common" или стиль по умолчанию.
        private static ObjectId GetTextStyleId(Database db, Transaction tr)
        {
            TextStyleTable tst = tr.GetObject(db.TextStyleTableId, OpenMode.ForRead) as TextStyleTable;
            if (tst != null && tst.Has("ren Gost.common")) return tst["ren Gost.common"];
            return db.Textstyle;
        }

        // Рисует однострочный текст DBText.
        // Точка: basePt → смещение (angle1, dist1) → смещение (angle2, dist2).
        // d72: 0=Left, 1=Center, 2=Right; d73: 0=Baseline, 1=Bottom, 2=Middle, 3=Top.
        // Аналог LISP: REN_Y (widthFactor=0.8) и REN_Y2 (widthFactor задаётся явно)
        private static void RenText(BlockTableRecord space, Transaction tr, Database db,
            Point3d basePt, string text, double height,
            double angle1, double dist1, double angle2, double dist2,
            int d72, int d73, double widthFactor = 0.8)
        {
            if (string.IsNullOrEmpty(text)) return;
            Point3d pt = Polar(Polar(basePt, angle1, dist1), angle2, dist2);

            DBText txt = new DBText();
            txt.TextString = text;
            txt.Height = height;
            txt.WidthFactor = widthFactor;
            txt.Layer = "posedit.mtr.layer_t1";
            txt.TextStyleId = GetTextStyleId(db, tr);

            if (d72 == 0 && d73 == 0)
            {
                txt.Position = pt;
            }
            else
            {
                txt.HorizontalMode = (TextHorizontalMode)d72;
                txt.VerticalMode   = (TextVerticalMode)d73;
                txt.AlignmentPoint = pt;
                txt.Position       = pt;
            }

            space.AppendEntity(txt);
            tr.AddNewlyCreatedDBObject(txt, true);
        }

        // Пытается вставить блок PZ_XX (эскиз формы) в таблицу.
        // Если шаблона для этого типа нет — тихо пропускает.
        // Аналог LISP: (command ".-insert" dwgname ...)
        //
        // ВАЖНО (найдено и исправлено 2026-08-19): раньше этот метод искал PZ_XX.dwg НА
        // ДИСКЕ через GetStandardPath() (Temp\Standard\ или папку рядом с DLL) - но
        // реальные файлы (Resources\Standard\PZ_00.dwg..PZ_95.dwg) лежат совсем в другом
        // месте и вообще не копируются рядом с собранной DLL. Из-за этого File.Exists()
        // всегда возвращал false, метод молча выходил (return) без единой ошибки - RQTN
        // отрабатывал полностью успешно, просто эскиз в таблице никогда не появлялся.
        // Теперь используем те же встроенные (EmbeddedResource) ресурсы, что и PZREDEFN
        // в LegacyCommands.cs - они гарантированно лежат внутри самой DLL, путь на диске
        // тут вообще не при чём.
        private static void TryInsertPzBlock(BlockTableRecord space, Transaction tr, Database db,
            string tip, Point3d insertPt, double scaleX, double scaleY,
            Dictionary<string, string> dims)
        {
            try
            {
                string blockName = $"PZ_{tip}";
                BlockTable bt = tr.GetObject(db.BlockTableId, OpenMode.ForWrite) as BlockTable;
                ObjectId btrId;

                if (bt.Has(blockName))
                {
                    btrId = bt[blockName];
                }
                else
                {
                    // Сначала пробуем шаблон конкретного типа, если его нет (например,
                    // PZ_96..PZ_99 - для них .dwg-файлов пока нет) - откатываемся на PZ_99
                    // как "нераспознанный" placeholder, как и в исходном LISP.
                    string tempFile = ExtractPzTemplate(tip) ?? ExtractPzTemplate("99");
                    if (tempFile == null) return;

                    using (Database srcDb = new Database(false, true))
                    {
                        srcDb.ReadDwgFile(tempFile, FileOpenMode.OpenForReadAndAllShare, true, "");
                        btrId = db.Insert(blockName, srcDb, true);
                    }
                }

                BlockReference blkRef = new BlockReference(insertPt, btrId);
                blkRef.ScaleFactors = new Scale3d(scaleX, scaleY, 1.0);
                space.AppendEntity(blkRef);
                tr.AddNewlyCreatedDBObject(blkRef, true);

                BlockTableRecord btr = tr.GetObject(btrId, OpenMode.ForRead) as BlockTableRecord;
                if (btr != null && btr.HasAttributeDefinitions)
                {
                    foreach (ObjectId id in btr)
                    {
                        DBObject obj = tr.GetObject(id, OpenMode.ForRead);
                        if (obj is AttributeDefinition attDef)
                        {
                            AttributeReference attRef = new AttributeReference();
                            attRef.SetAttributeFromBlock(attDef, blkRef.BlockTransform);
                            string key = attDef.Tag.ToUpper();
                            if (dims.ContainsKey(key)) attRef.TextString = dims[key];
                            blkRef.AttributeCollection.AppendAttribute(attRef);
                            tr.AddNewlyCreatedDBObject(attRef, true);
                        }
                    }
                }
            }
            catch { }
        }

        // Извлекает встроенный ресурс "PoseEdit2026.Resources.Standard.PZ_<tip>.dwg" во
        // временный файл и возвращает путь к нему, либо null, если такого ресурса нет
        // (LegacyCommands.ExtractEmbeddedResource бросает исключение на отсутствующий
        // ресурс - тут превращаем это в null, а не даём упасть всей команде RQTN).
        private static string ExtractPzTemplate(string tip)
        {
            string blockName = "PZ_" + tip;
            string resourceName = $"PoseEdit2026.Resources.Standard.{blockName}.dwg";
            string tempFile = Path.Combine(Path.GetTempPath(), blockName + "_sketch.dwg");
            try
            {
                LegacyCommands.ExtractEmbeddedResource(resourceName, tempFile);
                return tempFile;
            }
            catch
            {
                return null;
            }
        }

        // Общая логика рисования трёх таблиц.
        // lang: "rus" — только русский, "eng" — только английский, "re" — двуязычный.
        private static void DrawTablesCore(Point3d yerlesim, List<DiameterInfo> capListe,
            List<RebarPositionInfo> toplamBilgi, double olcek, string lang)
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            if (doc == null) return;
            Database db = doc.Database;
            double olcuCarp = 1.0 / GetUnits();

            int pozAdet = toplamBilgi.Count;
            int capAdet = capListe.Count;
            // "01" - прямой стержень (номер "00" упразднён 2026-08-22, "01" занял его место
            // и его роль - не печатать эскиз в таблице); "00" тоже проверяем, чтобы старые
            // чертежи с уже сохранённым TIP=00 по-прежнему не печатались.
            int tipBilgiAdet = toplamBilgi.Count(x => x.Tip == "00" || x.Tip == "01"); // прямые стержни
            int pozTipAdet   = toplamBilgi.Count(x => x.Tip != "00" && x.Tip != "01"); // с формой

            const double sag   = 0.0;
            const double asagi = Math.PI * 1.5;

            // Ширины колонок (Sut) и высоты строк (Sat), умноженные на masshtab olcek
            double sut1_01 = olcek * 1.70;
            double sut1_02 = olcek * 6.00;
            double sut1_2  = olcek * 0.80;
            // Для RE-версии: заголовочная строка таблицы 1 выше — двойная высота
            double sat1_01 = lang == "re" ? olcek * 1.50 : olcek * 0.80;
            double sat1_02 = olcek * 1.50;

            double sut2_01 = olcek * 1.50;
            double sut2_02 = olcek * 6.00;
            double sut2_03 = olcek * 6.00;
            double sut2_04 = olcek * 1.50;
            double sut2_05 = olcek * 1.50;
            double sut2_06 = olcek * 2.00;
            double sut2_3  = olcek * 0.80;
            double sat2_01 = olcek * 0.80;
            double sat2_02 = olcek * 1.50;
            double sat2_03 = olcek * 0.80;

            double sut3_01 = olcek * 4.00;
            double sut3_02 = olcek * 1.50;
            double sut3_03 = olcek * 2.00;
            double sat3_01 = olcek * 0.80;

            double yzy1 = olcek * 0.30;
            double yzy2 = olcek * 0.30;

            int duseyadet = (pozAdet == tipBilgiAdet) ? 6 : 5;
            int yatayadet = (pozAdet == tipBilgiAdet) ? 7 : 6;

            // Опорные точки зависят от схемы размещения
            string[] placement = GetTablePlacementSettings();
            string yer1 = placement.Length > 0 ? placement[0] : "0";
            string yer2 = placement.Length > 1 ? placement[1] : "0";

            Point3d p001, p003, p006;
            if (yer1 == "1")
            {
                p001 = yerlesim;
                p003 = Polar(p001, sag, sut1_01 + sut1_02 + sut1_2);
                p006 = Polar(p001, sag, sut1_01 + sut1_02 + sut1_2 + sut2_01 + sut2_02 + sut2_03 + sut2_04 + sut2_05 + sut2_06 + sut2_3);
            }
            else if (yer2 == "1")
            {
                p003 = yerlesim;
                p001 = Polar(p003, asagi, sat2_02 + sat1_01 / 2 + sat2_03 * (pozAdet + duseyadet + 1));
                p006 = Polar(p001, sag, sut1_01 + sut1_02 + sat1_01 / 2);
            }
            else // Yerlesim_3 (default)
            {
                p001 = yerlesim;
                p003 = Polar(p001, sag, sut1_01 + sut1_02 + sut1_2);
                Point3d p0061 = Polar(p003, asagi, sat2_02 + sat1_01 / 2 + sat2_03 * (pozAdet + duseyadet + 1));
                p006 = Polar(p0061, sag, (sut2_01 + sut2_02 + sut2_03 + sut2_04 + sut2_05 + sut2_06)
                                       - (sut3_01 + (1 + capAdet) * sut3_02 + sut3_03));
            }

            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                BlockTableRecord space = tr.GetObject(db.CurrentSpaceId, OpenMode.ForWrite) as BlockTableRecord;

                // Локальные функции-обёртки для краткости вызовов
                void Line_(Point3d pt, double a, double d, int t) => RenLine(space, tr, pt, a, d, t);
                void Text_(Point3d bp, string txt, double h, double a1, double d1, double a2, double d2, int h72, int v73, double wf = 0.8)
                    => RenText(space, tr, db, bp, txt, h, a1, d1, a2, d2, h72, v73, wf);

                // Вспомогатель: пишет 1 строку (rus/eng) или 2 строки (re)
                void T2(Point3d bp, string rus, string eng, double h,
                        double a1, double d1, double a2off1, double a2off2,
                        int h72, int v73, double wf = 0.8)
                {
                    double a2 = asagi;
                    if (lang == "re")
                    {
                        Text_(bp, rus, h, a1, d1, a2, a2off1, h72, v73, wf);
                        Text_(bp, eng, h, a1, d1, a2, a2off2, h72, v73, wf);
                    }
                    else if (lang == "eng")
                        Text_(bp, eng, h, a1, d1, a2, (a2off1 + a2off2) / 2, h72, v73, wf);
                    else
                        Text_(bp, rus, h, a1, d1, a2, (a2off1 + a2off2) / 2, h72, v73, wf);
                }

                // ============================================================
                // ТАБЛИЦА 1: Ведомость деталей / Detail list
                // ============================================================
                Point3d p002 = Polar(p001, asagi, sat1_01);
                int sutAdet = tipBilgiAdet == 0 ? 2 : 3;
                int satAdet = tipBilgiAdet == 0 ? 1 : 2;

                Line_(p001, sag,   sut1_01 + sut1_02, 2);
                Line_(p001, asagi, sat1_01, 2);
                Line_(Polar(p001, sag, sut1_01 + sut1_02), asagi, sat1_01, 2);
                Line_(p002, sag,   sut1_01 + sut1_02, 1);
                Line_(p002, asagi, sat1_02 * (pozTipAdet + satAdet), 1);
                Line_(Polar(p002, sag, sut1_01), asagi, sat1_02 * (pozTipAdet + 1), 1);
                Line_(Polar(p002, sag, sut1_01 + sut1_02), asagi, sat1_02 * (pozTipAdet + satAdet), 1);
                for (int i = 1; i < pozTipAdet + sutAdet; i++)
                    Line_(Polar(p002, asagi, i * sat1_02), sag, sut1_01 + sut1_02, 1);

                T2(p001, "Ведомость деталей", "Detail list", yzy2,
                   sag, 0.5 * (sut1_01 + sut1_02), 0.25 * sat1_01, 0.75 * sat1_01, 1, 2);
                T2(p002, "Поз.", "Pos.", yzy2,
                   sag, 0.5 * sut1_01, 0.25 * sat1_02, 0.75 * sat1_02, 1, 2);
                T2(p002, "Эскиз", "Draft", yzy2,
                   sag, sut1_01 + 0.5 * sut1_02, 0.25 * sat1_02, 0.75 * sat1_02, 1, 2);

                int j = 0;
                for (int i = 0; i < pozAdet; i++)
                {
                    if (toplamBilgi[i].Tip == "00" || toplamBilgi[i].Tip == "01") continue;
                    T2(p002, toplamBilgi[i].Poz, toplamBilgi[i].Poz, yzy2,
                       sag, 0.5 * sut1_01, (j + 1.25) * sat1_02, (j + 1.75) * sat1_02, 1, 2);

                    Point3d pzPt = Polar(Polar(p002, sag, sut1_01 + 0.5 * sut1_02), asagi, (j + 1.5) * sat1_02);
                    var dims = new Dictionary<string, string> {
                        ["A"] = toplamBilgi[i].A, ["B"] = toplamBilgi[i].B,
                        ["C"] = toplamBilgi[i].C, ["D"] = toplamBilgi[i].D,
                        ["E"] = toplamBilgi[i].E, ["F"] = toplamBilgi[i].F,
                        ["R"] = toplamBilgi[i].R
                    };
                    TryInsertPzBlock(space, tr, db, toplamBilgi[i].Tip, pzPt,
                        sut1_02 / 450.0, sat1_02 / 100.0, dims);
                    j++;
                }

                // ============================================================
                // ТАБЛИЦА 2: Спецификация арматурных изделий
                // ============================================================
                Point3d p004 = Polar(p003, asagi, sat2_01);
                Point3d p005 = Polar(p004, asagi, sat2_02);
                Point3d p106 = Polar(p005, asagi, 2 * sat2_03);
                Point3d p107 = Polar(p106, sag, sut2_01 + sut2_02);
                Point3d p108 = Polar(p106, sag, sut2_01 + sut2_02 + 0.30 * sut2_03);
                Point3d p109 = Polar(p106, sag, sut2_01 + sut2_02 + 0.60 * sut2_03);
                double t2w = sut2_01 + sut2_02 + sut2_03 + sut2_04 + sut2_05 + sut2_06;

                // Рамка и сетка таблицы 2
                Line_(p003, sag,   t2w, 2);
                Line_(p003, asagi, sat2_01, 2);
                Line_(Polar(p003, sag, t2w), asagi, sat2_01, 2);
                Line_(p004, sag, t2w, 1);
                Line_(p005, sag, t2w, 1);
                for (int i = 1; i < pozAdet + yatayadet; i++)
                    Line_(Polar(p005, asagi, i * sat2_03), sag, t2w, 1);
                Line_(p004,                                             asagi, sat2_02 + sat2_03 * (pozAdet + duseyadet), 1);
                Line_(Polar(p004, sag, sut2_01),                       asagi, sat2_02 + sat2_03 * (pozAdet + 5), 1);
                Line_(Polar(p004, sag, sut2_01 + sut2_02),             asagi, sat2_02 + sat2_03 * (pozAdet + 5), 1);
                Line_(Polar(p004, sag, sut2_01 + sut2_02 + sut2_03),   asagi, sat2_02 + sat2_03 * (pozAdet + 5), 1);
                Line_(p108,                                             asagi, sat2_03 * pozAdet, 1);
                Line_(p109,                                             asagi, sat2_03 * pozAdet, 1);
                Line_(Polar(p004, sag, sut2_01 + sut2_02 + sut2_03 + sut2_04),           asagi, sat2_02 + sat2_03 * (pozAdet + 5), 1);
                Line_(Polar(p004, sag, sut2_01 + sut2_02 + sut2_03 + sut2_04 + sut2_05), asagi, sat2_02 + sat2_03 * (pozAdet + 5), 1);
                Line_(Polar(p004, sag, t2w),                           asagi, sat2_02 + sat2_03 * (pozAdet + duseyadet), 1);

                // Заголовок и колонки таблицы 2
                T2(p003, "Спецификация арматурных изделий", "Reinforcement details specification", yzy2,
                   sag, 0.5 * t2w, 0.25 * sat2_01, 0.75 * sat2_01, 1, 2);
                T2(p004, "Поз.",         "Pos.",         yzy2, sag, 0.5 * sut2_01, 0.25 * sat2_02, 0.75 * sat2_02, 1, 2);
                T2(p004, "Обозначение",  "Designation",  yzy2, sag, sut2_01 + 0.5 * sut2_02, 0.25 * sat2_02, 0.75 * sat2_02, 1, 2);
                T2(p004, "Наименование", "Name",         yzy2, sag, sut2_01 + sut2_02 + 0.5 * sut2_03, 0.25 * sat2_02, 0.75 * sat2_02, 1, 2);
                T2(p004, "Кол.",         "Q-ty",         yzy2, sag, sut2_01 + sut2_02 + sut2_03 + 0.5 * sut2_04, 0.25 * sat2_02, 0.75 * sat2_02, 1, 2);
                T2(p004, "Масса ед., кг","W. of pc.",    yzy1, sag, sut2_01 + sut2_02 + sut2_03 + sut2_04 + 0.5 * sut2_05, 0.25 * sat2_02, 0.75 * sat2_02, 1, 2, 0.6);
                T2(p004, "Примечание",   "Notes",        yzy1, sag, sut2_01 + sut2_02 + sut2_03 + sut2_04 + sut2_05 + 0.5 * sut2_06, 0.25 * sat2_02, 0.75 * sat2_02, 1, 2);

                // Подзаголовки колонки "Наименование"
                Text_(p004, "...",            yzy1, sag, sut2_01 + sut2_02 + 0.5 * sut2_03, asagi, sat2_02 + 0.25 * sat2_03, 1, 2);
                Text_(p004, "...",            yzy1, sag, sut2_01 + sut2_02 + 0.5 * sut2_03, asagi, sat2_02 + 0.75 * sat2_03, 1, 2);
                T2(p004, "%%UСтержни", "Details", yzy1, sag, sut2_01 + sut2_02 + 0.5 * sut2_03, sat2_02 + 1.25 * sat2_03, sat2_02 + 1.75 * sat2_03, 1, 2);
                T2(p004, "Материал", "Material", yzy1, sag, sut2_01 + sut2_02 + 0.5 * sut2_03, sat2_02 + sat2_03 * (pozAdet + 3.25), sat2_02 + sat2_03 * (pozAdet + 3.75), 1, 2);
                T2(p004, "Бетон",    "Concrete", yzy1, sag, sut2_01 + sut2_02 + 0.5 * sut2_03, sat2_02 + sat2_03 * (pozAdet + 4.25), sat2_02 + sat2_03 * (pozAdet + 4.75), 1, 2);
                Text_(p004, "м³", yzy1, sag, sut2_01 + sut2_02 + sut2_03 + 0.5 * sut2_04,                         asagi, sat2_02 + sat2_03 * (pozAdet + 4.5), 1, 2);
                Text_(p004, "...", yzy1, sag, sut2_01 + sut2_02 + sut2_03 + sut2_04 + 0.5 * sut2_05,              asagi, sat2_02 + sat2_03 * (pozAdet + 4.5), 1, 2);
                Text_(p004, "...", yzy1, sag, sut2_01 + sut2_02 + sut2_03 + sut2_04 + sut2_05 + 0.5 * sut2_06,   asagi, sat2_02 + sat2_03 * (pozAdet + 4.5), 1, 2);

                if (pozAdet == tipBilgiAdet)
                {
                    string noteRus = "Размеры поз. 1-2 даны по внутр. граням.\nРазмеры остальных позиций даны по наруж. граням.";
                    string noteEng = "Straight bars are conditionaly not presented in details list";
                    T2(p004, noteRus, noteEng, yzy1, sag, 0.1 * sut2_01, sat2_02 + sat2_03 * (pozAdet + 5.25), sat2_02 + sat2_03 * (pozAdet + 5.75), 0, 2);
                }

                // Данные строк
                for (int i = 0; i < pozAdet; i++)
                {
                    var info = toplamBilgi[i];
                    Text_(p005, info.Poz, yzy1, sag, 0.5 * sut2_01, asagi, (i + 2.5) * sat2_03, 1, 2);
                    T2(p005, "ГОСТ 34028-2016", "GOST 34028-2016", yzy1,
                       sag, sut2_01 + 0.1 * sut2_02, (i + 2.25) * sat2_03, (i + 2.75) * sat2_03, 0, 2);
                    Text_(p107, "%%C" + info.Cap, yzy1, sag, 0.05 * sut2_03, asagi, (i + 0.5) * sat2_03, 0, 2);
                    Text_(p108, info.Malzeme,     yzy1, sag, 0.05 * sut2_03, asagi, (i + 0.5) * sat2_03, 0, 2);
                    Text_(p109, info.Boy,         yzy1, sag, 0.05 * sut2_03, asagi, (i + 0.5) * sat2_03, 0, 2);
                    Text_(p005, info.Adet, yzy1, sag, sut2_01 + sut2_02 + sut2_03 + 0.9 * sut2_04, asagi, (i + 2.5) * sat2_03, 2, 2);

                    double cap = double.TryParse(info.Cap, out double cv) ? cv : 0;
                    double bw  = Math.Floor(0.5 + 1000.0 * CpAg(cap)) * 0.001;
                    double ew  = olcuCarp * info.BoyInt * bw;
                    double tw  = (double.TryParse(info.Adet, out double av) ? av : 0) * ew;
                    Text_(p005, ew.ToString("F2"), yzy1, sag, sut2_01 + sut2_02 + sut2_03 + sut2_04 + 0.9 * sut2_05, asagi, (i + 2.5) * sat2_03, 2, 2);
                    Text_(p005, tw.ToString("F2"), yzy1, sag, sut2_01 + sut2_02 + sut2_03 + sut2_04 + sut2_05 + 0.9 * sut2_06, asagi, (i + 2.5) * sat2_03, 2, 2);
                }

                // ============================================================
                // ТАБЛИЦА 3: Ведомость расхода стали / Steel Flow Specification
                // ============================================================
                Point3d p007  = Polar(p006, asagi, sat3_01);
                Point3d p008  = Polar(p007, asagi, 5 * sat3_01);
                Point3d p010  = Polar(p007, sag,   sut3_01);
                Point3d p011  = Polar(p010, asagi, sat3_01);
                Point3d p012  = Polar(p011, asagi, sat3_01);
                Point3d p013  = Polar(p012, asagi, sat3_01);
                Point3d p014  = Polar(p013, asagi, sat3_01);
                Point3d p026  = Polar(p011, sag,   (1 + capAdet) * sut3_02);
                Point3d p032  = Polar(p006, sag,   sut3_01 + (1 + capAdet) * sut3_02 + sut3_03);
                Point3d p033  = Polar(p032, asagi, sat3_01);
                double t3w = sut3_01 + (1 + capAdet) * sut3_02 + sut3_03;

                Line_(p006, sag,   t3w, 2);
                Line_(p006, asagi, sat3_01, 2);
                Line_(p032, asagi, sat3_01, 2);
                Line_(p007, sag,   t3w, 1);
                Line_(p007, asagi, 6 * sat3_01, 1);
                Line_(p010, asagi, 6 * sat3_01, 1);
                Line_(p033, asagi, 6 * sat3_01, 1);
                Line_(p008, sag, t3w, 1);
                Line_(Polar(p008, asagi, sat3_01), sag, t3w, 1);
                Line_(p011, sag, (1 + capAdet) * sut3_02 + sut3_03, 1);
                Line_(p012, sag, (1 + capAdet) * sut3_02, 1);
                Line_(p013, sag, (1 + capAdet) * sut3_02, 1);
                Line_(p014, sag, (1 + capAdet) * sut3_02, 1);
                Line_(p026, asagi, 5 * sat3_01, 1);
                Point3d p014cur = p014;
                for (int i = 1; i <= capAdet; i++)
                {
                    p014cur = Polar(p014cur, sag, sut3_02);
                    Line_(p014cur, asagi, 2 * sat3_01, 1);
                }

                T2(p006, "Ведомость расхода стали, кг", "Steel Flow Specification, kg", yzy2,
                   sag, 0.5 * t3w, 0.3 * sat3_01, 0.7 * sat3_01, 1, 2);
                T2(p007, "Марка элемента", "Mark of Element", yzy1,
                   sag, 0.5 * sut3_01, 2.3 * sat3_01, 2.7 * sat3_01, 1, 2);
                Text_(p008, "...", yzy1, sag, 0.5 * sut3_01, asagi, 0.3 * sat3_01, 1, 2);
                Text_(p008, "...", yzy1, sag, 0.5 * sut3_01, asagi, 0.7 * sat3_01, 1, 2);
                T2(p010, "Изделия арматурные", "Reinforcement Goods", yzy1,
                   sag, 0.5 * ((1 + capAdet) * sut3_02 + sut3_03), 0.3 * sat3_01, 0.7 * sat3_01, 1, 2);
                T2(p011, "Арматура класса", "Class of reinforcement", yzy1,
                   sag, 0.5 * (1 + capAdet) * sut3_02, 0.3 * sat3_01, 0.7 * sat3_01, 1, 2, 0.73);
                if (toplamBilgi.Count > 0)
                    Text_(p012, toplamBilgi[0].Malzeme, yzy1, sag, 0.5 * (1 + capAdet) * sut3_02, asagi, 0.5 * sat3_01, 1, 2);
                T2(p013, "ГОСТ 34028-2016", "GOST P 52544-2006", yzy1,
                   sag, 0.5 * (1 + capAdet) * sut3_02, 0.3 * sat3_01, 0.7 * sat3_01, 1, 2);
                T2(p026, "Всего", "Total", yzy1,
                   sag, 0.5 * sut3_03, 1.8 * sat3_01, 2.2 * sat3_01, 1, 2);

                double totalWt = 0;
                for (int i = 0; i < capAdet; i++)
                {
                    double cap = double.TryParse(capListe[i].Cap, out double cv) ? cv : 0;
                    double bw  = Math.Floor(0.5 + 1000.0 * CpAg(cap)) * 0.001;
                    double wt  = olcuCarp * capListe[i].TotalLength * bw;
                    totalWt   += wt;
                    Text_(p013, "%%C" + capListe[i].Cap, yzy1, sag, (i + 0.5) * sut3_02, asagi, 1.5 * sat3_01, 1, 2);
                    Text_(p013, wt.ToString("F2"),        yzy1, sag, (i + 0.5) * sut3_02, asagi, 2.5 * sat3_01, 1, 2);
                }
                T2(p013, "Итого", "Total", yzy1,
                   sag, (capAdet + 0.5) * sut3_02, 1.5 * sat3_01, 1.8 * sat3_01, 1, 2);
                Text_(p013, totalWt.ToString("F2"), yzy1, sag, (capAdet + 0.5) * sut3_02,          asagi, 2.5 * sat3_01, 1, 2);
                Text_(p013, totalWt.ToString("F2"), yzy1, sag, (capAdet + 0.5) * sut3_02 + sut3_03, asagi, 2.5 * sat3_01, 1, 2);

                tr.Commit();
            }
        }

        // ====================================================================================
        // РИСОВАНИЕ ТАБЛИЦ — три публичных метода
        // ====================================================================================

        private static void DrawTablesRussian(Point3d yerlesim, List<DiameterInfo> capListe, List<RebarPositionInfo> toplamBilgi, double olcek)
            => DrawTablesCore(yerlesim, capListe, toplamBilgi, olcek, "rus");

        private static void DrawTablesEnglish(Point3d yerlesim, List<DiameterInfo> capListe, List<RebarPositionInfo> toplamBilgi, double olcek)
            => DrawTablesCore(yerlesim, capListe, toplamBilgi, olcek, "eng");

        private static void DrawTablesRussianEnglish(Point3d yerlesim, List<DiameterInfo> capListe, List<RebarPositionInfo> toplamBilgi, double olcek)
            => DrawTablesCore(yerlesim, capListe, toplamBilgi, olcek, "re");
    }
}

