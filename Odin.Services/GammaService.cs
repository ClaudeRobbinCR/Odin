using System;
using System.Drawing; // Add reference: Right-click project > Add > Reference > Assemblies > Framework > System.Drawing (if needed for older .NET versions) OR ensure <UseWindowsForms>true</UseWindowsForms> in csproj
using System.Runtime.InteropServices;
using Microsoft.Win32; // Required for SystemEvents

using Serilog; // Add Serilog

namespace Odin.Services // Updated namespace
{
    // NOTE: Ensure <UseWindowsForms>true</UseWindowsForms> is in Odin.Services.csproj
    public class GammaService : IDisposable
    {
        private static readonly ILogger _log = Log.ForContext<GammaService>(); // Add logger instance

        [DllImport("gdi32.dll")]
        private static extern bool SetDeviceGammaRamp(IntPtr hDC, ref RAMP lpRamp);

        [DllImport("gdi32.dll")]
        private static extern bool GetDeviceGammaRamp(IntPtr hDC, ref RAMP lpRamp);

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
        private struct RAMP
        {
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 256)]
            public ushort[] Red;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 256)]
            public ushort[] Green;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 256)]
            public ushort[] Blue;
        }

        private RAMP originalRamp;
        private bool hasOriginalRamp = false;
        private bool isNightLightApplied = false;
        private float currentTemperature = 0.0f;

        public GammaService()
        {
            BackupOriginalGammaRamp();
            try
            {
                SystemEvents.DisplaySettingsChanged += OnDisplaySettingsChanged;
            }
            catch (Exception ex)
            {
                _log.Warning(ex, "Failed to subscribe to DisplaySettingsChanged event.");
                // Functionality might degrade if display changes aren't handled
            }
        }

        public void ApplyNightLight(float temperature) // cite: 194
        {
            if (temperature < 0.0f || temperature > 1.0f)
                temperature = Math.Clamp(temperature, 0.0f, 1.0f);

            currentTemperature = temperature;
            RAMP ramp = new RAMP // cite: 194
            {
                Red = new ushort[256], // cite: 194
                Green = new ushort[256], // cite: 194
                Blue = new ushort[256] // cite: 195
            };

            for (int i = 0; i < 256; i++) // cite: 196
            {
                double linearValue = (double)i / 255.0;

                double redFactor = 1.0; // cite: 196
                double greenFactor = 1.0 - (temperature * 0.3); // Adjusted factor // cite: 197
                double blueFactor = 1.0 - (temperature * 0.6);  // Adjusted factor // cite: 197

                // Apply minimum thresholds to prevent colors going too low or inverting
                greenFactor = Math.Max(0.4, greenFactor); // cite: 197, 198 (Concept derived)
                blueFactor = Math.Max(0.2, blueFactor); // cite: 197, 198 (Concept derived)

                // Apply gamma correction (e.g., 2.2) for perceptual linearity
                double gamma = 2.2;
                ramp.Red[i]   = (ushort)Math.Min(65535, Math.Pow(linearValue * redFactor,   1.0 / gamma) * 65535 + 0.5); // cite: 198, 199 (Concept derived)
                ramp.Green[i] = (ushort)Math.Min(65535, Math.Pow(linearValue * greenFactor, 1.0 / gamma) * 65535 + 0.5); // cite: 198, 199 (Concept derived)
                ramp.Blue[i]  = (ushort)Math.Min(65535, Math.Pow(linearValue * blueFactor,  1.0 / gamma) * 65535 + 0.5); // cite: 199
            }

            IntPtr hdc = GetDC(IntPtr.Zero); // cite: 200 (Concept: Get Handle)
            if (hdc != IntPtr.Zero)
            {
                SetDeviceGammaRamp(hdc, ref ramp); // cite: 200
                ReleaseDC(IntPtr.Zero, hdc); // cite: 200 (Concept: Release Handle)
                isNightLightApplied = true;
            }
            else
            {
                _log.Error("Could not get screen device context (hDC) to apply gamma.");
                // Consider throwing an exception if this failure is critical
            }
        }

        public void RestoreOriginalGamma() // cite: 201
        {
            _log.Debug("Attempting to restore original gamma ramp. HasOriginalRamp: {HasRamp}", hasOriginalRamp);
            if (hasOriginalRamp) // cite: 201
            {
                 IntPtr hdc = GetDC(IntPtr.Zero); // cite: 201
                 if (hdc != IntPtr.Zero)
                 {
                    bool success = SetDeviceGammaRamp(hdc, ref originalRamp); // cite: 201
                    ReleaseDC(IntPtr.Zero, hdc); // cite: 201
                    if (success)
                    {
                        _log.Information("Successfully restored original gamma ramp.");
                        isNightLightApplied = false;
                    }
                    else
                    {
                        _log.Error("SetDeviceGammaRamp failed when trying to restore original ramp.");
                    }
                 }
                 else
                 {
                    _log.Error("Could not get screen device context (hDC) to restore gamma.");
                 }
            }
            else
            {
                _log.Warning("Cannot restore original gamma ramp because it was not successfully backed up.");
            }
        }

        private void BackupOriginalGammaRamp() // cite: 202
        {
            _log.Debug("Attempting to back up original gamma ramp...");
            originalRamp = new RAMP // cite: 202
            {
                Red = new ushort[256], // cite: 202
                Green = new ushort[256], // cite: 203
                Blue = new ushort[256] // cite: 203
            };
            IntPtr hdc = GetDC(IntPtr.Zero); // cite: 204 (Concept: Get Handle)
            if (hdc != IntPtr.Zero)
            {
                hasOriginalRamp = GetDeviceGammaRamp(hdc, ref originalRamp); // cite: 205
                ReleaseDC(IntPtr.Zero, hdc); // cite: 205 (Concept: Release Handle)

                if (!hasOriginalRamp)
                {
                    _log.Warning("GetDeviceGammaRamp failed. Initializing a default linear ramp as fallback original.");
                    // Initialize with a linear ramp as a fallback
                    for (int i = 0; i < 256; i++)
                    {
                         ushort val = (ushort)(i * 256); // Linear ramp
                         originalRamp.Red[i] = val;
                         originalRamp.Green[i] = val;
                         originalRamp.Blue[i] = val;
                    }
                    hasOriginalRamp = true; // Assume we can still set it
                }
            }
            else
            {
                _log.Error("Could not get screen device context (hDC) for gamma backup.");
                hasOriginalRamp = false;
            }
        }

        private void OnDisplaySettingsChanged(object? sender, EventArgs e)
        {
            _log.Information("Display settings changed event detected. Re-evaluating gamma...");
            BackupOriginalGammaRamp();
            if (isNightLightApplied)
            {
                _log.Information("Re-applying night light due to display settings change...");
                // Use a small delay if changes happen rapidly, although often not needed
                // System.Threading.Thread.Sleep(50); // Optional small delay
                ApplyNightLight(currentTemperature);
            }
        }

        [DllImport("user32.dll")]
        private static extern IntPtr GetDC(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern int ReleaseDC(IntPtr hWnd, IntPtr hDC);

        public void Dispose()
        {
            _log.Debug("Disposing GammaService, restoring original gamma...");
            RestoreOriginalGamma();
             try
             {
                 SystemEvents.DisplaySettingsChanged -= OnDisplaySettingsChanged;
             }
             catch (Exception ex)
             {
                 _log.Warning(ex, "Failed to unsubscribe from DisplaySettingsChanged event during Dispose.");
             }
            GC.SuppressFinalize(this);
        }
    }
}