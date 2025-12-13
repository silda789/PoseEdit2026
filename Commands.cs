// #nullable disable — отключаем строгую проверку на null (пустые значения).
// В .NET 8 это включено по умолчанию, и без этой строки будет много желтых предупреждений,
// которые новичку только мешают.
#nullable disable

using System;
using System.IO; // Работа с файловой системой (Path, File, FileStream)
using System.Reflection; // "Рефлексия" - позволяет программе смотреть внутрь самой себя (нужно для ресурсов)
using System.Collections.Generic;
using System.Linq;
using System.Globalization;

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
#region Константы и общие настройки
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
#endregion

#region Главная команда EEN (окно редактирования поз)
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
                // C# 12: Используем новый синтаксис коллекций (collection expressions)
                // Вместо new TypedValue[] { ... } теперь можно писать просто [ ... ]
                TypedValue[] filterList = [
                    new TypedValue((int)DxfCode.Start, "INSERT"),
                    new TypedValue((int)DxfCode.BlockName, "RL-POS,RL-POS2")
                ];
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
                                    // ПРОБЛЕМА МОЖЕТ БЫТЬ ТУТ: Если объект удален или некорректен
                                    DBObject obj = tr.GetObject(id, OpenMode.ForRead);

                                    if (obj is AttributeDefinition attDef) // Безопасная проверка типа
                                    {
                                        using (AttributeReference attRef = new AttributeReference())
                                        {
                                            attRef.SetAttributeFromBlock(attDef, blkRef.BlockTransform);
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

#endregion

#region Вспомогательные методы (слои, блоки, геометрия, парсинг TB)
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

        // ---------------------------------------------------------------------------------------
        // КОМАНДА: POZVER
        // Назначает номера позиций выбранным блокам RL-POS.
        // Нумеруем только те, у кого POZ отсутствует или равен 0.
        // Старт = (максимальный POZ среди всех RL-POS* в чертеже) + 1.
        // Сортировка: диаметр (из TB) по убыванию, затем BOY по убыванию.
        // ---------------------------------------------------------------------------------------
        [CommandMethod("POZVER")]
        public void AssignPositions()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Editor ed = doc.Editor;

            try
            {
                // Фильтр по блокам RL-POS*
                TypedValue[] filterValues = [
                    new TypedValue((int)DxfCode.Start, "INSERT"),
                    new TypedValue((int)DxfCode.BlockName, "RL-POS*")
                ];
                SelectionFilter filter = new SelectionFilter(filterValues);
                PromptSelectionOptions selOpts = new PromptSelectionOptions
                {
                    MessageForAdding = "\nSelect RL-POS blocks:"
                };

                PromptSelectionResult selRes = ed.GetSelection(selOpts, filter);
                if (selRes.Status != PromptStatus.OK || selRes.Value.Count == 0)
                {
                    // Если не выбрали - пробуем взять все
                    selRes = ed.SelectAll(filter);
                }
                if (selRes.Status != PromptStatus.OK || selRes.Value.Count == 0)
                {
                    ed.WriteMessage("\nNo RL-POS blocks selected.");
                    return;
                }

                var ids = selRes.Value.GetObjectIds();
                var selectedItems = new List<(ObjectId id, Dictionary<string, string> attrs)>();

                // Чтение атрибутов выделенных
                using (Transaction tr = doc.TransactionManager.StartTransaction())
                {
                    foreach (var id in ids)
                    {
                        selectedItems.Add((id, BlockHelper.GetAttributes(id)));
                    }
                    tr.Commit();
                }

                // Определяем максимальный POZ среди всех RL-POS* в чертеже
                int maxExistingPoz = 0;
                var allRes = ed.SelectAll(filter);
                if (allRes.Status == PromptStatus.OK)
                {
                    using (Transaction tr = doc.TransactionManager.StartTransaction())
                    {
                        foreach (var id in allRes.Value.GetObjectIds())
                        {
                            var attrs = BlockHelper.GetAttributes(id);
                            int p = TryParseInt(attrs, "POZ");
                            if (p > maxExistingPoz) maxExistingPoz = p;
                        }
                        tr.Commit();
                    }
                }

                // Парсинг диаметра и BOY
                int ParseDiameter(Dictionary<string, string> attrs)
                {
                    string tb = attrs.TryGetValue("TB", out var t) ? t : "";
                    if (string.IsNullOrWhiteSpace(tb)) return 0;
                    // ищем ØNN или %%CNN
                    int idx = tb.IndexOf("Ø", StringComparison.Ordinal);
                    if (idx < 0) idx = tb.IndexOf("%%C", StringComparison.OrdinalIgnoreCase);
                    if (idx >= 0)
                    {
                        string rest = tb.Substring(idx + (tb[idx] == 'Ø' ? 1 : 3));
                        var num = new string(rest.TakeWhile(char.IsDigit).ToArray());
                        if (int.TryParse(num, out int v)) return v;
                    }
                    return 0;
                }

                double ParseBoy(Dictionary<string, string> attrs)
                {
                    if (attrs.TryGetValue("BOY", out var b) && double.TryParse(b.Replace(",", "."), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double v))
                        return v;
                    return 0;
                }

                // Отбираем только пустые/нулевые POZ
                var candidates = selectedItems
                    .Where(i => TryParseInt(i.attrs, "POZ") <= 0)
                    .OrderByDescending(i => ParseDiameter(i.attrs))
                    .ThenByDescending(i => ParseBoy(i.attrs))
                    .ToList();

                if (!candidates.Any())
                {
                    ed.WriteMessage("\nPOZVER: no empty POZ to assign.");
                    return;
                }

                int currentPoz = maxExistingPoz + 1;
                foreach (var item in candidates)
                {
                    var nv = new Dictionary<string, string>
                    {
                        ["POZ"] = currentPoz.ToString()
                    };
                    BlockHelper.SetAttributes(item.id, nv);
                    currentPoz++;
                }

                ed.WriteMessage($"\nPOZVER: updated {candidates.Count} block(s). Last POZ = {currentPoz - 1}.");
            }
            catch (System.Exception ex)
            {
                ed.WriteMessage($"\nError in POZVER: {ex.Message}");
            }
        }

        private int TryParseInt(Dictionary<string, string> dict, string key)
        {
            if (dict.TryGetValue(key, out var s) && int.TryParse(s, out int v)) return v;
            return 0;
        }

        private double GetUnitsSafe()
        {
            return AppSettings.DrawingUnit > 0 ? AppSettings.DrawingUnit : 1000.0;
        }

        private double GetScaleSafe()
        {
            return AppSettings.SheetScale > 0 ? AppSettings.SheetScale : 50.0;
        }

        // Мини-помощник для строкового ввода с возможностью обязательного поля
        private string PromptString(Editor ed, string label, bool required = false)
        {
            while (true)
            {
                var pr = ed.GetString($"\n{label}: ");
                if (pr.Status != PromptStatus.OK) return "";
                var s = pr.StringResult ?? "";
                if (!required || !string.IsNullOrWhiteSpace(s))
                    return s.Trim();
                ed.WriteMessage($"\n{label} is required.");
            }
        }

        private static Point3d Offset(Point3d pt, double angleRad, double dist)
        {
            return new Point3d(
                pt.X + Math.Cos(angleRad) * dist,
                pt.Y + Math.Sin(angleRad) * dist,
                pt.Z);
        }

        private static void MoveAttribute(BlockReference br, Transaction tr, string tag, Point3d target)
        {
            foreach (ObjectId attId in br.AttributeCollection)
            {
                var att = (AttributeReference)tr.GetObject(attId, OpenMode.ForWrite);
                if (string.Equals(att.Tag, tag, StringComparison.OrdinalIgnoreCase))
                {
                    att.Position = target;
                    att.AlignmentPoint = target;
                    att.AdjustAlignment(br.Database);
                }
            }
        }

        // ===================================================================================
        // ПРОСТЫЕ КОМАНДЫ ИЗ QUANTITY2.LSP (базовые правки атрибутов)
        // ===================================================================================
        // Общие вспомогатели для работы с TB (adet x Øcap / aralik)
        private record TbParts(string AdetCarpi, string Adet, string Cap, string Aralik);

        private TbParts ParseTb(string tbRaw)
        {
            if (string.IsNullOrWhiteSpace(tbRaw))
                return new TbParts("", "", "", "");

            string tb = tbRaw.Trim();
            tb = tb.Replace("%%C", "Ø", StringComparison.OrdinalIgnoreCase)
                   .Replace("%%c", "Ø", StringComparison.OrdinalIgnoreCase);

            int fiIndex = tb.IndexOf('Ø');
            if (fiIndex < 0)
                return new TbParts("", "", "", "");

            string left = tb[..fiIndex];
            string adetCarpi = "";
            string adet = "";

            int xIndex = left.IndexOf('x');
            if (xIndex < 0) xIndex = left.IndexOf('X');
            if (xIndex >= 0)
            {
                adetCarpi = left[..xIndex].Trim();
                adet = left[(xIndex + 1)..].Trim();
            }
            else
            {
                adet = left.Trim();
            }

            string right = tb[(fiIndex + 1)..];
            string cap;
            string aralik = "";

            int slashIndex = right.IndexOf('/');
            if (slashIndex >= 0)
            {
                cap = right[..slashIndex].Trim();
                aralik = right[(slashIndex + 1)..].Trim();
            }
            else
            {
                cap = right.Trim();
            }

            return new TbParts(adetCarpi, adet, cap, aralik);
        }

        private string BuildTb(TbParts p)
        {
            string prefix = string.IsNullOrWhiteSpace(p.AdetCarpi) ? "" : $"{p.AdetCarpi}x";
            string aralik = string.IsNullOrWhiteSpace(p.Aralik) ? "" : $"/{p.Aralik}";
            string adet = string.IsNullOrWhiteSpace(p.Adet) ? "0" : p.Adet;
            string cap = string.IsNullOrWhiteSpace(p.Cap) ? "0" : p.Cap;
            return $"{prefix}{adet}Ø{cap}{aralik}";
        }

        private void UpdateBlocksTb(Func<TbParts, TbParts> updater, Action<Dictionary<string, string>> extraSetter = null)
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            if (doc == null) return;
            Editor ed = doc.Editor;

            TypedValue[] filterValues = [
                new TypedValue((int)DxfCode.Start, "INSERT"),
                new TypedValue((int)DxfCode.BlockName, "RL-POS*")
            ];
            SelectionFilter filter = new SelectionFilter(filterValues);
            PromptSelectionOptions selOpts = new PromptSelectionOptions
            {
                MessageForAdding = "\nSelect RL-POS blocks:"
            };

            PromptSelectionResult selRes = ed.GetSelection(selOpts, filter);
            if (selRes.Status != PromptStatus.OK || selRes.Value.Count == 0)
            {
                ed.WriteMessage("\nNo RL-POS blocks selected.");
                return;
            }

            int updated = 0;
            foreach (ObjectId id in selRes.Value.GetObjectIds())
            {
                var attrs = BlockHelper.GetAttributes(id);
                if (!attrs.TryGetValue("TB", out var tb))
                    continue;

                var parts = ParseTb(tb);
                var newParts = updater(parts);
                attrs["TB"] = BuildTb(newParts);

                extraSetter?.Invoke(attrs);
                BlockHelper.SetAttributes(id, attrs);
                updated++;
            }

            ed.WriteMessage($"\nUpdated {updated} block(s).");
        }

        // ------------------------------------------------------------------------------
        // POZCLAYER — переводит выбранные RL-POS* на слой ren.mtr.tb
        // ------------------------------------------------------------------------------
        [CommandMethod("POZCLAYER")]
        public void PozToLayer()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            if (doc == null) return;
            Editor ed = doc.Editor;
            Database db = doc.Database;

            EnsureLayerExists(db, "ren.mtr.tb");

            TypedValue[] filterValues = [
                new TypedValue((int)DxfCode.Start, "INSERT"),
                new TypedValue((int)DxfCode.BlockName, "RL-POS*")
            ];
            SelectionFilter filter = new SelectionFilter(filterValues);
            PromptSelectionOptions selOpts = new PromptSelectionOptions
            {
                MessageForAdding = "\nSelect RL-POS blocks to move to layer ren.mtr.tb:"
            };
            PromptSelectionResult selRes = ed.GetSelection(selOpts, filter);
            if (selRes.Status != PromptStatus.OK) return;

            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                foreach (ObjectId id in selRes.Value.GetObjectIds())
                {
                    Entity ent = tr.GetObject(id, OpenMode.ForWrite) as Entity;
                    if (ent != null) ent.Layer = "ren.mtr.tb";
                }
                tr.Commit();
            }

            ed.WriteMessage($"\nMoved {selRes.Value.Count} block(s) to layer ren.mtr.tb.");
        }

        // ------------------------------------------------------------------------------
        // ADET — меняет количество (adet) в TB, сохраняя множитель, Ø и шаг
        // ------------------------------------------------------------------------------
        [CommandMethod("ADET")]
        public void CmdAdet()
        {
            PromptIntegerOptions opt = new PromptIntegerOptions("\nNew quantity (adet): ")
            {
                AllowNegative = false,
                AllowZero = false
            };
            PromptIntegerResult res = Application.DocumentManager.MdiActiveDocument.Editor.GetInteger(opt);
            if (res.Status != PromptStatus.OK) return;
            string newAdet = res.Value.ToString(CultureInfo.InvariantCulture);

            UpdateBlocksTb(parts => parts with { Adet = newAdet });
        }

        // ------------------------------------------------------------------------------
        // ADET2 — меняет множитель (adet carpi) в TB, сохраняя остальное
        // ------------------------------------------------------------------------------
        [CommandMethod("ADET2")]
        public void CmdAdet2()
        {
            PromptIntegerOptions opt = new PromptIntegerOptions("\nNew quantity multiplier: ")
            {
                AllowNegative = false,
                AllowZero = false
            };
            PromptIntegerResult res = Application.DocumentManager.MdiActiveDocument.Editor.GetInteger(opt);
            if (res.Status != PromptStatus.OK) return;
            string newCarpi = res.Value <= 1 ? "" : res.Value.ToString(CultureInfo.InvariantCulture);

            UpdateBlocksTb(parts => parts with { AdetCarpi = newCarpi });
        }

        // ------------------------------------------------------------------------------
        // CAP — меняет диаметр в TB, ставит TIK=1 (как в LISP), остальное сохраняет
        // ------------------------------------------------------------------------------
        [CommandMethod("CAP")]
        public void CmdCap()
        {
            PromptIntegerOptions opt = new PromptIntegerOptions("\nNew diameter (cap): ")
            {
                AllowNegative = false,
                AllowZero = false
            };
            PromptIntegerResult res = Application.DocumentManager.MdiActiveDocument.Editor.GetInteger(opt);
            if (res.Status != PromptStatus.OK) return;
            string newCap = res.Value.ToString(CultureInfo.InvariantCulture);

            UpdateBlocksTb(
                parts => parts with { Cap = newCap },
                attrs =>
                {
                    if (attrs.ContainsKey("TIK")) attrs["TIK"] = "1";
                });
        }

        // ------------------------------------------------------------------------------
        // ARALIK — меняет шаг (spacing) в TB и атрибут ARALIK
        // ------------------------------------------------------------------------------
        [CommandMethod("ARALIK")]
        public void CmdAralik()
        {
            PromptIntegerOptions opt = new PromptIntegerOptions("\nNew spacing (aralik): ")
            {
                AllowNegative = false,
                AllowZero = false
            };
            PromptIntegerResult res = Application.DocumentManager.MdiActiveDocument.Editor.GetInteger(opt);
            if (res.Status != PromptStatus.OK) return;
            string newAralik = res.Value.ToString(CultureInfo.InvariantCulture);

            UpdateBlocksTb(
                parts => parts with { Aralik = newAralik },
                attrs =>
                {
                    attrs["ARALIK"] = newAralik;
                });
        }

        // ------------------------------------------------------------------------------
        // GRUP — меняет GC (групповой множитель) у выбранных
        // ------------------------------------------------------------------------------
        [CommandMethod("GRUP")]
        public void CmdGrup()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            if (doc == null) return;
            Editor ed = doc.Editor;

            PromptIntegerOptions opt = new PromptIntegerOptions("\nGrup carpani (GC): ")
            {
                AllowNegative = false,
                AllowZero = false
            };
            PromptIntegerResult res = ed.GetInteger(opt);
            if (res.Status != PromptStatus.OK) return;
            string gcValue = res.Value.ToString(CultureInfo.InvariantCulture);

            TypedValue[] filterValues = [
                new TypedValue((int)DxfCode.Start, "INSERT"),
                new TypedValue((int)DxfCode.BlockName, "RL-POS*")
            ];
            SelectionFilter filter = new SelectionFilter(filterValues);
            PromptSelectionOptions selOpts = new PromptSelectionOptions
            {
                MessageForAdding = "\nSelect RL-POS blocks:"
            };
            PromptSelectionResult selRes = ed.GetSelection(selOpts, filter);
            if (selRes.Status != PromptStatus.OK || selRes.Value.Count == 0)
            {
                ed.WriteMessage("\nNo RL-POS blocks selected.");
                return;
            }

            int updated = 0;
            foreach (ObjectId id in selRes.Value.GetObjectIds())
            {
                var attrs = BlockHelper.GetAttributes(id);
                attrs["GC"] = gcValue;
                BlockHelper.SetAttributes(id, attrs);
                updated++;
            }

            ed.WriteMessage($"\nUpdated GC for {updated} block(s).");
        }

        // Вспомогатель для массового прохода по выбранным RL-POS*
        private void ForEachPoz(Action<ObjectId, Dictionary<string, string>> action, string prompt = "\nSelect RL-POS blocks:")
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            if (doc == null) return;
            Editor ed = doc.Editor;

            TypedValue[] filterValues = [
                new TypedValue((int)DxfCode.Start, "INSERT"),
                new TypedValue((int)DxfCode.BlockName, "RL-POS*")
            ];
            SelectionFilter filter = new SelectionFilter(filterValues);
            PromptSelectionOptions selOpts = new PromptSelectionOptions
            {
                MessageForAdding = prompt
            };
            var selRes = ed.GetSelection(selOpts, filter);
            if (selRes.Status != PromptStatus.OK || selRes.Value.Count == 0)
            {
                ed.WriteMessage("\nNo RL-POS blocks selected.");
                return;
            }

            foreach (var id in selRes.Value.GetObjectIds())
            {
                var attrs = BlockHelper.GetAttributes(id);
                action(id, attrs);
            }
        }

        // ------------------------------------------------------------------------------
        // DEGIS — замена значения атрибутов по имени (упрощенно)
        // ------------------------------------------------------------------------------
        [CommandMethod("DEGIS")]
        public void CmdDegis()
        {
            Editor ed = Application.DocumentManager.MdiActiveDocument.Editor;

            var attrRes = ed.GetString("\nAttribute to change (POZ,ADET,ADET2,CAP,ARALIK,BOY,TIP,A,B,C,D,E,F,R,GC,NOT,TB): ");
            if (attrRes.Status != PromptStatus.OK) return;
            string attr = attrRes.StringResult.Trim().ToUpperInvariant();
            if (string.IsNullOrEmpty(attr)) return;

            var oldRes = ed.GetString("\nOld value: ");
            if (oldRes.Status != PromptStatus.OK) return;
            string oldVal = oldRes.StringResult ?? "";

            var newRes = ed.GetString("\nNew value: ");
            if (newRes.Status != PromptStatus.OK) return;
            string newVal = newRes.StringResult ?? "";

            int changed = 0;
            ForEachPoz((id, attrs) =>
            {
                bool touched = false;
                if (attr is "ADET" or "ADET2" or "CAP" or "ARALIK")
                {
                    if (!attrs.TryGetValue("TB", out var tb)) tb = "";
                    var parts = ParseTb(tb);
                    if (attr == "ADET" && parts.Adet == oldVal) { parts = parts with { Adet = newVal }; touched = true; }
                    if (attr == "ADET2" && parts.AdetCarpi == oldVal) { parts = parts with { AdetCarpi = newVal }; touched = true; }
                    if (attr == "CAP" && parts.Cap == oldVal) { parts = parts with { Cap = newVal }; touched = true; attrs["TIK"] = "1"; }
                    if (attr == "ARALIK" && parts.Aralik == oldVal) { parts = parts with { Aralik = newVal }; touched = true; attrs["ARALIK"] = newVal; }
                    if (touched) attrs["TB"] = BuildTb(parts);
                }
                else
                {
                    if (attrs.TryGetValue(attr, out var cur) && cur == oldVal)
                    {
                        attrs[attr] = newVal;
                        touched = true;
                    }
                }

                if (touched)
                {
                    BlockHelper.SetAttributes(id, attrs);
                    changed++;
                }
            });

            ed.WriteMessage($"\nDEGIS: updated {changed} block(s).");
        }

        // ------------------------------------------------------------------------------
        // TDDK — копирует атрибуты с образца на выбранные INSERT
        // ------------------------------------------------------------------------------
        [CommandMethod("TDDK")]
        public void CmdTddk()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            if (doc == null) return;
            Editor ed = doc.Editor;

            var srcSel = ed.GetSelection(new PromptSelectionOptions
            {
                MessageForAdding = "\nSelect source block with attributes:"
            });
            if (srcSel.Status != PromptStatus.OK || srcSel.Value.Count == 0) return;
            ObjectId srcId = srcSel.Value[0].ObjectId;

            var srcAttrs = BlockHelper.GetAttributes(srcId);
            if (srcAttrs.Count == 0)
            {
                ed.WriteMessage("\nNo attributes on source.");
                return;
            }

            var dstSel = ed.GetSelection(new PromptSelectionOptions
            {
                MessageForAdding = "\nSelect target blocks:"
            });
            if (dstSel.Status != PromptStatus.OK || dstSel.Value.Count == 0) return;

            int updated = 0;
            foreach (var id in dstSel.Value.GetObjectIds())
            {
                // копируем только совпадающие теги
                var dstAttrs = BlockHelper.GetAttributes(id);
                if (dstAttrs.Count == 0) continue;
                foreach (var kv in srcAttrs)
                {
                    if (dstAttrs.ContainsKey(kv.Key))
                        dstAttrs[kv.Key] = kv.Value;
                }
                BlockHelper.SetAttributes(id, dstAttrs);
                updated++;
            }

            ed.WriteMessage($"\nTDDK: copied attributes to {updated} block(s).");
        }

        // ------------------------------------------------------------------------------
        // TDDB — автозаполнение BOY по сумме сегментов (упрощенно)
        // ------------------------------------------------------------------------------
        [CommandMethod("TDDB")]
        public void CmdTddb()
        {
            Editor ed = Application.DocumentManager.MdiActiveDocument.Editor;
            int updated = 0;
            ForEachPoz((id, attrs) =>
            {
                if (attrs.TryGetValue("TIP", out var tip) && tip == "99") return;

                double Sum(params string[] vals)
                {
                    double s = 0;
                    foreach (var v in vals)
                    {
                        if (double.TryParse(v?.Replace(",", "."), NumberStyles.Any, CultureInfo.InvariantCulture, out var d))
                            s += d;
                    }
                    return s;
                }

                double total = Sum(
                    attrs.GetValueOrDefault("A"),
                    attrs.GetValueOrDefault("B"),
                    attrs.GetValueOrDefault("C"),
                    attrs.GetValueOrDefault("D"),
                    attrs.GetValueOrDefault("E"),
                    attrs.GetValueOrDefault("F"));

                // Простейшее приближение без учета радиусов
                string boyStr = $"L={total:0}";
                attrs["BOY"] = boyStr;
                BlockHelper.SetAttributes(id, attrs);
                updated++;
            }, "\nSelect RL-POS blocks for TDDB:");

            ed.WriteMessage($"\nTDDB: updated {updated} block(s).");
        }

        // ------------------------------------------------------------------------------
        // TDDH — выводит содержимое error.txt в модель в виде текста
        // ------------------------------------------------------------------------------
        [CommandMethod("TDDH")]
        public void CmdTddh()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            if (doc == null) return;
            Editor ed = doc.Editor;
            Database db = doc.Database;

            var ptRes = ed.GetPoint("\nPick point to place error log: ");
            if (ptRes.Status != PromptStatus.OK) return;
            Point3d pos = ptRes.Value;

            // Пытаемся найти error.txt: сначала рядом с DLL, потом в Documents/PoseEdit2026, потом в текущей
            string assemblyPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string[] candidates = new[]
            {
                Path.Combine(assemblyPath ?? "", "error.txt"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "PoseEdit2026", "error.txt"),
                "error.txt"
            };
            string file = candidates.FirstOrDefault(File.Exists);
            if (file == null)
            {
                ed.WriteMessage("\nerror.txt not found.");
                return;
            }

            string[] lines = File.ReadAllLines(file);
            if (lines.Length == 0)
            {
                ed.WriteMessage("\nerror.txt is empty.");
                return;
            }

            double textHeight = 0.002 * GetScaleSafe() * GetUnitsSafe();
            double dy = 1.5 * textHeight;

            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                BlockTableRecord space = (BlockTableRecord)tr.GetObject(db.CurrentSpaceId, OpenMode.ForWrite);
                Point3d cur = pos;
                foreach (var line in lines)
                {
                    DBText txt = new DBText
                    {
                        Position = cur,
                        Height = textHeight,
                        TextString = line
                    };
                    space.AppendEntity(txt);
                    tr.AddNewlyCreatedDBObject(txt, true);
                    cur = new Point3d(cur.X, cur.Y - dy, cur.Z);
                }
                tr.Commit();
            }

            ed.WriteMessage($"\nTDDH: placed {lines.Length} line(s) from error.txt");
        }

        // ------------------------------------------------------------------------------
        // TDD1 — перестановка TB/BOY/NOT по схеме из LISP tdd1
        // ------------------------------------------------------------------------------
        [CommandMethod("TDD1")]
        public void CmdTdd1()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            if (doc == null) return;
            Editor ed = doc.Editor;

            TypedValue[] filterValues = [
                new TypedValue((int)DxfCode.Start, "INSERT"),
                new TypedValue((int)DxfCode.BlockName, "RL-POS,RL-POS2")
            ];
            SelectionFilter filter = new SelectionFilter(filterValues);
            var selRes = ed.GetSelection(new PromptSelectionOptions { MessageForAdding = "\nSelect RL-POS blocks for TDD1:" }, filter);
            if (selRes.Status != PromptStatus.OK || selRes.Value.Count == 0)
            {
                ed.WriteMessage("\nNo blocks selected.");
                return;
            }

            using (Transaction tr = doc.TransactionManager.StartTransaction())
            {
                foreach (var id in selRes.Value.GetObjectIds())
                {
                    var br = (BlockReference)tr.GetObject(id, OpenMode.ForWrite);
                    double ang = br.Rotation;
                    double scale = br.ScaleFactors.X;
                    double absScale = Math.Abs(scale);
                    Point3d ins = br.Position;

                    Point3d p11 = Offset(ins, ang, 45 * absScale);
                    p11 = Offset(p11, ang + Math.PI * 0.5, 10 * absScale);
                    Point3d p21 = Offset(p11, ang + Math.PI * 1.5, 45 * scale);
                    Point3d p31 = Offset(p21, ang + Math.PI * 1.5, 45 * scale);

                    MoveAttribute(br, tr, "TB", p11);
                    MoveAttribute(br, tr, "BOY", p21);
                    MoveAttribute(br, tr, "NOT", p31);
                }
                tr.Commit();
            }
        }

        // ------------------------------------------------------------------------------
        // TDD2 — перестановка TB/BOY/NOT по схеме из LISP tdd2
        // ------------------------------------------------------------------------------
        [CommandMethod("TDD2")]
        public void CmdTdd2()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            if (doc == null) return;
            Editor ed = doc.Editor;

            TypedValue[] filterValues = [
                new TypedValue((int)DxfCode.Start, "INSERT"),
                new TypedValue((int)DxfCode.BlockName, "RL-POS,RL-POS2")
            ];
            SelectionFilter filter = new SelectionFilter(filterValues);
            var selRes = ed.GetSelection(new PromptSelectionOptions { MessageForAdding = "\nSelect RL-POS blocks for TDD2:" }, filter);
            if (selRes.Status != PromptStatus.OK || selRes.Value.Count == 0)
            {
                ed.WriteMessage("\nNo blocks selected.");
                return;
            }

            using (Transaction tr = doc.TransactionManager.StartTransaction())
            {
                foreach (var id in selRes.Value.GetObjectIds())
                {
                    var br = (BlockReference)tr.GetObject(id, OpenMode.ForWrite);
                    double ang = br.Rotation;
                    double scale = br.ScaleFactors.X;
                    Point3d ins = br.Position;

                    Point3d p11 = Offset(ins, ang + Math.PI * 1.5, 45 * scale);
                    p11 = Offset(p11, ang + Math.PI, 30 * scale);
                    Point3d p21 = Offset(p11, ang + Math.PI * 1.5, 45 * scale);
                    Point3d p31 = Offset(p21, ang + Math.PI * 1.5, 45 * scale);

                    MoveAttribute(br, tr, "TB", p11);
                    MoveAttribute(br, tr, "BOY", p21);
                    MoveAttribute(br, tr, "NOT", p31);
                }
                tr.Commit();
            }
        }

        // ------------------------------------------------------------------------------
        // TDD3 (бывш. tdd5) — перестановка TB/BOY/NOT с учетом длины текста
        // ------------------------------------------------------------------------------
        [CommandMethod("TDD3")]
        public void CmdTdd3()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            if (doc == null) return;
            Editor ed = doc.Editor;

            TypedValue[] filterValues = [
                new TypedValue((int)DxfCode.Start, "INSERT"),
                new TypedValue((int)DxfCode.BlockName, "RL-POS,RL-POS2")
            ];
            SelectionFilter filter = new SelectionFilter(filterValues);
            var selRes = ed.GetSelection(new PromptSelectionOptions { MessageForAdding = "\nSelect RL-POS blocks for TDD3:" }, filter);
            if (selRes.Status != PromptStatus.OK || selRes.Value.Count == 0)
            {
                ed.WriteMessage("\nNo blocks selected.");
                return;
            }

            using (Transaction tr = doc.TransactionManager.StartTransaction())
            {
                foreach (var id in selRes.Value.GetObjectIds())
                {
                    var br = (BlockReference)tr.GetObject(id, OpenMode.ForWrite);
                    double ang = br.Rotation;
                    double scale = br.ScaleFactors.X;
                    double absScale = Math.Abs(scale);
                    Point3d ins = br.Position;

                    // Получаем высоты/ScaleFactor/длину текста для TB и BOY
                    double tbHeight = 1.0, tbScale = 1.0, tbLen = 0.0;
                    double boyHeight = 1.0, boyScale = 1.0, boyLen = 0.0;

                    foreach (ObjectId attId in br.AttributeCollection)
                    {
                        var att = (AttributeReference)tr.GetObject(attId, OpenMode.ForRead);
                        string tag = att.Tag.ToUpperInvariant();
                        if (tag == "TB")
                        {
                            tbHeight = att.Height;
                            tbScale = 1.0; // AttributeReference не имеет ScaleFactor, оставляем 1.0
                            tbLen = (att.TextString ?? "").Length;
                        }
                        if (tag == "BOY")
                        {
                            boyHeight = att.Height;
                            boyScale = 1.0; // AttributeReference не имеет ScaleFactor, оставляем 1.0
                            boyLen = (att.TextString ?? "").Length;
                        }
                    }

                    double tbGen = 0.05 * tbHeight + 0.65 * tbHeight * tbScale * tbLen;
                    double boyGen = 0.55 * boyHeight + 0.75 * boyHeight * boyScale * boyLen;

                    Point3d p11 = Offset(ins, ang, 45 * absScale);
                    p11 = Offset(p11, ang + Math.PI * 0.5, 10 * absScale);
                    Point3d p21 = Offset(p11, ang, tbGen);
                    Point3d p31 = Offset(p21, ang, boyGen);

                    MoveAttribute(br, tr, "TB", p11);
                    MoveAttribute(br, tr, "BOY", p21);
                    MoveAttribute(br, tr, "NOT", p31);
                }
                tr.Commit();
            }
        }

        // ------------------------------------------------------------------------------
        // PZG — синхронизация атрибутов RL-POS (аналог ATTSYNC)
        // ------------------------------------------------------------------------------
        [CommandMethod("PZG")]
        public void CmdPzg()
        {
            Editor ed = Application.DocumentManager.MdiActiveDocument.Editor;
            try
            {
                ed.Command("-ATTSYNC", "_N", "RL-POS", "");
                ed.Command("-ATTSYNC", "_N", "RL-POS2", "");
                ed.WriteMessage("\nPZG: ATTSYNC done for RL-POS and RL-POS2.");
            }
            catch (System.Exception ex)
            {
                ed.WriteMessage("\nError in PZG: " + ex.Message);
            }
        }

        // ------------------------------------------------------------------------------
        // 77 — ставит связанный выносной блок (RL-POS2.dwg) с полями, читающими POZ/TB/NOT
        // ------------------------------------------------------------------------------
        [CommandMethod("77")]
        public void Cmd77()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            if (doc == null) return;
            Editor ed = doc.Editor;
            Database db = doc.Database;

            try
            {
                // Выбор одного RL-POS блока
                TypedValue[] filterValues = [
                    new TypedValue((int)DxfCode.Start, "INSERT"),
                    new TypedValue((int)DxfCode.BlockName, "RL-POS")
                ];
                SelectionFilter filter = new SelectionFilter(filterValues);
                PromptSelectionOptions selOpts = new PromptSelectionOptions
                {
                    MessageForAdding = "\nSelect RL-POS block to create linked callout:"
                };
                selOpts.SingleOnly = true;

                var selRes = ed.GetSelection(selOpts, filter);
                if (selRes.Status != PromptStatus.OK) return;
                ObjectId srcId = selRes.Value[0].ObjectId;

                // Читаем атрибуты
                var attrs = BlockHelper.GetAttributes(srcId);
                if (!attrs.TryGetValue("POZ", out var pozTag)) pozTag = "";
                if (!attrs.TryGetValue("TB", out var tbTag)) tbTag = "";
                if (!attrs.TryGetValue("NOT", out var notTag)) notTag = "";

                // Определяем масштаб: ins_scale = (OLCEK_OKU / BIRIM_OKU) * 100
                double insScale = (GetScaleSafe() / GetUnitsSafe()) * 100.0;

                // Поворот берем у исходного блока
                double rotationDeg = 0;
                Point3d basePt;
                using (Transaction tr = db.TransactionManager.StartTransaction())
                {
                    var br = (BlockReference)tr.GetObject(srcId, OpenMode.ForRead);
                    rotationDeg = br.Rotation * 180.0 / Math.PI;
                    basePt = br.Position;
                    tr.Commit();
                }

                // Точка вставки нового выносного блока
                var ptRes = ed.GetPoint("\nPlacement point for linked callout: ");
                if (ptRes.Status != PromptStatus.OK) return;

                // Вставляем RL-POS2.dwg из ресурсов
                ObjectId calloutId = ImportBlockFromResource(db, ResourceName2, ptRes.Value, insScale);
                if (calloutId == ObjectId.Null)
                {
                    ed.WriteMessage("\nFailed to insert RL-POS2.");
                    return;
                }

                // Назначаем поля (в C# ставим прямые тексты, без FIELD)
                var nv = new Dictionary<string, string>
                {
                    ["POZ"] = pozTag,
                    ["TB"] = tbTag,
                    ["NOT"] = notTag
                };
                BlockHelper.SetAttributes(calloutId, nv);

                // Повернем как исходный
                ed.Command("_.ROTATE", calloutId, "", ptRes.Value, rotationDeg);
                ed.WriteMessage("\nCallout created.");
            }
            catch (System.Exception ex)
            {
                ed.WriteMessage("\nError in 77: " + ex.Message);
            }
        }

        // ------------------------------------------------------------------------------
        // 77B — находит поля ACAD_FIELD в выбранных полях и зумит к источникам (MTEXT/TEXT/ATTRIB)
        // ------------------------------------------------------------------------------
        [CommandMethod("77B")]
        public void Cmd77b()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            if (doc == null) return;
            Editor ed = doc.Editor;

            try
            {
                while (true)
                {
                    var nsel = ed.GetNestedEntity("\nSelect field (or Enter to stop): ");
                    if (nsel.Status != PromptStatus.OK) break;
                    ObjectId entId = nsel.ObjectId;

                    using (Transaction tr = doc.TransactionManager.StartTransaction())
                    {
                        var ent = tr.GetObject(entId, OpenMode.ForRead);
                        if (ent is DBText or MText or AttributeReference)
                        {
                            // Попытка найти ссылочный объект через FIELD
                            List<ObjectId> targets = FindFieldSources(entId, tr);
                            if (targets.Count == 0)
                            {
                                ed.WriteMessage("\nNo valid field sources found.");
                            }
                            else
                            {
                                foreach (var tid in targets)
                                {
                                    ed.Command("ZOOM", "O", tid, "");
                                }
                            }
                        }
                        else
                        {
                            ed.WriteMessage("\nNot a text/attribute.");
                        }
                        tr.Commit();
                    }
                }
            }
            catch (System.Exception ex)
            {
                ed.WriteMessage("\nError in 77B: " + ex.Message);
            }
        }

        // Рекурсивный поиск ObjectId источников поля (по аналогии с GFO/_GFO в LISP)
        private List<ObjectId> FindFieldSources(ObjectId id, Transaction tr)
        {
            List<ObjectId> result = new List<ObjectId>();
            var edata = entget_with_xdata(id, tr);
            TraverseXdata(edata, result);
            return result;
        }

        private static void TraverseXdata(List<TypedValue> data, List<ObjectId> acc)
        {
            foreach (var tv in data)
            {
                if (tv.TypeCode == 360 && tv.Value is ObjectId oid)
                {
                    acc.Add(oid);
                    // рекурсивный спуск
                    var sub = entget_with_xdata(oid, null);
                    TraverseXdata(sub, acc);
                }
                if (tv.TypeCode == 331 && tv.Value is ObjectId oid331)
                {
                    acc.Add(oid331);
                }
            }
        }

        private static List<TypedValue> entget_with_xdata(ObjectId id, Transaction trOrNull)
        {
            // Если транзакция не дана — откроем краткую транзакцию только для чтения
            if (trOrNull == null)
            {
                var db = HostApplicationServices.WorkingDatabase;
                using (var tr = db.TransactionManager.StartTransaction())
                {
                    var list = entget_with_xdata(id, tr);
                    tr.Commit();
                    return list;
                }
            }

            var ent = (Entity)trOrNull.GetObject(id, OpenMode.ForRead, false);
            return ent?.GetXDataForApplication(null)?.Cast<TypedValue>().ToList() ?? new List<TypedValue>();
        }

        #endregion

        #region Команды: PZ, DIEZ, TDDU, PPP/PPP2, выносные блоки, очистка
        // ------------------------------------------------------------------------------
        // PZREDEF — переопределение блоков PZ_* по списку tip_liste (из REN_QT_tip_file.txt)
        // ------------------------------------------------------------------------------
        [CommandMethod("PZREDEF")]
        public void CmdPzRedef()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            if (doc == null) return;
            Editor ed = doc.Editor;

            try
            {
                // Путь к source_path: берем папку Temp как dev-стандарт
                string sourcePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "GitHub", "PoseEdit2026", "Temp") + Path.DirectorySeparatorChar;
                // Попытка прочитать список типов из Standard/REN_QT_tip_file.txt
                string tipFile = Path.Combine(sourcePath, "Standard", "REN_QT_tip_file.txt");
                if (!File.Exists(tipFile))
                {
                    ed.WriteMessage("\nREN_QT_tip_file.txt not found.");
                    return;
                }
                var tipListe = File.ReadAllLines(tipFile)
                    .Select(l => l.Trim())
                    .Where(l => !string.IsNullOrWhiteSpace(l))
                    .ToList();

                if (tipListe.Count == 0)
                {
                    ed.WriteMessage("\nREN_QT_tip_file.txt is empty.");
                    return;
                }

                // Убедимся, что слои для метража есть
                EnsureLayerExists(doc.Database, "ren.mtr.bar");

                // Для каждого типа — подставляем DWG PZ_XX.dwg
                foreach (var tip in tipListe)
                {
                    string dwgPath = Path.Combine(sourcePath, "Standard", $"PZ_{tip}.dwg");
                    if (!File.Exists(dwgPath))
                    {
                        ed.WriteMessage($"\nFile not found: {dwgPath}");
                        continue;
                    }
                    // Вставляем временно и удаляем (обновляет определение)
                    ObjectId tmpId = ImportBlockFromFile(doc.Database, dwgPath, Point3d.Origin, 1.0);
                    if (tmpId != ObjectId.Null)
                    {
                        using (Transaction tr = doc.TransactionManager.StartTransaction())
                        {
                            var ent = (Entity)tr.GetObject(tmpId, OpenMode.ForWrite);
                            ent.Erase();
                            tr.Commit();
                        }
                        // ATTSYNC для имени блока
                        ed.Command("-ATTSYNC", "_N", Path.GetFileNameWithoutExtension(dwgPath), "");
                    }
                }

                ed.WriteMessage("\nPZREDEF: completed.");
            }
            catch (System.Exception ex)
            {
                ed.WriteMessage("\nError in PZREDEF: " + ex.Message);
            }
        }

        // Импорт блока из внешнего файла DWG (на модельный/текущий space)
        private ObjectId ImportBlockFromFile(Database db, string filePath, Point3d position, double scale)
        {
            ObjectId blockId = ObjectId.Null;
            using (Database srcDb = new Database(false, true))
            {
                srcDb.ReadDwgFile(filePath, FileShare.Read, true, "");
                ObjectIdCollection ids = new ObjectIdCollection();
                using (Transaction tr = srcDb.TransactionManager.StartTransaction())
                {
                    BlockTable bt = (BlockTable)tr.GetObject(srcDb.BlockTableId, OpenMode.ForRead);
                    foreach (ObjectId btrId in bt)
                    {
                        BlockTableRecord btr = (BlockTableRecord)tr.GetObject(btrId, OpenMode.ForRead);
                        if (!btr.IsLayout)
                        {
                            ids.Add(btrId);
                        }
                    }
                    tr.Commit();
                }

                if (ids.Count > 0)
                {
                    IdMapping mapping = new IdMapping();
                    db.WblockCloneObjects(ids, db.BlockTableId, mapping, DuplicateRecordCloning.Replace, false);
                }
            }

            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                BlockTable bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);
                string name = Path.GetFileNameWithoutExtension(filePath);
                if (bt.Has(name))
                {
                    BlockTableRecord space = (BlockTableRecord)tr.GetObject(db.CurrentSpaceId, OpenMode.ForWrite);
                    BlockTableRecord btr = (BlockTableRecord)tr.GetObject(bt[name], OpenMode.ForRead);
                    BlockReference br = new BlockReference(position, btr.ObjectId)
                    {
                        ScaleFactors = new Scale3d(scale)
                    };
                    space.AppendEntity(br);
                    tr.AddNewlyCreatedDBObject(br, true);
                    blockId = br.ObjectId;
                }
                tr.Commit();
            }

            return blockId;
        }

        // ------------------------------------------------------------------------------
        // DIEZ — отмечает все RL-POS/RL-POS2, где атрибуты содержат '#', стрелками к заданной точке
        // ------------------------------------------------------------------------------
        [CommandMethod("DIEZ")]
        public void CmdDiez()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            if (doc == null) return;
            Editor ed = doc.Editor;
            Database db = doc.Database;

            try
            {
                // Все RL-POS/RL-POS2
                TypedValue[] filterValues = [
                    new TypedValue((int)DxfCode.Start, "INSERT"),
                    new TypedValue((int)DxfCode.BlockName, "RL-POS,RL-POS2")
                ];
                SelectionFilter filter = new SelectionFilter(filterValues);
                var allRes = ed.SelectAll(filter);
                if (allRes.Status != PromptStatus.OK || allRes.Value.Count == 0)
                {
                    ed.WriteMessage("\nNo RL-POS blocks.");
                    return;
                }

                double hk = 1000.0 * Convert.ToDouble(Application.GetSystemVariable("DIMSCALE"));

                // Собираем «помеченные» (строка атрибутов содержит '#')
                var marked = new List<ObjectId>();
                using (Transaction tr = db.TransactionManager.StartTransaction())
                {
                    foreach (var id in allRes.Value.GetObjectIds())
                    {
                        var attrs = BlockHelper.GetAttributes(id);
                        string concat = string.Concat(
                            attrs.GetValueOrDefault("POZ"),
                            attrs.GetValueOrDefault("TB"),
                            attrs.GetValueOrDefault("A"),
                            attrs.GetValueOrDefault("B"),
                            attrs.GetValueOrDefault("C"),
                            attrs.GetValueOrDefault("D"),
                            attrs.GetValueOrDefault("E"),
                            attrs.GetValueOrDefault("F"),
                            attrs.GetValueOrDefault("R"));
                        if (concat.Contains("#"))
                        {
                            marked.Add(id);
                        }
                    }
                    tr.Commit();
                }

                if (marked.Count == 0)
                {
                    ed.WriteMessage("\nNo DIEZ marked positions.");
                    return;
                }

                // Точка, куда вести стрелки
                var ptRes = ed.GetPoint($"\n{marked.Count} marked item(s) found. Pick target point for arrows: ");
                if (ptRes.Status != PromptStatus.OK) return;
                Point3d target = ptRes.Value;

                using (Transaction tr = db.TransactionManager.StartTransaction())
                {
                    BlockTableRecord space = (BlockTableRecord)tr.GetObject(db.CurrentSpaceId, OpenMode.ForWrite);
                    foreach (var id in marked)
                    {
                        var br = (BlockReference)tr.GetObject(id, OpenMode.ForRead);
                        Point3d pt1 = br.Position;
                        var vec = pt1.GetVectorTo(target);
                        double ang = Math.Atan2(vec.Y, vec.X);
                        Point3d pt2 = Offset(pt1, ang, hk * 0.240);

                        Polyline pl = new Polyline();
                        pl.AddVertexAt(0, new Point2d(pt1.X, pt1.Y), 0, hk * 0.080, hk * 0.080);
                        pl.AddVertexAt(1, new Point2d(pt2.X, pt2.Y), 0, hk * 0.080, 0);
                        pl.AddVertexAt(2, new Point2d(target.X, target.Y), 0, 0, 0);
                        pl.Layer = "ren.arrow";
                        space.AppendEntity(pl);
                        tr.AddNewlyCreatedDBObject(pl, true);
                    }
                    tr.Commit();
                }

                ed.WriteMessage($"\nDIEZ: arrows drawn for {marked.Count} block(s).");
            }
            catch (System.Exception ex)
            {
                ed.WriteMessage("\nError in DIEZ: " + ex.Message);
            }
        }

        // ------------------------------------------------------------------------------
        // TDDU — применить данные опорного блока к другим с тем же POZ
        // ------------------------------------------------------------------------------
        [CommandMethod("TDDU")]
        public void CmdTddu()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            if (doc == null) return;
            Editor ed = doc.Editor;

            try
            {
                // 1) Выбор образца (можно Enter -> ручной ввод)
                var sel = ed.GetSelection(new PromptSelectionOptions { MessageForAdding = "\nSelect source RL-POS (Enter for manual): " },
                    new SelectionFilter(new[]
                    {
                        new TypedValue((int)DxfCode.Start, "INSERT"),
                        new TypedValue((int)DxfCode.BlockName, "RL-POS")
                    }));

                string poz, cap, boy, tip, a, b, c, d, e, f, r;
                if (sel.Status != PromptStatus.OK || sel.Value.Count == 0)
                {
                    // Ручной ввод
                    poz = PromptString(ed, "Poz", required: true);
                    cap = PromptString(ed, "Cap", required: true);
                    boy = PromptString(ed, "Boy", required: true);
                    if (!boy.StartsWith("L=", StringComparison.OrdinalIgnoreCase)) boy = "L=" + boy;
                    tip = PromptString(ed, "Tip", required: true);
                    a = PromptString(ed, "A", required: true);
                    b = PromptString(ed, "B", required: true);
                    c = PromptString(ed, "C", required: true);
                    d = PromptString(ed, "D", required: true);
                    e = PromptString(ed, "E", required: true);
                    f = PromptString(ed, "F", required: true);
                    r = PromptString(ed, "R", required: true);
                }
                else
                {
                    // Читаем из выбранного блока
                    var srcId = sel.Value[0].ObjectId;
                    var attrs = BlockHelper.GetAttributes(srcId);
                    poz = attrs.GetValueOrDefault("POZ", "");
                    cap = ParseTb(attrs.GetValueOrDefault("TB", "")).Cap;
                    boy = attrs.GetValueOrDefault("BOY", "");
                    tip = attrs.GetValueOrDefault("TIP", "");
                    a = attrs.GetValueOrDefault("A", "");
                    b = attrs.GetValueOrDefault("B", "");
                    c = attrs.GetValueOrDefault("C", "");
                    d = attrs.GetValueOrDefault("D", "");
                    e = attrs.GetValueOrDefault("E", "");
                    f = attrs.GetValueOrDefault("F", "");
                    r = attrs.GetValueOrDefault("R", "");
                }

                // 2) Выбор целевых блоков RL-POS
                var targetSel = ed.GetSelection(new PromptSelectionOptions { MessageForAdding = "\nSelect RL-POS to apply data:" },
                    new SelectionFilter(new[]
                    {
                        new TypedValue((int)DxfCode.Start, "INSERT"),
                        new TypedValue((int)DxfCode.BlockName, "RL-POS")
                    }));
                if (targetSel.Status != PromptStatus.OK || targetSel.Value.Count == 0)
                {
                    ed.WriteMessage("\nNo targets selected.");
                    return;
                }

                int updated = 0;
                foreach (var id in targetSel.Value.GetObjectIds())
                {
                    var attrs = BlockHelper.GetAttributes(id);
                    if (!string.Equals(attrs.GetValueOrDefault("POZ", ""), poz, StringComparison.OrdinalIgnoreCase))
                        continue;

                    var tbParts = ParseTb(attrs.GetValueOrDefault("TB", ""));
                    string bolu = string.IsNullOrWhiteSpace(tbParts.Aralik) ? "" : "/";
                    string tbNew = $"{(string.IsNullOrWhiteSpace(tbParts.AdetCarpi) ? "" : tbParts.AdetCarpi + "x")}{tbParts.Adet}Ø{cap}{bolu}{tbParts.Aralik}";

                    var nv = new Dictionary<string, string>
                    {
                        ["POZ"] = poz,
                        ["TB"] = tbNew,
                        ["BOY"] = boy,
                        ["TIP"] = tip,
                        ["A"] = a,
                        ["B"] = b,
                        ["C"] = c,
                        ["D"] = d,
                        ["E"] = e,
                        ["F"] = f,
                        ["R"] = r
                    };
                    BlockHelper.SetAttributes(id, nv);
                    updated++;
                }

                ed.WriteMessage($"\nTDDU: updated {updated} block(s).");
            }
            catch (System.Exception ex)
            {
                ed.WriteMessage("\nError in TDDU: " + ex.Message);
            }
        }

        // ------------------------------------------------------------------------------
        // PPP — стрелки от найденных позиций к точке (по образцу LISP, упрощенный)
        // ------------------------------------------------------------------------------
        [CommandMethod("PPP")]
        public void CmdPpp()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            if (doc == null) return;
            Editor ed = doc.Editor;
            Database db = doc.Database;

            try
            {
                // Запрос позы
                var pozRes = ed.GetString("\nPoz no: ");
                if (pozRes.Status != PromptStatus.OK || string.IsNullOrWhiteSpace(pozRes.StringResult)) return;
                string poz = pozRes.StringResult.Trim();

                // Сбор блоков RL-POS/RL-POS2
                TypedValue[] filterValues = [
                    new TypedValue((int)DxfCode.Start, "INSERT"),
                    new TypedValue((int)DxfCode.BlockName, "RL-POS,RL-POS2")
                ];
                var filter = new SelectionFilter(filterValues);
                var allRes = ed.SelectAll(filter);
                if (allRes.Status != PromptStatus.OK || allRes.Value.Count == 0)
                {
                    ed.WriteMessage("\nNo RL-POS blocks.");
                    return;
                }

                // Точка, куда вести стрелки
                var ptRes = ed.GetPoint("\nBase point for arrows: ");
                if (ptRes.Status != PromptStatus.OK) return;
                Point3d target = ptRes.Value;

                double hk = 0.05 * GetScaleSafe() * GetUnitsSafe(); // как в LISP: HK = 0.05 * (OLCEK_OKU * BIRIM_OKU)
                int arrows = 0;

                using (Transaction tr = db.TransactionManager.StartTransaction())
                {
                    BlockTableRecord space = (BlockTableRecord)tr.GetObject(db.CurrentSpaceId, OpenMode.ForWrite);

                    foreach (var id in allRes.Value.GetObjectIds())
                    {
                        var a = BlockHelper.GetAttributes(id);
                        if (!a.TryGetValue("POZ", out var p) || p != poz) continue;

                        var br = (BlockReference)tr.GetObject(id, OpenMode.ForRead);
                        Point3d pt1 = br.Position;
                        var vec = pt1.GetVectorTo(target);
                        double ang = Math.Atan2(vec.Y, vec.X);
                        Point3d pt2 = new Point3d(
                            pt1.X + Math.Cos(ang) * (hk * 0.240),
                            pt1.Y + Math.Sin(ang) * (hk * 0.240),
                            pt1.Z);

                        // Полилиния-стрелка
                        Polyline pl = new Polyline();
                        pl.AddVertexAt(0, new Point2d(pt1.X, pt1.Y), 0, hk * 0.080, hk * 0.080);
                        pl.AddVertexAt(1, new Point2d(pt2.X, pt2.Y), 0, hk * 0.080, 0);
                        pl.AddVertexAt(2, new Point2d(target.X, target.Y), 0, 0, 0);
                        pl.Layer = "ren.arrow";
                        space.AppendEntity(pl);
                        tr.AddNewlyCreatedDBObject(pl, true);

                        arrows++;
                    }

                    tr.Commit();
                }

                ed.WriteMessage($"\nPPP: created {arrows} arrow(s).");
            }
            catch (System.Exception ex)
            {
                ed.WriteMessage("\nError in PPP: " + ex.Message);
            }
        }

        // ------------------------------------------------------------------------------
        // PPP2 — поиск поз, зум и интерактивный просмотр/удаление (упрощенный)
        // ------------------------------------------------------------------------------
        [CommandMethod("PPP2")]
        public void CmdPpp2()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            if (doc == null) return;
            Editor ed = doc.Editor;
            Database db = doc.Database;

            try
            {
                var pozRes = ed.GetString("\nPoz no: ");
                if (pozRes.Status != PromptStatus.OK || string.IsNullOrWhiteSpace(pozRes.StringResult)) return;
                string poz = pozRes.StringResult.Trim();

                TypedValue[] filterValues = [
                    new TypedValue((int)DxfCode.Start, "INSERT"),
                    new TypedValue((int)DxfCode.BlockName, "RL-POS,RL-POS2")
                ];
                var filter = new SelectionFilter(filterValues);
                var allRes = ed.SelectAll(filter);
                if (allRes.Status != PromptStatus.OK || allRes.Value.Count == 0)
                {
                    ed.WriteMessage("\nNo RL-POS blocks.");
                    return;
                }

                var matches = new List<ObjectId>();
                using (Transaction tr = db.TransactionManager.StartTransaction())
                {
                    foreach (var id in allRes.Value.GetObjectIds())
                    {
                        var a = BlockHelper.GetAttributes(id);
                        if (a.TryGetValue("POZ", out var p) && p == poz)
                        {
                            matches.Add(id);
                        }
                    }
                    tr.Commit();
                }

                if (matches.Count == 0)
                {
                    ed.WriteMessage("\nNo matches.");
                    return;
                }

                int idx = 0;
                while (idx < matches.Count)
                {
                    var id = matches[idx];
                    using (Transaction tr = db.TransactionManager.StartTransaction())
                    {
                        var br = (BlockReference)tr.GetObject(id, OpenMode.ForRead);
                        Point3d pt = br.Position;
                        // Зум-окно вокруг точки (приблизительно)
                        double u = GetUnitsSafe();
                        ed.Command("ZOOM", "W",
                            new Point3d(pt.X - 3 * u, pt.Y - 1 * u, 0),
                            new Point3d(pt.X + 3 * u, pt.Y + 1 * u, 0));

                        tr.Commit();
                    }

                    // Диалог удаления или продолжения
                    var resp = ed.GetString($"\nNo {idx + 1}/{matches.Count} - delete this? [Y/N/Exit]: ");
                    if (resp.Status != PromptStatus.OK) break;
                    string ans = resp.StringResult?.Trim().ToUpperInvariant() ?? "";
                    if (ans == "Y")
                    {
                        using (Transaction tr = db.TransactionManager.StartTransaction())
                        {
                            var ent = (Entity)tr.GetObject(id, OpenMode.ForWrite);
                            ent.Erase();
                            tr.Commit();
                        }
                    }
                    else if (ans == "EXIT" || ans == "E")
                    {
                        break;
                    }

                    idx++;
                }
            }
            catch (System.Exception ex)
            {
                ed.WriteMessage("\nError in PPP2: " + ex.Message);
            }
        }

        // ---------------------------------------------------------------------------------------
        // КОМАНДА: POZSIL
        // Очищает позицию (POZ) у выбранных блоков RL-POS* (ставит 0).
        // ---------------------------------------------------------------------------------------
        [CommandMethod("POZSIL")]
        public void ClearPositions()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Editor ed = doc.Editor;

            try
            {
                TypedValue[] filterValues = [
                    new TypedValue((int)DxfCode.Start, "INSERT"),
                    new TypedValue((int)DxfCode.BlockName, "RL-POS*")
                ];
                SelectionFilter filter = new SelectionFilter(filterValues);
                PromptSelectionOptions selOpts = new PromptSelectionOptions
                {
                    MessageForAdding = "\nSelect RL-POS blocks to clear POZ:"
                };

                PromptSelectionResult selRes = ed.GetSelection(selOpts, filter);
                if (selRes.Status != PromptStatus.OK || selRes.Value.Count == 0)
                {
                    ed.WriteMessage("\nNo RL-POS blocks selected.");
                    return;
                }

                int cleared = 0;
                foreach (ObjectId id in selRes.Value.GetObjectIds())
                {
                    var nv = new Dictionary<string, string>
                    {
                        ["POZ"] = "0"
                    };
                    BlockHelper.SetAttributes(id, nv);
                    cleared++;
                }

                ed.WriteMessage($"\nPOZSIL: cleared POZ for {cleared} block(s).");
            }
            catch (System.Exception ex)
            {
                ed.WriteMessage($"\nError in POZSIL: " + ex.Message);
            }
        }
#endregion
    }
}