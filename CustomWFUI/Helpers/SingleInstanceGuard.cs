using System;
using System.Threading;

namespace ErikwnkWFUI.Helpers
{
    /// <summary>
    /// Detects whether another instance of this app is already running, via
    /// a named <see cref="Mutex"/> - the standard .NET single-instance
    /// pattern. Create one as early as possible in <c>Main</c>, check
    /// <see cref="IsFirstInstance"/>, and keep the guard alive (e.g. in a
    /// <c>using</c> around <c>Application.Run</c>) for as long as the app
    /// should hold the lock - disposing it releases the mutex, letting a
    /// later instance become "first" again.
    /// </summary>
    /// <example>
    /// <code>
    /// using var guard = new SingleInstanceGuard("MyApp");
    /// if (!guard.IsFirstInstance)
    /// {
    ///     MessageBox.Show("MyApp is already running.");
    ///     return;
    /// }
    /// Application.Run(new MainForm());
    /// </code>
    /// </example>
    public sealed class SingleInstanceGuard : IDisposable
    {
        private readonly Mutex _mutex;
        private bool _isDisposed;

        /// <summary>True if this is the only running instance (the mutex was newly created, not already held).</summary>
        public bool IsFirstInstance { get; }

        /// <summary>
        /// <paramref name="appName"/> should be unique to this app (e.g. its
        /// product name) - it becomes part of the mutex name, so two
        /// different apps must not pass the same value or they'll count as
        /// the same "instance". Prefixed with <c>Global\</c> so the check
        /// holds across Windows sessions/users, not just within one.
        /// </summary>
        public SingleInstanceGuard(string appName)
        {
            if (string.IsNullOrWhiteSpace(appName))
                throw new ArgumentException("appName must not be empty.", nameof(appName));

            _mutex = new Mutex(true, "Global\\" + appName, out bool createdNew);
            IsFirstInstance = createdNew;
        }

        public void Dispose()
        {
            if (_isDisposed)
                return;

            _isDisposed = true;

            // Only release what this instance actually acquired - a second
            // instance's mutex handle exists but was never owned (createdNew
            // was false for it), and releasing an unowned mutex throws.
            if (IsFirstInstance)
                _mutex.ReleaseMutex();

            _mutex.Dispose();
        }
    }
}
