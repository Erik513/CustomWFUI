using System.Drawing;

namespace CustomWFUI.Styles
{
    /// <summary>
    /// A curated palette of base colors for <see cref="UIColors.SetAccent"/>,
    /// picked at roughly the same brightness/saturation as <see cref="Blue"/>
    /// (the default) so every choice produces a comparably good set of
    /// derived light/dark shades. Not exhaustive - SetAccent takes any Color,
    /// this is just a ready-made "pick one" set for a color-picker UI.
    /// </summary>
    public static class UIAccentColors
    {
        /// <summary>CustomWFUI's original, always-has-been-the-default accent.</summary>
        public static readonly Color Blue = Color.FromArgb(0, 90, 158);

        public static readonly Color Red = Color.FromArgb(200, 45, 45);
        public static readonly Color Orange = Color.FromArgb(210, 110, 20);
        public static readonly Color Amber = Color.FromArgb(200, 150, 0);
        public static readonly Color Green = Color.FromArgb(30, 140, 70);
        public static readonly Color Teal = Color.FromArgb(0, 130, 114);
        public static readonly Color Cyan = Color.FromArgb(0, 130, 170);
        public static readonly Color Indigo = Color.FromArgb(65, 80, 180);
        public static readonly Color Purple = Color.FromArgb(110, 60, 170);
        public static readonly Color Magenta = Color.FromArgb(170, 40, 130);
        public static readonly Color Pink = Color.FromArgb(200, 60, 110);
        public static readonly Color Gray = Color.FromArgb(90, 90, 90);

        public static readonly Color[] All =
        {
            Blue, Red, Orange, Amber, Green, Teal,
            Cyan, Indigo, Purple, Magenta, Pink, Gray
        };
    }
}
