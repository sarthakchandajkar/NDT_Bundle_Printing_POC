using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using NDTBundlePOC.Core.Models;
using NDTBundlePOC.Core.Services;
using OfficeOpenXml;
using System.IO;

namespace NDTBundlePOC.UI
{
    public partial class MainForm : Form
    {
        private readonly INDTBundleService _bundleService;
        private readonly IPrinterService _printerService;
        private readonly ExcelExportService _excelService;
        private readonly IDataRepository _repository;

        // Header Controls
        private Panel _headerPanel = null!;
        private Label _lblTitle = null!;
        private Label _lblDateTime = null!;
        private System.Windows.Forms.Timer _dateTimeTimer = null!;

        // Filter Controls
        private Panel _filterPanel = null!;
        private DateTimePicker _dtpFrom = null!;
        private DateTimePicker _dtpTo = null!;
        private AutoCompleteTextBox _txtPO = null!;
        private AutoCompleteTextBox _txtGrade = null!;
        private AutoCompleteTextBox _txtSize = null!;
        private AutoCompleteTextBox _txtThick = null!;
        private AutoCompleteTextBox _txtLength = null!;
        private AutoCompleteTextBox _txtType = null!;
        private ComboBox _cmbHRCBatch = null!;
        private ComboBox _cmbSlitNo = null!;
        private Button _btnFilter = null!;
        private List<Button> _shopLineButtons = new List<Button>();
        private Button _btnPrintBundles = null!;
        private Button _btnViewLog = null!;
        private TextBox _txtSearch = null!;

        // Grid Controls
        private DataGridView _bundlesGrid = null!;
        private ContextMenuStrip _gridContextMenu = null!;

        // Status Bar
        private Panel _statusBar = null!;
        private Label _lblStatus = null!;
        private Label _lblRecordCount = null!;

        // Data
        private DataTable _bundlesTable = null!;
        private List<NDTBundle> _allBundles = new List<NDTBundle>();

        public MainForm(INDTBundleService bundleService, IPrinterService printerService, ExcelExportService excelService, IDataRepository repository)
        {
            _bundleService = bundleService;
            _printerService = printerService;
            _excelService = excelService;
            _repository = repository;
            
            InitializeComponent();
            InitializeData();
            LoadBundles();
            StartDateTimeTimer();
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();

            // Form properties
            this.Text = "IIoT (PAS) - NDT Bundle Tag Printing";
            this.WindowState = FormWindowState.Maximized;
            this.StartPosition = FormStartPosition.CenterScreen;
            this.Font = new Font("Segoe UI", 9F);
            this.BackColor = Color.White;

            // Header Panel
            CreateHeaderPanel();

            // Filter Panel
            CreateFilterPanel();

            // Grid
            CreateGrid();

            // Status Bar
            CreateStatusBar();

            this.ResumeLayout(false);
        }

        private void CreateHeaderPanel()
        {
            _headerPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 40,
                BackColor = Color.FromArgb(240, 240, 240),
                Padding = new Padding(10, 5, 10, 5)
            };

            _lblTitle = new Label
            {
                Text = "IIoT (PAS) - NDT Bundle Tag Printing",
                Location = new Point(10, 10),
                AutoSize = true,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                ForeColor = Color.Black
            };
            _headerPanel.Controls.Add(_lblTitle);

            _lblDateTime = new Label
            {
                Text = DateTime.Now.ToString("dd-MMM-yy HH:mm:ss"),
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                Location = new Point(0, 10),
                AutoSize = true,
                Font = new Font("Segoe UI", 9F),
                ForeColor = Color.Black
            };
            _headerPanel.Controls.Add(_lblDateTime);

            this.Controls.Add(_headerPanel);
        }

        private void CreateFilterPanel()
        {
            _filterPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 200,
                BackColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                Padding = new Padding(10)
            };

            int yPos = 5;
            int labelWidth = 50;

            // Row 1: Date Range Filters
            Label lblFrom = new Label { Text = "From:", Location = new Point(10, yPos), Width = labelWidth, AutoSize = false };
            _filterPanel.Controls.Add(lblFrom);

            _dtpFrom = new DateTimePicker
            {
                Location = new Point(70, yPos - 2),
                Width = 180,
                Format = DateTimePickerFormat.Custom,
                CustomFormat = "dd-MMM-yy HH:mm:ss",
                ShowUpDown = true,
                Value = DateTime.Now.AddMonths(-1)
            };
            _filterPanel.Controls.Add(_dtpFrom);

            Label lblTo = new Label { Text = "To:", Location = new Point(260, yPos), Width = labelWidth, AutoSize = false };
            _filterPanel.Controls.Add(lblTo);

            _dtpTo = new DateTimePicker
            {
                Location = new Point(300, yPos - 2),
                Width = 180,
                Format = DateTimePickerFormat.Custom,
                CustomFormat = "dd-MMM-yy HH:mm:ss",
                ShowUpDown = true,
                Value = DateTime.Now
            };
            _filterPanel.Controls.Add(_dtpTo);

            yPos += 35;

            // Row 2: PO and Pipe Filters
            Label lblPO = new Label { Text = "PO:", Location = new Point(10, yPos), Width = labelWidth, AutoSize = false };
            _filterPanel.Controls.Add(lblPO);

            _txtPO = new AutoCompleteTextBox
            {
                Location = new Point(70, yPos - 2),
                Width = 150,
                Height = 20,
                Font = new Font("Segoe UI", 8F),
                FilterMode = "startswith"
            };
            _txtPO.PlaceholderText = "Select PO";
            _filterPanel.Controls.Add(_txtPO);

            Label lblPipe = new Label { Text = "Pipe:", Location = new Point(230, yPos), Width = labelWidth, AutoSize = false };
            _filterPanel.Controls.Add(lblPipe);

            int pipeX = 280;
            _txtGrade = CreatePipeFilter("Grade", pipeX, yPos, 60);
            pipeX += 70;
            _txtSize = CreatePipeFilter("Size", pipeX, yPos, 90);
            pipeX += 100;
            _txtThick = CreatePipeFilter("Thick", pipeX, yPos, 60);
            pipeX += 70;
            _txtLength = CreatePipeFilter("Length", pipeX, yPos, 60);
            pipeX += 70;
            _txtType = CreatePipeFilter("Type", pipeX, yPos, 60);

            yPos += 30;

            // Row 3: HRC Batch and Slit No
            Label lblHRC = new Label { Text = "HRC Batch No:", Location = new Point(10, yPos), Width = 100, AutoSize = false };
            _filterPanel.Controls.Add(lblHRC);

            _cmbHRCBatch = new ComboBox
            {
                Location = new Point(120, yPos - 2),
                Width = 175,
                Height = 22,
                DropDownStyle = ComboBoxStyle.DropDown,
                AutoCompleteMode = AutoCompleteMode.SuggestAppend,
                AutoCompleteSource = AutoCompleteSource.ListItems
            };
            _cmbHRCBatch.Items.Add("Select");
            _filterPanel.Controls.Add(_cmbHRCBatch);

            Label lblSlit = new Label { Text = "Slit No:", Location = new Point(305, yPos), Width = 60, AutoSize = false };
            _filterPanel.Controls.Add(lblSlit);

            _cmbSlitNo = new ComboBox
            {
                Location = new Point(375, yPos - 2),
                Width = 175,
                Height = 22,
                DropDownStyle = ComboBoxStyle.DropDown,
                AutoCompleteMode = AutoCompleteMode.SuggestAppend,
                AutoCompleteSource = AutoCompleteSource.ListItems
            };
            _cmbSlitNo.Items.Add("Select");
            _filterPanel.Controls.Add(_cmbSlitNo);

            _btnFilter = new Button
            {
                Text = "Filter",
                Location = new Point(560, yPos - 2),
                Width = 80,
                Height = 22,
                Font = new Font("Segoe UI", 8F),
                BackColor = Color.FromArgb(0, 120, 215),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            _btnFilter.FlatAppearance.BorderSize = 0;
            _btnFilter.Click += BtnFilter_Click;
            _filterPanel.Controls.Add(_btnFilter);

            yPos += 30;

            // Row 4: Shop/Line Selection Buttons
            Label lblShop = new Label { Text = "Shop/Line:", Location = new Point(10, yPos), Width = 80, AutoSize = false };
            _filterPanel.Controls.Add(lblShop);

            int shopX = 100;
            string[] shops = { "Mill 1", "Mill 2", "Mill 3", "Common" };
            foreach (var shop in shops)
            {
                var btn = new Button
                {
                    Text = shop,
                    Location = new Point(shopX, yPos - 2),
                    Width = 80,
                    Height = 22,
                    Font = new Font("Segoe UI", 8F),
                    BackColor = Color.FromArgb(240, 240, 240),
                    ForeColor = Color.Black,
                    FlatStyle = FlatStyle.Flat,
                    Tag = false // false = inactive
                };
                btn.FlatAppearance.BorderSize = 1;
                btn.Click += ShopLineButton_Click;
                _shopLineButtons.Add(btn);
                _filterPanel.Controls.Add(btn);
                shopX += 90;
            }

            yPos += 30;

            // Row 5: Action Buttons (Right-aligned)
            _btnPrintBundles = new Button
            {
                Text = "Print Bundles",
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                Location = new Point(0, yPos - 2),
                Width = 120,
                Height = 22,
                Font = new Font("Segoe UI", 8F),
                BackColor = Color.FromArgb(0, 120, 215),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            _btnPrintBundles.FlatAppearance.BorderSize = 0;
            _btnPrintBundles.Click += BtnPrintBundles_Click;
            _filterPanel.Controls.Add(_btnPrintBundles);

            _btnViewLog = new Button
            {
                Text = "View Log",
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                Location = new Point(0, yPos - 2),
                Width = 90,
                Height = 22,
                Font = new Font("Segoe UI", 8F),
                BackColor = Color.FromArgb(0, 120, 215),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            _btnViewLog.FlatAppearance.BorderSize = 0;
            _btnViewLog.Click += (s, e) => MessageBox.Show("View Log functionality not implemented", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
            _filterPanel.Controls.Add(_btnViewLog);

            // Search Box
            _txtSearch = new TextBox
            {
                PlaceholderText = "Search bundles...",
                Location = new Point(650, yPos - 2),
                Width = 200,
                Height = 22
            };
            _txtSearch.TextChanged += TxtSearch_TextChanged;
            _filterPanel.Controls.Add(_txtSearch);

            this.Controls.Add(_filterPanel);
        }

        private AutoCompleteTextBox CreatePipeFilter(string placeholder, int x, int y, int width)
        {
            var txt = new AutoCompleteTextBox
            {
                Location = new Point(x, y - 2),
                Width = width,
                Height = 20,
                Font = new Font("Segoe UI", 8F),
                FilterMode = "startswith"
            };
            txt.PlaceholderText = placeholder;
            _filterPanel.Controls.Add(txt);
            return txt;
        }

        private void CreateGrid()
        {
            _bundlesGrid = new DataGridView
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = true,
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

            // Context Menu
            _gridContextMenu = new ContextMenuStrip();
            _gridContextMenu.Items.Add("Print Selected", null, (s, e) => PrintSelectedBundles(false));
            _gridContextMenu.Items.Add("Reprint Selected", null, (s, e) => PrintSelectedBundles(true));
            _gridContextMenu.Items.Add("View Details", null, (s, e) => ViewBundleDetails());
            _gridContextMenu.Items.Add("Export to Excel", null, (s, e) => ExportGridToExcel());
            _bundlesGrid.ContextMenuStrip = _gridContextMenu;

            // Toolbar for grid
            var gridToolbar = new Panel
            {
                Dock = DockStyle.Top,
                Height = 35,
                BackColor = Color.FromArgb(240, 240, 240),
                Padding = new Padding(5)
            };

            var btnExcelExport = new Button
            {
                Text = "Excel Export",
                Location = new Point(5, 5),
                Width = 100,
                Height = 25,
                Font = new Font("Segoe UI", 9F),
                BackColor = Color.FromArgb(0, 120, 215),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            btnExcelExport.FlatAppearance.BorderSize = 0;
            btnExcelExport.Click += (s, e) => ExportGridToExcel();
            gridToolbar.Controls.Add(btnExcelExport);

            var panel = new Panel { Dock = DockStyle.Fill };
            panel.Controls.Add(_bundlesGrid);
            panel.Controls.Add(gridToolbar);
            this.Controls.Add(panel);
        }

        private void CreateStatusBar()
        {
            _statusBar = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 30,
                BackColor = Color.FromArgb(240, 240, 240),
                BorderStyle = BorderStyle.FixedSingle
            };

            _lblStatus = new Label
            {
                Text = "Ready",
                Dock = DockStyle.Left,
                Padding = new Padding(10, 0, 0, 0),
                Font = new Font("Segoe UI", 9F),
                TextAlign = ContentAlignment.MiddleLeft
            };
            _statusBar.Controls.Add(_lblStatus);

            _lblRecordCount = new Label
            {
                Text = "",
                Dock = DockStyle.Right,
                Padding = new Padding(0, 0, 10, 0),
                Font = new Font("Segoe UI", 9F),
                TextAlign = ContentAlignment.MiddleRight
            };
            _statusBar.Controls.Add(_lblRecordCount);

            this.Controls.Add(_statusBar);
        }

        private void InitializeData()
        {
            // Initialize AutoComplete data sources
            var poPlans = _repository.GetPOPlans();
            if (_txtPO != null)
                _txtPO.DataSource = poPlans.Select(p => p.PO_No).Distinct().ToList();

            var grades = poPlans.Select(p => p.Pipe_Grade).Distinct().Where(g => !string.IsNullOrEmpty(g)).ToList();
            if (_txtGrade != null)
                _txtGrade.DataSource = grades;
            if (_txtSize != null)
                _txtSize.DataSource = poPlans.Select(p => p.Pipe_Size).Distinct().Where(s => !string.IsNullOrEmpty(s)).ToList();

            // Initialize ComboBoxes
            var batches = _repository.GetNDTBundles().Select(b => b.Batch_No).Distinct().Where(b => !string.IsNullOrEmpty(b)).ToList();
            if (_cmbHRCBatch != null)
                _cmbHRCBatch.Items.AddRange(batches.ToArray());

            var slits = _repository.GetPOPlans().Select(p => p.PO_Plan_ID.ToString()).Distinct().ToList();
            if (_cmbSlitNo != null)
                _cmbSlitNo.Items.AddRange(slits.ToArray());
        }

        private void LoadBundles()
        {
            try
            {
                _allBundles = _bundleService.GetAllNDTBundles();
                _bundlesTable = CreateBundlesDataTable();
                PopulateBundlesTable();
                _bundlesGrid.DataSource = _bundlesTable;
                ConfigureGridColumns();
                ApplyFilters();
                UpdateStatusBar();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading bundles: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                _lblStatus.Text = $"Error: {ex.Message}";
            }
        }

        private DataTable CreateBundlesDataTable()
        {
            var dt = new DataTable();
            dt.Columns.Add("NDTBundle_ID", typeof(int));
            dt.Columns.Add("Bundle_No", typeof(string));
            dt.Columns.Add("Batch_No", typeof(string));
            dt.Columns.Add("PO_No", typeof(string));
            dt.Columns.Add("NDT_Pcs", typeof(int));
            dt.Columns.Add("Bundle_Wt", typeof(decimal));
            dt.Columns.Add("Status", typeof(string));
            dt.Columns.Add("BundleStartTime", typeof(DateTime));
            dt.Columns.Add("BundleEndTime", typeof(DateTime?));
            dt.Columns.Add("OperatorDoneTime", typeof(DateTime?));
            dt.Columns.Add("IsFullBundle", typeof(bool));
            dt.Columns.Add("Slit_No", typeof(string));
            return dt;
        }

        private void PopulateBundlesTable()
        {
            _bundlesTable.Rows.Clear();
            foreach (var bundle in _allBundles)
            {
                var printData = _bundleService.GetBundlePrintData(bundle.NDTBundle_ID);
                var poPlan = _repository.GetPOPlan(bundle.PO_Plan_ID);
                
                _bundlesTable.Rows.Add(
                    bundle.NDTBundle_ID,
                    bundle.Bundle_No ?? "",
                    bundle.Batch_No ?? "",
                    printData?.PO_No ?? "N/A",
                    bundle.NDT_Pcs,
                    bundle.Bundle_Wt,
                    GetStatusText(bundle.Status),
                    bundle.BundleStartTime,
                    bundle.BundleEndTime ?? (DateTime?)null,
                    bundle.OprDoneTime ?? (DateTime?)null,
                    bundle.IsFullBundle,
                    bundle.PO_Plan_ID.ToString()
                );
            }
        }

        private void ConfigureGridColumns()
        {
            if (_bundlesGrid.Columns.Count == 0) return;

            // Hide ID column
            var idColumn = _bundlesGrid.Columns["NDTBundle_ID"];
            if (idColumn != null)
                idColumn.Visible = false;

            ConfigureColumn("Bundle_No", "Bundle No.", 7, DataGridViewContentAlignment.MiddleRight);
            ConfigureColumn("Batch_No", "Batch No.", 7);
            ConfigureColumn("PO_No", "PO Number", 6);
            ConfigureColumn("NDT_Pcs", "NDT Pcs", 4, DataGridViewContentAlignment.MiddleRight);
            ConfigureColumn("Bundle_Wt", "Bundle Wt (Tons)", 5, DataGridViewContentAlignment.MiddleRight, "F2");
            ConfigureColumn("Status", "Status", 5);
            ConfigureColumn("BundleStartTime", "Bundle Start Time", 8, DataGridViewContentAlignment.MiddleRight, "dd-MMM-yy HH:mm:ss");
            ConfigureColumn("BundleEndTime", "Bundle End Time", 8, DataGridViewContentAlignment.MiddleRight, "dd-MMM-yy HH:mm:ss");
            ConfigureColumn("OperatorDoneTime", "Opr Done Time", 8, DataGridViewContentAlignment.MiddleRight, "dd-MMM-yy HH:mm:ss");
            ConfigureColumn("IsFullBundle", "Is Full", 5, DataGridViewContentAlignment.MiddleCenter);
            ConfigureColumn("Slit_No", "Slit No.", 7, DataGridViewContentAlignment.MiddleRight);

            // Color code Status column
            var statusColumn = _bundlesGrid.Columns["Status"];
            if (statusColumn != null)
            {
                int statusColumnIndex = statusColumn.Index;
                _bundlesGrid.CellFormatting += (s, e) =>
                {
                    if (e.ColumnIndex == statusColumnIndex && e.Value != null)
                    {
                        var status = e.Value.ToString();
                        if (status == "Active")
                            e.CellStyle.ForeColor = Color.Green;
                        else if (status == "Completed")
                            e.CellStyle.ForeColor = Color.Blue;
                        else if (status == "Printed")
                            e.CellStyle.ForeColor = Color.Gray;
                    }
                };
            }
        }

        private void ConfigureColumn(string name, string header, int widthPercent, DataGridViewContentAlignment alignment = DataGridViewContentAlignment.MiddleLeft, string? format = null)
        {
            if (_bundlesGrid.Columns[name] == null) return;
            
            var col = _bundlesGrid.Columns[name];
            if (col == null) return;
            
            col.HeaderText = header;
            col.Width = (int)(_bundlesGrid.Width * widthPercent / 100.0);
            col.DefaultCellStyle.Alignment = alignment;
            if (!string.IsNullOrEmpty(format))
                col.DefaultCellStyle.Format = format;
        }

        private void ApplyFilters()
        {
            var filteredRows = _bundlesTable.AsEnumerable();

            // Date filter
            filteredRows = filteredRows.Where(r =>
            {
                var startTime = r.Field<DateTime>("BundleStartTime");
                return startTime >= _dtpFrom.Value && startTime <= _dtpTo.Value;
            });

            // PO filter
            if (!string.IsNullOrEmpty(_txtPO.Text))
            {
                filteredRows = filteredRows.Where(r => r.Field<string>("PO_No")?.Contains(_txtPO.Text, StringComparison.OrdinalIgnoreCase) == true);
            }

            // Search filter
            if (!string.IsNullOrEmpty(_txtSearch.Text))
            {
                var search = _txtSearch.Text.ToLower();
                filteredRows = filteredRows.Where(r =>
                    r.Field<string>("Bundle_No")?.ToLower().Contains(search) == true ||
                    r.Field<string>("Batch_No")?.ToLower().Contains(search) == true ||
                    r.Field<string>("PO_No")?.ToLower().Contains(search) == true
                );
            }

            // Apply filter to grid
            var filteredTable = filteredRows.CopyToDataTable();
            _bundlesGrid.DataSource = filteredTable;
            UpdateStatusBar();
        }

        private void UpdateStatusBar()
        {
            int totalCount = _allBundles.Count;
            int filteredCount = _bundlesGrid.Rows.Count;
            int selectedCount = _bundlesGrid.SelectedRows.Count;

            _lblRecordCount.Text = $"Total: {totalCount} | Filtered: {filteredCount} | Selected: {selectedCount}";
        }

        private string GetStatusText(int status)
        {
            return status switch
            {
                1 => "Active",
                2 => "Completed",
                3 => "Printed",
                _ => "Unknown"
            };
        }

        private void StartDateTimeTimer()
        {
            _dateTimeTimer = new System.Windows.Forms.Timer { Interval = 1000 };
            _dateTimeTimer.Tick += (s, e) => _lblDateTime.Text = DateTime.Now.ToString("dd-MMM-yy HH:mm:ss");
            _dateTimeTimer.Start();
        }

        // Event Handlers
        private void BtnFilter_Click(object? sender, EventArgs e)
        {
            ApplyFilters();
            _lblStatus.Text = "Filters applied";
        }

        private void ShopLineButton_Click(object? sender, EventArgs e)
        {
            if (sender is Button btn)
            {
                bool isActive = btn.Tag is bool tagValue && tagValue;
                btn.Tag = !isActive;
                
                if (!isActive)
                {
                    btn.BackColor = Color.FromArgb(59, 185, 255);
                    btn.ForeColor = Color.Black;
                }
                else
                {
                    btn.BackColor = Color.FromArgb(240, 240, 240);
                    btn.ForeColor = Color.Black;
                }
                
                ApplyFilters();
            }
        }

        private void TxtSearch_TextChanged(object? sender, EventArgs e)
        {
            ApplyFilters();
        }

        private void BtnPrintBundles_Click(object? sender, EventArgs e)
        {
            PrintSelectedBundles(false);
        }

        private void PrintSelectedBundles(bool isReprint)
        {
            var selectedRows = _bundlesGrid.SelectedRows.Cast<DataGridViewRow>().ToList();
            if (selectedRows.Count == 0)
            {
                MessageBox.Show("Please select at least one bundle to print.", "No Selection", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            if (selectedRows.Count == 1)
            {
                // Single bundle - show dialog
                var bundleIdValue = selectedRows[0].Cells["NDTBundle_ID"].Value;
                if (bundleIdValue is int bundleId)
                {
                var bundleNo = selectedRows[0].Cells["Bundle_No"].Value?.ToString() ?? "";
                ShowPrintDialog(bundleNo, isReprint, bundleId);
                }
            }
            else
            {
                // Multiple bundles - print directly
                PrintMultipleBundles(selectedRows, isReprint);
            }
        }

        private void ShowPrintDialog(string bundleNo, bool isReprint, int bundleId)
        {
            using (var dialog = new PrintDialogForm(bundleNo, isReprint, bundleId, _bundleService))
            {
                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    PrintBundle(bundleId, isReprint);
                }
            }
        }

        private void PrintBundle(int bundleId, bool isReprint)
        {
            try
            {
                var printData = _bundleService.GetBundlePrintData(bundleId);
                if (printData == null)
                {
                    MessageBox.Show("Could not retrieve bundle data.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                printData.IsReprint = isReprint;
                bool printed = _printerService.PrintNDTBundleTag(printData);
                _excelService.ExportNDTBundleToExcel(printData);

                if (printed)
                {
                    _bundleService.MarkBundleAsPrinted(bundleId);
                    string action = isReprint ? "Reprinted" : "Printed";
                    _lblStatus.Text = $"✓ {action} bundle {printData.BundleNo} successfully.";
                    LoadBundles();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error printing: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void PrintMultipleBundles(List<DataGridViewRow> rows, bool isReprint)
        {
            // Show progress dialog for multiple prints
            // For now, print sequentially
            int successCount = 0;
            foreach (var row in rows)
            {
                try
                {
                    var cellValue = row.Cells["NDTBundle_ID"].Value;
                    if (cellValue != null)
                    {
                        int bundleId = (int)cellValue;
                        PrintBundle(bundleId, isReprint);
                        successCount++;
                    }
                }
                catch { }
            }
            MessageBox.Show($"Printed {successCount} of {rows.Count} bundles.", "Print Complete", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void ViewBundleDetails()
        {
            var selectedRow = _bundlesGrid.SelectedRows.Cast<DataGridViewRow>().FirstOrDefault();
            if (selectedRow == null) return;

            var bundleIdValue = selectedRow.Cells["NDTBundle_ID"].Value;
            if (bundleIdValue is not int bundleId) return;
            
            var bundle = _repository.GetNDTBundle(bundleId);
            if (bundle == null) return;

            string details = $"Bundle Details:\n\n" +
                            $"Bundle No: {bundle.Bundle_No}\n" +
                            $"Batch No: {bundle.Batch_No}\n" +
                            $"NDT Pieces: {bundle.NDT_Pcs}\n" +
                            $"Bundle Weight: {bundle.Bundle_Wt} Tons\n" +
                            $"Status: {GetStatusText(bundle.Status)}\n" +
                            $"Start Time: {bundle.BundleStartTime:dd-MMM-yy HH:mm:ss}\n" +
                            $"End Time: {bundle.BundleEndTime?.ToString("dd-MMM-yy HH:mm:ss") ?? "N/A"}\n" +
                            $"Is Full Bundle: {bundle.IsFullBundle}";

            MessageBox.Show(details, "Bundle Details", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void ExportGridToExcel()
        {
            try
            {
                var fileName = $"NDTBundleDetails_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
                var filePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "NDT_Bundle_POC_Exports", fileName);

                var dir = Path.GetDirectoryName(filePath);
                if (!string.IsNullOrEmpty(dir))
                    Directory.CreateDirectory(dir);

                using (var package = new ExcelPackage(new FileInfo(filePath)))
                {
                    var worksheet = package.Workbook.Worksheets.Add("NDT Bundles");
                    
                    // Headers
                    for (int i = 0; i < _bundlesGrid.Columns.Count; i++)
                    {
                        if (_bundlesGrid.Columns[i].Visible)
                        {
                            worksheet.Cells[1, i + 1].Value = _bundlesGrid.Columns[i].HeaderText;
                            worksheet.Cells[1, i + 1].Style.Font.Bold = true;
                        }
                    }

                    // Data
                    int rowIndex = 2;
                    foreach (DataGridViewRow row in _bundlesGrid.Rows)
                    {
                        int colIndex = 1;
                        foreach (DataGridViewColumn col in _bundlesGrid.Columns)
                        {
                            if (col.Visible)
                            {
                                worksheet.Cells[rowIndex, colIndex].Value = row.Cells[col.Index].Value;
                                colIndex++;
                            }
                        }
                        rowIndex++;
                    }

                    worksheet.Cells.AutoFitColumns();
                    package.Save();
                }

                MessageBox.Show($"Excel file exported successfully:\n{filePath}", "Export Complete", MessageBoxButtons.OK, MessageBoxIcon.Information);
                _lblStatus.Text = $"✓ Excel exported: {fileName}";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error exporting to Excel: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            _dateTimeTimer?.Stop();
            _dateTimeTimer?.Dispose();
            base.OnFormClosing(e);
        }
    }
}
