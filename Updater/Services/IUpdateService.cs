using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System;
using Updater.DataContractModels;
using Updater.Models;

namespace Updater.Services
{
    public interface IUpdateService
    {
        Task<UpdateInfoModel> GetUpdateInfoAsync(string updateUrl, CancellationToken cancellationToken = default);
        Task<UpdateConfig> GetUpdateConfigAsync(string updateUrl, CancellationToken cancellationToken = default);
        Task<bool> CheckForSelfUpdateAsync(string updateUrl, CancellationToken cancellationToken = default);
        Task<bool> PerformSelfUpdateAsync(string updateUrl, CancellationToken cancellationToken = default);
        Task<List<FileModel>> GetFilesToUpdateAsync(UpdateInfoModel updateInfo, UpdateTypes updateType, string rootPath, CancellationToken cancellationToken = default);
        Task<bool> DownloadAndUpdateFilesAsync(IEnumerable<FileModel> files, string updateUrl, string savePath, IProgress<UpdateProgress> progress, CancellationToken cancellationToken = default);
        Task<bool> ValidateUpdateAsync(UpdateInfoModel updateInfo, CancellationToken cancellationToken = default);
    }

    public class UpdateProgress
    {
        public int CurrentFile { get; set; }
        public int TotalFiles { get; set; }
        public long BytesDownloaded { get; set; }
        public long TotalBytes { get; set; }
        public string CurrentFileName { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public double ProgressPercentage => TotalFiles > 0 ? (double)CurrentFile / TotalFiles * 100 : 0;
        public double DownloadProgressPercentage => TotalBytes > 0 ? (double)BytesDownloaded / TotalBytes * 100 : 0;
    }
}
