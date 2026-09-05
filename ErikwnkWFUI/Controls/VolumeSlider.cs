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
            // DragStarted is only needed for the click-without-moving edge
            // case, where SetValueFromUser's no-op-if-unchanged check means
            // ValueChanged never fires at all.
            DragStarted += delegate { ShowPopup(); };
            ValueChanged += delegate { ShowPopup(); StartHideTimer(); };

            // Forced (bypasses the throttle below) - releasing a fast drag
            // right on 0% or 100% could otherwise land its very last
            // ValueChanged inside the throttle window, leaving the popup
            // stuck showing a slightly-stale value forever (the drag is
            // over, so nothing else would ever trigger another update).
            DragEnded += delegate { ShowPopup(force: true); StartHideTimer(); };

            // Same problem, same fix, for holding an arrow key: OS key-repeat
            // can outrun the throttle just like a fast drag can, so the last
            // repeat before release might get skipped, stranding the popup on
            // a slightly-stale value (e.g. 95% instead of 100%). KeyUp fires
            // even though IsInputKey claims these keys - it only changes
            // which keys generate KeyDown/KeyUp, not whether they do.
            KeyUp += delegate { ShowPopup(force: true); StartHideTimer(); };
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

        // Narrowly scoped to just around a ShowPopup() call (see below) -
        // deliberately NOT "is the popup currently visible", which was tried
        // first and was wrong: the popup is meant to stay visible for up to
        // PopupHideDelayMs after ANY interaction, so that check treated
        // every focus loss in that whole window - including a deliberate
        // click on some other control entirely - as "my own popup stole
        // focus" and kept stealing it back, blocking that other control.
        private bool _isSelfFocusChurn;

        // Guards against scheduling a fresh BeginInvoke below on every
        // single ShowPopup() call - which, during a fast drag, fires on
        // every mouse-move (dozens of times a second). Each BeginInvoke
        // posts a real message through the window's message queue; doing
        // that at mouse-move frequency was enough overhead to visibly stall
        // unrelated things driven by the same message loop, like the
        // Showcase's own ProgressBar animation timer. Only one clear is
        // ever pending at a time - later calls just keep _isSelfFocusChurn
        // true for longer, which is exactly what's wanted anyway.
        private bool _focusChurnClearPending;

        // ShowCenteredAbove (specifically its unconditional BringToFront()
        // call, which forces a Z-order/DWM update every single time) turned
        // out to be the real cost, not the BeginInvoke above - it runs once
        // per ValueChanged, i.e. once per mouse-move during a drag, dozens
        // of times a second, and that alone was enough to visibly stall the
        // Showcase's ProgressBar animation. A percentage readout doesn't
        // need to redraw faster than this to still look instant, so actual
        // updates are capped to this interval; ShowPopup() calls in between
        // just keep the already-visible popup up (via the hide timer) with
        // whatever value it last showed.
        private const int MinPopupUpdateIntervalMs = 40;
        private int _lastPopupUpdateTickCount = int.MinValue;

        // force bypasses the throttle - used by DragEnded to guarantee the
        // final value is always shown, even if it lands inside the window.
        private void ShowPopup(bool force = false)
        {
            if (FindForm() == null)
                return;

            int now = Environment.TickCount;
            bool alreadyVisible = _popup != null && !_popup.IsDisposed && _popup.Visible;
            if (!force && alreadyVisible && unchecked(now - _lastPopupUpdateTickCount) < MinPopupUpdateIntervalMs)
                return;

            _lastPopupUpdateTickCount = now;

            _isSelfFocusChurn = true;
            Popup.ShowCenteredAbove(_formatValue(Value), this, ThumbCenterX);

            if (_focusChurnClearPending)
                return;

            _focusChurnClearPending = true;

            // Cleared on the NEXT message-loop iteration, not immediately -
            // Show()'s transient focus loss can arrive slightly after this
            // call returns, not only synchronously within it.
            BeginInvoke(new MethodInvoker(delegate
            {
                _focusChurnClearPending = false;
                _isSelfFocusChurn = false;
            }));
        }

        private void HidePopup()
        {
            if (_popup != null && !_popup.IsDisposed)
                _popup.Hide();
        }

        protected override void OnLostFocus(EventArgs e)
        {
            base.OnLostFocus(e);

            if (_isSelfFocusChurn)
            {
                // Our own popup's Show()/BringToFront() just took Win32
                // focus, not the user moving away - reclaim it once the
                // current message (which might still be Show()'s own
                // internal pump) has fully unwound. Reclaiming inline here
                // was tried and made things measurably worse during a fast
                // drag: Show() pumps messages internally, so a queued
                // MouseMove could dispatch and recurse back into
                // ShowPopup() while Show() was still on the call stack,
                // corrupting the popup window (UI freezes, a popup that
                // never finished painting).
                BeginInvoke(new MethodInvoker(delegate
                {
                    if (!IsDisposed && !Focused)
                        Focus();
                }));
                return;
            }

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
