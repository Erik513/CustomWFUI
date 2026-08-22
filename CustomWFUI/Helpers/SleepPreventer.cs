using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace ErikwnkWFUI.Helpers
{
    /// <summary>
    /// Keeps the display and system from sleeping while active, e.g. during
    /// a long-running download or playback - call <see cref="PreventSleep"/>
    /// when starting and <see cref="AllowSleep"/> when done. Note:
    /// <c>SetThreadExecutionState</c> is per-thread, so call both from the
    /// same thread (typically the UI thread).
    /// </summary>
    public static class SleepPreventer
    {
        private static readonly ExecutionFlag PreventSleepFlags =
            ExecutionFlag.System |
            ExecutionFlag.Display |
            ExecutionFlag.Continuous;

        private static readonly ExecutionFlag RestoreSleepFlags =
            ExecutionFlag.Continuous;

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern uint SetThreadExecutionState(
            ExecutionFlag flags);

        [Flags]
        private enum ExecutionFlag : uint
        {
            System = 0x00000001,
            Display = 0x00000002,
            Continuous = 0x80000000
        }

        /// <summary>Starts preventing sleep - remember to call <see cref="AllowSleep"/> when done, this doesn't reset itself.</summary>
        public static void PreventSleep()
        {
            TrySetExecutionState(
                PreventSleepFlags,
                "prevent sleep");
        }

        /// <summary>Undoes <see cref="PreventSleep"/>.</summary>
        public static void AllowSleep()
        {
            TrySetExecutionState(
                RestoreSleepFlags,
                "restore sleep state");
        }

        private static void TrySetExecutionState(
            ExecutionFlag flags,
            string operation)
        {
            // SetThreadExecutionState reports failure via its return value (0),
            // not an exception - P/Invoke calls to it essentially never throw.
            uint previousState = SetThreadExecutionState(flags);

            if (previousState == 0)
            {
                int error = Marshal.GetLastWin32Error();
                Debug.WriteLine(
                    $"Failed to {operation}: SetThreadExecutionState returned 0 (Win32 error {error}).");
            }
        }
    }
}