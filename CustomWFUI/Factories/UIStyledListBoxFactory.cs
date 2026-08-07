using System;
using CustomWFUI.Controls;

namespace CustomWFUI.Factories
{
    [Obsolete("Use CustomWFUI.UIStyles.StyledListBoxes instead.", false)]
    public static class UIStyledListBoxFactory
    {
        public static StyledListBox Create(
            string displayTextMember = null,
            bool allowReorder = true,
            bool showEnumeration = false)
        {
            return new StyledListBox
            {
                DisplayTextMember = displayTextMember,
                AllowReorder = allowReorder,
                ShowEnumeration = showEnumeration
            };
        }
    }
}