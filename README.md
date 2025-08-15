# L2Updater - Modern Launcher for Lineage 2

A modern, secure, and maintainable launcher/updater for Lineage 2 private servers, built with .NET 6 and WPF.

## 🚀 Features

### ✨ Modern Architecture
- **.NET 6** - Latest LTS version with performance improvements
- **MVVM Pattern** - Clean separation of concerns using CommunityToolkit.Mvvm
- **Dependency Injection** - Microsoft.Extensions.DependencyInjection for loose coupling
- **Configuration Management** - JSON-based configuration with hot reload
- **Structured Logging** - Serilog for comprehensive logging

### 🔒 Security Enhancements
- **Certificate Validation** - HTTPS certificate verification
- **File Integrity Checks** - SHA256 hash validation for downloaded files
- **Executable Validation** - PE header verification for executables
- **Extension Filtering** - Whitelist/blacklist for file extensions
- **Secure Downloads** - HttpClient with proper timeout and retry logic

### ⚡ Performance Improvements
- **Concurrent Downloads** - Parallel file downloads with configurable limits
- **Retry Logic** - Automatic retry with exponential backoff
- **Progress Tracking** - Real-time progress with detailed status
- **Memory Optimization** - Efficient resource management

### 🛠️ Developer Experience
- **Clean Code** - SOLID principles and modern C# practices
- **Error Handling** - Comprehensive exception handling with logging
- **Unit Testing Ready** - Services designed for easy testing
- **Configuration Driven** - Easy customization without code changes

## 📋 Requirements

- **.NET 6.0 Runtime** or later
- **Windows 10/11** (64-bit)
- **Administrator privileges** (for game execution)

## 🚀 Quick Start

### 1. Configuration
Edit `appsettings.json` to configure your server settings:

```json
{
  "UpdateSettings": {
    "UpdateUrl": "https://your-server.com/update/",
    "MaxConcurrentDownloads": 3,
    "RetryDelaySeconds": 5
  },
  "GameSettings": {
    "ExecutableName": "l2.exe",
    "ExpectedExecutableSize": 429568
  }
}
```

### 2. Build and Run
```bash
# Build the project
dotnet build

# Run the application
dotnet run

# Or publish for distribution
dotnet publish -c Release -r win-x64 --self-contained false
```

## 🏗️ Architecture

### Project Structure
```
L2Updater/
├── Services/           # Business logic services
│   ├── IUpdateService.cs
│   ├── UpdateService.cs
│   ├── ISecurityService.cs
│   └── SecurityService.cs
├── ViewModels/         # MVVM ViewModels
│   └── MainViewModel.cs
├── Models/             # Data models
│   └── AppSettings.cs
├── DataContractModels/ # Legacy models (for compatibility)
└── UtillsClasses/      # Utility classes
```

### Key Components

#### UpdateService
- Handles all update-related operations
- Manages concurrent downloads
- Implements retry logic
- Validates update integrity

#### SecurityService
- Validates file signatures
- Checks file integrity
- Filters file extensions
- Validates SSL certificates

#### MainViewModel
- Manages UI state and commands
- Handles user interactions
- Provides progress updates
- Implements MVVM pattern

## 🔧 Configuration

### Update Settings
- `UpdateUrl`: Server update URL
- `MaxRetryAttempts`: Number of retry attempts
- `DownloadTimeoutSeconds`: Download timeout
- `MaxConcurrentDownloads`: Concurrent download limit

### Security Settings
- `ValidateCertificates`: Enable SSL certificate validation
- `RequireSignatureValidation`: Enable executable signature validation
- `AllowedFileExtensions`: Whitelist of allowed file extensions
- `BlockedFileExtensions`: Blacklist of blocked file extensions

### Logging Settings
- `LogFilePath`: Path to log file
- `MaxLogFileSizeMB`: Maximum log file size
- `RetainedLogFiles`: Number of log files to retain

## 🧪 Testing

The project is designed for easy testing:

```csharp
// Example unit test
[Test]
public async Task UpdateService_ValidUpdateInfo_ReturnsSuccess()
{
    // Arrange
    var mockHttpClient = new Mock<IHttpClient>();
    var mockSecurityService = new Mock<ISecurityService>();
    var service = new UpdateService(mockHttpClient.Object, mockSecurityService.Object, settings);

    // Act
    var result = await service.ValidateUpdateAsync(updateInfo);

    // Assert
    Assert.IsTrue(result);
}
```

## 📝 Logging

The application uses structured logging with Serilog:

```csharp
_logger.LogInformation("Starting {UpdateType} update", updateType);
_logger.LogError(ex, "Error downloading file {FileName}", fileName);
```

Logs are written to:
- Console (during development)
- File (in production)
- Structured format for easy parsing

## 🔄 Migration from Legacy Version

### Breaking Changes
- **.NET Framework 4.5** → **.NET 6**
- **WebClient** → **HttpClient**
- **XML Configuration** → **JSON Configuration**
- **Manual MVVM** → **CommunityToolkit.Mvvm**

### Migration Steps
1. Update project file to .NET 6
2. Replace WebClient with HttpClient
3. Convert XML config to JSON
4. Update ViewModels to use CommunityToolkit.Mvvm
5. Add dependency injection setup

## 🤝 Contributing

1. Fork the repository
2. Create a feature branch
3. Make your changes
4. Add tests if applicable
5. Submit a pull request

## 📄 License

This project is licensed under the MIT License - see the LICENSE file for details.

## 🆘 Support

For support and questions:
- Create an issue on GitHub
- Check the logs in `logs/l2updater.log`
- Review the configuration in `appsettings.json`

## 🔮 Future Enhancements

- [ ] Multi-language support (beyond Russian/English)
- [ ] CDN support for faster downloads
- [ ] Delta updates for smaller downloads
- [ ] Automatic backup before updates
- [ ] Plugin system for custom features
- [ ] Web-based admin panel
- [ ] Real-time server status
- [ ] In-game integration

---

**Note**: This is a refactored version of the original L2Updater project, addressing security vulnerabilities, performance issues, and maintainability concerns while preserving all original functionality.
