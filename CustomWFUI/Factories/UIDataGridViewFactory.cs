using System.Windows.Forms;

namespace CustomWFUI.Factories
{
    // Returns the System.Windows.Forms.DataGridView base type on purpose,
    // not Controls.DataGridView - callers never need to spell out the
    // derived class name (which would otherwise clash with this same
    // "using System.Windows.Forms;" in most consuming files).
    internal static class UIDataGridViewFactory
    {
        public static DataGridView CreateStandard(object dataSource = null)
        {
            return new Controls.DataGridView
            {
                DataSource = dataSource
            };
        }
    }
}
