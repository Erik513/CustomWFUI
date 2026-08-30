using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Text;
using System.Windows.Forms;
using ErikwnkWFUI.Forms;
using ErikwnkWFUI.Styles;

namespace ErikwnkWFUI.Controls
{
    /// <summary>How <see cref="Controls.ListView"/> confirms a Ctrl+C/Ctrl+Shift+C copy.</summary>
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
    /// <remarks>
    /// Named the same as its own base class, same as
    /// <see cref="Controls.DataGridView"/> and <see cref="Controls.ListBox"/>
    /// - the base type reference below stays fully qualified so the class
    /// doesn't try to inherit from itself.
    /// </remarks>
    public class ListView : System.Windows.Forms.ListView
    {
        private const int DefaultMinimumColumnWidth = 40;

        private int _anchorRow = -1;
        private int _anchorColumn = -1;
        private int _activeRow = -1;
        private int _activeColumn = -1;
        private bool _isDragSelecting;
        private int _headerHeight = 24;
        private bool _isPollTrackingPress;
        private bool _isPolledDragSelecting;
        private Point _pollPressPoint;
        private bool _wasLeftButtonDownLastPoll;
        private bool _isHeaderPressActive;
        private bool _isContextMenuOpen;
        private bool _isScrollBarPressActive;
        private Point _selectionAnchorPoint;
        private Point _selectionCurrentPoint;
        private readonly Timer _externalDragPollTimer;
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
        private bool _allowColumnReordering = true;
        private bool _allowColumnResizing = true;
        private Color _columnReorderIndicatorColorOverride;
        private bool _columnReorderIndicatorColorIsOverridden;
        private bool _isDraggingColumn;
        private int _dragColumnIndex = -1;
        private int _dragInsertBeforeDisplayIndex = -1;
        private Font _headerFontOverride;
        private ImageList _rowHeightImageList;
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
        /// ListBox's DragIndicatorColor before the same fix), so an
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
        /// below) rather than using <see cref="System.Windows.Forms.ListView.AllowColumnReorder"/>
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

        /// <summary>
        /// Font for the column header row - defaults to a bold version of
        /// <see cref="Control.Font"/> if never set. Setting this to a larger
        /// size is also the way to make the native header band itself
        /// taller (there's no direct "header height" API on the underlying
        /// Win32 header control - it sizes itself from its own font
        /// metrics, same as row height below).
        /// </summary>
        public Font HeaderFont
        {
            // Always a fresh instance - OnDrawColumnHeader disposes whatever
            // it gets from this getter after each paint, which would
            // silently dispose the caller's own Font object (breaking every
            // paint after the first) if this ever handed that instance back
            // directly instead of a clone.
            get { return _headerFontOverride != null ? (Font)_headerFontOverride.Clone() : new Font(Font, FontStyle.Bold); }
            set
            {
                _headerFontOverride = value;
                Invalidate();
            }
        }

        /// <summary>
        /// Forces every row to this exact height in pixels. Unset (0) lets
        /// rows size themselves from <see cref="Control.Font"/> as usual.
        /// Implemented via the classic Win32 trick of assigning a 1px-wide,
        /// N-tall <c>SmallImageList</c> - the native ListView derives its
        /// row height from the small image list's height when one is set,
        /// since there's no direct row-height API either. Don't also assign
        /// a real <c>SmallImageList</c> for per-item icons while this is
        /// set; the two would fight over the same slot.
        /// </summary>
        public int RowHeight
        {
            get { return _rowHeightImageList?.ImageSize.Height ?? 0; }
            set
            {
                if (value <= 0)
                {
                    _rowHeightImageList = null;
                    SmallImageList = null;
                    return;
                }

                _rowHeightImageList = new ImageList { ImageSize = new Size(1, value) };
                SmallImageList = _rowHeightImageList;
            }
        }

        public ListView()
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

            // Polls rather than relying on this control's own MouseMove/
            // MouseUp for the "drag started outside this control entirely"
            // case (see OnExternalDragPollTick) - a plain MouseMove-based
            // approach was tried first and never actually fired, because
            // WinForms implicitly captures the mouse for whatever control
            // the button-down happened on, so this control never receives
            // mouse messages for a drag it didn't itself start. Polling
            // Control.MouseButtons/Cursor.Position instead sidesteps
            // capture ownership entirely, since those reflect true global
            // input state rather than routed messages.
            _externalDragPollTimer = new Timer { Interval = 25 };
            _externalDragPollTimer.Tick += OnExternalDragPollTick;
            _externalDragPollTimer.Start();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _cellToolTip.Dispose();
                _externalDragPollTimer.Stop();
                _externalDragPollTimer.Dispose();
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
        /// <summary>
        /// Master switch for whether ANY column can be resized at all -
        /// simpler than calling <see cref="SetColumnResizable"/> for every
        /// column when the answer is "none of them". Per-column overrides
        /// via <see cref="SetColumnResizable"/> still apply among whichever
        /// columns this allows; setting this false overrides all of them
        /// (nothing becomes resizable no matter what they say). Defaults to
        /// true, matching this control's previous unconditional behavior.
        /// </summary>
        public bool AllowColumnResizing
        {
            get => _allowColumnResizing;
            set => _allowColumnResizing = value;
        }

        /// <summary>
        /// Master switch for whether ANY column can be dragged to reorder
        /// it at all - simpler than calling <see cref="SetColumnReorderable"/>
        /// for every column when the answer is "none of them". Per-column
        /// overrides via <see cref="SetColumnReorderable"/> still apply
        /// among whichever columns this allows; setting this false
        /// overrides all of them. Defaults to true, matching this control's
        /// previous unconditional behavior.
        /// </summary>
        public bool AllowColumnReordering
        {
            get => _allowColumnReordering;
            set => _allowColumnReordering = value;
        }

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
            return _allowColumnResizing && !_nonResizableColumns.Contains(columnIndex);
        }

        /// <summary>
        /// Configurable per column, independent of <see cref="AllowColumnReordering"/>:
        /// a column can be pinned in place (e.g. a leading "Time"/"Metric"
        /// column that should always stay leftmost) while the rest can
        /// still be freely dragged into a new order.
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
            return _allowColumnReordering && !_nonReorderableColumns.Contains(columnIndex);
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

            // Guards against a one-off glitch seen on the very first theme
            // switch that rebuilds this control (not on plain construction) -
            // the initial WM_PAINT right after a handle is (re)created can
            // land before layout/theme colors have fully settled, so the
            // outer border drawn there (see WndProc) could momentarily use
            // stale values. Deferring one tick, the same way ApplyFillColumn
            // already gets deferred elsewhere in this class after a native
            // reorder, guarantees at least one more repaint once everything
            // has actually settled, without needing the user to trigger a
            // second redraw themselves (e.g. by resizing).
            BeginInvoke(new MethodInvoker(Invalidate));
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

        // BorderStyle is None - this draws a light frame around the whole
        // control instead, matching the border each header column already
        // gets (OnDrawColumnHeader). Since the list content itself is
        // natively painted (WM_PAINT bypasses .NET's owner-draw pipeline
        // for everything except what DrawItem/DrawSubItem/DrawColumnHeader
        // already hook), the border can't be added via OnPaint either -
        // it's drawn straight onto the client dc right after the native
        // paint finishes, the same technique used to fix a stray native
        // border on ProgressBar earlier in this codebase's history.
        private const int WM_PAINT = 0x000F;

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern System.IntPtr GetDC(System.IntPtr hWnd);

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern int ReleaseDC(System.IntPtr hWnd, System.IntPtr hDC);

        // Used by OnExternalDragPollTick to recognize a press on this
        // control's own native scrollbar (vertical or horizontal) - those
        // live in the non-client area, at a position the poll would
        // otherwise see only as "a fresh press that isn't on a real cell"
        // and wrongly treat as a click that should clear the selection
        // (same class of problem the header/context-menu checks below
        // already solve, just not previously covered for the scrollbar).
        // A first attempt asked Windows itself via WM_NCHITTEST, but that
        // didn't actually work here (confirmed by the user still seeing
        // the selection clear) - the geometric check below is more direct
        // and doesn't depend on that message being routed/answered the way
        // a plain, non-owner-drawn control would: the native scrollbar's
        // non-client strip is exactly the gap between ClientSize (the area
        // rows are actually laid out in) and the control's own full Size
        // (its outer bounds, scrollbar included), so a point is "on the
        // scrollbar" whenever it falls in that gap.
        private bool IsPointOnScrollBar(Point screenPoint)
        {
            var clientPoint = PointToClient(screenPoint);

            bool onVerticalScrollBar =
                clientPoint.X >= ClientSize.Width && clientPoint.X < Width &&
                clientPoint.Y >= 0 && clientPoint.Y < Height;
            bool onHorizontalScrollBar =
                clientPoint.Y >= ClientSize.Height && clientPoint.Y < Height &&
                clientPoint.X >= 0 && clientPoint.X < Width;

            return onVerticalScrollBar || onHorizontalScrollBar;
        }

        protected override void WndProc(ref Message m)
        {
            base.WndProc(ref m);

            if (m.Msg != WM_PAINT || Width <= 1 || Height <= 1)
            {
                return;
            }

            System.IntPtr dc = GetDC(Handle);
            if (dc == System.IntPtr.Zero)
            {
                return;
            }

            try
            {
                using (Graphics g = Graphics.FromHdc(dc))
                using (Pen pen = new Pen(UIColors.BorderMedium))
                {
                    // Skips the whole header strip (y < _headerHeight)
                    // entirely, not just its top edge - the header sits
                    // there as its own separate native child window,
                    // repainting completely independently of this
                    // WM_PAINT, and OnDrawColumnHeader already draws a full
                    // border around every column through the normal
                    // owner-draw path (no native-paint race possible
                    // there). A real glitch was seen specifically near the
                    // top-left - right where a rectangle spanning the full
                    // height used to overlap that independently-repainting
                    // area - so this leaves that whole strip to the one
                    // place already drawing it correctly, rather than
                    // trying to coexist with it.
                    int top = Math.Min(_headerHeight, Height - 1);
                    g.DrawLine(pen, 0, top, 0, Height - 1);
                    g.DrawLine(pen, Width - 1, top, Width - 1, Height - 1);
                    g.DrawLine(pen, 0, Height - 1, Width - 1, Height - 1);
                }
            }
            finally
            {
                ReleaseDC(Handle, dc);
            }
        }

        // Clicking a cell already clears/replaces the selection on its own
        // (see OnListViewMouseDown), but nothing previously cleared it when
        // focus moved to a completely different control elsewhere in the
        // app - the selected cells stayed highlighted indefinitely even
        // though nothing about them was still relevant. Matches how a
        // spreadsheet's own selection typically doesn't survive switching
        // away to another window/control either.
        protected override void OnLeave(EventArgs e)
        {
            base.OnLeave(e);
            ClearSelection();
        }

        private void OnColumnWidthChanging(object sender, ColumnWidthChangingEventArgs e)
        {
            if (!IsColumnResizable(e.ColumnIndex))
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

        // Re-applies on ANY column's width change, including the fill
        // column's own - previously excluded the fill column itself
        // (assuming a manual resize of it meant "let the user override the
        // fill width"), but that just left it stuck at whatever smaller
        // width the user dragged it to instead of snapping back to fill
        // the leftover space, which is the whole point of it being the
        // fill column in the first place. _isApplyingFillColumn still
        // guards against ApplyFillColumn's own width assignment
        // re-triggering this handler.
        private void OnColumnWidthChanged(object sender, ColumnWidthChangedEventArgs e)
        {
            if (!_isApplyingFillColumn)
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
                _isContextMenuOpen = true;
            };

            // Left-clicking a menu item (including hovering into the
            // "Copy selection"/"Copy all" submenus) is, from
            // OnExternalDragPollTick's perspective, indistinguishable from
            // any other left-button press that isn't on a real cell - it
            // would otherwise clear the selection the instant the menu is
            // clicked, before the item's own Click handler (which reads
            // that same selection) gets a chance to run. _isContextMenuOpen
            // tells the poll tick to stay out of the way entirely while
            // this whole cascading menu is up; Closed only fires once the
            // whole thing (including any open submenu) actually closes.
            menu.Closed += (sender, e) => _isContextMenuOpen = false;

            return menu;
        }

        private void OnDrawColumnHeader(object sender, DrawListViewColumnHeaderEventArgs e)
        {
            _headerHeight = e.Bounds.Height;

            using (var background = new SolidBrush(_headerBackColor))
            using (var divider = new Pen(UIColors.BorderMedium))
            using (var font = HeaderFont)
            {
                e.Graphics.FillRectangle(background, e.Bounds);
                e.Graphics.DrawRectangle(divider, e.Bounds.Left, e.Bounds.Top, e.Bounds.Width - 1, e.Bounds.Height - 1);
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

            // A filled rectangle rather than a DrawLine pen - a pen is
            // centered on its coordinate, so a line drawn exactly at x=0
            // (the very first column's left edge) has half its width
            // clipped off the visible header entirely, making the leftmost
            // position look noticeably thinner than every other one. A
            // rectangle has no such centering ambiguity: it always occupies
            // exactly [x, x + lineWidth), fully visible regardless of which
            // edge it's flush against.
            const int lineWidth = 2;

            using (var brush = new SolidBrush(ColumnReorderIndicatorColor))
            {
                if (e.Header.DisplayIndex == _dragInsertBeforeDisplayIndex)
                {
                    e.Graphics.FillRectangle(brush, e.Bounds.Left, e.Bounds.Top, lineWidth, e.Bounds.Height);
                }
                else if (_dragInsertBeforeDisplayIndex == Columns.Count && e.Header.DisplayIndex == Columns.Count - 1)
                {
                    e.Graphics.FillRectangle(brush, e.Bounds.Right - lineWidth, e.Bounds.Top, lineWidth, e.Bounds.Height);
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

            var width = column.Width;

            // The leftmost column only is inset 1px on its own left edge -
            // reserves that pixel column exclusively for the outer border
            // (see WndProc's own comment on why that border exists), so row
            // content painting can never overwrite it. Without this,
            // something about hovering a row repaints its background and
            // erases the border pixel at x=0 with nothing left to redraw it
            // afterward - confirmed by comparing screenshots before and
            // after hovering the first row. Every other column's bounds
            // are completely unaffected, so this can't misalign anything
            // with the header's own (unshifted) column positions.
            if (column.DisplayIndex == 0)
            {
                left += 1;
                width -= 1;
            }

            return new Rectangle(left, fallbackVerticalBounds.Top, width, fallbackVerticalBounds.Height);
        }

        // _anchorColumn/_activeColumn are tracked in DISPLAY order (visual
        // left-to-right position), not the column's own data index - so a
        // dragged selection rectangle stays visually correct regardless of
        // whether columns have been reordered. dataColumnIndex (as reported
        // by the ownerdraw events) is converted to its current display
        // position before comparing.
        private bool IsCellSelected(int row, int dataColumnIndex)
        {
            if (row < 0 || row >= Items.Count || dataColumnIndex < 0 || dataColumnIndex >= Columns.Count)
            {
                return false;
            }

            // While a mouse drag is actually in progress (either the
            // normal on-cell one, or a poll-driven one - see
            // OnExternalDragPollTick), selection is real pixel-rectangle
            // intersection against the cell's own bounds, matching how
            // Explorer's own rubber-band selection works: a cell counts
            // only if its bounds genuinely overlap the dragged area.
            // Independently clamping row and column index from the
            // cursor's X and Y (the previous approach) could select a cell
            // whose row merely happened to share a Y-coordinate band with
            // the cursor while X was nowhere near any column at all (e.g.
            // dragging somewhere far to the side of the whole table).
            if (_isDragSelecting || _isPolledDragSelecting)
            {
                var cellRect = GetSubItemBounds(Items[row], Columns[dataColumnIndex], Items[row].Bounds);
                return cellRect.IntersectsWith(GetNormalizedSelectionRectangle());
            }

            if (_anchorRow < 0)
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

        // Built from the RAW anchor/current points (never clamped - see
        // their own assignment sites), then intersected with what's
        // actually on screen right now. Intersecting the finished
        // rectangle, rather than clamping each endpoint to the client area
        // beforehand, is the part that actually matters: clamping the
        // endpoints still lets a drag that never touches the visible area
        // at all (e.g. entirely below this control) produce a real,
        // zero-height rectangle sitting exactly on the bottom edge - and a
        // zero-height rectangle sitting ON a boundary still counts as
        // touching (IntersectsWith) whatever row's bounds happen to end
        // there, wrongly selecting the last visible row. Intersecting
        // instead means a rectangle with no genuine overlap collapses to
        // Rectangle.Empty (IntersectsWith nothing at all), while one that
        // does overlap gets trimmed to only the part actually on screen.
        private Rectangle GetNormalizedSelectionRectangle()
        {
            int left = Math.Min(_selectionAnchorPoint.X, _selectionCurrentPoint.X);
            int right = Math.Max(_selectionAnchorPoint.X, _selectionCurrentPoint.X);
            int top = Math.Min(_selectionAnchorPoint.Y, _selectionCurrentPoint.Y);
            int bottom = Math.Max(_selectionAnchorPoint.Y, _selectionCurrentPoint.Y);
            var rawRectangle = Rectangle.FromLTRB(left, top, right, bottom);

            return Rectangle.Intersect(rawRectangle, GetVisibleContentArea());
        }

        // The area rows can actually be painted into right now - below the
        // header strip, within the control's current client size.
        private Rectangle GetVisibleContentArea()
        {
            int top = Math.Min(_headerHeight, ClientSize.Height);
            return new Rectangle(0, top, ClientSize.Width, Math.Max(0, ClientSize.Height - top));
        }

        // Converts the live pixel selection rectangle into the index-based
        // _anchorRow/_anchorColumn/_activeRow/_activeColumn representation
        // once a drag ends, so keyboard navigation (MoveSelectionWithArrowKey),
        // Ctrl+C, and a plain click's "is this cell already selected" check
        // keep working the normal, index-based way afterward - only the
        // live drag itself needs pixel intersection. A rectangle dragged
        // over a regular cell grid always covers a contiguous index range,
        // so tracking the min/max row and display-column index among every
        // intersected cell fully reconstructs it.
        private void FinalizeDragSelection()
        {
            if (Items.Count == 0 || Columns.Count == 0)
            {
                ClearSelection();
                return;
            }

            var selectionRect = GetNormalizedSelectionRectangle();
            var orderedColumns = GetColumnsInDisplayOrder();

            int minRow = -1, maxRow = -1, minColumn = -1, maxColumn = -1;

            for (int row = 0; row < Items.Count; row++)
            {
                var rowBounds = Items[row].Bounds;

                for (int displayIndex = 0; displayIndex < orderedColumns.Count; displayIndex++)
                {
                    var cellRect = GetSubItemBounds(Items[row], orderedColumns[displayIndex], rowBounds);
                    if (!cellRect.IntersectsWith(selectionRect))
                    {
                        continue;
                    }

                    minRow = minRow < 0 ? row : Math.Min(minRow, row);
                    maxRow = Math.Max(maxRow, row);
                    minColumn = minColumn < 0 ? displayIndex : Math.Min(minColumn, displayIndex);
                    maxColumn = Math.Max(maxColumn, displayIndex);
                }
            }

            if (minRow < 0)
            {
                ClearSelection();
                return;
            }

            _anchorRow = minRow;
            _activeRow = maxRow;
            _anchorColumn = minColumn;
            _activeColumn = maxColumn;
        }

        private void OnListViewMouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left)
            {
                return;
            }

            // A real header click never reaches this handler at all - it
            // lands on the header's own separate native child window (see
            // HeaderInputSubclass).
            var hitTest = HitTest(e.Location);
            if (hitTest.Item == null)
            {
                // Handled entirely by OnExternalDragPollTick instead -
                // native mouse events for an "off-item" press turned out
                // unreliable here (confirmed by logging a real attempt:
                // the native ListView fires its own MouseUp almost
                // immediately for such a press, even while the physical
                // button is still held, wiping out any state tracked from
                // MouseDown before a real drag ever got a chance to
                // register). Polling Control.MouseButtons/Cursor.Position
                // instead doesn't depend on this control's own mouse
                // events at all, so it isn't affected by that quirk - and
                // handles a press starting outside this control the same
                // way, uniformly, with no need to tell the two apart.
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
                _selectionAnchorPoint = e.Location;
                _selectionCurrentPoint = _selectionAnchorPoint;
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

            if (e.Location == _selectionCurrentPoint)
            {
                return;
            }

            _selectionCurrentPoint = e.Location;
            Invalidate();
        }

        private void OnListViewMouseUp(object sender, MouseEventArgs e)
        {
            if (_isDragSelecting)
            {
                FinalizeDragSelection();
                Invalidate();
            }

            _isDragSelecting = false;
        }

        // Drives cell selection for every press that ISN'T a direct hit on
        // a real cell - both a press starting in this control's own dead
        // zone (below the last row, beside the last column) and one
        // starting somewhere else in the app entirely, uniformly, with no
        // need to tell the two apart. A direct on-cell press is still
        // handled the normal way, by OnListViewMouseDown/Move/Up - those
        // reliably fire for a genuine item hit, and already have their own
        // richer click behavior (toggle off if already selected,
        // Ctrl+Alt to extend) that only makes sense there.
        //
        // Everything here works off polled global state
        // (Control.MouseButtons/Cursor.Position) instead of this
        // control's own mouse events, because two different event-based
        // attempts both broke on real native quirks: routing through
        // MouseMove never saw a single event for a press that started
        // outside this control (WinForms implicitly captures the mouse
        // for whichever control the button actually went down on, so nothing
        // ever reaches here); and routing through this control's own
        // MouseDown/MouseUp for a dead-zone press broke because the native
        // ListView fires its own MouseUp almost immediately for an
        // "off-item" press, even while the physical button is still held -
        // confirmed by logging a real attempt, where MouseUp landed right
        // after MouseDown at the same point while every MouseMove
        // afterward kept reporting the button as still down. Polling
        // doesn't depend on any of that; it just asks the OS directly,
        // every tick.
        private void OnExternalDragPollTick(object sender, EventArgs e)
        {
            bool leftDown = (MouseButtons & MouseButtons.Left) != 0;
            bool justPressed = leftDown && !_wasLeftButtonDownLastPoll;
            bool justReleased = !leftDown && _wasLeftButtonDownLastPoll;
            _wasLeftButtonDownLastPoll = leftDown;

            // A press on the header (reordering a column, or just resizing
            // one) is owned entirely by HeaderInputSubclass/DoDragDrop -
            // this control's own MouseDown never even fires for it (see
            // HeaderInputSubclass's own comment), so without this check
            // this poll would otherwise see "a fresh press that isn't on a
            // real cell" and start a cell-selection drag at the same time
            // as a column-reorder drag.
            if (_isHeaderPressActive)
            {
                _isPollTrackingPress = false;
                _isPolledDragSelecting = false;
                return;
            }

            // A fresh press is checked against the scrollbar (native
            // non-client area) before anything else below can react to it -
            // scrolling by dragging the thumb or clicking the track/arrows
            // must never clear or start a selection, the same as a header
            // or context-menu press. The check only needs to run once, on
            // the press itself: while the button stays down afterward
            // (dragging the thumb), the cursor can move away from the
            // scrollbar's own bounds (Windows keeps tracking the drag via
            // its own capture regardless), so latching the result for the
            // whole press - instead of re-hit-testing every tick - is what
            // keeps a thumb-drag that briefly crosses over the list content
            // from suddenly being treated as a selection drag mid-scroll.
            if (justPressed)
            {
                _isScrollBarPressActive = IsPointOnScrollBar(Cursor.Position);
            }

            if (_isScrollBarPressActive)
            {
                if (justReleased)
                {
                    _isScrollBarPressActive = false;
                }

                _isPollTrackingPress = false;
                _isPolledDragSelecting = false;
                return;
            }

            // Same idea as the header check above - a left-click on this
            // control's own ContextMenuStrip (including its "Copy
            // selection"/"Copy all" submenus) isn't on a real cell either,
            // and without this the poll would clear the very selection
            // that click's own menu item is about to act on, before its
            // Click handler ever gets a chance to read it - confirmed live:
            // right-click a selected cell, then left-click "Copy selection"
            // in the menu, and nothing got copied (no toast, selection
            // visibly gone) because this poll cleared it out from under the
            // menu click.
            if (_isContextMenuOpen)
            {
                _isPollTrackingPress = false;
                _isPolledDragSelecting = false;
                return;
            }

            if (justPressed && !_isDragSelecting)
            {
                // _isDragSelecting can only already be true here if the
                // press landed on a real cell - OnListViewMouseDown (a
                // normal input event, delivered before this poll tick could
                // possibly run) already claimed it. A fresh press anywhere
                // else clears whatever was selected, same as the original
                // "click below the last row clears the selection" behavior,
                // just no longer limited to that one specific dead zone.
                if (_anchorRow >= 0)
                {
                    ClearSelection();
                }

                _pollPressPoint = PointToClient(Cursor.Position);
                _isPollTrackingPress = true;
            }

            // Reliable, poll-driven release detection - a backstop for
            // BOTH kinds of drag, not just the poll-driven one. The same
            // native-event unreliability documented above for a press
            // (MouseUp firing early/never for anything off-item) applies
            // just as much to a drag that STARTED on a real cell
            // (_isDragSelecting, normally finalized by OnListViewMouseUp)
            // once the cursor leaves this control's bounds mid-drag -
            // OnListViewMouseMove/MouseUp can simply stop arriving from
            // that point on. Without this, _isDragSelecting could get
            // stuck true forever after such a drag, which - since the
            // justPressed handling above only clears the selection when
            // "!_isDragSelecting" - would silently block every future
            // click here from ever deselecting anything again.
            if (justReleased)
            {
                if (_isDragSelecting || _isPolledDragSelecting)
                {
                    FinalizeDragSelection();
                    Invalidate();
                }

                _isDragSelecting = false;
                _isPollTrackingPress = false;
                _isPolledDragSelecting = false;
                return;
            }

            if (!leftDown)
            {
                return;
            }

            // Same reliability gap while the button is still down: once an
            // on-cell drag's cursor leaves this control, native MouseMove
            // can stop updating _selectionCurrentPoint too, freezing the
            // selection rectangle at whatever it last saw instead of
            // following the still-active drag. The poll keeps it live
            // here as a fallback - if native MouseMove is still firing
            // fine, this just recomputes the same point every tick, a
            // no-op past the equality check below.
            if (_isDragSelecting)
            {
                var followedPoint = PointToClient(Cursor.Position);
                if (followedPoint != _selectionCurrentPoint)
                {
                    _selectionCurrentPoint = followedPoint;
                    Invalidate();
                }

                return;
            }

            if (!_isPollTrackingPress || Items.Count == 0 || Columns.Count == 0)
            {
                return;
            }

            var currentPoint = PointToClient(Cursor.Position);

            if (!_isPolledDragSelecting)
            {
                bool movedEnough =
                    Math.Abs(currentPoint.X - _pollPressPoint.X) >= SystemInformation.DragSize.Width ||
                    Math.Abs(currentPoint.Y - _pollPressPoint.Y) >= SystemInformation.DragSize.Height;

                if (!movedEnough)
                {
                    return;
                }

                // Anchored at the ORIGINAL press point - safe even when
                // that point is far outside this control entirely (pressed
                // somewhere else in the app), because selection is real
                // rectangle intersection (see IsCellSelected) against a
                // rectangle that GetNormalizedSelectionRectangle always
                // intersects down to what's actually visible - a raw
                // anchor/current pair that never touches the visible area
                // at all collapses to an empty rectangle there, matching
                // any cell nowhere at all.
                _selectionAnchorPoint = _pollPressPoint;
                _selectionCurrentPoint = currentPoint;
                _isPolledDragSelecting = true;
                Invalidate();
                return;
            }

            if (currentPoint == _selectionCurrentPoint)
            {
                return;
            }

            _selectionCurrentPoint = currentPoint;
            Invalidate();
        }

        // Called from HeaderInputSubclass once a header click has moved
        // past the drag threshold - a header click lands on the header's
        // own native child window, not on this control, so detecting the
        // click/threshold has to happen there (see HeaderInputSubclass).
        // From here on, though, tracking the rest of the drag is handed
        // off to WinForms' own DoDragDrop/OnDragOver/OnDragDrop, the same
        // mechanism ListBox already uses successfully for its own
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

                // DoDragDrop runs its own internal message loop for the
                // whole drag, so the header's own WM_LBUTTONUP (which
                // would otherwise clear this) may never actually reach
                // HeaderInputSubclass's normal WndProc handling for a real
                // reorder - this is the reliable place to clear it instead,
                // since DoDragDrop has, by definition, just finished.
                _isHeaderPressActive = false;

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

        // comctl32's header control has no concept of "this specific
        // column isn't resizable" - it always shows the resize cursor near
        // any column boundary, regardless of SetColumnResizable. Dragging
        // is already blocked there (OnColumnWidthChanging cancels it), but
        // without this the cursor itself would still misleadingly suggest
        // it's possible. Resizing a boundary adjusts the column to its
        // LEFT (comctl32's own convention), so that's the column whose
        // resizability actually governs this specific boundary.
        private bool IsNearNonResizableColumnBorder(int x)
        {
            const int resizeGripWidth = 5;
            var cumulativeWidth = 0;
            foreach (var column in GetColumnsInDisplayOrder())
            {
                cumulativeWidth += column.Width;
                if (Math.Abs(x - cumulativeWidth) <= resizeGripWidth)
                {
                    return !IsColumnResizable(column.Index);
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
        // entirely to ListView.BeginColumnDragDrop (WinForms'
        // DoDragDrop/OnDragOver/OnDragDrop, the same mechanism
        // ListBox already uses for its own item-reorder drag) rather
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

            private readonly ListView _owner;
            private int _pendingColumnIndex = -1;
            private int _pendingStartX;

            private const uint RDW_INVALIDATE = 0x0001;
            private const uint RDW_ERASE = 0x0004;
            private const uint RDW_UPDATENOW = 0x0100;

            [System.Runtime.InteropServices.DllImport("user32.dll")]
            private static extern System.IntPtr SendMessage(System.IntPtr hWnd, int msg, System.IntPtr wParam, System.IntPtr lParam);

            [System.Runtime.InteropServices.DllImport("user32.dll")]
            private static extern bool RedrawWindow(System.IntPtr hWnd, System.IntPtr lprcUpdate, System.IntPtr hrgnUpdate, uint flags);

            public HeaderInputSubclass(ListView owner)
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
                        // Set for ANY press here, including a resize-grip
                        // click OnMouseDown itself ignores (leaves
                        // _pendingColumnIndex at -1) - a resize is still a
                        // header interaction, not a cell one, and must
                        // block OnExternalDragPollTick's cell-selection
                        // polling exactly the same as a reorder does.
                        _owner._isHeaderPressActive = true;
                        OnMouseDown(GetX(m.LParam));
                        break;

                    case WM_MOUSEMOVE:
                        // Checked AFTER base.WndProc, not via WM_SETCURSOR
                        // beforehand - an earlier WM_SETCURSOR-based attempt
                        // never actually suppressed anything, because the
                        // header sets its resize cursor directly from
                        // inside its own WM_MOUSEMOVE handling (a common
                        // comctl32 pattern - hot-tracking controls often
                        // call SetCursor straight from mouse-move handling
                        // rather than going through WM_SETCURSOR at all),
                        // so intercepting WM_SETCURSOR first just meant
                        // native code set the cursor moments later anyway.
                        // Letting base run first and then setting our own
                        // cursor right after is the same "let native act,
                        // then correct it" approach already used elsewhere
                        // in this class (the reorder line's color, the
                        // outer border) - whichever SetCursor call happens
                        // last is the one that's actually visible.
                        TrySuppressResizeCursor(GetX(m.LParam));
                        OnMouseMove(GetX(m.LParam));
                        break;

                    case WM_LBUTTONUP:
                        // A plain click (never exceeded the threshold) just
                        // clears the pending state - nothing to reorder,
                        // nothing to undo, since BeginColumnDragDrop is
                        // only ever called after OnMouseMove sees the
                        // threshold exceeded.
                        _pendingColumnIndex = -1;
                        _owner._isHeaderPressActive = false;
                        break;
                }
            }

            private static int GetX(System.IntPtr lParam)
            {
                return unchecked((short)((long)lParam & 0xFFFF));
            }

            // See the WM_MOUSEMOVE case above for why this runs after
            // base.WndProc instead of intercepting WM_SETCURSOR.
            private void TrySuppressResizeCursor(int x)
            {
                if (_owner.IsNearNonResizableColumnBorder(x))
                {
                    Cursor.Current = Cursors.Default;
                }
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
