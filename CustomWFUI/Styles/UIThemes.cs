using System.Drawing;

namespace ErikwnkWFUI.Styles
{
    /// <summary>
    /// Ready-to-use base themes for <see cref="UIColors.ApplyTheme"/>. Dark
    /// matches ErikwnkWFUI's original, always-has-been-the-default palette
    /// exactly (applying it is a no-op unless something else already changed
    /// the theme). Light is a from-scratch light palette, not a per-channel
    /// inversion of Dark - see the note on <see cref="UIColorTheme"/> about
    /// why the same role can hold a lighter or darker RGB value depending on
    /// which theme is active.
    /// </summary>
    public static class UIThemes
    {
        public static readonly UIColorTheme Dark = new UIColorTheme();

        public static readonly UIColorTheme Light = new UIColorTheme
        {
            BackgroundBlack = Color.FromArgb(243, 243, 243),
            BackgroundDark = Color.FromArgb(255, 255, 255),
            BackgroundDarkElevated = Color.FromArgb(235, 235, 235),
            BackgroundMedium = Color.FromArgb(255, 255, 255),
            BackgroundMediumElevated = Color.FromArgb(250, 250, 250),
            BackgroundLight = Color.FromArgb(235, 235, 235),
            BackgroundLighter = Color.FromArgb(222, 222, 222),

            TextPrimary = Color.FromArgb(20, 20, 20),
            TextPrimaryDim = Color.FromArgb(45, 45, 45),
            TextSecondary = Color.FromArgb(90, 90, 90),
            TextTertiary = Color.FromArgb(120, 120, 120),
            TextDisabled = Color.FromArgb(170, 170, 170),
            TextMuted = Color.FromArgb(140, 140, 140),

            BorderDark = Color.FromArgb(220, 220, 220),
            BorderMedium = Color.FromArgb(200, 200, 200),
            BorderLight = Color.FromArgb(180, 180, 180),

            // A translucent dark tint reads fine as a hover/press cue on a
            // light surface too, so these stay the same as Dark's.
            HoverOverlay = Color.FromArgb(30, 30, 30, 80),
            ActiveOverlay = Color.FromArgb(40, 40, 40, 120)
        };
    }
}
