using System.Drawing;

namespace CustomWFUI.Styles
{
    /// <summary>
    /// The set of base surface colors (backgrounds, text, borders, hover/active
    /// tints) that make up a theme - everything except the accent color, which
    /// is set independently via <see cref="UIColors.SetAccent"/> so any accent
    /// can be combined with either theme. Property names describe each color's
    /// role (e.g. "the default control surface"), not its literal shade - in
    /// <see cref="UIThemes.Light"/>, "BackgroundLighter" still means "the most
    /// raised/active surface", even though it holds a darker RGB value than
    /// "BackgroundMedium" there (you can't go lighter than white, so the light
    /// theme's hover/press feedback works by adding a bit of gray instead).
    /// Defaults to today's dark values, so creating one and overriding just a
    /// few properties still yields a complete, usable theme.
    /// </summary>
    public class UIColorTheme
    {
        public Color BackgroundBlack { get; set; } = Color.FromArgb(10, 10, 10);
        public Color BackgroundDark { get; set; } = Color.FromArgb(20, 20, 20);
        public Color BackgroundDarkElevated { get; set; } = Color.FromArgb(25, 25, 25);
        public Color BackgroundMedium { get; set; } = Color.FromArgb(35, 35, 35);
        public Color BackgroundMediumElevated { get; set; } = Color.FromArgb(40, 40, 40);
        public Color BackgroundLight { get; set; } = Color.FromArgb(50, 50, 50);
        public Color BackgroundLighter { get; set; } = Color.FromArgb(60, 60, 60);

        public Color TextPrimary { get; set; } = Color.FromArgb(240, 240, 240);
        public Color TextPrimaryDim { get; set; } = Color.FromArgb(220, 220, 220);
        public Color TextSecondary { get; set; } = Color.FromArgb(180, 180, 180);
        public Color TextTertiary { get; set; } = Color.FromArgb(140, 140, 140);
        public Color TextDisabled { get; set; } = Color.FromArgb(100, 100, 100);
        public Color TextMuted { get; set; } = Color.FromArgb(120, 120, 120);

        public Color BorderDark { get; set; } = Color.FromArgb(50, 50, 50);
        public Color BorderMedium { get; set; } = Color.FromArgb(70, 70, 70);
        public Color BorderLight { get; set; } = Color.FromArgb(90, 90, 90);

        public Color HoverOverlay { get; set; } = Color.FromArgb(30, 30, 30, 80);
        public Color ActiveOverlay { get; set; } = Color.FromArgb(40, 40, 40, 120);
    }
}
