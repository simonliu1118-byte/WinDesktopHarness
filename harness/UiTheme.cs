using System.Drawing;

namespace WinDesktopHarness;

internal static class UiTheme
{
    public static readonly Color Window = Color.FromArgb(245, 247, 250);
    public static readonly Color Surface = Color.White;
    public static readonly Color Border = Color.FromArgb(220, 224, 230);
    public static readonly Color Text = Color.FromArgb(31, 41, 55);
    public static readonly Color Muted = Color.FromArgb(102, 112, 133);
    public static readonly Color Accent = Color.FromArgb(46, 74, 113);
    public static readonly Color AccentHover = Color.FromArgb(39, 63, 97);
    public static readonly Color Success = Color.FromArgb(33, 130, 92);
    public static readonly Color Danger = Color.FromArgb(180, 55, 55);

    public static Font Font(float size = 10f, FontStyle style = FontStyle.Regular) =>
        new("Microsoft JhengHei UI", size, style, GraphicsUnit.Point);

    public static Button Button(string text, bool primary = false)
    {
        var button = new Button
        {
            Text = text,
            AutoSize = false,
            Height = 36,
            Width = 108,
            FlatStyle = FlatStyle.Flat,
            BackColor = primary ? Accent : Surface,
            ForeColor = primary ? Color.White : Text,
            Font = Font(9.5f, FontStyle.Regular),
            Cursor = Cursors.Hand,
            Margin = new Padding(0, 0, 8, 0),
            TabStop = true
        };
        button.FlatAppearance.BorderSize = 1;
        button.FlatAppearance.BorderColor = primary ? Accent : Border;
        button.FlatAppearance.MouseOverBackColor = primary ? AccentHover : Color.FromArgb(248, 249, 251);
        return button;
    }

    public static Label Label(string text, float size = 10f, Color? color = null, FontStyle style = FontStyle.Regular)
    {
        return new Label
        {
            Text = text,
            AutoSize = true,
            ForeColor = color ?? Text,
            Font = Font(size, style),
            BackColor = Color.Transparent
        };
    }
}
