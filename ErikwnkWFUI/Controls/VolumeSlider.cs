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
    /// user drags it (and briefly on an arrow-key change). The consuming app
    /// only wires <see cref="SliderBar.ValueChanged"/> and sets
    /// <see cref="SliderBar.Value"/> - the readout is handled here.
    /// </summary>
    public class VolumeSlider : SliderBar
    {
        private InfoPopupForm _popup;
        private Func<double, string> _formatValue;

        public VolumeSlider()
        {
            Maximum = 1.0;
            _formatValue = DefaultFormat;

            DragStarted += delegate { ShowPopup(); };
            ValueChanged += delegate { if (IsDragging) ShowPopup(); };
            DragEnded += delegate { HidePopup(); };
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

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            if (!IsDragging)
                HidePopup();
        }

        protected override void OnLostFocus(EventArgs e)
        {
            base.OnLostFocus(e);
            HidePopup();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && _popup != null)
            {
                _popup.Dispose();
                _popup = null;
            }
            base.Dispose(disposing);
        }
    }
}
