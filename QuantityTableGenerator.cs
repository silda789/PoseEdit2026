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
                TypedValue[] filterList = new TypedValue[] {
                    new TypedValue((int)DxfCode.Start, "INSERT"),
                    new TypedValue((int)DxfCode.BlockName, "RL-POS")
                };
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

            for (int i = 0; i < data.Count; i++)
            {
                if (i < data.Count - 1 &&
                    data[i].Poz == data[i + 1].Poz &&
                    data[i].Cap == data[i + 1].Cap &&
                    data[i].Boy == data[i + 1].Boy)
                {
                    // Объединяем количество
                    int adet1 = int.TryParse(data[i].Adet, out int a1) ? a1 : 0;
                    int adet2 = int.TryParse(data[i + 1].Adet, out int a2) ? a2 : 0;
                    data[i].Adet = (adet1 + adet2).ToString();
                    i++; // Пропускаем следующий элемент
                }

                result.Add(data[i]);
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

            // Группируем одинаковые диаметры и суммируем длины
            List<DiameterInfo> result = new List<DiameterInfo>();
            for (int i = 0; i < tempList.Count; i++)
            {
                if (i < tempList.Count - 1 && tempList[i].Cap == tempList[i + 1].Cap)
                {
                    // Объединяем одинаковые диаметры
                    tempList[i].TotalLength += tempList[i + 1].TotalLength;
                    i++; // Пропускаем следующий элемент
                }
                result.Add(tempList[i]);
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
                int slashIndex = tb.IndexOf("/", fiIndex);
                if (slashIndex > fiIndex)
                {
                    string capStr = tb.Substring(fiIndex + fiLength, slashIndex - fiIndex - fiLength);
                    return capStr.Trim();
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

        private static int ParseBoyInt(string boy)
        {
            if (string.IsNullOrEmpty(boy)) return 0;

            // Убираем "L=" и другие символы
            boy = boy.Replace("L=", "").Replace("l=", "").Replace("(", "").Replace(")", "").Replace("~", "").Trim();

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
        private static string GetClientPath()
        {
            // 1. Проверяем папку Temp (для разработки и тестирования)
            string tempPath = @"C:\Users\durum\Documents\GitHub\PoseEdit2026\Temp";
            if (Directory.Exists(tempPath))
            {
                // Проверяем, есть ли там файлы настроек
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
                    List<string> settings = new List<string>();
                    
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
            return new string[] { "0", "0", "1" };
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

                string[] layers = {
                    "ren.mtr.layer_l1",    // Основные линии таблиц
                    "ren.mtr.layer_t1",    // Текст таблиц
                    "ren.mtr.bar"          // Слой для таблиц
                };

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
            // TODO: Реализовать рисование таблиц на русском языке
            // Аналог функции RENAISSANCE_metraj_tablo_ciz_rus
        }

        private static void DrawTablesEnglish(Point3d yerlesim, List<DiameterInfo> capListe, List<RebarPositionInfo> toplamBilgi, double olcek)
        {
            // TODO: Реализовать рисование таблиц на английском языке
            // Аналог функции RENAISSANCE_metraj_tablo_ciz_eng
        }

        private static void DrawTablesRussianEnglish(Point3d yerlesim, List<DiameterInfo> capListe, List<RebarPositionInfo> toplamBilgi, double olcek)
        {
            // TODO: Реализовать рисование таблиц на русском и английском языках
            // Аналог функции RENAISSANCE_metraj_tablo_ciz_re
        }
    }
}

