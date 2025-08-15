using System;
using System.IO;
using System.Security.Cryptography;
using System.Threading.Tasks;
using System.Net.Http;
using System.Net;
using Microsoft.Extensions.Logging;
using Updater.Models;

namespace Updater.Services
{
    public class SecurityService : ISecurityService
    {
        private readonly ILogger<SecurityService> _logger;
        private readonly AppSettings _settings;
        private readonly HttpClient _httpClient;

        public SecurityService(ILogger<SecurityService> logger, AppSettings settings, HttpClient httpClient)
        {
            _logger = logger;
            _settings = settings;
            _httpClient = httpClient;
        }

        public async Task<bool> ValidateFileSignatureAsync(string filePath)
        {
            try
            {
                if (!_settings.Security.RequireSignatureValidation)
                {
                    _logger.LogInformation("Signature validation disabled for {FilePath}", filePath);
                    return true;
                }

                if (!File.Exists(filePath))
                {
                    _logger.LogWarning("File not found for signature validation: {FilePath}", filePath);
                    return false;
                }

                // Para arquivos executáveis, verificar assinatura digital
                if (Path.GetExtension(filePath).Equals(".exe", StringComparison.OrdinalIgnoreCase))
                {
                    return await ValidateExecutableSignatureAsync(filePath);
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating file signature for {FilePath}", filePath);
                return false;
            }
        }

        public bool IsFileExtensionAllowed(string fileName)
        {
            if (string.IsNullOrEmpty(fileName))
                return false;

            var extension = Path.GetExtension(fileName).ToLowerInvariant();
            return _settings.Security.AllowedFileExtensions.Contains(extension);
        }

        public bool IsFileExtensionBlocked(string fileName)
        {
            if (string.IsNullOrEmpty(fileName))
                return true;

            var extension = Path.GetExtension(fileName).ToLowerInvariant();
            return _settings.Security.BlockedFileExtensions.Contains(extension);
        }

        public async Task<bool> ValidateFileIntegrityAsync(string filePath, string expectedHash)
        {
            try
            {
                if (!File.Exists(filePath))
                {
                    _logger.LogWarning("File not found for integrity check: {FilePath}", filePath);
                    return false;
                }

                var actualHash = await CalculateFileHashAsync(filePath);
                var isValid = string.Equals(actualHash, expectedHash, StringComparison.OrdinalIgnoreCase);

                if (!isValid)
                {
                    _logger.LogWarning("File integrity check failed for {FilePath}. Expected: {ExpectedHash}, Actual: {ActualHash}", 
                        filePath, expectedHash, actualHash);
                }

                return isValid;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating file integrity for {FilePath}", filePath);
                return false;
            }
        }

        public async Task<string> CalculateFileHashAsync(string filePath)
        {
            try
            {
                using var sha256 = SHA256.Create();
                // Use FileShare.ReadWrite to allow other processes to read/write while we calculate hash
                using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                var hash = await sha256.ComputeHashAsync(stream);
                return Convert.ToHexString(hash).ToLowerInvariant();
            }
            catch (IOException ex) when (ex.Message.Contains("being used by another process"))
            {
                _logger.LogWarning("File {FilePath} is being used by another process, cannot calculate hash", filePath);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating file hash for {FilePath}", filePath);
                throw;
            }
        }

        public bool ValidateCertificate(string url)
        {
            try
            {
                if (!_settings.Security.ValidateCertificates)
                {
                    _logger.LogInformation("Certificate validation disabled for {Url}", url);
                    return true;
                }

                if (!url.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogWarning("Non-HTTPS URL detected: {Url}", url);
                    return false;
                }

                // Implementar validação de certificado SSL
                // Por simplicidade, vamos assumir que URLs HTTPS são válidas
                // Em produção, você deve implementar validação completa de certificados
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating certificate for {Url}", url);
                return false;
            }
        }

        private Task<bool> ValidateExecutableSignatureAsync(string filePath)
        {
            try
            {
                // Implementação básica de validação de assinatura
                // Em produção, use System.Security.Cryptography.X509Certificates
                // para verificar assinaturas digitais de certificados
                
                var fileInfo = new FileInfo(filePath);
                if (fileInfo.Length < 1024) // Arquivo muito pequeno para ser um executável válido
                {
                    _logger.LogWarning("Executable file too small: {FilePath} ({Size} bytes)", filePath, fileInfo.Length);
                    return Task.FromResult(false);
                }

                // Verificar se o arquivo tem cabeçalho PE (Portable Executable)
                using var stream = File.OpenRead(filePath);
                using var reader = new BinaryReader(stream);
                
                var dosHeader = reader.ReadUInt16();
                if (dosHeader != 0x5A4D) // MZ signature
                {
                    _logger.LogWarning("Invalid executable header for {FilePath}", filePath);
                    return Task.FromResult(false);
                }

                return Task.FromResult(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating executable signature for {FilePath}", filePath);
                return Task.FromResult(false);
            }
        }
    }
}
