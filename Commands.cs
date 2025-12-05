using System;
using System.IO; // Для работы с файлами (FileStream, Path)
using System.Reflection; // Для работы с ресурсами внутри DLL (Assembly)
using Autodesk.AutoCAD.Runtime; // Для регистрации команд [CommandMethod]
using Autodesk.AutoCAD.ApplicationServices; // Для доступа к Документу и Приложению
using Autodesk.AutoCAD.DatabaseServices; // Для работы с Базой Данных (BlockTable, LayerTable)
using Autodesk.AutoCAD.EditorInput; // Для взаимодействия с пользователем (выбор, клики)
using Autodesk.AutoCAD.Geometry; // Для точек (Point3d) и векторов
using Autodesk.AutoCAD.Colors; // Для работы с цветами слоев

namespace PoseEdit2026
{
    public class Commands
    {
        // ---------------------------------------------------------------------------------------
        // НАСТРОЙКИ РЕСУРСОВ
        // ---------------------------------------------------------------------------------------
        // Имя ресурса теперь: PoseEdit2026.Resources.RL-POS.dwg
        private const string ResourceName1 = "PoseEdit2026.Resources.RL-POS.dwg";
        private const string ResourceName2 = "PoseEdit2026.Resources.RL-POS2.dwg";

        // Имя слоя, на который мы будем закидывать наши блоки
        private const string TargetLayer = "ren.mtr.tb";

        // ---------------------------------------------------------------------------------------
        // ОСНОВНАЯ КОМАНДА "EEN"
        // ---------------------------------------------------------------------------------------
        [CommandMethod("EEN")]
        public void EditPoseCommand()
        {
            // Получаем доступ к текущему активному чертежу и его базе данных
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Editor ed = doc.Editor;     // "Редактор" - отвечает за ввод/вывод (сообщения, клики)
            Database db = doc.Database; // "База данных" - хранит все линии, блоки и слои

            // 1. СОХРАНЕНИЕ НАСТРОЕК
            // Мы будем менять системные переменные, чтобы команда работала чисто.
            // Но хороший тон программиста - вернуть всё как было после завершения.
            object oldClayer = Application.GetSystemVariable("CLAYER"); // Текущий слой
            object oldDimzin = Application.GetSystemVariable("DIMZIN"); // Подавление нулей в размерах
            object oldAttreq = Application.GetSystemVariable("ATTREQ"); // Запрос атрибутов при вставке

            // Переменные для логики работы
            bool isNewBlock = false;            // Флаг: мы редактируем старый или создаем новый?
            ObjectId blockId = ObjectId.Null;   // ID блока, с которым будем работать
            Point3d insertPoint = Point3d.Origin; // Точка вставки (нужна для поворота в конце)

            try
            {
                // 2. НАСТРОЙКА СРЕДЫ
                // Отключаем эхо команд (чтобы в командной строке не спамило)
                Application.SetSystemVariable("CMDECHO", 0);
                // Настраиваем формат чисел (1 означает не подавлять нули, если нужно)
                Application.SetSystemVariable("DIMZIN", 1);
                // Отключаем запрос атрибутов. Мы заполним их программно. 
                // Если оставить 1, AutoCAD спросит пользователя ввести значения в командной строке.
                Application.SetSystemVariable("ATTREQ", 0);

                // Проверяем, есть ли нужный слой. Если нет - создаем.
                EnsureLayerExists(db, TargetLayer);

                // 3. ВЫБОР ОБЪЕКТА
                // Настраиваем фильтр. Разрешаем выбирать только Блоки (INSERT) с именами RL-POS или RL-POS2.
                TypedValue[] filterList = new TypedValue[] {
                    new TypedValue((int)DxfCode.Start, "INSERT"), // Тип сущности
                    new TypedValue((int)DxfCode.BlockName, "RL-POS,RL-POS2") // Имена (через запятую)
                };
                SelectionFilter filter = new SelectionFilter(filterList);

                // Настройки запроса выбора
                PromptSelectionOptions selOpts = new PromptSelectionOptions();
                //selOpts.MessageForAdding = "\nВыберите позицию (RL-POS или RL-POS2) или нажмите Enter для новой: ";
                selOpts.MessageForAdding = "\nSelect position (RL-POS or RL-POS2) or press Enter for new: ";
                selOpts.SingleOnly = true; // Разрешаем выбрать только ОДИН объект

                // Запрашиваем выбор у пользователя
                PromptSelectionResult selRes = ed.GetSelection(selOpts, filter);

                // 4. ЛОГИКА ВЕТВЛЕНИЯ
                if (selRes.Status == PromptStatus.OK)
                {
                    // === ВЕТКА А: Пользователь выбрал существующий блок ===
                    blockId = selRes.Value[0].ObjectId; // Берем ID выбранного блока
                    isNewBlock = false;                 // Это не новый блок
                }
                else
                {
                    // === ВЕТКА Б: Пользователь нажал Enter (ничего не выбрал) -> Создаем НОВЫЙ ===
                    isNewBlock = true;

                    // Спрашиваем, куда поставить новый блок
                    //PromptPointOptions ptOpts = new PromptPointOptions("\nТочка вставки новой позиции: ");
                    PromptPointOptions ptOpts = new PromptPointOptions("\nInsertion point for new position: ");
                    PromptPointResult ptRes = ed.GetPoint(ptOpts);

                    // Если пользователь нажал Esc на этапе точки - прерываем команду
                    if (ptRes.Status != PromptStatus.OK) return;

                    insertPoint = ptRes.Value;

                    // Переключаем текущий слой на наш целевой, чтобы блок лег куда надо
                    Application.SetSystemVariable("CLAYER", TargetLayer);

                    // --- ВСТАВКА ИЗ РЕСУРСОВ ---
                    // По умолчанию вставляем RL-POS (как в оригинальном Лиспе).
                    // Если захочешь сделать выбор, здесь можно добавить условие.
                    string resourceToUse = ResourceName1;

                    // Вызываем наш мощный метод импорта (описан ниже)
                    blockId = ImportBlockFromResource(db, resourceToUse, insertPoint, 1.0);

                    // Если метод вернул Null, значит что-то пошло не так (например, имя ресурса кривое)
                    if (blockId == ObjectId.Null)
                    {
                        //ed.WriteMessage("\nОшибка: Не удалось загрузить блок из ресурсов DLL. Проверьте имя ресурса!");
                        ed.WriteMessage("\nError: Failed to load block from DLL resources. Check resource name!");
                        return;
                    }
                }

                // 5. ЗАПУСК ФОРМЫ
                // Конструкция 'using' гарантирует, что форма правильно удалится из памяти после закрытия
                using (PoseEditForm form = new PoseEditForm(blockId))
                {
                    // Показываем форму как "Модальное окно" (блокирует AutoCAD, пока не закроешь)
                    System.Windows.Forms.DialogResult result = Application.ShowModalDialog(form);

                    // Если пользователь нажал кнопку "Update Pose" (мы там прописали DialogResult.OK)
                    if (result == System.Windows.Forms.DialogResult.OK)
                    {
                        // Если блок был НОВЫМ, нужно дать пользователю его повернуть.
                        // В Лиспе это было: (command "rotate" ...)
                        if (isNewBlock)
                        {
                            // Вызываем штатную команду AutoCAD, чтобы пользователь увидел "резинку" поворота
                            ed.Command("_.ROTATE", blockId, "", insertPoint);
                        }
                    }
                    else if (isNewBlock) // Если нажали "Discard" (Отмена) или крестик
                    {
                        // Если мы создали новый блок, но передумали его заполнять - его надо удалить.
                        // Иначе на чертеже останется пустой блок.
                        using (Transaction tr = db.TransactionManager.StartTransaction())
                        {
                            // Проверяем, существует ли блок (вдруг его удалили руками)
                            if (!blockId.IsNull && !blockId.IsErased)
                            {
                                Entity ent = tr.GetObject(blockId, OpenMode.ForWrite) as Entity;
                                ent.Erase(); // Удаляем
                            }
                            tr.Commit();
                        }
                    }
                }
            }
            catch (System.Exception ex)
            {
                // Если случилась любая ошибка - пишем её в консоль
                ed.WriteMessage("\nКритическая ошибка в команде EE: " + ex.Message);
            }
            finally
            {
                // 6. ВОССТАНОВЛЕНИЕ НАСТРОЕК
                // Блок 'finally' выполняется ВСЕГДА, даже если была ошибка.
                // Это гарантирует, что мы не сломаем настройки пользователя.
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
        // МЕТОД ИМПОРТА: Достает DWG из DLL и вставляет в чертеж
        // ---------------------------------------------------------------------------------------
        private ObjectId ImportBlockFromResource(Database db, string resourceId, Point3d position, double scale)
        {
            // Получаем доступ к текущей запущенной DLL (нашей программе)
            Assembly assembly = Assembly.GetExecutingAssembly();

            // Определяем "Чистое имя блока".
            // Из строки "AutoCAD2024Final.Resources.RL-POS.dwg" делаем "RL-POS".
            // Это нужно, чтобы в таблице блоков AutoCAD он назывался красиво.
            string cleanBlockName = resourceId;
            // Убираем начало (namespace + папка)
            cleanBlockName = cleanBlockName.Replace("AutoCAD2024Final.Resources.", "");
            // Убираем расширение
            cleanBlockName = cleanBlockName.Replace(".dwg", "");

            // Открываем поток чтения ресурса из DLL
            using (Stream stream = assembly.GetManifestResourceStream(resourceId))
            {
                // Если поток пустой - значит имя ресурса написано с ошибкой
                if (stream == null)
                {
                    string[] resources = assembly.GetManifestResourceNames();
                    //Application.ShowAlertDialog("Не найден ресурс: " + resourceId + "\n\nДоступные ресурсы:\n" + string.Join("\n", resources));
                    // Алерт при отсутствии ресурса (в методе ImportBlockFromResource)
                    Application.ShowAlertDialog("Resource not found: " + resourceId + "\n\nAvailable resources:\n" + string.Join("\n", resources));
                    return ObjectId.Null;
                }

                // AutoCAD не умеет вставлять блоки прямо из оперативной памяти. Ему нужен файл.
                // Поэтому мы создаем временный файл на диске.
                string tempFile = Path.GetTempFileName();

                try
                {
                    // Копируем байты из DLL во временный файл
                    using (FileStream fileStream = File.OpenWrite(tempFile))
                    {
                        stream.CopyTo(fileStream);
                    }

                    ObjectId btrId = ObjectId.Null; // ID определения блока (шаблона)

                    // Начинаем транзакцию для работы с базой
                    using (Transaction tr = db.TransactionManager.StartTransaction())
                    {
                        // Открываем Таблицу Блоков (список всех блоков в чертеже)
                        BlockTable bt = tr.GetObject(db.BlockTableId, OpenMode.ForWrite) as BlockTable;

                        // Проверяем: А вдруг такой блок (RL-POS) уже есть в чертеже?
                        if (bt.Has(cleanBlockName))
                        {
                            // Если есть - используем его ID. Не надо импортировать заново.
                            btrId = bt[cleanBlockName];
                        }
                        else
                        {
                            // Если нет - импортируем из нашего временного файла
                            using (Database sourceDb = new Database(false, true))
                            {
                                // Читаем DWG файл в память
                                sourceDb.ReadDwgFile(tempFile, FileOpenMode.OpenForReadAndAllShare, true, "");
                                // Вставляем его в текущий чертеж под чистым именем
                                btrId = db.Insert(cleanBlockName, sourceDb, true);
                            }
                        }

                        // Теперь создаем "Вхождение блока" (тот самый объект, который виден на экране)
                        // BtrId - это ID "чертежа" блока (Definition).
                        // BlkRef - это "ссылка" на него в конкретной точке (Reference).

                        using (BlockReference blkRef = new BlockReference(position, btrId))
                        {
                            // Задаем масштаб
                            blkRef.ScaleFactors = new Scale3d(scale, scale, scale);

                            // Добавляем блок в текущее пространство (Модель или Лист)
                            BlockTableRecord curSpace = tr.GetObject(db.CurrentSpaceId, OpenMode.ForWrite) as BlockTableRecord;
                            curSpace.AppendEntity(blkRef);
                            tr.AddNewlyCreatedDBObject(blkRef, true); // Сообщаем транзакции о новом объекте

                            // --- САМАЯ ВАЖНАЯ ЧАСТЬ: АТРИБУТЫ ---
                            // При программной вставке атрибуты НЕ создаются сами. Их нужно клонировать.

                            // Открываем определение блока (шаблон)
                            BlockTableRecord btr = tr.GetObject(btrId, OpenMode.ForRead) as BlockTableRecord;

                            // Проверяем, есть ли в шаблоне атрибуты
                            if (btr.HasAttributeDefinitions)
                            {
                                // Перебираем все объекты внутри шаблона блока
                                foreach (ObjectId id in btr)
                                {
                                    DBObject obj = tr.GetObject(id, OpenMode.ForRead);

                                    // Если объект - это Определение Атрибута (AttributeDefinition)
                                    if (obj is AttributeDefinition attDef)
                                    {
                                        // Создаем Ссылку на Атрибут (AttributeReference) - это то, что хранит текст
                                        using (AttributeReference attRef = new AttributeReference())
                                        {
                                            // Копируем свойства из шаблона (позицию, стиль, тэг)
                                            attRef.SetAttributeFromBlock(attDef, blkRef.BlockTransform);

                                            // Добавляем атрибут к нашему блоку
                                            blkRef.AttributeCollection.AppendAttribute(attRef);
                                            tr.AddNewlyCreatedDBObject(attRef, true);
                                        }
                                    }
                                }
                            }

                            tr.Commit(); // Сохраняем изменения
                            return blkRef.ObjectId; // Возвращаем ID созданного блока
                        }
                    }
                }
                finally
                {
                    // Удаляем временный файл, чтобы не мусорить на диске пользователя
                    if (File.Exists(tempFile))
                    {
                        try { File.Delete(tempFile); } catch { }
                    }
                }
            }
        }

        // ---------------------------------------------------------------------------------------
        // МЕТОД СОЗДАНИЯ СЛОЯ
        // ---------------------------------------------------------------------------------------
        private void EnsureLayerExists(Database db, string layerName)
        {
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                // Открываем таблицу слоев
                LayerTable lt = tr.GetObject(db.LayerTableId, OpenMode.ForRead) as LayerTable;

                // Если слоя с таким именем нет
                if (!lt.Has(layerName))
                {
                    lt.UpgradeOpen(); // Переходим в режим записи

                    LayerTableRecord newLayer = new LayerTableRecord();
                    newLayer.Name = layerName;
                    newLayer.Color = Color.FromColorIndex(ColorMethod.ByAci, 7); // Цвет 7 (Белый/Черный)

                    lt.Add(newLayer); // Добавляем в таблицу
                    tr.AddNewlyCreatedDBObject(newLayer, true); // Подтверждаем создание
                }
                tr.Commit();
            }
        }
    }
}