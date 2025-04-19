using System;
using System.Drawing;
using System.Runtime.InteropServices; // Needed for DllImport
using System.Windows.Forms;
using Microsoft.Win32;

namespace Odin.Services
{
    // Internal helper class for the click-through overlay
    internal class ClickThroughForm : Form
    {
        // Define necessary WinAPI constants
        private const int WS_EX_LAYERED = 0x80000;
        private const int WS_EX_TRANSPARENT = 0x20;
        private const int GWL_EXSTYLE = -20;

        [DllImport("user32.dll", SetLastError = true)]
        private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll")]
        private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams cp = base.CreateParams;
                cp.ExStyle |= WS_EX_LAYERED | WS_EX_TRANSPARENT;
                return cp;
            }
        }

        // Optional: Override WndProc to further ensure transparency if needed,
        // but WS_EX_TRANSPARENT usually suffices.
        // protected override void WndProc(ref Message m) { ... }
    }


    public class DimmerService : IDisposable
    {
        // Declare as nullable (Form?) to handle CS8618
        private Form? overlayForm;
        private bool isVisible = false;
        private bool isDisposed = false;

        public DimmerService()
        {
            // overlayForm is initialized in InitializeOverlay, satisfying the non-null requirement after constructor logic flow.
            // Alternatively, InitializeOverlay() could be called directly here, but separating allows finer control if needed.
            InitializeOverlay(); // Ensure initialization happens
        }

        private void InitializeOverlay() // Made private as it's internal setup
        {
            // Check if already initialized (though unlikely needed with constructor call)
            if (overlayForm != null) return;

            overlayForm = new ClickThroughForm // Use the derived class
            {
                 Text = "Odin Dimmer Overlay", // Won't be visible
                 FormBorderStyle = FormBorderStyle.None,
                 StartPosition = FormStartPosition.Manual,
                 ShowInTaskbar = false,
                 BackColor = Color.Black, // Background color
                 // Opacity = 0.3, // Set Opacity AFTER handle creation or in Show()
                 TopMost = true, // Keep on top
            };
            // Subscribe AFTER form is created
             try { SystemEvents.DisplaySettingsChanged += OnDisplaySettingsChanged; } catch (Exception ex) { Console.WriteLine($"Warning: Failed to subscribe to DisplaySettingsChanged event for Dimmer: {ex.Message}"); }
        }


        public void SetDimLevel(float level)
        {
            // Check null before accessing overlayForm
            if (isDisposed || overlayForm == null) return;
            overlayForm.Opacity = Math.Clamp(level, 0.0f, 0.95f);
        }

        public void Show()
        {
             if (isDisposed || overlayForm == null || isVisible) return;
             try
             {
                UpdateOverlayBounds();
                // Ensure handle is created before setting opacity if not already shown
                if (!overlayForm.IsHandleCreated) overlayForm.CreateControl();
                // Set opacity here or use SetDimLevel which already does
                // overlayForm.Opacity = Math.Clamp(currentDimLevel, 0.0f, 0.95f); // Assuming currentDimLevel field exists
                overlayForm.Show();
                isVisible = true;
                overlayForm.TopMost = true; // Re-assert TopMost after showing
             }
              catch (ObjectDisposedException) { /* Ignore */ }
              catch (Exception ex) { Console.WriteLine($"Error showing dimmer overlay: {ex.Message}"); }
        }

        public void Hide()
        {
            if (isDisposed || overlayForm == null || !isVisible) return;
            try
            {
                overlayForm.Hide();
                isVisible = false;
            }
            catch (ObjectDisposedException) { /* Ignore */ }
            catch (Exception ex) { Console.WriteLine($"Error hiding dimmer overlay: {ex.Message}"); }
        }


        // Corrected OnDisplaySettingsChanged event handler signature (object? sender) (Fixes CS8622)
        private void OnDisplaySettingsChanged(object? sender, EventArgs e)
        {
            UpdateOverlayBounds();
        }

        private void UpdateOverlayBounds()
        {
             if (isDisposed || overlayForm == null) return;
             try
             {
                Rectangle totalBounds = SystemInformation.VirtualScreen;
                overlayForm.Bounds = totalBounds;
                if (isVisible) { overlayForm.TopMost = true; }
             }
             catch (ObjectDisposedException) { /* Ignore */ }
             catch (Exception ex) { Console.WriteLine($"Error updating dimmer bounds: {ex.Message}"); }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (isDisposed) return;

            if (disposing)
            {
                // Unsubscribe from events
                try { SystemEvents.DisplaySettingsChanged -= OnDisplaySettingsChanged; } catch (Exception ex) { Console.WriteLine($"Warning: Failed to unsubscribe from DisplaySettingsChanged event for Dimmer: {ex.Message}"); }

                // Dispose managed resources
                if (overlayForm != null)
                {
                    try
                    {
                        // Check if invoke required before closing/disposing UI element from non-UI thread
                        if (overlayForm.InvokeRequired)
                        {
                            overlayForm.Invoke((MethodInvoker)delegate {
                                overlayForm.Close(); // Close first
                                overlayForm.Dispose(); // Then dispose
                            });
                        }
                        else
                        {
                            overlayForm.Close();
                            overlayForm.Dispose();
                        }
                    }
                    catch (ObjectDisposedException) { /* Ignore */ }
                    catch (InvalidOperationException) { /* Ignore if handle not created/disposed */ }
                    overlayForm = null; // Set field to null (Fixes CS8625 warning source)
                }
            }
            isDisposed = true;
        }
    }
}