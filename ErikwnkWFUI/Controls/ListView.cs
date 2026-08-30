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
    /// A dark-themed, multi-column ListView (Details view) with completely
    /// standard, native row selection - click, Ctrl+click, Shift+click, and
    /// the native rubber-band drag (including its own auto-scroll past an
    /// edge) all work exactly like Explorer's own list view, since they
    /// simply are Explorer's own list view under the hood. Only the visuals
    /// (row/header colors, fonts, the selection overlay) are this control's
    /// own - selection itself is left entirely to
    /// <see cref="System.Windows.Forms.ListView.MultiSelect"/> and
    /// <see cref="System.Windows.Forms.ListView.SelectedItems"/>, not
    /// reimplemented by hand.
    /// <list type="bullet">
    /// <item>Hovering a cell whose text is wider than its column shows the full text in a tooltip.</item>
    /// </list>
    /// Ctrl+C copies every selected row (tab-separated columns, one line
    /// per row, no header line - meant to be pasted as plain data). Ctrl+
    /// Shift+C copies the same rows with a leading row of column names, for
    /// pasting as a proper table. Both also place an HTML table on the
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

        private int _headerHeight = 24;
        private bool _isApplyingFillColumn;
        private int _pendingToggleDeselectItemIndex = -1;
        private readonly Timer _toggleDeselectSettleTimer;
        private int _toggleDeselectWatchIndex = -1;
        private readonly OutsideClickDeselectFilter _outsideClickDeselectFilter;

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
            MultiSelect = true;
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
            MouseDown += OnListViewMouseDownForToggleDeselect;
            MouseUp += OnListViewMouseUpForToggleDeselect;
            ItemSelectionChanged += OnItemSelectionChangedForToggleDeselect;
            // Safety-net only, only ever running for ~1.5s after a toggle-
            // deselect - see OnListViewMouseUpForToggleDeselect for why
            // this exists at all.
            _toggleDeselectSettleTimer = new Timer { Interval = 1500 };
            _toggleDeselectSettleTimer.Tick += OnToggleDeselectSettleTimerTick;
            MouseMove += OnListViewMouseMoveForToolTip;
            MouseLeave += OnListViewMouseLeave;
            KeyDown += OnListViewKeyDown;
            ColumnWidthChanging += OnColumnWidthChanging;
            ColumnWidthChanged += OnColumnWidthChanged;

            ContextMenuStrip = BuildContextMenu();

            // Reacts to a left-button press landing on any OTHER window in
            // this app - a message filter, not a poll, so this doesn't
            // bring back the kind of hand-rolled per-tick tracking the
            // selection rewrite just got rid of. See
            // OutsideClickDeselectFilter for what counts as "outside".
            _outsideClickDeselectFilter = new OutsideClickDeselectFilter(this);
            Application.AddMessageFilter(_outsideClickDeselectFilter);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _cellToolTip.Dispose();
                _toggleDeselectSettleTimer.Stop();
                _toggleDeselectSettleTimer.Dispose();
                Application.RemoveMessageFilter(_outsideClickDeselectFilter);
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

        // LVM_SETEXTENDEDLISTVIEWSTYLE / LVS_EX_DOUBLEBUFFER - turns on the
        // native ListView's OWN internal double buffering. Without this,
        // scrolling an owner-drawn ListView (this control draws every cell
        // itself via DrawSubItem) can leave stray gray streaks/lines behind:
        // comctl32 scrolls existing content with ScrollWindowEx and then
        // repaints only the newly-exposed strip directly to screen, and that
        // strip's owner-draw callbacks can visibly lag behind the blit for
        // a frame, showing whatever was underneath (typically gray) instead
        // of this control's own row background. This is the standard fix
        // for exactly that class of artifact and has no public .NET API -
        // DoubleBuffered/ControlStyles (already set in the constructor) only
        // cover .NET's own OnPaint pipeline, which this control's actual
        // row/cell content never goes through.
        private const int LVM_FIRST = 0x1000;
        private const int LVM_SETEXTENDEDLISTVIEWSTYLE = LVM_FIRST + 54;
        private const int LVS_EX_DOUBLEBUFFER = 0x00010000;

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern System.IntPtr SendMessage(System.IntPtr hWnd, int msg, System.IntPtr wParam, System.IntPtr lParam);

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            ApplyFillColumn();

            // Re-applied every time the handle is (re)created, same as the
            // header subclass just below - this extended style lives on the
            // native control itself, not anything .NET persists across a
            // handle recreation.
            SendMessage(Handle, LVM_SETEXTENDEDLISTVIEWSTYLE, (System.IntPtr)LVS_EX_DOUBLEBUFFER, (System.IntPtr)LVS_EX_DOUBLEBUFFER);

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

        // Scrolling (scrollbar drag/click, mouse wheel, or a keyboard
        // scroll) makes the native ListView shift its existing pixels with
        // ScrollWindowEx and then repaint only the newly-exposed strip -
        // for an owner-drawn control that can leave a stray gray edge
        // behind at the seam, confirmed to only happen scrolling DOWN
        // (matching a blit/seam bug rather than anything in the actual
        // per-cell drawing logic in OnDrawSubItem, which isn't direction-
        // dependent). Forcing a full repaint on every scroll message,
        // instead of trusting that partial blit-based update, redraws
        // every visible row fresh and gets rid of it. LVS_EX_DOUBLEBUFFER
        // above keeps this from re-introducing the flicker the partial
        // blit was originally meant to avoid.
        private const int WM_VSCROLL = 0x0115;
        private const int WM_HSCROLL = 0x0114;
        private const int WM_MOUSEWHEEL = 0x020A;

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern bool LockWindowUpdate(System.IntPtr hWndLock);

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == WM_VSCROLL || m.Msg == WM_HSCROLL || m.Msg == WM_MOUSEWHEEL)
            {
                // base.WndProc below runs the native control's own scroll
                // handling SYNCHRONOUSLY, including its glitchy partial
                // ScrollWindowEx-based repaint - by the time control
                // returns here, that bad frame has already reached the
                // screen once, so invalidating afterward (an earlier
                // attempt) still let it flash for a frame before the
                // corrected repaint replaced it. A WM_SETREDRAW(FALSE)
                // suppression around that same call (also tried) still let
                // an occasional frame through - LVS_EX_DOUBLEBUFFER's own
                // internal presentation isn't fully gated by that flag on
                // every comctl32 version. LockWindowUpdate is the stronger
                // guarantee: it blocks ANY pixel of this window (and its
                // children, including the header) from reaching the screen
                // at the GDI level while locked, regardless of how the
                // native control internally decides to paint - so nothing
                // native can flash through no matter the mechanism.
                // Re-enabling it and forcing an immediate synchronous
                // repaint (Update(), not just Invalidate()) means the very
                // first frame the user actually sees is the corrected one.
                LockWindowUpdate(Handle);
                try
                {
                    base.WndProc(ref m);
                }
                finally
                {
                    LockWindowUpdate(System.IntPtr.Zero);
                }

                Invalidate();
                Update();
                return;
            }

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
                copySelection.Enabled = SelectedItems.Count > 0;
                copyAll.Enabled = Items.Count > 0;
            };

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

            // FullRowSelect + native SelectedItems - the whole row's worth
            // of cells share one selected/not-selected state, straight from
            // the native control, not any hand-tracked range.
            if (e.Item.Selected)
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

        // Native click behavior always ends up with just the clicked item
        // selected (for a plain click, no modifiers) - it never TOGGLES an
        // already-selected item back off, the way this control used to
        // (see the class doc's history). Restoring just that one piece:
        // remember, at MouseDown, whether the pressed item was already the
        // sole selected item; if MouseUp lands back on that same item
        // (i.e. this wasn't a drag to somewhere else), deselect it. Native
        // selection handling itself is untouched - this only adds a
        // correction afterward, so drag-select/Ctrl/Shift-click all keep
        // working exactly as native.
        private void OnListViewMouseDownForToggleDeselect(object sender, MouseEventArgs e)
        {
            _pendingToggleDeselectItemIndex = -1;

            if (e.Button != MouseButtons.Left || ModifierKeys != Keys.None)
            {
                return;
            }

            var hitTest = HitTest(e.Location);
            if (hitTest.Item != null && hitTest.Item.Selected && SelectedItems.Count == 1)
            {
                _pendingToggleDeselectItemIndex = hitTest.Item.Index;
            }
        }

        private void OnListViewMouseUpForToggleDeselect(object sender, MouseEventArgs e)
        {
            var pendingIndex = _pendingToggleDeselectItemIndex;
            _pendingToggleDeselectItemIndex = -1;

            if (pendingIndex < 0 || e.Button != MouseButtons.Left)
            {
                return;
            }

            var hitTest = HitTest(e.Location);
            if (hitTest.Item != null && hitTest.Item.Index == pendingIndex && hitTest.Item.Selected)
            {
                hitTest.Item.Selected = false;

                // comctl32 arms an internal "click to rename" timer for ANY
                // press on an item that's already both selected AND
                // focused, regardless of LabelEdit - that timer fires
                // natively about a second later and re-applies the item's
                // selected state on its own, silently undoing the deselect
                // above. There's no way to preempt that timer itself from
                // managed code, but ItemSelectionChanged fires the instant
                // it does fire - watching for that and reverting it right
                // there (see OnItemSelectionChangedForToggleDeselect) reacts
                // as soon as it happens instead of waiting out a guessed
                // delay, so the item never visibly sits there re-selected
                // for the better part of a second. The timer below is only
                // a safety net that stops the watch if that reselect never
                // actually happens.
                _toggleDeselectWatchIndex = pendingIndex;
                _toggleDeselectSettleTimer.Stop();
                _toggleDeselectSettleTimer.Start();
            }
        }

        private void OnItemSelectionChangedForToggleDeselect(object sender, ListViewItemSelectionChangedEventArgs e)
        {
            if (_toggleDeselectWatchIndex < 0 || e.ItemIndex != _toggleDeselectWatchIndex || !e.IsSelected)
            {
                return;
            }

            // Clear the watch BEFORE reverting - Selected's setter below
            // raises this same event again (with IsSelected false this
            // time), and without clearing first that recursive call would
            // still match the guard above and try to act again.
            _toggleDeselectWatchIndex = -1;
            _toggleDeselectSettleTimer.Stop();
            e.Item.Selected = false;
        }

        // Only reached if the native reselect quirk never actually fired
        // for this click - just stops watching, since
        // OnItemSelectionChangedForToggleDeselect already handles the real
        // case the instant it happens.
        private void OnToggleDeselectSettleTimerTick(object sender, EventArgs e)
        {
            _toggleDeselectSettleTimer.Stop();
            _toggleDeselectWatchIndex = -1;
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

        // Arrow-key navigation/range-extension (Up/Down/Shift+Up/Down, plus
        // Ctrl-navigate-without-selecting) is entirely native - only Ctrl+C/
        // Ctrl+Shift+C/Ctrl+A need handling here, since the native control
        // has no built-in accelerator for any of those three.
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
        }

        // The native ListView has no built-in "select everything" - unlike
        // ListBox, which does.
        private void SelectAll()
        {
            if (Items.Count == 0)
            {
                return;
            }

            BeginUpdate();
            try
            {
                foreach (ListViewItem item in Items)
                {
                    item.Selected = true;
                }
            }
            finally
            {
                EndUpdate();
            }
        }

        // Returns a DISPLAY index (position in visual column order) - used
        // for header-reorder/tooltip column hit-testing, unrelated to
        // selection (which is now entirely native/row-based).
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
            // SelectedItems isn't guaranteed to be in visual order - sorting
            // by Index keeps the copied rows in the same top-to-bottom order
            // they're actually shown in, regardless of the order they were
            // clicked/dragged into the selection.
            var selectedItems = SelectedItems.Cast<ListViewItem>().OrderBy(item => item.Index).ToList();
            if (selectedItems.Count == 0)
            {
                return;
            }

            // Every column, in DISPLAY order - a whole-row copy always
            // includes every column, so (unlike the old cell-range copy)
            // there's no column span to compute here.
            var orderedColumns = GetColumnsInDisplayOrder();

            List<string> headerCells = null;
            if (includeHeader)
            {
                headerCells = orderedColumns.Select(column => column.Text).ToList();
            }

            var plainTextBuilder = new StringBuilder();
            if (headerCells != null)
            {
                plainTextBuilder.AppendLine(string.Join("\t", headerCells));
            }

            var rows = new List<List<string>>();
            foreach (var item in selectedItems)
            {
                var cells = new List<string>();
                foreach (var column in orderedColumns)
                {
                    var dataColumn = column.Index;
                    cells.Add(dataColumn < item.SubItems.Count ? item.SubItems[dataColumn].Text : string.Empty);
                }

                rows.Add(cells);
                plainTextBuilder.AppendLine(string.Join("\t", cells));
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

            var message = selectedItems.Count == 1
                ? UIStrings.Get("ListView.RowCopied")
                : string.Format(UIStrings.Get("ListView.RowsCopied"), selectedItems.Count);
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

        // Clears the selection when a left-button press lands anywhere else
        // in the app - a Windows message filter sees every message before
        // its target window does, which is the standard, non-polling way
        // to react to "a click happened somewhere I'm not". A press
        // targeting this control's OWN window handle is never "outside",
        // whether it's WM_LBUTTONDOWN on the client area (a real cell, or
        // empty space below the last row/beside the last column) or
        // WM_NCLBUTTONDOWN on its own non-client area (the scrollbar) -
        // both report m.HWnd as this control's own handle either way. The
        // header is a separate native child window ("SysHeader32", see
        // HeaderInputSubclass) so it needs its own explicit exclusion.
        // Anything else - another control in this form, empty space on the
        // form, a completely different window elsewhere in this app - is
        // genuinely "outside" and clears the selection. A click in a
        // different application entirely doesn't even resolve to a Control
        // via FromChildHandle, so it's naturally excluded too.
        private sealed class OutsideClickDeselectFilter : IMessageFilter
        {
            private const int WM_LBUTTONDOWN = 0x0201;
            private const int WM_NCLBUTTONDOWN = 0x00A1;

            private readonly ListView _owner;

            public OutsideClickDeselectFilter(ListView owner)
            {
                _owner = owner;
            }

            public bool PreFilterMessage(ref Message m)
            {
                if (m.Msg != WM_LBUTTONDOWN && m.Msg != WM_NCLBUTTONDOWN)
                {
                    return false;
                }

                if (_owner.IsDisposed || !_owner.IsHandleCreated || _owner.SelectedItems.Count == 0)
                {
                    return false;
                }

                if (m.HWnd == _owner.Handle)
                {
                    return false;
                }

                var headerHandle = HeaderInputSubclass.GetHeaderHandle(_owner.Handle);
                if (headerHandle != System.IntPtr.Zero && m.HWnd == headerHandle)
                {
                    return false;
                }

                if (Control.FromChildHandle(m.HWnd) == null)
                {
                    // Not one of this process's own windows (e.g. a click
                    // in a different application) - nothing to react to.
                    return false;
                }

                foreach (ListViewItem item in _owner.SelectedItems.Cast<ListViewItem>().ToList())
                {
                    item.Selected = false;
                }

                return false;
            }
        }
    }
}
