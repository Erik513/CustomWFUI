using System.Windows.Forms;

namespace ErikwnkWFUI.Factories
{
    // Returns the System.Windows.Forms.ListView base type on purpose, not
    // Controls.ListView - callers never need to spell out the derived class
    // name (which would otherwise clash with "using System.Windows.Forms;"
    // in most consuming files).
    internal static class UIListViewFactory
    {
        public static ListView CreateStandard()
        {
            return new Controls.ListView();
        }
    }
}
