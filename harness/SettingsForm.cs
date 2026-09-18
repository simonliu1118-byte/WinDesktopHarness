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
        ClientSize = new Size(430, 418);
        BackColor = UiTheme.Surface;
        Font = UiTheme.Font();
        FormBorderStyle = FormBorderStyle.None;
        ShowInTaskbar = false;

        const int outerLeft = 16;
        const int outerRight = 414;

        var title = UiTheme.Label("設定", 15f, UiTheme.Text, FontStyle.Bold);
        title.Location = new Point(24, 20);
        Controls.Add(title);

        var subtitle = UiTheme.Label("調整檔案輸出方式與一般操作偏好。", 9.1f, UiTheme.Muted);
        subtitle.Location = new Point(24, 52);
        Controls.Add(subtitle);

        var headerSeparator = new Panel
        {
            Left = 24,
            Top = 78,
            Width = 380,
            Height = 1,
            BackColor = UiTheme.Border
        };
        Controls.Add(headerSeparator);

        var outputGroup = new GroupBox
        {
            Text = "輸出設定",
            Left = outerLeft,
            Top = 94,
            Width = outerRight - outerLeft,
            Height = 198,
            Font = UiTheme.Font(10f, FontStyle.Bold),
            ForeColor = UiTheme.Text,
            BackColor = UiTheme.Surface
        };
        Controls.Add(outputGroup);

        const int labelX = 18;
        const int controlX = 100;
        const int rightX = 382;

        AddRowLabel(outputGroup, "儲存方式", labelX, 38);
        _outputMode.DropDownStyle = ComboBoxStyle.DropDownList;
        _outputMode.Items.AddRange(new object[] { "來源旁邊", "固定資料夾", "每批詢問" });
        _outputMode.SelectedItem = Preferences.OutputMode;
        if (_outputMode.SelectedIndex < 0) _outputMode.SelectedIndex = 0;
        _outputMode.Location = new Point(controlX, 32);
        _outputMode.Size = new Size(rightX - controlX, 28);
        _outputMode.Font = UiTheme.Font(9.8f);
        _outputMode.SelectedIndexChanged += (_, _) => RefreshFolderState();
        outputGroup.Controls.Add(_outputMode);

        AddRowLabel(outputGroup, "固定資料夾", labelX, 82);
        _fixedFolder.Text = Preferences.FixedFolder;
        _fixedFolder.Location = new Point(controlX, 76);
        _fixedFolder.Size = new Size(190, 28);
        _fixedFolder.Font = UiTheme.Font(9.8f);
        outputGroup.Controls.Add(_fixedFolder);

        _browse.Text = "瀏覽…";
        _browse.Location = new Point(298, 76);
        _browse.Size = new Size(84, 28);
        _browse.FlatStyle = FlatStyle.Flat;
        _browse.BackColor = UiTheme.Surface;
        _browse.ForeColor = UiTheme.Text;
        _browse.FlatAppearance.BorderColor = UiTheme.Border;
        _browse.FlatAppearance.BorderSize = 1;
        _browse.Font = UiTheme.Font(9.2f);
        _browse.Click += (_, _) => BrowseFolder();
        outputGroup.Controls.Add(_browse);

        AddRowLabel(outputGroup, "輸出格式", labelX, 126);
        _format.DropDownStyle = ComboBoxStyle.DropDownList;
        _format.Items.AddRange(new object[] { "PNG", "JPG" });
        _format.SelectedItem = Preferences.OutputFormat;
        if (_format.SelectedIndex < 0) _format.SelectedIndex = 0;
        _format.Location = new Point(controlX, 120);
        _format.Size = new Size(132, 28);
        _format.Font = UiTheme.Font(9.8f);
        outputGroup.Controls.Add(_format);

        var modeHelp = UiTheme.Label("每批詢問：開始工作前先選一次輸出資料夾。", 8.4f, UiTheme.Muted);
        modeHelp.Location = new Point(controlX, 158);
        modeHelp.MaximumSize = new Size(282, 0);
        outputGroup.Controls.Add(modeHelp);

        var operationGroup = new GroupBox
        {
            Text = "操作設定",
            Left = outerLeft,
            Top = 302,
            Width = outerRight - outerLeft,
            Height = 64,
            Font = UiTheme.Font(10f, FontStyle.Bold),
            ForeColor = UiTheme.Text,
            BackColor = UiTheme.Surface
        };
        Controls.Add(operationGroup);

        _notify.Text = "工作完成時顯示完成提示";
        _notify.Checked = Preferences.NotifyOnComplete;
        _notify.AutoSize = true;
        _notify.Location = new Point(18, 28);
        _notify.Font = UiTheme.Font(9.4f);
        _notify.BackColor = Color.Transparent;
        operationGroup.Controls.Add(_notify);

        var cancel = UiTheme.Button("取消");
        cancel.Left = 190;
        cancel.Top = 378;
        cancel.Width = 100;
        cancel.Click += (_, _) => DialogResult = DialogResult.Cancel;
        Controls.Add(cancel);

        var save = UiTheme.Button("儲存", true);
        save.Left = 304;
        save.Top = 378;
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
