using System.Drawing;

namespace WinDesktopHarness;

internal sealed class HistoryForm : Form
{
    public HistoryForm(IReadOnlyList<ResultRow> rows)
    {
        Text = "紀錄";
        StartPosition = FormStartPosition.CenterParent;
        ClientSize = new Size(780, 500);
        MinimumSize = new Size(680, 430);
        BackColor = UiTheme.Window;
        Font = UiTheme.Font();
        ShowInTaskbar = false;

        var title = UiTheme.Label("紀錄", 15f, UiTheme.Text, FontStyle.Bold);
        title.Location = new Point(22, 18);
        Controls.Add(title);

        var sub = UiTheme.Label("依類型分開瀏覽；正式版本會顯示長期保存的建立與驗證紀錄。", 9.1f, UiTheme.Muted);
        sub.Location = new Point(23, 49);
        Controls.Add(sub);

        var tabs = new TabControl
        {
            Left = 20,
            Top = 78,
            Width = 740,
            Height = 356,
            Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
            Font = UiTheme.Font(9.6f)
        };
        Controls.Add(tabs);

        var createdRows = rows.Where(x => x.Category == "建立").Reverse().ToList();
        var verifiedRows = rows.Where(x => x.Category == "驗證").Reverse().ToList();

        tabs.TabPages.Add(BuildPage($"建立紀錄  {createdRows.Count}", createdRows, "尚無建立紀錄"));
        tabs.TabPages.Add(BuildPage($"驗證紀錄  {verifiedRows.Count}", verifiedRows, "尚無驗證紀錄"));

        var close = UiTheme.Button("關閉", true);
        close.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
        close.Left = ClientSize.Width - 130;
        close.Top = ClientSize.Height - 50;
        close.Width = 108;
        close.Click += (_, _) => Close();
        Controls.Add(close);
    }

    private static TabPage BuildPage(string title, IReadOnlyList<ResultRow> rows, string emptyText)
    {
        var page = new TabPage(title)
        {
            BackColor = UiTheme.Surface,
            Padding = new Padding(0)
        };

        if (rows.Count == 0)
        {
            var empty = UiTheme.Label(emptyText, 10f, UiTheme.Muted);
            empty.Location = new Point(22, 24);
            page.Controls.Add(empty);
            return page;
        }

        var grid = new DataGridView
        {
            Dock = DockStyle.Fill,
            BackgroundColor = UiTheme.Surface,
            BorderStyle = BorderStyle.None,
            ReadOnly = true,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            AllowUserToResizeRows = false,
            RowHeadersVisible = false,
            AutoGenerateColumns = false,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            MultiSelect = false,
            Font = UiTheme.Font(9.3f),
            GridColor = Color.FromArgb(232, 235, 240),
            ColumnHeadersHeight = 38,
            RowTemplate = { Height = 36 }
        };
        grid.EnableHeadersVisualStyles = false;
        grid.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(246, 248, 250);
        grid.ColumnHeadersDefaultCellStyle.ForeColor = UiTheme.Text;
        grid.ColumnHeadersDefaultCellStyle.Font = UiTheme.Font(9.2f, FontStyle.Bold);
        grid.ColumnHeadersDefaultCellStyle.SelectionBackColor = Color.FromArgb(246, 248, 250);
        grid.DefaultCellStyle.BackColor = UiTheme.Surface;
        grid.DefaultCellStyle.ForeColor = UiTheme.Text;
        grid.DefaultCellStyle.SelectionBackColor = Color.FromArgb(227, 235, 245);
        grid.DefaultCellStyle.SelectionForeColor = UiTheme.Text;
        grid.DefaultCellStyle.Padding = new Padding(5, 0, 5, 0);

        grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "檔案",
            Width = 260,
            SortMode = DataGridViewColumnSortMode.NotSortable
        });
        grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "結果",
            Width = 118,
            SortMode = DataGridViewColumnSortMode.NotSortable
        });
        grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "摘要",
            AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill,
            SortMode = DataGridViewColumnSortMode.NotSortable
        });

        foreach (var row in rows)
            grid.Rows.Add(row.FileName, row.Status, row.Detail);

        page.Controls.Add(grid);
        return page;
    }
}
