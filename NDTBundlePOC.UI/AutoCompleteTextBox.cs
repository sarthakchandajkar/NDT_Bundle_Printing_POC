using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace NDTBundlePOC.UI
{
    public class AutoCompleteTextBox : TextBox
    {
        private List<string> _dataSource;
        private string _filterMode; // "startswith", "contains"
        private string _placeholderText = "";

        public AutoCompleteTextBox()
        {
            _dataSource = new List<string>();
            _filterMode = "startswith";
            this.AutoCompleteMode = AutoCompleteMode.SuggestAppend;
            this.AutoCompleteSource = AutoCompleteSource.CustomSource;
            this.AutoCompleteCustomSource = new AutoCompleteStringCollection();
            
            this.Enter += AutoCompleteTextBox_Enter;
            this.Leave += AutoCompleteTextBox_Leave;
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public List<string> DataSource
        {
            get => _dataSource;
            set
            {
                _dataSource = value ?? new List<string>();
                UpdateAutoComplete();
            }
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public string FilterMode
        {
            get => _filterMode;
            set => _filterMode = value ?? "startswith";
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public new string PlaceholderText
        {
            get => _placeholderText;
            set
            {
                _placeholderText = value;
                if (string.IsNullOrEmpty(this.Text) || this.Text == _placeholderText)
                {
                    this.Text = value;
                    this.ForeColor = Color.Gray;
                }
            }
        }

        private void UpdateAutoComplete()
        {
            this.AutoCompleteCustomSource.Clear();
            foreach (var item in _dataSource)
            {
                this.AutoCompleteCustomSource.Add(item);
            }
        }

        public void FilterDataSource(string filterText)
        {
            if (string.IsNullOrEmpty(filterText))
            {
                UpdateAutoComplete();
                return;
            }

            var filtered = _dataSource.Where(item =>
            {
                if (string.IsNullOrEmpty(item)) return false;
                var itemLower = item.ToLower();
                var filterLower = filterText.ToLower();
                
                return _filterMode == "startswith" 
                    ? itemLower.StartsWith(filterLower)
                    : itemLower.Contains(filterLower);
            }).ToList();

            this.AutoCompleteCustomSource.Clear();
            foreach (var item in filtered)
            {
                this.AutoCompleteCustomSource.Add(item);
            }
        }

        private void AutoCompleteTextBox_Enter(object? sender, EventArgs e)
        {
            if (this.Text == _placeholderText)
            {
                this.Text = "";
                this.ForeColor = Color.Black;
            }
        }

        private void AutoCompleteTextBox_Leave(object? sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(this.Text))
            {
                this.Text = _placeholderText;
                this.ForeColor = Color.Gray;
            }
        }
    }
}
