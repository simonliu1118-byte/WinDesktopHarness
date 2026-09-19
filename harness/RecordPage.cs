using System.Drawing;
using System.Runtime.InteropServices;

namespace WinDesktopHarness;

internal sealed class RecordPage : UserControl
{
    private const int LvmFirst = 0x1000;
    private const int LvmGetCountPerPage = LvmFirst + 40;
    private static readonly object PlaceholderRow = new();

    private readonly string _category;
    private readonly TextBox _keyword = new();
    private readonly ComboBox _range = new();
    private readonly ComboBox _status = new();
    private readonly ListView _list = new();
    private readonly ImageList _rowHeightImages = new();
    private readonly ToolTip _fileToolTip = new();
    private readonly List<ResultRow> _rows = new();
    private readonly List<ResultRow> _visible = new();
    private readonly Button _search;
    private readonly Button _clear;
    private readonly Label _keywordLabel;
    private readonly Label _rangeLabel;
    private readonly Label _statusLabel;
    private string? _activeTooltipText;
    private bool _placeholderRefreshQueued;
    private bool _updatingPlaceholders;
    private bool _disposed;

    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    private static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);

    public RecordPage(string category)
    {
        _category = category;
        Dock = DockStyle.Fill;
        BackColor = Color.White;
        Font = UiTheme.Font();

        _keywordLabel = FilterLabel("關鍵字");
        _rangeLabel = FilterLabel("日期");
        _statusLabel = FilterLabel("狀態");
        Controls.Add(_keywordLabel);
        Controls.Add(_rangeLabel);
        Controls.Add(_statusLabel);

        _keyword.Font = UiTheme.Font(9.5f);
        _keyword.PlaceholderText = category == "建立" ? "參考編號 / 檔名" : "檔名 / 結果";
        Controls.Add(_keyword);

        _range.DropDownStyle = ComboBoxStyle.DropDownList;
        _range.Items.AddRange(new object[] { "全部", "今天", "最近 7 天", "最近 30 天" });
        _range.SelectedIndex = 0;
        _range.Font = UiTheme.Font(9.5f);
        Controls.Add(_range);

        _status.DropDownStyle = ComboBoxStyle.DropDownList;
        _status.Items.Add("全部");
        if (category == "建立")
            _status.Items.AddRange(new object[] { "完成", "失敗" });
        else
            _status.Items.AddRange(new object[] { "驗證成功", "驗證失敗" });
        _status.SelectedIndex = 0;
        _status.Font = UiTheme.Font(9.5f);
        Controls.Add(_status);

        _search = UiTheme.Button("查詢", true);
        _search.Size = new Size(92, 32);
        _search.Click += (_, _) => ApplyFilter();
        Controls.Add(_search);

        _clear = UiTheme.Button("清除");
        _clear.Size = new Size(92, 32);
        _clear.Click += (_, _) =>
        {
            _keyword.Clear();
            _range.SelectedIndex = 0;
            _status.SelectedIndex = 0;
            ApplyFilter();
        };
        Controls.Add(_clear);

        ConfigureList();
        Controls.Add(_list);

        Resize += (_, _) =>
        {
            LayoutPage();
            QueuePlaceholderRefresh();
        };
        VisibleChanged += (_, _) =>
        {
            if (!Visible) return;
            LayoutPage();
            QueuePlaceholderRefresh();
        };
        _keyword.KeyDown += (_, e) =>
        {
            if (e.KeyCode != Keys.Enter) return;
            ApplyFilter();
            e.SuppressKeyPress = true;
        };
        _list.MouseDown += (_, e) =>
        {
            var hit = _list.HitTest(e.Location);
            if (ReferenceEquals(hit.Item?.Tag, PlaceholderRow))
                _list.SelectedItems.Clear();
        };
        _list.DoubleClick += (_, _) =>
        {
            if (_list.SelectedItems.Count != 1) return;
            if (_list.SelectedItems[0].Tag is ResultRow row)
                ShowDetails(row);
        };
        _list.MouseMove += (_, e) => UpdateFileTooltip(e.Location);
        _list.MouseLeave += (_, _) => SetFileTooltip(null);

        LayoutPage();
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

    private static Label FilterLabel(string text) => new()
    {
        Text = text,
        AutoSize = true,
        ForeColor = UiTheme.Text,
        Font = UiTheme.Font(9.4f, FontStyle.Bold),
        TextAlign = ContentAlignment.MiddleLeft,
        BackColor = Color.Transparent
    };

    private void ConfigureList()
    {
        _list.View = View.Details;
        _list.FullRowSelect = true;
        _list.HideSelection = true;
        _list.MultiSelect = false;
        _list.GridLines = false;
        _list.HeaderStyle = ColumnHeaderStyle.Nonclickable;
        _list.BorderStyle = BorderStyle.FixedSingle;
        _list.BackColor = Color.White;
        _list.ForeColor = UiTheme.Text;
        _list.Font = UiTheme.Font(9.4f);
        _list.UseCompatibleStateImageBehavior = false;
        _list.OwnerDraw = true;

        _rowHeightImages.ColorDepth = ColorDepth.Depth32Bit;
        _rowHeightImages.ImageSize = new Size(1, 23);
        _rowHeightImages.Images.Add(new Bitmap(1, 23));
        _list.SmallImageList = _rowHeightImages;

        _list.Columns.Add("時間", 145, HorizontalAlignment.Left);
        _list.Columns.Add("參考編號", 125, HorizontalAlignment.Left);
        _list.Columns.Add("檔案", 280, HorizontalAlignment.Left);
        _list.Columns.Add(_category == "建立" ? "狀態" : "結果", _category == "建立" ? 86 : 112, HorizontalAlignment.Left);
        _list.Columns.Add("說明", 320, HorizontalAlignment.Left);

        _list.DrawColumnHeader += (_, e) => DrawHeader(e);
        _list.DrawItem += (_, e) =>
        {
            if (_list.View != View.Details) e.DrawDefault = true;
        };
        _list.DrawSubItem += (_, e) => DrawSubItem(e);
    }

    private void DrawHeader(DrawListViewColumnHeaderEventArgs e)
    {
        using var background = new SolidBrush(Color.FromArgb(246, 246, 246));
        e.Graphics.FillRectangle(background, e.Bounds);
        var textBounds = Rectangle.Inflate(e.Bounds, -6, 0);
        TextRenderer.DrawText(e.Graphics, e.Header?.Text ?? string.Empty, _list.Font, textBounds,
            UiTheme.Text, TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine |
            TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);
        using var pen = new Pen(Color.FromArgb(190, 190, 190));
        e.Graphics.DrawLine(pen, e.Bounds.Right - 1, e.Bounds.Top, e.Bounds.Right - 1, e.Bounds.Bottom);
        e.Graphics.DrawLine(pen, e.Bounds.Left, e.Bounds.Bottom - 1, e.Bounds.Right, e.Bounds.Bottom - 1);
    }

    private void DrawSubItem(DrawListViewSubItemEventArgs e)
    {
        var placeholder = ReferenceEquals(e.Item.Tag, PlaceholderRow);
        var selected = e.Item.Selected && !placeholder;
        var background = selected
            ? Color.FromArgb(224, 233, 244)
            : e.Item.Index % 2 == 0 ? Color.White : Color.FromArgb(239, 244, 249);

        using (var brush = new SolidBrush(background))
            e.Graphics.FillRectangle(brush, e.Bounds);

        if (!placeholder)
        {
            var textBounds = Rectangle.Inflate(e.Bounds, -6, 0);
            TextRenderer.DrawText(e.Graphics, e.SubItem?.Text ?? string.Empty, _list.Font, textBounds,
                UiTheme.Text, TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine |
                TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);
        }

        using var pen = new Pen(Color.FromArgb(205, 205, 205));
        e.Graphics.DrawLine(pen, e.Bounds.Right - 1, e.Bounds.Top, e.Bounds.Right - 1, e.Bounds.Bottom);
        e.Graphics.DrawLine(pen, e.Bounds.Left, e.Bounds.Bottom - 1, e.Bounds.Right, e.Bounds.Bottom - 1);
    }

    private void LayoutPage()
    {
        if (_disposed) return;

        const int left = 16;
        const int top = 16;
        const int labelGap = 8;
        const int fieldHeight = 28;

        _keywordLabel.Location = new Point(left, top + 5);
        _keyword.Location = new Point(78, top);
        _keyword.Size = new Size(240, fieldHeight);

        _rangeLabel.Location = new Point(336, top + 5);
        _range.Location = new Point(374, top);
        _range.Size = new Size(136, fieldHeight);

        _statusLabel.Location = new Point(528, top + 5);
        _status.Location = new Point(574, top);
        _status.Size = new Size(136, fieldHeight);

        _clear.Left = Math.Max(812, ClientSize.Width - left - _clear.Width);
        _search.Left = _clear.Left - labelGap - _search.Width;
        _search.Top = top - 2;
        _clear.Top = top - 2;

        _list.Left = left;
        _list.Top = 58;
        _list.Width = Math.Max(720, ClientSize.Width - (left * 2));
        _list.Height = Math.Max(300, ClientSize.Height - _list.Top - 14);

        LayoutColumns();
    }

    private void LayoutColumns()
    {
        if (_disposed || _list.Columns.Count != 5) return;

        const int timeWidth = 145;
        const int referenceWidth = 125;
        var resultWidth = _category == "建立" ? 86 : 112;
        var clientWidth = Math.Max(760, _list.ClientSize.Width - 5);
        var fileWidth = Math.Max(230, (int)(clientWidth * 0.30));
        var used = timeWidth + referenceWidth + fileWidth + resultWidth;
        var detailWidth = Math.Max(260, clientWidth - used);

        _list.Columns[0].Width = timeWidth;
        _list.Columns[1].Width = referenceWidth;
        _list.Columns[2].Width = fileWidth;
        _list.Columns[3].Width = resultWidth;
        _list.Columns[4].Width = detailWidth;
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

        _visible.Clear();
        _visible.AddRange(filtered);
        RefreshList();
    }

    private void RefreshList()
    {
        if (_disposed) return;

        _list.BeginUpdate();
        try
        {
            _list.Items.Clear();
            foreach (var row in _visible)
            {
                var item = new ListViewItem((row.CreatedAt ?? DateTime.Now).ToString("yyyy/MM/dd HH:mm"))
                {
                    Tag = row
                };
                item.SubItems.Add(row.Reference);
                item.SubItems.Add(row.FileName);
                item.SubItems.Add(row.Status);
                item.SubItems.Add(row.Detail);
                _list.Items.Add(item);
            }
        }
        finally
        {
            _list.EndUpdate();
        }

        QueuePlaceholderRefresh();
        _list.Invalidate(true);
    }

    private void QueuePlaceholderRefresh()
    {
        if (_disposed || _placeholderRefreshQueued || _updatingPlaceholders || !Visible || !IsHandleCreated) return;
        _placeholderRefreshQueued = true;
        BeginInvoke((Action)(() =>
        {
            _placeholderRefreshQueued = false;
            if (_disposed || !Visible || _updatingPlaceholders) return;
            RefreshPlaceholderRows();
        }));
    }

    private void RefreshPlaceholderRows()
    {
        if (_disposed || _updatingPlaceholders || !_list.IsHandleCreated) return;

        _updatingPlaceholders = true;
        try
        {
            var capacity = (int)SendMessage(_list.Handle, LvmGetCountPerPage, IntPtr.Zero, IntPtr.Zero);
            if (capacity <= 0)
                capacity = Math.Max(1, (_list.ClientSize.Height - 30) / 23);

            _list.BeginUpdate();
            try
            {
                while (_list.Items.Count > _visible.Count)
                    _list.Items.RemoveAt(_list.Items.Count - 1);

                // Fake zebra rows only fill unused visible slots. They never extend the
                // scroll range beyond one page. Real rows replace these one-for-one.
                if (_visible.Count < capacity)
                {
                    var placeholders = capacity - _visible.Count;
                    for (var i = 0; i < placeholders; i++)
                    {
                        var blank = new ListViewItem(string.Empty) { Tag = PlaceholderRow };
                        blank.SubItems.Add(string.Empty);
                        blank.SubItems.Add(string.Empty);
                        blank.SubItems.Add(string.Empty);
                        blank.SubItems.Add(string.Empty);
                        _list.Items.Add(blank);
                    }
                }
            }
            finally
            {
                _list.EndUpdate();
            }
        }
        finally
        {
            _updatingPlaceholders = false;
        }

        _list.Invalidate(true);
    }

    private void UpdateFileTooltip(Point location)
    {
        var hit = _list.HitTest(location);
        if (hit.Item?.Tag is not ResultRow row || hit.SubItem is null)
        {
            SetFileTooltip(null);
            return;
        }

        var subItemIndex = hit.Item.SubItems.IndexOf(hit.SubItem);
        if (subItemIndex != 2)
        {
            SetFileTooltip(null);
            return;
        }

        var availableWidth = Math.Max(1, _list.Columns[2].Width - 14);
        var measuredWidth = TextRenderer.MeasureText(row.FileName, _list.Font,
            new Size(int.MaxValue, 23), TextFormatFlags.SingleLine | TextFormatFlags.NoPadding).Width;

        SetFileTooltip(measuredWidth > availableWidth ? row.FileName : null);
    }

    private void SetFileTooltip(string? text)
    {
        if (string.Equals(_activeTooltipText, text, StringComparison.Ordinal)) return;
        _activeTooltipText = text;
        _fileToolTip.SetToolTip(_list, text ?? string.Empty);
    }

    private void ShowDetails(ResultRow row)
    {
        var time = (row.CreatedAt ?? DateTime.Now).ToString("yyyy/MM/dd HH:mm:ss");
        MessageBox.Show(this,
            $"時間：{time}\n參考編號：{row.Reference}\n檔案：{row.FileName}\n結果：{row.Status}\n\n{row.Detail}",
            _category == "建立" ? "建立紀錄詳細資料" : "驗證紀錄詳細資料",
            MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    protected override void Dispose(bool disposing)
    {
        _disposed = true;
        if (disposing)
        {
            _fileToolTip.Dispose();
            _rowHeightImages.Dispose();
        }
        base.Dispose(disposing);
    }
}
