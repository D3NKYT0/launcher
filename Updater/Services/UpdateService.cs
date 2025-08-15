using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Updater.DataContractModels;
using Updater.Models;
using Updater.UtillsClasses;

namespace Updater.Services
{
    public class UpdateService : IUpdateService
    {
        private readonly ILogger<UpdateService> _logger;
        private readonly HttpClient _httpClient;
        private readonly ISecurityService _securityService;
        private readonly AppSettings _settings;

        public UpdateService(
            ILogger<UpdateService> logger,
            HttpClient httpClient,
            ISecurityService securityService,
            AppSettings settings)
        {
            _logger = logger;
            _httpClient = httpClient;
            _securityService = securityService;
            _settings = settings;
        }

        public async Task<UpdateInfoModel> GetUpdateInfoAsync(string updateUrl, CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("Fetching update info from {UpdateUrl}", updateUrl);
                
                if (!_securityService.ValidateCertificate(updateUrl))
                {
                    throw new InvalidOperationException($"Invalid certificate for URL: {updateUrl}");
                }

                var url = Path.Combine(updateUrl, "UpdateInfo.xml");
                var response = await _httpClient.GetStringAsync(url, cancellationToken);
                
                var updateInfo = JsonSerializer.Deserialize<UpdateInfoModel>(response, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (updateInfo == null)
                {
                    throw new InvalidOperationException("Failed to deserialize update info");
                }

                _logger.LogInformation("Successfully fetched update info with {FileCount} files", 
                    GetTotalFileCount(updateInfo.Folder));

                return updateInfo;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching update info from {UpdateUrl}", updateUrl);
                throw;
            }
        }

        public async Task<UpdateConfig> GetUpdateConfigAsync(string updateUrl, CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("Fetching update config from {UpdateUrl}", updateUrl);
                
                if (!_securityService.ValidateCertificate(updateUrl))
                {
                    throw new InvalidOperationException($"Invalid certificate for URL: {updateUrl}");
                }

                var url = Path.Combine(updateUrl, "UpdateConfig.xml");
                var response = await _httpClient.GetStringAsync(url, cancellationToken);
                
                var updateConfig = JsonSerializer.Deserialize<UpdateConfig>(response, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (updateConfig == null)
                {
                    throw new InvalidOperationException("Failed to deserialize update config");
                }

                _logger.LogInformation("Successfully fetched update config");
                return updateConfig;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching update config from {UpdateUrl}", updateUrl);
                throw;
            }
        }

        public async Task<bool> CheckForSelfUpdateAsync(string updateUrl, CancellationToken cancellationToken = default)
        {
            try
            {
                var url = Path.Combine(updateUrl, "updaterver.txt");
                var response = await _httpClient.GetStringAsync(url, cancellationToken);
                
                if (int.TryParse(response.Trim(), out var remoteVersion))
                {
                    var hasUpdate = remoteVersion > _settings.UpdateSettings.UpdaterVersion;
                    _logger.LogInformation("Self-update check: Current={CurrentVersion}, Remote={RemoteVersion}, HasUpdate={HasUpdate}", 
                        _settings.UpdateSettings.UpdaterVersion, remoteVersion, hasUpdate);
                    return hasUpdate;
                }

                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking for self-update from {UpdateUrl}", updateUrl);
                return false;
            }
        }

        public async Task<bool> PerformSelfUpdateAsync(string updateUrl, CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("Performing self-update from {UpdateUrl}", updateUrl);
                
                var updaterFileName = "upd.exe";
                var tempPath = Path.Combine(Path.GetTempPath(), updaterFileName);
                
                // Download new updater
                var downloadUrl = Path.Combine(updateUrl, updaterFileName);
                using var response = await _httpClient.GetAsync(downloadUrl, cancellationToken);
                response.EnsureSuccessStatusCode();
                
                using var fileStream = File.Create(tempPath);
                await response.Content.CopyToAsync(fileStream, cancellationToken);
                
                // Validate downloaded file
                if (!await _securityService.ValidateFileSignatureAsync(tempPath))
                {
                    _logger.LogError("Downloaded updater failed signature validation");
                    File.Delete(tempPath);
                    return false;
                }

                // Start new updater process
                var currentExePath = Environment.ProcessPath ?? throw new InvalidOperationException("Cannot determine current executable path");
                var startInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = tempPath,
                    Arguments = $"\"{currentExePath}\"",
                    UseShellExecute = true
                };

                System.Diagnostics.Process.Start(startInfo);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error performing self-update from {UpdateUrl}", updateUrl);
                return false;
            }
        }

        public async Task<List<FileModel>> GetFilesToUpdateAsync(UpdateInfoModel updateInfo, UpdateTypes updateType, string rootPath, CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("Getting files to update for type {UpdateType}", updateType);
                
                var allFiles = UpdateUtills.GetAllFileInfos(updateInfo.Folder, updateType, updateInfo.Folder.Name);
                var filesToUpdate = new List<FileModel>();

                foreach (var file in allFiles)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    if (!_securityService.IsFileExtensionAllowed(file.Name))
                    {
                        _logger.LogWarning("Skipping file with blocked extension: {FileName}", file.Name);
                        continue;
                    }

                    if (_securityService.IsFileExtensionBlocked(file.Name))
                    {
                        _logger.LogWarning("Skipping file with blocked extension: {FileName}", file.Name);
                        continue;
                    }

                    var filePath = Path.Combine(rootPath, file.SavePath, file.Name);
                    if (!UpdateUtills.CheckFile(rootPath, file, updateType))
                    {
                        filesToUpdate.Add(file);
                    }
                }

                _logger.LogInformation("Found {FileCount} files to update", filesToUpdate.Count);
                return filesToUpdate;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting files to update");
                throw;
            }
        }

        public async Task<bool> DownloadAndUpdateFilesAsync(IEnumerable<FileModel> files, string updateUrl, string savePath, IProgress<UpdateProgress> progress, CancellationToken cancellationToken = default)
        {
            try
            {
                var fileList = files.ToList();
                var totalFiles = fileList.Count;
                var currentFile = 0;
                var totalBytes = fileList.Sum(f => f.Size);
                var downloadedBytes = 0L;

                _logger.LogInformation("Starting download of {FileCount} files ({TotalBytes} bytes)", totalFiles, totalBytes);

                // Create semaphore for concurrent downloads
                using var semaphore = new SemaphoreSlim(_settings.UpdateSettings.MaxConcurrentDownloads);
                var tasks = new List<Task>();

                foreach (var file in fileList)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var task = Task.Run(async () =>
                    {
                        await semaphore.WaitAsync(cancellationToken);
                        try
                        {
                            await DownloadFileWithRetryAsync(file, updateUrl, savePath, cancellationToken);
                            
                            Interlocked.Increment(ref currentFile);
                            Interlocked.Add(ref downloadedBytes, file.Size);
                            
                            progress?.Report(new UpdateProgress
                            {
                                CurrentFile = currentFile,
                                TotalFiles = totalFiles,
                                BytesDownloaded = downloadedBytes,
                                TotalBytes = totalBytes,
                                CurrentFileName = file.Name,
                                Status = $"Downloaded {file.Name}"
                            });
                        }
                        finally
                        {
                            semaphore.Release();
                        }
                    }, cancellationToken);

                    tasks.Add(task);
                }

                await Task.WhenAll(tasks);
                
                _logger.LogInformation("Successfully downloaded all {FileCount} files", totalFiles);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error downloading and updating files");
                return false;
            }
        }

        public async Task<bool> ValidateUpdateAsync(UpdateInfoModel updateInfo, CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("Validating update info");
                
                if (updateInfo?.Folder == null)
                {
                    _logger.LogError("Update info or folder is null");
                    return false;
                }

                // Validate critical files
                var criticalFiles = GetCriticalFiles(updateInfo.Folder);
                foreach (var file in criticalFiles)
                {
                    if (!_securityService.IsFileExtensionAllowed(file.Name))
                    {
                        _logger.LogError("Critical file has blocked extension: {FileName}", file.Name);
                        return false;
                    }
                }

                _logger.LogInformation("Update validation successful");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating update");
                return false;
            }
        }

        private async Task DownloadFileWithRetryAsync(FileModel file, string updateUrl, string savePath, CancellationToken cancellationToken)
        {
            var maxRetries = _settings.UpdateSettings.MaxRetryAttempts;
            var retryDelay = TimeSpan.FromSeconds(_settings.UpdateSettings.RetryDelaySeconds);

            for (int attempt = 1; attempt <= maxRetries; attempt++)
            {
                try
                {
                    await DownloadSingleFileAsync(file, updateUrl, savePath, cancellationToken);
                    return;
                }
                catch (Exception ex) when (attempt < maxRetries)
                {
                    _logger.LogWarning(ex, "Download attempt {Attempt} failed for {FileName}, retrying in {Delay}ms", 
                        attempt, file.Name, retryDelay.TotalMilliseconds);
                    await Task.Delay(retryDelay, cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "All download attempts failed for {FileName}", file.Name);
                    throw;
                }
            }
        }

        private async Task DownloadSingleFileAsync(FileModel file, string updateUrl, string savePath, CancellationToken cancellationToken)
        {
            var downloadUrl = Path.Combine(updateUrl, file.Path);
            var localPath = Path.Combine(savePath, file.SavePath, file.Name);
            
            // Ensure directory exists
            var directory = Path.GetDirectoryName(localPath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            using var response = await _httpClient.GetAsync(downloadUrl, cancellationToken);
            response.EnsureSuccessStatusCode();

            using var fileStream = File.Create(localPath);
            await response.Content.CopyToAsync(fileStream, cancellationToken);

            // Validate file integrity if hash is provided
            if (!string.IsNullOrEmpty(file.Hash))
            {
                if (!await _securityService.ValidateFileIntegrityAsync(localPath, file.Hash))
                {
                    File.Delete(localPath);
                    throw new InvalidOperationException($"File integrity check failed for {file.Name}");
                }
            }

            _logger.LogDebug("Successfully downloaded {FileName} to {LocalPath}", file.Name, localPath);
        }

        private int GetTotalFileCount(FolderModel folder)
        {
            var count = folder.Files?.Count ?? 0;
            foreach (var subFolder in folder.Folders ?? Enumerable.Empty<FolderModel>())
            {
                count += GetTotalFileCount(subFolder);
            }
            return count;
        }

        private List<FileModel> GetCriticalFiles(FolderModel folder)
        {
            var criticalFiles = new List<FileModel>();
            
            if (folder.Files != null)
            {
                criticalFiles.AddRange(folder.Files.Where(f => 
                    f.Name.Equals("l2.exe", StringComparison.OrdinalIgnoreCase) ||
                    f.Name.Equals("l2.bin", StringComparison.OrdinalIgnoreCase)));
            }

            foreach (var subFolder in folder.Folders ?? Enumerable.Empty<FolderModel>())
            {
                criticalFiles.AddRange(GetCriticalFiles(subFolder));
            }

            return criticalFiles;
        }
    }
}
