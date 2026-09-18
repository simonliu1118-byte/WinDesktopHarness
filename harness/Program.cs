namespace WinDesktopHarness;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        ApplicationConfiguration.Initialize();
        var form = new MainForm();
        UiTheme.EnlargeTabs(form);
        foreach (Control control in form.Controls)
        {
            if (control is TabControl tabs && tabs.TabPages.Count > 0)
            {
                tabs.TabPages[0].Text = "操作";
                break;
            }
        }
        Application.Run(form);
    }
}
