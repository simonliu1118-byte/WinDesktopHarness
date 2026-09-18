namespace WinDesktopHarness;

internal sealed record ResultRow(
    string FileName,
    string Status,
    string Detail,
    string Category = "建立",
    DateTime? CreatedAt = null,
    string Reference = "");

internal sealed class AppPreferences
{
    // Synthetic UI choices only. These names intentionally describe generic
    // file-output behavior and are not a production data contract.
    public string OutputMode { get; set; } = "來源旁邊";
    public string FixedFolder { get; set; } = string.Empty;
    public string OutputFormat { get; set; } = "PNG";
    public bool NotifyOnComplete { get; set; } = true;
}
