using System;
using System.Drawing;
using System.Windows.Forms;
using ErikwnkWFUI.Styles;

namespace ErikwnkWFUI.Controls
{
    /// <summary>
    /// A thin, fully custom-drawn progress bar - unlike <see cref="ProgressBar"/>,
    /// there is no native Win32 control underneath, so none of the
    /// theming/non-client-border quirks that control needs working around
    /// apply here. Meant for slim status-strip-style progress indicators
    /// (e.g. a background task's progress shown as a thin strip under a
    /// status label) rather than a prominent, full-size bar - see
    /// <see cref="UIStyles.ProgressBars"/> for that.
    /// </summary>
    public class SlimProgressBar : Control
    {
        private const int DefaultHeight = 4;

        private int _minimum;
        private int _maximum = 100;
        private int _value;

        public SlimProgressBar()
        {
            SetStyle(
                ControlStyles.AllPaintingInWmPaint |
                ControlStyles.UserPaint |
                ControlStyles.OptimizedDoubleBuffer,
                true);

            DoubleBuffered = true;
            Height = DefaultHeight;
        }

        public int Minimum
        {
            get { return _minimum; }
            set
            {
                _minimum = value;
                ClampValue();
                Invalidate();
            }
        }

        public int Maximum
        {
            get { return _maximum; }
            set
            {
                _maximum = value;
                ClampValue();
                Invalidate();
            }
        }

        public int Value
        {
            get { return _value; }
            set
            {
                int clamped = Clamp(value, _minimum, _maximum);

                if (clamped == _value)
                    return;

                _value = clamped;
                Invalidate();
            }
        }

        /// <summary>
        /// When true, the fill color follows <see cref="Value"/> instead of
        /// <see cref="Control.ForeColor"/> - red at <see cref="Minimum"/>,
        /// yellow at the midpoint, green at <see cref="Maximum"/>, blending
        /// smoothly between them. For a "status/health" bar where the color
        /// itself communicates good/bad, not just the fill amount.
        /// </summary>
        public bool UseStatusGradient { get; set; }

        protected override void OnEnabledChanged(EventArgs e)
        {
            base.OnEnabledChanged(e);

            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            Graphics g = e.Graphics;

            using (SolidBrush trackBrush = new SolidBrush(BackColor))
                g.FillRectangle(trackBrush, ClientRectangle);

            double range = _maximum - _minimum;
            double fraction = range > 0 ? (_value - _minimum) / range : 0;
            int fillWidth = (int)Math.Round(Width * fraction);

            if (fillWidth <= 0)
                return;

            Color fillColor;

            if (!Enabled)
                fillColor = UIColors.DisabledGray;
            else if (UseStatusGradient)
                fillColor = ComputeStatusColor(_value, _minimum, _maximum);
            else
                fillColor = ForeColor;

            using (SolidBrush fillBrush = new SolidBrush(fillColor))
                g.FillRectangle(fillBrush, new Rectangle(0, 0, fillWidth, Height));
        }

        private void ClampValue()
        {
            _value = Clamp(_value, _minimum, _maximum);
        }

        private static int Clamp(int value, int min, int max)
        {
            if (max < min)
                max = min;

            return Math.Max(min, Math.Min(max, value));
        }

        // Two-segment blend - red to yellow, then yellow to green - rather
        // than a straight red-to-green blend, which would pass through a
        // muddy brown/olive around the midpoint instead of a clear yellow
        // "midway" cue. The breakpoint between the two segments is NOT the
        // midpoint (0.5): green reading as "done" should only dominate near
        // the top of the range, so the red->yellow segment gets the larger
        // share (0-65%) and only the last third blends yellow->green - a
        // flat 50/50 split turned green too early ("es wird zu schnell
        // grün"). Same logic as UIProgressBarFactory's BorderedProgressBar.
        // ComputeStatusColor - duplicated rather than shared, matching this
        // codebase's existing pattern of small per-control color helpers
        // (see BlendTowardColor in ToggleSwitch.cs/UICheckBoxFactory.cs)
        // rather than one central utility.
        private const double YellowBreakpoint = 0.65;

        private static Color ComputeStatusColor(int value, int minimum, int maximum)
        {
            double range = maximum - minimum;
            double fraction = range > 0 ? (value - minimum) / range : 0;
            fraction = Math.Max(0, Math.Min(1, fraction));

            if (fraction <= YellowBreakpoint)
                return Lerp(UIColors.Red, UIColors.Yellow, fraction / YellowBreakpoint);

            return Lerp(UIColors.Yellow, UIColors.Green, (fraction - YellowBreakpoint) / (1 - YellowBreakpoint));
        }

        private static Color Lerp(Color from, Color to, double t)
        {
            t = Math.Max(0, Math.Min(1, t));

            return Color.FromArgb(
                (int)Math.Round(from.R + (to.R - from.R) * t),
                (int)Math.Round(from.G + (to.G - from.G) * t),
                (int)Math.Round(from.B + (to.B - from.B) * t));
        }
    }
}
