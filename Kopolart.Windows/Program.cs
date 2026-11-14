using Kopolart.Windows.UI.Helpers;
using Kopolart.Windows.UI.Forms;
using Kopolart.Windows.Application.Interfaces;
using Kopolart.Windows.Infrastructure.Settings;
using Kopolart.Windows.Infrastructure.Services;
using Kopolart.Windows.Infrastructure.Security;
using Kopolart.Windows.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Net.Http;

namespace Kopolart.Windows
{
    internal static class Program
    {
        /// <summary>Also used by the headless scheduled check to detect a running instance.</summary>
        internal const string AppMutexName = "Global\\KopolartWindowsApp_6b3c5c2d";

        public static IServiceProvider ServiceProvider { get; private set; } = null!;

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {
            // Headless scheduled run: check for new posts and exit without showing any window.
            if (args is { Length: > 0 } &&
                Array.Exists(args, a => a.Equals(HeadlessPostCheckRunner.CheckArgument, StringComparison.OrdinalIgnoreCase)))
            {
                HeadlessPostCheckRunner.Run();
                return;
            }

            using var mutex = new Mutex(initiallyOwned: true, name: AppMutexName, createdNew: out bool createdNew);

            if (!createdNew)
            {
                BringExistingInstanceToFront();
                return;
            }

            try
            {
                ConfigureApplication();
                RegisterGlobalExceptionHandlers();

                var host = Host.CreateDefaultBuilder()
                    .ConfigureServices((context, services) =>
                    {
                        ConfigureServices(services);
                    })
                    .Build();

                ServiceProvider = host.Services;

                var settingsManager = ServiceProvider.GetRequiredService<ISettingsManager>();
                DesktopShortcutManager.EnsureDesktopShortcut(settingsManager);
                TaskbarJumpListManager.InitializeJumpList();

                // Keep the app-closed post check registered while notifications are enabled.
                ServiceProvider.GetRequiredService<IBackgroundPostCheckScheduler>()
                    .Sync(settingsManager.GetNotifyNewContent());

                using var mainForm = ServiceProvider.GetRequiredService<Form1>();
                System.Windows.Forms.Application.Run(mainForm);
            }
            catch (Exception ex)
            {
                HandleException(ex, showMessageBox: true, isFatal: true);
            }
        }

        private static void ConfigureServices(IServiceCollection services)
        {
            // Register HTTP Client Factory
            services.AddHttpClient("DefaultClient", client => 
            {
                client.Timeout = TimeSpan.FromSeconds(30);
            })
            .ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler
            {
                PooledConnectionLifetime = TimeSpan.FromMinutes(2)
            });

            // Register infrastructure settings
            services.AddSingleton<ISettingsManager, AppSettingsManager>();

            // Register services
            services.AddSingleton<IWebViewService, WebViewService>();
            services.AddSingleton<IAssetService, AssetService>();
            services.AddSingleton<IUpdateService, UpdateService>();
            services.AddSingleton<IDownloadService, DownloadService>();
            services.AddSingleton<IUpdatePackageVerifier, UpdatePackageVerifier>();
            services.AddSingleton<IDataBackupService, DataBackupService>();
            services.AddSingleton<IUserIpLogger, UserIpLogger>();
            services.AddSingleton<IAppVisitTrackerService, AppVisitTrackerService>();
            services.AddSingleton<IOnlineUsersService, OnlineUsersService>();
            services.AddSingleton<IRssFeedService, RssFeedService>();
            services.AddSingleton<IBackgroundPostCheckScheduler, WindowsBackgroundPostCheckService>();
            services.AddSingleton<ISearchSuggestionService, SearchSuggestionService>();
            services.AddSingleton<IWallpaperService, WindowsWallpaperService>();

            // Register forms/views
            services.AddTransient<Form1>();
        }

        private static void ConfigureApplication()
        {
            TaskbarJumpListManager.EnsureAppUserModelId();
            System.Windows.Forms.Application.SetHighDpiMode(HighDpiMode.PerMonitorV2);
            System.Windows.Forms.Application.EnableVisualStyles();
            System.Windows.Forms.Application.SetCompatibleTextRenderingDefault(false);
        }

        private static void RegisterGlobalExceptionHandlers()
        {
            System.Windows.Forms.Application.ThreadException += (sender, e) =>
                HandleException(e.Exception, showMessageBox: false, isFatal: false);

            AppDomain.CurrentDomain.UnhandledException += (sender, e) =>
                HandleException(e.ExceptionObject as Exception, showMessageBox: false, isFatal: true);

            TaskScheduler.UnobservedTaskException += (sender, e) =>
            {
                HandleException(e.Exception, showMessageBox: false, isFatal: false);
                e.SetObserved();
            };
        }

        private static void BringExistingInstanceToFront()
        {
            try
            {
                var args = Environment.GetCommandLineArgs();
                IntPtr actionParam = IntPtr.Zero;
                if (args is { Length: > 1 })
                {
                    if (Array.Exists(args, a => a.Equals("--dashboard", StringComparison.OrdinalIgnoreCase)))
                        actionParam = (IntPtr)1;
                    else if (Array.Exists(args, a => a.Equals("--downloads", StringComparison.OrdinalIgnoreCase)))
                        actionParam = (IntPtr)2;
                    else if (Array.Exists(args, a => a.Equals("--last-page", StringComparison.OrdinalIgnoreCase)))
                        actionParam = (IntPtr)3;
                }

                // Broadcast registered restore message with actionParam so the existing instance handles the JumpList action
                NativeMethods.PostMessage(NativeMethods.HWND_BROADCAST, NativeMethods.WM_SHOW_KOPOLART, actionParam, IntPtr.Zero);

                var currentProcess = Process.GetCurrentProcess();

                foreach (var process in Process.GetProcessesByName(currentProcess.ProcessName))
                {
                    if (process.Id == currentProcess.Id)
                        continue;

                    var hWnd = process.MainWindowHandle;
                    if (hWnd == IntPtr.Zero)
                        continue;

                    NativeMethods.ShowWindow(hWnd, NativeMethods.IsIconic(hWnd) ? NativeMethods.SW_RESTORE : NativeMethods.SW_SHOW);

                    NativeMethods.BringWindowToTop(hWnd);
                    NativeMethods.SetForegroundWindow(hWnd);
                    break;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to bring existing instance to front: {ex}");
            }
        }

        private static void HandleException(Exception? ex, bool showMessageBox = false, bool isFatal = false)
        {
            if (ex == null)
                return;

            try
            {
                Debug.WriteLine($"{(isFatal ? "Fatal exception" : "Unhandled exception")}: {ex}");
                WriteCrashLog(ex, isFatal);

                // Only show a message box if explicitly asked AND if it's safe (e.g. from main try/catch)
                if (showMessageBox)
                {
                    bool isDark = false;
                    try
                    {
                        if (ServiceProvider?.GetService(typeof(ISettingsManager)) is ISettingsManager settings)
                        {
                            isDark = settings.GetIsDarkMode();
                        }
                    }
                    catch
                    {
                        // Startup failures may occur before settings are readable.
                    }

                    CustomMessageBox.Show(
                        null,
                        "خطایی غیرمنتظره رخ داد و برنامه باید بسته شود.\n\nلطفاً مجدداً برنامه را اجرا کنید.",
                        "کپل‌آرت",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error,
                        isDark);
                }
            }
            catch
            {
                // Never throw from exception handler
            }
        }

        private static void WriteCrashLog(Exception ex, bool isFatal)
        {
            try
            {
                var logDirectory = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "KopolArt");

                Directory.CreateDirectory(logDirectory);

                var logPath = Path.Combine(logDirectory, "exceptions.log");
                var entry = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {(isFatal ? "FATAL" : "ERROR")}{Environment.NewLine}{ex}{Environment.NewLine}{Environment.NewLine}";
                File.AppendAllText(logPath, entry);
            }
            catch
            {
                // Logging must never break the exception handler.
            }
        }
    }
}