using System;
using System.Drawing;
using System.Windows.Forms;
using ErikwnkWFUI.Forms;
using ErikwnkWFUI.Styles;

namespace ErikwnkWFUI.Controls
{
    /// <summary>
    /// A <see cref="SliderBar"/> preconfigured for a 0..1 volume value that
    /// shows the current percentage in a small popup above the thumb while the
    /// user drags it, then briefly after they let go, click once, or change
    /// the value with an arrow key. The consuming app only wires
    /// <see cref="SliderBar.ValueChanged"/> and sets <see cref="SliderBar.Value"/> -
    /// the readout is handled here.
    /// </summary>
    public class VolumeSlider : SliderBar
    {
        // How long the popup stays up once nothing is actively changing the
        // value anymore (drag released, single click, or the last arrow-key
        // press) - there's no single "interaction ended" event that covers
        // all three, so this timeout stands in for it everywhere, like a
        // tooltip. A plain click (mouse down then up with no movement) still
        // goes through DragStarted/DragEnded just as fast as any other
        // click, so without this the popup would flash on and instantly
        // back off again.
        private const int PopupHideDelayMs = 700;

        private InfoPopupForm _popup;
        private Timer _hideTimer;
        private Func<double, string> _formatValue;

        public VolumeSlider()
        {
            Maximum = 1.0;
            _formatValue = DefaultFormat;

            // ValueChanged alone would already cover dragging too (it fires
            // continuously on every mouse-move, restarting the hide timer
            // each time so it never gets 700ms of quiet to actually fire) -
            // DragStarted/DragEnded are only needed for the click-without-
            // moving edge case, where SetValueFromUser's no-op-if-unchanged
            // check means ValueChanged never fires at all.
            DragStarted += delegate { ShowPopup(); };
            ValueChanged += delegate { ShowPopup(); StartHideTimer(); };
            DragEnded += delegate { StartHideTimer(); };
        }

        private void StartHideTimer()
        {
            if (_hideTimer == null)
            {
                _hideTimer = new Timer { Interval = PopupHideDelayMs };
                _hideTimer.Tick += delegate
                {
                    _hideTimer.Stop();
                    HidePopup();
                };
            }

            // Restart rather than let an already-running one finish early -
            // repeated arrow presses (or a fresh drag right after a click)
            // should keep the popup up continuously, only hiding after the
            // last interaction.
            _hideTimer.Stop();
            _hideTimer.Start();
        }

        /// <summary>
        /// How the popup renders the 0..1 value. Default: whole percent, e.g.
        /// 0.1 -> "10%".
        /// </summary>
        public Func<double, string> FormatValue
        {
            get { return _formatValue; }
            set { _formatValue = value ?? DefaultFormat; }
        }

        private static string DefaultFormat(double value)
        {
            return (int)Math.Round(value * 100) + "%";
        }

        private InfoPopupForm Popup
        {
            get
            {
                if (_popup == null || _popup.IsDisposed)
                {
                    _popup = new InfoPopupForm { Compact = true };

                    // Fixed size for the widest value ("100%") so it doesn't
                    // jitter as digits are added/removed while dragging - just a
                    // few px of breathing room around the text, like a tooltip.
                    Size widest = TextRenderer.MeasureText(
                        _formatValue(1.0), UIFonts.Normal,
                        new Size(short.MaxValue, short.MaxValue),
                        TextFormatFlags.NoPadding | TextFormatFlags.SingleLine);
                    _popup.CompactSize = new Size(widest.Width + 8, widest.Height + 6);
                }
                return _popup;
            }
        }

        private void ShowPopup()
        {
            if (FindForm() == null)
                return;

            Popup.ShowCenteredAbove(_formatValue(Value), this, ThumbCenterX);
        }

        private void HidePopup()
        {
            if (_popup != null && !_popup.IsDisposed)
                _popup.Hide();
        }

        protected override void OnLostFocus(EventArgs e)
        {
            base.OnLostFocus(e);
            if (_hideTimer != null)
                _hideTimer.Stop();
            HidePopup();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (_hideTimer != null)
                {
                    _hideTimer.Stop();
                    _hideTimer.Dispose();
                    _hideTimer = null;
                }

                if (_popup != null)
                {
                    _popup.Dispose();
                    _popup = null;
                }
            }
            base.Dispose(disposing);
        }
    }
}
