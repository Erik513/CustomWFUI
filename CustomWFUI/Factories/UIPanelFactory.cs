using System.Drawing;
using System.Windows.Forms;
using CustomWFUI.Styles;

namespace CustomWFUI.Factories
{
    internal static class UIPanelFactory
    {
        public static Panel CreateDark()
        {
            return CreatePanel(
                UIColors.BackgroundDark,
                DockStyle.Fill);
        }

        public static Panel CreateMedium()
        {
            return CreatePanel(
                UIColors.BackgroundMedium,
                DockStyle.Fill);
        }

        public static Panel CreateElevated()
        {
            return CreatePanel(
                UIColors.BackgroundMediumElevated,
                DockStyle.Fill);
        }

        public static Panel CreateTransparent()
        {
            return CreatePanel(
                Color.Transparent,
                DockStyle.None);
        }

        public static Panel CreatePrimary()
        {
            return CreatePanel(
                UIColors.PrimaryDarkDark,
                DockStyle.Fill);
        }

        private static Panel CreatePanel(
            Color backColor,
            DockStyle dock)
        {
            return new Panel
            {
                Dock = dock,
                BackColor = backColor,
                BorderStyle = BorderStyle.None,
                Padding = new Padding(0),
                Margin = new Padding(0)
            };
        }
    }
}