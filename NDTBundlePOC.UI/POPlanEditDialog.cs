using System;
using System.Drawing;
using System.Windows.Forms;
using NDTBundlePOC.Core.Models;
using NDTBundlePOC.Core.Services;

namespace NDTBundlePOC.UI
{
    public partial class POPlanEditDialog : Form
    {
        private readonly IDataRepository _repository;
        private readonly POPlan _poPlan;
        private readonly bool _isEditMode;

        private TextBox _txtPLC_POID;
        private TextBox _txtPO_No;
        private TextBox _txtPipe_Type;
        private TextBox _txtPipe_Size;
        private NumericUpDown _numPcsPerBundle;
        private NumericUpDown _numPipe_Len;
        private NumericUpDown _numPipeWt_per_mtr;
        private TextBox _txtSAP_Type;
        private NumericUpDown _numShop_ID;
        private Button _btnSave;
        private Button _btnCancel;

        public POPlanEditDialog(POPlan poPlan, IDataRepository repository)
        {
            _repository = repository;
            _poPlan = poPlan ?? new POPlan();
            _isEditMode = poPlan != null;
            InitializeComponent();
            LoadData();
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();

            // Form properties
            this.Text = _isEditMode ? "Edit PO Plan" : "Add New PO Plan";
            this.Size = new Size(500, 450);
            this.StartPosition = FormStartPosition.CenterParent;
            this.Font = new Font("Segoe UI", 9F);
            this.BackColor = Color.White;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;

            int yPos = 20;
            int labelWidth = 150;
            int controlWidth = 300;
            int spacing = 35;

            // PLC_POID
            var lblPLC_POID = new Label
            {
                Text = "PLC PO ID:",
                Location = new Point(20, yPos),
                Size = new Size(labelWidth, 23),
                TextAlign = ContentAlignment.MiddleRight
            };
            this.Controls.Add(lblPLC_POID);

            _txtPLC_POID = new TextBox
            {
                Location = new Point(180, yPos),
                Size = new Size(controlWidth, 23)
            };
            this.Controls.Add(_txtPLC_POID);
            yPos += spacing;

            // PO_No
            var lblPO_No = new Label
            {
                Text = "PO Number:",
                Location = new Point(20, yPos),
                Size = new Size(labelWidth, 23),
                TextAlign = ContentAlignment.MiddleRight
            };
            this.Controls.Add(lblPO_No);

            _txtPO_No = new TextBox
            {
                Location = new Point(180, yPos),
                Size = new Size(controlWidth, 23)
            };
            this.Controls.Add(_txtPO_No);
            yPos += spacing;

            // Pipe_Type
            var lblPipe_Type = new Label
            {
                Text = "Pipe Type:",
                Location = new Point(20, yPos),
                Size = new Size(labelWidth, 23),
                TextAlign = ContentAlignment.MiddleRight
            };
            this.Controls.Add(lblPipe_Type);

            _txtPipe_Type = new TextBox
            {
                Location = new Point(180, yPos),
                Size = new Size(controlWidth, 23)
            };
            this.Controls.Add(_txtPipe_Type);
            yPos += spacing;

            // Pipe_Size
            var lblPipe_Size = new Label
            {
                Text = "Pipe Size:",
                Location = new Point(20, yPos),
                Size = new Size(labelWidth, 23),
                TextAlign = ContentAlignment.MiddleRight
            };
            this.Controls.Add(lblPipe_Size);

            _txtPipe_Size = new TextBox
            {
                Location = new Point(180, yPos),
                Size = new Size(controlWidth, 23)
            };
            this.Controls.Add(_txtPipe_Size);
            yPos += spacing;

            // PcsPerBundle
            var lblPcsPerBundle = new Label
            {
                Text = "Pcs Per Bundle:",
                Location = new Point(20, yPos),
                Size = new Size(labelWidth, 23),
                TextAlign = ContentAlignment.MiddleRight
            };
            this.Controls.Add(lblPcsPerBundle);

            _numPcsPerBundle = new NumericUpDown
            {
                Location = new Point(180, yPos),
                Size = new Size(controlWidth, 23),
                Minimum = 0,
                Maximum = 10000,
                Value = 0
            };
            this.Controls.Add(_numPcsPerBundle);
            yPos += spacing;

            // Pipe_Len
            var lblPipe_Len = new Label
            {
                Text = "Pipe Length:",
                Location = new Point(20, yPos),
                Size = new Size(labelWidth, 23),
                TextAlign = ContentAlignment.MiddleRight
            };
            this.Controls.Add(lblPipe_Len);

            _numPipe_Len = new NumericUpDown
            {
                Location = new Point(180, yPos),
                Size = new Size(controlWidth, 23),
                Minimum = 0,
                Maximum = 100000,
                DecimalPlaces = 2,
                Value = 0
            };
            this.Controls.Add(_numPipe_Len);
            yPos += spacing;

            // PipeWt_per_mtr
            var lblPipeWt_per_mtr = new Label
            {
                Text = "Pipe Wt/mtr:",
                Location = new Point(20, yPos),
                Size = new Size(labelWidth, 23),
                TextAlign = ContentAlignment.MiddleRight
            };
            this.Controls.Add(lblPipeWt_per_mtr);

            _numPipeWt_per_mtr = new NumericUpDown
            {
                Location = new Point(180, yPos),
                Size = new Size(controlWidth, 23),
                Minimum = 0,
                Maximum = 100000,
                DecimalPlaces = 2,
                Value = 0
            };
            this.Controls.Add(_numPipeWt_per_mtr);
            yPos += spacing;

            // SAP_Type
            var lblSAP_Type = new Label
            {
                Text = "SAP Type:",
                Location = new Point(20, yPos),
                Size = new Size(labelWidth, 23),
                TextAlign = ContentAlignment.MiddleRight
            };
            this.Controls.Add(lblSAP_Type);

            _txtSAP_Type = new TextBox
            {
                Location = new Point(180, yPos),
                Size = new Size(controlWidth, 23)
            };
            this.Controls.Add(_txtSAP_Type);
            yPos += spacing;

            // Shop_ID
            var lblShop_ID = new Label
            {
                Text = "Shop ID:",
                Location = new Point(20, yPos),
                Size = new Size(labelWidth, 23),
                TextAlign = ContentAlignment.MiddleRight
            };
            this.Controls.Add(lblShop_ID);

            _numShop_ID = new NumericUpDown
            {
                Location = new Point(180, yPos),
                Size = new Size(controlWidth, 23),
                Minimum = 0,
                Maximum = 10000,
                Value = 0
            };
            this.Controls.Add(_numShop_ID);
            yPos += spacing + 10;

            // Buttons
            var buttonPanel = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 50,
                Padding = new Padding(10)
            };

            _btnSave = new Button
            {
                Text = "Save",
                Anchor = AnchorStyles.Bottom | AnchorStyles.Right,
                Location = new Point(0, 10),
                Size = new Size(100, 30),
                BackColor = Color.FromArgb(0, 120, 215),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                DialogResult = DialogResult.OK
            };
            _btnSave.FlatAppearance.BorderSize = 0;
            _btnSave.Click += BtnSave_Click;
            buttonPanel.Controls.Add(_btnSave);

            _btnCancel = new Button
            {
                Text = "Cancel",
                Anchor = AnchorStyles.Bottom | AnchorStyles.Right,
                Location = new Point(120, 10),
                Size = new Size(100, 30),
                BackColor = Color.FromArgb(108, 117, 125),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                DialogResult = DialogResult.Cancel
            };
            _btnCancel.FlatAppearance.BorderSize = 0;
            buttonPanel.Controls.Add(_btnCancel);

            this.Controls.Add(buttonPanel);
            this.AcceptButton = _btnSave;
            this.CancelButton = _btnCancel;

            this.ResumeLayout(false);
        }

        private void LoadData()
        {
            if (_isEditMode && _poPlan != null)
            {
                _txtPLC_POID.Text = _poPlan.PLC_POID?.ToString() ?? "";
                _txtPO_No.Text = _poPlan.PO_No ?? "";
                _txtPipe_Type.Text = _poPlan.Pipe_Type ?? "";
                _txtPipe_Size.Text = _poPlan.Pipe_Size ?? "";
                _numPcsPerBundle.Value = _poPlan.PcsPerBundle;
                _numPipe_Len.Value = _poPlan.Pipe_Len;
                _numPipeWt_per_mtr.Value = _poPlan.PipeWt_per_mtr;
                _txtSAP_Type.Text = _poPlan.SAP_Type ?? "";
                _numShop_ID.Value = _poPlan.Shop_ID ?? 0;
            }
        }

        private void BtnSave_Click(object sender, EventArgs e)
        {
            // Validate required fields
            if (string.IsNullOrWhiteSpace(_txtPO_No.Text))
            {
                MessageBox.Show("PO Number is required.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                _txtPO_No.Focus();
                return;
            }

            try
            {
                // Update PO Plan object
                if (!string.IsNullOrWhiteSpace(_txtPLC_POID.Text) && int.TryParse(_txtPLC_POID.Text, out int plcPoid))
                {
                    _poPlan.PLC_POID = plcPoid;
                }
                else
                {
                    _poPlan.PLC_POID = null;
                }

                _poPlan.PO_No = _txtPO_No.Text.Trim();
                _poPlan.Pipe_Type = string.IsNullOrWhiteSpace(_txtPipe_Type.Text) ? null : _txtPipe_Type.Text.Trim();
                _poPlan.Pipe_Size = string.IsNullOrWhiteSpace(_txtPipe_Size.Text) ? null : _txtPipe_Size.Text.Trim();
                _poPlan.PcsPerBundle = (int)_numPcsPerBundle.Value;
                _poPlan.Pipe_Len = _numPipe_Len.Value;
                _poPlan.PipeWt_per_mtr = _numPipeWt_per_mtr.Value;
                _poPlan.SAP_Type = string.IsNullOrWhiteSpace(_txtSAP_Type.Text) ? null : _txtSAP_Type.Text.Trim();
                _poPlan.Shop_ID = _numShop_ID.Value > 0 ? (int?)_numShop_ID.Value : null;

                // Save to repository
                if (_isEditMode)
                {
                    _repository.UpdatePOPlan(_poPlan);
                }
                else
                {
                    _repository.AddPOPlan(_poPlan);
                }

                this.DialogResult = DialogResult.OK;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving PO Plan: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                this.DialogResult = DialogResult.None;
            }
        }
    }
}

