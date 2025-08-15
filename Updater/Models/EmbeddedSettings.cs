using System.Collections.Generic;

namespace Updater.Models
{
    /// <summary>
    /// Configurações embutidas no código para eliminar dependência do arquivo appsettings.json
    /// </summary>
    public static class EmbeddedSettings
    {
        public static AppSettings GetDefaultSettings()
        {
            return new AppSettings
            {
                UpdateSettings = new UpdateSettings
                {
                    UpdateUrl = "https://updater.denky.dev.br",
                    SelfUpdatePath = "https://updater.denky.dev.br",
                    UpdaterVersion = 1,
                    GameStartPath = "system",
                    PatchPath = "https://updater.denky.dev.br",
                    MaxRetryAttempts = 3,
                    RetryDelaySeconds = 5,
                    DownloadTimeoutSeconds = 300,
                    MaxConcurrentDownloads = 3
                },
                GameSettings = new GameSettings
                {
                    ExecutableName = "l2.exe",
                    BinaryName = "l2.bin",
                    ExpectedExecutableSize = 429568,
                    WorkingDirectory = "system"
                },
                Links = new Links
                {
                    Site = "https://pdl.denky.dev.br",
                    Forum = "https://forum.pdl.com",
                    Registration = "https://pdl.denky.dev.br",
                    AboutServer = "https://pdl.denky.dev.br",
                    Help = "https://pdl.denky.dev.br",
                    Bonus = "https://pdl.denky.dev.br",
                    Facebook = "https://facebook.com/pdl",
                    Discord = "https://discord.gg/pdl",
                    Telegram = "https://t.me/pdl",
                    VK = "https://vk.com/pdl",
                    Support = "https://pdl.denky.dev.br",
                    Donation = "https://pdl.denky.dev.br",
                    Cabinet = "https://pdl.denky.dev.br",
                    L2Top = "https://pdl.denky.dev.br",
                    MMOTop = "https://pdl.denky.dev.br"
                },
                DownloadSettings = new DownloadSettings
                {
                    DownloadLink1 = "https://pdl.denky.dev.br",
                    DownloadLink2 = "https://pdl.denky.dev.br",
                    DownloadLink3 = "https://pdl.denky.dev.br"
                },
                Logging = new LoggingSettings
                {
                    LogLevel = new LogLevel
                    {
                        Default = "Information",
                        Microsoft = "Warning",
                        MicrosoftHostingLifetime = "Information"
                    },
                    LogFilePath = "logs/l2updater.log",
                    MaxLogFileSizeMB = 10,
                    RetainedLogFiles = 5
                },
                Security = new SecuritySettings
                {
                    ValidateCertificates = false,
                    RequireSignatureValidation = false,
                    AllowedFileExtensions = new List<string> { ".exe", ".dll", ".bin", ".dat", ".ini", ".txt", ".xml", ".zip" },
                    BlockedFileExtensions = new List<string> { ".bat", ".cmd", ".ps1", ".vbs", ".js" }
                }
            };
        }

        /// <summary>
        /// Método para personalizar configurações específicas do servidor
        /// </summary>
        /// <param name="serverName">Nome do servidor</param>
        /// <param name="baseUrl">URL base do servidor</param>
        /// <returns>Configurações personalizadas</returns>
        public static AppSettings GetCustomizedSettings(string serverName, string baseUrl)
        {
            var settings = GetDefaultSettings();
            
            // Personalizar URLs baseadas no servidor
            settings.UpdateSettings.UpdateUrl = $"{baseUrl}/update";
            settings.UpdateSettings.SelfUpdatePath = $"{baseUrl}/update";
            settings.UpdateSettings.PatchPath = $"{baseUrl}/update";
            
            settings.Links.Site = baseUrl;
            settings.Links.Registration = $"{baseUrl}/register";
            settings.Links.AboutServer = $"{baseUrl}/about";
            settings.Links.Help = $"{baseUrl}/help";
            settings.Links.Bonus = $"{baseUrl}/bonus";
            settings.Links.Support = $"{baseUrl}/support";
            settings.Links.Donation = $"{baseUrl}/donation";
            settings.Links.Cabinet = $"{baseUrl}/cabinet";
            settings.Links.L2Top = $"{baseUrl}/l2top";
            settings.Links.MMOTop = $"{baseUrl}/mmotop";
            
            settings.DownloadSettings.DownloadLink1 = $"{baseUrl}/download";
            settings.DownloadSettings.DownloadLink2 = $"{baseUrl}/download2";
            settings.DownloadSettings.DownloadLink3 = $"{baseUrl}/download3";
            
            return settings;
        }
    }
}
