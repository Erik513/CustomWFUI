using System;
using System.Drawing;
using System.Windows.Forms;

namespace CustomWFUI.Factories
{
    [Obsolete("Use CustomWFUI.UIStyles.FlowLayoutPanels instead.", false)]
    public static class UIFlowLayoutPanelFactory
    {
        public static FlowLayoutPanel CreateStandard()
        {
            return new FlowLayoutPanel
            {
                AutoSize = true,
                WrapContents = false,
                BackColor = Color.Transparent,
                Padding = new Padding(0),
                Margin = new Padding(0)
            };
        }
    }
}
