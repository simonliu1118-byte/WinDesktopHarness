using System.Drawing;

namespace WinDesktopHarness;

internal sealed class RecordPage : UserControl
{
    private readonly string _category;
    private readonly TextBox _keyword = new();
    private readonly ComboBox _range = new();
    private readonly ComboBox _status = new();
    private readonly ListView _list = new();
    private readonly List<ResultRow> _rows = new();
    private readonly Button _search;
    private readonly Button _clear;

    public RecordPage(string category)
    {
        _category = category;
        Dock = DockStyle.Fill;
        BackColor = UiTheme.Surface;
        Font = UiTheme.Font();

        const int left = 22;
        const int top = 16;

        var keyLabel = UiTheme.Label("關鍵字", 9.2f, UiTheme.Text, FontStyle.Bold);
        keyLabel.Location = new Point(left, top);
        Controls.Add(keyLabel);
        _keyword.Location = new Point(left, top + 24);
        _keyword.Size = new Size(280, 27);
        _keyword.Font = UiTheme.Font(9.5f);
        _keyword.PlaceholderText = category == "建立" ? "參考編號 / 檔名" : "檔名 / 結果";
        Controls.Add(_keyword);

        var rangeLabel = UiTheme.Label("日期", 9.2f, UiTheme.Text, FontStyle.Bold);
        rangeLabel.Location = new Point(318, top);
        Controls.Add(rangeLabel);
        _range.DropDownStyle = ComboBoxStyle.DropDownList;
        _range.Items.AddRange(new object[] { "全部", "今天", "最近 7 天", "最近 30 天" });
        _range.SelectedIndex = 0;
        _range.Location = new Point(318, top + 24);
        _range.Size = new Size(132, 27);
        _range.Font = UiTheme.Font(9.5f);
        Controls.Add(_range);

        var statusLabel = UiTheme.Label(category == "建立" ? "狀態" : "結果", 9.2f, UiTheme.Text, FontStyle.Bold);
        statusLabel.Location = new Point(466, top);
        Controls.Add(statusLabel);
        _status.DropDownStyle = ComboBoxStyle.DropDownList;
        _status.Items.Add("全部");
        if (category == "建立")
            _status.Items.AddRange(new object[] { "完成", "失敗" });
        else
            _status.Items.AddRange(new object[] { "驗證成功", "驗證失敗" });
        _status.SelectedIndex = 0;
        _status.Location = new Point(466, top + 24);
        _status.Size = new Size(132, 27);
        _status.Font = UiTheme.Font(9.5f);
        Controls.Add(_status);

        _search = UiTheme.Button("查詢", true);
        _search.Size = new Size(92, 34);
        _search.Top = top + 18;
        _search.Click += (_, _) => ApplyFilter();
        Controls.Add(_search);

        _clear = UiTheme.Button("清除");
        _clear.Size = new Size(92, 34);
        _clear.Top = top + 18;
        _clear.Click += (_, _) =>
        {
            _keyword.Clear();
            _range.SelectedIndex = 0;
            _status.SelectedIndex = 0;
            ApplyFilter();
        };
        Controls.Add(_clear);

        var separator = new Panel
        {
            Left = left,
            Top = 78,
            Height = 1,
            BackColor = UiTheme.Border,
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
        };
        Controls.Add(separator);

        ConfigureList();
        _list.Left = left;
        _list.Top = 92;
        _list.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
        Controls.Add(_list);

        Resize += (_, _) => LayoutPage(separator);
        _keyword.KeyDown += (_, e) =>
        {
            if (e.KeyCode == Keys.Enter)
            {
                ApplyFilter();
                e.SuppressKeyPress = true;
            }
        };
        _list.DoubleClick += (_, _) =>
        {
            if (_list.SelectedItems.Count != 1) return;
            if (_list.SelectedItems[0].Tag is ResultRow row)
                ShowDetails(row);
        };

        LayoutPage(separator);
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

    private void ConfigureList()
    {
        _list.View = View.Details;
        _list.FullRowSelect = true;
        _list.HideSelection = false;
        _list.MultiSelect = false;
        _list.GridLines = true;
        _list.HeaderStyle = ColumnHeaderStyle.Clickable;
        _list.BorderStyle = BorderStyle.FixedSingle;
        _list.BackColor = Color.White;
        _list.ForeColor = UiTheme.Text;
        _list.Font = UiTheme.Font(9.4f);
        _list.UseCompatibleStateImageBehavior = false;

        _list.Columns.Add("時間", 150, HorizontalAlignment.Left);
        _list.Columns.Add("參考編號", 130, HorizontalAlignment.Left);
        _list.Columns.Add("檔案", 260, HorizontalAlignment.Left);
        _list.Columns.Add(_category == "建立" ? "狀態" : "結果", 130, HorizontalAlignment.Left);
        _list.Columns.Add("說明", 280, HorizontalAlignment.Left);
    }

    private void LayoutPage(Panel separator)
    {
        var width = Math.Max(720, ClientSize.Width - 44);
        separator.Width = width;
        _clear.Left = Math.Max(620, ClientSize.Width - 22 - _clear.Width);
        _search.Left = _clear.Left - 10 - _search.Width;

        _list.Width = width;
        _list.Height = Math.Max(300, ClientSize.Height - _list.Top - 20);

        if (_list.Columns.Count == 5)
        {
            var fixedWidth = _list.Columns[0].Width + _list.Columns[1].Width + _list.Columns[2].Width + _list.Columns[3].Width;
            _list.Columns[4].Width = Math.Max(180, _list.ClientSize.Width - fixedWidth - 6);
        }
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

        _list.BeginUpdate();
        try
        {
            _list.Items.Clear();
            var index = 0;
            foreach (var row in filtered)
            {
                var time = (row.CreatedAt ?? DateTime.Now).ToString("yyyy/MM/dd HH:mm");
                var item = new ListViewItem(time)
                {
                    Tag = row,
                    BackColor = index % 2 == 0 ? Color.White : Color.FromArgb(246, 249, 252)
                };
                item.SubItems.Add(row.Reference);
                item.SubItems.Add(row.FileName);
                item.SubItems.Add(row.Status);
                item.SubItems.Add(row.Detail);
                _list.Items.Add(item);
                index++;
            }
        }
        finally
        {
            _list.EndUpdate();
        }
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
