namespace Odin.Models // Ensure the namespace matches your project
{
    public class UserSettings
    {
        // Default value for color temperature (e.g., 70% warm) [cite: 64, 224]
        public float ColorTemperature { get; set; } = 0.7f;

        // Default value for dim level (e.g., 30% dim) [cite: 65, 224]
        public float DimLevel { get; set; } = 0.3f;

        // Default value for break interval in minutes [cite: 65, 225]
        public int BreakIntervalMinutes { get; set; } = 20;

        // Optional: Add flags to remember if features were enabled on last exit
        // public bool IsNightLightEnabledOnExit { get; set; } = false;
        // public bool IsDimmerEnabledOnExit { get; set; } = false;
        // public bool IsReminderEnabledOnExit { get; set; } = false;
    }
}