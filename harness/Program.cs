namespace WinDesktopHarness;

internal static class Program
{
    [STAThread]
    private static void Main(string[] args)
    {
        ApplicationConfiguration.Initialize();

        if (args.Any(arg => string.Equals(arg, "--tab-smoke-test", StringComparison.OrdinalIgnoreCase)))
        {
            RunTabSmokeTest();
            return;
        }

        var form = new MainForm();
        UiTheme.EnlargeTabs(form);
        RenameMainTab(form);
        InspectionResultVisuals.Attach(form);
        if (AcceptanceMode.IsEnabled)
            AcceptanceMode.Apply(form);
        Application.Run(form);
    }

    private static void RunTabSmokeTest()
    {
        using var form = new MainForm
        {
            ShowInTaskbar = false,
            Opacity = 0
        };
        UiTheme.EnlargeTabs(form);
        RenameMainTab(form);
        InspectionResultVisuals.Attach(form);
        AcceptanceMode.Apply(form, 120);
        form.Show();
        Application.DoEvents();

        var tabs = FindTabControl(form) ?? throw new InvalidOperationException("找不到主頁籤控制項。");
        if (tabs.TabPages.Count < 3) throw new InvalidOperationException("主頁籤數量不足。");

        var sizes = new[]
        {
            new Size(900, 720),
            new Size(1000, 780),
            new Size(1280, 900)
        };

        foreach (var size in sizes)
        {
            form.ClientSize = size;
            Application.DoEvents();
            for (var pass = 0; pass < 4; pass++)
            {
                for (var index = 0; index < 3; index++)
                {
                    tabs.SelectedIndex = index;
                    Application.DoEvents();
                }
            }
        }

        form.Close();
        Application.DoEvents();
    }

    private static void RenameMainTab(Control root)
    {
        var tabs = FindTabControl(root);
        if (tabs is not null && tabs.TabPages.Count > 0)
            tabs.TabPages[0].Text = "操作";
    }

    private static TabControl? FindTabControl(Control root)
    {
        foreach (Control child in root.Controls)
        {
            if (child is TabControl tabs) return tabs;
            var nested = FindTabControl(child);
            if (nested is not null) return nested;
        }
        return null;
    }
}
