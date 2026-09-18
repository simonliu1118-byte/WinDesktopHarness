using System.Drawing;

namespace WinDesktopHarness;

internal sealed class MainForm : Form
{
    private readonly Button _pickButton;
    private readonly Button _startButton;
    private readonly Button _inspectButton;
    private readonly Button _exportButton;
    private readonly Button _historyButton;
    private readonly Button _settingsButton;
    private readonly DataGridView _grid;
    private readonly Label _statusLabel;
    private readonly ProgressBar _progress;
    private readonly Label _pendingValue;
    private readonly Label _doneValue;
    private readonly Label _failedValue;

    private readonly List<string> _selectedFiles = new();
    private readonly List<ResultRow> _results = new();
    private CancellationTokenSource? _workCancellation;
    private bool _closeAfterWork;
    private AppPreferences _preferences = new();

    public MainForm()
    {
        Text = "Desktop Workflow Preview";
        StartPosition = FormStartPosition.CenterScreen;
        ClientSize = new Size(1040, 720);
        MinimumSize = new Size(900, 620);
        BackColor = UiTheme.Window;
        Font = UiTheme.Font();
        AllowDrop = true;
        AutoScaleMode = AutoScaleMode.Dpi;

        var header = new Panel
        {
            Dock = DockStyle.Top,
            Height = 72,
            BackColor = UiTheme.Surface,
            Padding = new Padding(22, 15, 22, 10)
        };
        Controls.Add(header);

        var title = UiTheme.Label("桌面工作流程預覽", 16f, UiTheme.Text, FontStyle.Bold);
        title.Location = new Point(22, 13);
        header.Controls.Add(title);

        var subtitle = UiTheme.Label("用假資料測試操作流程、批次狀態、結果呈現與文件匯出。", 9.2f, UiTheme.Muted);
        subtitle.Location = new Point(23, 43);
        header.Controls.Add(subtitle);

        var toolbar = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 58,
            Padding = new Padding(20, 11, 20, 8),
            BackColor = UiTheme.Window,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false
        };
        Controls.Add(toolbar);
        toolbar.BringToFront();

        _pickButton = UiTheme.Button("選擇檔案…");
        _startButton = UiTheme.Button("開始處理", true);
        _inspectButton = UiTheme.Button("檢查檔案…");
        _exportButton = UiTheme.Button("匯出 PDF…");
        _exportButton.Width = 118;
        _historyButton = UiTheme.Button("紀錄");
        _historyButton.Width = 88;
        _settingsButton = UiTheme.Button("設定…");
        _settingsButton.Width = 88;
        toolbar.Controls.AddRange(new Control[]
        {
            _pickButton, _startButton, _inspectButton, _exportButton, _historyButton, _settingsButton
        });

        var body = new Panel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(20, 0, 20, 16),
            BackColor = UiTheme.Window
        };
        Controls.Add(body);
        body.BringToFront();

        var summary = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 84,
            ColumnCount = 3,
            RowCount = 1,
            BackColor = UiTheme.Window,
            Padding = new Padding(0, 0, 0, 10)
        };
        summary.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.333f));
        summary.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.333f));
        summary.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.333f));
        body.Controls.Add(summary);

        _pendingValue = AddSummaryCard(summary, 0, "待處理", "0");
        _doneValue = AddSummaryCard(summary, 1, "完成", "0");
        _failedValue = AddSummaryCard(summary, 2, "失敗", "0");

        var resultCard = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = UiTheme.Surface,
            Padding = new Padding(0)
        };
        body.Controls.Add(resultCard);
        resultCard.BringToFront();

        var gridTitleBar = new Panel
        {
            Dock = DockStyle.Top,
            Height = 48,
            BackColor = UiTheme.Surface,
            Padding = new Padding(14, 14, 14, 8)
        };
        resultCard.Controls.Add(gridTitleBar);
        var gridTitle = UiTheme.Label("本次結果", 11f, UiTheme.Text, FontStyle.Bold);
        gridTitle.Location = new Point(14, 14);
        gridTitleBar.Controls.Add(gridTitle);

        _grid = BuildGrid();
        resultCard.Controls.Add(_grid);
        _grid.BringToFront();

        var footer = new Panel
        {
            Dock = DockStyle.Bottom,
            Height = 62,
            BackColor = UiTheme.Window,
            Padding = new Padding(2, 8, 2, 0)
        };
        body.Controls.Add(footer);
        footer.BringToFront();

        _statusLabel = UiTheme.Label("請選擇檔案，或直接拖放檔案／資料夾到視窗。", 9.2f, UiTheme.Muted);
        _statusLabel.AutoEllipsis = true;
        _statusLabel.Dock = DockStyle.Top;
        _statusLabel.Height = 26;
        footer.Controls.Add(_statusLabel);

        _progress = new ProgressBar
        {
            Dock = DockStyle.Bottom,
            Height = 12,
            Style = ProgressBarStyle.Continuous,
            Minimum = 0,
            Maximum = 1,
            Value = 0
        };
        footer.Controls.Add(_progress);

        _pickButton.Click += (_, _) => PickFiles();
        _startButton.Click += async (_, _) => await StartOrCancelAsync();
        _inspectButton.Click += (_, _) => InspectOneFile();
        _exportButton.Click += (_, _) => ExportPdf();
        _historyButton.Click += (_, _) => ShowHistory();
        _settingsButton.Click += (_, _) => ShowSettings();
        DragEnter += OnDragEnter;
        DragDrop += OnDragDrop;
        FormClosing += OnFormClosing;

        UpdateActionState();
    }

    private Label AddSummaryCard(TableLayoutPanel parent, int column, string caption, string value)
    {
        var card = new Panel
        {
            Dock = DockStyle.Fill,
            Margin = new Padding(column == 0 ? 0 : 6, 0, column == 2 ? 0 : 6, 0),
            BackColor = UiTheme.Surface,
            Padding = new Padding(14, 10, 14, 8)
        };
        parent.Controls.Add(card, column, 0);

        var captionLabel = UiTheme.Label(caption, 8.7f, UiTheme.Muted);
        captionLabel.Location = new Point(14, 10);
        card.Controls.Add(captionLabel);

        var valueLabel = UiTheme.Label(value, 19f, UiTheme.Text, FontStyle.Bold);
        valueLabel.Location = new Point(12, 31);
        card.Controls.Add(valueLabel);
        return valueLabel;
    }

    private static DataGridView BuildGrid()
    {
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
            Font = UiTheme.Font(9.5f),
            GridColor = Color.FromArgb(234, 236, 240),
            ColumnHeadersHeight = 38,
            RowTemplate = { Height = 36 }
        };
        grid.EnableHeadersVisualStyles = false;
        grid.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(246, 248, 250);
        grid.ColumnHeadersDefaultCellStyle.ForeColor = UiTheme.Text;
        grid.ColumnHeadersDefaultCellStyle.Font = UiTheme.Font(9.3f, FontStyle.Bold);
        grid.ColumnHeadersDefaultCellStyle.SelectionBackColor = Color.FromArgb(246, 248, 250);
        grid.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.Single;
        grid.DefaultCellStyle.BackColor = UiTheme.Surface;
        grid.DefaultCellStyle.ForeColor = UiTheme.Text;
        grid.DefaultCellStyle.SelectionBackColor = Color.FromArgb(227, 235, 245);
        grid.DefaultCellStyle.SelectionForeColor = UiTheme.Text;
        grid.DefaultCellStyle.Padding = new Padding(4, 0, 4, 0);
        grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "檔案",
            Width = 260,
            SortMode = DataGridViewColumnSortMode.NotSortable
        });
        grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "狀態",
            Width = 105,
            SortMode = DataGridViewColumnSortMode.NotSortable
        });
        grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "說明",
            AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill,
            SortMode = DataGridViewColumnSortMode.NotSortable
        });
        return grid;
    }

    private void PickFiles()
    {
        if (_workCancellation is not null) return;
        using var dialog = new OpenFileDialog
        {
            Title = "選擇檔案",
            Filter = "所有檔案 (*.*)|*.*",
            Multiselect = true,
            CheckFileExists = true
        };
        if (dialog.ShowDialog(this) != DialogResult.OK) return;
        SetSelectedFiles(dialog.FileNames);
    }

    private void SetSelectedFiles(IEnumerable<string> paths)
    {
        _selectedFiles.Clear();
        _selectedFiles.AddRange(paths.Where(File.Exists).Distinct(StringComparer.OrdinalIgnoreCase));
        _results.Clear();
        _grid.Rows.Clear();
        UpdateSummary();
        _statusLabel.Text = _selectedFiles.Count == 0
            ? "沒有可用的檔案。"
            : $"已選擇 {_selectedFiles.Count} 個檔案。可按「開始處理」。";
        UpdateActionState();
    }

    private async Task StartOrCancelAsync()
    {
        if (_workCancellation is not null)
        {
            _workCancellation.Cancel();
            _startButton.Enabled = false;
            _statusLabel.Text = "正在停止…";
            return;
        }
        if (_selectedFiles.Count == 0) return;

        _workCancellation = new CancellationTokenSource();
        var token = _workCancellation.Token;
        _results.Clear();
        _grid.Rows.Clear();
        _progress.Minimum = 0;
        _progress.Maximum = Math.Max(1, _selectedFiles.Count);
        _progress.Value = 0;
        _statusLabel.Text = "正在處理…";
        UpdateSummary();
        UpdateActionState();

        try
        {
            for (var index = 0; index < _selectedFiles.Count; index++)
            {
                token.ThrowIfCancellationRequested();
                await Task.Delay(420, token);

                var fileName = Path.GetFileName(_selectedFiles[index]);
                var simulatedFailure = fileName.Contains("fail", StringComparison.OrdinalIgnoreCase);
                var row = simulatedFailure
                    ? new ResultRow(fileName, "失敗", "這是用於測試錯誤呈現的模擬結果。")
                    : new ResultRow(fileName, "完成", "模擬處理完成。");
                _results.Add(row);
                _grid.Rows.Add(row.FileName, row.Status, row.Detail);
                _progress.Value = index + 1;
                _statusLabel.Text = $"正在處理… {index + 1} / {_selectedFiles.Count}";
                UpdateSummary();
            }

            _statusLabel.Text = $"處理完成，共 {_results.Count} 筆結果。";
            if (_preferences.NotifyOnComplete)
            {
                Text = "Desktop Workflow Preview — 完成";
            }
        }
        catch (OperationCanceledException)
        {
            _statusLabel.Text = "已取消。已完成的模擬結果仍保留在清單中。";
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

    private void InspectOneFile()
    {
        using var dialog = new OpenFileDialog
        {
            Title = "檢查檔案",
            Filter = "所有檔案 (*.*)|*.*",
            Multiselect = false,
            CheckFileExists = true
        };
        if (dialog.ShowDialog(this) != DialogResult.OK) return;

        using var result = new Form
        {
            Text = "檢查結果",
            StartPosition = FormStartPosition.CenterParent,
            ClientSize = new Size(520, 300),
            FormBorderStyle = FormBorderStyle.FixedDialog,
            MaximizeBox = false,
            MinimizeBox = false,
            ShowInTaskbar = false,
            BackColor = UiTheme.Window,
            Font = UiTheme.Font()
        };

        var card = new Panel
        {
            Left = 18,
            Top = 18,
            Width = 484,
            Height = 210,
            BackColor = UiTheme.Surface,
            Padding = new Padding(20)
        };
        result.Controls.Add(card);
        var heading = UiTheme.Label("檢查完成", 14f, UiTheme.Text, FontStyle.Bold);
        heading.Location = new Point(20, 18);
        card.Controls.Add(heading);
        var name = UiTheme.Label(Path.GetFileName(dialog.FileName), 10f, UiTheme.Text);
        name.Location = new Point(20, 59);
        name.MaximumSize = new Size(440, 0);
        card.Controls.Add(name);
        var status = UiTheme.Label("模擬狀態：正常", 11f, UiTheme.Success, FontStyle.Bold);
        status.Location = new Point(20, 99);
        card.Controls.Add(status);
        var note = UiTheme.Label("此結果只用於測試資訊層級、文字長度與操作流程。", 9.2f, UiTheme.Muted);
        note.Location = new Point(20, 142);
        note.MaximumSize = new Size(440, 0);
        card.Controls.Add(note);
        var close = UiTheme.Button("關閉", true);
        close.Left = 394;
        close.Top = 246;
        close.Click += (_, _) => result.Close();
        result.Controls.Add(close);
        result.AcceptButton = close;
        result.ShowDialog(this);
    }

    private void ExportPdf()
    {
        using var dialog = new SaveFileDialog
        {
            Title = "匯出 PDF",
            Filter = "PDF 文件 (*.pdf)|*.pdf",
            DefaultExt = "pdf",
            AddExtension = true,
            FileName = "DesktopHarness_Report.pdf",
            OverwritePrompt = true
        };
        if (dialog.ShowDialog(this) != DialogResult.OK) return;

        try
        {
            PdfExporter.Write(dialog.FileName, _results);
            MessageBox.Show(this, $"PDF 已儲存：\n{dialog.FileName}", "匯出完成",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, $"PDF 匯出失敗：\n{ex.Message}", "無法匯出",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    private void ShowHistory()
    {
        using var dialog = new HistoryForm(_results);
        dialog.ShowDialog(this);
    }

    private void ShowSettings()
    {
        using var dialog = new SettingsForm(_preferences);
        if (dialog.ShowDialog(this) != DialogResult.OK) return;
        _preferences = dialog.Preferences;
        _statusLabel.Text = "設定已儲存，下一次操作生效。";
    }

    private void UpdateSummary()
    {
        var done = _results.Count(r => r.Status == "完成");
        var failed = _results.Count(r => r.Status == "失敗");
        var pending = Math.Max(0, _selectedFiles.Count - _results.Count);
        _pendingValue.Text = pending.ToString();
        _doneValue.Text = done.ToString();
        _failedValue.Text = failed.ToString();
        _doneValue.ForeColor = done > 0 ? UiTheme.Success : UiTheme.Text;
        _failedValue.ForeColor = failed > 0 ? UiTheme.Danger : UiTheme.Text;
    }

    private void UpdateActionState()
    {
        var working = _workCancellation is not null;
        _pickButton.Enabled = !working;
        _inspectButton.Enabled = !working;
        _exportButton.Enabled = !working;
        _historyButton.Enabled = !working;
        _settingsButton.Enabled = !working;
        _startButton.Enabled = working || _selectedFiles.Count > 0;
        _startButton.Text = working ? "取消" : "開始處理";
    }

    private void OnDragEnter(object? sender, DragEventArgs e)
    {
        e.Effect = _workCancellation is null && e.Data?.GetDataPresent(DataFormats.FileDrop) == true
            ? DragDropEffects.Copy
            : DragDropEffects.None;
    }

    private void OnDragDrop(object? sender, DragEventArgs e)
    {
        if (_workCancellation is not null) return;
        if (e.Data?.GetData(DataFormats.FileDrop) is not string[] dropped) return;

        var files = new List<string>();
        foreach (var path in dropped)
        {
            if (File.Exists(path))
            {
                files.Add(path);
                continue;
            }
            if (Directory.Exists(path))
            {
                try
                {
                    files.AddRange(Directory.EnumerateFiles(path, "*", SearchOption.TopDirectoryOnly));
                }
                catch
                {
                    // A folder that cannot be enumerated is simply ignored in this UI harness.
                }
            }
        }
        SetSelectedFiles(files);
    }

    private void OnFormClosing(object? sender, FormClosingEventArgs e)
    {
        if (_workCancellation is null) return;
        e.Cancel = true;
        _closeAfterWork = true;
        _workCancellation.Cancel();
        _startButton.Enabled = false;
        _statusLabel.Text = "正在停止… 完成收尾後會關閉視窗。";
    }
}
