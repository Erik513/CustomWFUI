using System;
using CustomWFUI.Controls;

namespace CustomWFUI.Factories
{
    [Obsolete("Use CustomWFUI.UIStyles.PropertyTables instead.", false)]
    public static class UIStyledPropertyTableFactory
    {
        public static StyledPropertyTable Create()
        {
            return new StyledPropertyTable();
        }
    }
}