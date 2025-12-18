#nullable disable

using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Globalization;
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
        /// Главная команда RQT - создание таблиц спецификации
        /// </summary>
        [CommandMethod("RQT")]
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
        /// </summary>
        private static List<RebarPositionInfo> GroupRebarData(List<RebarPositionInfo> data)
        {
            if (data == null || data.Count == 0) return new List<RebarPositionInfo>();

            return data
                .GroupBy(x => new { x.Poz, x.Cap, x.Boy, x.Tip, x.A, x.B, x.C, x.D, x.E, x.F, x.R })
                .Select(g =>
                {
                    var first = g.First();
                    int totalAdet = g.Sum(x => int.TryParse(x.Adet, out int a) ? a : 0);
                    return new RebarPositionInfo
                    {
                        Cntr = first.Cntr,
                        Poz = first.Poz,
                        Adet = totalAdet.ToString(),
                        Cap = first.Cap,
                        Aralik = first.Aralik,
                        Boy = first.Boy,
                        Tip = first.Tip,
                        A = first.A,
                        B = first.B,
                        C = first.C,
                        D = first.D,
                        E = first.E,
                        F = first.F,
                        R = first.R,
                        BoyInt = first.BoyInt,
                        Malzeme = first.Malzeme
                    };
                })
                .OrderBy(x => int.TryParse(x.Poz, out int poz) ? poz : 9999)
                .ToList();
        }

        /// <summary>
        /// Создает список диаметров с суммарными длинами
        /// </summary>
        private static List<DiameterInfo> CreateDiameterList(List<RebarPositionInfo> data)
        {
            if (data == null || data.Count == 0) return new List<DiameterInfo>();

            return data
                .GroupBy(x => x.Cap)
                .Select(g => new DiameterInfo
                {
                    Cap = g.Key,
                    TotalLength = g.Sum(x => (int.TryParse(x.Adet, out int a) ? a : 0) * (double)x.BoyInt)
                })
                .OrderBy(x => int.TryParse(x.Cap, out int cap) ? cap : 9999)
                .ToList();
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

            if (fiIndex >= 0)
            {
                string leftPart = tb.Substring(0, fiIndex).ToUpper();
                
                // Если есть 'x', перемножаем все числа (например, "2x10" -> 20)
                if (leftPart.Contains("X"))
                {
                    string[] parts = leftPart.Split('X');
                    int total = 1;
                    bool found = false;
                    foreach (var p in parts)
                    {
                        string numStr = new string(p.Where(char.IsDigit).ToArray());
                        if (int.TryParse(numStr, out int val))
                        {
                            total *= val;
                            found = true;
                        }
                    }
                    return found ? total : 1;
                }
                else
                {
                    // Пытаемся найти число во всей левой части (например, "20")
                    string countStr = new string(leftPart.Where(char.IsDigit).ToArray());
                    if (int.TryParse(countStr, out int adet))
                    {
                        return adet;
                    }
                }
                
                // Если число не найдено, но Ø есть, значит количество 1 (например, "SØ12")
                return 1;
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
                string rest = tb.Substring(fiIndex + fiLength);
                // Читаем цифры после символа диаметра
                string capStr = new string(rest.TakeWhile(char.IsDigit).ToArray());
                if (!string.IsNullOrEmpty(capStr))
                {
                    return capStr;
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
                string rest = tb.Substring(slashIndex + 1);
                // Читаем цифры после косой черты
                string aralikStr = new string(rest.TakeWhile(char.IsDigit).ToArray());
                return aralikStr;
            }

            return "";
        }

        private static int ParseBoyInt(string boy)
        {
            if (string.IsNullOrEmpty(boy)) return 0;

            // Извлекаем только первое число из строки (например, "L=2260 (2265)" -> 2260)
            string digits = "";
            bool foundDigit = false;
            foreach (char c in boy)
            {
                if (char.IsDigit(c))
                {
                    digits += c;
                    foundDigit = true;
                }
                else if (foundDigit)
                {
                    // Если уже нашли цифры и встретили не-цифру, прерываем (конец первого числа)
                    break;
                }
            }

            if (int.TryParse(digits, out int result))
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
        /// </summary>
        private static string GetClientPath()
        {
            try
            {
                // 1. Проверяем папку Temp рядом с исполняемым файлом (для разработки)
                string assemblyPath = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
                string tempPath = Path.Combine(assemblyPath, "Temp");
                if (Directory.Exists(tempPath))
                {
                    return tempPath + Path.DirectorySeparatorChar;
                }

                // 2. Папка Standard рядом с DLL
                string standardPath = Path.Combine(assemblyPath, "Standard");
                if (Directory.Exists(standardPath))
                {
                    return assemblyPath + Path.DirectorySeparatorChar;
                }

                // 3. Папка пользователя
                string userPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                    "PoseEdit2026");
                if (Directory.Exists(userPath))
                {
                    return userPath + Path.DirectorySeparatorChar;
                }

                return assemblyPath + Path.DirectorySeparatorChar;
            }
            catch
            {
                return AppDomain.CurrentDomain.BaseDirectory;
            }
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
        private static double GetUnits()
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
        private static double GetScale()
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
                    "ren.mtr.layer_l1",    // Основные линии таблиц
                    "ren.mtr.layer_t1",    // Текст таблиц
                    "ren.mtr.bar"          // Слой для таблиц
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
        // РИСОВАНИЕ ТАБЛИЦ
        // ====================================================================================

        private static void DrawTablesRussian(Point3d yerlesim, List<DiameterInfo> capListe, List<RebarPositionInfo> toplamBilgi, double olcek)
        {
            DrawTablesGeneric(yerlesim, capListe, toplamBilgi, olcek, "rus");
        }

        private static void DrawTablesEnglish(Point3d yerlesim, List<DiameterInfo> capListe, List<RebarPositionInfo> toplamBilgi, double olcek)
        {
            DrawTablesGeneric(yerlesim, capListe, toplamBilgi, olcek, "eng");
        }

        private static void DrawTablesRussianEnglish(Point3d yerlesim, List<DiameterInfo> capListe, List<RebarPositionInfo> toplamBilgi, double olcek)
        {
            DrawTablesGeneric(yerlesim, capListe, toplamBilgi, olcek, "re");
        }

        /// <summary>
        /// Построение таблиц в виде примитивов (Line, DBText).
        /// Размеры рассчитываются в мм на бумаге и умножаются на масштаб.
        /// </summary>
        private static void DrawTablesGeneric(Point3d yerlesim, List<DiameterInfo> capListe, List<RebarPositionInfo> toplamBilgi, double olcekMultiplier, string lang)
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            if (doc == null) return;
            Database db = doc.Database;

            // Коэффициент масштаба (например, 50 для 1:50)
            double scale = GetScale();
            
            // Размеры в мм на бумаге
            double paperTextHData = 3.0;   // 3мм обычный текст
            double paperTextHHeader = 5.0; // 5мм заголовок проекта
            double paperRowH = 8.0;        // 8мм высота строки (стандарт)

            // Конвертация в единицы модели
            double textHData = paperTextHData * scale;
            double textHHeader = paperTextHHeader * scale;
            double rowH = paperRowH * scale;

            // Компактные ширины столбцов в мм на бумаге
            double[] paperColWidths = { 
                12,  // ПОЗ
                10,  // Ø
                12,  // К-во
                12,  // Шаг
                22,  // Длина
                10,  // Тип
                12, 12, 12, 12, 12, 12, 12, // A-R (размеры гибки)
                22   // Итог
            };
            double[] colWidths = paperColWidths.Select(w => w * scale).ToArray();

            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                BlockTableRecord btr = (BlockTableRecord)tr.GetObject(db.CurrentSpaceId, OpenMode.ForWrite);

                // --- 1. ТАБЛИЦА СВОДКИ ПО ДИАМЕТРАМ ---
                string hdrCap = "Ø";
                string hdrLen = lang == "rus" ? "Длина, м" : (lang == "re" ? "Len/Дл, m" : "Length, m");
                
                Point3d currentPt = yerlesim;
                double diaColW1 = 15 * scale;
                double diaColW2 = 30 * scale;

                string projName = AppSettings.ProjectName;
                if (!string.IsNullOrWhiteSpace(projName))
                {
                    DrawCell(btr, tr, currentPt, diaColW1 + diaColW2, rowH * 1.2, projName, textHHeader, true);
                    currentPt = new Point3d(currentPt.X, currentPt.Y - rowH * 1.2, 0);
                }

                // Заголовки сводки
                DrawCell(btr, tr, currentPt, diaColW1, rowH, hdrCap, textHData, true);
                DrawCell(btr, tr, new Point3d(currentPt.X + diaColW1, currentPt.Y, 0), diaColW2, rowH, hdrLen, textHData, true);
                currentPt = new Point3d(currentPt.X, currentPt.Y - rowH, 0);

                foreach (var info in capListe)
                {
                    double lenM = info.TotalLength * 0.001;
                    DrawCell(btr, tr, currentPt, diaColW1, rowH, info.Cap, textHData, false);
                    DrawCell(btr, tr, new Point3d(currentPt.X + diaColW1, currentPt.Y, 0), diaColW2, rowH, lenM.ToString("0.###", CultureInfo.InvariantCulture), textHData, false);
                    currentPt = new Point3d(currentPt.X, currentPt.Y - rowH, 0);
                }

                // --- 2. ТАБЛИЦА ДЕТАЛИЗАЦИИ ПО ПОЗИЦИЯМ ---
                currentPt = new Point3d(yerlesim.X, currentPt.Y - (rowH * 2), 0); // Отступ между таблицами
                double totalMainW = colWidths.Sum();

                if (!string.IsNullOrWhiteSpace(projName))
                {
                    DrawCell(btr, tr, currentPt, totalMainW, rowH * 1.2, projName, textHHeader, true);
                    currentPt = new Point3d(currentPt.X, currentPt.Y - rowH * 1.2, 0);
                }

                string[] headers = lang switch
                {
                    "rus" => new[] { "ПОЗ", "Ø", "К-во", "Шаг", "Длина", "Тип", "A", "B", "C", "D", "E", "F", "R", "Итог, м" },
                    "re"  => new[] { "POZ", "Ø", "Qty", "Step", "Len", "Tip", "A", "B", "C", "D", "E", "F", "R", "Tot m" },
                    _     => new[] { "POZ", "Ø", "Qty", "Step", "Len", "Tip", "A", "B", "C", "D", "E", "F", "R", "Tot m" }
                };

                // Заголовки основной таблицы
                double xCoord = currentPt.X;
                for (int i = 0; i < headers.Length; i++)
                {
                    DrawCell(btr, tr, new Point3d(xCoord, currentPt.Y, 0), colWidths[i], rowH, headers[i], textHData, true);
                    xCoord += colWidths[i];
                }
                currentPt = new Point3d(currentPt.X, currentPt.Y - rowH, 0);

                // Данные основной таблицы
                foreach (var it in toplamBilgi)
                {
                    double adetValue = int.TryParse(it.Adet, out var a) ? a : 0;
                    double totalLenM = adetValue * it.BoyInt * 0.001;

                    string[] vals = { 
                        it.Poz, 
                        it.Cap, 
                        it.Adet, 
                        it.Aralik == "0" ? "-" : it.Aralik, 
                        it.Boy, 
                        it.Tip, 
                        it.A, it.B, it.C, it.D, it.E, it.F, it.R, 
                        totalLenM.ToString("0.###", CultureInfo.InvariantCulture) 
                    };

                    xCoord = currentPt.X;
                    for (int i = 0; i < vals.Length; i++)
                    {
                        DrawCell(btr, tr, new Point3d(xCoord, currentPt.Y, 0), colWidths[i], rowH, vals[i], textHData, false);
                        xCoord += colWidths[i];
                    }
                    currentPt = new Point3d(currentPt.X, currentPt.Y - rowH, 0);
                }

                tr.Commit();
            }
        }

        /// <summary>
        /// Вспомогательный метод для отрисовки ячейки (линии + текст)
        /// </summary>
        private static void DrawCell(BlockTableRecord btr, Transaction tr, Point3d topLeft, double w, double h, string text, double textH, bool isHeader)
        {
            // Прямоугольник (линии)
            Point3d p1 = topLeft;
            Point3d p2 = new Point3d(topLeft.X + w, topLeft.Y, 0);
            Point3d p3 = new Point3d(topLeft.X + w, topLeft.Y - h, 0);
            Point3d p4 = new Point3d(topLeft.X, topLeft.Y - h, 0);

            Line[] lines = { new Line(p1, p2), new Line(p2, p3), new Line(p3, p4), new Line(p4, p1) };

            foreach (var l in lines)
            {
                l.Layer = "ren.mtr.layer_l1";
                btr.AppendEntity(l);
                tr.AddNewlyCreatedDBObject(l, true);
            }

            // Текст
            if (!string.IsNullOrEmpty(text))
            {
                DBText dbText = new DBText();
                dbText.Height = textH;
                dbText.TextString = text;
                dbText.Layer = "ren.mtr.layer_t1";
                dbText.Justify = AttachmentPoint.MiddleCenter;
                dbText.AlignmentPoint = new Point3d(topLeft.X + w / 2.0, topLeft.Y - h / 2.0, 0);
                
                btr.AppendEntity(dbText);
                tr.AddNewlyCreatedDBObject(dbText, true);
            }
        }
    }
}

