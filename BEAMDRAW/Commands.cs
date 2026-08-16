// ====================================================================================
// ФАЙЛ: Commands.cs
// НАЗНАЧЕНИЕ: Точка входа плагина BEAMDRAW - отдельный AutoCAD-плагин (своя DLL, свой
//             NETLOAD) внутри Solution PoseEdit2026, для черчения арматурных деталей
//             монолитных балок и ригелей.
//
// ТЕКУЩЕЕ СОСТОЯНИЕ (см. CLAUDE.md / память сессии): открывает окно ввода данных
// (BeamDrawWindow) и просто печатает введённые данные в командную строку AutoCAD -
// самого черчения ещё нет, методика (шаг хомутов у опор/в пролёте, крюки, анкеровка)
// будет описана пользователем позже, отдельно для каждого шага.
// ====================================================================================
#nullable disable
using System.Linq; // Sum() - для подсчёта суммарной длины балки в отчёте.
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
                ed.WriteMessage("\nBEAMDRAW: otmeneno polzovatelem.");
                return;
            }

            // Черчения пока нет - просто печатаем сводку введённых данных, чтобы
            // подтвердить, что окно и модель данных работают правильно "от начала до
            // конца" (ввод -> OK -> данные доступны в коде команды).
            double totalLength = beam.Spans.Sum(s => s.Length);
            ed.WriteMessage($"\nBEAMDRAW '{beam.Name}': proletov {beam.Spans.Count}, summarnaya dlina {totalLength:F2} m.");
            ed.WriteMessage("\nChertezh poka ne stroitsya - zhdem metodiku postroeniya (shag homutov, kryuki, ankerovka i t.d.).");
        }
    }
}
