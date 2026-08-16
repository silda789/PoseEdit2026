// ====================================================================================
// ФАЙЛ: Commands.cs
// НАЗНАЧЕНИЕ: Точка входа плагина BEAMDRAW - отдельный AutoCAD-плагин (своя DLL, свой
//             NETLOAD) внутри Solution PoseEdit2026, для черчения арматурных деталей
//             монолитных балок и ригелей. Пока только заглушка - логика черчения будет
//             добавлена по техзаданию.
// ====================================================================================
#nullable disable
using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.EditorInput;

namespace BEAMDRAW
{
    public class Commands
    {
        [CommandMethod("BEAMDRAW")]
        public void DrawBeamCommand()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            if (doc == null) return;
            Editor ed = doc.Editor;

            ed.WriteMessage("\nBEAMDRAW: komanda poka ne realizovana - zhdem tehnicheskoe zadanie (kakie dannye o balke/rigele na vhode i kak chertit armaturu).");
        }
    }
}
