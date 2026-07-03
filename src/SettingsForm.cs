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
        private TextBox _txtTomorqaOffset;
        private TextBox _txtTomorqaHeight;
        private ComboBox _cmbTomorqaCorner;
        private TextBox _txtTomorqaArcRadius;
        private TextBox _txtTomorqaLtScale;

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
            ClientSize = new Size(500, 536);
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
            y += step;

            AddLabel("Tomorqa ichkariga (m):", labelX, y + 3);
            _txtTomorqaOffset = new TextBox { Left = inputX, Top = y, Width = 120 };
            Controls.Add(_txtTomorqaOffset);
            y += step;

            AddLabel("Tomorqa matn balandligi:", labelX, y + 3);
            _txtTomorqaHeight = new TextBox { Left = inputX, Top = y, Width = 120 };
            Controls.Add(_txtTomorqaHeight);
            y += step;

            AddLabel("Tomorqa burchaklari:", labelX, y + 3);
            _cmbTomorqaCorner = new ComboBox { Left = inputX, Top = y, Width = 200, DropDownStyle = ComboBoxStyle.DropDownList };
            _cmbTomorqaCorner.Items.AddRange(new object[] { "Qirrali", "Yoysimon" });
            Controls.Add(_cmbTomorqaCorner);
            y += step;

            AddLabel("Tomorqa yoy radiusi:", labelX, y + 3);
            _txtTomorqaArcRadius = new TextBox { Left = inputX, Top = y, Width = 120 };
            Controls.Add(_txtTomorqaArcRadius);
            y += step;

            AddLabel("Tomorqa chiziq masshtabi:", labelX, y + 3);
            _txtTomorqaLtScale = new TextBox { Left = inputX, Top = y, Width = 120 };
            Controls.Add(_txtTomorqaLtScale);
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
            _txtTomorqaOffset.Text = _s.TomorqaOffset.ToString(CultureInfo.InvariantCulture);
            _txtTomorqaHeight.Text = _s.TomorqaTextHeight.ToString(CultureInfo.InvariantCulture);
            _cmbTomorqaCorner.SelectedIndex = (int)_s.TomorqaCorner; // Sharp=0, Arc=1
            _txtTomorqaArcRadius.Text = _s.TomorqaArcRadius.ToString(CultureInfo.InvariantCulture);
            _txtTomorqaLtScale.Text = _s.TomorqaLtScale.ToString(CultureInfo.InvariantCulture);
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
            double toff;
            if (!double.TryParse(_txtTomorqaOffset.Text.Replace(',', '.'),
                    NumberStyles.Float, CultureInfo.InvariantCulture, out toff) || toff <= 0)
            {
                MessageBox.Show("Tomorqa masofasi (ichkariga) musbat son bo'lishi kerak.", "Xatolik",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                DialogResult = DialogResult.None;
                return;
            }

            double tth;
            if (!double.TryParse(_txtTomorqaHeight.Text.Replace(',', '.'),
                    NumberStyles.Float, CultureInfo.InvariantCulture, out tth) || tth <= 0)
            {
                MessageBox.Show("Tomorqa matn balandligi musbat son bo'lishi kerak.", "Xatolik",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                DialogResult = DialogResult.None;
                return;
            }

            _s.NeighborsColWidthFactor = (double)_numNeighborsWidth.Value;
            _s.Ijrochi = _txtIjrochi.Text.Trim();
            double tls;
            if (!double.TryParse(_txtTomorqaLtScale.Text.Replace(',', '.'),
                    NumberStyles.Float, CultureInfo.InvariantCulture, out tls) || tls <= 0)
            {
                MessageBox.Show("Tomorqa chiziq masshtabi musbat son bo'lishi kerak.", "Xatolik",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                DialogResult = DialogResult.None;
                return;
            }

            double tar;
            if (!double.TryParse(_txtTomorqaArcRadius.Text.Replace(',', '.'),
                    NumberStyles.Float, CultureInfo.InvariantCulture, out tar) || tar <= 0)
            {
                MessageBox.Show("Tomorqa yoy radiusi musbat son bo'lishi kerak.", "Xatolik",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                DialogResult = DialogResult.None;
                return;
            }

            _s.TomorqaOffset = toff;
            _s.TomorqaTextHeight = tth;
            _s.TomorqaCorner = (TomorqaCornerStyle)_cmbTomorqaCorner.SelectedIndex;
            _s.TomorqaArcRadius = tar;
            _s.TomorqaLtScale = tls;
            _s.Save();
        }
    }
}
