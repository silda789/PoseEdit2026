// ====================================================================================
// ФАЙЛ: LayerCreator.cs
// НАЗНАЧЕНИЕ: Класс для создания слоев (Layers) в AutoCAD
// ====================================================================================
// Этот класс содержит команду для создания слоев на основе настроек из WPF формы.
// Слои создаются с префиксом и другими настройками, указанными пользователем.
//
// ОСНОВНЫЕ ФУНКЦИИ:
//   1. Создание слоев из списка с применением префикса
//   2. Настройка цвета слоев
//   3. Настройка типа линий
//   4. Настройка веса линий
//
// КОМАНДА: CREATELAYERS (или CL)
// ====================================================================================

#nullable disable

using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Colors;

namespace PoseEdit2026
{
    // ====================================================================================
    // СТРУКТУРА: LayerCreationResult
    // ====================================================================================
    // Результат операции создания/обновления слоев
    // ====================================================================================
    public struct LayerCreationResult
    {
        public int Created;      // Количество созданных новых слоев
        public int Updated;      // Количество обновленных существующих слоев
        public int Skipped;      // Количество пропущенных слоев (если SkipExisting = true)
        public int Errors;       // Количество ошибок
        public int Total;        // Общее количество обработанных слоев

        public int SuccessCount => Created + Updated;
    }

    // ====================================================================================
    // КЛАСС: LayerCreator
    // ====================================================================================
    // Класс для создания слоев в AutoCAD чертеже
    // ====================================================================================
    public static class LayerCreator
    {
        // ====================================================================================
        // КОМАНДА: CREATELAYERS (или CL)
        // ====================================================================================
        // НАЗНАЧЕНИЕ: Создает слои на основе настроек из WPF формы
        // ====================================================================================
        [CommandMethod("CREATELAYERS")]
        [CommandMethod("CL")]
        public static void CreateLayersCommand()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            if (doc == null)
            {
                Application.ShowAlertDialog("No document is open.");
                return;
            }

            Editor ed = doc.Editor;

            try
            {
                // Открываем окно настроек для создания слоев
                LayerSettingsWindow settingsWindow = new LayerSettingsWindow();
                
                // Показываем окно как модальное
                bool? dialogResult = Application.ShowModalWindow(settingsWindow);
                
                // Если пользователь нажал OK (сохранил настройки)
                if (dialogResult == true)
                {
                    // Получаем настройки из окна
                    LayerSettings settings = settingsWindow.GetSettings();
                    
                    if (settings == null)
                    {
                        ed.WriteMessage("\nSozdanie sloev otmeneno.");
                        return;
                    }

                    // Получаем Database заново после закрытия модального окна
                    Document currentDoc = Application.DocumentManager.MdiActiveDocument;
                    if (currentDoc == null)
                    {
                        ed.WriteMessage("\nNet otkrytogo dokumenta.");
                        return;
                    }
                    Database db = currentDoc.Database;

                    // Создаем слои
                    LayerCreationResult result = CreateLayers(db, settings);

                    // Детальное сообщение о результатах
                    if (result.SuccessCount > 0)
                    {
                        string message = $"\nObrabotano sloev: {result.SuccessCount}:";
                        if (result.Created > 0)
                            message += $" sozdano {result.Created}";
                        if (result.Updated > 0)
                            message += $" obnovleno {result.Updated}";
                        if (result.Skipped > 0)
                            message += $" propuscheno {result.Skipped} (uzhe sushchestvuyut)";
                        if (result.Errors > 0)
                            message += $" oshibok {result.Errors}";
                        message += $" iz {result.Total} vybrannyh.";
                        ed.WriteMessage(message);
                    }
                    else
                    {
                        if (result.Total == 0)
                        {
                            ed.WriteMessage("\nSloi ne vybrany.");
                        }
                        else if (result.Skipped > 0 && settings.SkipExisting)
                        {
                            ed.WriteMessage($"\nSloi ne sozdany. Vse {result.Total} vybrannyh sloya(ev) uzhe sushchestvuyut (Skip existing = ON).");
                            ed.WriteMessage("\nSovet: snimite galochku 'Skip existing layers', chtoby obnovit sushchestvuyushchie sloi.");
                        }
                        else if (result.Errors > 0)
                        {
                            ed.WriteMessage($"\nNe udalos obrabotat sloi. Oshibok: {result.Errors}.");
                        }
                        else
                        {
                            ed.WriteMessage($"\nNi odin sloi ne obrabotan. Proverte nastroiki.");
                        }
                    }
                }
                else
                {
                    ed.WriteMessage("\nSozdanie sloev otmeneno.");
                }
            }
            catch (System.Exception ex)
            {
                ed.WriteMessage($"\nOshibka pri sozdanii sloev: {ex.Message}");
                Application.ShowAlertDialog($"Error creating layers:\n{ex.Message}");
            }
        }

        // ====================================================================================
        // МЕТОД: CreateLayers
        // ====================================================================================
        // НАЗНАЧЕНИЕ: Создает слои в базе данных AutoCAD на основе настроек
        //
        // ПАРАМЕТРЫ:
        //   db - база данных AutoCAD (Database)
        //   settings - настройки для создания слоев (LayerSettings)
        //
        // ВОЗВРАЩАЕТ:
        //   LayerCreationResult - детальная статистика операции
        // ====================================================================================
        public static LayerCreationResult CreateLayers(Database db, LayerSettings settings)
        {
            LayerCreationResult result = new LayerCreationResult();

            if (db == null || settings == null) return result;

            // Используем новый формат с LayerDefinitions, если доступен
            List<LayerDefinition> layersToCreate = new List<LayerDefinition>();
            
            if (settings.LayerDefinitions != null && settings.LayerDefinitions.Count > 0)
            {
                layersToCreate = settings.LayerDefinitions;
            }

            if (layersToCreate.Count == 0) return result;

            result.Total = layersToCreate.Count;

            // Создаем все слои в одной транзакции (как в рабочем коде CreateMetrajLayers)
            // Это должно избежать проблем с блокировками при создании множества слоев
            List<string> createdLayerNames = new List<string>();
            
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                try
                {
                    // Получаем таблицу слоев
                    LayerTable lt = tr.GetObject(db.LayerTableId, OpenMode.ForRead) as LayerTable;

                    foreach (LayerDefinition layerDef in layersToCreate)
                    {
                        // Пропускаем пустые имена
                        if (string.IsNullOrWhiteSpace(layerDef.Name)) continue;

                        // Формируем полное имя слоя с префиксом
                        string fullLayerName = string.IsNullOrWhiteSpace(settings.Prefix)
                            ? layerDef.Name.Trim()
                            : $"{settings.Prefix.Trim()}.{layerDef.Name.Trim()}";

                        // Проверяем, существует ли слой
                        if (lt.Has(fullLayerName))
                        {
                            // Если слой уже существует и настройка "Skip existing" включена - пропускаем
                            if (settings.SkipExisting)
                            {
                                result.Skipped++;
                                continue;
                            }
                            // Если слой существует и "Skip existing" выключен - обновим его позже
                            else
                            {
                                createdLayerNames.Add(fullLayerName);
                                continue;
                            }
                        }

                        // Создаем новый слой (как в рабочем коде CreateMetrajLayers)
                        try
                        {
                            if (!lt.IsWriteEnabled)
                            {
                                lt.UpgradeOpen();
                            }

                            LayerTableRecord newLayer = new LayerTableRecord();
                            newLayer.Name = fullLayerName;
                            newLayer.Color = Color.FromColorIndex(ColorMethod.ByAci, layerDef.ColorIndex); // Цвет из определения

                            lt.Add(newLayer);
                            tr.AddNewlyCreatedDBObject(newLayer, true);
                            
                            // НЕ вызываем DowngradeOpen между слоями - таблица останется в режиме записи до конца транзакции

                            result.Created++;
                            createdLayerNames.Add(fullLayerName);
                        }
                        catch (System.Exception ex)
                        {
                            // Если не удалось создать слой, пропускаем его и продолжаем
                            result.Errors++;
                            string errorMsg = $"Failed to create layer '{fullLayerName}': {ex.GetType().Name} - {ex.Message}";
                            if (ex.InnerException != null)
                            {
                                errorMsg += $" (Inner: {ex.InnerException.Message})";
                            }
                            System.Diagnostics.Debug.WriteLine(errorMsg);
                            Editor ed = Application.DocumentManager.MdiActiveDocument?.Editor;
                            if (ed != null)
                            {
                                ed.WriteMessage($"\n{errorMsg}");
                            }
                            // Пытаемся вернуть режим чтения на случай ошибки
                            try
                            {
                                if (lt.IsWriteEnabled)
                                {
                                    lt.DowngradeOpen();
                                }
                            }
                            catch { }
                            continue;
                        }
                    }

                    tr.Commit();
                }
                catch (System.Exception ex)
                {
                    // В случае критической ошибки откатываем транзакцию
                    System.Diagnostics.Debug.WriteLine($"Critical error in CreateLayers: {ex.Message}");
                    throw; // Пробрасываем исключение дальше
                }
            }

            // ШАГ 2: Обновляем дополнительные свойства (тип линии, вес линии) для всех созданных/существующих слоев
            // Делаем это в отдельных транзакциях, чтобы избежать проблем с блокировками
            foreach (LayerDefinition layerDef in layersToCreate)
            {
                if (string.IsNullOrWhiteSpace(layerDef.Name)) continue;

                string fullLayerName = string.IsNullOrWhiteSpace(settings.Prefix)
                    ? layerDef.Name.Trim()
                    : $"{settings.Prefix.Trim()}.{layerDef.Name.Trim()}";

                // Обновляем только если слой был создан или должен быть обновлен
                if (!createdLayerNames.Contains(fullLayerName) && !settings.SkipExisting)
                    continue;

                using (Transaction tr = db.TransactionManager.StartTransaction())
                {
                    try
                    {
                        LayerTable lt = tr.GetObject(db.LayerTableId, OpenMode.ForRead) as LayerTable;
                        
                        if (lt.Has(fullLayerName))
                        {
                            ObjectId layerId = lt[fullLayerName];
                            LayerTableRecord layer = tr.GetObject(layerId, OpenMode.ForWrite) as LayerTableRecord;
                            
                            if (layer != null)
                            {
                                // Обновляем только дополнительные свойства (тип линии и вес линии)
                                // Цвет уже установлен при создании
                                
                                // Устанавливаем тип линии (без вызова команд AutoCAD)
                                if (!string.IsNullOrWhiteSpace(layerDef.Linetype))
                                {
                                    LinetypeTable ltt = tr.GetObject(db.LinetypeTableId, OpenMode.ForRead) as LinetypeTable;
                                    if (ltt != null && ltt.Has(layerDef.Linetype))
                                    {
                                        layer.LinetypeObjectId = ltt[layerDef.Linetype];
                                    }
                                }

                                // Устанавливаем вес линии
                                if (layerDef.LineWeightMM.HasValue)
                                {
                                    layer.LineWeight = GetLineWeightFromMM(layerDef.LineWeightMM.Value);
                                }
                                else
                                {
                                    layer.LineWeight = Autodesk.AutoCAD.DatabaseServices.LineWeight.ByLayer;
                                }

                                // Если это существующий слой, обновляем и цвет
                                if (createdLayerNames.Contains(fullLayerName) && !settings.SkipExisting)
                                {
                                    layer.Color = Color.FromColorIndex(ColorMethod.ByAci, layerDef.ColorIndex);
                                    if (result.Updated == 0) // Считаем только если еще не считали
                                    {
                                        result.Updated++;
                                    }
                                }
                            }
                        }
                        
                        tr.Commit();
                    }
                    catch (System.Exception ex)
                    {
                        // Игнорируем ошибки обновления свойств - слой уже создан
                        System.Diagnostics.Debug.WriteLine($"Failed to update properties for layer {fullLayerName}: {ex.Message}");
                        // Транзакция будет автоматически откачена при Dispose
                    }
                }
            }

            // Логируем статистику (для отладки)
            if (result.Skipped > 0 || result.Errors > 0 || result.Created > 0 || result.Updated > 0)
            {
                System.Diagnostics.Debug.WriteLine($"CreateLayers: Created={result.Created}, Updated={result.Updated}, Skipped={result.Skipped}, Errors={result.Errors}, Total={result.Total}");
            }

            return result;
        }

        // ====================================================================================
        // МЕТОД: UpdateLayerPropertiesFromDefinition
        // ====================================================================================
        // НАЗНАЧЕНИЕ: Обновляет свойства слоя из определения слоя
        //
        // ПАРАМЕТРЫ:
        //   layer - слой для обновления (LayerTableRecord)
        //   layerDef - определение слоя (LayerDefinition)
        //   db - база данных AutoCAD
        //   tr - транзакция (для получения типов линий)
        // ====================================================================================
        private static void UpdateLayerPropertiesFromDefinition(LayerTableRecord layer, LayerDefinition layerDef, Database db, Transaction tr)
        {
            if (layer == null || layerDef == null) return;

            // Устанавливаем цвет
            layer.Color = Color.FromColorIndex(ColorMethod.ByAci, layerDef.ColorIndex);

            // Устанавливаем тип линии
            if (!string.IsNullOrWhiteSpace(layerDef.Linetype))
            {
                ObjectId linetypeId = GetLinetypeId(db, layerDef.Linetype, tr);
                if (!linetypeId.IsNull)
                {
                    layer.LinetypeObjectId = linetypeId;
                }
            }

            // Устанавливаем вес линии
            if (layerDef.LineWeightMM.HasValue)
            {
                layer.LineWeight = GetLineWeightFromMM(layerDef.LineWeightMM.Value);
            }
            else
            {
                layer.LineWeight = Autodesk.AutoCAD.DatabaseServices.LineWeight.ByLayer;
            }
        }


        // ====================================================================================
        // МЕТОД: GetLineWeightFromMM
        // ====================================================================================
        // НАЗНАЧЕНИЕ: Преобразует миллиметры в LineWeight
        //
        // ПАРАМЕТРЫ:
        //   mm - значение в миллиметрах
        //
        // ВОЗВРАЩАЕТ:
        //   LineWeight - соответствующий вес линии
        // ====================================================================================
        private static LineWeight GetLineWeightFromMM(double mm)
        {
            // Преобразование миллиметров в LineWeight
            if (mm <= 0.00) return Autodesk.AutoCAD.DatabaseServices.LineWeight.ByLineWeightDefault;
            if (mm <= 0.05) return Autodesk.AutoCAD.DatabaseServices.LineWeight.LineWeight000;
            if (mm <= 0.09) return Autodesk.AutoCAD.DatabaseServices.LineWeight.LineWeight005;
            if (mm <= 0.13) return Autodesk.AutoCAD.DatabaseServices.LineWeight.LineWeight009;
            if (mm <= 0.15) return Autodesk.AutoCAD.DatabaseServices.LineWeight.LineWeight013;
            if (mm <= 0.18) return Autodesk.AutoCAD.DatabaseServices.LineWeight.LineWeight015;
            if (mm <= 0.20) return Autodesk.AutoCAD.DatabaseServices.LineWeight.LineWeight018;
            if (mm <= 0.25) return Autodesk.AutoCAD.DatabaseServices.LineWeight.LineWeight020;
            if (mm <= 0.30) return Autodesk.AutoCAD.DatabaseServices.LineWeight.LineWeight025;
            if (mm <= 0.35) return Autodesk.AutoCAD.DatabaseServices.LineWeight.LineWeight030;
            if (mm <= 0.40) return Autodesk.AutoCAD.DatabaseServices.LineWeight.LineWeight035;
            if (mm <= 0.50) return Autodesk.AutoCAD.DatabaseServices.LineWeight.LineWeight040;
            if (mm <= 0.53) return Autodesk.AutoCAD.DatabaseServices.LineWeight.LineWeight050;
            if (mm <= 0.60) return Autodesk.AutoCAD.DatabaseServices.LineWeight.LineWeight053;
            if (mm <= 0.70) return Autodesk.AutoCAD.DatabaseServices.LineWeight.LineWeight060;
            if (mm <= 0.80) return Autodesk.AutoCAD.DatabaseServices.LineWeight.LineWeight070;
            if (mm <= 0.90) return Autodesk.AutoCAD.DatabaseServices.LineWeight.LineWeight080;
            if (mm <= 1.00) return Autodesk.AutoCAD.DatabaseServices.LineWeight.LineWeight090;
            if (mm <= 1.06) return Autodesk.AutoCAD.DatabaseServices.LineWeight.LineWeight100;
            if (mm <= 1.20) return Autodesk.AutoCAD.DatabaseServices.LineWeight.LineWeight106;
            if (mm <= 1.40) return Autodesk.AutoCAD.DatabaseServices.LineWeight.LineWeight120;
            if (mm <= 1.58) return Autodesk.AutoCAD.DatabaseServices.LineWeight.LineWeight140;
            if (mm <= 2.00) return Autodesk.AutoCAD.DatabaseServices.LineWeight.LineWeight158;
            if (mm <= 2.11) return Autodesk.AutoCAD.DatabaseServices.LineWeight.LineWeight200;
            return Autodesk.AutoCAD.DatabaseServices.LineWeight.LineWeight211;
        }

        // ====================================================================================
        // МЕТОД: GetLinetypeId
        // ====================================================================================
        // НАЗНАЧЕНИЕ: Получает ID типа линии по имени, загружает стандартные типы, если их нет
        //
        // ПАРАМЕТРЫ:
        //   db - база данных AutoCAD
        //   linetypeName - имя типа линии
        //   tr - транзакция (если null, создается новая транзакция)
        //
        // ВОЗВРАЩАЕТ:
        //   ObjectId - ID типа линии или ObjectId.Null, если не найден и не может быть загружен
        // ====================================================================================
        private static ObjectId GetLinetypeId(Database db, string linetypeName, Transaction tr = null)
        {
            if (db == null || string.IsNullOrWhiteSpace(linetypeName)) return ObjectId.Null;

            bool shouldCommit = false;
            Transaction transaction = tr;
            
            // Если транзакция не передана, создаем новую
            if (transaction == null)
            {
                transaction = db.TransactionManager.StartTransaction();
                shouldCommit = true;
            }

            try
            {
                LinetypeTable lt = transaction.GetObject(db.LinetypeTableId, OpenMode.ForRead) as LinetypeTable;
                
                if (lt != null && lt.Has(linetypeName))
                {
                    return lt[linetypeName];
                }

                // Если тип линии не найден, пытаемся загрузить стандартные типы
                // Стандартные типы AutoCAD: Continuous, Dashed, Dotted, DashDot, Center, Hidden
                string[] standardLinetypes = ["Continuous", "Dashed", "Dotted", "DashDot", "Center", "Hidden"];
                
                if (Array.IndexOf(standardLinetypes, linetypeName) >= 0)
                {
                    // Пытаемся загрузить стандартный тип линии через команду AutoCAD
                    // Это безопаснее, чем пытаться создать его вручную
                    try
                    {
                        Editor ed = Application.DocumentManager.MdiActiveDocument?.Editor;
                        if (ed != null)
                        {
                            // Загружаем тип линии через команду AutoCAD
                            ed.Command("_.LINETYPE", "_LOAD", linetypeName, "acad.lin", "");
                            
                            // Проверяем снова после загрузки
                            if (lt.Has(linetypeName))
                            {
                                return lt[linetypeName];
                            }
                        }
                    }
                    catch
                    {
                        // Игнорируем ошибки загрузки - просто вернем Null
                    }
                }

                return ObjectId.Null;
            }
            finally
            {
                if (shouldCommit && transaction != null)
                {
                    transaction.Commit();
                    transaction.Dispose();
                }
            }
        }

        // ====================================================================================
        // МЕТОД: EnsureLayerExists
        // ====================================================================================
        // НАЗНАЧЕНИЕ: Создает слой, если он не существует (вспомогательный метод)
        //
        // ПАРАМЕТРЫ:
        //   db - база данных AutoCAD
        //   layerName - имя слоя
        //   colorIndex - индекс цвета (по умолчанию 7 - белый/черный)
        //
        // ВОЗВРАЩАЕТ:
        //   bool - true, если слой создан или уже существует, false в случае ошибки
        // ====================================================================================
        public static bool EnsureLayerExists(Database db, string layerName, short colorIndex = 7)
        {
            if (db == null || string.IsNullOrWhiteSpace(layerName)) return false;

            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                LayerTable lt = tr.GetObject(db.LayerTableId, OpenMode.ForRead) as LayerTable;

                if (!lt.Has(layerName))
                {
                    lt.UpgradeOpen();

                    LayerTableRecord newLayer = new LayerTableRecord();
                    newLayer.Name = layerName;
                    newLayer.Color = Color.FromColorIndex(ColorMethod.ByAci, colorIndex);

                    lt.Add(newLayer);
                    tr.AddNewlyCreatedDBObject(newLayer, true);
                }

                tr.Commit();
            }

            return true;
        }
    }

    // ====================================================================================
    // КЛАСС: LayerDefinition
    // ====================================================================================
    // НАЗНАЧЕНИЕ: Определение одного слоя с его параметрами
    // ====================================================================================
    public class LayerDefinition
    {
        public string Name { get; set; } = "";
        public short ColorIndex { get; set; } = 7;
        public string Linetype { get; set; } = "Continuous";
        public double? LineWeightMM { get; set; } = null; // Вес линии в миллиметрах (null = ByLayer)
        public string Description { get; set; } = "";
    }

    // ====================================================================================
    // КЛАСС: StandardLayers
    // ====================================================================================
    // НАЗНАЧЕНИЕ: Предустановленные стандартные слои из LAYERS-Model.pdf
    // ====================================================================================
    public static class StandardLayers
    {
        // ====================================================================================
        // МЕТОД: GetStandardLayers
        // ====================================================================================
        // НАЗНАЧЕНИЕ: Возвращает список стандартных слоев с их параметрами
        // ====================================================================================
        public static List<LayerDefinition> GetStandardLayers()
        {
            return new List<LayerDefinition>
            {
                new LayerDefinition { Name = "annotation", ColorIndex = 2, Linetype = "Continuous", LineWeightMM = 0.2, Description = "Текст и линии описаний и примечаний / Text and line descriptions and notes" },
                new LayerDefinition { Name = "arrow", ColorIndex = 34, Linetype = "Continuous", LineWeightMM = 0.2, Description = "Линии стрелок, указателей, диапазонов / Line arrows, pointers, ranges" },
                new LayerDefinition { Name = "axis", ColorIndex = 8, Linetype = "Center", LineWeightMM = 0.2, Description = "Координационные оси здания / Axis of the building" },
                new LayerDefinition { Name = "axis.circle", ColorIndex = 8, Linetype = "Center", LineWeightMM = 0.2, Description = "Обозначения координационных осей / Designation of the axes" },
                new LayerDefinition { Name = "axis.dimension", ColorIndex = 12, Linetype = "Center", LineWeightMM = 0.2, Description = "Осевые размеры / Axial dimension" },
                new LayerDefinition { Name = "axis.text", ColorIndex = 3, Linetype = "Continuous", LineWeightMM = 0.2, Description = "Текст названия осей / Text of axis name" },
                new LayerDefinition { Name = "beam", ColorIndex = 134, Linetype = "Continuous", LineWeightMM = 0.2, Description = "Линии балок на плане / Lines of beams on plan" },
                new LayerDefinition { Name = "beam.bot", ColorIndex = 135, Linetype = "Dashed", LineWeightMM = 0.2, Description = "Скрытые линии балок на плане / Hidden lines of beams on plan" },
                new LayerDefinition { Name = "beam.name", ColorIndex = 201, Linetype = "Continuous", LineWeightMM = 0.2, Description = "Маркировка балок / Beam marks" },
                new LayerDefinition { Name = "column", ColorIndex = 4, Linetype = "Continuous", LineWeightMM = 0.6, Description = "Линии колонн на плане / Lines of columns on plan" },
                new LayerDefinition { Name = "column.name", ColorIndex = 74, Linetype = "Continuous", LineWeightMM = 0.2, Description = "Маркировка колонн / Column marks" },
                new LayerDefinition { Name = "d-wall", ColorIndex = 140, Linetype = "Continuous", LineWeightMM = 0.6, Description = "Подпорные стены / Supporting walls" },
                new LayerDefinition { Name = "dimension", ColorIndex = 1, Linetype = "Continuous", LineWeightMM = 0.2, Description = "Размеры / Dimensions" },
                new LayerDefinition { Name = "elevation", ColorIndex = 3, Linetype = "Continuous", LineWeightMM = 0.2, Description = "Отметки / Elevations" },
                new LayerDefinition { Name = "finish.element", ColorIndex = 192, Linetype = "Continuous", LineWeightMM = 0.2, Description = "Элементы финишной отделки / Finishing elements" },
                new LayerDefinition { Name = "foundation", ColorIndex = 124, Linetype = "Continuous", LineWeightMM = 0.6, Description = "Линии фундаментов на плане / Lines of foundation on plan" },
                new LayerDefinition { Name = "hatch", ColorIndex = 8, Linetype = "Continuous", LineWeightMM = 0.2, Description = "Штриховки / Hatch" }, // 0.15-0.2, используем 0.2
                new LayerDefinition { Name = "hold", ColorIndex = 2, Linetype = "Continuous", LineWeightMM = 0.2, Description = "Удерживающие элементы / Holding elements" },
                new LayerDefinition { Name = "hole", ColorIndex = 162, Linetype = "Continuous", LineWeightMM = 0.2, Description = "Линии и обозначения отверстий / Lines and symbols of holes" },
                new LayerDefinition { Name = "joint", ColorIndex = 71, Linetype = "Continuous", LineWeightMM = 0.2, Description = "Элементы соединений и швов / Element of joints" },
                new LayerDefinition { Name = "Defpoints", ColorIndex = 9, Linetype = "Continuous", LineWeightMM = null, Description = "Непечатаемый слой / Nonprinting layer" },
                new LayerDefinition { Name = "pile", ColorIndex = 8, Linetype = "Continuous", LineWeightMM = 0.2, Description = "Сваи / Piles" },
                new LayerDefinition { Name = "reinforcement", ColorIndex = 7, Linetype = "Continuous", LineWeightMM = 0.6, Description = "Арматура / Reinforcement" },
                new LayerDefinition { Name = "rev", ColorIndex = 30, Linetype = "Continuous", LineWeightMM = 0.2, Description = "Облака и обозначения изменений / Revision cloud and mark" },
                new LayerDefinition { Name = "secondary", ColorIndex = 8, Linetype = "Dashed", LineWeightMM = 0.2, Description = "Вторичные линии и элементы / Secondary lines and elements" },
                new LayerDefinition { Name = "section", ColorIndex = 4, Linetype = "Continuous", LineWeightMM = 0.6, Description = "Линии конструкций в сечении / Lines of structures in section" },
                new LayerDefinition { Name = "slope", ColorIndex = 254, Linetype = "Continuous", LineWeightMM = 0.2, Description = "Обозначения уклонов / Symbols of slopes" },
                new LayerDefinition { Name = "stair", ColorIndex = 252, Linetype = "Continuous", LineWeightMM = 0.2, Description = "Линии лестниц на плане / Lines of stairs on plan" },
                new LayerDefinition { Name = "steel", ColorIndex = 8, Linetype = "Continuous", LineWeightMM = 0.2, Description = "Элементы стальных конструкций / Elements of steel structures" },
                new LayerDefinition { Name = "table", ColorIndex = 12, Linetype = "Continuous", LineWeightMM = 0.2, Description = "Линии и текст таблиц и спецификаций / Lines and text of tables and spec." },
                new LayerDefinition { Name = "text", ColorIndex = 3, Linetype = "Continuous", LineWeightMM = 0.2, Description = "Текст на чертежe / Text on the drawing" },
                new LayerDefinition { Name = "title", ColorIndex = 5, Linetype = "Continuous", LineWeightMM = 0.6, Description = "Основная надпись (штамп) на чертеже / Title blocks (stamp) on drawing" },
                new LayerDefinition { Name = "title.text", ColorIndex = 1, Linetype = "Continuous", LineWeightMM = 0.2, Description = "Текст в основной надписи (штампе) / Text in title block (stamp)" },
                new LayerDefinition { Name = "view", ColorIndex = 124, Linetype = "Continuous", LineWeightMM = 0.2, Description = "Линия проекции на плане и сечении / Projection line on plan and section" },
                new LayerDefinition { Name = "wall", ColorIndex = 150, Linetype = "Continuous", LineWeightMM = 0.6, Description = "Линии стен на плане / Lines of walls on plan" },
                new LayerDefinition { Name = "wall.name", ColorIndex = 53, Linetype = "Continuous", LineWeightMM = 0.2, Description = "Маркировка стен / Wall marks" },
                new LayerDefinition { Name = "line", ColorIndex = 8, Linetype = "Continuous", LineWeightMM = 0.2, Description = "Линии экспортируемые из CAD систем / Lines exported from CAD systems" },
                new LayerDefinition { Name = "line.thin", ColorIndex = 124, Linetype = "Continuous", LineWeightMM = 0.2, Description = "--- тонкие / ... thin" },
                new LayerDefinition { Name = "line.medium", ColorIndex = 254, Linetype = "Continuous", LineWeightMM = 0.3, Description = "--- средние / ... medium" },
                new LayerDefinition { Name = "line.wide", ColorIndex = 7, Linetype = "Continuous", LineWeightMM = 0.6, Description = "--- широкие / ... wide" },
                new LayerDefinition { Name = "line.hidden", ColorIndex = 8, Linetype = "Hidden", LineWeightMM = 0.2, Description = "--- скрытые штриховые / ... - hidden dashed" }
            };
        }
    }

    // ====================================================================================
    // КЛАСС: LayerSettings
    // ====================================================================================
    // НАЗНАЧЕНИЕ: Хранит настройки для создания слоев
    // ====================================================================================
    public class LayerSettings
    {
        // Список определений слоев (с индивидуальными параметрами)
        public List<LayerDefinition> LayerDefinitions { get; set; } = new List<LayerDefinition>();

        // Префикс для слоев (например: "ren", "model", "arch", "std")
        public string Prefix { get; set; } = "";

        // Пропускать существующие слои (не обновлять их)
        public bool SkipExisting { get; set; } = true;
    }
}


