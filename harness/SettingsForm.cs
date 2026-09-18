using System.Drawing;

namespace WinDesktopHarness;

internal sealed class SettingsForm : Form
{
    private readonly ComboBox _density = new();
    private readonly CheckBox _notify = new();
    private readonly CheckBox _openFolder = new();

    public AppPreferences Preferences { get; }

    public SettingsForm(AppPreferences current)
    {
        Preferences = new AppPreferences
        {
            Density = current.Density,
            NotifyOnComplete = current.NotifyOnComplete,
            OpenFolderAfterExport = current.OpenFolderAfterExport
        };

        Text = "設定";
        StartPosition = FormStartPosition.CenterParent;
        ClientSize = new Size(520, 310);
        MinimumSize = new Size(520, 310);
        MaximumSize = new Size(520, 310);
        BackColor = UiTheme.Window;
        Font = UiTheme.Font();
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        ShowInTaskbar = false;

        var card = new Panel
        {
            Left = 18,
            Top = 18,
            Width = 484,
            Height = 214,
            BackColor = UiTheme.Surface,
            Padding = new Padding(20)
        };
        Controls.Add(card);

        var title = UiTheme.Label("介面測試設定", 13f, UiTheme.Text, FontStyle.Bold);
        title.Location = new Point(20, 18);
        card.Controls.Add(title);

        var help = UiTheme.Label("這些選項只用來測試設定視窗的資訊層級與操作手感。", 9f, UiTheme.Muted);
        help.Location = new Point(20, 48);
        card.Controls.Add(help);

        var densityLabel = UiTheme.Label("介面密度", 9.5f);
        densityLabel.Location = new Point(20, 90);
        card.Controls.Add(densityLabel);

        _density.DropDownStyle = ComboBoxStyle.DropDownList;
        _density.Items.AddRange(new object[] { "寬鬆", "標準", "緊湊" });
        _density.SelectedItem = Preferences.Density;
        _density.Location = new Point(145, 86);
        _density.Width = 170;
        _density.Font = UiTheme.Font(9.5f);
        card.Controls.Add(_density);

        _notify.Text = "工作完成時顯示通知";
        _notify.Checked = Preferences.NotifyOnComplete;
        _notify.AutoSize = true;
        _notify.Location = new Point(145, 128);
        _notify.Font = UiTheme.Font(9.5f);
        card.Controls.Add(_notify);

        _openFolder.Text = "匯出後顯示完成訊息";
        _openFolder.Checked = Preferences.OpenFolderAfterExport;
        _openFolder.AutoSize = true;
        _openFolder.Location = new Point(145, 160);
        _openFolder.Font = UiTheme.Font(9.5f);
        card.Controls.Add(_openFolder);

        var cancel = UiTheme.Button("取消");
        cancel.Left = 274;
        cancel.Top = 250;
        cancel.Click += (_, _) => DialogResult = DialogResult.Cancel;
        Controls.Add(cancel);

        var save = UiTheme.Button("儲存", true);
        save.Left = 394;
        save.Top = 250;
        save.Click += (_, _) =>
        {
            Preferences.Density = _density.SelectedItem?.ToString() ?? "標準";
            Preferences.NotifyOnComplete = _notify.Checked;
            Preferences.OpenFolderAfterExport = _openFolder.Checked;
            DialogResult = DialogResult.OK;
        };
        Controls.Add(save);

        AcceptButton = save;
        CancelButton = cancel;
    }
}
