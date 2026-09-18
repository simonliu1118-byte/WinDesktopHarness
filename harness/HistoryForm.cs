using System.Drawing;

namespace WinDesktopHarness;

internal sealed class HistoryForm : Form
{
    public HistoryForm(IReadOnlyList<ResultRow> rows)
    {
        Text = "紀錄";
        StartPosition = FormStartPosition.CenterParent;
        ClientSize = new Size(760, 460);
        MinimumSize = new Size(640, 380);
        BackColor = UiTheme.Window;
        Font = UiTheme.Font();
        ShowInTaskbar = false;

        var title = UiTheme.Label("本次工作階段紀錄", 14f, UiTheme.Text, FontStyle.Bold);
        title.Location = new Point(18, 18);
        Controls.Add(title);

        var sub = UiTheme.Label($"共 {rows.Count} 筆模擬結果。此視窗只用於測試資訊瀏覽方式。", 9f, UiTheme.Muted);
        sub.Location = new Point(18, 49);
        Controls.Add(sub);

        var grid = new DataGridView
        {
            Left = 18,
            Top = 82,
            Width = 724,
            Height = 320,
            Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
            BackgroundColor = UiTheme.Surface,
            BorderStyle = BorderStyle.FixedSingle,
            ReadOnly = true,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            AllowUserToResizeRows = false,
            RowHeadersVisible = false,
            AutoGenerateColumns = false,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            MultiSelect = false,
            Font = UiTheme.Font(9.5f)
        };
        grid.EnableHeadersVisualStyles = false;
        grid.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(244, 246, 249);
        grid.ColumnHeadersDefaultCellStyle.ForeColor = UiTheme.Text;
        grid.ColumnHeadersDefaultCellStyle.Font = UiTheme.Font(9.5f, FontStyle.Bold);
        grid.DefaultCellStyle.SelectionBackColor = Color.FromArgb(226, 234, 244);
        grid.DefaultCellStyle.SelectionForeColor = UiTheme.Text;
        grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "檔案", DataPropertyName = "FileName", Width = 250 });
        grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "狀態", DataPropertyName = "Status", Width = 100 });
        grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "說明", DataPropertyName = "Detail", AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill });
        foreach (var row in rows) grid.Rows.Add(row.FileName, row.Status, row.Detail);
        Controls.Add(grid);

        var close = UiTheme.Button("關閉", true);
        close.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
        close.Left = ClientSize.Width - 126;
        close.Top = ClientSize.Height - 48;
        close.Click += (_, _) => Close();
        Controls.Add(close);
    }
}
