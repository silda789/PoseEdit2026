#nullable disable
using System;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.ApplicationServices;

// Имя должно быть PoseEdit2026, а не AutoCAD2024Final
namespace PoseEdit2026
{
    public static class BlockHelper
    {
        public static Dictionary<string, string> GetAttributes(ObjectId blockId)
        {
            var attributes = new Dictionary<string, string>();
            if (blockId.IsNull) return attributes;

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
                            attributes[attRef.Tag.ToUpper()] = attRef.TextString;
                        }
                    }
                }
                tr.Commit();
            }
            return attributes;
        }

        public static void SetAttributes(ObjectId blockId, Dictionary<string, string> newValues)
        {
            if (blockId.IsNull || newValues == null) return;

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