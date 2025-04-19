using Serilog;
using System;
using System.IO;

namespace Odin.Utilities // Updated namespace
{
    public static class LoggerSetup
    {
        public static void Configure()
        {
            string logFilePath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "Odin", // Changed folder name
                "logs",
                "odin-.txt"); // Changed log file name pattern

            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.File(logFilePath, rollingInterval: RollingInterval.Day)
                .CreateLogger();
        }
    }
}