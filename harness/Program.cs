namespace WinDesktopHarness;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        ApplicationConfiguration.Initialize();
        var form = new MainForm();
        UiTheme.EnlargeTabs(form);
        Application.Run(form);
    }
}
