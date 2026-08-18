using System;
using System.Collections.Generic;
using System.Drawing;
using System.Reflection;
using System.Windows.Forms;
using CustomWFUI.Styles;

namespace CustomWFUI.Controls
{
    public class StyledListBox : ListBox
    {
        private const int DragHandleWidth = 30;
        private const int DragHandleHitAreaPadding = 5;
        private const int ItemIconSize = 22;
        private const int ItemIconMargin = 8;

        private static readonly StringFormat CenterLeftFormat = new StringFormat
        {
            Alignment = StringAlignment.Near,
            LineAlignment = StringAlignment.Center,
            Trimming = StringTrimming.EllipsisCharacter
        };

        private readonly Dictionary<Type, PropertyInfo> _displayPropertyCache = new Dictionary<Type, PropertyInfo>();

        private Color _itemBackColor = UIColors.BackgroundMedium;
        private Color _alternateItemBackColor;
        private Color _itemForeColor = UIColors.TextPrimary;
        private Color _selectedBackColor = UIColors.Primary;
        private Color _hoverBackColor = UIColors.BackgroundLight;
        private Color _dragHandleColor = UIColors.TextTertiary;
        private Color _dragIndicatorColor = UIColors.PrimaryLight;
        private Color _disabledForeColor = UIColors.TextDisabled;
        private Color _disabledBackColor = UIColors.BackgroundDarkElevated;

        private int _itemHeight = 35;
        private int _hoverIndex = -1;

        private bool _allowReorder = true;
        private bool _showEnumeration;
        private int _dragIndex = -1;
        private bool _isDragging;
        private Point _dragStartPoint;
        private int _dragInsertPosition = -1;

        private Func<object, string> _displayTextProvider;
        private string _displayTextMember;
        private Func<object, Image> _iconProvider;
        private Func<object, bool> _isItemDisabled;

        public event EventHandler ItemsReordered;

        public Func<object, string> DisplayTextProvider
        {
            get => _displayTextProvider;
            set
            {
                _displayTextProvider = value;
                Invalidate();
            }
        }

        public string DisplayTextMember
        {
            get => _displayTextMember;
            set
            {
                _displayTextMember = value;
                _displayPropertyCache.Clear();
                Invalidate();
            }
        }

        public bool ShowEnumeration
        {
            get => _showEnumeration;
            set
            {
                _showEnumeration = value;
                Invalidate();
            }
        }

        public Func<object, Image> IconProvider
        {
            get => _iconProvider;
            set
            {
                _iconProvider = value;
                Invalidate();
            }
        }

        public Func<object, bool> IsItemDisabled
        {
            get => _isItemDisabled;
            set
            {
                _isItemDisabled = value;
                Invalidate();
            }
        }

        public bool AllowReorder
        {
            get => _allowReorder;
            set
            {
                _allowReorder = value;
                AllowDrop = value;
                Invalidate();
            }
        }

        public int ItemHeightCustom
        {
            get => _itemHeight;
            set
            {
                _itemHeight = value;
                ItemHeight = value;
                Invalidate();
            }
        }

        public Color DragIndicatorColor
        {
            get => _dragIndicatorColor;
            set
            {
                _dragIndicatorColor = value;
                Invalidate();
            }
        }

        public Color DisabledForeColor
        {
            get => _disabledForeColor;
            set
            {
                _disabledForeColor = value;
                Invalidate();
            }
        }

        public Color DisabledBackColor
        {
            get => _disabledBackColor;
            set
            {
                _disabledBackColor = value;
                Invalidate();
            }
        }

        // Defaults to UIColors.Primary (the accent color) - unlike most of
        // this control's other colors, there was previously no way for a
        // consuming app to override just the selection color without
        // reaching into private state.
        public Color SelectedBackColor
        {
            get => _selectedBackColor;
            set
            {
                _selectedBackColor = value;
                Invalidate();
            }
        }

        public Color ItemBackColor
        {
            get => _itemBackColor;
            set
            {
                _itemBackColor = value;
                _alternateItemBackColor = Darken(_itemBackColor, 5);
                Invalidate();
            }
        }

        public Color ItemForeColor
        {
            get => _itemForeColor;
            set
            {
                _itemForeColor = value;
                Invalidate();
            }
        }

        public Color HoverBackColor
        {
            get => _hoverBackColor;
            set
            {
                _hoverBackColor = value;
                Invalidate();
            }
        }

        public Color DragHandleColor
        {
            get => _dragHandleColor;
            set
            {
                _dragHandleColor = value;
                Invalidate();
            }
        }

        public StyledListBox()
        {
            _alternateItemBackColor = Darken(_itemBackColor, 5);

            DrawMode = DrawMode.OwnerDrawFixed;
            ItemHeight = _itemHeight;
            BackColor = UIColors.BackgroundDark;
            ForeColor = _itemForeColor;
            BorderStyle = BorderStyle.None;
            Font = UIFonts.Normal;
            IntegralHeight = false;
            AllowDrop = _allowReorder;

            SetStyle(
                ControlStyles.OptimizedDoubleBuffer |
                ControlStyles.ResizeRedraw |
                ControlStyles.AllPaintingInWmPaint,
                true);

            EnableDoubleBuffering();

            UpdateStyles();
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            UpdateHoverIndex(e.Location);
            TryStartDrag(e);

            base.OnMouseMove(e);
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            int index = IndexFromPoint(e.Location);

            if (TryPrepareDrag(index, e.Location))
                return;

            if (index == -1)
            {
                ClearSelected();
                _dragIndex = -1;
                return;
            }

            base.OnMouseDown(e);
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            ResetDragState();
            base.OnMouseUp(e);
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            if (_isDragging)
            {
                base.OnMouseLeave(e);
                return;
            }

            int oldHoverIndex = _hoverIndex;
            _hoverIndex = -1;

            InvalidateItem(oldHoverIndex);

            base.OnMouseLeave(e);
        }

        protected override void OnDragLeave(EventArgs e)
        {
            InvalidateDragIndicator();

            _dragInsertPosition = -1;
            InvalidateDragIndicator();

            base.OnDragLeave(e);
        }

        protected override void OnDragOver(DragEventArgs drgevent)
        {
            if (!_allowReorder)
            {
                drgevent.Effect = DragDropEffects.None;
                return;
            }

            Point point = PointToClient(new Point(drgevent.X, drgevent.Y));
            int newInsertPosition = GetInsertPosition(point, out DragDropEffects effect);

            drgevent.Effect = effect;

            if (_dragInsertPosition != newInsertPosition)
            {
                InvalidateDragIndicator();
                _dragInsertPosition = newInsertPosition;
                InvalidateDragIndicator();
            }

            AutoScrollDuringDrag(point, drgevent);

            base.OnDragOver(drgevent);
        }

        protected override void OnDragDrop(DragEventArgs drgevent)
        {
            Point point = PointToClient(new Point(drgevent.X, drgevent.Y));

            bool droppedInside = ClientRectangle.Contains(point);

            if (droppedInside && _allowReorder && _dragIndex != -1 && _dragInsertPosition != -1)
            {
                ReorderDraggedItem();
            }

            ResetDragState();

            _hoverIndex = -1;
            Invalidate();

            base.OnDragDrop(drgevent);
        }

        protected override void OnGiveFeedback(GiveFeedbackEventArgs gfbevent)
        {
            gfbevent.UseDefaultCursors = false;
            Cursor.Current = Cursors.SizeAll;

            base.OnGiveFeedback(gfbevent);
        }

        protected override void OnDrawItem(DrawItemEventArgs e)
        {
            if (e.Index < 0)
                return;

            Rectangle rect = e.Bounds;
            object item = Items[e.Index];

            bool isSelected = (e.State & DrawItemState.Selected) != 0 && !_isDragging;
            bool isHovered = e.Index == _hoverIndex && !_isDragging;
            bool isDisabled = _isItemDisabled?.Invoke(item) == true;
            bool isVScrollVisible = IsVerticalScrollBarVisible();

            Color backColor = GetBackColor(e.Index, isSelected, isHovered, isDisabled);
            Color textColor = GetTextColor(isSelected, isDisabled);

            FillItemBackground(e.Graphics, rect, backColor);

            Rectangle dragRect = Rectangle.Empty;

            if (_allowReorder)
            {
                dragRect = GetDragHandleRectangle(rect, isVScrollVisible);
                DrawDragHandle(e.Graphics, dragRect);
            }

            DrawItemContent(e.Graphics, rect, item, e.Index, textColor, dragRect, isVScrollVisible);

            if (_isDragging && _dragInsertPosition != -1)
            {
                if (_dragInsertPosition == e.Index)
                {
                    DrawIndicatorLine(e.Graphics, rect.Top);
                }
                else if (_dragInsertPosition >= Items.Count && e.Index == Items.Count - 1)
                {
                    DrawIndicatorLine(e.Graphics, rect.Bottom);
                }
            }

            base.OnDrawItem(e);
        }

        public new void ClearSelected()
        {
            if (SelectedIndex == -1)
                return;

            SelectedIndex = -1;
            Invalidate();
        }

        public void MoveItem(int fromIndex, int toIndex)
        {
            if (fromIndex < 0 || fromIndex >= Items.Count || toIndex < 0 || toIndex >= Items.Count)
                return;

            BeginUpdate();

            try
            {
                object item = Items[fromIndex];
                Items.RemoveAt(fromIndex);
                Items.Insert(toIndex, item);

                SelectedIndex = toIndex;
                ItemsReordered?.Invoke(this, EventArgs.Empty);
            }
            finally
            {
                EndUpdate();
            }
        }

        private void UpdateHoverIndex(Point location)
        {
            if (_isDragging)
                return;

            int index = IndexFromPoint(location);

            if (_hoverIndex == index)
                return;

            int oldHoverIndex = _hoverIndex;
            _hoverIndex = index;

            InvalidateItem(oldHoverIndex);
            InvalidateItem(_hoverIndex);
        }

        private bool TryPrepareDrag(int index, Point location)
        {
            if (!_allowReorder || index == -1)
                return false;

            Rectangle itemRect = GetItemRectangle(index);
            Rectangle dragRect = GetDragHandleRectangle(itemRect, IsVerticalScrollBarVisible());

            if (!dragRect.Contains(location))
                return false;

            _dragIndex = index;
            _dragStartPoint = location;
            SelectedIndex = index;

            return true;
        }

        private void TryStartDrag(MouseEventArgs e)
        {
            if (!_allowReorder || e.Button != MouseButtons.Left || _isDragging || _dragIndex == -1)
                return;

            bool movedEnough =
                Math.Abs(e.X - _dragStartPoint.X) > SystemInformation.DragSize.Width ||
                Math.Abs(e.Y - _dragStartPoint.Y) > SystemInformation.DragSize.Height;

            if (!movedEnough)
                return;

            _isDragging = true;
            DoDragDrop(_dragIndex, DragDropEffects.Move);
        }

        private int GetInsertPosition(Point point, out DragDropEffects effect)
        {
            effect = DragDropEffects.None;

            if (Items.Count == 0)
            {
                effect = DragDropEffects.Move;
                return 0;
            }

            int targetIndex = IndexFromPoint(point);

            if (targetIndex != -1)
            {
                Rectangle itemRect = GetItemRectangle(targetIndex);
                bool isTopHalf = point.Y < itemRect.Y + itemRect.Height / 2;

                effect = DragDropEffects.Move;
                return isTopHalf ? targetIndex : targetIndex + 1;
            }

            if (point.Y > GetItemRectangle(Items.Count - 1).Bottom)
            {
                effect = DragDropEffects.Move;
                return Items.Count;
            }

            return -1;
        }

        private void AutoScrollDuringDrag(Point point, DragEventArgs drgevent)
        {
            if (point.Y < 20 && TopIndex > 0)
            {
                TopIndex--;
                drgevent.Effect = DragDropEffects.Move;
            }
            else if (point.Y > ClientSize.Height - 20 && TopIndex < Items.Count - 1)
            {
                TopIndex++;
                drgevent.Effect = DragDropEffects.Move;
            }
        }

        private void ReorderDraggedItem()
        {
            int targetPosition = _dragInsertPosition;

            if (targetPosition > _dragIndex)
                targetPosition--;

            targetPosition = Math.Max(0, Math.Min(targetPosition, Items.Count));

            if (targetPosition == _dragIndex)
                return;

            BeginUpdate();

            try
            {
                object draggedItem = Items[_dragIndex];

                Items.RemoveAt(_dragIndex);

                if (targetPosition > Items.Count)
                    targetPosition = Items.Count;

                Items.Insert(targetPosition, draggedItem);
                SelectedIndex = targetPosition;

                EnsureItemVisible(targetPosition);

                ItemsReordered?.Invoke(this, EventArgs.Empty);
            }
            finally
            {
                EndUpdate();
            }
        }

        private void EnsureItemVisible(int index)
        {
            if (index < TopIndex)
            {
                TopIndex = index;
                return;
            }

            int visibleItemCount = ClientSize.Height / ItemHeight;
            int lastVisibleIndex = TopIndex + visibleItemCount - 1;

            if (index > lastVisibleIndex)
            {
                TopIndex = index - visibleItemCount + 1;
            }
        }

        private void ResetDragState()
        {
            bool needsInvalidate = _isDragging || _dragIndex != -1 || _dragInsertPosition != -1;

            _isDragging = false;
            _dragIndex = -1;
            _dragInsertPosition = -1;

            if (needsInvalidate)
                Invalidate();
        }

        private void FillItemBackground(Graphics graphics, Rectangle rect, Color backColor)
        {
            using (SolidBrush brush = new SolidBrush(backColor))
            {
                graphics.FillRectangle(brush, rect);
            }
        }

        private void DrawItemContent(
            Graphics graphics,
            Rectangle rect,
            object item,
            int index,
            Color textColor,
            Rectangle dragRect,
            bool isVScrollVisible)
        {
            int textLeft = rect.X + 8;
            int textRightMargin = 12;
            int reservedDragWidth = _allowReorder ? dragRect.Width : 0;
            int scrollBarWidth = isVScrollVisible ? SystemInformation.VerticalScrollBarWidth : 0;

            if (_showEnumeration)
            {
                textLeft = DrawEnumeration(graphics, rect, index, textLeft, textColor);
            }

            Image icon = _iconProvider?.Invoke(item);

            if (icon != null)
            {
                textLeft = DrawIcon(graphics, rect, icon, textLeft);
            }

            Rectangle textRect = new Rectangle(
                textLeft,
                rect.Y,
                rect.Width - (textLeft - rect.X) - reservedDragWidth - textRightMargin - scrollBarWidth,
                rect.Height);

            DrawText(graphics, textRect, GetDisplayText(item), textColor);
        }

        private int DrawEnumeration(Graphics graphics, Rectangle rect, int index, int textLeft, Color textColor)
        {
            string numberText = $"{index + 1}.";
            SizeF numberSize = graphics.MeasureString(numberText, Font);

            Rectangle numberRect = new Rectangle(
                textLeft,
                rect.Y,
                (int)Math.Ceiling(numberSize.Width) + 4,
                rect.Height);

            using (SolidBrush brush = new SolidBrush(textColor))
            {
                graphics.DrawString(numberText, Font, brush, numberRect, CenterLeftFormat);
            }

            return textLeft + numberRect.Width + 4;
        }

        private int DrawIcon(Graphics graphics, Rectangle rect, Image icon, int textLeft)
        {
            Rectangle iconRect = new Rectangle(
                textLeft,
                rect.Y + (rect.Height - ItemIconSize) / 2,
                ItemIconSize,
                ItemIconSize);

            graphics.DrawImage(icon, iconRect);

            return textLeft + ItemIconSize + ItemIconMargin;
        }

        private void DrawText(Graphics graphics, Rectangle textRect, string text, Color textColor)
        {
            using (SolidBrush brush = new SolidBrush(textColor))
            {
                graphics.DrawString(text, Font, brush, textRect, CenterLeftFormat);
            }
        }

        private void DrawDragHandle(Graphics graphics, Rectangle dragRect)
        {
            using (Pen pen = new Pen(_dragHandleColor, 1.5f))
            {
                int centerY = dragRect.Y + dragRect.Height / 2;
                int centerX = dragRect.X + dragRect.Width / 2;
                int startX = centerX - 6;

                int lineHeight = 3;
                int spacing = 1;

                for (int i = 0; i < 3; i++)
                {
                    int y = centerY - lineHeight - spacing + i * (lineHeight + spacing);
                    graphics.DrawLine(pen, startX, y, startX + 12, y);
                }
            }
        }

        private void DrawIndicatorLine(Graphics graphics, int y)
        {
            using (Pen pen = new Pen(_dragIndicatorColor, 3))
            {
                graphics.DrawLine(pen, 0, y, Width, y);
            }
        }

        private string GetDisplayText(object item)
        {
            if (_displayTextProvider != null)
                return _displayTextProvider(item);

            if (!string.IsNullOrWhiteSpace(_displayTextMember))
            {
                PropertyInfo property = GetCachedDisplayProperty(item);

                return property?.GetValue(item)?.ToString() ?? "";
            }

            return item?.ToString() ?? "";
        }

        private PropertyInfo GetCachedDisplayProperty(object item)
        {
            if (item == null)
                return null;

            Type itemType = item.GetType();

            if (_displayPropertyCache.TryGetValue(itemType, out PropertyInfo cachedProperty))
                return cachedProperty;

            PropertyInfo property = itemType.GetProperty(_displayTextMember);
            _displayPropertyCache[itemType] = property;

            return property;
        }

        private Color GetBackColor(int index, bool isSelected, bool isHovered, bool isDisabled)
        {
            if (isSelected)
                return _selectedBackColor;

            if (isHovered)
                return _hoverBackColor;

            if (isDisabled)
                return _disabledBackColor;

            return index % 2 == 0
                ? _itemBackColor
                : _alternateItemBackColor;
        }

        private Color GetTextColor(bool isSelected, bool isDisabled)
        {
            // Derived live from _selectedBackColor (rather than a fixed
            // white) so a custom/accent SelectedBackColor - which might be
            // bright, e.g. yellow - always gets readable text.
            if (isSelected)
                return UIColors.GetContrastingForeColor(_selectedBackColor);

            if (isDisabled)
                return _disabledForeColor;

            return _itemForeColor;
        }

        private Rectangle GetDragHandleRectangle(Rectangle itemRect, bool isVScrollVisible)
        {
            int x = itemRect.Right - DragHandleWidth;

            if (isVScrollVisible)
                x -= SystemInformation.VerticalScrollBarWidth;

            return new Rectangle(
                x - DragHandleHitAreaPadding,
                itemRect.Y,
                DragHandleWidth + DragHandleHitAreaPadding * 2,
                itemRect.Height);
        }

        private int GetDragIndicatorYPosition()
        {
            int yPosition;

            if (_dragInsertPosition == 0)
            {
                yPosition = 0;
            }
            else if (_dragInsertPosition >= Items.Count)
            {
                yPosition = Items.Count > 0
                    ? GetItemRectangle(Items.Count - 1).Bottom
                    : 0;
            }
            else
            {
                yPosition = GetItemRectangle(_dragInsertPosition).Top;
            }

            return Math.Max(0, yPosition);
        }

        private bool IsVerticalScrollBarVisible()
        {
            return Items.Count > 0 && Items.Count * ItemHeight > ClientSize.Height;
        }

        private void InvalidateItem(int index)
        {
            if (index < 0 || index >= Items.Count)
                return;

            Invalidate(GetItemRectangle(index));
        }

        private void InvalidateDragIndicator()
        {
            if (_dragInsertPosition == -1)
                return;

            int y = GetDragIndicatorYPosition();
            Invalidate(new Rectangle(0, Math.Max(0, y - 4), Width, 8));
        }

        private void EnableDoubleBuffering()
        {
            typeof(Control)
                .GetProperty("DoubleBuffered", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(this, true, null);
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