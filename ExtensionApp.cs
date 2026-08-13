// ====================================================================================
// ФАЙЛ: ExtensionApp.cs
// НАЗНАЧЕНИЕ: Точка входа плагина для AutoCAD (IExtensionApplication). AutoCAD сам
// вызывает Initialize() один раз при загрузке DLL (NETLOAD) и Terminate() при выгрузке.
// ====================================================================================
// Здесь подписываемся на команды REGEN/REGENALL во всех открытых чертежах: если у позиции
// (блок RL-POS) есть связанная полилиния (см. RebarRecognizer.SetLinkedPolyline, кнопка
// "Determination" в EEN), при каждом REGEN/REGENALL она пересчитывается заново - так изменения
// длины/угла полилинии "на заднем фоне" попадают в TIP/A-F/R/BOY позиции без открытия EEN.
#nullable disable
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.Runtime;

[assembly: ExtensionApplication(typeof(PoseEdit2026.ExtensionApp))]

namespace PoseEdit2026
{
    public class ExtensionApp : IExtensionApplication
    {
        public void Initialize()
        {
            // Уже открытые на момент NETLOAD чертежи
            foreach (Document doc in Application.DocumentManager)
                doc.CommandEnded += OnCommandEnded;

            // Чертежи, которые откроют позже в этой же сессии AutoCAD
            Application.DocumentManager.DocumentCreated += (s, e) =>
            {
                if (e.Document != null) e.Document.CommandEnded += OnCommandEnded;
            };
        }

        public void Terminate()
        {
        }

        private void OnCommandEnded(object sender, CommandEventArgs e)
        {
            string cmd = e.GlobalCommandName?.ToUpperInvariant();
            if (cmd != "REGEN" && cmd != "REGENALL") return;

            if (sender is not Document doc) return;

            try
            {
                RebarRecognizer.SyncAllLinkedBlocksInDatabase(doc.Database);
            }
            catch
            {
                // Фоновая синхронизация не должна мешать обычной работе пользователя -
                // при ошибке просто пропускаем этот REGEN, попробуем на следующем.
            }
        }
    }
}
