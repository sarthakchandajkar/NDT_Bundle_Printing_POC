using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using NDTBundlePOC.Core.Models;
using NDTBundlePOC.Core.Services;

namespace NDTBundlePOC.UI
{
    public partial class POPlanManagementForm : Form
    {
        private readonly IDataRepository _repository;
        private DataGridView _grid;
        private Button _btnAdd;
        private Button _btnEdit;
        private Button _btnDelete;
        private Button _btnRefresh;
        private Button _btnClose;
        private TextBox _txtSearch;
        private Label _lblStatus;

        public POPlanManagementForm(IDataRepository repository)
        {
            _repository = repository;
            InitializeComponent();
            LoadPOPlans();
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();

            // Form properties
            this.Text = "PO Plan Management";
            this.Size = new Size(1200, 600);
            this.StartPosition = FormStartPosition.CenterParent;
            this.Font = new Font("Segoe UI", 9F);
            this.BackColor = Color.White;
            this.MinimumSize = new Size(1000, 500);

            // Header Panel
            var headerPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 50,
                BackColor = Color.FromArgb(0, 120, 215),
                Padding = new Padding(10)
            };

            var lblTitle = new Label
            {
                Text = "PO Plan Management",
                Dock = DockStyle.Left,
                Font = new Font("Segoe UI", 12F, FontStyle.Bold),
                ForeColor = Color.White,
                AutoSize = false,
                Height = 30,
                TextAlign = ContentAlignment.MiddleLeft
            };
            headerPanel.Controls.Add(lblTitle);

            this.Controls.Add(headerPanel);

            // Toolbar Panel
            var toolbarPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 50,
                BackColor = Color.FromArgb(240, 240, 240),
                Padding = new Padding(10, 5, 10, 5)
            };

            _btnAdd = new Button
            {
                Text = "Add New",
                Location = new Point(10, 5),
                Size = new Size(100, 30),
                BackColor = Color.FromArgb(0, 120, 215),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            _btnAdd.FlatAppearance.BorderSize = 0;
            _btnAdd.Click += BtnAdd_Click;
            toolbarPanel.Controls.Add(_btnAdd);

            _btnEdit = new Button
            {
                Text = "Edit",
                Location = new Point(120, 5),
                Size = new Size(100, 30),
                BackColor = Color.FromArgb(0, 120, 215),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            _btnEdit.FlatAppearance.BorderSize = 0;
            _btnEdit.Click += BtnEdit_Click;
            toolbarPanel.Controls.Add(_btnEdit);

            _btnDelete = new Button
            {
                Text = "Delete",
                Location = new Point(230, 5),
                Size = new Size(100, 30),
                BackColor = Color.FromArgb(220, 53, 69),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            _btnDelete.FlatAppearance.BorderSize = 0;
            _btnDelete.Click += BtnDelete_Click;
            toolbarPanel.Controls.Add(_btnDelete);

            _btnRefresh = new Button
            {
                Text = "Refresh",
                Location = new Point(340, 5),
                Size = new Size(100, 30),
                BackColor = Color.FromArgb(108, 117, 125),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            _btnRefresh.FlatAppearance.BorderSize = 0;
            _btnRefresh.Click += (s, e) => LoadPOPlans();
            toolbarPanel.Controls.Add(_btnRefresh);

            _txtSearch = new TextBox
            {
                PlaceholderText = "Search PO Plans...",
                Location = new Point(450, 8),
                Size = new Size(250, 24),
                Anchor = AnchorStyles.Top | AnchorStyles.Left
            };
            _txtSearch.TextChanged += TxtSearch_TextChanged;
            toolbarPanel.Controls.Add(_txtSearch);

            _btnClose = new Button
            {
                Text = "Close",
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                Location = new Point(0, 5),
                Size = new Size(100, 30),
                BackColor = Color.FromArgb(108, 117, 125),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            _btnClose.FlatAppearance.BorderSize = 0;
            _btnClose.Click += (s, e) => this.Close();
            toolbarPanel.Controls.Add(_btnClose);

            this.Controls.Add(toolbarPanel);

            // Grid
            _grid = new DataGridView
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                RowHeadersVisible = false,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AllowUserToResizeColumns = true,
                Font = new Font("Segoe UI", 9F),
                BackgroundColor = Color.White,
                GridColor = Color.FromArgb(240, 240, 240),
                DefaultCellStyle = new DataGridViewCellStyle
                {
                    SelectionBackColor = Color.FromArgb(0, 120, 215),
                    SelectionForeColor = Color.White,
                    Padding = new Padding(3)
                },
                ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
                {
                    BackColor = Color.FromArgb(240, 240, 240),
                    Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                    ForeColor = Color.Black,
                    Alignment = DataGridViewContentAlignment.MiddleCenter
                },
                AlternatingRowsDefaultCellStyle = new DataGridViewCellStyle
                {
                    BackColor = Color.FromArgb(250, 250, 250)
                }
            };
            _grid.DoubleClick += (s, e) => BtnEdit_Click(s, e);
            this.Controls.Add(_grid);

            // Status Bar
            var statusBar = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 30,
                BackColor = Color.FromArgb(240, 240, 240),
                BorderStyle = BorderStyle.FixedSingle,
                Padding = new Padding(10, 0, 10, 0)
            };

            _lblStatus = new Label
            {
                Text = "Ready",
                Dock = DockStyle.Left,
                Font = new Font("Segoe UI", 9F),
                TextAlign = ContentAlignment.MiddleLeft
            };
            statusBar.Controls.Add(_lblStatus);

            this.Controls.Add(statusBar);

            this.ResumeLayout(false);
        }

        private void LoadPOPlans()
        {
            try
            {
                var poPlans = _repository.GetPOPlans();
                var table = new DataTable();

                table.Columns.Add("PO_Plan_ID", typeof(int));
                table.Columns.Add("PLC_POID", typeof(int?));
                table.Columns.Add("PO_No", typeof(string));
                table.Columns.Add("Pipe_Type", typeof(string));
                table.Columns.Add("Pipe_Size", typeof(string));
                table.Columns.Add("PcsPerBundle", typeof(int));
                table.Columns.Add("Pipe_Len", typeof(decimal));
                table.Columns.Add("PipeWt_per_mtr", typeof(decimal));
                table.Columns.Add("SAP_Type", typeof(string));
                table.Columns.Add("Shop_ID", typeof(int?));

                foreach (var plan in poPlans)
                {
                    table.Rows.Add(
                        plan.PO_Plan_ID,
                        plan.PLC_POID,
                        plan.PO_No ?? "",
                        plan.Pipe_Type ?? "",
                        plan.Pipe_Size ?? "",
                        plan.PcsPerBundle,
                        plan.Pipe_Len,
                        plan.PipeWt_per_mtr,
                        plan.SAP_Type ?? "",
                        plan.Shop_ID
                    );
                }

                _grid.DataSource = table;
                ConfigureGridColumns();
                ApplySearchFilter();
                _lblStatus.Text = $"Loaded {poPlans.Count} PO Plan(s)";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading PO Plans: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                _lblStatus.Text = $"Error: {ex.Message}";
            }
        }

        private void ConfigureGridColumns()
        {
            if (_grid.Columns.Count == 0) return;

            // Hide ID column
            var idColumn = _grid.Columns["PO_Plan_ID"];
            if (idColumn != null)
                idColumn.Visible = false;

            ConfigureColumn("PLC_POID", "PLC PO ID", 8, DataGridViewContentAlignment.MiddleRight);
            ConfigureColumn("PO_No", "PO Number", 10);
            ConfigureColumn("Pipe_Type", "Pipe Type", 10);
            ConfigureColumn("Pipe_Size", "Pipe Size", 10);
            ConfigureColumn("PcsPerBundle", "Pcs Per Bundle", 10, DataGridViewContentAlignment.MiddleRight);
            ConfigureColumn("Pipe_Len", "Pipe Length", 10, DataGridViewContentAlignment.MiddleRight, "F2");
            ConfigureColumn("PipeWt_per_mtr", "Pipe Wt/mtr", 10, DataGridViewContentAlignment.MiddleRight, "F2");
            ConfigureColumn("SAP_Type", "SAP Type", 10);
            ConfigureColumn("Shop_ID", "Shop ID", 8, DataGridViewContentAlignment.MiddleRight);
        }

        private void ConfigureColumn(string name, string header, int widthPercent, DataGridViewContentAlignment alignment = DataGridViewContentAlignment.MiddleLeft, string format = null)
        {
            if (_grid.Columns[name] == null) return;

            var col = _grid.Columns[name];
            col.HeaderText = header;
            col.Width = (int)(_grid.Width * widthPercent / 100.0);
            col.DefaultCellStyle.Alignment = alignment;
            if (!string.IsNullOrEmpty(format))
                col.DefaultCellStyle.Format = format;
        }

        private void ApplySearchFilter()
        {
            if (string.IsNullOrEmpty(_txtSearch.Text))
            {
                _grid.DataSource = ((DataTable)_grid.DataSource)?.DefaultView;
                return;
            }

            var table = (DataTable)_grid.DataSource;
            if (table != null)
            {
                var search = _txtSearch.Text.ToLower();
                table.DefaultView.RowFilter = $@"PO_No LIKE '%{search}%' OR 
                                                  Pipe_Type LIKE '%{search}%' OR 
                                                  Pipe_Size LIKE '%{search}%' OR 
                                                  SAP_Type LIKE '%{search}%'";
            }
        }

        private void TxtSearch_TextChanged(object sender, EventArgs e)
        {
            ApplySearchFilter();
        }

        private void BtnAdd_Click(object sender, EventArgs e)
        {
            using (var dialog = new POPlanEditDialog(null, _repository))
            {
                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    LoadPOPlans();
                }
            }
        }

        private void BtnEdit_Click(object sender, EventArgs e)
        {
            if (_grid.SelectedRows.Count == 0)
            {
                MessageBox.Show("Please select a PO Plan to edit.", "No Selection", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var selectedRow = _grid.SelectedRows[0];
            var poPlanId = (int)selectedRow.Cells["PO_Plan_ID"].Value;
            var poPlan = _repository.GetPOPlan(poPlanId);

            if (poPlan == null)
            {
                MessageBox.Show("PO Plan not found.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            using (var dialog = new POPlanEditDialog(poPlan, _repository))
            {
                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    LoadPOPlans();
                }
            }
        }

        private void BtnDelete_Click(object sender, EventArgs e)
        {
            if (_grid.SelectedRows.Count == 0)
            {
                MessageBox.Show("Please select a PO Plan to delete.", "No Selection", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var selectedRow = _grid.SelectedRows[0];
            var poPlanId = (int)selectedRow.Cells["PO_Plan_ID"].Value;
            var poNo = selectedRow.Cells["PO_No"].Value?.ToString() ?? "";

            var result = MessageBox.Show(
                $"Are you sure you want to delete PO Plan '{poNo}' (ID: {poPlanId})?\n\nThis action cannot be undone.",
                "Confirm Delete",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning);

            if (result == DialogResult.Yes)
            {
                try
                {
                    _repository.DeletePOPlan(poPlanId);
                    _lblStatus.Text = $"PO Plan '{poNo}' deleted successfully.";
                    LoadPOPlans();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error deleting PO Plan: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    _lblStatus.Text = $"Error: {ex.Message}";
                }
            }
        }
    }
}

