using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using Updater.DataContractModels;
using Updater.Localization;
using Updater.Models;
using Updater.Services;
using System.Windows;

namespace Updater.ViewModels
{
    public partial class MainViewModel : ObservableObject
    {
        private readonly ILogger<MainViewModel> _logger;
        private readonly IUpdateService _updateService;
        private readonly ISecurityService _securityService;
        private readonly AppSettings _settings;
        private CancellationTokenSource? _cancellationTokenSource;

        [ObservableProperty]
        private LocString _info = new("Готов к работе", "Ready to work");

        [ObservableProperty]
        private LocString _downloadInfo = new("", "");

        [ObservableProperty]
        private double _progressFull;

        [ObservableProperty]
        private double _maxProgressFull = 100;

        [ObservableProperty]
        private double _progressFile;

        [ObservableProperty]
        private double _maxProgressFile = 100;

        [ObservableProperty]
        private string _downloadSpeed = "";

        [ObservableProperty]
        private bool _isBusy;

        [ObservableProperty]
        private string _savePath = AppDomain.CurrentDomain.BaseDirectory;

        [ObservableProperty]
        private bool _hasError;

        [ObservableProperty]
        private string _currentFileName = "";

        [ObservableProperty]
        private long _totalBytes;

        [ObservableProperty]
        private long _downloadedBytes;

        [ObservableProperty]
        private int _currentFile;

        [ObservableProperty]
        private int _totalFiles;

        [ObservableProperty]
        private bool _isSearching;

        public MainViewModel(
            ILogger<MainViewModel> logger,
            IUpdateService updateService,
            ISecurityService securityService,
            AppSettings settings)
        {
            _logger = logger;
            _updateService = updateService;
            _securityService = securityService;
            _settings = settings;
        }

        [RelayCommand]
        private async Task StartQuickUpdateAsync()
        {
            _logger.LogInformation("StartQuickUpdateAsync command executed");
            await StartUpdateAsync(UpdateTypes.Quick);
        }

        [RelayCommand]
        private async Task StartFullUpdateAsync()
        {
            _logger.LogInformation("StartFullUpdateAsync command executed");
            await StartUpdateAsync(UpdateTypes.Full);
        }

        [RelayCommand]
        private async Task StartGameAsync()
        {
            _logger.LogInformation("StartGameAsync command executed");
            try
            {
                _logger.LogInformation("Starting game");
                var gamePath = GetGamePath();
                var executablePath = Path.Combine(gamePath, _settings.GameSettings.ExecutableName);

                if (!File.Exists(executablePath))
                {
                    _logger.LogError("Game executable not found: {ExecutablePath}", executablePath);
                    Info = new LocString("Игра не найдена", "Game not found");
                    return;
                }

                // Validate executable
                var fileInfo = new FileInfo(executablePath);
                if (fileInfo.Length != _settings.GameSettings.ExpectedExecutableSize)
                {
                    _logger.LogWarning("Executable size mismatch. Expected: {Expected}, Actual: {Actual}", 
                        _settings.GameSettings.ExpectedExecutableSize, fileInfo.Length);
                    
                    // Try to run l2.exe directly
                    await StartProcessAsync(executablePath, gamePath);
                }
                else
                {
                    // Run l2.bin with elevated privileges
                    var binaryPath = Path.Combine(gamePath, _settings.GameSettings.BinaryName);
                    if (File.Exists(binaryPath))
                    {
                        await StartProcessWithElevationAsync(binaryPath, _settings.GameSettings.WorkingDirectory);
                    }
                    else
                    {
                        _logger.LogError("Game binary not found: {BinaryPath}", binaryPath);
                        Info = new LocString("Бинарный файл игры не найден", "Game binary not found");
                        return;
                    }
                }

                _logger.LogInformation("Game started successfully");
                Environment.Exit(0);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error starting game");
                Info = new LocString("Ошибка запуска игры", "Error starting game");
            }
        }

        [RelayCommand]
        private void CancelUpdate()
        {
            _logger.LogInformation("CancelUpdate command executed");
            _cancellationTokenSource?.Cancel();
        }

        [RelayCommand]
        private void Exit()
        {
            _logger.LogInformation("Exit command executed");
            Application.Current.Shutdown();
        }

        [RelayCommand]
        private void Tray()
        {
            _logger.LogInformation("Tray command executed");
            // Implement tray functionality here
            // For now, just minimize the window
            if (Application.Current.MainWindow != null)
            {
                Application.Current.MainWindow.WindowState = WindowState.Minimized;
            }
        }



        [RelayCommand]
        private void OpenWebsite()
        {
            OpenUrl(_settings.Links.Site);
        }

        [RelayCommand]
        private void OpenForum()
        {
            OpenUrl(_settings.Links.Forum);
        }

        [RelayCommand]
        private void OpenDiscord()
        {
            OpenUrl(_settings.Links.Discord);
        }

        [RelayCommand]
        private void OpenDonation()
        {
            OpenUrl(_settings.Links.Donation);
        }

        private async Task StartUpdateAsync(UpdateTypes updateType)
        {
            _logger.LogInformation("StartUpdateAsync called with type: {UpdateType}", updateType);
            
            if (IsBusy)
            {
                _logger.LogWarning("Update already in progress");
                return;
            }

            try
            {
                IsBusy = true;
                HasError = false;
                _cancellationTokenSource = new CancellationTokenSource();
                var cancellationToken = _cancellationTokenSource.Token;

                _logger.LogInformation("Starting {UpdateType} update", updateType);

                // Check for self-update first
                _logger.LogInformation("Checking for self-update...");
                if (await _updateService.CheckForSelfUpdateAsync(_settings.UpdateSettings.UpdateUrl, cancellationToken))
                {
                    Info = new LocString("Доступна новая версия апдейтера", "New version of updater is available");
                    if (await _updateService.PerformSelfUpdateAsync(_settings.UpdateSettings.UpdateUrl, cancellationToken))
                    {
                        return;
                    }
                }

                // Get update info
                _logger.LogInformation("Getting update info...");
                Info = new LocString("Получение информации о обновлении", "Getting update information");
                var updateInfo = await _updateService.GetUpdateInfoAsync(_settings.UpdateSettings.UpdateUrl, cancellationToken);

                // Validate update
                _logger.LogInformation("Validating update...");
                if (!await _updateService.ValidateUpdateAsync(updateInfo, cancellationToken))
                {
                    throw new InvalidOperationException("Update validation failed");
                }

                // Get files to update
                _logger.LogInformation("Getting files to update...");
                Info = new LocString("Проверка файлов для обновления", "Checking files for update");
                var filesToUpdate = await _updateService.GetFilesToUpdateAsync(updateInfo, updateType, SavePath, cancellationToken);

                _logger.LogInformation("Found {FileCount} files to update", filesToUpdate.Count());

                if (!filesToUpdate.Any())
                {
                    Info = new LocString("Обновление не требуется", "No update required");
                    return;
                }

                // Create progress reporter
                var progress = new Progress<UpdateProgress>(OnUpdateProgress);

                // Download and update files
                _logger.LogInformation("Starting download and update...");
                Info = new LocString("Загрузка файлов", "Downloading files");
                var success = await _updateService.DownloadAndUpdateFilesAsync(
                    filesToUpdate, 
                    _settings.UpdateSettings.UpdateUrl, 
                    SavePath, 
                    progress, 
                    cancellationToken);

                if (success)
                {
                    Info = new LocString("Обновление успешно завершено", "Update completed successfully");
                    _logger.LogInformation("Update completed successfully");
                }
                else
                {
                    HasError = true;
                    Info = new LocString("Ошибка во время обновления", "Error during update");
                    _logger.LogError("Update failed");
                }
            }
            catch (OperationCanceledException)
            {
                Info = new LocString("Обновление отменено пользователем", "Update cancelled by user");
                _logger.LogInformation("Update cancelled by user");
            }
            catch (InvalidOperationException ex) when (ex.Message.Contains("Network error") || ex.Message.Contains("Request timeout"))
            {
                HasError = true;
                Info = new LocString("Ошибка сети. Проверьте подключение к интернету.", "Network error. Please check your internet connection.");
                _logger.LogError(ex, "Network error during update");
            }
            catch (Exception ex)
            {
                HasError = true;
                Info = new LocString("Ошибка обновления", "Update error");
                _logger.LogError(ex, "Error during update");
            }
            finally
            {
                IsBusy = false;
                _cancellationTokenSource?.Dispose();
                _cancellationTokenSource = null;
            }
        }

        private void OnUpdateProgress(UpdateProgress progress)
        {
            CurrentFile = progress.CurrentFile;
            TotalFiles = progress.TotalFiles;
            DownloadedBytes = progress.BytesDownloaded;
            TotalBytes = progress.TotalBytes;
            CurrentFileName = progress.CurrentFileName;
            ProgressFull = progress.ProgressPercentage;
            ProgressFile = progress.DownloadProgressPercentage;
            DownloadInfo = new LocString(progress.Status, progress.Status);
        }

        private string GetGamePath()
        {
            return Path.Combine(SavePath, _settings.UpdateSettings.GameStartPath);
        }

        private async Task StartProcessAsync(string executablePath, string workingDirectory)
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = executablePath,
                WorkingDirectory = workingDirectory,
                UseShellExecute = true
            };

            var process = Process.Start(startInfo);
            if (process == null)
            {
                throw new InvalidOperationException($"Failed to start process: {executablePath}");
            }

            await Task.Delay(1000); // Give process time to start
        }

        private async Task StartProcessWithElevationAsync(string binaryPath, string workingDirectory)
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = binaryPath,
                WorkingDirectory = workingDirectory,
                Verb = "runas",
                UseShellExecute = false
            };

            var process = Process.Start(startInfo);
            if (process == null)
            {
                throw new InvalidOperationException($"Failed to start elevated process: {binaryPath}");
            }

            await Task.Delay(1000); // Give process time to start
        }

        private void OpenUrl(string url)
        {
            try
            {
                var startInfo = new ProcessStartInfo
                {
                    FileName = url,
                    UseShellExecute = true
                };

                Process.Start(startInfo);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error opening URL: {Url}", url);
            }
        }
    }
}
