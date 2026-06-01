using System;
using System.Drawing;
using System.Windows.Forms;
using CustomWFUI.Styles;

namespace CustomWFUI.Controls
{
    public class StyledListBoxControl : UserControl
    {
        private const int DefaultHeaderHeight = 30;
        private const int HeaderSidePadding = 10;

        private readonly Panel _headerPanel;
        private readonly Label _titleLabel;
        private readonly StyledListBox _listBox;

        public event EventHandler SelectedIndexChanged;
        public event EventHandler ItemsReordered;

        public StyledListBoxControl()
            : this(null, false, false, null, ContentAlignment.MiddleLeft)
        {
        }

        public StyledListBoxControl(
            string displayTextMember = null,
            bool allowReorder = false,
            bool showEnumeration = false,
            string headerTitle = null,
            ContentAlignment headerTextAlign = ContentAlignment.MiddleLeft)
        {
            ConfigureControl();

            _headerPanel = CreateHeaderPanel();
            _titleLabel = CreateTitleLabel(headerTextAlign);
            _listBox = CreateListBox(displayTextMember, allowReorder, showEnumeration);

            _headerPanel.Controls.Add(_titleLabel);

            Controls.Add(_listBox);
            Controls.Add(_headerPanel);

            WireEvents();

            Title = headerTitle;
            HeaderTextAlign = headerTextAlign;

            SetStyle(
                ControlStyles.OptimizedDoubleBuffer |
                ControlStyles.AllPaintingInWmPaint |
                ControlStyles.ResizeRedraw,
                true);

            UpdateStyles();
        }

        public StyledListBox InnerListBox
        {
            get { return _listBox; }
        }

        public Func<object, bool> IsItemDisabled
        {
            get { return _listBox.IsItemDisabled; }
            set { _listBox.IsItemDisabled = value; }
        }

        public Func<object, Image> IconProvider
        {
            get { return _listBox.IconProvider; }
            set { _listBox.IconProvider = value; }
        }

        public string Title
        {
            get { return _titleLabel.Text; }
            set
            {
                string title = value ?? "";

                _titleLabel.Text = title;
                _headerPanel.Visible = !string.IsNullOrWhiteSpace(title);
            }
        }

        public int HeaderHeight
        {
            get { return _headerPanel.Height; }
            set { _headerPanel.Height = Math.Max(0, value); }
        }

        public Color HeaderBackColor
        {
            get { return _headerPanel.BackColor; }
            set { _headerPanel.BackColor = value; }
        }

        public Color HeaderForeColor
        {
            get { return _titleLabel.ForeColor; }
            set { _titleLabel.ForeColor = value; }
        }

        public Font HeaderFont
        {
            get { return _titleLabel.Font; }
            set { _titleLabel.Font = value; }
        }

        public ContentAlignment HeaderTextAlign
        {
            get { return _titleLabel.TextAlign; }
            set
            {
                _titleLabel.TextAlign = value;
                _titleLabel.Padding = GetHeaderPadding(value);
            }
        }

        public ListBox.ObjectCollection Items
        {
            get { return _listBox.Items; }
        }

        public object SelectedItem
        {
            get { return _listBox.SelectedItem; }
            set { _listBox.SelectedItem = value; }
        }

        public int SelectedIndex
        {
            get { return _listBox.SelectedIndex; }
            set { _listBox.SelectedIndex = value; }
        }

        public int ItemHeightCustom
        {
            get { return _listBox.ItemHeightCustom; }
            set { _listBox.ItemHeightCustom = value; }
        }

        public Color DragIndicatorColor
        {
            get { return _listBox.DragIndicatorColor; }
            set { _listBox.DragIndicatorColor = value; }
        }

        public int IndexFromPoint(Point point)
        {
            return _listBox.IndexFromPoint(point);
        }

        public void ClearSelected()
        {
            _listBox.ClearSelected();
        }

        public void MoveItem(int fromIndex, int toIndex)
        {
            _listBox.MoveItem(fromIndex, toIndex);
        }

        public void BeginUpdate()
        {
            if (_listBox == null)
                return;

            _listBox.BeginUpdate();
        }

        public void EndUpdate()
        {
            if (_listBox == null)
                return;

            _listBox.EndUpdate();
        }

        private void ConfigureControl()
        {
            BackColor = Color.Transparent;
            Margin = new Padding(0);
            Padding = new Padding(0);
        }

        private Panel CreateHeaderPanel()
        {
            return new Panel
            {
                Dock = DockStyle.Top,
                Height = DefaultHeaderHeight,
                BackColor = UIColors.BackgroundDark,
                Visible = false,
                Margin = new Padding(0),
                Padding = new Padding(0)
            };
        }

        private Label CreateTitleLabel(ContentAlignment textAlign)
        {
            return new Label
            {
                Dock = DockStyle.Fill,
                AutoSize = false,
                Text = "",
                ForeColor = UIColors.TextPrimary,
                Font = UIFonts.Title,
                TextAlign = textAlign,
                Padding = GetHeaderPadding(textAlign),
                BackColor = Color.Transparent,
                AutoEllipsis = true,
                UseMnemonic = false
            };
        }

        private StyledListBox CreateListBox(
            string displayTextMember,
            bool allowReorder,
            bool showEnumeration)
        {
            return new StyledListBox
            {
                Dock = DockStyle.Fill,
                DisplayTextMember = displayTextMember,
                AllowReorder = allowReorder,
                ShowEnumeration = showEnumeration,
                Margin = new Padding(0)
            };
        }

        private void WireEvents()
        {
            if (_listBox == null)
                return;

            _listBox.SelectedIndexChanged += OnListBoxSelectedIndexChanged;
            _listBox.ItemsReordered += OnListBoxItemsReordered;
            _listBox.MouseMove += OnListBoxMouseMove;
            _listBox.MouseLeave += OnListBoxMouseLeave;
        }

        private void OnListBoxSelectedIndexChanged(object sender, EventArgs e)
        {
            if (SelectedIndexChanged != null)
                SelectedIndexChanged(sender, e);
        }

        private void OnListBoxItemsReordered(object sender, EventArgs e)
        {
            if (ItemsReordered != null)
                ItemsReordered(sender, e);
        }

        private void OnListBoxMouseMove(object sender, MouseEventArgs e)
        {
            OnMouseMove(e);
        }

        private void OnListBoxMouseLeave(object sender, EventArgs e)
        {
            OnMouseLeave(e);
        }

        private Padding GetHeaderPadding(ContentAlignment textAlign)
        {
            switch (textAlign)
            {
                case ContentAlignment.MiddleLeft:
                case ContentAlignment.TopLeft:
                case ContentAlignment.BottomLeft:
                    return new Padding(HeaderSidePadding, 0, 0, 0);

                case ContentAlignment.MiddleRight:
                case ContentAlignment.TopRight:
                case ContentAlignment.BottomRight:
                    return new Padding(0, 0, HeaderSidePadding, 0);

                default:
                    return new Padding(0);
            }
        }
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                UnwireEvents();
            }

            base.Dispose(disposing);
        }

        private void UnwireEvents()
        {
            if (_listBox == null)
                return;

            _listBox.SelectedIndexChanged -= OnListBoxSelectedIndexChanged;
            _listBox.ItemsReordered -= OnListBoxItemsReordered;
            _listBox.MouseMove -= OnListBoxMouseMove;
            _listBox.MouseLeave -= OnListBoxMouseLeave;
        }
    }
}