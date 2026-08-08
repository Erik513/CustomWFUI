using System.Drawing;
using CustomWFUI.Controls;

namespace CustomWFUI.Factories
{
    internal static class UIStyledListBoxControlFactory
    {
        public static StyledListBoxControl Create(
            string headerTitle = null,
            string displayTextMember = null,
            bool allowReorder = false,
            bool showEnumeration = false,
            ContentAlignment headerTextAlign = ContentAlignment.MiddleLeft)
        {
            return new StyledListBoxControl(
                displayTextMember,
                allowReorder,
                showEnumeration,
                headerTitle,
                headerTextAlign);
        }
    }
}