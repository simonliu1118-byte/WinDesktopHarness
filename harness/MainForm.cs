using System.Drawing;

namespace WinDesktopHarness;

internal sealed class MainForm : Form
{
    private readonly Button _settingsButton;
    private readonly Button _historyButton;
    private readonly Button _pickGenerateButton;
    private readonly Button _startButton;
    private readonly Button _pickInspectButton;
    private readonly Button _inspectButton;
    private readonly Button _reportButton;
    private readonly Label _generateSelection;
    private readonly Label _outputSummary;
    private readonly Label _generateStatus;
    private readonly Label _generateResultSummary;
    private readonly ProgressBar _progress;
    private readonly Label _inspectSelection;
    private readonly Label _inspectBadge;
    private readonly Label _inspectHeadline;
    private readonly Label _inspectDetail;

    private readonly List<string> _selectedFiles = new();
    private readonly List<ResultRow> _history = new();
    private CancellationTokenSource? _workCancellation;
    private bool _closeAfterWork;
    private string? _inspectionFile;
    private ResultRow? _inspectionResult;
    private AppPreferences _preferences = new();

    public MainForm()
    {
        Text = "Desktop Workflow Preview";
        StartPosition = FormStartPosition.CenterScreen;
        ClientSize = new Size(1000, 760);
        MinimumSize = new Size(900, 700);
        BackColor = UiTheme.Window;
        Font = UiTheme.Font();
        AutoScaleMode = AutoScaleMode.Dpi;

        var header = new Panel
        {
            Dock = DockStyle.Top,
            Height = 78,
            BackColor = UiTheme.Surface
        };
        Controls.Add(header);

        var title = UiTheme.Label("桌面工具預覽", 17f, UiTheme.Text, FontStyle.Bold);
        title.Location = new Point(26, 14);
        header.Controls.Add(title);
        var subtitle = UiTheme.Label("上方建立檔案，下方檢查檔案；主要操作都在同一頁完成。", 9.4f, UiTheme.Muted);
        subtitle.Location = new Point(27, 47);
        header.Controls.Add(subtitle);

        _settingsButton = UiTheme.Button("設定…");
        _settingsButton.Size = new Size(92, 34);
        _settingsButton.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        _settingsButton.Location = new Point(ClientSize.Width - 118, 22);
        header.Controls.Add(_settingsButton);
        _historyButton = UiTheme.Button("紀錄");
        _historyButton.Size = new Size(82, 34);
        _historyButton.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        _historyButton.Location = new Point(ClientSize.Width - 210, 22);
        header.Controls.Add(_historyButton);

        var stack = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            AutoScroll = true,
            BackColor = UiTheme.Window,
            Padding = new Padding(24, 22, 24, 24)
        };
        Controls.Add(stack);
        stack.BringToFront();

        var createCard = new Panel
        {
            Width = 930,
            Height = 286,
            BackColor = UiTheme.Surface,
            Margin = new Padding(0, 0, 0, 18),
            BorderStyle = BorderStyle.FixedSingle
        };
        stack.Controls.Add(createCard);

        var createStep = StepLabel("1");
        createStep.Location = new Point(22, 17);
        createCard.Controls.Add(createStep);
        var createTitle = UiTheme.Label("建立檔案", 13.5f, UiTheme.Text, FontStyle.Bold);
        createTitle.Location = new Point(62, 18);
        createCard.Controls.Add(createTitle);
        var createHelp = UiTheme.Label("可一次選擇多個檔案。完成後保留本批結果與輸出方式摘要。", 9f, UiTheme.Muted);
        createHelp.Location = new Point(63, 47);
        createCard.Controls.Add(createHelp);

        var createDrop = DropPanel();
        createDrop.Location = new Point(22, 82);
        createDrop.Size = new Size(585, 104);
        createDrop.AllowDrop = true;
        createCard.Controls.Add(createDrop);
        var dropTitle = UiTheme.Label("拖放檔案到這裡", 11f, UiTheme.Text, FontStyle.Bold);
        dropTitle.Location = new Point(18, 17);
        createDrop.Controls.Add(dropTitle);
        var dropHelp = UiTheme.Label("或使用右側按鈕選擇檔案", 8.8f, UiTheme.Muted);
        dropHelp.Location = new Point(18, 45);
        createDrop.Controls.Add(dropHelp);
        _generateSelection = UiTheme.Label("尚未選擇檔案", 9.2f, UiTheme.Accent, FontStyle.Bold);
        _generateSelection.Location = new Point(18, 72);
        _generateSelection.AutoSize = false;
        _generateSelection.AutoEllipsis = true;
        _generateSelection.Size = new Size(390, 23);
        createDrop.Controls.Add(_generateSelection);
        _pickGenerateButton = UiTheme.Button("選擇檔案…");
        _pickGenerateButton.Location = new Point(455, 49);
        _pickGenerateButton.Size = new Size(112, 36);
        createDrop.Controls.Add(_pickGenerateButton);

        var outputCard = new Panel
        {
            Location = new Point(621, 82),
            Size = new Size(286, 104),
            BackColor = Color.FromArgb(247, 249, 252)
        };
        createCard.Controls.Add(outputCard);
        var outputTitle = UiTheme.Label("輸出方式", 9.2f, UiTheme.Muted, FontStyle.Bold);
        outputTitle.Location = new Point(16, 14);
        outputCard.Controls.Add(outputTitle);
        _outputSummary = UiTheme.Label("來源旁邊 · PNG", 10.2f, UiTheme.Text, FontStyle.Bold);
        _outputSummary.Location = new Point(16, 42);
        _outputSummary.MaximumSize = new Size(250, 0);
        outputCard.Controls.Add(_outputSummary);
        var outputHint = UiTheme.Label("可從右上角「設定」調整", 8.5f, UiTheme.Muted);
        outputHint.Location = new Point(16, 74);
        outputCard.Controls.Add(outputHint);

        _generateStatus = UiTheme.Label("選擇檔案後即可開始。", 9.2f, UiTheme.Muted);
        _generateStatus.Location = new Point(22, 203);
        _generateStatus.AutoSize = false;
        _generateStatus.AutoEllipsis = true;
        _generateStatus.Size = new Size(500, 22);
        createCard.Controls.Add(_generateStatus);
        _progress = new ProgressBar
        {
            Location = new Point(22, 232),
            Size = new Size(585, 10),
            Minimum = 0,
            Maximum = 1,
            Value = 0,
            Style = ProgressBarStyle.Continuous
        };
        createCard.Controls.Add(_progress);
        _generateResultSummary = UiTheme.Label("尚無本批結果", 8.8f, UiTheme.Muted);
        _generateResultSummary.Location = new Point(22, 250);
        _generateResultSummary.AutoSize = false;
        _generateResultSummary.AutoEllipsis = true;
        _generateResultSummary.Size = new Size(585, 22);
        createCard.Controls.Add(_generateResultSummary);
        _startButton = UiTheme.Button("開始處理", true);
        _startButton.Location = new Point(727, 220);
        _startButton.Size = new Size(180, 46);
        _startButton.Font = UiTheme.Font(10.5f, FontStyle.Bold);
        createCard.Controls.Add(_startButton);

        var inspectCard = new Panel
        {
            Width = 930,
            Height = 330,
            BackColor = UiTheme.Surface,
            Margin = new Padding(0),
            BorderStyle = BorderStyle.FixedSingle
        };
        stack.Controls.Add(inspectCard);
        var inspectStep = StepLabel("2");
        inspectStep.Location = new Point(22, 17);
        inspectCard.Controls.Add(inspectStep);
        var inspectTitle = UiTheme.Label("檢查檔案", 13.5f, UiTheme.Text, FontStyle.Bold);
        inspectTitle.Location = new Point(62, 18);
        inspectCard.Controls.Add(inspectTitle);
        var inspectHelp = UiTheme.Label("一次檢查一個檔案；結果直接顯示在右側，需要時再產出 PDF 報告。", 9f, UiTheme.Muted);
        inspectHelp.Location = new Point(63, 47);
        inspectCard.Controls.Add(inspectHelp);

        var inspectDrop = DropPanel();
        inspectDrop.Location = new Point(22, 82);
        inspectDrop.Size = new Size(360, 166);
        inspectDrop.AllowDrop = true;
        inspectCard.Controls.Add(inspectDrop);
        var inspectDropTitle = UiTheme.Label("拖放一個檔案", 11f, UiTheme.Text, FontStyle.Bold);
        inspectDropTitle.Location = new Point(18, 19);
        inspectDrop.Controls.Add(inspectDropTitle);
        var inspectDropHelp = UiTheme.Label("或從電腦選擇要檢查的檔案", 8.8f, UiTheme.Muted);
        inspectDropHelp.Location = new Point(18, 48);
        inspectDrop.Controls.Add(inspectDropHelp);
        _inspectSelection = UiTheme.Label("尚未選擇", 9.2f, UiTheme.Accent, FontStyle.Bold);
        _inspectSelection.Location = new Point(18, 80);
        _inspectSelection.AutoSize = false;
        _inspectSelection.AutoEllipsis = true;
        _inspectSelection.Size = new Size(320, 23);
        inspectDrop.Controls.Add(_inspectSelection);
        _pickInspectButton = UiTheme.Button("選擇檔案…");
        _pickInspectButton.Location = new Point(18, 116);
        _pickInspectButton.Size = new Size(126, 36);
        inspectDrop.Controls.Add(_pickInspectButton);

        var resultCard = new Panel
        {
            Location = new Point(398, 82),
            Size = new Size(509, 166),
            BackColor = Color.FromArgb(247, 249, 252)
        };
        inspectCard.Controls.Add(resultCard);
        _inspectBadge = new Label
        {
            Text = "尚未檢查",
            AutoSize = false,
            TextAlign = ContentAlignment.MiddleCenter,
            Location = new Point(20, 18),
            Size = new Size(104, 28),
            Font = UiTheme.Font(8.8f, FontStyle.Bold),
            ForeColor = UiTheme.Muted,
            BackColor = Color.FromArgb(235, 238, 242)
        };
        resultCard.Controls.Add(_inspectBadge);
        _inspectHeadline = UiTheme.Label("選擇檔案後開始檢查", 13f, UiTheme.Text, FontStyle.Bold);
        _inspectHeadline.Location = new Point(20, 62);
        _inspectHeadline.MaximumSize = new Size(460, 0);
        resultCard.Controls.Add(_inspectHeadline);
        _inspectDetail = UiTheme.Label("結果、說明與後續操作會集中顯示在這裡。", 9.3f, UiTheme.Muted);
        _inspectDetail.Location = new Point(20, 98);
        _inspectDetail.MaximumSize = new Size(460, 0);
        resultCard.Controls.Add(_inspectDetail);

        _inspectButton = UiTheme.Button("開始檢查", true);
        _inspectButton.Location = new Point(603, 267);
        _inspectButton.Size = new Size(142, 44);
        _inspectButton.Font = UiTheme.Font(10f, FontStyle.Bold);
        inspectCard.Controls.Add(_inspectButton);
        _reportButton = UiTheme.Button("產出 PDF 報告…");
        _reportButton.Location = new Point(757, 267);
        _reportButton.Size = new Size(150, 44);
        inspectCard.Controls.Add(_reportButton);

        stack.ClientSizeChanged += (_, _) =>
        {
            var width = Math.Max(840, stack.ClientSize.Width - stack.Padding.Horizontal - 8);
            createCard.Width = width;
            inspectCard.Width = width;
            LayoutCreateCard(createCard, createDrop, outputCard);
            LayoutInspectCard(inspectCard, inspectDrop, resultCard);
        };

        _pickGenerateButton.Click += (_, _) => PickGenerateFiles();
        _startButton.Click += async (_, _) => await StartOrCancelAsync();
        _pickInspectButton.Click += (_, _) => PickInspectionFile();
        _inspectButton.Click += async (_, _) => await InspectSelectedAsync();
        _reportButton.Click += (_, _) => ExportInspectionReport();
        _settingsButton.Click += (_, _) => ShowSettings();
        _historyButton.Click += (_, _) => ShowHistory();
        AttachDrop(createDrop, multiple: true);
        AttachDrop(inspectDrop, multiple: false);
        FormClosing += OnFormClosing;

        RefreshOutputSummary();
        UpdateActionState();
    }

    private static Label StepLabel(string text) => new()
    {
        Text = text,
        AutoSize = false,
        TextAlign = ContentAlignment.MiddleCenter,
        Size = new Size(30, 30),
        Font = UiTheme.Font(10f, FontStyle.Bold),
        ForeColor = Color.White,
        BackColor = UiTheme.Accent
    };

    private static Panel DropPanel() => new()
    {
        BackColor = Color.FromArgb(250, 251, 253),
        BorderStyle = BorderStyle.FixedSingle,
        Cursor = Cursors.Hand
    };

    private void LayoutCreateCard(Panel card, Panel drop, Panel output)
    {
        const int margin = 22;
        const int gap = 14;
        const int outputWidth = 286;
        output.Left = card.ClientSize.Width - margin - outputWidth;
        output.Width = outputWidth;
        drop.Width = Math.Max(420, output.Left - gap - margin);
        _pickGenerateButton.Left = Math.Max(300, drop.Width - _pickGenerateButton.Width - 18);
        _startButton.Left = card.ClientSize.Width - margin - _startButton.Width;
        _generateStatus.Width = Math.Max(360, _startButton.Left - 36);
        _progress.Width = drop.Width;
        _generateResultSummary.Width = drop.Width;
    }

    private void LayoutInspectCard(Panel card, Panel drop, Panel result)
    {
        const int margin = 22;
        const int gap = 16;
        var leftWidth = Math.Max(330, (int)(card.ClientSize.Width * 0.39));
        drop.Width = leftWidth;
        _inspectSelection.Width = Math.Max(240, leftWidth - 36);
        result.Left = margin + leftWidth + gap;
        result.Width = Math.Max(390, card.ClientSize.Width - result.Left - margin);
        _reportButton.Left = card.ClientSize.Width - margin - _reportButton.Width;
        _inspectButton.Left = _reportButton.Left - 12 - _inspectButton.Width;
    }

    private void AttachDrop(Control target, bool multiple)
    {
        target.DragEnter += (_, e) =>
        {
            if (e.Data?.GetDataPresent(DataFormats.FileDrop) == true)
                e.Effect = DragDropEffects.Copy;
        };
        target.DragDrop += (_, e) =>
        {
            if (e.Data?.GetData(DataFormats.FileDrop) is not string[] dropped || dropped.Length == 0)
                return;
            var files = ExpandDroppedPaths(dropped).ToArray();
            if (files.Length == 0) return;
            if (multiple) SetGenerateFiles(files);
            else SetInspectionFile(files[0]);
        };
    }

    private static IEnumerable<string> ExpandDroppedPaths(IEnumerable<string> paths)
    {
        foreach (var path in paths)
        {
            if (File.Exists(path))
            {
                yield return path;
                continue;
            }
            if (!Directory.Exists(path)) continue;
            IEnumerable<string> files;
            try { files = Directory.EnumerateFiles(path, "*.*", SearchOption.TopDirectoryOnly); }
            catch { continue; }
            foreach (var file in files) yield return file;
        }
    }

    private void PickGenerateFiles()
    {
        if (_workCancellation is not null) return;
        using var dialog = new OpenFileDialog
        {
            Title = "選擇檔案",
            Filter = "所有檔案 (*.*)|*.*",
            Multiselect = true,
            CheckFileExists = true
        };
        if (dialog.ShowDialog(this) == DialogResult.OK)
            SetGenerateFiles(dialog.FileNames);
    }

    private void SetGenerateFiles(IEnumerable<string> paths)
    {
        _selectedFiles.Clear();
        _selectedFiles.AddRange(paths.Where(File.Exists).Distinct(StringComparer.OrdinalIgnoreCase));
        _generateSelection.Text = _selectedFiles.Count switch
        {
            0 => "尚未選擇檔案",
            1 => Path.GetFileName(_selectedFiles[0]),
            _ => $"已選擇 {_selectedFiles.Count} 個檔案"
        };
        _generateStatus.Text = _selectedFiles.Count == 0 ? "選擇檔案後即可開始。" : "已準備好，可以開始處理。";
        _generateResultSummary.Text = "尚無本批結果";
        _progress.Value = 0;
        UpdateActionState();
    }

    private async Task StartOrCancelAsync()
    {
        if (_workCancellation is not null)
        {
            _workCancellation.Cancel();
            _startButton.Enabled = false;
            _generateStatus.Text = "正在停止…";
            return;
        }
        if (_selectedFiles.Count == 0) return;

        if (_preferences.OutputMode == "每批詢問")
        {
            using var folder = new FolderBrowserDialog
            {
                Description = "選擇這一批的輸出資料夾",
                UseDescriptionForTitle = true,
                ShowNewFolderButton = true
            };
            if (folder.ShowDialog(this) != DialogResult.OK)
            {
                _generateStatus.Text = "已取消，尚未開始處理。";
                return;
            }
        }

        _workCancellation = new CancellationTokenSource();
        var token = _workCancellation.Token;
        _progress.Minimum = 0;
        _progress.Maximum = Math.Max(1, _selectedFiles.Count);
        _progress.Value = 0;
        _generateStatus.Text = "正在處理…";
        _generateResultSummary.Text = "本批進行中";
        UpdateActionState();

        var batchResults = new List<ResultRow>();
        try
        {
            for (var index = 0; index < _selectedFiles.Count; index++)
            {
                token.ThrowIfCancellationRequested();
                await Task.Delay(480, token);
                var fileName = Path.GetFileName(_selectedFiles[index]);
                var failed = fileName.Contains("fail", StringComparison.OrdinalIgnoreCase);
                var row = failed
                    ? new ResultRow(fileName, "失敗", "模擬失敗，用於確認錯誤狀態的呈現方式。")
                    : new ResultRow(fileName, "完成", "模擬處理完成。");
                batchResults.Add(row);
                _history.Add(row);
                _progress.Value = index + 1;
                _generateStatus.Text = $"正在處理… {index + 1} / {_selectedFiles.Count}";
                var success = batchResults.Count(x => x.Status == "完成");
                var failure = batchResults.Count - success;
                _generateResultSummary.Text = failure == 0
                    ? $"已完成 {success} 個檔案"
                    : $"完成 {success}，失敗 {failure}";
            }

            _generateStatus.Text = "本批處理完成。";
            if (_preferences.NotifyOnComplete)
                Text = "Desktop Workflow Preview — 完成";
        }
        catch (OperationCanceledException)
        {
            _generateStatus.Text = "已取消；已完成的項目仍保留。";
            var success = batchResults.Count(x => x.Status == "完成");
            var failure = batchResults.Count - success;
            _generateResultSummary.Text = $"取消前完成 {success}，失敗 {failure}";
        }
        finally
        {
            _workCancellation.Dispose();
            _workCancellation = null;
            UpdateActionState();
            if (_closeAfterWork)
            {
                _closeAfterWork = false;
                BeginInvoke(Close);
            }
        }
    }

    private void PickInspectionFile()
    {
        if (_workCancellation is not null) return;
        using var dialog = new OpenFileDialog
        {
            Title = "選擇要檢查的檔案",
            Filter = "所有檔案 (*.*)|*.*",
            Multiselect = false,
            CheckFileExists = true
        };
        if (dialog.ShowDialog(this) == DialogResult.OK)
            SetInspectionFile(dialog.FileName);
    }

    private void SetInspectionFile(string path)
    {
        if (!File.Exists(path)) return;
        _inspectionFile = path;
        _inspectionResult = null;
        _inspectSelection.Text = Path.GetFileName(path);
        SetInspectionIdle("已選擇檔案", "按「開始檢查」取得結果。", UiTheme.Accent);
        UpdateActionState();
    }

    private async Task InspectSelectedAsync()
    {
        if (_inspectionFile is null || _workCancellation is not null) return;
        _inspectButton.Enabled = false;
        _pickInspectButton.Enabled = false;
        _reportButton.Enabled = false;
        _inspectBadge.Text = "檢查中";
        _inspectBadge.ForeColor = UiTheme.Accent;
        _inspectBadge.BackColor = Color.FromArgb(232, 239, 248);
        _inspectHeadline.Text = "正在檢查檔案…";
        _inspectDetail.Text = "請稍候，完成後結果會顯示在這裡。";

        await Task.Delay(650);

        var fileName = Path.GetFileName(_inspectionFile);
        var flagged = fileName.Contains("fail", StringComparison.OrdinalIgnoreCase);
        _inspectionResult = flagged
            ? new ResultRow(fileName, "需注意", "這是模擬結果，用於確認警示資訊的視覺層級。")
            : new ResultRow(fileName, "完成", "這是模擬結果，用於確認結果與報告操作流程。");
        _history.Add(_inspectionResult);

        if (flagged)
        {
            _inspectBadge.Text = "需注意";
            _inspectBadge.ForeColor = UiTheme.Danger;
            _inspectBadge.BackColor = Color.FromArgb(252, 238, 238);
            _inspectHeadline.Text = "檢查完成，結果需要注意";
        }
        else
        {
            _inspectBadge.Text = "完成";
            _inspectBadge.ForeColor = UiTheme.Success;
            _inspectBadge.BackColor = Color.FromArgb(233, 247, 240);
            _inspectHeadline.Text = "檢查完成";
        }
        _inspectDetail.Text = _inspectionResult.Detail;
        UpdateActionState();
    }

    private void ExportInspectionReport()
    {
        if (_inspectionResult is null || _inspectionFile is null) return;
        using var dialog = new SaveFileDialog
        {
            Title = "儲存 PDF 報告",
            Filter = "PDF 文件 (*.pdf)|*.pdf",
            DefaultExt = "pdf",
            AddExtension = true,
            FileName = Path.GetFileNameWithoutExtension(_inspectionFile) + "_report.pdf",
            OverwritePrompt = true
        };
        if (dialog.ShowDialog(this) != DialogResult.OK) return;
        try
        {
            PdfExporter.Write(dialog.FileName, new[] { _inspectionResult });
            MessageBox.Show(this, "PDF 報告已儲存。", "完成",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, $"PDF 匯出失敗：\n{ex.Message}", "無法匯出",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    private void ShowSettings()
    {
        if (_workCancellation is not null) return;
        using var dialog = new SettingsForm(_preferences);
        if (dialog.ShowDialog(this) != DialogResult.OK) return;
        _preferences = dialog.Preferences;
        RefreshOutputSummary();
    }

    private void ShowHistory()
    {
        if (_workCancellation is not null) return;
        using var dialog = new HistoryForm(_history);
        dialog.ShowDialog(this);
    }

    private void RefreshOutputSummary()
    {
        _outputSummary.Text = _preferences.OutputMode switch
        {
            "固定資料夾" => $"固定資料夾 · {_preferences.OutputFormat}",
            "每批詢問" => $"每批詢問 · {_preferences.OutputFormat}",
            _ => $"來源旁邊 · {_preferences.OutputFormat}"
        };
    }

    private void SetInspectionIdle(string headline, string detail, Color color)
    {
        _inspectBadge.Text = "待檢查";
        _inspectBadge.ForeColor = color;
        _inspectBadge.BackColor = Color.FromArgb(235, 240, 247);
        _inspectHeadline.Text = headline;
        _inspectDetail.Text = detail;
    }

    private void UpdateActionState()
    {
        var busy = _workCancellation is not null;
        _startButton.Text = busy ? "取消" : "開始處理";
        _startButton.Enabled = busy || _selectedFiles.Count > 0;
        _pickGenerateButton.Enabled = !busy;
        _settingsButton.Enabled = !busy;
        _historyButton.Enabled = !busy;
        _pickInspectButton.Enabled = !busy;
        _inspectButton.Enabled = !busy && _inspectionFile is not null;
        _reportButton.Enabled = !busy && _inspectionResult is not null;
    }

    private void OnFormClosing(object? sender, FormClosingEventArgs e)
    {
        if (_workCancellation is null) return;
        e.Cancel = true;
        _closeAfterWork = true;
        _workCancellation.Cancel();
        _startButton.Enabled = false;
        _generateStatus.Text = "正在停止…";
    }
}
