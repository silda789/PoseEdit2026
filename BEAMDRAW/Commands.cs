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
                // Kirillica na komandnoy stroke AutoCAD nenadyozhna (sm. CLAUDE.md /
                // suschestvuyuschie *N komandy v PoseEdit2026) - poetomu tut translit RU
                // + angliyskiy tekst, a ne normalnaya kirillica.
                // Cyrillic on the AutoCAD command line is unreliable - transliterated
                // RU + English instead.
                ed.WriteMessage("\nBEAMDRAW: otmeneno polzovatelem. / Cancelled by user.");
                return;
            }

            // Черчения пока нет - просто печатаем сводку введённых данных, чтобы
            // подтвердить, что окно и модель данных работают правильно "от начала до
            // конца" (ввод -> OK -> данные доступны в коде команды).
            // No drawing yet - just print a summary of the entered data to confirm the
            // window and data model work end to end (input -> OK -> data available here).
            double totalLength = beam.Spans.Sum(s => s.Length);
            ed.WriteMessage($"\nBEAMDRAW '{beam.Name}': proletov/spans {beam.Spans.Count}, summarnaya dlina/total length {totalLength:F2} m.");
            ed.WriteMessage("\nChertezh poka ne stroitsya - zhdem metodiku postroeniya (shag homutov, kryuki, ankerovka i t.d.). / Drawing not built yet - waiting on the drawing methodology (stirrup spacing, hooks, anchorage, etc.).");
        }
    }
}
