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

        public static readonly Color PrimaryDarkDark = Color.FromArgb(0, 30, 60);
        public static readonly Color PrimaryDark = Color.FromArgb(0, 50, 90);
        public static readonly Color Primary = Color.FromArgb(0, 90, 158);
        public static readonly Color PrimaryLight = Color.FromArgb(0, 120, 215);

        public static readonly Color SecondaryDark = Color.FromArgb(20, 80, 140);
        public static readonly Color Secondary = Color.FromArgb(30, 100, 180);
        public static readonly Color SecondaryLight = Color.FromArgb(50, 130, 210);

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
        public static readonly Color TextPrimaryDim = Color.FromArgb(220, 220, 220);
        public static readonly Color TextSecondary = Color.FromArgb(180, 180, 180);
        public static readonly Color TextTertiary = Color.FromArgb(140, 140, 140);
        public static readonly Color TextDisabled = Color.FromArgb(100, 100, 100);
        public static readonly Color TextMuted = Color.FromArgb(120, 120, 120);

        public static readonly Color BorderDark = Color.FromArgb(50, 50, 50);
        public static readonly Color BorderMedium = Color.FromArgb(70, 70, 70);
        public static readonly Color BorderLight = Color.FromArgb(90, 90, 90);
        public static readonly Color BorderPrimary = Color.FromArgb(0, 100, 180);
        public static readonly Color BorderRed = Color.FromArgb(180, 40, 50);

        public static readonly Color HoverOverlay = Color.FromArgb(30, 30, 30, 80);
        public static readonly Color ActiveOverlay = Color.FromArgb(40, 40, 40, 120);
        public static readonly Color Selection = Color.FromArgb(0, 90, 158, 60);

        public static readonly Color Transparent = Color.Transparent;
        public static readonly Color OverlayDark = Color.FromArgb(0, 0, 0, 180);
        public static readonly Color OverlayMedium = Color.FromArgb(0, 0, 0, 120);
        public static readonly Color OverlayLight = Color.FromArgb(0, 0, 0, 60);
    }
}
