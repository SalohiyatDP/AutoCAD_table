using System;
using System.Drawing;
using System.Globalization;
using System.Windows.Forms;

namespace SalohiyatDP.AutoCADTable
{
    /// <summary>
    /// SalohiyatTable sozlamalari uchun dialog. "Saqlash" bosilganda qiymatlar
    /// PluginSettings orqali diskka yoziladi (keyingi seanslarda ham qoladi).
    /// </summary>
    public class SettingsForm : Form
    {
        private readonly PluginSettings _s;

        private TextBox _txtHeight;
        private NumericUpDown _numDecimals;
        private ComboBox _cmbMarker;
        private ComboBox _cmbAnchor;
        private ComboBox _cmbNeighborsAnchor;
        private NumericUpDown _numNeighborsWidth;
        private TextBox _txtNeighborsHeight;
        private TextBox _txtIjrochi;

        public SettingsForm(PluginSettings settings)
        {
            _s = settings ?? new PluginSettings();
            BuildUi();
            LoadValues();
        }

        private void BuildUi()
        {
            Text = "SalohiyatTable — Sozlamalar";
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition = FormStartPosition.CenterScreen;
            MaximizeBox = false;
            MinimizeBox = false;
            ClientSize = new Size(500, 366);
            Font = new Font("Segoe UI", 9F);

            int labelX = 15;
            int inputX = 220;
            int y = 20;
            int step = 34;

            AddLabel("Matn balandligi:", labelX, y + 3);
            _txtHeight = new TextBox { Left = inputX, Top = y, Width = 120 };
            Controls.Add(_txtHeight);
            y += step;

            AddLabel("O'nlik xonalar soni:", labelX, y + 3);
            _numDecimals = new NumericUpDown { Left = inputX, Top = y, Width = 120, Minimum = 0, Maximum = 6 };
            Controls.Add(_numDecimals);
            y += step;

            AddLabel("Nuqta belgisi:", labelX, y + 3);
            _cmbMarker = new ComboBox { Left = inputX, Top = y, Width = 200, DropDownStyle = ComboBoxStyle.DropDownList };
            _cmbMarker.Items.AddRange(new object[] { "Hech (belgi yo'q)", "Doira", "X belgisi" });
            Controls.Add(_cmbMarker);
            y += step;

            AddLabel("Jadval burchagi:", labelX, y + 3);
            _cmbAnchor = new ComboBox { Left = inputX, Top = y, Width = 200, DropDownStyle = ComboBoxStyle.DropDownList };
            _cmbAnchor.Items.AddRange(new object[] { "Chap-yuqori", "O'ng-yuqori", "Chap-pastki", "O'ng-pastki" });
            Controls.Add(_cmbAnchor);
            y += step;

            AddLabel("Chegaradoshlar burchagi:", labelX, y + 3);
            _cmbNeighborsAnchor = new ComboBox { Left = inputX, Top = y, Width = 200, DropDownStyle = ComboBoxStyle.DropDownList };
            _cmbNeighborsAnchor.Items.AddRange(new object[] { "Chap-yuqori", "O'ng-yuqori", "Chap-pastki", "O'ng-pastki" });
            Controls.Add(_cmbNeighborsAnchor);
            y += step;

            AddLabel("Chegaradosh matn balandligi:", labelX, y + 3);
            _txtNeighborsHeight = new TextBox { Left = inputX, Top = y, Width = 120 };
            Controls.Add(_txtNeighborsHeight);
            y += step;

            AddLabel("Chegaradosh ustuni (×h):", labelX, y + 3);
            _numNeighborsWidth = new NumericUpDown { Left = inputX, Top = y, Width = 120, Minimum = 4, Maximum = 80, DecimalPlaces = 0 };
            Controls.Add(_numNeighborsWidth);
            y += step;

            AddLabel("Ijrochi:", labelX, y + 3);
            _txtIjrochi = new TextBox { Left = inputX, Top = y, Width = 260 };
            Controls.Add(_txtIjrochi);
            y += step + 12;

            var btnOk = new Button { Text = "Saqlash", Left = 300, Top = y, Width = 90, DialogResult = DialogResult.OK };
            btnOk.Click += OnSave;
            Controls.Add(btnOk);

            var btnCancel = new Button { Text = "Bekor qilish", Left = 395, Top = y, Width = 90, DialogResult = DialogResult.Cancel };
            Controls.Add(btnCancel);

            AcceptButton = btnOk;
            CancelButton = btnCancel;
        }

        private void AddLabel(string text, int x, int y)
        {
            Controls.Add(new Label { Text = text, Left = x, Top = y, AutoSize = true });
        }

        private void LoadValues()
        {
            _txtHeight.Text = _s.TextHeight.ToString(CultureInfo.InvariantCulture);
            _numDecimals.Value = Math.Max(0, Math.Min(6, _s.Decimals));
            _cmbMarker.SelectedIndex = (int)_s.Marker;
            _cmbAnchor.SelectedIndex = (int)_s.Anchor;
            _cmbNeighborsAnchor.SelectedIndex = (int)_s.NeighborsAnchor;
            _txtNeighborsHeight.Text = _s.NeighborsTextHeight.ToString(CultureInfo.InvariantCulture);
            _numNeighborsWidth.Value = (decimal)Math.Max(4.0, Math.Min(80.0, _s.NeighborsColWidthFactor));
            _txtIjrochi.Text = _s.Ijrochi;
        }

        private void OnSave(object sender, EventArgs e)
        {
            double h;
            if (!double.TryParse(_txtHeight.Text.Replace(',', '.'),
                    NumberStyles.Float, CultureInfo.InvariantCulture, out h) || h <= 0)
            {
                MessageBox.Show("Matn balandligi musbat son bo'lishi kerak.", "Xatolik",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                DialogResult = DialogResult.None;
                return;
            }

            double nh;
            if (!double.TryParse(_txtNeighborsHeight.Text.Replace(',', '.'),
                    NumberStyles.Float, CultureInfo.InvariantCulture, out nh) || nh <= 0)
            {
                MessageBox.Show("Chegaradosh matn balandligi musbat son bo'lishi kerak.", "Xatolik",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                DialogResult = DialogResult.None;
                return;
            }

            _s.TextHeight = h;
            _s.Decimals = (int)_numDecimals.Value;
            _s.Marker = (MarkerType)_cmbMarker.SelectedIndex;
            _s.Anchor = (TableAnchor)_cmbAnchor.SelectedIndex;
            _s.NeighborsAnchor = (TableAnchor)_cmbNeighborsAnchor.SelectedIndex;
            _s.NeighborsTextHeight = nh;
            _s.NeighborsColWidthFactor = (double)_numNeighborsWidth.Value;
            _s.Ijrochi = _txtIjrochi.Text.Trim();
            _s.Save();
        }
    }
}
