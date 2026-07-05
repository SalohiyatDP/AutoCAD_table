using System;
using System.Windows.Forms;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Runtime;
using AcadApp = Autodesk.AutoCAD.ApplicationServices.Application;
using AcadDoc = Autodesk.AutoCAD.ApplicationServices.Document;
// "Exception" nomi System va Autodesk.AutoCAD.Runtime da mavjud; System.Exception ni nazarda tutamiz.
using Exception = System.Exception;

// Yangi buyruqlar sinfini AutoCAD ro'yxatga olishi uchun CommandClass shart
// (loyihada boshqa CommandClass'lar mavjud bo'lgani uchun).
[assembly: CommandClass(typeof(SalohiyatDP.AutoCADTable.Licensing.LicenseCommands))]

namespace SalohiyatDP.AutoCADTable.Licensing
{
    /// <summary>
    /// Litsenziya buyruqlari:
    ///   PTID   - shu kompyuterning Machine ID sini ko'rsatadi (vendorga yuborish uchun).
    ///   PTLIC  - litsenziya faylini (.lic) tanlab o'rnatadi.
    /// Bu buyruqlar litsenziyasiz ham ishlaydi (aks holda litsenziya o'rnatib bo'lmasdi).
    /// </summary>
    public class LicenseCommands
    {
        [CommandMethod("PTID")]
        public void ShowMachineId()
        {
            AcadDoc doc = AcadApp.DocumentManager.MdiActiveDocument;
            if (doc == null) return;
            Editor ed = doc.Editor;

            string id = MachineIdProvider.Get();
            ed.WriteMessage("\nMachine ID: " + id + "\n(Nusxa olindi. Ushbu ID ni litsenziya olish uchun yuboring.)\n");

            try { Clipboard.SetText(id); } catch { /* clipboard band bo'lishi mumkin */ }

            LicenseResult cur = LicenseManager.CheckInstalled();
            string status = cur.IsValid ? "Faol" : "Faol emas";
            MessageBox.Show(
                "Machine ID (nusxa olindi):\n\n" + id +
                "\n\nLitsenziya holati: " + status + "\n" + cur.Message,
                "SalohiyatTable - Machine ID",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        [CommandMethod("PTACTIVATE")]
        public void Activate()
        {
            AcadDoc doc = AcadApp.DocumentManager.MdiActiveDocument;
            if (doc == null) return;
            using (var form = new ActivationForm())
            {
                AcadApp.ShowModalDialog(form);
            }
        }

        [CommandMethod("PTLIC")]
        public void InstallLicense()
        {
            AcadDoc doc = AcadApp.DocumentManager.MdiActiveDocument;
            if (doc == null) return;
            Editor ed = doc.Editor;

            string licenseText = null;
            using (var dlg = new OpenFileDialog
            {
                Title = "Litsenziya faylini tanlang",
                Filter = "Litsenziya fayllari (*.lic;*.txt)|*.lic;*.txt|Barcha fayllar (*.*)|*.*"
            })
            {
                // OpenFileDialog — CommonDialog (Form emas), shuning uchun to'g'ridan-to'g'ri ShowDialog().
                if (dlg.ShowDialog() != DialogResult.OK)
                {
                    ed.WriteMessage("\nBekor qilindi.");
                    return;
                }
                try { licenseText = System.IO.File.ReadAllText(dlg.FileName); }
                catch (Exception ex) { ed.WriteMessage("\nFaylni o'qib bo'lmadi: " + ex.Message); return; }
            }

            LicenseResult result = LicenseManager.Install(licenseText);
            ed.WriteMessage("\n[Litsenziya] " + result.Message + "\n");
            MessageBox.Show(result.Message,
                result.IsValid ? "Litsenziya o'rnatildi" : "Litsenziya xatosi",
                MessageBoxButtons.OK,
                result.IsValid ? MessageBoxIcon.Information : MessageBoxIcon.Warning);
        }
    }
}
