using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace CustomWFUI.Controls
{
    public class ToggleSwitch : Control
    {
        private const int DefaultWidth = 45;
        private const int DefaultHeight = 25;
        private const int PaddingSize = 2;
        private const int BorderThickness = 1;

        private bool _checked;
        private bool _isHovered;
        private bool _isPressed;

        private ToolTip _toolTip;

        private string _toolTipTextChecked = "";
        private string _toolTipTextUnchecked = "";

        public event EventHandler CheckedChanged;

        public bool Checked
        {
            get
            {
                return _checked;
            }
            set
            {
                if (_checked == value)
                    return;

                _checked = value;

                UpdateToolTip();
                Invalidate();

                CheckedChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        public string ToolTipTextChecked
        {
            get
            {
                return _toolTipTextChecked;
            }
            set
            {
                _toolTipTextChecked = value ?? "";
                UpdateToolTip();
            }
        }

        public string ToolTipTextUnchecked
        {
            get
            {
                return _toolTipTextUnchecked;
            }
            set
            {
                _toolTipTextUnchecked = value ?? "";
                UpdateToolTip();
            }
        }

        public string ToolTipText
        {
            set
            {
                string text = value ?? "";

                _toolTipTextChecked = text;
                _toolTipTextUnchecked = text;

                UpdateToolTip();
            }
        }

        public ToggleSwitch()
        {
            ConfigureControl();
            CreateToolTip();
        }

        protected override void OnMouseEnter(EventArgs e)
        {
            _isHovered = true;

            Invalidate();

            base.OnMouseEnter(e);
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            _isHovered = false;
            _isPressed = false;

            Invalidate();

            base.OnMouseLeave(e);
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                _isPressed = true;
                Invalidate();
            }

            base.OnMouseDown(e);
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                _isPressed = false;
                Checked = !Checked;

                Invalidate();
            }

            base.OnMouseUp(e);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

            Rectangle toggleRectangle = GetToggleRectangle();
            Rectangle knobRectangle = GetKnobRectangle(toggleRectangle);

            DrawBackground(e.Graphics, toggleRectangle);
            DrawBorder(e.Graphics, toggleRectangle);
            DrawKnob(e.Graphics, knobRectangle);
            DrawKnobBorder(e.Graphics, knobRectangle);
        }

        protected override void OnPaintBackground(PaintEventArgs e)
        {
            if (Parent == null || BackColor != Color.Transparent)
            {
                base.OnPaintBackground(e);
                return;
            }

            GraphicsState state = e.Graphics.Save();

            try
            {
                e.Graphics.TranslateTransform(-Left, -Top);

                Rectangle rectangle = new Rectangle(
                    Parent.Location,
                    Parent.Size);

                InvokePaintBackground(
                    Parent,
                    new PaintEventArgs(e.Graphics, rectangle));

                InvokePaint(
                    Parent,
                    new PaintEventArgs(e.Graphics, rectangle));
            }
            finally
            {
                e.Graphics.Restore(state);
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (_toolTip != null)
                {
                    _toolTip.Dispose();
                    _toolTip = null;
                }
            }

            base.Dispose(disposing);
        }

        private void ConfigureControl()
        {
            Size = new Size(DefaultWidth, DefaultHeight);

            SetStyle(
                ControlStyles.UserPaint |
                ControlStyles.AllPaintingInWmPaint |
                ControlStyles.OptimizedDoubleBuffer |
                ControlStyles.ResizeRedraw |
                ControlStyles.SupportsTransparentBackColor,
                true);

            DoubleBuffered = true;

            Cursor = Cursors.Hand;
            BackColor = Color.Transparent;
        }

        private void CreateToolTip()
        {
            _toolTip = new ToolTip
            {
                InitialDelay = 500,
                ReshowDelay = 100,
                AutoPopDelay = 5000
            };
        }

        private void UpdateToolTip()
        {
            if (_toolTip == null)
                return;

            string text = Checked
                ? _toolTipTextChecked
                : _toolTipTextUnchecked;

            _toolTip.SetToolTip(this, text ?? "");
        }

        private Rectangle GetToggleRectangle()
        {
            return new Rectangle(
                PaddingSize,
                PaddingSize,
                Width - PaddingSize * 2,
                Height - PaddingSize * 2);
        }

        private Rectangle GetKnobRectangle(Rectangle toggleRectangle)
        {
            int knobSize = toggleRectangle.Height - 2;

            int knobX = Checked
                ? toggleRectangle.Right - knobSize - 2
                : toggleRectangle.X + 2;

            return new Rectangle(
                knobX,
                toggleRectangle.Y + 2,
                knobSize,
                knobSize);
        }

        private void DrawBackground(Graphics graphics, Rectangle rectangle)
        {
            GraphicsPath path = CreateRoundedRectanglePath(
                rectangle,
                rectangle.Height / 2);

            SolidBrush brush = new SolidBrush(GetBackgroundColor());

            try
            {
                graphics.FillPath(brush, path);
            }
            finally
            {
                brush.Dispose();
                path.Dispose();
            }
        }

        private void DrawBorder(Graphics graphics, Rectangle rectangle)
        {
            GraphicsPath path = CreateRoundedRectanglePath(
                rectangle,
                rectangle.Height / 2);

            Pen pen = new Pen(GetBorderColor(), BorderThickness);

            try
            {
                graphics.DrawPath(pen, path);
            }
            finally
            {
                pen.Dispose();
                path.Dispose();
            }
        }

        private void DrawKnob(Graphics graphics, Rectangle rectangle)
        {
            SolidBrush brush = new SolidBrush(GetKnobColor());

            try
            {
                graphics.FillEllipse(brush, rectangle);
            }
            finally
            {
                brush.Dispose();
            }
        }

        private void DrawKnobBorder(Graphics graphics, Rectangle rectangle)
        {
            Pen pen = new Pen(UIStyles.Colors.BorderMedium, BorderThickness);

            try
            {
                graphics.DrawEllipse(pen, rectangle);
            }
            finally
            {
                pen.Dispose();
            }
        }

        private Color GetBackgroundColor()
        {
            if (!Enabled)
                return UIStyles.Colors.BackgroundDark;

            if (Checked)
                return UIStyles.Colors.Primary;

            return UIStyles.Colors.BackgroundMedium;
        }

        private Color GetBorderColor()
        {
            if (!Enabled)
                return UIStyles.Colors.BorderDark;

            if (_isHovered && !Checked)
                return UIStyles.Colors.Primary;

            return UIStyles.Colors.BorderMedium;
        }

        private Color GetKnobColor()
        {
            if (!Enabled)
                return UIStyles.Colors.TextDisabled;

            if (_isPressed)
                return UIStyles.Colors.PrimaryLight;

            if (_isHovered)
                return UIStyles.Colors.TextPrimary;

            return UIStyles.Colors.White;
        }

        private GraphicsPath CreateRoundedRectanglePath(
            Rectangle rectangle,
            int radius)
        {
            int diameter = radius * 2;

            GraphicsPath path = new GraphicsPath();

            path.AddArc(
                rectangle.X,
                rectangle.Y,
                diameter,
                diameter,
                180,
                90);

            path.AddArc(
                rectangle.Right - diameter,
                rectangle.Y,
                diameter,
                diameter,
                270,
                90);

            path.AddArc(
                rectangle.Right - diameter,
                rectangle.Bottom - diameter,
                diameter,
                diameter,
                0,
                90);

            path.AddArc(
                rectangle.X,
                rectangle.Bottom - diameter,
                diameter,
                diameter,
                90,
                90);

            path.CloseFigure();

            return path;
        }
    }
}