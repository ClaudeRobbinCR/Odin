using Odin.Models; // Ensure using statement is correct
using Newtonsoft.Json;
using System;
using System.IO;
using Serilog; // Add using for logging if you want to use Log instead of Console

namespace Odin.Utilities // Ensure namespace is correct
{
    public class ConfigurationManager
    {
        private static readonly string ConfigFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Odin");
        private static readonly string ConfigFile = Path.Combine(ConfigFolder, "appsettings.json");

        public UserSettings LoadSettings() // cite: 227
        {
            try
            {
                if (File.Exists(ConfigFile)) // cite: 227
                {
                    string json = File.ReadAllText(ConfigFile); // cite: 228
                    // Use ?? new UserSettings() as a fallback if deserialization returns null
                    return JsonConvert.DeserializeObject<UserSettings>(json) ?? new UserSettings(); // cite: 228
                }
            }
            catch (Exception ex)
            {
                // Log the error (using Console or Serilog if configured)
                // Corrected Line: Added closing parenthesis and semicolon
                Console.WriteLine($"Error loading Odin settings: {ex.Message}");
                // If using Serilog: Log.Error(ex, "Failed to load Odin settings from {ConfigFile}", ConfigFile);
            }
            // Return default settings if file doesn't exist or loading failed
            return new UserSettings(); // cite: 228
        } // <-- This closing brace likely corresponds to line 15 error if the inner code was wrong

        public void SaveSettings(UserSettings settings) // cite: 229
        {
            try
            {
                Directory.CreateDirectory(ConfigFolder); // Ensure directory exists
                string json = JsonConvert.SerializeObject(settings, Formatting.Indented); // cite: 229
                File.WriteAllText(ConfigFile, json); // cite: 230
            }
            catch (Exception ex)
            {
                // Log the error
                // Corrected Line: Added closing parenthesis and semicolon
                Console.WriteLine($"Error saving Odin settings: {ex.Message}");
                // If using Serilog: Log.Error(ex, "Failed to save Odin settings to {ConfigFile}", ConfigFile);
                // Optionally inform the user via UI if saving failed critically
            }
        } // <-- Closing brace for the method
    } // <-- Closing brace for the class
} // <-- Closing brace for the namespace