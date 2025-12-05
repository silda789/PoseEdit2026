// #nullable disable — отключаем строгую проверку на null (пустые значения).
// В .NET 8 это включено по умолчанию, и без этой строки будет много желтых предупреждений,
// которые новичку только мешают.
#nullable disable

using System;
using System.IO; // Работа с файловой системой (Path, File, FileStream)
using System.Reflection; // "Рефлексия" - позволяет программе смотреть внутрь самой себя (нужно для ресурсов)

// --- ПРОСТРАНСТВА ИМЕН AUTOCAD ---
using Autodesk.AutoCAD.Runtime; // Атрибуты команд [CommandMethod]
using Autodesk.AutoCAD.ApplicationServices; // Доступ к приложению (Application, Document)
using Autodesk.AutoCAD.DatabaseServices; // Работа с базой DWG (Transaction, BlockTable, Entity)
using Autodesk.AutoCAD.EditorInput; // Взаимодействие с пользователем (Selection, Point)
using Autodesk.AutoCAD.Geometry; // Геометрия (Point3d, Scale3d)
using Autodesk.AutoCAD.Colors; // Цвета (Color)

namespace PoseEdit2026
{
    // Этот класс не обязательно должен быть static, но методы команд должны быть public.
    public class Commands
    {
        // ---------------------------------------------------------------------------------------
        // КОНСТАНТЫ И НАСТРОЙКИ
        // ---------------------------------------------------------------------------------------

        // Полные имена ресурсов (файлов DWG), зашитых внутри нашей DLL.
        // Формат строго такой: "ИмяПроекта.Папка.ИмяФайла.расширение"
        // Если проект "PoseEdit2026", папка "Resources", файл "RL-POS.dwg":
        private const string ResourceName1 = "PoseEdit2026.Resources.RL-POS.dwg";
        private const string ResourceName2 = "PoseEdit2026.Resources.RL-POS2.dwg";

        // Имя слоя, на который мы будем автоматически помещать наши блоки.
        private const string TargetLayer = "ren.mtr.tb";

        // ---------------------------------------------------------------------------------------
        // ГЛАВНАЯ КОМАНДА "EEN"
        // ---------------------------------------------------------------------------------------
        // [CommandMethod] регистрирует команду в AutoCAD. 
        // Когда пользователь введет "EEN", вызовется этот метод.
        [CommandMethod("EEN")]
        public void EditPoseCommand()
        {
            // Получаем доступ к текущему открытому чертежу (Document)
            Document doc = Application.DocumentManager.MdiActiveDocument;
            if (doc == null) return; // Если нет открытых документов - выходим

            Editor ed = doc.Editor;     // "Редактор" - отвечает за общение (ввод/вывод)
            Database db = doc.Database; // "База данных" - здесь хранятся все линии и блоки

            // 1. СОХРАНЕНИЕ ПОЛЬЗОВАТЕЛЬСКИХ НАСТРОЕК
            // Мы будем менять системные переменные, чтобы команда работала корректно.
            // Хороший тон программиста — запомнить, как было, и вернуть всё назад в конце.
            object oldClayer = Application.GetSystemVariable("CLAYER"); // Текущий слой
            object oldDimzin = Application.GetSystemVariable("DIMZIN"); // Подавление нулей
            object oldAttreq = Application.GetSystemVariable("ATTREQ"); // Запрос атрибутов при вставке

            // Логические флаги
            bool isNewBlock = false;            // Мы создаем новый или редактируем старый?
            ObjectId blockId = ObjectId.Null;   // ID блока, с которым будем работать
            Point3d insertPoint = Point3d.Origin; // Точка вставки (нужна для поворота)

            try
            {
                // 2. НАСТРОЙКА СРЕДЫ ПЕРЕД РАБОТОЙ
                // CMDECHO = 0: Отключаем "спам" в командной строке от выполняемых скриптов
                Application.SetSystemVariable("CMDECHO", 0);
                // DIMZIN = 1: Не подавлять нули (важно для корректного чтения чисел)
                Application.SetSystemVariable("DIMZIN", 1);
                // ATTREQ = 0: Самое важное! Отключаем запрос атрибутов. 
                // Иначе при вставке блока AutoCAD "зависнет", ожидая, что пользователь введет текст атрибутов.
                Application.SetSystemVariable("ATTREQ", 0);

                // Проверяем, существует ли слой "ren.mtr.tb". Если нет — создаем его.
                EnsureLayerExists(db, TargetLayer);

                // 3. ВЫБОР ОБЪЕКТА
                // Создаем фильтр выбора. Разрешаем выбирать только:
                // 1. Объекты типа INSERT (Блоки)
                // 2. С именами "RL-POS" или "RL-POS2"
                TypedValue[] filterList = new TypedValue[] {
                    new TypedValue((int)DxfCode.Start, "INSERT"),
                    new TypedValue((int)DxfCode.BlockName, "RL-POS,RL-POS2")
                };
                SelectionFilter filter = new SelectionFilter(filterList);

                // Настройки запроса
                PromptSelectionOptions selOpts = new PromptSelectionOptions();
                selOpts.MessageForAdding = "\nSelect position (RL-POS or RL-POS2) or press Enter for new: ";
                selOpts.SingleOnly = true; // Разрешаем выбрать только один объект за раз

                // Просим пользователя выбрать
                PromptSelectionResult selRes = ed.GetSelection(selOpts, filter);

                // 4. ЛОГИКА: РЕДАКТИРОВАНИЕ ИЛИ СОЗДАНИЕ?
                if (selRes.Status == PromptStatus.OK)
                {
                    // === ВЕТКА А: Пользователь выбрал блок ===
                    blockId = selRes.Value[0].ObjectId; // Запоминаем ID выбранного блока
                    isNewBlock = false;                 // Это не новый блок
                }
                else
                {
                    // === ВЕТКА Б: Пользователь нажал Enter (ничего не выбрал) ===
                    isNewBlock = true;

                    // Спрашиваем точку вставки
                    PromptPointOptions ptOpts = new PromptPointOptions("\nInsertion point for new position: ");
                    PromptPointResult ptRes = ed.GetPoint(ptOpts);

                    // Если пользователь нажал Esc — прерываем команду
                    if (ptRes.Status != PromptStatus.OK) return;

                    insertPoint = ptRes.Value;

                    // Переключаем текущий слой на наш целевой
                    Application.SetSystemVariable("CLAYER", TargetLayer);

                    // --- ВСТАВКА ИЗ РЕСУРСОВ ---
                    // Вызываем наш метод ImportBlockFromResource.
                    // Он достанет файл из DLL и вставит его в чертеж.
                    blockId = ImportBlockFromResource(db, ResourceName1, insertPoint, 1.0);

                    // Если метод вернул Null, значит произошла ошибка (например, неправильное имя ресурса)
                    if (blockId == ObjectId.Null)
                    {
                        ed.WriteMessage("\nError: Failed to load block from DLL resources.");
                        return;
                    }
                }

                // 5. ЗАПУСК ОКНА WPF
                // Создаем экземпляр нашего окна PoseEditWindow
                PoseEditWindow win = new PoseEditWindow(blockId);

                // ShowModalWindow — это специальный метод AutoCAD для запуска WPF окон.
                // Он блокирует AutoCAD, пока окно открыто.
                // Возвращает bool? (true, false или null).
                // true = мы закрыли окно через this.DialogResult = true (кнопка Update).
                bool? result = Application.ShowModalWindow(win);

                // Если пользователь нажал "Update Pose"
                if (result == true)
                {
                    // Если блок был НОВЫМ, нужно дать пользователю повернуть его.
                    // В .NET сложно сделать интерактивный поворот (Jig), поэтому мы просто
                    // вызываем стандартную команду _.ROTATE.
                    if (isNewBlock)
                    {
                        ed.Command("_.ROTATE", blockId, "", insertPoint);
                    }
                }
                // Если пользователь нажал "Discard" (Отмена) или закрыл крестиком
                else if (isNewBlock)
                {
                    // Если блок был новым, его надо удалить, чтобы не оставлять мусор на чертеже.
                    using (Transaction tr = db.TransactionManager.StartTransaction())
                    {
                        // Проверяем, жив ли еще объект (вдруг пользователь успел его удалить)
                        if (!blockId.IsNull && !blockId.IsErased)
                        {
                            Entity ent = tr.GetObject(blockId, OpenMode.ForWrite) as Entity;
                            ent.Erase(); // Удаляем
                        }
                        tr.Commit();
                    }
                }
            }
            catch (System.Exception ex)
            {
                // Ловим любые неожиданные ошибки и пишем их в командную строку
                ed.WriteMessage("\nCritical error: " + ex.Message);
            }
            finally
            {
                // 6. БЛОК FINALLY (ВЫПОЛНЯЕТСЯ ВСЕГДА)
                // Восстанавливаем системные переменные, даже если программа упала с ошибкой.
                try
                {
                    if (oldClayer != null) Application.SetSystemVariable("CLAYER", oldClayer);
                    if (oldDimzin != null) Application.SetSystemVariable("DIMZIN", oldDimzin);
                    if (oldAttreq != null) Application.SetSystemVariable("ATTREQ", oldAttreq);
                    Application.SetSystemVariable("CMDECHO", 1);
                }
                catch { /* Игнорируем ошибки при восстановлении */ }
            }
        }

        // ---------------------------------------------------------------------------------------
        // МЕТОД: ИМПОРТ БЛОКА ИЗ РЕСУРСОВ DLL
        // ---------------------------------------------------------------------------------------
        private ObjectId ImportBlockFromResource(Database db, string resourceId, Point3d position, double scale)
        {
            // Получаем ссылку на текущую сборку (наш файл .dll)
            Assembly assembly = Assembly.GetExecutingAssembly();

            // Вычисляем чистое имя блока (для AutoCAD) из имени ресурса.
            // Из "PoseEdit2026.Resources.RL-POS.dwg" делаем "RL-POS".
            string cleanBlockName = resourceId.Replace("PoseEdit2026.Resources.", "").Replace(".dwg", "");

            // Открываем поток данных (Stream) из ресурса
            using (Stream stream = assembly.GetManifestResourceStream(resourceId))
            {
                // Если stream == null, значит файл не найден (ошибка в имени ресурса)
                if (stream == null)
                {
                    string[] resources = assembly.GetManifestResourceNames();
                    Application.ShowAlertDialog("Resource not found: " + resourceId + "\n\nAvailable:\n" + string.Join("\n", resources));
                    return ObjectId.Null;
                }

                // AutoCAD не умеет вставлять блоки из потока памяти (Stream). Ему нужен файл на диске.
                // Поэтому мы создаем временный файл.
                string tempFile = Path.GetTempFileName();
                try
                {
                    // Копируем байты из DLL во временный файл
                    using (FileStream fileStream = File.OpenWrite(tempFile))
                    {
                        stream.CopyTo(fileStream);
                    }

                    ObjectId btrId = ObjectId.Null; // ID определения блока (Definition)

                    // Начинаем транзакцию
                    using (Transaction tr = db.TransactionManager.StartTransaction())
                    {
                        // Открываем таблицу блоков чертежа для записи
                        BlockTable bt = tr.GetObject(db.BlockTableId, OpenMode.ForWrite) as BlockTable;

                        // Проверяем: может, такой блок уже есть в чертеже?
                        if (bt.Has(cleanBlockName))
                        {
                            btrId = bt[cleanBlockName]; // Если есть, берем его ID
                        }
                        else
                        {
                            // Если нет, создаем "стороннюю базу данных" для временного файла
                            using (Database sourceDb = new Database(false, true))
                            {
                                // Читаем временный файл как DWG
                                sourceDb.ReadDwgFile(tempFile, FileOpenMode.OpenForReadAndAllShare, true, "");
                                // Вставляем (импортируем) блок в наш чертеж
                                btrId = db.Insert(cleanBlockName, sourceDb, true);
                            }
                        }

                        // Теперь создаем "Вхождение блока" (BlockReference) - то, что мы видим на экране
                        // btrId - это чертеж блока в памяти.
                        // blkRef - это ссылка на этот чертеж в конкретной точке.
                        BlockTableRecord btr = tr.GetObject(btrId, OpenMode.ForRead) as BlockTableRecord;

                        using (BlockReference blkRef = new BlockReference(position, btrId))
                        {
                            blkRef.ScaleFactors = new Scale3d(scale, scale, scale); // Масштаб

                            // Добавляем блок в текущее пространство (Модель или Лист)
                            BlockTableRecord curSpace = tr.GetObject(db.CurrentSpaceId, OpenMode.ForWrite) as BlockTableRecord;
                            curSpace.AppendEntity(blkRef);
                            tr.AddNewlyCreatedDBObject(blkRef, true); // Регистрируем новый объект

                            // --- КОПИРОВАНИЕ АТРИБУТОВ ---
                            // При программной вставке атрибуты НЕ создаются автоматически.
                            // Мы должны вручную пробежаться по определению блока и создать AttributeReference для каждого AttributeDefinition.

                            if (btr.HasAttributeDefinitions)
                            {
                                foreach (ObjectId id in btr)
                                {
                                    DBObject obj = tr.GetObject(id, OpenMode.ForRead);
                                    // Если объект - это определение атрибута
                                    if (obj is AttributeDefinition attDef)
                                    {
                                        using (AttributeReference attRef = new AttributeReference())
                                        {
                                            // Копируем свойства (позиция, стиль, тэг)
                                            attRef.SetAttributeFromBlock(attDef, blkRef.BlockTransform);
                                            // Добавляем к нашему блоку
                                            blkRef.AttributeCollection.AppendAttribute(attRef);
                                            tr.AddNewlyCreatedDBObject(attRef, true);
                                        }
                                    }
                                }
                            }
                            tr.Commit(); // Применяем изменения
                            return blkRef.ObjectId; // Возвращаем ID созданного блока
                        }
                    }
                }
                finally
                {
                    // Удаляем временный файл, чтобы не мусорить на диске
                    if (File.Exists(tempFile)) try { File.Delete(tempFile); } catch { }
                }
            }
        }

        // ---------------------------------------------------------------------------------------
        // МЕТОД: СОЗДАНИЕ СЛОЯ
        // ---------------------------------------------------------------------------------------
        private void EnsureLayerExists(Database db, string layerName)
        {
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                LayerTable lt = tr.GetObject(db.LayerTableId, OpenMode.ForRead) as LayerTable;

                // Если слоя нет - создаем
                if (!lt.Has(layerName))
                {
                    lt.UpgradeOpen(); // Разрешаем запись в таблицу слоев

                    LayerTableRecord newLayer = new LayerTableRecord();
                    newLayer.Name = layerName;
                    newLayer.Color = Color.FromColorIndex(ColorMethod.ByAci, 7); // Цвет 7 (Белый/Черный)

                    lt.Add(newLayer);
                    tr.AddNewlyCreatedDBObject(newLayer, true);
                }
                tr.Commit();
            }
        }
    }
}