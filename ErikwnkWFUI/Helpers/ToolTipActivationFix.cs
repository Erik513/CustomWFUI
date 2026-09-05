using System;
using System.Windows.Forms;

namespace ErikwnkWFUI.Helpers
{
    /// <summary>
    /// Works around a WinForms/Windows quirk where a <see cref="ToolTip"/> can
    /// silently stop popping up once its owning top-level window has lost and
    /// regained focus (e.g. Alt-Tab) - the native tooltip control's internal
    /// activation state gets left off and normal mouse hover no longer
    /// reactivates it. Toggling <see cref="ToolTip.Active"/> off then on again
    /// resets that state; this hooks it automatically to the owning
    /// <see cref="Form"/>'s <see cref="Form.Activated"/> event, following the
    /// control through reparenting.
    /// </summary>
    public static class ToolTipActivationFix
    {
        /// <summary>
        /// Re-arms <paramref name="toolTip"/> every time the <see cref="Form"/>
        /// that (currently) contains <paramref name="owner"/> becomes active.
        /// Safe to call once per <see cref="ToolTip"/>/owner pair, as soon as
        /// the owner exists - re-hooks itself if the owner is later reparented
        /// into a different form, and unhooks on the owner's disposal.
        /// </summary>
        public static void ReviveOnFormActivate(this ToolTip toolTip, Control owner)
        {
            Form hooked = null;

            EventHandler rearm = (s, e) =>
            {
                toolTip.Active = false;
                toolTip.Active = true;
            };

            void Hook()
            {
                Form form = owner.FindForm();
                if (ReferenceEquals(form, hooked))
                    return;

                if (hooked != null)
                    hooked.Activated -= rearm;

                hooked = form;

                if (hooked != null)
                    hooked.Activated += rearm;
            }

            owner.ParentChanged += (s, e) => Hook();
            owner.HandleCreated += (s, e) => Hook();
            owner.Disposed += (s, e) =>
            {
                if (hooked != null)
                    hooked.Activated -= rearm;
            };

            Hook();
        }
    }
}
