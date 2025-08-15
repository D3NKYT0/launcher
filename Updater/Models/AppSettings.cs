using System.Collections.Generic;

namespace Updater.Models
{
    public class AppSettings
    {
        public UpdateSettings UpdateSettings { get; set; } = new();
        public GameSettings GameSettings { get; set; } = new();
        public Links Links { get; set; } = new();
        public DownloadSettings DownloadSettings { get; set; } = new();
        public LoggingSettings Logging { get; set; } = new();
        public SecuritySettings Security { get; set; } = new();
    }

    public class UpdateSettings
    {
        public string UpdateUrl { get; set; } = string.Empty;
        public string SelfUpdatePath { get; set; } = string.Empty;
        public int UpdaterVersion { get; set; } = 1;
        public string GameStartPath { get; set; } = "system";
        public string PatchPath { get; set; } = string.Empty;
        public int MaxRetryAttempts { get; set; } = 3;
        public int RetryDelaySeconds { get; set; } = 5;
        public int DownloadTimeoutSeconds { get; set; } = 300;
        public int MaxConcurrentDownloads { get; set; } = 3;
        public bool AutoCloseGameProcesses { get; set; } = true;
        public int ProcessCloseTimeoutMs { get; set; } = 5000;
        public int ProcessKillTimeoutMs { get; set; } = 2000;
    }

    public class GameSettings
    {
        public string ExecutableName { get; set; } = "l2.exe";
        public string BinaryName { get; set; } = "l2.bin";
        public long ExpectedExecutableSize { get; set; } = 429568;
        public string WorkingDirectory { get; set; } = "system";
    }

    public class Links
    {
        public string Site { get; set; } = string.Empty;
        public string Forum { get; set; } = string.Empty;
        public string Registration { get; set; } = string.Empty;
        public string AboutServer { get; set; } = string.Empty;
        public string Help { get; set; } = string.Empty;
        public string Bonus { get; set; } = string.Empty;
        public string Facebook { get; set; } = string.Empty;
        public string Discord { get; set; } = string.Empty;
        public string Telegram { get; set; } = string.Empty;
        public string VK { get; set; } = string.Empty;
        public string Support { get; set; } = string.Empty;
        public string Donation { get; set; } = string.Empty;
        public string Cabinet { get; set; } = string.Empty;
        public string L2Top { get; set; } = string.Empty;
        public string MMOTop { get; set; } = string.Empty;
    }

    public class DownloadSettings
    {
        public string DownloadLink1 { get; set; } = string.Empty;
        public string DownloadLink2 { get; set; } = string.Empty;
        public string DownloadLink3 { get; set; } = string.Empty;
    }

    public class LoggingSettings
    {
        public LogLevel LogLevel { get; set; } = new();
        public string LogFilePath { get; set; } = "logs/l2updater.log";
        public int MaxLogFileSizeMB { get; set; } = 10;
        public int RetainedLogFiles { get; set; } = 5;
    }

    public class LogLevel
    {
        public string Default { get; set; } = "Information";
        public string Microsoft { get; set; } = "Warning";
        public string MicrosoftHostingLifetime { get; set; } = "Information";
    }

    public class SecuritySettings
    {
        public bool ValidateCertificates { get; set; } = true;
        public bool RequireSignatureValidation { get; set; } = true;
        public List<string> AllowedFileExtensions { get; set; } = new();
        public List<string> BlockedFileExtensions { get; set; } = new();
    }
}
