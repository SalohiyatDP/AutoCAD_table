using System;
using System.Drawing;
using System.Globalization;
using System.Windows.Forms;

namespace SalohiyatDP.AutoCADTable
{
    /// <summary>
    /// SalohiyatTable sozlamalari. Har bir funksiya sozlamalari alohida tabda ko'rsatiladi:
    /// Jadval / Chegaradoshlar / Tomorqa / Devor. "Saqlash" bosilganda diskka yoziladi.
    /// </summary>
    public class SettingsForm : Form
    {
        private readonly PluginSettings _s;

        // Jadval
        private TextBox _txtHeight;
        private NumericUpDown _numDecimals;
        private ComboBox _cmbMarker;
        private ComboBox _cmbAnchor;

        // Chegaradoshlar
        private ComboBox _cmbNeighborsAnchor;
        private TextBox _txtNeighborsHeight;
        private NumericUpDown _numNeighborsWidth;
        private TextBox _txtIjrochi;

        // Tomorqa
        private TextBox _txtTomorqaOffset;
        private TextBox _txtTomorqaHeight;
        private ComboBox _cmbTomorqaCorner;
        private TextBox _txtTomorqaArcRadius;
        private TextBox _txtTomorqaLtScale;

        // Devor
        private TextBox _txtDevorLinetype;
        private TextBox _txtDevorLtScale;
        private TextBox _txtDevorLayer;
        private TextBox _txtDevorWidth;
        private CheckBox _chkDevorReverse;

        private const int LabelX = 15;
        private const int InputX = 205;
        private const int RowStep = 34;

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
            ClientSize = new Size(490, 400);
            Font = new Font("Segoe UI", 9F);

            var tabs = new TabControl { Left = 8, Top = 8, Width = 474, Height = 340 };
            Controls.Add(tabs);

            tabs.TabPages.Add(BuildJadvalTab());
            tabs.TabPages.Add(BuildChegaraTab());
            tabs.TabPages.Add(BuildTomorqaTab());
            tabs.TabPages.Add(BuildDevorTab());

            var btnOk = new Button { Text = "Saqlash", Left = 290, Top = 356, Width = 90, DialogResult = DialogResult.OK };
            btnOk.Click += OnSave;
            Controls.Add(btnOk);

            var btnCancel = new Button { Text = "Bekor qilish", Left = 385, Top = 356, Width = 90, DialogResult = DialogResult.Cancel };
            Controls.Add(btnCancel);

            AcceptButton = btnOk;
            CancelButton = btnCancel;
        }

        private TabPage BuildJadvalTab()
        {
            var page = new TabPage("Jadval");
            int y = 18;

            _txtHeight = new TextBox { Width = 120 };
            AddRow(page, ref y, "Matn balandligi:", _txtHeight);

            _numDecimals = new NumericUpDown { Width = 120, Minimum = 0, Maximum = 6 };
            AddRow(page, ref y, "O'nlik xonalar soni:", _numDecimals);

            _cmbMarker = new ComboBox { Width = 200, DropDownStyle = ComboBoxStyle.DropDownList };
            _cmbMarker.Items.AddRange(new object[] { "Hech (belgi yo'q)", "Doira", "X belgisi" });
            AddRow(page, ref y, "Nuqta belgisi:", _cmbMarker);

            _cmbAnchor = new ComboBox { Width = 200, DropDownStyle = ComboBoxStyle.DropDownList };
            _cmbAnchor.Items.AddRange(new object[] { "Chap-yuqori", "O'ng-yuqori", "Chap-pastki", "O'ng-pastki" });
            AddRow(page, ref y, "Jadval burchagi:", _cmbAnchor);

            return page;
        }

        private TabPage BuildChegaraTab()
        {
            var page = new TabPage("Chegaradoshlar");
            int y = 18;

            _cmbNeighborsAnchor = new ComboBox { Width = 200, DropDownStyle = ComboBoxStyle.DropDownList };
            _cmbNeighborsAnchor.Items.AddRange(new object[] { "Chap-yuqori", "O'ng-yuqori", "Chap-pastki", "O'ng-pastki" });
            AddRow(page, ref y, "Chegaradosh burchagi:", _cmbNeighborsAnchor);

            _txtNeighborsHeight = new TextBox { Width = 120 };
            AddRow(page, ref y, "Chegaradosh matn balandligi:", _txtNeighborsHeight);

            _numNeighborsWidth = new NumericUpDown { Width = 120, Minimum = 4, Maximum = 80, DecimalPlaces = 0 };
            AddRow(page, ref y, "Chegaradosh ustuni (×h):", _numNeighborsWidth);

            _txtIjrochi = new TextBox { Width = 250 };
            AddRow(page, ref y, "Ijrochi:", _txtIjrochi);

            return page;
        }

        private TabPage BuildTomorqaTab()
        {
            var page = new TabPage("Tomorqa");
            int y = 18;

            _txtTomorqaOffset = new TextBox { Width = 120 };
            AddRow(page, ref y, "Ichkariga (m):", _txtTomorqaOffset);

            _txtTomorqaHeight = new TextBox { Width = 120 };
            AddRow(page, ref y, "Matn balandligi:", _txtTomorqaHeight);

            _cmbTomorqaCorner = new ComboBox { Width = 200, DropDownStyle = ComboBoxStyle.DropDownList };
            _cmbTomorqaCorner.Items.AddRange(new object[] { "Qirrali", "Yoysimon" });
            AddRow(page, ref y, "Burchaklari:", _cmbTomorqaCorner);

            _txtTomorqaArcRadius = new TextBox { Width = 120 };
            AddRow(page, ref y, "Yoy radiusi:", _txtTomorqaArcRadius);

            _txtTomorqaLtScale = new TextBox { Width = 120 };
            AddRow(page, ref y, "Chiziq masshtabi:", _txtTomorqaLtScale);

            return page;
        }

        private TabPage BuildDevorTab()
        {
            var page = new TabPage("Devor");
            int y = 18;

            _txtDevorLinetype = new TextBox { Width = 250 };
            AddRow(page, ref y, "Chiziq turi (linetype):", _txtDevorLinetype);

            _txtDevorLtScale = new TextBox { Width = 120 };
            AddRow(page, ref y, "Chiziq masshtabi:", _txtDevorLtScale);

            _txtDevorLayer = new TextBox { Width = 250 };
            AddRow(page, ref y, "Qatlam (layer):", _txtDevorLayer);

            _txtDevorWidth = new TextBox { Width = 120 };
            AddRow(page, ref y, "Devor eni (masofa, 0=yo'q):", _txtDevorWidth);

            _chkDevorReverse = new CheckBox
            {
                Text = "Ikkinchi chiziqni qarama-qarshi tomonga",
                Left = LabelX,
                Top = y,
                AutoSize = true
            };
            page.Controls.Add(_chkDevorReverse);

            return page;
        }

        private static void AddRow(TabPage page, ref int y, string labelText, Control ctl)
        {
            page.Controls.Add(new Label { Text = labelText, Left = LabelX, Top = y + 3, AutoSize = true });
            ctl.Left = InputX;
            ctl.Top = y;
            page.Controls.Add(ctl);
            y += RowStep;
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
            _cmbTomorqaCorner.SelectedIndex = (int)_s.TomorqaCorner;
            _txtTomorqaArcRadius.Text = _s.TomorqaArcRadius.ToString(CultureInfo.InvariantCulture);
            _txtTomorqaLtScale.Text = _s.TomorqaLtScale.ToString(CultureInfo.InvariantCulture);

            _txtDevorLinetype.Text = _s.DevorLinetype;
            _txtDevorLtScale.Text = _s.DevorLtScale.ToString(CultureInfo.InvariantCulture);
            _txtDevorLayer.Text = _s.DevorLayer;
            _txtDevorWidth.Text = _s.DevorWidth.ToString(CultureInfo.InvariantCulture);
            _chkDevorReverse.Checked = _s.DevorReverse;
        }

        private void OnSave(object sender, EventArgs e)
        {
            double h;
            if (!TryPositive(_txtHeight.Text, out h)) { Warn("Matn balandligi musbat son bo'lishi kerak."); return; }

            double nh;
            if (!TryPositive(_txtNeighborsHeight.Text, out nh)) { Warn("Chegaradosh matn balandligi musbat son bo'lishi kerak."); return; }

            double toff;
            if (!TryPositive(_txtTomorqaOffset.Text, out toff)) { Warn("Tomorqa masofasi musbat son bo'lishi kerak."); return; }

            double tth;
            if (!TryPositive(_txtTomorqaHeight.Text, out tth)) { Warn("Tomorqa matn balandligi musbat son bo'lishi kerak."); return; }

            double tar;
            if (!TryPositive(_txtTomorqaArcRadius.Text, out tar)) { Warn("Tomorqa yoy radiusi musbat son bo'lishi kerak."); return; }

            double tls;
            if (!TryPositive(_txtTomorqaLtScale.Text, out tls)) { Warn("Tomorqa chiziq masshtabi musbat son bo'lishi kerak."); return; }

            double dls;
            if (!TryPositive(_txtDevorLtScale.Text, out dls)) { Warn("Devor chiziq masshtabi musbat son bo'lishi kerak."); return; }

            double dw;
            if (!TryNonNegative(_txtDevorWidth.Text, out dw)) { Warn("Devor eni (masofa) manfiy bo'lmasligi kerak (0 = ikkinchi chiziq yo'q)."); return; }

            _s.TextHeight = h;
            _s.Decimals = (int)_numDecimals.Value;
            _s.Marker = (MarkerType)_cmbMarker.SelectedIndex;
            _s.Anchor = (TableAnchor)_cmbAnchor.SelectedIndex;

            _s.NeighborsAnchor = (TableAnchor)_cmbNeighborsAnchor.SelectedIndex;
            _s.NeighborsTextHeight = nh;
            _s.NeighborsColWidthFactor = (double)_numNeighborsWidth.Value;
            _s.Ijrochi = _txtIjrochi.Text.Trim();

            _s.TomorqaOffset = toff;
            _s.TomorqaTextHeight = tth;
            _s.TomorqaCorner = (TomorqaCornerStyle)_cmbTomorqaCorner.SelectedIndex;
            _s.TomorqaArcRadius = tar;
            _s.TomorqaLtScale = tls;

            _s.DevorLinetype = _txtDevorLinetype.Text.Trim();
            _s.DevorLtScale = dls;
            _s.DevorLayer = _txtDevorLayer.Text.Trim();
            _s.DevorWidth = dw;
            _s.DevorReverse = _chkDevorReverse.Checked;

            _s.Save();
        }

        private static bool TryPositive(string text, out double value)
        {
            return double.TryParse((text ?? "").Replace(',', '.'),
                       NumberStyles.Float, CultureInfo.InvariantCulture, out value) && value > 0;
        }

        private static bool TryNonNegative(string text, out double value)
        {
            return double.TryParse((text ?? "").Replace(',', '.'),
                       NumberStyles.Float, CultureInfo.InvariantCulture, out value) && value >= 0;
        }

        private void Warn(string message)
        {
            MessageBox.Show(message, "Xatolik", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            DialogResult = DialogResult.None;
        }
    }
}
