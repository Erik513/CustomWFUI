using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace ErikwnkWFUI.Controls
{
    /// <summary>
    /// A slim, theme-matching horizontal slider (WinForms has none of its own).
    /// Value runs from 0 to <see cref="Maximum"/>. <see cref="ValueChanged"/>
    /// fires on every change from the user (drag, track click, arrow keys); it
    /// does not fire when <see cref="Value"/> is set from code.
    /// </summary>
    public class SliderBar : Control
    {
        private double _value;
        private double _maximum = 1.0;
        private bool _dragging;
        private bool _hover;

        private const int TrackHeight = 4;
        private const int ThumbRadius = 6;

        /// <summary>
        /// Pixels the track is inset from each edge (room for the thumb).
        /// Anything meant to line up with the track (e.g. a progress bar next to
        /// the slider) should use the same left/right margin.
        /// </summary>
        public const int TrackInset = ThumbRadius + 1;

        public SliderBar()
        {
            SetStyle(
                ControlStyles.OptimizedDoubleBuffer
                | ControlStyles.AllPaintingInWmPaint
                | ControlStyles.UserPaint
                | ControlStyles.ResizeRedraw
                | ControlStyles.SupportsTransparentBackColor
                | ControlStyles.Selectable,
                true);
            Height = 20;
            TabStop = true;
            BackColor = Color.Transparent;
        }

        /// <summary>Raised when the user changes the value (not when code sets it).</summary>
        public event EventHandler ValueChanged;

        /// <summary>Raised when the user presses the thumb/track to begin a drag.</summary>
        public event EventHandler DragStarted;

        /// <summary>Raised when the user releases the mouse after a drag.</summary>
        public event EventHandler DragEnded;

        /// <summary>True while the user is dragging the thumb - callers can pause external updates.</summary>
        public bool IsDragging
        {
            get { return _dragging; }
        }

        public double Maximum
        {
            get { return _maximum; }
            set
            {
                _maximum = Math.Max(0.0001, value);
                if (_value > _maximum)
                {
                    _value = _maximum;
                }
                Invalidate();
            }
        }

        /// <summary>Setting this from code updates the display without raising <see cref="ValueChanged"/>.</summary>
        public double Value
        {
            get { return _value; }
            set
            {
                double clamped = Clamp(value, 0, _maximum);
                if (Math.Abs(clamped - _value) < double.Epsilon)
                {
                    return;
                }
                _value = clamped;
                Invalidate();
            }
        }

        /// <summary>Pixel x of the thumb centre - for positioning something (e.g. a value popup) over it.</summary>
        protected int ThumbCenterX
        {
            get
            {
                double frac = _maximum <= 0 ? 0 : _value / _maximum;
                return TrackLeft + (int)Math.Round(frac * TrackWidth);
            }
        }

        private static double Clamp(double v, double min, double max)
        {
            if (v < min) return min;
            if (v > max) return max;
            return v;
        }

        private void SetValueFromUser(double v)
        {
            double clamped = Clamp(v, 0, _maximum);
            if (Math.Abs(clamped - _value) < double.Epsilon)
            {
                return;
            }
            _value = clamped;
            Invalidate();
            if (ValueChanged != null)
            {
                ValueChanged(this, EventArgs.Empty);
            }
        }

        private int TrackLeft
        {
            get { return TrackInset; }
        }

        private int TrackRight
        {
            get { return Width - TrackInset; }
        }

        private int TrackWidth
        {
            get { return Math.Max(1, TrackRight - TrackLeft); }
        }

        private double ValueFromX(int x)
        {
            return (x - TrackLeft) / (double)TrackWidth * _maximum;
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            if (e.Button == MouseButtons.Left && Enabled)
            {
                Focus();
                _dragging = true;
                if (DragStarted != null)
                {
                    DragStarted(this, EventArgs.Empty);
                }
                SetValueFromUser(ValueFromX(e.X));
            }
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            if (_dragging)
            {
                SetValueFromUser(ValueFromX(e.X));
            }
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            if (_dragging)
            {
                _dragging = false;
                if (DragEnded != null)
                {
                    DragEnded(this, EventArgs.Empty);
                }
            }
        }

        protected override void OnMouseEnter(EventArgs e)
        {
            base.OnMouseEnter(e);
            _hover = true;
            Invalidate();
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            _hover = false;
            Invalidate();
        }

        protected override bool IsInputKey(Keys keyData)
        {
            if (keyData == Keys.Left || keyData == Keys.Right
                || keyData == Keys.Home || keyData == Keys.End)
            {
                return true;
            }
            return base.IsInputKey(keyData);
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);
            if (!Enabled)
            {
                return;
            }

            double step = _maximum / 20.0;   // ~5% per arrow press
            switch (e.KeyCode)
            {
                case Keys.Left:
                    SetValueFromUser(_value - step);
                    e.Handled = true;
                    break;
                case Keys.Right:
                    SetValueFromUser(_value + step);
                    e.Handled = true;
                    break;
                case Keys.Home:
                    SetValueFromUser(0);
                    e.Handled = true;
                    break;
                case Keys.End:
                    SetValueFromUser(_maximum);
                    e.Handled = true;
                    break;
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            int midY = Height / 2;
            int trackTop = midY - TrackHeight / 2;
            double frac = _maximum <= 0 ? 0 : _value / _maximum;
            int thumbX = TrackLeft + (int)Math.Round(frac * TrackWidth);

            Color trackColor = UIStyles.Colors.BorderMedium;
            Color fillColor = Enabled ? UIStyles.Colors.Primary : UIStyles.Colors.TextDisabled;

            using (SolidBrush track = new SolidBrush(trackColor))
            {
                g.FillRectangle(track, TrackLeft, trackTop, TrackWidth, TrackHeight);
            }
            using (SolidBrush fill = new SolidBrush(fillColor))
            {
                g.FillRectangle(fill, TrackLeft, trackTop, Math.Max(0, thumbX - TrackLeft), TrackHeight);
            }

            if (Enabled)
            {
                // The thumb grows slightly on hover, drag or keyboard focus -
                // that is the only focus cue (a dotted rectangle around a slider
                // looks out of place).
                int r = _hover || _dragging || Focused ? ThumbRadius : ThumbRadius - 1;
                using (SolidBrush thumb = new SolidBrush(fillColor))
                {
                    g.FillEllipse(thumb, thumbX - r, midY - r, r * 2, r * 2);
                }
            }
        }
    }
}
