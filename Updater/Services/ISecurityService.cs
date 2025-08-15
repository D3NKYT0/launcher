using System.IO;
using System.Threading.Tasks;

namespace Updater.Services
{
    public interface ISecurityService
    {
        Task<bool> ValidateFileSignatureAsync(string filePath);
        bool IsFileExtensionAllowed(string fileName);
        bool IsFileExtensionBlocked(string fileName);
        Task<bool> ValidateFileIntegrityAsync(string filePath, string expectedHash);
        Task<string> CalculateFileHashAsync(string filePath);
        bool ValidateCertificate(string url);
    }
}
