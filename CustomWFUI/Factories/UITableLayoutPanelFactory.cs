using System;
using System.Drawing;
using System.Windows.Forms;
using CustomWFUI.Styles;

namespace CustomWFUI.Factories
{
    public static class UITableLayoutPanelFactory
    {
        public static TableLayoutPanel CreateStandard(
            int columnCount,
            int rowCount)
        {
            return CreateTableLayoutPanel(
                columnCount,
                rowCount,
                UIColors.BackgroundMediumElevated);
        }

        public static TableLayoutPanel CreateDark(
            int columnCount,
            int rowCount)
        {
            return CreateTableLayoutPanel(
                columnCount,
                rowCount,
                UIColors.BackgroundDark);
        }

        private static TableLayoutPanel CreateTableLayoutPanel(
            int columnCount,
            int rowCount,
            Color backColor)
        {
            return new TableLayoutPanel
            {
                ColumnCount = Math.Max(0, columnCount),
                RowCount = Math.Max(0, rowCount),
                BackColor = backColor,
                Margin = new Padding(0),
                Padding = new Padding(0)
            };
        }
    }
}