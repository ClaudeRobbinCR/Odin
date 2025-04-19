using System;
using System.Windows.Forms;

namespace Odin.Services
{
    public class ReminderService : IDisposable
    {
        // Declare timer and icon as potentially nullable
        private System.Windows.Forms.Timer? timer;
        private NotifyIcon? trayIcon; // Passed in, make nullable

        // Keep track if disposed
        private bool isDisposed = false;

        public ReminderService(NotifyIcon notifyIcon) // Constructor still requires non-null icon
        {
             this.trayIcon = notifyIcon ?? throw new ArgumentNullException(nameof(notifyIcon), "NotifyIcon cannot be null for ReminderService.");

             timer = new System.Windows.Forms.Timer();
             timer.Tick += Timer_Tick; // Wire up event handler
             timer.Enabled = false;
        }

        // Use null-conditional operator ?. for safety
        public bool IsRunning => timer?.Enabled ?? false;

        public void Start(int intervalMinutes)
        {
            if (isDisposed || timer == null) return; // Check if disposed or timer is null

            if (intervalMinutes < 1 || intervalMinutes > 120)
                throw new ArgumentOutOfRangeException(nameof(intervalMinutes), "Interval must be between 1 and 120 minutes.");

            timer.Interval = intervalMinutes * 60 * 1000;
            timer.Start();
        }

        public void Stop()
        {
            if (isDisposed) return;
            timer?.Stop(); // Use null-conditional operator
        }

        public void SetInterval(int intervalMinutes)
        {
             if (isDisposed || timer == null) return; // Check if disposed or timer is null

             if (intervalMinutes < 1 || intervalMinutes > 120)
                 throw new ArgumentOutOfRangeException(nameof(intervalMinutes), "Interval must be between 1 and 120 minutes.");

             bool wasRunning = timer.Enabled;
             if (wasRunning) timer.Stop();
             timer.Interval = intervalMinutes * 60 * 1000;
             if (wasRunning) timer.Start();
        }

        // Corrected Timer_Tick event handler signature and logic
        // Use object? for sender to match EventHandler delegate nullability (Fixes CS8622)
        private void Timer_Tick(object? sender, EventArgs e)
        {
             // FIX: Remove .IsDisposed check. Only check if trayIcon is not null and Visible.
             if (trayIcon != null && trayIcon.Visible) // Fixes CS1061 & CS8602
             {
                 try
                 {
                    trayIcon.ShowBalloonTip(
                        5000,
                        "Odin: Cognitive Break",
                        "Pause visual input. Look away for 20 seconds.",
                        ToolTipIcon.Info
                    );
                 }
                 catch(ObjectDisposedException)
                 {
                    // Handle case where icon might get disposed externally between checks
                    Console.WriteLine("Warning: Reminder Tick - TrayIcon was disposed unexpectedly.");
                    Stop(); // Stop timer if icon is gone
                 }
                 catch(Exception ex) // Catch other potential errors
                 {
                    Console.WriteLine($"Error showing balloon tip: {ex.Message}");
                 }
             }
             else
             {
                 // Log or handle if icon isn't available (it might have been closed/disposed)
                 if (trayIcon == null) {
                    Console.WriteLine("Warning: Reminder Tick - TrayIcon is null. Stopping timer.");
                    Stop(); // Stop if icon reference lost
                 }
             }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (isDisposed) return; // Prevent double disposal

            if (disposing)
            {
                // Dispose managed resources
                if (timer != null)
                {
                    timer.Stop();
                    // Safely unsubscribe event handler
                    timer.Tick -= Timer_Tick;
                    timer.Dispose();
                    timer = null; // Set field to null (Fixes CS8625 warning source)
                }
                // Set trayIcon reference to null, but DO NOT dispose it here
                // as it's owned by MainForm
                trayIcon = null; // Set field to null (Fixes CS8625 warning source)
            }

            // Free unmanaged resources (if any)

            isDisposed = true;
        }
    }
}