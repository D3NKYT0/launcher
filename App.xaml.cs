using System;
using System.IO;
using System.Windows;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
using Updater.Models;
using Updater.Services;
using Updater.ViewModels;

namespace L2Updater
{
    public partial class App : Application
    {
        private ServiceProvider? _serviceProvider;

        protected override void OnStartup(StartupEventArgs e)
        {
            try
            {
                // Configure Serilog
                Log.Logger = new LoggerConfiguration()
                    .WriteTo.Console()
                    .WriteTo.File("logs/l2updater.log", rollingInterval: RollingInterval.Day, retainedFileCountLimit: 7)
                    .CreateLogger();

                // Configure services
                var services = new ServiceCollection();
                ConfigureServices(services);

                _serviceProvider = services.BuildServiceProvider();

                // Create and show main window
                var mainWindow = _serviceProvider.GetRequiredService<Updater.MainWindow>();
                mainWindow.Show();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error starting application: {ex.Message}", "Startup Error", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
                Shutdown(1);
            }
        }

        protected override void OnExit(ExitEventArgs e)
        {
            try
            {
                Log.CloseAndFlush();
                _serviceProvider?.Dispose();
            }
            catch (Exception ex)
            {
                // Log error but don't throw during shutdown
                Console.WriteLine($"Error during shutdown: {ex.Message}");
            }
        }

        private IConfiguration GetConfiguration()
        {
            return new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production"}.json", optional: true)
                .Build();
        }

        private void ConfigureServices(IServiceCollection services)
        {
            // Configuration
            var configuration = GetConfiguration();
            services.AddSingleton<IConfiguration>(configuration);

            // Settings
            var appSettings = new AppSettings();
            configuration.Bind(appSettings);
            services.AddSingleton(appSettings);

            // Logging
            services.AddLogging(builder =>
            {
                builder.AddSerilog(dispose: true);
            });

            // HTTP Client
            services.AddHttpClient("UpdateClient", client =>
            {
                client.Timeout = TimeSpan.FromSeconds(appSettings.UpdateSettings.DownloadTimeoutSeconds);
                client.DefaultRequestHeaders.Add("User-Agent", "L2Updater/1.0");
            });

            // Services
            services.AddSingleton<ISecurityService, SecurityService>();
            services.AddSingleton<IUpdateService, UpdateService>();

            // ViewModels
            services.AddTransient<MainViewModel>();

            // Views
            services.AddTransient<Updater.MainWindow>();
        }
    }
}
