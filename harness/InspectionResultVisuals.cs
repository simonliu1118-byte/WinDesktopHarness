using System.Drawing;

namespace WinDesktopHarness;

internal static class InspectionResultVisuals
{
    public static void Attach(Control root)
    {
        var badge = FindBadge(root);
        if (badge?.Parent is not Panel panel) return;

        void Refresh()
        {
            panel.BackColor = badge.Text switch
            {
                "驗證成功" => Color.FromArgb(238, 249, 244),
                "驗證失敗" => Color.FromArgb(253, 241, 241),
                _ => Color.FromArgb(247, 249, 252)
            };
        }

        badge.TextChanged += (_, _) => Refresh();
        Refresh();
    }

    private static Label? FindBadge(Control root)
    {
        foreach (Control child in root.Controls)
        {
            if (child is Label label && string.Equals(label.Text, "待檢查", StringComparison.Ordinal))
                return label;

            var nested = FindBadge(child);
            if (nested is not null) return nested;
        }

        return null;
    }
}
