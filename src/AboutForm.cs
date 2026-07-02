using System.Drawing;
using System.Windows.Forms;

namespace SalohiyatDP.AutoCADTable
{
    /// <summary>
    /// Menyudagi "Haqida" (About) bo'limi: plagin nomi va mualliflar ko'rsatiladi.
    /// </summary>
    public class AboutForm : Form
    {
        public AboutForm()
        {
            Text = "SalohiyatTable — Haqida";
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition = FormStartPosition.CenterScreen;
            MaximizeBox = false;
            MinimizeBox = false;
            ClientSize = new Size(460, 240);
            Font = new Font("Segoe UI", 9F);

            var title = new Label
            {
                Text = PluginInfo.Name + "  v" + PluginInfo.Version,
                Font = new Font("Segoe UI", 12F, FontStyle.Bold),
                Left = 20,
                Top = 18,
                AutoSize = true
            };
            Controls.Add(title);

            var sub = new Label
            {
                Text = "Nuqtalarni raqamlab koordinata/masofa jadvali yasovchi plagin",
                Left = 20,
                Top = 52,
                Width = 420,
                AutoSize = false,
                Height = 20
            };
            Controls.Add(sub);

            var authorsTitle = new Label
            {
                Text = PluginInfo.AuthorsTitle,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                Left = 20,
                Top = 90,
                AutoSize = true
            };
            Controls.Add(authorsTitle);

            var authors = new Label
            {
                Text = PluginInfo.Author1 + "\r\n" +
                       PluginInfo.Author2 + "\r\n" +
                       PluginInfo.Organization,
                Left = 20,
                Top = 118,
                Width = 420,
                Height = 70,
                AutoSize = false
            };
            Controls.Add(authors);

            var btnOk = new Button
            {
                Text = "Yopish",
                Left = 360,
                Top = 200,
                Width = 80,
                DialogResult = DialogResult.OK
            };
            Controls.Add(btnOk);

            AcceptButton = btnOk;
            CancelButton = btnOk;
        }
    }
}
