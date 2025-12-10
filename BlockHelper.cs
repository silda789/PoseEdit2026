// ====================================================================================
// ФАЙЛ: BlockHelper.cs
// НАЗНАЧЕНИЕ: Вспомогательный класс для работы с атрибутами блоков AutoCAD
// ====================================================================================
// Этот класс содержит методы для чтения и записи атрибутов блоков.
// Атрибуты блоков - это дополнительные данные, которые хранятся внутри блока.
// Например, блок "RL-POS" содержит атрибуты: POZ, TB, TIP, A, B, C, D, E, F, R и т.д.
//
// ОСНОВНЫЕ МЕТОДЫ:
//   1. GetAttributes() - читает все атрибуты из блока
//   2. SetAttributes() - записывает новые значения атрибутов в блок
//
// ВАЖНО: Все операции с базой данных AutoCAD должны выполняться внутри транзакции (Transaction)
// ====================================================================================

#nullable disable
using System;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.ApplicationServices;

namespace PoseEdit2026
{
    // ====================================================================================
    // КЛАСС: BlockHelper
    // ====================================================================================
    // "static" означает, что нам не нужно создавать объект этого класса.
    // Мы просто вызываем методы напрямую: BlockHelper.GetAttributes(...)
    //
    // ПРИМЕР ИСПОЛЬЗОВАНИЯ:
    //   ObjectId blockId = ...; // ID блока
    //   var attrs = BlockHelper.GetAttributes(blockId); // Читаем атрибуты
    //   string poz = attrs["POZ"]; // Получаем значение атрибута "POZ"
    // ====================================================================================
    public static class BlockHelper
    {
        // ====================================================================================
        // МЕТОД: GetAttributes
        // ====================================================================================
        // НАЗНАЧЕНИЕ: Читает все атрибуты из блока и возвращает их в виде словаря
        //
        // ПАРАМЕТРЫ:
        //   blockId - ID блока в базе данных AutoCAD (ObjectId)
        //
        // ВОЗВРАЩАЕТ:
        //   Dictionary<string, string> - словарь, где:
        //     - Ключ (string) - имя атрибута (Tag) в верхнем регистре (например, "POZ", "TB")
        //     - Значение (string) - текст атрибута (TextString)
        //
        // КАК ЭТО РАБОТАЕТ:
        //   1. Проверяем, что blockId не пустой (IsNull)
        //   2. Открываем транзакцию для чтения данных
        //   3. Получаем объект BlockReference по его ID
        //   4. Проходимся по всем атрибутам в коллекции AttributeCollection
        //   5. Для каждого атрибута читаем его Tag (имя) и TextString (значение)
        //   6. Сохраняем в словарь и возвращаем результат
        //
        // ПРИМЕРЫ:
        //   Если блок имеет атрибуты:
        //     POZ = "5"
        //     TB = "2x20Ø10/100 L=2260"
        //     TIP = "11"
        //
        //   Результат будет:
        //     {
        //       ["POZ"] = "5",
        //       ["TB"] = "2x20Ø10/100 L=2260",
        //       ["TIP"] = "11"
        //     }
        // ====================================================================================
        public static Dictionary<string, string> GetAttributes(ObjectId blockId)
        {
            // Создаем пустой словарь для хранения атрибутов
            var attributes = new Dictionary<string, string>();
            
            // Проверяем, что blockId не пустой
            // Если блок не найден или ID пустой, возвращаем пустой словарь
            if (blockId.IsNull) return attributes;

            // ТРАНЗАКЦИЯ - это способ безопасной работы с базой данных AutoCAD
            // Все операции чтения/записи должны выполняться внутри транзакции
            // "using" автоматически закроет транзакцию в конце (даже если будет ошибка)
            using (Transaction tr = blockId.Database.TransactionManager.StartTransaction())
            {
                // Получаем объект BlockReference из базы данных по его ID
                // "as BlockReference" означает: "попробуй преобразовать в BlockReference, 
                // если не получится - верни null"
                BlockReference blkRef = tr.GetObject(blockId, OpenMode.ForRead) as BlockReference;
                
                // Если блок найден и успешно преобразован
                if (blkRef != null)
                {
                    // AttributeCollection - это коллекция всех атрибутов блока
                    AttributeCollection attCol = blkRef.AttributeCollection;
                    
                    // Проходимся по всем атрибутам в коллекции
                    foreach (ObjectId attId in attCol)
                    {
                        // Получаем объект AttributeReference по его ID
                        AttributeReference attRef = tr.GetObject(attId, OpenMode.ForRead) as AttributeReference;
                        
                        // Если атрибут успешно получен
                        if (attRef != null)
                        {
                            // Сохраняем атрибут в словарь:
                            // - Ключ: Tag (имя атрибута) в верхнем регистре (ToUpper)
                            //   Например: "POZ", "TB", "TIP"
                            // - Значение: TextString (текст атрибута)
                            //   Например: "5", "2x20Ø10/100 L=2260", "11"
                            attributes[attRef.Tag.ToUpper()] = attRef.TextString;
                        }
                    }
                }
                
                // Сохраняем транзакцию (хотя мы только читали данные, не изменяли)
                // В реальности, для чтения можно использовать StartTransaction() без Commit,
                // но Commit не помешает и является хорошей практикой
                tr.Commit();
            }
            
            // Возвращаем словарь с атрибутами
            return attributes;
        }

        // ====================================================================================
        // МЕТОД: SetAttributes
        // ====================================================================================
        // НАЗНАЧЕНИЕ: Записывает новые значения атрибутов в блок
        //
        // ПАРАМЕТРЫ:
        //   blockId - ID блока в базе данных AutoCAD (ObjectId)
        //   newValues - словарь с новыми значениями атрибутов, где:
        //     - Ключ (string) - имя атрибута (например, "POZ", "TB")
        //     - Значение (string) - новое значение атрибута
        //
        // КАК ЭТО РАБОТАЕТ:
        //   1. Проверяем, что blockId не пустой и newValues не null
        //   2. Открываем транзакцию для записи данных
        //   3. Получаем объект BlockReference по его ID (режим записи - ForWrite)
        //   4. Проходимся по всем атрибутам в коллекции
        //   5. Для каждого атрибута проверяем, есть ли новое значение в словаре newValues
        //   6. Если есть - "переключаем" атрибут в режим записи (UpgradeOpen) и обновляем значение
        //   7. Сохраняем изменения (Commit)
        //
        // ПРИМЕРЫ:
        //   ObjectId blockId = ...;
        //   Dictionary<string, string> newValues = new Dictionary<string, string>
        //   {
        //       ["POZ"] = "10",
        //       ["TB"] = "3x25Ø12/150 L=3000",
        //       ["TIP"] = "21"
        //   };
        //   BlockHelper.SetAttributes(blockId, newValues);
        //   // Теперь атрибуты блока обновлены
        //
        // ВАЖНО:
        //   - Если атрибут с указанным именем не существует в блоке, он будет пропущен
        //   - Если в newValues нет значения для атрибута, он останется без изменений
        //   - Все имена атрибутов сравниваются в верхнем регистре (Tag.ToUpper())
        // ====================================================================================
        public static void SetAttributes(ObjectId blockId, Dictionary<string, string> newValues)
        {
            // Проверяем входные параметры
            // Если blockId пустой или newValues равен null, выходим из метода (ничего не делаем)
            if (blockId.IsNull || newValues == null) return;

            // ТРАНЗАКЦИЯ - открываем транзакцию для записи данных
            using (Transaction tr = blockId.Database.TransactionManager.StartTransaction())
            {
                // Получаем объект BlockReference из базы данных по его ID
                // OpenMode.ForWrite - режим записи (нужно для изменения атрибутов)
                BlockReference blkRef = tr.GetObject(blockId, OpenMode.ForWrite) as BlockReference;
                
                // Если блок найден и успешно преобразован
                if (blkRef != null)
                {
                    // Проходимся по всем атрибутам в коллекции блока
                    foreach (ObjectId attId in blkRef.AttributeCollection)
                    {
                        // Получаем объект AttributeReference по его ID (режим чтения)
                        AttributeReference attRef = tr.GetObject(attId, OpenMode.ForRead) as AttributeReference;
                        
                        // Проверяем два условия:
                        //   1. attRef != null - атрибут успешно получен
                        //   2. newValues.ContainsKey(attRef.Tag.ToUpper()) - в словаре newValues
                        //      есть значение для этого атрибута (по имени в верхнем регистре)
                        if (attRef != null && newValues.ContainsKey(attRef.Tag.ToUpper()))
                        {
                            // "Переключаем" атрибут в режим записи
                            // UpgradeOpen() - это метод, который "повышает" уровень доступа
                            // с ForRead до ForWrite, чтобы мы могли изменить значение
                            attRef.UpgradeOpen();
                            
                            // Обновляем текст атрибута новым значением из словаря
                            // attRef.Tag.ToUpper() - имя атрибута в верхнем регистре (например, "POZ")
                            // newValues[...] - новое значение из словаря (например, "10")
                            attRef.TextString = newValues[attRef.Tag.ToUpper()];
                        }
                    }
                }
                
                // Сохраняем все изменения в базе данных AutoCAD
                // Без Commit() все изменения будут потеряны при закрытии транзакции
                tr.Commit();
            }
        }
    }
}