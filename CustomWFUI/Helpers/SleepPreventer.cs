using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace CustomWFUI.Helpers
{
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

        public static void PreventSleep()
        {
            TrySetExecutionState(
                PreventSleepFlags,
                "prevent sleep");
        }

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
            try
            {
                SetThreadExecutionState(flags);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(
                    $"Failed to {operation}: {ex.Message}");
            }
        }
    }
}