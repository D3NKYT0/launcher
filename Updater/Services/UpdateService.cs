using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
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
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ISecurityService _securityService;
        private readonly AppSettings _settings;

        public UpdateService(
            ILogger<UpdateService> logger,
            IHttpClientFactory httpClientFactory,
            ISecurityService securityService,
            AppSettings settings)
        {
            _logger = logger;
            _httpClientFactory = httpClientFactory;
            _securityService = securityService;
            _settings = settings;
        }

        private string CombineUrl(string baseUrl, string path)
        {
            try
            {
                // Remove trailing slash from base URL and leading slash from path
                baseUrl = baseUrl.TrimEnd('/');
                path = path.TrimStart('/');
                var combinedUrl = $"{baseUrl}/{path}";
                
                _logger.LogDebug("Combined URL: {BaseUrl} + {Path} = {CombinedUrl}", baseUrl, path, combinedUrl);
                
                // Validate the combined URL
                if (!Uri.TryCreate(combinedUrl, UriKind.Absolute, out var uri))
                {
                    throw new InvalidOperationException($"Invalid URL created: {combinedUrl}");
                }
                
                return combinedUrl;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error combining URL: {BaseUrl} + {Path}", baseUrl, path);
                throw;
            }
        }

        private HttpClient CreateHttpClient()
        {
            var client = _httpClientFactory.CreateClient("UpdateClient");
            
            // Adicionar headers únicos para cada requisição para evitar cache
            client.DefaultRequestHeaders.Remove("X-Request-ID");
            client.DefaultRequestHeaders.Add("X-Request-ID", Guid.NewGuid().ToString());
            client.DefaultRequestHeaders.Remove("Cache-Control");
            client.DefaultRequestHeaders.Add("Cache-Control", "no-cache, no-store, must-revalidate");
            client.DefaultRequestHeaders.Remove("Pragma");
            client.DefaultRequestHeaders.Add("Pragma", "no-cache");
            
            return client;
        }

        private async Task<bool> ValidateDownloadedFileAsync(string filePath, string expectedHash, string fileName)
        {
            try
            {
                if (string.IsNullOrEmpty(expectedHash))
                {
                    _logger.LogDebug("No hash provided for {FileName}, skipping validation", fileName);
                    return true;
                }

                var actualHash = await _securityService.CalculateFileHashAsync(filePath);
                var isValid = string.Equals(actualHash, expectedHash, StringComparison.OrdinalIgnoreCase);

                if (!isValid)
                {
                    _logger.LogWarning("File integrity check failed for {FileName}. Expected: {ExpectedHash}, Actual: {ActualHash}", 
                        fileName, expectedHash, actualHash);
                }
                else
                {
                    _logger.LogDebug("File integrity check passed for {FileName}", fileName);
                }

                return isValid;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating file integrity for {FileName}", fileName);
                return false;
            }
        }


        public async Task<UpdateInfoModel> GetUpdateInfoAsync(string updateUrl, CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("Fetching update info from {UpdateUrl}", updateUrl);
                
                // Log the HttpClient configuration
                using var httpClient = CreateHttpClient();
                _logger.LogDebug("HttpClient timeout: {Timeout}", httpClient.Timeout);
                _logger.LogDebug("HttpClient base address: {BaseAddress}", httpClient.BaseAddress);
                
                if (!_securityService.ValidateCertificate(updateUrl))
                {
                    throw new InvalidOperationException($"Invalid certificate for URL: {updateUrl}");
                }

                var url = CombineUrl(updateUrl, "UpdateInfo.xml");
                _logger.LogInformation("Requesting URL: {Url}", url);
                
                _logger.LogDebug("Certificate validation disabled for {Url}", url);
                
                // Test URL parsing
                if (!Uri.TryCreate(url, UriKind.Absolute, out var testUri))
                {
                    throw new InvalidOperationException($"Failed to parse URL: {url}");
                }
                
                _logger.LogDebug("URL parsed successfully: {Scheme}://{Host}:{Port}{PathAndQuery}", 
                    testUri.Scheme, testUri.Host, testUri.Port, testUri.PathAndQuery);
                
                var response = await httpClient.GetStringAsync(url, cancellationToken);
                
                // Use XmlSerializer for XML parsing
                var serializer = new System.Xml.Serialization.XmlSerializer(typeof(UpdateInfoModel));
                using var reader = new StringReader(response);
                var updateInfo = (UpdateInfoModel?)serializer.Deserialize(reader);

                if (updateInfo == null)
                {
                    throw new InvalidOperationException("Failed to deserialize update info");
                }

                _logger.LogInformation("Successfully fetched update info with {FileCount} files", 
                    GetTotalFileCount(updateInfo.Folder));
                
                return updateInfo;
            }
            catch (TaskCanceledException ex)
            {
                _logger.LogError(ex, "Request timeout fetching update info from {UpdateUrl}", updateUrl);
                throw new InvalidOperationException($"Request timeout. Please check your internet connection and try again.", ex);
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
                
                var url = CombineUrl(updateUrl, "UpdateConfig.xml");
                using var httpClient = CreateHttpClient();
                var response = await httpClient.GetStringAsync(url, cancellationToken);
                
                // Use XmlSerializer for XML parsing
                var serializer = new System.Xml.Serialization.XmlSerializer(typeof(UpdateConfig));
                using var reader = new StringReader(response);
                var config = (UpdateConfig?)serializer.Deserialize(reader);

                if (config == null)
                {
                    throw new InvalidOperationException("Failed to deserialize update config");
                }

                _logger.LogInformation("Successfully fetched update config");
                return config;
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
                _logger.LogInformation("Checking for self-update from {UpdateUrl}", updateUrl);
                
                var url = CombineUrl(updateUrl, "updaterver.txt");
                using var httpClient = CreateHttpClient();
                var response = await httpClient.GetStringAsync(url, cancellationToken);
                
                if (int.TryParse(response.Trim(), out var remoteVersion))
                {
                    var currentVersion = _settings.UpdateSettings.UpdaterVersion;
                    
                    if (remoteVersion > currentVersion)
                    {
                        _logger.LogInformation("Self-update available: current={CurrentVersion}, remote={RemoteVersion}", 
                            currentVersion, remoteVersion);
                        return true;
                    }
                    else
                    {
                        _logger.LogDebug("No self-update needed: current={CurrentVersion}, remote={RemoteVersion}", 
                            currentVersion, remoteVersion);
                        return false;
                    }
                }
                else
                {
                    _logger.LogWarning("Invalid version format in response: {Response}", response);
                    return false;
                }
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
                var downloadUrl = CombineUrl(updateUrl, updaterFileName);
                using var httpClient = CreateHttpClient();
                using var response = await httpClient.GetAsync(downloadUrl, cancellationToken);
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

        public Task<List<FileModel>> GetFilesToUpdateAsync(UpdateInfoModel updateInfo, UpdateTypes updateType, string rootPath, CancellationToken cancellationToken = default)
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
                return Task.FromResult(filesToUpdate);
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

                // Report initial progress
                progress?.Report(new UpdateProgress
                {
                    CurrentFile = 0,
                    TotalFiles = totalFiles,
                    BytesDownloaded = 0,
                    TotalBytes = totalBytes,
                    CurrentFileName = "Starting download...",
                    Status = "Initializing download..."
                });

                // Process files sequentially for better progress reporting
                foreach (var file in fileList)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    try
                    {
                        // Report current file being processed
                        progress?.Report(new UpdateProgress
                        {
                            CurrentFile = currentFile,
                            TotalFiles = totalFiles,
                            BytesDownloaded = downloadedBytes,
                            TotalBytes = totalBytes,
                            CurrentFileName = file.Name,
                            Status = $"Downloading {file.Name}..."
                        });

                        await DownloadFileWithRetryAsync(file, updateUrl, savePath, cancellationToken, progress, currentFile, totalFiles, downloadedBytes, totalBytes);
                        
                        currentFile++;
                        downloadedBytes += file.Size;
                        
                        // Report completion of current file
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
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to download file {FileName}", file.Name);
                        
                        // Report error but continue with next file
                        progress?.Report(new UpdateProgress
                        {
                            CurrentFile = currentFile,
                            TotalFiles = totalFiles,
                            BytesDownloaded = downloadedBytes,
                            TotalBytes = totalBytes,
                            CurrentFileName = file.Name,
                            Status = $"Error downloading {file.Name}: {ex.Message}"
                        });
                    }
                }
                
                _logger.LogInformation("Successfully downloaded {CurrentFile} of {TotalFiles} files", currentFile, totalFiles);
                return currentFile == totalFiles;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error downloading and updating files");
                return false;
            }
        }

        public Task<bool> ValidateUpdateAsync(UpdateInfoModel updateInfo, CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("Validating update info");
                
                if (updateInfo?.Folder == null)
                {
                    _logger.LogError("Update info or folder is null");
                    return Task.FromResult(false);
                }

                // Validate critical files
                var criticalFiles = GetCriticalFiles(updateInfo.Folder);
                foreach (var file in criticalFiles)
                {
                    if (!_securityService.IsFileExtensionAllowed(file.Name))
                    {
                        _logger.LogError("Critical file has blocked extension: {FileName}", file.Name);
                        return Task.FromResult(false);
                    }
                }

                _logger.LogInformation("Update validation successful");
                return Task.FromResult(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating update");
                return Task.FromResult(false);
            }
        }

        private async Task DownloadFileWithRetryAsync(FileModel file, string updateUrl, string savePath, CancellationToken cancellationToken, IProgress<UpdateProgress>? progress = null, int currentFile = 0, int totalFiles = 0, long downloadedBytes = 0, long totalBytes = 0)
        {
            var maxRetries = _settings.UpdateSettings.MaxRetryAttempts;
            var retryDelay = TimeSpan.FromSeconds(_settings.UpdateSettings.RetryDelaySeconds);

            for (int attempt = 1; attempt <= maxRetries; attempt++)
            {
                try
                {
                    await DownloadSingleFileAsync(file, updateUrl, savePath, cancellationToken, progress, currentFile, totalFiles, downloadedBytes, totalBytes);
                    return;
                }
                catch (IOException ex) when (attempt < maxRetries && ex.Message.Contains("being used by another process"))
                {
                    _logger.LogWarning(ex, "File locked on attempt {Attempt} for {FileName}, closing processes and retrying in {Delay}ms", 
                        attempt, file.Name, retryDelay.TotalMilliseconds);
                    
                    // Try to close processes that might be using the file
                    var localPath = Path.Combine(savePath, file.SavePath, file.Name);
                    await TryCloseFileProcessesAsync(localPath);
                    
                    // Additional delay for file handles to be released
                    await Task.Delay(1000);
                    
                    await Task.Delay(retryDelay, cancellationToken);
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

        private async Task DownloadSingleFileAsync(FileModel file, string updateUrl, string savePath, CancellationToken cancellationToken, IProgress<UpdateProgress>? progress = null, int currentFile = 0, int totalFiles = 0, long downloadedBytes = 0, long totalBytes = 0)
        {
            // Construir a URL correta para o arquivo ZIP
            // O builder cria arquivos ZIP individuais: PATCH/system/arquivo.zip
            var fileZipPath = Path.Combine(file.Path, file.Name + ".zip");
            var downloadUrl = CombineUrl(updateUrl, fileZipPath);
            var localPath = Path.Combine(savePath, file.SavePath, file.Name);
            
            _logger.LogDebug("Downloading file: {FileName} from {DownloadUrl} to {LocalPath}", file.Name, downloadUrl, localPath);
            
            // Ensure directory exists
            var directory = Path.GetDirectoryName(localPath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            // Try to close any processes that might be using the file
            await TryCloseFileProcessesAsync(localPath);

            // Try to delete existing file if it exists and is not in use
            if (File.Exists(localPath))
            {
                try
                {
                    // Try to delete with retry mechanism
                    await DeleteFileWithRetryAsync(localPath, maxRetries: 3, delayMs: 1000);
                }
                catch (IOException ex)
                {
                    _logger.LogWarning(ex, "Cannot delete existing file {FilePath}, will try to overwrite", localPath);
                    // Continue with download attempt - File.Create will overwrite if possible
                }
            }

            // Create a new HttpClient instance for this download to avoid state sharing
            using var httpClient = CreateHttpClient();
            
            // Clean up old temporary files
            await CleanupTempFilesAsync(Path.GetTempPath());
            
            // Use a unique temporary file name to avoid conflicts
            var tempZipPath = Path.Combine(Path.GetTempPath(), $"l2updater_{Guid.NewGuid()}_{file.Name}.zip");
            
                         try
             {
                 using var response = await httpClient.GetAsync(downloadUrl, cancellationToken);
                 response.EnsureSuccessStatusCode();

                 // Download to temporary location with proper file sharing
                 using (var fileStream = new FileStream(tempZipPath, FileMode.Create, FileAccess.Write, FileShare.None))
                 {
                     var contentLength = response.Content.Headers.ContentLength ?? 0;
                     var buffer = new byte[8192];
                     var totalRead = 0L;
                     
                     using var contentStream = await response.Content.ReadAsStreamAsync(cancellationToken);
                     
                     while (true)
                     {
                         var bytesRead = await contentStream.ReadAsync(buffer, 0, buffer.Length, cancellationToken);
                         if (bytesRead == 0) break;
                         
                         await fileStream.WriteAsync(buffer, 0, bytesRead, cancellationToken);
                         totalRead += bytesRead;
                         
                         // Report progress during download
                         if (progress != null && contentLength > 0)
                         {
                             var fileProgress = (double)totalRead / contentLength * 100;
                             var overallProgress = (double)(downloadedBytes + totalRead) / totalBytes * 100;
                             
                             progress.Report(new UpdateProgress
                             {
                                 CurrentFile = currentFile,
                                 TotalFiles = totalFiles,
                                 BytesDownloaded = downloadedBytes + totalRead,
                                 TotalBytes = totalBytes,
                                 CurrentFileName = file.Name,
                                 Status = $"Downloading {file.Name}... ({fileProgress:F1}%)"
                             });
                         }
                     }
                     
                     // Ensure the file is fully written before proceeding
                     fileStream.Flush();
                 }
                 
                 // Force garbage collection to ensure file handles are released
                 GC.Collect();
                 GC.WaitForPendingFinalizers();
                 
                 // Small delay to ensure file handles are released
                 await Task.Delay(100);
                 
                 // Extract the ZIP file
                 _logger.LogDebug("Extracting ZIP file: {ZipPath} to {ExtractPath}", tempZipPath, localPath);
                 await ExtractZipFileAsync(tempZipPath, localPath, cancellationToken);
             }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "HTTP error downloading {FileName} from {Url}", file.Name, downloadUrl);
                throw new InvalidOperationException($"Failed to download {file.Name}: {ex.Message}", ex);
            }
            catch (TaskCanceledException ex)
            {
                _logger.LogError(ex, "Timeout downloading {FileName} from {Url}", file.Name, downloadUrl);
                throw new InvalidOperationException($"Timeout downloading {file.Name}", ex);
            }
            finally
            {
                // Always clean up the temporary ZIP file
                try
                {
                    if (File.Exists(tempZipPath))
                    {
                        File.Delete(tempZipPath);
                    }
                }
                catch (IOException ex)
                {
                    _logger.LogWarning(ex, "Could not delete temporary ZIP file: {ZipPath}", tempZipPath);
                }
            }

            // Validate file integrity if hash is provided
            if (!string.IsNullOrEmpty(file.Hash))
            {
                try
                {
                    if (!await ValidateDownloadedFileAsync(localPath, file.Hash, file.Name))
                    {
                        try
                        {
                            File.Delete(localPath);
                        }
                        catch (IOException ex)
                        {
                            _logger.LogWarning(ex, "Cannot delete corrupted file {FilePath}", localPath);
                        }
                        throw new InvalidOperationException($"File integrity check failed for {file.Name}");
                    }
                    else
                    {
                        _logger.LogDebug("File integrity check passed for {FileName}", file.Name);
                    }
                }
                catch (IOException ex)
                {
                    _logger.LogWarning(ex, "Cannot validate file integrity for {FilePath}, skipping validation", localPath);
                    // Continue without validation if file is in use
                }
            }

            _logger.LogDebug("Successfully downloaded {FileName} to {LocalPath}", file.Name, localPath);
        }

        private async Task DeleteFileWithRetryAsync(string filePath, int maxRetries, int delayMs)
        {
            for (int attempt = 1; attempt <= maxRetries; attempt++)
            {
                try
                {
                    File.Delete(filePath);
                    return;
                }
                catch (IOException) when (attempt < maxRetries)
                {
                    await Task.Delay(delayMs);
                }
            }
            throw new IOException($"Failed to delete file after {maxRetries} attempts: {filePath}");
        }

        private async Task ExtractZipFileAsync(string zipPath, string extractPath, CancellationToken cancellationToken)
        {
            try
            {
                // Ensure the directory exists
                var extractDir = Path.GetDirectoryName(extractPath);
                if (!string.IsNullOrEmpty(extractDir) && !Directory.Exists(extractDir))
                {
                    Directory.CreateDirectory(extractDir);
                }

                // Check if ZIP file exists and is accessible
                if (!File.Exists(zipPath))
                {
                    throw new FileNotFoundException($"ZIP file not found: {zipPath}");
                }

                // Wait for ZIP file to be accessible
                if (!await WaitForFileAccessAsync(zipPath))
                {
                    throw new IOException($"ZIP file is locked by another process after waiting: {zipPath}");
                }

                                 // Use a more robust approach to extract the ZIP file
                 using (var fileStream = new FileStream(zipPath, FileMode.Open, FileAccess.Read, FileShare.Read))
                 {
                     using var archive = new System.IO.Compression.ZipArchive(fileStream, System.IO.Compression.ZipArchiveMode.Read);
                     
                     var entry = archive.Entries.FirstOrDefault();
                     
                     if (entry == null)
                     {
                         throw new InvalidOperationException($"No entries found in ZIP file: {zipPath}");
                     }

                     // Check if target file is locked before extraction
                     if (File.Exists(extractPath))
                     {
                         if (!await WaitForFileAccessAsync(extractPath))
                         {
                             throw new IOException($"Target file is locked by another process after waiting: {extractPath}");
                         }
                     }

                     // Extract the first (and only) entry to the target path
                     using var entryStream = entry.Open();
                     using var targetStream = new FileStream(extractPath, FileMode.Create, FileAccess.Write, FileShare.None);
                     await entryStream.CopyToAsync(targetStream, cancellationToken);
                 }
                
                                 _logger.LogDebug("Successfully extracted file from {ZipPath} to {ExtractPath}", 
                     zipPath, extractPath);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error extracting ZIP file: {ZipPath} to {ExtractPath}", zipPath, extractPath);
                throw new InvalidOperationException($"Failed to extract ZIP file {zipPath}: {ex.Message}", ex);
            }
        }

        private async Task<bool> IsFileAccessibleAsync(string filePath)
        {
            try
            {
                // Try to open the file with exclusive access to check if it's locked
                using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.None);
                return true;
            }
            catch (IOException)
            {
                // File is locked by another process
                return false;
            }
            catch (UnauthorizedAccessException)
            {
                // File access is denied
                return false;
            }
        }

        private async Task<bool> WaitForFileAccessAsync(string filePath, int maxWaitMs = 10000)
        {
            var startTime = DateTime.UtcNow;
            var waitInterval = 500; // Check every 500ms
            
            while ((DateTime.UtcNow - startTime).TotalMilliseconds < maxWaitMs)
            {
                if (await IsFileAccessibleAsync(filePath))
                {
                    return true;
                }
                
                await Task.Delay(waitInterval);
            }
            
            return false;
        }

        private async Task CleanupTempFilesAsync(string directory)
        {
            try
            {
                if (!Directory.Exists(directory))
                    return;

                var tempFiles = Directory.GetFiles(directory, "l2updater_*.zip", SearchOption.TopDirectoryOnly);
                var cutoffTime = DateTime.UtcNow.AddHours(-1); // Clean files older than 1 hour

                foreach (var tempFile in tempFiles)
                {
                    try
                    {
                        var fileInfo = new FileInfo(tempFile);
                        if (fileInfo.CreationTimeUtc < cutoffTime)
                        {
                            File.Delete(tempFile);
                            _logger.LogDebug("Cleaned up old temporary file: {TempFile}", tempFile);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogDebug(ex, "Could not clean up temporary file: {TempFile}", tempFile);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error cleaning up temporary files in directory: {Directory}", directory);
            }
        }

        // Método removido por segurança - não fechamos processos automaticamente

        private async Task TryCloseFileProcessesAsync(string filePath)
        {
            try
            {
                // Check if auto-close is enabled
                if (!_settings.UpdateSettings.AutoCloseGameProcesses)
                {
                    _logger.LogDebug("Auto-close game processes is disabled");
                    return;
                }

                var fileName = Path.GetFileName(filePath);
                _logger.LogInformation("Auto-close is disabled for safety. Please close game manually if needed.");
                
                // Por segurança, não fechamos nenhum processo automaticamente
                // O usuário deve fechar o jogo manualmente se necessário
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error in TryCloseFileProcessesAsync for {FilePath}", filePath);
            }
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
