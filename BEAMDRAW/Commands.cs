// ====================================================================================
// ФАЙЛ: Commands.cs
// НАЗНАЧЕНИЕ: Точка входа плагина BEAMDRAW - отдельный AutoCAD-плагин (своя DLL, свой
//             NETLOAD) внутри Solution PoseEdit2026, для черчения арматурных деталей
//             монолитных балок и ригелей.
//
// ТЕКУЩЕЕ СОСТОЯНИЕ (см. CLAUDE.md / память сессии): открывает окно ввода данных
// (BeamDrawWindow), затем чертит только "скелет" балки (BeamDrawer.DrawSkeleton) -
// ленту балки, опоры, оси, размеры пролётов и подписи сечений. АРМАТУРЫ (хомутов,
// крюков, анкеровки) на чертеже ещё нет - методика для неё будет описана пользователем
// позже, отдельно для каждого шага.
// ====================================================================================
#nullable disable
using System.Linq; // Sum() - для подсчёта суммарной длины балки в отчёте.
using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices; // Database - нужна, чтобы передать её в BeamDrawer.
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry; // Point3d - точка вставки, которую спрашиваем у пользователя.

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
            Database db = doc.Database;

            // Создаём "пустую" балку с одним пролётом по умолчанию (см. Beam.CreateDefaultSpan
            // в BeamData.cs) и открываем окно ввода данных.
            Beam beam = new Beam();

            BeamDrawWindow win = new BeamDrawWindow(beam);

            // Application.ShowModalWindow - тот же приём, что и в PoseEdit2026 (см.
            // Commands.cs основного проекта, команда EEN): открывает WPF-окно поверх
            // AutoCAD и ждёт, пока пользователь его закроет. Возвращает true, если
            // закрыто через кнопку OK (DialogResult = true), false - если "Отмена".
            bool? result = Application.ShowModalWindow(win);

            if (result != true)
            {
                // Kirillica na komandnoy stroke AutoCAD nenadyozhna (sm. CLAUDE.md /
                // suschestvuyuschie *N komandy v PoseEdit2026) - poetomu tut translit RU,
                // a ne normalnaya kirillica. Ranshe tut byl eshche i angliyskiy tekst
                // cherez "/" - user poprosil 2026-08-16 ubrat ego iz komandnoy stroki
                // (dvuyazychnym ostalos tolko samo okno BeamDrawWindow, s pereklyuchatelem).
                ed.WriteMessage("\nBEAMDRAW: otmeneno polzovatelem.");
                return;
            }

            double totalLength = beam.Spans.Sum(s => s.Length);
            ed.WriteMessage($"\nBEAMDRAW '{beam.Name}': proletov {beam.Spans.Count}, summarnaya dlina {totalLength:F0} mm.");

            // Спрашиваем у пользователя, ГДЕ на чертеже начать (левый нижний угол ленты
            // балки, ось №1) - стандартный приём AutoCAD-команд "укажи точку".
            PromptPointOptions ppo = new PromptPointOptions(
                "\nUkazhite tochku vstavki (levyi nizhnii ugol, os 1): ");
            PromptPointResult ppr = ed.GetPoint(ppo);
            if (ppr.Status != PromptStatus.OK)
            {
                ed.WriteMessage("\nBEAMDRAW: vstavka otmenena.");
                return;
            }

            // doc.LockDocument() - обязательная "блокировка" документа перед тем, как
            // менять базу чертежа из кода команды (а не через обычный пользовательский
            // ввод) - тот же приём, что в LegacyCommands.cs основного проекта.
            using (doc.LockDocument())
            {
                BeamDrawer.DrawSkeleton(db, beam, ppr.Value);
            }

            ed.WriteMessage("\nBEAMDRAW: skelet balki postroen (bez armatury - metodika eshche ne opisana).");
        }
    }
}
