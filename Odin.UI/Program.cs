using Odin.Services;
using Odin.UI.Forms;
using Odin.Utilities;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using System;
using System.Threading;
using System.Windows.Forms;

namespace Odin.UI
{
    static class Program
    {
        // Fix CS8625: Declare mutex as nullable
        private static Mutex? mutex = null;
        const string AppName = "OdinApp"; // Unique name for mutex

        [STAThread]
        static void Main()
        {
            bool createdNew;
            mutex = new Mutex(true, AppName, out createdNew);

            if (!createdNew)
            {
                MessageBox.Show("Odin is already running.", "Instance Exists", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            LoggerSetup.Configure();
            Log.Information("Odin Application Starting...");

            Application.ThreadException += Application_ThreadException;
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            // --- Dependency Injection Setup ---
            var services = new ServiceCollection();
            ConfigureServices(services); // Call ConfigureServices
            var serviceProvider = services.BuildServiceProvider();

            // --- Resolve MainForm FIRST to get NotifyIcon ---
            MainForm? mainFormInstance = null;
            NotifyIcon? trayIconInstance = null;
            try
            {
                // Resolve services that MainForm and MainPresenter depend on
                var gammaService = serviceProvider.GetRequiredService<GammaService>();
                var dimmerService = serviceProvider.GetRequiredService<DimmerService>();
                var configManager = serviceProvider.GetRequiredService<ConfigurationManager>(); // Assuming MainPresenter needs this

                // Manually create MainForm
                // Ensure MainForm constructor takes GammaService and DimmerService
                mainFormInstance = new MainForm(gammaService, dimmerService);

                // Get NotifyIcon from MainForm
                trayIconInstance = mainFormInstance.GetTrayIcon();

                if (trayIconInstance == null)
                {
                    throw new InvalidOperationException("TrayIcon could not be retrieved from MainForm.");
                }

                // Manually create ReminderService
                var reminderServiceInstance = new ReminderService(trayIconInstance);

                // Manually create MainPresenter, passing all dependencies including the MainForm instance
                // Ensure MainPresenter constructor takes MainForm, GammaService, DimmerService, ReminderService, and ConfigurationManager
                var presenter = new MainPresenter(mainFormInstance, gammaService, dimmerService, reminderServiceInstance, configManager);

                // Set the presenter on the MainForm instance
                mainFormInstance.SetPresenter(presenter);

                // Initialize the presenter
                presenter.Initialize();

                Log.Information("Running Odin main form...");
                Application.Run(mainFormInstance); // Run the manually created MainForm instance
            }
            catch (Exception ex)
            {
                 Log.Fatal(ex, "Application terminated unexpectedly during startup or runtime.");
                 MessageBox.Show($"A critical error occurred: {ex.Message}\n\nPlease check the logs for details.", "Runtime Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                 Log.Information("Odin Application Exiting. Disposing services...");

                 // Explicitly dispose services obtained from the provider or created manually
                 try
                 {
                     serviceProvider?.Dispose(); // Dispose the container and its disposables (if any were registered as scoped/transient)

                     // Manually dispose singletons if not handled by container disposal (safer to do both)
                     (serviceProvider?.GetService<GammaService>() as IDisposable)?.Dispose();
                     (serviceProvider?.GetService<DimmerService>() as IDisposable)?.Dispose();
                     // ReminderService was created manually, need to dispose it if it's IDisposable
                     // Assuming reminderServiceInstance is accessible here or made so
                     // (reminderServiceInstance as IDisposable)?.Dispose();
                 }
                 catch (Exception ex)
                 {
                     Log.Error(ex, "Error during service disposal.");
                 }

                 Log.Information("Service disposal complete.");
                 Log.CloseAndFlush(); // Flush logs after disposal attempts
                 mutex?.ReleaseMutex();
                 mutex?.Dispose();
            }
        }

        // Original ConfigureServices (excluding ReminderService registration initially)
        private static void ConfigureServices(IServiceCollection services)
        {
             Log.Debug("Configuring services...");
             services.AddSingleton<ConfigurationManager>();
             services.AddSingleton<GammaService>();
             services.AddSingleton<DimmerService>();
             // MainForm and MainPresenter are created manually in Main to resolve circular dependency
             // ReminderService is also created manually in Main
             Log.Debug("Service configuration complete.");
        }

        private static void Application_ThreadException(object sender, ThreadExceptionEventArgs e)
        {
             Log.Error(e.Exception, "Unhandled UI thread exception.");
             MessageBox.Show($"An unexpected error occurred: {e.Exception.Message}\n\nThe application may need to close. Please check the logs.", "Unhandled Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        private static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
             Log.Fatal(e.ExceptionObject as Exception, "Unhandled non-UI thread exception. IsTerminating: {IsTerminating}", e.IsTerminating);
             Log.CloseAndFlush();
        }
    }
}