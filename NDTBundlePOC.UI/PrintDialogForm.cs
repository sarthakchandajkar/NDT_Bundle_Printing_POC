using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using NDTBundlePOC.Core.Services;

namespace NDTBundlePOC.UI
{
    public partial class PrintDialogForm : Form
    {
        private TabControl _tabControl = null!;
        private TabPage _tabOneBundle = null!;
        private TabPage _tabMultipleBundles = null!;
        private TextBox _txtBundleNo = null!;
        private TextBox _txtStartBundleNo = null!;
        private ComboBox _cmbEndBundleNo = null!;
        private Button _btnPrint = null!;
        private bool _isReprint;
        private int _bundleId;
        private INDTBundleService _bundleService;

        public PrintDialogForm(string bundleNo, bool isReprint, int bundleId, INDTBundleService bundleService)
        {
            _isReprint = isReprint;
            _bundleId = bundleId;
            _bundleService = bundleService;
            InitializeComponent(bundleNo);
        }

        private void InitializeComponent(string bundleNo)
        {
            this.SuspendLayout();

            // Form properties
            this.Text = _isReprint ? "Reprint Bundles" : "Print Bundles";
            this.Size = new Size(600, 350);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Font = new Font("Segoe UI", 9F);

            // TabControl
            _tabControl = new TabControl
            {
                Dock = DockStyle.Fill,
                Padding = new Point(10, 5),
                Font = new Font("Segoe UI", 9F)
            };

            // Tab 1: One Bundle
            _tabOneBundle = new TabPage("One Bundle");
            _tabOneBundle.Padding = new Padding(20);

            Label lblBundleNo = new Label
            {
                Text = "Bundle No:",
                Location = new Point(20, 30),
                Width = 200,
                TextAlign = ContentAlignment.MiddleRight,
                Font = new Font("Segoe UI", 9F)
            };
            _tabOneBundle.Controls.Add(lblBundleNo);

            _txtBundleNo = new TextBox
            {
                Location = new Point(230, 28),
                Width = 300,
                Text = bundleNo,
                ReadOnly = true,
                Font = new Font("Segoe UI", 9F),
                BackColor = Color.FromArgb(240, 240, 240)
            };
            _tabOneBundle.Controls.Add(_txtBundleNo);

            _tabControl.TabPages.Add(_tabOneBundle);

            // Tab 2: Multiple Bundle
            _tabMultipleBundles = new TabPage("Multiple Bundle");
            _tabMultipleBundles.Padding = new Padding(20);

            Label lblStartBundle = new Label
            {
                Text = "Start Bundle No:",
                Location = new Point(20, 30),
                Width = 200,
                TextAlign = ContentAlignment.MiddleRight,
                Font = new Font("Segoe UI", 9F)
            };
            _tabMultipleBundles.Controls.Add(lblStartBundle);

            _txtStartBundleNo = new TextBox
            {
                Location = new Point(230, 28),
                Width = 300,
                Text = bundleNo,
                ReadOnly = true,
                Font = new Font("Segoe UI", 9F),
                BackColor = Color.FromArgb(240, 240, 240)
            };
            _tabMultipleBundles.Controls.Add(_txtStartBundleNo);

            Label lblEndBundle = new Label
            {
                Text = "End Bundle No:",
                Location = new Point(20, 70),
                Width = 200,
                TextAlign = ContentAlignment.MiddleRight,
                Font = new Font("Segoe UI", 9F)
            };
            _tabMultipleBundles.Controls.Add(lblEndBundle);

            _cmbEndBundleNo = new ComboBox
            {
                Location = new Point(230, 68),
                Width = 300,
                DropDownStyle = ComboBoxStyle.DropDown,
                AutoCompleteMode = AutoCompleteMode.SuggestAppend,
                AutoCompleteSource = AutoCompleteSource.ListItems,
                Font = new Font("Segoe UI", 9F)
            };
            _cmbEndBundleNo.Items.Add("Select");
            
            // Populate with bundle numbers
            var bundles = _bundleService.GetAllNDTBundles();
            foreach (var b in bundles.OrderBy(b => b.Bundle_No))
            {
                if (!string.IsNullOrEmpty(b.Bundle_No))
                    _cmbEndBundleNo.Items.Add(b.Bundle_No);
            }
            _tabMultipleBundles.Controls.Add(_cmbEndBundleNo);

            _tabControl.TabPages.Add(_tabMultipleBundles);

            this.Controls.Add(_tabControl);

            // Print Button (bottom, right-aligned, blue background, white text)
            _btnPrint = new Button
            {
                Text = _isReprint ? "Reprint Sticker" : "Print Sticker",
                Anchor = AnchorStyles.Bottom | AnchorStyles.Right,
                Location = new Point(this.Width - 150, this.Height - 50),
                Width = 120,
                Height = 36,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                BackColor = Color.FromArgb(0, 120, 215),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                UseVisualStyleBackColor = false,
                DialogResult = DialogResult.OK
            };
            _btnPrint.FlatAppearance.BorderSize = 0;
            this.Controls.Add(_btnPrint);

            this.ResumeLayout(false);
        }
    }
}

