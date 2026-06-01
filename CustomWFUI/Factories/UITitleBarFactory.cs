using System.Drawing;
using System.Windows.Forms;
using CustomWFUI.Controls;

namespace CustomWFUI.Factories
{
    public static class UITitleBarFactory
    {
        public static TitleBarControl Create(
            Image icon = null,
            string title = "",
            ContentAlignment titleTextAlign = ContentAlignment.MiddleCenter,
            bool showMinimizeButton = true,
            bool showMaximizeButton = true,
            bool showCloseButton = true,
            bool allowWindowSnapAndMaximize = true,
            Color? backColor = null)
        {
            return new TitleBarControl(
                icon,
                title,
                titleTextAlign,
                showMinimizeButton,
                showMaximizeButton,
                showCloseButton,
                allowWindowSnapAndMaximize,
                backColor);
        }
    }
}