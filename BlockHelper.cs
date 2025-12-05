using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.ApplicationServices;

namespace AutoCAD2024Final
{
    // Класс-помощник для работы с атрибутами блока
    public static class BlockHelper
    {
        // Метод для чтения всех атрибутов блока в словарь (Dictionary)
        // Аналог функции (a_oku ...)
        public static Dictionary<string, string> GetAttributes(ObjectId blockId)
        {
            var attributes = new Dictionary<string, string>();

            using (Transaction tr = blockId.Database.TransactionManager.StartTransaction())
            {
                BlockReference blkRef = tr.GetObject(blockId, OpenMode.ForRead) as BlockReference;
                if (blkRef != null)
                {
                    AttributeCollection attCol = blkRef.AttributeCollection;
                    foreach (ObjectId attId in attCol)
                    {
                        AttributeReference attRef = tr.GetObject(attId, OpenMode.ForRead) as AttributeReference;
                        if (attRef != null)
                        {
                            // Сохраняем тег (имя) атрибута и его значение
                            // Используем ToUpper(), чтобы не зависеть от регистра
                            attributes[attRef.Tag.ToUpper()] = attRef.TextString;
                        }
                    }
                }
                tr.Commit();
            }
            return attributes;
        }

        // Метод для записи значений атрибутов
        // Аналог функции (att_yaz ...)
        public static void SetAttributes(ObjectId blockId, Dictionary<string, string> newValues)
        {
            using (Transaction tr = blockId.Database.TransactionManager.StartTransaction())
            {
                BlockReference blkRef = tr.GetObject(blockId, OpenMode.ForWrite) as BlockReference;
                if (blkRef != null)
                {
                    foreach (ObjectId attId in blkRef.AttributeCollection)
                    {
                        AttributeReference attRef = tr.GetObject(attId, OpenMode.ForRead) as AttributeReference;
                        if (attRef != null && newValues.ContainsKey(attRef.Tag.ToUpper()))
                        {
                            // Открываем атрибут для записи только если нужно менять
                            attRef.UpgradeOpen();
                            attRef.TextString = newValues[attRef.Tag.ToUpper()];
                        }
                    }
                }
                tr.Commit();
            }
        }
    }
}