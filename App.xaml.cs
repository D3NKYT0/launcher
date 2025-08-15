using System;
using System.Net.Http;
using System.Windows;
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
                var viewModel = _serviceProvider.GetRequiredService<MainViewModel>();
                var mainWindow = new Updater.MainWindow(viewModel);
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

        private void ConfigureServices(IServiceCollection services)
        {
            // Settings - Usando ConfigurationManager para carregar configurações
            var appSettings = ConfigurationManager.LoadSettings();
            services.AddSingleton(appSettings);

            // Logging
            services.AddLogging(builder =>
            {
                builder.AddSerilog(dispose: true);
            });

            // HTTP Client
            services.AddHttpClient();

            // Services
            services.AddSingleton<ISecurityService, SecurityService>();
            services.AddSingleton<IUpdateService, UpdateService>();

            // ViewModels
            services.AddTransient<MainViewModel>();

            // Views - Removed since we create MainWindow manually
        }
    }
}
