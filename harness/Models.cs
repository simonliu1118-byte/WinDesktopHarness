namespace WinDesktopHarness;

internal sealed record ResultRow(string FileName, string Status, string Detail);

internal sealed class AppPreferences
{
    public string Density { get; set; } = "標準";
    public bool NotifyOnComplete { get; set; } = true;
    public bool OpenFolderAfterExport { get; set; } = false;
}
