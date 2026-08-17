using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CustomWFUI.Styles
{
    public static class UIColors
    {
        public static readonly Color Black = Color.Black;

        public static readonly Color BackgroundBlack = Color.FromArgb(10, 10, 10);
        public static readonly Color BackgroundDark = Color.FromArgb(20, 20, 20);
        public static readonly Color BackgroundDarkElevated = Color.FromArgb(25, 25, 25);
        public static readonly Color BackgroundMedium = Color.FromArgb(35, 35, 35);
        public static readonly Color BackgroundMediumElevated = Color.FromArgb(40, 40, 40);
        public static readonly Color BackgroundLight = Color.FromArgb(50, 50, 50);
        public static readonly Color BackgroundLighter = Color.FromArgb(60, 60, 60);

        // All shades of the app's single "accent" color - blue by default,
        // like every built-in Windows control. Not readonly (unlike the
        // rest of this file) because SetAccent below needs to overwrite
        // all of them at once when a consuming app wants its own accent
        // instead (e.g. DealOrNoDeal's black/yellow theme). Left untouched,
        // they keep exactly today's values, so calling SetAccent is
        // strictly opt-in and every existing consumer is unaffected.
        public static Color PrimaryDarkDark { get; set; } = Color.FromArgb(0, 30, 60);
        public static Color PrimaryDark { get; set; } = Color.FromArgb(0, 50, 90);
        public static Color Primary { get; set; } = Color.FromArgb(0, 90, 158);
        public static Color PrimaryLight { get; set; } = Color.FromArgb(0, 120, 215);

        public static Color SecondaryDark { get; set; } = Color.FromArgb(20, 80, 140);
        public static Color Secondary { get; set; } = Color.FromArgb(30, 100, 180);
        public static Color SecondaryLight { get; set; } = Color.FromArgb(50, 130, 210);

        public static readonly Color GreenDark = Color.FromArgb(20, 100, 50);
        public static readonly Color Green = Color.FromArgb(30, 150, 70);
        public static readonly Color GreenLight = Color.FromArgb(40, 180, 90);
        public static readonly Color GreenLighter = Color.FromArgb(50, 210, 110);

        public static readonly Color YellowDark = Color.FromArgb(170, 125, 0);
        public static readonly Color Yellow = Color.FromArgb(200, 150, 0);
        public static readonly Color YellowLight = Color.FromArgb(230, 180, 30);
        public static readonly Color YellowLighter = Color.FromArgb(255, 210, 60);

        public static readonly Color RedDark = Color.FromArgb(150, 20, 30);
        public static readonly Color Red = Color.FromArgb(180, 40, 50);
        public static readonly Color RedLight = Color.FromArgb(210, 60, 70);

        public static readonly Color White = Color.White;
        public static readonly Color TextPrimary = Color.FromArgb(240, 240, 240);
        // Fixed dark text color used against any surface that's brighter
        // than its idle state - e.g. every button's mouse-down background,
        // which is always a lighter shade than its idle one regardless of
        // hue, so pressed text needs to go dark unconditionally rather
        // than depend on that particular hue's computed contrast.
        public static readonly Color DarkForeColor = Color.FromArgb(20, 20, 20);
        public static readonly Color TextPrimaryDim = Color.FromArgb(220, 220, 220);
        public static readonly Color TextSecondary = Color.FromArgb(180, 180, 180);
        public static readonly Color TextTertiary = Color.FromArgb(140, 140, 140);
        public static readonly Color TextDisabled = Color.FromArgb(100, 100, 100);
        public static readonly Color TextMuted = Color.FromArgb(120, 120, 120);

        public static readonly Color BorderDark = Color.FromArgb(50, 50, 50);
        public static readonly Color BorderMedium = Color.FromArgb(70, 70, 70);
        public static readonly Color BorderLight = Color.FromArgb(90, 90, 90);
        public static Color BorderPrimary { get; set; } = Color.FromArgb(0, 100, 180);
        public static readonly Color BorderRed = Color.FromArgb(180, 40, 50);

        // Text/icon color to use on top of an accent-colored surface (e.g.
        // CreatePrimary buttons, ToastForm). White by default, matching
        // every existing surface today - SetAccent recomputes it so a
        // bright accent (e.g. yellow) gets dark text instead of white
        // text nobody can read.
        public static Color AccentForeColor { get; set; } = Color.FromArgb(240, 240, 240);

        public static readonly Color HoverOverlay = Color.FromArgb(30, 30, 30, 80);
        public static readonly Color ActiveOverlay = Color.FromArgb(40, 40, 40, 120);
        // Color.FromArgb(alpha, r, g, b) - this previously had alpha=0
        // (fully transparent, i.e. invisible no matter what it was painted
        // over) because the arguments were in the wrong order for a
        // translucent tint of Primary (0, 90, 158). Fixed to match the same
        // (alpha, r, g, b) pattern as HoverOverlay/ActiveOverlay above.
        public static Color Selection { get; set; } = Color.FromArgb(60, 0, 90, 158);

        public static readonly Color Transparent = Color.Transparent;
        public static readonly Color OverlayDark = Color.FromArgb(0, 0, 0, 180);
        public static readonly Color OverlayMedium = Color.FromArgb(0, 0, 0, 120);
        public static readonly Color OverlayLight = Color.FromArgb(0, 0, 0, 60);

        /// <summary>
        /// Replaces every accent shade (Primary/Secondary/Selection/
        /// BorderPrimary - everything a consuming app would otherwise have
        /// to override control-by-control) with tints and shades computed
        /// from a single base color. Call once, as early as possible
        /// (before building any UI) - a handful of controls read their
        /// accent color once into a field at construction time rather than
        /// on every paint, so they won't pick up a change made after
        /// they're already built.
        /// </summary>
        public static void SetAccent(Color accent)
        {
            // Gentler than a straight percentage-of-blue-value darken would
            // suggest - blue's own default (0, 90, 158) was already fairly
            // dark, so darkening it hard still left a visible dark blue.
            // Doing the same to a bright, near-max-brightness accent (e.g.
            // a bright yellow) would crush it into a muddy brown instead.
            PrimaryDarkDark = Darken(accent, 0.55);
            PrimaryDark = Darken(accent, 0.30);
            Primary = accent;
            PrimaryLight = Lighten(accent, 0.35);

            SecondaryDark = Darken(accent, 0.10);
            Secondary = Lighten(accent, 0.10);
            SecondaryLight = Lighten(accent, 0.25);

            BorderPrimary = accent;
            Selection = Color.FromArgb(60, accent.R, accent.G, accent.B);

            AccentForeColor = GetContrastingForeColor(accent);
        }

        /// <summary>
        /// White or near-black, whichever reads better on top of the given
        /// background - e.g. white text is unreadable on a bright yellow
        /// accent even though it's fine on the default dark blue.
        /// </summary>
        public static Color GetContrastingForeColor(Color background)
        {
            double luminance = (0.299 * background.R + 0.587 * background.G + 0.114 * background.B) / 255.0;
            return luminance > 0.5 ? DarkForeColor : TextPrimary;
        }

        private static Color Darken(Color color, double amount)
        {
            return Color.FromArgb(
                (int)(color.R * (1 - amount)),
                (int)(color.G * (1 - amount)),
                (int)(color.B * (1 - amount)));
        }

        private static Color Lighten(Color color, double amount)
        {
            return Color.FromArgb(
                color.R + (int)((255 - color.R) * amount),
                color.G + (int)((255 - color.G) * amount),
                color.B + (int)((255 - color.B) * amount));
        }
    }
}
