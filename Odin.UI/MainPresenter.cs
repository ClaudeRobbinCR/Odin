using Odin.Models;         // Need reference to Models project
using Odin.Services;       // Need reference to Services project
using Odin.Utilities;      // Need reference to Utilities project
using Odin.UI.Forms;       // Need access to MainForm definition
using Serilog;             // Requires Serilog package
using System;
using System.ComponentModel; // Required for CancelEventArgs

namespace Odin.UI // Namespace should be Odin.UI
{
    public class MainPresenter
    {
        // Make view nullable, although it's required, for safer DI patterns
        private readonly MainForm? view;
        // Services can also be nullable if DI setup might fail, but typically required
        private readonly GammaService gammaService;
        private readonly DimmerService dimmerService;
        private readonly ReminderService reminderService;
        private readonly ConfigurationManager configManager;

        // Use nullable UserSettings to indicate it might not be loaded initially
        private UserSettings? currentSettings;

        // Constructor taking dependencies (usually resolved by DI)
        public MainPresenter(
            MainForm mainForm, // View instance
            GammaService gammaSvc,
            DimmerService dimmerSvc,
            ReminderService reminderSvc,
            ConfigurationManager configMgr) // cite: 76
        {
            // Assign dependencies, throw if required ones are null
            view = mainForm ?? throw new ArgumentNullException(nameof(mainForm));
            gammaService = gammaSvc ?? throw new ArgumentNullException(nameof(gammaSvc)); // cite: 77
            dimmerService = dimmerSvc ?? throw new ArgumentNullException(nameof(dimmerSvc)); // cite: 77
            reminderService = reminderSvc ?? throw new ArgumentNullException(nameof(reminderSvc)); // cite: 77
            configManager = configMgr ?? throw new ArgumentNullException(nameof(configMgr)); // cite: 77

            // Wire up view events AFTER view is constructed and passed in
            WireUpViewEvents(); // cite: 77
        }

        // Called once from Program.cs or MainForm constructor after presenter is created
        public void Initialize()
        {
             Log.Information("Presenter Initializing...");
             LoadSettings();
             ApplyInitialSettings();
             // Only update view if it's definitely assigned (it should be via constructor check)
             view?.UpdateStatus("Ready.");
             Log.Information("Presenter Initialized.");
        }

        // Subscribe to events raised by the View
        private void WireUpViewEvents() // cite: 78
        {
            if (view == null) return; // Safety check

            view.ViewLoaded += OnViewLoaded;
            view.ViewClosing += OnViewClosing; // Handle saving settings on close // cite: 79

            view.NightLightToggled += OnNightLightToggled; // cite: 78
            view.DimmerToggled += OnDimmerToggled; // cite: 79
            view.BreakReminderToggled += OnBreakReminderToggled; // cite: 79

            view.ColorTemperatureChanged += OnColorTemperatureChanged; // cite: 79
            view.DimLevelChanged += OnDimLevelChanged; // cite: 79
            view.BreakIntervalChanged += OnBreakIntervalChanged; // cite: 79
        }

        // --- Event Handlers ---

        private void OnViewLoaded(object? sender, EventArgs e) // Use object?
        {
             Log.Information("View Loaded event handled by presenter. Applying loaded settings to view controls.");
             // Apply loaded settings to view controls now that the view is loaded
             if (view != null && currentSettings != null)
             {
                 try
                 {
                     view.ColorTemperature = currentSettings.ColorTemperature;
                     view.DimLevel = currentSettings.DimLevel;
                     view.BreakIntervalMinutes = currentSettings.BreakIntervalMinutes;
                     Log.Debug("Applied settings to view: Temp={Temp}, Dim={Dim}, Interval={Interval}",
                         view.ColorTemperature, view.DimLevel, view.BreakIntervalMinutes);
                 }
                 catch (Exception ex)
                 {
                     Log.Error(ex, "Error applying loaded settings to view controls in OnViewLoaded.");
                     view?.ShowError($"Failed to apply initial settings: {ex.Message}");
                 }
             }
             else
             {
                 Log.Warning("Cannot apply settings to view in OnViewLoaded: View or Settings are null.");
             }
        }

        private void OnViewClosing(object? sender, CancelEventArgs e) // Use object?
        {
            Log.Information("View Closing event handled by presenter. Saving settings.");
            SaveSettings();
            DisposeServices(); // Dispose IDisposable services owned/managed here (if any)
        }

        private void OnNightLightToggled(object? sender, bool enabled) // Use object?
        {
            if (view == null) return;
            try
            {
                if (enabled)
                {
                    gammaService.ApplyNightLight(view.ColorTemperature); // cite: 80
                     Log.Information("Night light enabled. Temperature: {Temp}", view.ColorTemperature); // cite: 82 (Concept)
                     view.UpdateStatus($"Night Light ON ({view.ColorTemperature:P0})");
                }
                else
                {
                    gammaService.RestoreOriginalGamma(); // cite: 81
                    Log.Information("Night light disabled."); // cite: 82 (Concept)
                    view.UpdateStatus("Night Light OFF");
                }
                 // Update settings object immediately if loaded
                 if (currentSettings != null) currentSettings.ColorTemperature = view.ColorTemperature;
                 // Optional: Add IsNightLightEnabled flag to settings
                 // if (currentSettings != null) currentSettings.IsNightLightEnabledOnExit = enabled;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to toggle Night Light state to {Enabled}", enabled); // cite: 82
                view.ShowError($"Error applying Night Light: {ex.Message}"); // cite: 83 (Concept)
                view.UpdateStatus("Error applying Night Light!");
            }
        }

        private void OnDimmerToggled(object? sender, bool enabled) // Use object?
        {
             if (view == null) return;
             try
             {
                if (enabled)
                {
                    dimmerService.SetDimLevel(view.DimLevel); // cite: 84
                    dimmerService.Show(); // cite: 84
                    Log.Information("Dimmer enabled. Level: {Level}", view.DimLevel); // cite: 86 (Concept)
                    view.UpdateStatus($"Dimmer ON ({view.DimLevel:P0})");
                }
                else
                {
                    dimmerService.Hide(); // cite: 85
                    Log.Information("Dimmer disabled."); // cite: 86 (Concept)
                    view.UpdateStatus("Dimmer OFF");
                }
                if (currentSettings != null) currentSettings.DimLevel = view.DimLevel;
                // Optional: Add IsDimmerEnabled flag to settings
                // if (currentSettings != null) currentSettings.IsDimmerEnabledOnExit = enabled;
             }
             catch (Exception ex)
             {
                 Log.Error(ex, "Failed to toggle Dimmer state to {Enabled}", enabled); // cite: 86
                 view.ShowError($"Error applying Dimmer: {ex.Message}"); // cite: 87 (Concept)
                 view.UpdateStatus("Error applying Dimmer!");
             }
        }

        private void OnBreakReminderToggled(object? sender, bool enabled) // Use object?
        {
            if (view == null) return;
            try
            {
                if (enabled)
                {
                    reminderService.Start(view.BreakIntervalMinutes); // cite: 87
                    Log.Information("Break Reminder enabled. Interval: {Interval} mins", view.BreakIntervalMinutes); // cite: 89 (Concept)
                    view.UpdateStatus($"Reminders ON (Every {view.BreakIntervalMinutes} min)");
                }
                else
                {
                    reminderService.Stop(); // cite: 88
                    Log.Information("Break Reminder disabled."); // cite: 89 (Concept)
                    view.UpdateStatus("Reminders OFF");
                }
                 if (currentSettings != null) currentSettings.BreakIntervalMinutes = view.BreakIntervalMinutes;
                 // Optional: Add IsReminderEnabled flag to settings
                 // if (currentSettings != null) currentSettings.IsReminderEnabledOnExit = enabled;
            }
            catch (ArgumentOutOfRangeException ex) // Catch specific validation exception
            {
                 Log.Warning(ex, "Invalid interval for Break Reminder: {Interval}", view.BreakIntervalMinutes); // cite: 90 (Concept)
                 view.ShowError($"Invalid break interval: {ex.ParamName} must be between 1 and 120 minutes."); // cite: 90 (Concept)
                 view.UpdateStatus("Invalid reminder interval!");
                 // Maybe uncheck the box visually if start failed? Requires call back to view.
            }
            catch (Exception ex)
            {
                 Log.Error(ex, "Failed to toggle Break Reminder state to {Enabled}", enabled); // cite: 90 (Concept)
                 view.ShowError($"Error setting Break Reminder: {ex.Message}"); // cite: 90 (Concept)
                 view.UpdateStatus("Error setting Reminder!");
            }
        }

        private void OnColorTemperatureChanged(object? sender, float temperature) // Use object?
        {
             if (view == null) return;
             // Only apply if night light is active
             if (view.IsNightLightOn) // cite: 91
             {
                try
                {
                    gammaService.ApplyNightLight(temperature); // cite: 91
                     Log.Debug("Color temperature changed to: {Temp}", temperature);
                     view.UpdateStatus($"Night Light ON ({temperature:P0})");
                }
                catch (Exception ex)
                {
                     Log.Error(ex, "Failed to apply new color temperature: {Temp}", temperature);
                     view.ShowError($"Error applying color temperature: {ex.Message}");
                     view.UpdateStatus("Error setting temperature!");
                }
             }
              // Update setting regardless of active state
              if (currentSettings != null) currentSettings.ColorTemperature = temperature;
        }

        private void OnDimLevelChanged(object? sender, float level) // Use object?
        {
             if (view == null) return;
             // Only apply if dimmer is active
             if (view.IsDimmerOn) // cite: 92
             {
                 try
                 {
                    dimmerService.SetDimLevel(level); // cite: 92
                    Log.Debug("Dim level changed to: {Level}", level);
                    view.UpdateStatus($"Dimmer ON ({level:P0})");
                 }
                 catch (Exception ex)
                 {
                     Log.Error(ex, "Failed to apply new dim level: {Level}", level);
                     view.ShowError($"Error applying dim level: {ex.Message}");
                     view.UpdateStatus("Error setting dim level!");
                 }
             }
             // Update setting regardless of active state
             if (currentSettings != null) currentSettings.DimLevel = level;
        }

        private void OnBreakIntervalChanged(object? sender, int interval) // Use object?
        {
            if (view == null) return;
            // Update setting
            if (currentSettings != null) currentSettings.BreakIntervalMinutes = interval;

            // Only restart timer if active
            if (view.IsBreakReminderOn) // cite: 92
            {
                try
                {
                    // Use SetInterval which handles stop/start
                    reminderService.SetInterval(interval); // cite: 93 (Concept)
                    Log.Information("Break interval updated to: {Interval} mins", interval);
                    view.UpdateStatus($"Reminders ON (Every {interval} min)");
                }
                catch (ArgumentOutOfRangeException ex)
                {
                    Log.Warning(ex, "Invalid interval for Break Reminder: {Interval}", interval);
                    view.ShowError($"Invalid break interval: {ex.ParamName} must be between 1 and 120 minutes.");
                    view.UpdateStatus("Invalid reminder interval!");
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Failed to update break interval to: {Interval}", interval);
                    view.ShowError($"Error updating break interval: {ex.Message}");
                    view.UpdateStatus("Error updating interval!");
                }
            }
        }

        // --- Settings Handling ---

        private void LoadSettings()
        {
             Log.Debug("Loading settings...");
             currentSettings = configManager.LoadSettings(); // cite: 94 (Concept)
             Log.Information("Settings loaded: Temp={Temp}, Dim={Dim}, Interval={Interval}",
                 currentSettings.ColorTemperature, currentSettings.DimLevel, currentSettings.BreakIntervalMinutes);

             // Settings are loaded here, but applied to the view in OnViewLoaded
        }

         private void ApplyInitialSettings()
         {
             // This method would apply settings that persist state (like if Night Light was ON on exit)
             // Requires adding boolean flags to UserSettings model
             Log.Debug("ApplyInitialSettings: No persistent state configured to apply on startup.");
             // Example if flags existed:
             // if (currentSettings?.IsNightLightEnabledOnExit == true && view != null) {
             //     view.SetNightLightState(true); // Requires method on view to bypass event
             //     OnNightLightToggled(this, true); // Trigger logic
             // }
         }

        private void SaveSettings()
        {
             if (currentSettings == null || view == null)
             {
                 Log.Warning("Cannot save settings - settings object or view is null.");
                 return;
             }

             // Ensure current settings object reflects the latest view state before saving
             currentSettings.ColorTemperature = view.ColorTemperature; // cite: 95 (Concept)
             currentSettings.DimLevel = view.DimLevel; // cite: 95 (Concept)
             currentSettings.BreakIntervalMinutes = view.BreakIntervalMinutes; // cite: 95
             // Optional: Save toggle states if flags exist in UserSettings
             // currentSettings.IsNightLightEnabledOnExit = view.IsNightLightOn;
             // currentSettings.IsDimmerEnabledOnExit = view.IsDimmerOn;
             // currentSettings.IsReminderEnabledOnExit = view.IsBreakReminderOn;

             Log.Debug("Saving settings...");
             configManager.SaveSettings(currentSettings); // cite: 96
             Log.Information("Settings saved.");
        }

         private void DisposeServices()
         {
              Log.Debug("Disposing services from presenter...");
              // Dispose services that implement IDisposable IF the presenter owns them
              // In our DI setup (Program.cs), the DI container owns singletons,
              // so they shouldn't typically be disposed here unless scoped differently.
              // (gammaService as IDisposable)?.Dispose(); // Example if needed
              // (dimmerService as IDisposable)?.Dispose();
              // (reminderService as IDisposable)?.Dispose();
              Log.Information("Presenter-managed service disposal complete (if any).");
         }
    }
}