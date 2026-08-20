using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Text;
using System.Windows.Forms;
using CustomWFUI.Forms;
using CustomWFUI.Styles;

namespace CustomWFUI.Controls
{
    /// <summary>How <see cref="StyledListView"/> confirms a Ctrl+C/Ctrl+Shift+C copy.</summary>
    public enum CopyConfirmationStyle
    {
        /// <summary>A <see cref="Forms.ToastForm"/> popup (the original, default behavior).</summary>
        Toast,
        /// <summary>A brief standard <see cref="ToolTip"/> instead - for apps that don't want ToastForm's separate popup window.</summary>
        ToolTip,
        /// <summary>No visual confirmation at all - the copy still happens, just silently.</summary>
        None
    }

    /// <summary>
    /// A dark-themed, multi-column ListView (Details view) with genuine,
    /// spreadsheet-like cell-range selection instead of the native
    /// whole-row highlight: cells stay dark until a real selection is made,
    /// and then only the selected cells turn blue.
    /// <list type="bullet">
    /// <item>A plain click selects just that one cell.</item>
    /// <item>Holding the left button down and dragging extends the selection to a rectangle between the click and the current cursor cell.</item>
    /// <item>Ctrl+Alt+Click extends the existing selection to the clicked cell without needing to drag all the way there.</item>
    /// <item>Ctrl+A selects every cell.</item>
    /// <item>Clicking a cell that's already selected deselects it again.</item>
    /// <item>Arrow keys move a single-cell selection; Shift+arrow extends it, the same way Shift+click would.</item>
    /// <item>Hovering a cell whose text is wider than its column shows the full text in a tooltip.</item>
    /// </list>
    /// Ctrl+C copies the selected rectangle (tab-separated columns, one line
    /// per row, no header line - meant to be pasted as plain data). Ctrl+
    /// Shift+C copies the same rectangle with a leading row of column names,
    /// for pasting as a proper table. Both also place an HTML table on the
    /// clipboard alongside the plain text, so apps that understand it (Word,
    /// Outlook, browsers, Excel, ...) paste an actual bordered table instead
    /// of raw tab characters; plain-text-only targets still get the tab-
    /// separated fallback. The right-click menu only shows "Copy selection"
    /// and "Copy all" at the top level; hovering either opens a submenu with
    /// the plain action again plus "As table". Every copy is confirmed with
    /// a <see cref="Forms.ToastForm"/>.
    /// </summary>
    public class StyledListView : ListView
    {
        private const int DefaultMinimumColumnWidth = 40;

        private int _anchorRow = -1;
        private int _anchorColumn = -1;
        private int _activeRow = -1;
        private int _activeColumn = -1;
        private bool _isDragSelecting;
        private bool _isApplyingFillColumn;

        private Color _rowBackColor = UIColors.BackgroundMedium;
        private Color _alternateRowBackColor;
        private Color _rowForeColor = UIColors.TextPrimary;
        private Color _selectionOverlayColorOverride;
        private bool _selectionOverlayColorIsOverridden;
        private Color _headerBackColor = UIColors.BackgroundDarkElevated;
        private Color _headerForeColor = UIColors.TextTertiary;
        private int _minimumColumnWidth = DefaultMinimumColumnWidth;
        private int _fillColumnIndex = -1;
        private readonly HashSet<int> _nonResizableColumns = new HashSet<int>();
        private readonly HashSet<int> _nonReorderableColumns = new HashSet<int>();
        private Color _columnReorderIndicatorColorOverride;
        private bool _columnReorderIndicatorColorIsOverridden;
        private bool _isDraggingColumn;
        private int _dragColumnIndex = -1;
        private int _dragInsertBeforeDisplayIndex = -1;
        private HeaderInputSubclass _headerInputSubclass;

        private readonly ToolTip _cellToolTip = new ToolTip { InitialDelay = 400, ReshowDelay = 100, AutoPopDelay = 8000, ShowAlways = true };
        private int _toolTipRow = -1;
        private int _toolTipDisplayColumn = -1;

        /// <summary>No column can be resized narrower than this.</summary>
        public int MinimumColumnWidth
        {
            get { return _minimumColumnWidth; }
            set { _minimumColumnWidth = Math.Max(1, value); }
        }

        /// <summary>How a Ctrl+C/Ctrl+Shift+C copy is confirmed. Defaults to <see cref="CopyConfirmationStyle.Toast"/> (unchanged from before this existed).</summary>
        public CopyConfirmationStyle CopyConfirmation { get; set; } = CopyConfirmationStyle.Toast;

        /// <summary>
        /// Which column stretches to fill any leftover width. -1 (the
        /// default) means "whichever column is last" - set this explicitly
        /// if a different column should be the one that stretches instead.
        /// </summary>
        public int FillColumnIndex
        {
            get { return _fillColumnIndex; }
            set
            {
                _fillColumnIndex = value;
                ApplyFillColumn();
            }
        }

        /// <summary>Background color of a normal (not selected) row. Odd/even rows alternate between this and a slightly darker shade of it.</summary>
        public Color RowBackColor
        {
            get { return _rowBackColor; }
            set
            {
                _rowBackColor = value;
                _alternateRowBackColor = Darken(value, 5);
                Invalidate();
            }
        }

        /// <summary>Text color of a normal (not selected) row.</summary>
        public Color RowForeColor
        {
            get { return _rowForeColor; }
            set
            {
                _rowForeColor = value;
                Invalidate();
            }
        }

        /// <summary>
        /// Painted on top of a selected cell's normal background rather than
        /// replacing it outright - <see cref="UIColors.Selection"/> is a
        /// translucent blue for exactly this, so the row's own (e.g.
        /// severity) color still shows through underneath a selection.
        /// Follows the current accent live until explicitly set - it used to
        /// be a plain field snapshotted once at construction (like
        /// StyledListBox's DragIndicatorColor before the same fix), so an
        /// app that changed its accent after building the list still showed
        /// the default blue regardless.
        /// </summary>
        public Color SelectionOverlayColor
        {
            get { return _selectionOverlayColorIsOverridden ? _selectionOverlayColorOverride : UIColors.Selection; }
            set
            {
                _selectionOverlayColorOverride = value;
                _selectionOverlayColorIsOverridden = true;
                Invalidate();
            }
        }

        /// <summary>
        /// Color of the vertical line the header shows while a column is
        /// being dragged to reorder it. Follows the current accent live
        /// until explicitly set, same as <see cref="SelectionOverlayColor"/>.
        /// Column reordering is fully hand-rolled (see the mouse handlers
        /// below) rather than using <see cref="ListView.AllowColumnReorder"/>
        /// - that hands the whole drag to the native Win32 header control
        /// (comctl32), which draws its own insertion line as native chrome
        /// with no public API to recolor, and (confirmed by instrumenting a
        /// live drag) draws it in a way that isn't reliably interceptable by
        /// subclassing at all. Owning the whole gesture means this line is
        /// just an ordinary part of <see cref="OnDrawColumnHeader"/>'s
        /// existing owner-draw painting - no native chrome involved.
        /// </summary>
        public Color ColumnReorderIndicatorColor
        {
            get { return _columnReorderIndicatorColorIsOverridden ? _columnReorderIndicatorColorOverride : UIColors.Primary; }
            set
            {
                _columnReorderIndicatorColorOverride = value;
                _columnReorderIndicatorColorIsOverridden = true;
            }
        }

        /// <summary>Background color of the column header row.</summary>
        public Color HeaderBackColor
        {
            get { return _headerBackColor; }
            set
            {
                _headerBackColor = value;
                Invalidate();
            }
        }

        /// <summary>Text color of the column header row.</summary>
        public Color HeaderForeColor
        {
            get { return _headerForeColor; }
            set
            {
                _headerForeColor = value;
                Invalidate();
            }
        }

        public StyledListView()
        {
            View = View.Details;
            FullRowSelect = true;
            HideSelection = true;
            MultiSelect = false;
            // Column reordering is hand-rolled (see HeaderInputSubclass and
            // OnDragOver/OnDragDrop below) instead of using the native
            // drag - see ColumnReorderIndicatorColor's doc comment for why.
            // AllowDrop is this control's own DoDragDrop-based reorder, not
            // an app-facing external drag-and-drop target.
            AllowColumnReorder = false;
            AllowDrop = true;
            BorderStyle = BorderStyle.None;
            BackColor = UIColors.BackgroundDark;
            ForeColor = _rowForeColor;
            Font = UIFonts.Normal;
            HeaderStyle = ColumnHeaderStyle.Nonclickable;
            OwnerDraw = true;

            _alternateRowBackColor = Darken(_rowBackColor, 5);

            SetStyle(
                ControlStyles.OptimizedDoubleBuffer |
                ControlStyles.ResizeRedraw |
                ControlStyles.AllPaintingInWmPaint,
                true);
            DoubleBuffered = true;
            UpdateStyles();

            DrawColumnHeader += OnDrawColumnHeader;
            DrawItem += OnDrawItem;
            DrawSubItem += OnDrawSubItem;
            MouseDown += OnListViewMouseDown;
            MouseMove += OnListViewMouseMove;
            MouseMove += OnListViewMouseMoveForToolTip;
            MouseLeave += OnListViewMouseLeave;
            MouseUp += OnListViewMouseUp;
            KeyDown += OnListViewKeyDown;
            ColumnWidthChanging += OnColumnWidthChanging;
            ColumnWidthChanged += OnColumnWidthChanged;

            ContextMenuStrip = BuildContextMenu();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _cellToolTip.Dispose();
            }

            base.Dispose(disposing);
        }

        /// <summary>
        /// Configurable per column, independent of <see cref="MinimumColumnWidth"/> - a
        /// column can be locked at its current width entirely (e.g. a
        /// narrow status/quality column that should never accidentally get
        /// dragged to something illegible) while others stay freely
        /// resizable.
        /// </summary>
        public void SetColumnResizable(int columnIndex, bool resizable)
        {
            if (resizable)
            {
                _nonResizableColumns.Remove(columnIndex);
            }
            else
            {
                _nonResizableColumns.Add(columnIndex);
            }
        }

        public bool IsColumnResizable(int columnIndex)
        {
            return !_nonResizableColumns.Contains(columnIndex);
        }

        /// <summary>
        /// Configurable per column, independent of the control-wide
        /// <see cref="ListView.AllowColumnReorder"/> switch: a column can be
        /// pinned in place (e.g. a leading "Time"/"Metric" column that should
        /// always stay leftmost) while the rest can still be freely dragged
        /// into a new order.
        /// </summary>
        public void SetColumnReorderable(int columnIndex, bool reorderable)
        {
            if (reorderable)
            {
                _nonReorderableColumns.Remove(columnIndex);
            }
            else
            {
                _nonReorderableColumns.Add(columnIndex);
            }
        }

        public bool IsColumnReorderable(int columnIndex)
        {
            return !_nonReorderableColumns.Contains(columnIndex);
        }

        /// <summary>
        /// Sizes every column (other than the fill column, which stretches
        /// regardless) to fit its current content and header text, then lets
        /// the fill column absorb whatever space is left. Call this again
        /// after rebuilding the rows, since content driving the "right" width
        /// may have changed.
        /// </summary>
        public void AutoFitColumnsToContent()
        {
            if (!IsHandleCreated || Columns.Count == 0)
            {
                return;
            }

            var fillIndex = GetEffectiveFillColumnIndex();
            for (var index = 0; index < Columns.Count; index++)
            {
                if (index == fillIndex)
                {
                    continue;
                }

                AutoResizeColumn(index, ColumnHeaderAutoResizeStyle.ColumnContent);
                var minimumWidth = GetEffectiveMinimumWidth(index);
                if (Columns[index].Width < minimumWidth)
                {
                    Columns[index].Width = minimumWidth;
                }
            }

            ApplyFillColumn();
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            ApplyFillColumn();

            // The header is a separate native child window (class
            // "SysHeader32"), recreated along with the ListView's own handle -
            // re-attach every time rather than once in the constructor. See
            // HeaderInputSubclass for why column-reorder dragging is hooked
            // here instead of this control's own mouse events.
            _headerInputSubclass?.ReleaseHandle();
            System.IntPtr headerHandle = HeaderInputSubclass.GetHeaderHandle(Handle);
            if (headerHandle != System.IntPtr.Zero)
            {
                _headerInputSubclass = new HeaderInputSubclass(this);
                _headerInputSubclass.AssignHandle(headerHandle);
            }
        }

        protected override void OnHandleDestroyed(EventArgs e)
        {
            _headerInputSubclass?.ReleaseHandle();
            _headerInputSubclass = null;

            base.OnHandleDestroyed(e);
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            ApplyFillColumn();
        }

        private void OnColumnWidthChanging(object sender, ColumnWidthChangingEventArgs e)
        {
            if (_nonResizableColumns.Contains(e.ColumnIndex))
            {
                e.NewWidth = Columns[e.ColumnIndex].Width;
                e.Cancel = true;
                return;
            }

            var minimumWidth = GetEffectiveMinimumWidth(e.ColumnIndex);
            if (e.NewWidth < minimumWidth)
            {
                e.NewWidth = minimumWidth;
                e.Cancel = true;
            }
        }

        // MinimumColumnWidth is a floor the caller chose, but a column must
        // never end up narrower than its own header text needs, or the
        // caption itself gets clipped - whichever of the two is larger wins.
        private int GetEffectiveMinimumWidth(int columnIndex)
        {
            return Math.Max(_minimumColumnWidth, MeasureHeaderTextWidth(columnIndex));
        }

        private int MeasureHeaderTextWidth(int columnIndex)
        {
            if (columnIndex < 0 || columnIndex >= Columns.Count)
            {
                return 0;
            }

            using (var font = new Font(Font, FontStyle.Bold))
            {
                var textSize = TextRenderer.MeasureText(
                    Columns[columnIndex].Text,
                    font,
                    new Size(int.MaxValue, int.MaxValue),
                    TextFormatFlags.Left | TextFormatFlags.NoPadding);

                // Matches the 7px left / 3px right padding OnDrawColumnHeader
                // draws the caption with.
                return textSize.Width + 10;
            }
        }

        private void OnColumnWidthChanged(object sender, ColumnWidthChangedEventArgs e)
        {
            if (!_isApplyingFillColumn && e.ColumnIndex != GetEffectiveFillColumnIndex())
            {
                ApplyFillColumn();
            }
        }

        // The column that fills leftover space is "whichever is last" by
        // default - once columns can be dragged into a different order
        // (AllowColumnReorder), "last" has to mean visually rightmost
        // (DisplayIndex), not whatever its original/data index happened to
        // be, or the wrong column would keep stretching after a reorder.
        private int GetEffectiveFillColumnIndex()
        {
            if (_fillColumnIndex >= 0 && _fillColumnIndex < Columns.Count)
            {
                return _fillColumnIndex;
            }

            if (Columns.Count == 0)
            {
                return -1;
            }

            var rightmost = Columns[0];
            foreach (ColumnHeader column in Columns)
            {
                if (column.DisplayIndex > rightmost.DisplayIndex)
                {
                    rightmost = column;
                }
            }

            return rightmost.Index;
        }

        private List<ColumnHeader> GetColumnsInDisplayOrder()
        {
            var ordered = new List<ColumnHeader>();
            foreach (ColumnHeader column in Columns)
            {
                ordered.Add(column);
            }

            ordered.Sort((first, second) => first.DisplayIndex.CompareTo(second.DisplayIndex));
            return ordered;
        }

        // Whatever space isn't claimed by the other columns goes to the
        // fill column (the last one, unless FillColumnIndex says
        // otherwise) - so the table always reaches the right edge instead
        // of leaving a dead strip of background, or needing a horizontal
        // scrollbar for a couple of stray pixels.
        private void ApplyFillColumn()
        {
            if (_isApplyingFillColumn || IsDisposed || !IsHandleCreated || Columns.Count == 0)
            {
                return;
            }

            var fillIndex = GetEffectiveFillColumnIndex();
            var otherColumnsWidth = 0;
            for (var index = 0; index < Columns.Count; index++)
            {
                if (index != fillIndex)
                {
                    otherColumnsWidth += Columns[index].Width;
                }
            }

            var scrollBarAllowance = IsVerticalScrollBarLikelyVisible() ? SystemInformation.VerticalScrollBarWidth : 0;
            var availableWidth = ClientSize.Width - otherColumnsWidth - scrollBarAllowance;
            var newWidth = Math.Max(_minimumColumnWidth, availableWidth);
            if (Columns[fillIndex].Width == newWidth)
            {
                return;
            }

            _isApplyingFillColumn = true;
            try
            {
                Columns[fillIndex].Width = newWidth;
            }
            finally
            {
                _isApplyingFillColumn = false;
            }
        }

        private bool IsVerticalScrollBarLikelyVisible()
        {
            if (Items.Count == 0)
            {
                return false;
            }

            // Estimated from the font rather than a live item's own Bounds -
            // reading Bounds requires the native control to have already
            // laid out that row, which isn't guaranteed at every point
            // ApplyFillColumn can run from (right after Items is cleared
            // and repopulated, or during a handle recreation) and has been
            // observed to throw there. A small fixed padding approximates
            // the same per-row height OwnerDraw would otherwise use.
            var itemHeight = Font.Height + 6;
            return itemHeight > 0 && Items.Count * itemHeight > ClientSize.Height;
        }

        // The top level only ever shows "Copy selection" / "Copy all" -
        // each is a plain submenu parent (native arrow, opens on hover, no
        // split-button chrome) whose flyout holds the actual two actions,
        // plain and "As table".
        private ContextMenuStrip BuildContextMenu()
        {
            var menu = new ContextMenuStrip();

            string copySelectionText = UIStrings.Get("ListView.CopySelection");
            string copyAllText = UIStrings.Get("ListView.CopyAll");
            string asTableText = UIStrings.Get("ListView.AsTable");

            var copySelection = new ToolStripMenuItem(copySelectionText);
            copySelection.DropDownItems.Add(copySelectionText, null, (sender, e) => CopySelection());
            copySelection.DropDownItems.Add(asTableText, null, (sender, e) => CopySelectionAsTable());

            var copyAll = new ToolStripMenuItem(copyAllText);
            copyAll.DropDownItems.Add(copyAllText, null, (sender, e) => { SelectAll(); CopySelection(); });
            copyAll.DropDownItems.Add(asTableText, null, (sender, e) => { SelectAll(); CopySelectionAsTable(); });

            menu.Items.Add(copySelection);
            menu.Items.Add(copyAll);

            menu.Opening += (sender, e) =>
            {
                // Nothing is selected outside a real anchor cell, so there's
                // nothing for "Copy selection" (plain or as table) to act on.
                copySelection.Enabled = _anchorRow >= 0;
                copyAll.Enabled = Items.Count > 0;
            };

            return menu;
        }

        private void OnDrawColumnHeader(object sender, DrawListViewColumnHeaderEventArgs e)
        {
            using (var background = new SolidBrush(_headerBackColor))
            using (var divider = new Pen(UIColors.BorderMedium))
            using (var font = new Font(Font, FontStyle.Bold))
            {
                e.Graphics.FillRectangle(background, e.Bounds);
                e.Graphics.DrawLine(divider, e.Bounds.Right - 1, e.Bounds.Top, e.Bounds.Right - 1, e.Bounds.Bottom);
                e.Graphics.DrawLine(divider, e.Bounds.Left, e.Bounds.Bottom - 1, e.Bounds.Right, e.Bounds.Bottom - 1);
                TextRenderer.DrawText(
                    e.Graphics,
                    e.Header.Text,
                    font,
                    new Rectangle(e.Bounds.X + 7, e.Bounds.Y, Math.Max(0, e.Bounds.Width - 10), e.Bounds.Height),
                    _headerForeColor,
                    TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
            }

            DrawColumnDragInsertionLine(e);
        }

        // Drawn as part of the same owner-draw pass as the header cell
        // itself (rather than as a separate overlay) so it's fully in our
        // own hands, unlike the native AllowColumnReorder line this
        // replaced - see ColumnReorderIndicatorColor's doc comment. Exactly
        // one header cell's left edge lines up with
        // _dragInsertBeforeDisplayIndex (or, for "insert after the last
        // column", the last cell's right edge), so at most one of these two
        // checks ever draws anything per call.
        private void DrawColumnDragInsertionLine(DrawListViewColumnHeaderEventArgs e)
        {
            if (!_isDraggingColumn || _dragInsertBeforeDisplayIndex < 0)
            {
                return;
            }

            using (var pen = new Pen(ColumnReorderIndicatorColor, 2))
            {
                if (e.Header.DisplayIndex == _dragInsertBeforeDisplayIndex)
                {
                    e.Graphics.DrawLine(pen, e.Bounds.Left, e.Bounds.Top, e.Bounds.Left, e.Bounds.Bottom);
                }
                else if (_dragInsertBeforeDisplayIndex == Columns.Count && e.Header.DisplayIndex == Columns.Count - 1)
                {
                    e.Graphics.DrawLine(pen, e.Bounds.Right - 1, e.Bounds.Top, e.Bounds.Right - 1, e.Bounds.Bottom);
                }
            }
        }

        private static void OnDrawItem(object sender, DrawListViewItemEventArgs e)
        {
            // Details view paints complete rows in OnDrawSubItem.
        }

        private void OnDrawSubItem(object sender, DrawListViewSubItemEventArgs e)
        {
            // e.Bounds is only reliable for whichever column the native
            // control drew FIRST in a given paint pass (normally the
            // visually leftmost one) - for every other column, once columns
            // have been reordered at least once, e.Bounds can report stale
            // position/size left over from a previous layout. Confirmed by
            // instrumenting every draw call after swapping two columns: text
            // itself (e.SubItem / e.Item.SubItems[e.ColumnIndex]) was always
            // correct, but relying on e.Bounds for the non-leftmost swapped
            // column drew it somewhere invisible - the reported symptom
            // wasn't wrong data, it was a blank cell. So bounds are computed
            // from scratch here for every column - left edge is the item's
            // own left edge plus the width of every column with a smaller
            // DisplayIndex, exactly mirroring GetColumnIndexAtX's model of
            // the current visual layout - rather than trusted from the event
            // at all; only e.Bounds.Top/.Height (unaffected by column order)
            // are still used.
            var bounds = GetSubItemBounds(e.Item, Columns[e.ColumnIndex], e.Bounds);

            var baseBackColor = e.ItemIndex % 2 == 0 ? _rowBackColor : _alternateRowBackColor;
            using (var background = new SolidBrush(baseBackColor))
            {
                e.Graphics.FillRectangle(background, bounds);
            }

            if (IsCellSelected(e.ItemIndex, e.ColumnIndex))
            {
                using (var overlay = new SolidBrush(SelectionOverlayColor))
                {
                    e.Graphics.FillRectangle(overlay, bounds);
                }
            }

            var text = e.ColumnIndex < e.Item.SubItems.Count ? e.Item.SubItems[e.ColumnIndex].Text : string.Empty;

            TextRenderer.DrawText(
                e.Graphics,
                text,
                e.Item.Font ?? Font,
                new Rectangle(bounds.X + 6, bounds.Y, Math.Max(0, bounds.Width - 9), bounds.Height),
                e.Item.ForeColor,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);
        }

        // The item's own left edge is always correct regardless of column
        // order (it's the row's bounds, not any one column's) - from there,
        // every column with a smaller DisplayIndex than the target column
        // contributes its full width, giving the target's true current
        // on-screen left edge without depending on e.Bounds at all.
        private Rectangle GetSubItemBounds(ListViewItem item, ColumnHeader column, Rectangle fallbackVerticalBounds)
        {
            var left = item.Bounds.Left;
            foreach (ColumnHeader other in Columns)
            {
                if (other.DisplayIndex < column.DisplayIndex)
                {
                    left += other.Width;
                }
            }

            return new Rectangle(left, fallbackVerticalBounds.Top, column.Width, fallbackVerticalBounds.Height);
        }

        // _anchorColumn/_activeColumn are tracked in DISPLAY order (visual
        // left-to-right position), not the column's own data index - so a
        // dragged selection rectangle stays visually correct regardless of
        // whether columns have been reordered. dataColumnIndex (as reported
        // by the ownerdraw events) is converted to its current display
        // position before comparing.
        private bool IsCellSelected(int row, int dataColumnIndex)
        {
            if (_anchorRow < 0 || dataColumnIndex < 0 || dataColumnIndex >= Columns.Count)
            {
                return false;
            }

            var displayIndex = Columns[dataColumnIndex].DisplayIndex;
            var rowStart = Math.Min(_anchorRow, _activeRow);
            var rowEnd = Math.Max(_anchorRow, _activeRow);
            var columnStart = Math.Min(_anchorColumn, _activeColumn);
            var columnEnd = Math.Max(_anchorColumn, _activeColumn);
            return row >= rowStart && row <= rowEnd && displayIndex >= columnStart && displayIndex <= columnEnd;
        }

        private void OnListViewMouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left)
            {
                return;
            }

            // Require an actual item hit here (not the clamped fallback
            // GetRowIndexAtY uses for an in-progress drag) - a click that
            // isn't over a real row is most often the column header (whose
            // own drag-reorder tracking lives entirely in
            // HeaderInputSubclass, since header clicks land on that
            // separate native child window and never reach this handler at
            // all) and must never disturb a cell selection.
            var hitTest = HitTest(e.Location);
            if (hitTest.Item == null)
            {
                // Clicking below the last row (empty space in the list) clears
                // the current selection, same as clicking outside a selection
                // in a spreadsheet. A click in the header area (above the
                // first row, or when there are no rows at all) is left alone.
                if (Items.Count > 0 && e.Location.Y > Items[Items.Count - 1].Bounds.Bottom)
                {
                    ClearSelection();
                }

                return;
            }

            var row = hitTest.Item.Index;
            var column = GetColumnIndexAtX(e.Location.X);
            var extendExisting = ModifierKeys == (Keys.Control | Keys.Alt) && _anchorRow >= 0;
            if (extendExisting)
            {
                _activeRow = row;
                _activeColumn = column;
            }
            else if (IsCellInCurrentSelection(row, column))
            {
                // Clicking a cell that's already selected toggles it back off,
                // instead of re-selecting the same single cell.
                ClearSelection();
                return;
            }
            else
            {
                _anchorRow = row;
                _anchorColumn = column;
                _activeRow = row;
                _activeColumn = column;
                _isDragSelecting = true;
            }

            Invalidate();
        }

        // Same rectangle test as IsCellSelected, but takes a column already
        // expressed in DISPLAY order (as produced by GetColumnIndexAtX)
        // instead of a data column index.
        private bool IsCellInCurrentSelection(int row, int displayColumn)
        {
            if (_anchorRow < 0)
            {
                return false;
            }

            var rowStart = Math.Min(_anchorRow, _activeRow);
            var rowEnd = Math.Max(_anchorRow, _activeRow);
            var columnStart = Math.Min(_anchorColumn, _activeColumn);
            var columnEnd = Math.Max(_anchorColumn, _activeColumn);
            return row >= rowStart && row <= rowEnd && displayColumn >= columnStart && displayColumn <= columnEnd;
        }

        private void ClearSelection()
        {
            _anchorRow = -1;
            _anchorColumn = -1;
            _activeRow = -1;
            _activeColumn = -1;
            _isDragSelecting = false;
            Invalidate();
        }

        private void OnListViewMouseMove(object sender, MouseEventArgs e)
        {
            if (!_isDragSelecting || (e.Button & MouseButtons.Left) == 0)
            {
                return;
            }

            var row = GetRowIndexAtY(e.Location.Y);
            if (row < 0)
            {
                return;
            }

            var column = GetColumnIndexAtX(e.Location.X);
            if (row == _activeRow && column == _activeColumn)
            {
                return;
            }

            _activeRow = row;
            _activeColumn = column;
            Invalidate();
        }

        private void OnListViewMouseUp(object sender, MouseEventArgs e)
        {
            _isDragSelecting = false;
        }

        // Called from HeaderInputSubclass once a header click has moved
        // past the drag threshold - a header click lands on the header's
        // own native child window, not on this control, so detecting the
        // click/threshold has to happen there (see HeaderInputSubclass).
        // From here on, though, tracking the rest of the drag is handed
        // off to WinForms' own DoDragDrop/OnDragOver/OnDragDrop, the same
        // mechanism StyledListBox already uses successfully for its own
        // item-reorder drag - it runs as a native OLE drag-drop operation
        // independent of which specific child window the cursor happens to
        // be over, sidestepping the whole header-hwnd-ownership problem
        // that made hand-rolled WM_MOUSEMOVE/SetCapture tracking (an
        // earlier attempt here) unreliable.
        private void BeginColumnDragDrop(int columnIndex)
        {
            _dragColumnIndex = columnIndex;
            _isDraggingColumn = true;
            _dragInsertBeforeDisplayIndex = -1;

            try
            {
                DoDragDrop(columnIndex, DragDropEffects.Move);
            }
            finally
            {
                // Covers every way the drag can end, including a cancelled
                // drag (Escape, or dropped somewhere OnDragDrop never
                // fires) - OnDragDrop itself only needs to perform the
                // actual move, not reset this shared state.
                _isDraggingColumn = false;
                _dragColumnIndex = -1;
                _dragInsertBeforeDisplayIndex = -1;
                InvalidateHeader();
            }
        }

        // Invalidate() alone only reaches this control's own client area -
        // the header is a distinct native child window (see
        // HeaderInputSubclass), so without this the drag insertion line
        // never actually gets painted (OnDrawColumnHeader simply wouldn't
        // be called again) even though the underlying drag/drop tracking
        // itself works fine.
        private void InvalidateHeader()
        {
            Invalidate();
            _headerInputSubclass?.InvalidateHeaderNow();
        }

        protected override void OnDragOver(DragEventArgs drgevent)
        {
            var point = PointToClient(new Point(drgevent.X, drgevent.Y));
            var insertBefore = GetColumnDropInsertionIndex(point.X);

            drgevent.Effect = DragDropEffects.Move;

            if (insertBefore != _dragInsertBeforeDisplayIndex)
            {
                _dragInsertBeforeDisplayIndex = insertBefore;
                InvalidateHeader();
            }

            base.OnDragOver(drgevent);
        }

        protected override void OnDragDrop(DragEventArgs drgevent)
        {
            if (_dragColumnIndex >= 0 && _dragInsertBeforeDisplayIndex >= 0)
            {
                MoveColumnToDisplayIndex(_dragColumnIndex, _dragInsertBeforeDisplayIndex);
            }

            base.OnDragDrop(drgevent);
        }

        protected override void OnGiveFeedback(GiveFeedbackEventArgs gfbevent)
        {
            gfbevent.UseDefaultCursors = false;
            Cursor.Current = Cursors.SizeWE;

            base.OnGiveFeedback(gfbevent);
        }

        // insertBeforeDisplayIndex is expressed in the ORIGINAL display
        // order (before the dragged column is removed from its old slot) -
        // the standard "move to before index P" -> "target index" adjustment
        // (subtract one if P is past the column's own current position) is
        // needed because DisplayIndex's setter moves the column to an
        // absolute position, and removing it from its old slot first would
        // shift everything after that slot left by one.
        private void MoveColumnToDisplayIndex(int columnIndex, int insertBeforeDisplayIndex)
        {
            if (columnIndex < 0 || columnIndex >= Columns.Count)
            {
                return;
            }

            var column = Columns[columnIndex];
            var originalDisplayIndex = column.DisplayIndex;
            var targetDisplayIndex = insertBeforeDisplayIndex > originalDisplayIndex
                ? insertBeforeDisplayIndex - 1
                : insertBeforeDisplayIndex;

            if (targetDisplayIndex == originalDisplayIndex)
            {
                return;
            }

            column.DisplayIndex = targetDisplayIndex;

            // Mirrors the native ColumnReordered handler this replaced -
            // deferring one tick keeps "which column is now rightmost" (see
            // GetEffectiveFillColumnIndex) accurate once DisplayIndex has
            // actually settled.
            BeginInvoke(new MethodInvoker(ApplyFillColumn));
        }

        // The resize grip (a few pixels either side of a column boundary)
        // is left entirely to the native header - only clicks clearly
        // inside a column's body start our own reorder drag, so resizing
        // (still native, unaffected by AllowColumnReorder) isn't
        // accidentally hijacked into a reorder attempt.
        private bool IsNearColumnBorder(int x)
        {
            const int resizeGripWidth = 5;
            var cumulativeWidth = 0;
            foreach (var column in GetColumnsInDisplayOrder())
            {
                cumulativeWidth += column.Width;
                if (Math.Abs(x - cumulativeWidth) <= resizeGripWidth)
                {
                    return true;
                }
            }

            return false;
        }

        // Where a column dropped at x would be inserted, expressed as
        // "insert before this display index" - the boundary flips at each
        // column's midpoint rather than its edges, so the insertion line
        // snaps to whichever side of the hovered column the cursor is
        // actually closer to (matching the feel of the native drag this
        // replaced), not just "whichever column the cursor is over".
        private int GetColumnDropInsertionIndex(int x)
        {
            var orderedColumns = GetColumnsInDisplayOrder();
            var cumulativeWidth = 0;
            for (var displayIndex = 0; displayIndex < orderedColumns.Count; displayIndex++)
            {
                var columnWidth = orderedColumns[displayIndex].Width;
                if (x < cumulativeWidth + columnWidth / 2)
                {
                    return displayIndex;
                }

                cumulativeWidth += columnWidth;
            }

            return orderedColumns.Count;
        }

        // Shows the full cell text on hover whenever OnDrawSubItem would
        // have had to ellipsize it - the same 6px left / 3px right padding
        // it draws text with is subtracted here to decide if it actually
        // overflows the column.
        private void OnListViewMouseMoveForToolTip(object sender, MouseEventArgs e)
        {
            var hitTest = HitTest(e.Location);
            if (hitTest.Item == null)
            {
                HideCellToolTip();
                return;
            }

            var row = hitTest.Item.Index;
            var displayColumn = GetColumnIndexAtX(e.Location.X);
            if (row == _toolTipRow && displayColumn == _toolTipDisplayColumn)
            {
                return;
            }

            _toolTipRow = row;
            _toolTipDisplayColumn = displayColumn;

            var orderedColumns = GetColumnsInDisplayOrder();
            if (displayColumn < 0 || displayColumn >= orderedColumns.Count)
            {
                _cellToolTip.Hide(this);
                return;
            }

            var column = orderedColumns[displayColumn];
            var item = hitTest.Item;
            var text = column.Index < item.SubItems.Count ? item.SubItems[column.Index].Text : string.Empty;

            if (string.IsNullOrEmpty(text) || !IsTextTruncated(text, item.Font ?? Font, column.Width))
            {
                _cellToolTip.Hide(this);
                return;
            }

            _cellToolTip.Show(text, this, e.Location.X + 12, e.Location.Y + 18, 8000);
        }

        private void OnListViewMouseLeave(object sender, EventArgs e)
        {
            HideCellToolTip();
        }

        private void HideCellToolTip()
        {
            _toolTipRow = -1;
            _toolTipDisplayColumn = -1;
            _cellToolTip.Hide(this);
        }

        private static bool IsTextTruncated(string text, Font font, int columnWidth)
        {
            var availableWidth = columnWidth - 9;
            if (availableWidth <= 0)
            {
                return true;
            }

            var measured = TextRenderer.MeasureText(
                text,
                font,
                new Size(int.MaxValue, int.MaxValue),
                TextFormatFlags.Left | TextFormatFlags.NoPadding | TextFormatFlags.SingleLine);
            return measured.Width > availableWidth;
        }

        private void OnListViewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Control && e.Shift && e.KeyCode == Keys.C)
            {
                CopySelectionAsTable();
                e.Handled = true;
                e.SuppressKeyPress = true;
            }
            else if (e.Control && e.KeyCode == Keys.C)
            {
                CopySelection();
                e.Handled = true;
                e.SuppressKeyPress = true;
            }
            else if (e.Control && e.KeyCode == Keys.A)
            {
                SelectAll();
                e.Handled = true;
                e.SuppressKeyPress = true;
            }
            else if (e.KeyCode == Keys.Up || e.KeyCode == Keys.Down || e.KeyCode == Keys.Left || e.KeyCode == Keys.Right)
            {
                if (MoveSelectionWithArrowKey(e.KeyCode, e.Shift))
                {
                    e.Handled = true;
                    e.SuppressKeyPress = true;
                }
            }
        }

        // Plain arrow key moves a single-cell selection by one row/column
        // (like clicking a neighboring cell). Shift+arrow instead extends
        // the active corner while the anchor stays put, same as Shift+click
        // would - so a range can be built up without touching the mouse.
        private bool MoveSelectionWithArrowKey(Keys key, bool extendSelection)
        {
            if (Items.Count == 0 || Columns.Count == 0)
            {
                return false;
            }

            int row;
            int column;
            if (_anchorRow < 0)
            {
                row = 0;
                column = 0;
            }
            else
            {
                row = _activeRow;
                column = _activeColumn;
                switch (key)
                {
                    case Keys.Up:
                        row = Math.Max(0, row - 1);
                        break;
                    case Keys.Down:
                        row = Math.Min(Items.Count - 1, row + 1);
                        break;
                    case Keys.Left:
                        column = Math.Max(0, column - 1);
                        break;
                    case Keys.Right:
                        column = Math.Min(Columns.Count - 1, column + 1);
                        break;
                }
            }

            if (extendSelection && _anchorRow >= 0)
            {
                _activeRow = row;
                _activeColumn = column;
            }
            else
            {
                _anchorRow = row;
                _anchorColumn = column;
                _activeRow = row;
                _activeColumn = column;
            }

            if (row >= 0 && row < Items.Count)
            {
                Items[row].EnsureVisible();
            }

            Invalidate();
            return true;
        }

        private void SelectAll()
        {
            if (Items.Count == 0 || Columns.Count == 0)
            {
                return;
            }

            _anchorRow = 0;
            _anchorColumn = 0;
            _activeRow = Items.Count - 1;
            _activeColumn = Columns.Count - 1;
            Invalidate();
        }

        private int GetRowIndexAtY(int y)
        {
            if (Items.Count == 0)
            {
                return -1;
            }

            var hitTest = HitTest(new Point(1, y));
            if (hitTest.Item != null)
            {
                return hitTest.Item.Index;
            }

            // Dragging above the first row or below the last one still
            // extends the selection to that end, the same way a
            // spreadsheet does when a drag leaves the visible grid.
            return y < Items[0].Bounds.Top ? 0 : Items.Count - 1;
        }

        // Returns a DISPLAY index (position in visual column order), to match
        // how _anchorColumn/_activeColumn and IsCellSelected are tracked -
        // necessary so drag-selection stays visually correct after columns
        // have been reordered.
        private int GetColumnIndexAtX(int x)
        {
            var orderedColumns = GetColumnsInDisplayOrder();
            var cumulativeWidth = 0;
            for (var displayIndex = 0; displayIndex < orderedColumns.Count; displayIndex++)
            {
                cumulativeWidth += orderedColumns[displayIndex].Width;
                if (x < cumulativeWidth)
                {
                    return displayIndex;
                }
            }

            return Math.Max(0, orderedColumns.Count - 1);
        }

        private void CopySelection()
        {
            CopySelectionCore(includeHeader: false);
        }

        // Same selected rectangle as CopySelection, but with a leading row of
        // column captions - so the clipboard content pastes as a proper
        // table (e.g. into a spreadsheet) instead of bare data rows.
        private void CopySelectionAsTable()
        {
            CopySelectionCore(includeHeader: true);
        }

        private void CopySelectionCore(bool includeHeader)
        {
            if (_anchorRow < 0)
            {
                return;
            }

            var rowStart = Math.Min(_anchorRow, _activeRow);
            var rowEnd = Math.Min(Math.Max(_anchorRow, _activeRow), Items.Count - 1);

            // _anchorColumn/_activeColumn are DISPLAY indices; map the
            // selected display range back to actual data columns before
            // indexing SubItems, so copying still lines up correctly after
            // the user has dragged columns into a different order.
            var orderedColumns = GetColumnsInDisplayOrder();
            var columnStart = Math.Min(_anchorColumn, _activeColumn);
            var columnEnd = Math.Min(Math.Max(_anchorColumn, _activeColumn), orderedColumns.Count - 1);

            List<string> headerCells = null;
            if (includeHeader)
            {
                headerCells = new List<string>();
                for (var displayColumn = columnStart; displayColumn <= columnEnd; displayColumn++)
                {
                    headerCells.Add(orderedColumns[displayColumn].Text);
                }
            }

            var plainTextBuilder = new StringBuilder();
            if (headerCells != null)
            {
                plainTextBuilder.AppendLine(string.Join("\t", headerCells));
            }

            var rows = new List<List<string>>();
            var cellCount = 0;
            for (var row = rowStart; row <= rowEnd; row++)
            {
                var item = Items[row];
                var cells = new List<string>();
                for (var displayColumn = columnStart; displayColumn <= columnEnd; displayColumn++)
                {
                    var dataColumn = orderedColumns[displayColumn].Index;
                    cells.Add(dataColumn < item.SubItems.Count ? item.SubItems[dataColumn].Text : string.Empty);
                    cellCount++;
                }

                rows.Add(cells);
                plainTextBuilder.AppendLine(string.Join("\t", cells));
            }

            if (cellCount == 0)
            {
                return;
            }

            // Plain text is the universal fallback (any app that only reads
            // text gets the tab-separated rows, exactly as before). "HTML
            // Format" rides alongside it on the same clipboard payload so
            // that apps which understand it - Word, Outlook, browsers,
            // Excel, most chat/notes apps - paste an actual bordered table
            // instead of raw tab characters.
            var dataObject = new DataObject();
            dataObject.SetText(plainTextBuilder.ToString(), TextDataFormat.UnicodeText);
            dataObject.SetData(DataFormats.Html, BuildCfHtmlTable(headerCells, rows));
            Clipboard.SetDataObject(dataObject, true);

            var message = cellCount == 1
                ? UIStrings.Get("ListView.CellCopied")
                : string.Format(UIStrings.Get("ListView.CellsCopied"), cellCount);
            ShowCopyToast(includeHeader ? message + UIStrings.Get("ListView.WithHeaderSuffix") : message);
        }

        // Wraps an HTML <table> in the CF_HTML clipboard envelope Windows
        // requires (Version/StartHTML/EndHTML/StartFragment/EndFragment byte
        // offsets around an <html><body> shell). See the CF_HTML spec - the
        // offsets are byte counts, and .NET writes "HTML Format" clipboard
        // data as UTF-8, so they're computed in UTF-8 bytes rather than .NET
        // char counts (matters here since summaries/titles routinely contain
        // German umlauts).
        private static string BuildCfHtmlTable(IReadOnlyList<string> headerCells, IReadOnlyList<List<string>> rows)
        {
            var table = BuildHtmlTable(headerCells, rows);

            const string HeaderTemplate =
                "Version:0.9\r\n" +
                "StartHTML:{0:0000000000}\r\n" +
                "EndHTML:{1:0000000000}\r\n" +
                "StartFragment:{2:0000000000}\r\n" +
                "EndFragment:{3:0000000000}\r\n";
            const string HtmlPrefix = "<html><head><meta charset=\"utf-8\"></head><body><!--StartFragment-->";
            const string HtmlSuffix = "<!--EndFragment--></body></html>";

            // Every offset is zero-padded to a fixed width, so the header's
            // own byte length is identical whether computed from placeholder
            // zeros or from the real (larger) offsets it ends up holding.
            var headerLength = Encoding.UTF8.GetByteCount(string.Format(HeaderTemplate, 0, 0, 0, 0));
            var startHtml = headerLength;
            var startFragment = startHtml + Encoding.UTF8.GetByteCount(HtmlPrefix);
            var endFragment = startFragment + Encoding.UTF8.GetByteCount(table);
            var endHtml = endFragment + Encoding.UTF8.GetByteCount(HtmlSuffix);

            return string.Format(HeaderTemplate, startHtml, endHtml, startFragment, endFragment) + HtmlPrefix + table + HtmlSuffix;
        }

        private static string BuildHtmlTable(IReadOnlyList<string> headerCells, IReadOnlyList<List<string>> rows)
        {
            var builder = new StringBuilder();
            builder.Append("<table style=\"border-collapse:collapse;font-family:Segoe UI,sans-serif;font-size:9pt;\">");

            if (headerCells != null)
            {
                builder.Append("<tr>");
                foreach (var cell in headerCells)
                {
                    builder.Append("<th style=\"border:1px solid #999;padding:4px 8px;background:#eee;text-align:left;\">");
                    builder.Append(WebUtility.HtmlEncode(cell));
                    builder.Append("</th>");
                }

                builder.Append("</tr>");
            }

            foreach (var row in rows)
            {
                builder.Append("<tr>");
                foreach (var cell in row)
                {
                    builder.Append("<td style=\"border:1px solid #999;padding:4px 8px;\">");
                    builder.Append(WebUtility.HtmlEncode(cell));
                    builder.Append("</td>");
                }

                builder.Append("</tr>");
            }

            builder.Append("</table>");
            return builder.ToString();
        }

        private void ShowCopyToast(string message)
        {
            switch (CopyConfirmation)
            {
                case CopyConfirmationStyle.None:
                    return;

                case CopyConfirmationStyle.ToolTip:
                    // Reuses the same ToolTip instance already used for
                    // overflow-text hover previews elsewhere in this class,
                    // rather than owning a second ToolTip component.
                    _cellToolTip.Show(message, this, 12, 12, 2000);
                    return;

                default:
                    var owner = FindForm();
                    if (owner != null)
                    {
                        ToastForm.ShowToast(message, owner);
                    }
                    return;
            }
        }

        private static Color Darken(Color color, int amount)
        {
            return Color.FromArgb(
                Math.Max(0, color.R - amount),
                Math.Max(0, color.G - amount),
                Math.Max(0, color.B - amount));
        }

        // Subclasses the ListView's own header child window (class
        // "SysHeader32") purely to notice a click/drag START in the header
        // - a click there lands on this separate native child window, not
        // on the ListView itself, confirmed the hard way: this control's
        // own MouseDown/MouseMove events never fired for a header click at
        // all. Once the drag threshold is exceeded, this hands off
        // entirely to StyledListView.BeginColumnDragDrop (WinForms'
        // DoDragDrop/OnDragOver/OnDragDrop, the same mechanism
        // StyledListBox already uses for its own item-reorder drag) rather
        // than continuing to track raw mouse messages here - an earlier
        // attempt at hand-rolled SetCapture-based tracking fought a losing
        // battle against the native header repeatedly releasing capture on
        // its own initiative. Everything else about the header (background,
        // text, resizing, owner-draw) is untouched.
        private sealed class HeaderInputSubclass : NativeWindow
        {
            private const int WM_LBUTTONDOWN = 0x0201;
            private const int WM_MOUSEMOVE = 0x0200;
            private const int WM_LBUTTONUP = 0x0202;
            private const int LVM_FIRST = 0x1000;
            private const int LVM_GETHEADER = LVM_FIRST + 31;

            private readonly StyledListView _owner;
            private int _pendingColumnIndex = -1;
            private int _pendingStartX;

            private const uint RDW_INVALIDATE = 0x0001;
            private const uint RDW_ERASE = 0x0004;
            private const uint RDW_UPDATENOW = 0x0100;

            [System.Runtime.InteropServices.DllImport("user32.dll")]
            private static extern System.IntPtr SendMessage(System.IntPtr hWnd, int msg, System.IntPtr wParam, System.IntPtr lParam);

            [System.Runtime.InteropServices.DllImport("user32.dll")]
            private static extern bool RedrawWindow(System.IntPtr hWnd, System.IntPtr lprcUpdate, System.IntPtr hrgnUpdate, uint flags);

            public HeaderInputSubclass(StyledListView owner)
            {
                _owner = owner;
            }

            public static System.IntPtr GetHeaderHandle(System.IntPtr listViewHandle)
            {
                return SendMessage(listViewHandle, LVM_GETHEADER, System.IntPtr.Zero, System.IntPtr.Zero);
            }

            // The header is a separate native child window - this
            // control's own Invalidate() only schedules a repaint for the
            // ListView's own client area, which doesn't reach a distinct
            // child hwnd, so it alone never gets OnDrawColumnHeader (and
            // therefore the drag insertion line) to actually redraw during
            // a drag. RedrawWindow with RDW_UPDATENOW forces the header to
            // repaint immediately rather than just marking it dirty for
            // whenever it next happens to paint on its own.
            public void InvalidateHeaderNow()
            {
                RedrawWindow(Handle, System.IntPtr.Zero, System.IntPtr.Zero, RDW_INVALIDATE | RDW_ERASE | RDW_UPDATENOW);
            }

            protected override void WndProc(ref Message m)
            {
                base.WndProc(ref m);

                switch (m.Msg)
                {
                    case WM_LBUTTONDOWN:
                        OnMouseDown(GetX(m.LParam));
                        break;

                    case WM_MOUSEMOVE:
                        OnMouseMove(GetX(m.LParam));
                        break;

                    case WM_LBUTTONUP:
                        // A plain click (never exceeded the threshold) just
                        // clears the pending state - nothing to reorder,
                        // nothing to undo, since BeginColumnDragDrop is
                        // only ever called after OnMouseMove sees the
                        // threshold exceeded.
                        _pendingColumnIndex = -1;
                        break;
                }
            }

            private static int GetX(System.IntPtr lParam)
            {
                return unchecked((short)((long)lParam & 0xFFFF));
            }

            private void OnMouseDown(int x)
            {
                _pendingColumnIndex = -1;

                if (_owner.Columns.Count == 0 || _owner.IsNearColumnBorder(x))
                {
                    return;
                }

                var displayIndex = _owner.GetColumnIndexAtX(x);
                var orderedColumns = _owner.GetColumnsInDisplayOrder();
                if (displayIndex < 0 || displayIndex >= orderedColumns.Count)
                {
                    return;
                }

                var clickedColumn = orderedColumns[displayIndex];
                if (!_owner.IsColumnReorderable(clickedColumn.Index))
                {
                    return;
                }

                _pendingColumnIndex = clickedColumn.Index;
                _pendingStartX = x;
            }

            private void OnMouseMove(int x)
            {
                if (_pendingColumnIndex < 0 || _owner._isDraggingColumn)
                {
                    return;
                }

                if (Math.Abs(x - _pendingStartX) < SystemInformation.DragSize.Width)
                {
                    return;
                }

                var columnIndex = _pendingColumnIndex;
                _pendingColumnIndex = -1;

                // DoDragDrop blocks for the duration of the drag, running
                // its own internal message loop - this call doesn't return
                // until the drag ends one way or another.
                _owner.BeginColumnDragDrop(columnIndex);
            }
        }
    }
}
