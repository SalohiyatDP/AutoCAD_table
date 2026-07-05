using System.Drawing;
using System.Windows.Forms;
using Exception = System.Exception;

namespace SalohiyatDP.AutoCADTable.Licensing
{
    /// <summary>
    /// Faollashtirish oynasi: "Product key" (shu kompyuterning Machine ID si) ko'rsatiladi,
    /// foydalanuvchi "Activation key" (litsenziya matni) ni joylaydi va "Activate" bosadi.
    /// </summary>
    public class ActivationForm : Form
    {
        private readonly TextBox _productKey;
        private readonly TextBox _activationKey;

        public ActivationForm()
        {
            Text = "SalohiyatTable — Faollashtirish";
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition = FormStartPosition.CenterScreen;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowInTaskbar = false;
            ClientSize = new Size(600, 300);
            Font = new Font("Segoe UI", 9F);
            BackColor = Color.White;

            var msg = new Label
            {
                Text = "Faollashtirish zarur. Bu plagindan foydalanishda davom etish uchun " +
                       "faollashtirish kalitini quyida joylang.",
                Font = new Font("Segoe UI", 9.75F, FontStyle.Bold),
                Left = 30, Top = 30, Width = 540, Height = 48,
                TextAlign = ContentAlignment.MiddleCenter
            };
            Controls.Add(msg);

            // Product key (Machine ID)
            Controls.Add(new Label { Text = "Product key", Left = 20, Top = 122, Width = 85, Height = 23, TextAlign = ContentAlignment.MiddleLeft });
            _productKey = new TextBox { Left = 110, Top = 120, Width = 380, ReadOnly = true };
            try { _productKey.Text = MachineIdProvider.Get(); } catch (Exception ex) { _productKey.Text = "(xato: " + ex.Message + ")"; }
            Controls.Add(_productKey);
            var btnCopy = new Button { Text = "Copy", Left = 500, Top = 118, Width = 78, Height = 26 };
            btnCopy.Click += (s, e) => { try { if (!string.IsNullOrEmpty(_productKey.Text)) Clipboard.SetText(_productKey.Text); } catch { } };
            Controls.Add(btnCopy);

            // Activation key (license)
            Controls.Add(new Label { Text = "Activation key", Left = 20, Top = 162, Width = 85, Height = 23, TextAlign = ContentAlignment.MiddleLeft });
            _activationKey = new TextBox { Left = 110, Top = 160, Width = 380 };
            Controls.Add(_activationKey);
            var btnPaste = new Button { Text = "Paste", Left = 500, Top = 158, Width = 78, Height = 26 };
            btnPaste.Click += (s, e) => { try { if (Clipboard.ContainsText()) _activationKey.Text = Clipboard.GetText().Trim(); } catch { } };
            Controls.Add(btnPaste);

            // Activate
            var btnActivate = new Button { Text = "Activate", Left = 498, Top = 250, Width = 80, Height = 32 };
            btnActivate.Click += OnActivate;
            Controls.Add(btnActivate);
            AcceptButton = btnActivate;

            // Joriy holatni tekshirib, agar faol bo'lsa xabar beramiz.
            LicenseResult cur = LicenseManager.CheckInstalled();
            if (cur.IsValid)
            {
                msg.Text = "Plagin faollashtirilgan. " + cur.Message;
                msg.ForeColor = Color.FromArgb(0x1F, 0x6F, 0x43);
            }
        }

        private void OnActivate(object sender, System.EventArgs e)
        {
            LicenseResult result = LicenseManager.Install(_activationKey.Text);
            MessageBox.Show(result.Message,
                result.IsValid ? "Faollashtirildi" : "Faollashtirish xatosi",
                MessageBoxButtons.OK,
                result.IsValid ? MessageBoxIcon.Information : MessageBoxIcon.Warning);
            if (result.IsValid)
            {
                DialogResult = DialogResult.OK;
                Close();
            }
        }
    }
}
