using System.Drawing;

namespace WinDesktopHarness;

internal sealed class HistoryForm : Form
{
    public HistoryForm(IReadOnlyList<ResultRow> rows)
    {
        Text = "紀錄";
        StartPosition = FormStartPosition.CenterParent;
        ClientSize = new Size(720, 520);
        MinimumSize = new Size(620, 420);
        BackColor = UiTheme.Window;
        Font = UiTheme.Font();
        ShowInTaskbar = false;

        var title = UiTheme.Label("近期紀錄", 15f, UiTheme.Text, FontStyle.Bold);
        title.Location = new Point(24, 20);
        Controls.Add(title);

        var sub = UiTheme.Label(rows.Count == 0
            ? "目前沒有紀錄。"
            : $"目前顯示 {rows.Count} 筆測試紀錄。", 9.2f, UiTheme.Muted);
        sub.Location = new Point(25, 52);
        Controls.Add(sub);

        var list = new FlowLayoutPanel
        {
            Left = 22,
            Top = 84,
            Width = 676,
            Height = 374,
            Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
            AutoScroll = true,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            BackColor = UiTheme.Window,
            Padding = new Padding(0, 0, 8, 0)
        };
        Controls.Add(list);

        if (rows.Count == 0)
        {
            var empty = new Panel
            {
                Width = 650,
                Height = 110,
                BackColor = UiTheme.Surface,
                Margin = new Padding(0, 0, 0, 10)
            };
            var emptyTitle = UiTheme.Label("尚無紀錄", 11f, UiTheme.Text, FontStyle.Bold);
            emptyTitle.Location = new Point(18, 22);
            empty.Controls.Add(emptyTitle);
            var emptyText = UiTheme.Label("完成一次工作後，這裡會顯示最近的項目。", 9.2f, UiTheme.Muted);
            emptyText.Location = new Point(18, 54);
            empty.Controls.Add(emptyText);
            list.Controls.Add(empty);
        }
        else
        {
            foreach (var row in rows.Reverse())
                list.Controls.Add(BuildRecordCard(row));
        }

        var close = UiTheme.Button("關閉", true);
        close.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
        close.Left = ClientSize.Width - 132;
        close.Top = ClientSize.Height - 48;
        close.Width = 110;
        close.Click += (_, _) => Close();
        Controls.Add(close);
    }

    private static Control BuildRecordCard(ResultRow row)
    {
        var card = new Panel
        {
            Width = 650,
            Height = 94,
            BackColor = UiTheme.Surface,
            Margin = new Padding(0, 0, 0, 10),
            Padding = new Padding(18, 14, 18, 12)
        };

        var file = UiTheme.Label(row.FileName, 10.4f, UiTheme.Text, FontStyle.Bold);
        file.Location = new Point(18, 15);
        file.MaximumSize = new Size(455, 24);
        file.AutoEllipsis = true;
        file.AutoSize = false;
        file.Size = new Size(455, 24);
        card.Controls.Add(file);

        var status = new Label
        {
            Text = row.Status,
            AutoSize = false,
            TextAlign = ContentAlignment.MiddleCenter,
            Font = UiTheme.Font(8.8f, FontStyle.Bold),
            ForeColor = row.Status == "失敗" ? UiTheme.Danger : UiTheme.Success,
            BackColor = row.Status == "失敗"
                ? Color.FromArgb(252, 238, 238)
                : Color.FromArgb(233, 247, 240),
            Location = new Point(525, 14),
            Size = new Size(92, 26)
        };
        card.Controls.Add(status);

        var detail = UiTheme.Label(row.Detail, 9.1f, UiTheme.Muted);
        detail.Location = new Point(18, 51);
        detail.MaximumSize = new Size(598, 0);
        card.Controls.Add(detail);

        return card;
    }
}
