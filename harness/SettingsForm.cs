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

        StartPosition = FormStartPosition.CenterParent;
        ClientSize = new Size(430, 374);
        BackColor = UiTheme.Surface;
        Font = UiTheme.Font();
        FormBorderStyle = FormBorderStyle.None;
        ShowInTaskbar = false;

        const int labelX = 24;
        const int controlX = 118;
        const int right = 404;

        var title = UiTheme.Label("設定", 15f, UiTheme.Text, FontStyle.Bold);
        title.Location = new Point(labelX, 20);
        Controls.Add(title);

        var subtitle = UiTheme.Label("調整檔案輸出方式與一般操作偏好。", 9.1f, UiTheme.Muted);
        subtitle.Location = new Point(labelX, 52);
        Controls.Add(subtitle);

        var headerSeparator = new Panel
        {
            Left = labelX,
            Top = 78,
            Width = right - labelX,
            Height = 1,
            BackColor = UiTheme.Border
        };
        Controls.Add(headerSeparator);

        var sectionTitle = UiTheme.Label("輸出設定", 11f, UiTheme.Text, FontStyle.Bold);
        sectionTitle.Location = new Point(labelX, 94);
        Controls.Add(sectionTitle);

        AddRowLabel(this, "儲存方式", labelX, 136);
        _outputMode.DropDownStyle = ComboBoxStyle.DropDownList;
        _outputMode.Items.AddRange(new object[] { "來源旁邊", "固定資料夾", "每批詢問" });
        _outputMode.SelectedItem = Preferences.OutputMode;
        if (_outputMode.SelectedIndex < 0) _outputMode.SelectedIndex = 0;
        _outputMode.Location = new Point(controlX, 130);
        _outputMode.Size = new Size(right - controlX, 28);
        _outputMode.Font = UiTheme.Font(9.8f);
        _outputMode.SelectedIndexChanged += (_, _) => RefreshFolderState();
        Controls.Add(_outputMode);

        AddRowLabel(this, "固定資料夾", labelX, 180);
        _fixedFolder.Text = Preferences.FixedFolder;
        _fixedFolder.Location = new Point(controlX, 174);
        _fixedFolder.Size = new Size(194, 28);
        _fixedFolder.Font = UiTheme.Font(9.8f);
        Controls.Add(_fixedFolder);

        _browse.Text = "瀏覽…";
        _browse.Location = new Point(320, 174);
        _browse.Size = new Size(84, _fixedFolder.Height);
        _browse.FlatStyle = FlatStyle.Flat;
        _browse.BackColor = UiTheme.Surface;
        _browse.ForeColor = UiTheme.Text;
        _browse.FlatAppearance.BorderColor = UiTheme.Border;
        _browse.FlatAppearance.BorderSize = 1;
        _browse.Font = UiTheme.Font(9.2f);
        _browse.Click += (_, _) => BrowseFolder();
        Controls.Add(_browse);

        AddRowLabel(this, "輸出格式", labelX, 224);
        _format.DropDownStyle = ComboBoxStyle.DropDownList;
        _format.Items.AddRange(new object[] { "PNG", "JPG" });
        _format.SelectedItem = Preferences.OutputFormat;
        if (_format.SelectedIndex < 0) _format.SelectedIndex = 0;
        _format.Location = new Point(controlX, 218);
        _format.Size = new Size(132, 28);
        _format.Font = UiTheme.Font(9.8f);
        Controls.Add(_format);

        var modeHelp = UiTheme.Label("每批詢問：開始工作前先選一次輸出資料夾。", 8.4f, UiTheme.Muted);
        modeHelp.Location = new Point(controlX, 255);
        modeHelp.MaximumSize = new Size(286, 0);
        Controls.Add(modeHelp);

        var separator = new Panel
        {
            Left = labelX,
            Top = 288,
            Width = right - labelX,
            Height = 1,
            BackColor = UiTheme.Border
        };
        Controls.Add(separator);

        _notify.Text = "工作完成時顯示完成提示";
        _notify.Checked = Preferences.NotifyOnComplete;
        _notify.AutoSize = true;
        _notify.Location = new Point(labelX, 306);
        _notify.Font = UiTheme.Font(9.4f);
        _notify.BackColor = Color.Transparent;
        Controls.Add(_notify);

        var cancel = UiTheme.Button("取消");
        cancel.Left = 190;
        cancel.Top = 332;
        cancel.Width = 100;
        cancel.Click += (_, _) => DialogResult = DialogResult.Cancel;
        Controls.Add(cancel);

        var save = UiTheme.Button("儲存", true);
        save.Left = 304;
        save.Top = 332;
        save.Width = 100;
        save.Click += (_, _) => SaveAndClose();
        Controls.Add(save);

        AcceptButton = save;
        CancelButton = cancel;
        RefreshFolderState();
    }

    private static void AddRowLabel(Control parent, string text, int x, int y)
    {
        var label = UiTheme.Label(text, 9.3f, UiTheme.Text, FontStyle.Bold);
        label.Location = new Point(x, y);
        parent.Controls.Add(label);
    }

    private void RefreshFolderState()
    {
        var fixedMode = string.Equals(_outputMode.SelectedItem?.ToString(), "固定資料夾", StringComparison.Ordinal);
        _fixedFolder.Enabled = fixedMode;
        _browse.Enabled = fixedMode;
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

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        using var pen = new Pen(UiTheme.Border);
        e.Graphics.DrawRectangle(pen, 0, 0, ClientSize.Width - 1, ClientSize.Height - 1);
    }
}
