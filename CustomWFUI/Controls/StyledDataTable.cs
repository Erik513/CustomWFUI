using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using CustomWFUI.Styles;

namespace CustomWFUI.Controls
{
    /// <summary>
    /// A lightweight, read-only, multi-column table for simple data
    /// display - unlike StyledListView (spreadsheet-style cell selection,
    /// clipboard copy, resizable/reorderable columns), this has none of
    /// that: fixed column order/width, no selection, no interaction at
    /// all. Sizes itself to exactly fit its header plus however many rows
    /// were set, so it never needs an explicit Size or a scrollbar for a
    /// short, known list.
    /// </summary>
    public class StyledDataTable : UserControl
    {
        private readonly TableLayoutPanel _grid;

        private string[] _columnHeaders = Array.Empty<string>();
        private int[] _columnWidths = Array.Empty<int>();
        private List<string[]> _rows = new List<string[]>();

        private Color _headerBackColor = UIColors.BackgroundDarkElevated;
        private Color _headerForeColor = UIColors.TextTertiary;
        private Color _rowBackColor = UIColors.BackgroundMedium;
        private Color _alternateRowBackColor;
        private Color _rowForeColor = UIColors.TextPrimary;
        private Font _headerFont;
        private Font _rowFont = UIFonts.Normal;
        private int _headerHeight = 32;
        private int _rowHeight = 32;

        public Color HeaderBackColor
        {
            get => _headerBackColor;
            set { _headerBackColor = value; Rebuild(); }
        }

        public Color HeaderForeColor
        {
            get => _headerForeColor;
            set { _headerForeColor = value; Rebuild(); }
        }

        public Color RowBackColor
        {
            get => _rowBackColor;
            set { _rowBackColor = value; _alternateRowBackColor = Darken(value, 5); Rebuild(); }
        }

        public Color RowForeColor
        {
            get => _rowForeColor;
            set { _rowForeColor = value; Rebuild(); }
        }

        public Font HeaderFont
        {
            get => _headerFont;
            set { _headerFont = value; Rebuild(); }
        }

        public Font RowFont
        {
            get => _rowFont;
            set { _rowFont = value; Rebuild(); }
        }

        public int HeaderHeight
        {
            get => _headerHeight;
            set { _headerHeight = Math.Max(0, value); Rebuild(); }
        }

        public int RowHeight
        {
            get => _rowHeight;
            set { _rowHeight = Math.Max(1, value); Rebuild(); }
        }

        public StyledDataTable()
        {
            AutoSize = true;
            AutoSizeMode = AutoSizeMode.GrowAndShrink;
            BackColor = Color.Transparent;
            Margin = new Padding(0);
            Padding = new Padding(0);

            _headerFont = new Font(_rowFont, FontStyle.Bold);
            _alternateRowBackColor = Darken(_rowBackColor, 5);

            _grid = new TableLayoutPanel
            {
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                CellBorderStyle = TableLayoutPanelCellBorderStyle.None,
                Margin = new Padding(0),
                Padding = new Padding(0)
            };

            Controls.Add(_grid);
        }

        /// <summary>
        /// Column headers and their fixed pixel widths, in display order -
        /// there's no drag-to-reorder here, so "display order" and "data
        /// order" are always the same thing.
        /// </summary>
        public void SetColumns(string[] headers, int[] widths)
        {
            _columnHeaders = headers ?? Array.Empty<string>();
            _columnWidths = widths ?? Array.Empty<int>();
            Rebuild();
        }

        /// <summary>
        /// Replaces every row. Each row's cells are matched to columns by
        /// position; a row with fewer cells than columns just leaves the
        /// remaining ones blank.
        /// </summary>
        public void SetRows(IEnumerable<string[]> rows)
        {
            _rows = rows?.ToList() ?? new List<string[]>();
            Rebuild();
        }

        private void Rebuild()
        {
            _grid.SuspendLayout();
            _grid.Controls.Clear();
            _grid.ColumnStyles.Clear();
            _grid.RowStyles.Clear();
            _grid.ColumnCount = Math.Max(1, _columnHeaders.Length);
            _grid.RowCount = 1 + _rows.Count;

            for (int column = 0; column < _columnHeaders.Length; column++)
            {
                int width = column < _columnWidths.Length ? _columnWidths[column] : 120;
                _grid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, width));
            }

            _grid.RowStyles.Add(new RowStyle(SizeType.Absolute, _headerHeight));
            for (int row = 0; row < _rows.Count; row++)
                _grid.RowStyles.Add(new RowStyle(SizeType.Absolute, _rowHeight));

            for (int column = 0; column < _columnHeaders.Length; column++)
            {
                _grid.Controls.Add(
                    CreateCellLabel(_columnHeaders[column], _headerFont, _headerForeColor, _headerBackColor),
                    column,
                    0);
            }

            for (int row = 0; row < _rows.Count; row++)
            {
                Color rowColor = row % 2 == 0 ? _rowBackColor : _alternateRowBackColor;
                string[] cells = _rows[row];

                for (int column = 0; column < _columnHeaders.Length; column++)
                {
                    string text = cells != null && column < cells.Length ? cells[column] : "";
                    _grid.Controls.Add(
                        CreateCellLabel(text, _rowFont, _rowForeColor, rowColor),
                        column,
                        row + 1);
                }
            }

            _grid.ResumeLayout(true);
        }

        private static Label CreateCellLabel(string text, Font font, Color foreColor, Color backColor)
        {
            return new Label
            {
                Text = text ?? "",
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(8, 0, 8, 0),
                Margin = new Padding(0),
                Font = font,
                ForeColor = foreColor,
                BackColor = backColor,
                AutoEllipsis = true,
                UseMnemonic = false
            };
        }

        private static Color Darken(Color color, int amount)
        {
            return Color.FromArgb(
                Math.Max(0, color.R - amount),
                Math.Max(0, color.G - amount),
                Math.Max(0, color.B - amount));
        }
    }
}
