using System.Drawing;

namespace WinDesktopHarness;

internal sealed class RecordPage : UserControl
{
    private readonly string _category;
    private readonly TextBox _keyword = new();
    private readonly ComboBox _range = new();
    private readonly ComboBox _status = new();
    private readonly DataGridView _grid = new();
    private readonly Label _count = new();
    private readonly List<ResultRow> _rows = new();

    public RecordPage(string category)
    {
        _category = category;
        Dock = DockStyle.Fill;
        BackColor = UiTheme.Window;
        Font = UiTheme.Font();

        var title = UiTheme.Label(category == "建立" ? "建立紀錄" : "驗證紀錄", 15f, UiTheme.Text, FontStyle.Bold);
        title.Location = new Point(24, 20);
        Controls.Add(title);

        var subtitle = UiTheme.Label(
            category == "建立"
                ? "查詢曾完成的建立作業。雙擊清單可查看完整模擬內容。"
                : "查詢曾完成的驗證作業。雙擊清單可查看完整模擬內容。",
            9.2f, UiTheme.Muted);
        subtitle.Location = new Point(25, 52);
        Controls.Add(subtitle);

        var filter = new Panel
        {
            Left = 22,
            Top = 86,
            Height = 70,
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
            BackColor = UiTheme.Surface
        };
        filter.Width = Math.Max(600, ClientSize.Width - 44);
        Controls.Add(filter);

        var keyLabel = UiTheme.Label("關鍵字", 9.2f, UiTheme.Text, FontStyle.Bold);
        keyLabel.Location = new Point(16, 12);
        filter.Controls.Add(keyLabel);
        _keyword.Location = new Point(16, 35);
        _keyword.Size = new Size(280, 27);
        _keyword.Font = UiTheme.Font(9.5f);
        _keyword.PlaceholderText = category == "建立" ? "參考編號 / 檔名" : "檔名 / 結果";
        filter.Controls.Add(_keyword);

        var rangeLabel = UiTheme.Label("日期", 9.2f, UiTheme.Text, FontStyle.Bold);
        rangeLabel.Location = new Point(312, 12);
        filter.Controls.Add(rangeLabel);
        _range.DropDownStyle = ComboBoxStyle.DropDownList;
        _range.Items.AddRange(new object[] { "全部", "今天", "最近 7 天", "最近 30 天" });
        _range.SelectedIndex = 0;
        _range.Location = new Point(312, 35);
        _range.Size = new Size(130, 27);
        _range.Font = UiTheme.Font(9.5f);
        filter.Controls.Add(_range);

        var statusLabel = UiTheme.Label(category == "建立" ? "狀態" : "結果", 9.2f, UiTheme.Text, FontStyle.Bold);
        statusLabel.Location = new Point(456, 12);
        filter.Controls.Add(statusLabel);
        _status.DropDownStyle = ComboBoxStyle.DropDownList;
        _status.Items.Add("全部");
        if (category == "建立")
            _status.Items.AddRange(new object[] { "完成", "失敗" });
        else
            _status.Items.AddRange(new object[] { "驗證成功", "驗證失敗" });
        _status.SelectedIndex = 0;
        _status.Location = new Point(456, 35);
        _status.Size = new Size(130, 27);
        _status.Font = UiTheme.Font(9.5f);
        filter.Controls.Add(_status);

        var search = UiTheme.Button("查詢", true);
        search.Location = new Point(602, 28);
        search.Size = new Size(92, 34);
        search.Click += (_, _) => ApplyFilter();
        filter.Controls.Add(search);

        var clear = UiTheme.Button("清除");
        clear.Location = new Point(704, 28);
        clear.Size = new Size(92, 34);
        clear.Click += (_, _) =>
        {
            _keyword.Clear();
            _range.SelectedIndex = 0;
            _status.SelectedIndex = 0;
            ApplyFilter();
        };
        filter.Controls.Add(clear);

        _count.Text = "0 筆";
        _count.AutoSize = true;
        _count.ForeColor = UiTheme.Muted;
        _count.Font = UiTheme.Font(9f);
        _count.Location = new Point(24, 170);
        Controls.Add(_count);

        ConfigureGrid();
        _grid.Left = 22;
        _grid.Top = 196;
        _grid.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
        _grid.Width = Math.Max(620, ClientSize.Width - 44);
        _grid.Height = Math.Max(260, ClientSize.Height - 218);
        Controls.Add(_grid);

        filter.Resize += (_, _) => LayoutFilter(filter, search, clear);
        Resize += (_, _) =>
        {
            filter.Width = Math.Max(600, ClientSize.Width - 44);
            _grid.Width = Math.Max(620, ClientSize.Width - 44);
            _grid.Height = Math.Max(260, ClientSize.Height - 218);
            LayoutFilter(filter, search, clear);
        };
        _keyword.KeyDown += (_, e) =>
        {
            if (e.KeyCode == Keys.Enter)
            {
                ApplyFilter();
                e.SuppressKeyPress = true;
            }
        };
        _grid.CellDoubleClick += (_, e) =>
        {
            if (e.RowIndex < 0 || e.RowIndex >= _grid.Rows.Count) return;
            if (_grid.Rows[e.RowIndex].Tag is not ResultRow row) return;
            ShowDetails(row);
        };
    }

    public void SetRows(IEnumerable<ResultRow> rows)
    {
        _rows.Clear();
        _rows.AddRange(rows.Where(r => string.Equals(r.Category, _category, StringComparison.Ordinal)));
        ApplyFilter();
    }

    public void AddRow(ResultRow row)
    {
        if (!string.Equals(row.Category, _category, StringComparison.Ordinal)) return;
        _rows.Add(row);
        ApplyFilter();
    }

    private static void LayoutFilter(Panel filter, Button search, Button clear)
    {
        clear.Left = filter.ClientSize.Width - 108;
        search.Left = clear.Left - 102;
    }

    private void ConfigureGrid()
    {
        _grid.BackgroundColor = UiTheme.Surface;
        _grid.BorderStyle = BorderStyle.FixedSingle;
        _grid.ReadOnly = true;
        _grid.AllowUserToAddRows = false;
        _grid.AllowUserToDeleteRows = false;
        _grid.AllowUserToResizeRows = false;
        _grid.RowHeadersVisible = false;
        _grid.MultiSelect = false;
        _grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        _grid.AutoGenerateColumns = false;
        _grid.EnableHeadersVisualStyles = false;
        _grid.ColumnHeadersHeight = 36;
        _grid.RowTemplate.Height = 34;
        _grid.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(244, 246, 249);
        _grid.ColumnHeadersDefaultCellStyle.ForeColor = UiTheme.Text;
        _grid.ColumnHeadersDefaultCellStyle.Font = UiTheme.Font(9.3f, FontStyle.Bold);
        _grid.DefaultCellStyle.Font = UiTheme.Font(9.3f);
        _grid.DefaultCellStyle.ForeColor = UiTheme.Text;
        _grid.DefaultCellStyle.SelectionBackColor = Color.FromArgb(228, 235, 245);
        _grid.DefaultCellStyle.SelectionForeColor = UiTheme.Text;
        _grid.GridColor = Color.FromArgb(229, 232, 236);

        _grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "時間", Width = 145 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "參考編號", Width = 125 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "檔案", Width = 260 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = _category == "建立" ? "狀態" : "結果", Width = 130 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "說明", AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill });
    }

    private void ApplyFilter()
    {
        var now = DateTime.Now;
        var keyword = _keyword.Text.Trim();
        var range = _range.SelectedItem?.ToString() ?? "全部";
        var status = _status.SelectedItem?.ToString() ?? "全部";

        IEnumerable<ResultRow> filtered = _rows.OrderByDescending(r => r.CreatedAt ?? DateTime.MinValue);
        filtered = filtered.Where(row =>
        {
            var time = row.CreatedAt ?? now;
            var dateOk = range switch
            {
                "今天" => time.Date == now.Date,
                "最近 7 天" => time >= now.AddDays(-7),
                "最近 30 天" => time >= now.AddDays(-30),
                _ => true
            };
            var keywordOk = string.IsNullOrEmpty(keyword) ||
                            row.FileName.Contains(keyword, StringComparison.OrdinalIgnoreCase) ||
                            row.Reference.Contains(keyword, StringComparison.OrdinalIgnoreCase) ||
                            row.Status.Contains(keyword, StringComparison.OrdinalIgnoreCase) ||
                            row.Detail.Contains(keyword, StringComparison.OrdinalIgnoreCase);
            var statusOk = status == "全部" || string.Equals(row.Status, status, StringComparison.Ordinal);
            return dateOk && keywordOk && statusOk;
        });

        var visible = filtered.ToList();
        _grid.Rows.Clear();
        foreach (var row in visible)
        {
            var time = (row.CreatedAt ?? DateTime.Now).ToString("yyyy/MM/dd HH:mm");
            var index = _grid.Rows.Add(time, row.Reference, row.FileName, row.Status, row.Detail);
            _grid.Rows[index].Tag = row;
        }
        _count.Text = $"{visible.Count} 筆";
    }

    private void ShowDetails(ResultRow row)
    {
        var time = (row.CreatedAt ?? DateTime.Now).ToString("yyyy/MM/dd HH:mm:ss");
        MessageBox.Show(this,
            $"時間：{time}\n參考編號：{row.Reference}\n檔案：{row.FileName}\n結果：{row.Status}\n\n{row.Detail}",
            _category == "建立" ? "建立紀錄詳細資料" : "驗證紀錄詳細資料",
            MessageBoxButtons.OK, MessageBoxIcon.Information);
    }
}
