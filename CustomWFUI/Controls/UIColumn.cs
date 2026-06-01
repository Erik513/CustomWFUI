using System.Windows.Forms;

namespace CustomWFUI.Controls
{
    public class UIColumn
    {
        public Control Control { get; private set; }

        public ColumnStyle Style { get; private set; }

        private UIColumn(
            Control control,
            ColumnStyle style)
        {
            Control = control;
            Style = style;
        }

        public static UIColumn Auto(Control control)
        {
            return new UIColumn(
                control,
                new ColumnStyle(SizeType.Percent, 100));
        }

        public static UIColumn Absolute(
            Control control,
            int width)
        {
            return new UIColumn(
                control,
                new ColumnStyle(SizeType.Absolute, width));
        }

        public static UIColumn Percent(
            Control control,
            float percent)
        {
            return new UIColumn(
                control,
                new ColumnStyle(SizeType.Percent, percent));
        }
    }

}
