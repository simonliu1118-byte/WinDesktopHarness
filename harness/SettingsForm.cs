using System.Drawing;

namespace WinDesktopHarness;

internal sealed class SettingsForm : Form
{
    private readonly ComboBox _outputMode = new();
    private readonly TextBox _fixedFolder = new();
    private readonly Button _browse = new();
    private readonly ComboBox _format = new();
    private readonly CheckBox _notify = new();

    public AppPreferences Preferences { get; }

    public SettingsForm(AppPreferences current)
    {
        Preferences = new AppPreferences
        {
            OutputMode = current.OutputMode,
            FixedFolder = current.FixedFolder,
            OutputFormat = current.OutputFormat,
            NotifyOnComplete = current.NotifyOnComplete
        };

        Text = "設定";
        StartPosition = FormStartPosition.CenterParent;
        ClientSize = new Size(590, 410);
        MinimumSize = new Size(590, 410);
        MaximumSize = new Size(590, 410);
        BackColor = UiTheme.Window;
        Font = UiTheme.Font();
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        ShowInTaskbar = false;

        var title = UiTheme.Label("設定", 15f, UiTheme.Text, FontStyle.Bold);
        title.Location = new Point(24, 20);
        Controls.Add(title);

        var subtitle = UiTheme.Label("調整檔案輸出方式與一般操作偏好。", 9.3f, UiTheme.Muted);
        subtitle.Location = new Point(25, 50);
        Controls.Add(subtitle);

        var outputCard = new Panel
        {
            Left = 22,
            Top = 82,
            Width = 546,
            Height = 208,
            BackColor = UiTheme.Surface,
            Padding = new Padding(20)
        };
        Controls.Add(outputCard);

        var outputTitle = UiTheme.Label("輸出設定", 11.2f, UiTheme.Text, FontStyle.Bold);
        outputTitle.Location = new Point(20, 17);
        outputCard.Controls.Add(outputTitle);

        AddRowLabel(outputCard, "儲存方式", 60);
        _outputMode.DropDownStyle = ComboBoxStyle.DropDownList;
        _outputMode.Items.AddRange(new object[] { "來源旁邊", "固定資料夾", "每批詢問" });
        _outputMode.SelectedItem = Preferences.OutputMode;
        if (_outputMode.SelectedIndex < 0) _outputMode.SelectedIndex = 0;
        _outputMode.Location = new Point(152, 56);
        _outputMode.Size = new Size(344, 28);
        _outputMode.Font = UiTheme.Font(10f);
        _outputMode.SelectedIndexChanged += (_, _) => RefreshFolderState();
        outputCard.Controls.Add(_outputMode);

        AddRowLabel(outputCard, "固定資料夾", 104);
        _fixedFolder.Text = Preferences.FixedFolder;
        _fixedFolder.Location = new Point(152, 100);
        _fixedFolder.Size = new Size(258, 28);
        _fixedFolder.Font = UiTheme.Font(10f);
        outputCard.Controls.Add(_fixedFolder);

        _browse.Text = "瀏覽…";
        _browse.Location = new Point(420, 99);
        _browse.Size = new Size(76, 30);
        _browse.FlatStyle = FlatStyle.Flat;
        _browse.BackColor = UiTheme.Surface;
        _browse.ForeColor = UiTheme.Text;
        _browse.FlatAppearance.BorderColor = UiTheme.Border;
        _browse.Font = UiTheme.Font(9.5f);
        _browse.Click += (_, _) => BrowseFolder();
        outputCard.Controls.Add(_browse);

        AddRowLabel(outputCard, "輸出格式", 148);
        _format.DropDownStyle = ComboBoxStyle.DropDownList;
        _format.Items.AddRange(new object[] { "PNG", "JPG" });
        _format.SelectedItem = Preferences.OutputFormat;
        if (_format.SelectedIndex < 0) _format.SelectedIndex = 0;
        _format.Location = new Point(152, 144);
        _format.Size = new Size(160, 28);
        _format.Font = UiTheme.Font(10f);
        outputCard.Controls.Add(_format);

        var modeHelp = UiTheme.Label("「每批詢問」會在開始工作前先選一次輸出資料夾。", 8.7f, UiTheme.Muted);
        modeHelp.Location = new Point(152, 178);
        outputCard.Controls.Add(modeHelp);

        var generalCard = new Panel
        {
            Left = 22,
            Top = 302,
            Width = 546,
            Height = 58,
            BackColor = UiTheme.Surface,
            Padding = new Padding(20, 15, 20, 10)
        };
        Controls.Add(generalCard);

        _notify.Text = "工作完成時顯示完成提示";
        _notify.Checked = Preferences.NotifyOnComplete;
        _notify.AutoSize = true;
        _notify.Location = new Point(20, 18);
        _notify.Font = UiTheme.Font(9.7f);
        generalCard.Controls.Add(_notify);

        var cancel = UiTheme.Button("取消");
        cancel.Left = 338;
        cancel.Top = 372;
        cancel.Width = 104;
        cancel.Click += (_, _) => DialogResult = DialogResult.Cancel;
        Controls.Add(cancel);

        var save = UiTheme.Button("儲存", true);
        save.Left = 454;
        save.Top = 372;
        save.Width = 114;
        save.Click += (_, _) => SaveAndClose();
        Controls.Add(save);

        AcceptButton = save;
        CancelButton = cancel;
        RefreshFolderState();
    }

    private static void AddRowLabel(Control parent, string text, int y)
    {
        var label = UiTheme.Label(text, 9.5f, UiTheme.Text, FontStyle.Bold);
        label.Location = new Point(20, y + 4);
        parent.Controls.Add(label);
    }

    private void RefreshFolderState()
    {
        var fixedMode = string.Equals(_outputMode.SelectedItem?.ToString(), "固定資料夾", StringComparison.Ordinal);
        _fixedFolder.Enabled = fixedMode;
        _browse.Enabled = fixedMode;
        _fixedFolder.BackColor = fixedMode ? Color.White : Color.FromArgb(244, 246, 248);
    }

    private void BrowseFolder()
    {
        using var dialog = new FolderBrowserDialog
        {
            Description = "選擇輸出資料夾",
            UseDescriptionForTitle = true,
            SelectedPath = Directory.Exists(_fixedFolder.Text) ? _fixedFolder.Text : string.Empty,
            ShowNewFolderButton = true
        };
        if (dialog.ShowDialog(this) == DialogResult.OK)
            _fixedFolder.Text = dialog.SelectedPath;
    }

    private void SaveAndClose()
    {
        var mode = _outputMode.SelectedItem?.ToString() ?? "來源旁邊";
        if (mode == "固定資料夾" && string.IsNullOrWhiteSpace(_fixedFolder.Text))
        {
            MessageBox.Show(this, "請先選擇固定輸出資料夾。", "設定未完成",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        Preferences.OutputMode = mode;
        Preferences.FixedFolder = _fixedFolder.Text.Trim();
        Preferences.OutputFormat = _format.SelectedItem?.ToString() ?? "PNG";
        Preferences.NotifyOnComplete = _notify.Checked;
        DialogResult = DialogResult.OK;
    }
}
