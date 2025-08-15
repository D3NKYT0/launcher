# Guia de Deploy - L2Updater

## Visão Geral

Este guia detalha como fazer o deploy e distribuição do L2Updater refatorado, incluindo compilação, configuração e distribuição para usuários finais.

## 🚀 Pré-requisitos

### **Para Desenvolvimento**
- **.NET 9.0 SDK** ou superior
- **Visual Studio 2022** ou **VS Code**
- **Windows 10/11** (64-bit)

### **Para Distribuição**
- **.NET 9.0 Runtime** (para usuários finais)
- **Windows 10/11** (64-bit) - Sistema alvo

## 📦 Compilação

### **1. Compilação Local**
```bash
# Navegar para o diretório do projeto
cd L2Updater

# Restaurar dependências
dotnet restore

# Compilar em modo Debug
dotnet build

# Compilar em modo Release
dotnet build -c Release
```

### **2. Publicação para Distribuição**
```bash
# Publicação para Windows x64 (Framework-dependent)
dotnet publish -c Release -r win-x64 --self-contained false

# Publicação para Windows x64 (Self-contained)
dotnet publish -c Release -r win-x64 --self-contained true

# Publicação como arquivo único
dotnet publish -c Release -r win-x64 --self-contained false -p:PublishSingleFile=true
```

### **3. Configurações de Publicação**

#### **Framework-dependent (Recomendado)**
```bash
dotnet publish -c Release -r win-x64 --self-contained false -p:PublishSingleFile=true -p:PublishTrimmed=true
```

**Vantagens:**
- ✅ Arquivo menor (~10-20MB)
- ✅ Usa .NET Runtime do sistema
- ✅ Atualizações automáticas do runtime

**Desvantagens:**
- ❌ Requer .NET 9.0 Runtime instalado
- ❌ Pode ter problemas de compatibilidade

#### **Self-contained**
```bash
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:PublishTrimmed=true
```

**Vantagens:**
- ✅ Funciona sem .NET Runtime
- ✅ Garantia de compatibilidade
- ✅ Distribuição independente

**Desvantagens:**
- ❌ Arquivo maior (~50-100MB)
- ❌ Não se beneficia de atualizações do runtime

## ⚙️ Configuração

### **1. Configuração do Servidor**

#### **Estrutura de Arquivos no Servidor**
```
https://your-server.com/update/
├── UpdateInfo.xml          # Informações de atualização
├── UpdateConfig.xml        # Configuração do launcher
├── updaterver.txt          # Versão do updater
├── upd.exe                 # Auto-updater
└── files/                  # Arquivos do jogo
    ├── system/
    │   ├── l2.exe
    │   ├── l2.bin
    │   └── *.dll
    └── data/
        └── *.dat
```

#### **UpdateInfo.xml**
```xml
<?xml version="1.0" encoding="utf-8"?>
<UpdateInfoModel>
  <Folder>
    <Name>system</Name>
    <Files>
      <FileModel>
        <Name>l2.exe</Name>
        <Path>system/l2.exe</Path>
        <Size>429568</Size>
        <Hash>a1b2c3d4e5f6...</Hash>
        <CheckHash>true</CheckHash>
        <QuickUpdate>true</QuickUpdate>
      </FileModel>
    </Files>
  </Folder>
</UpdateInfoModel>
```

#### **UpdateConfig.xml**
```xml
<?xml version="1.0" encoding="utf-8"?>
<UpdateConfig>
  <UpdaterTitle>L2TheOne Launcher</UpdaterTitle>
  <UpdaterVersion>3</UpdaterVersion>
  <GameStartPath>system</GameStartPath>
  <SiteLink>https://l2theone.com</SiteLink>
  <ForumLink>https://forum.l2theone.com</ForumLink>
  <DiscordLink>https://discord.gg/l2theone</DiscordLink>
</UpdateConfig>
```

### **2. Configuração do Cliente**

#### **appsettings.json**
```json
{
  "UpdateSettings": {
    "UpdateUrl": "https://your-server.com/update/",
    "SelfUpdatePath": "https://your-server.com/update/",
    "UpdaterVersion": 3,
    "MaxRetryAttempts": 3,
    "RetryDelaySeconds": 5,
    "DownloadTimeoutSeconds": 300,
    "MaxConcurrentDownloads": 3
  },
  "GameSettings": {
    "ExecutableName": "l2.exe",
    "BinaryName": "l2.bin",
    "ExpectedExecutableSize": 429568,
    "WorkingDirectory": "system"
  },
  "Security": {
    "ValidateCertificates": true,
    "RequireSignatureValidation": true,
    "AllowedFileExtensions": [".exe", ".dll", ".bin", ".dat", ".ini", ".txt", ".xml", ".zip"],
    "BlockedFileExtensions": [".bat", ".cmd", ".ps1", ".vbs", ".js"]
  }
}
```

## 📋 Checklist de Deploy

### **✅ Pré-deploy**
- [ ] Compilar em modo Release
- [ ] Testar localmente
- [ ] Verificar configurações
- [ ] Preparar arquivos do servidor
- [ ] Configurar HTTPS no servidor

### **✅ Deploy do Servidor**
- [ ] Upload dos arquivos de atualização
- [ ] Configurar CORS se necessário
- [ ] Testar URLs de download
- [ ] Verificar certificados SSL
- [ ] Testar auto-update

### **✅ Deploy do Cliente**
- [ ] Publicar executável
- [ ] Configurar appsettings.json
- [ ] Testar em ambiente limpo
- [ ] Verificar logs
- [ ] Testar todas as funcionalidades

## 🔧 Configurações Avançadas

### **1. Configuração de Logs**
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft": "Warning",
      "Microsoft.Hosting.Lifetime": "Information"
    },
    "LogFilePath": "logs/l2updater.log",
    "MaxLogFileSizeMB": 10,
    "RetainedLogFiles": 5
  }
}
```

### **2. Configuração de Performance**
```json
{
  "UpdateSettings": {
    "MaxConcurrentDownloads": 5,
    "DownloadTimeoutSeconds": 600,
    "RetryDelaySeconds": 10
  }
}
```

### **3. Configuração de Segurança**
```json
{
  "Security": {
    "ValidateCertificates": true,
    "RequireSignatureValidation": true,
    "AllowedFileExtensions": [".exe", ".dll", ".bin", ".dat", ".ini", ".txt", ".xml", ".zip"],
    "BlockedFileExtensions": [".bat", ".cmd", ".ps1", ".vbs", ".js", ".jar", ".msi"]
  }
}
```

## 🚀 Scripts de Deploy

### **1. Script de Build (build.bat)**
```batch
@echo off
echo Building L2Updater...

REM Limpar builds anteriores
dotnet clean

REM Restaurar dependências
dotnet restore

REM Build em Release
dotnet build -c Release

REM Publicar
dotnet publish -c Release -r win-x64 --self-contained false -p:PublishSingleFile=true -p:PublishTrimmed=true

echo Build completed!
pause
```

### **2. Script de Deploy (deploy.bat)**
```batch
@echo off
echo Deploying L2Updater...

REM Variáveis
set SOURCE_DIR=bin\Release\net9.0-windows\win-x64\publish
set TARGET_DIR=C:\inetpub\wwwroot\update
set BACKUP_DIR=C:\backup\update_%date:~-4,4%%date:~-10,2%%date:~-7,2%

REM Backup
echo Creating backup...
if not exist "%BACKUP_DIR%" mkdir "%BACKUP_DIR%"
xcopy "%TARGET_DIR%\*" "%BACKUP_DIR%\" /E /I /Y

REM Deploy
echo Deploying files...
xcopy "%SOURCE_DIR%\*" "%TARGET_DIR%\" /E /I /Y

echo Deploy completed!
pause
```

### **3. Script PowerShell (deploy.ps1)**
```powershell
param(
    [string]$Environment = "Production",
    [string]$ServerUrl = "https://your-server.com"
)

Write-Host "Deploying L2Updater to $Environment environment..."

# Build
dotnet clean
dotnet restore
dotnet build -c Release
dotnet publish -c Release -r win-x64 --self-contained false -p:PublishSingleFile=true

# Deploy
$sourcePath = "bin\Release\net9.0-windows\win-x64\publish"
$targetPath = "\\server\update"

Copy-Item -Path "$sourcePath\*" -Destination $targetPath -Recurse -Force

Write-Host "Deploy completed successfully!"
```

## 🔍 Monitoramento

### **1. Logs de Aplicação**
```csharp
// Configuração de logs estruturados
_logger.LogInformation("Application started");
_logger.LogWarning("Update check failed: {Error}", error);
_logger.LogError(ex, "Critical error during update");
```

### **2. Monitoramento de Performance**
```csharp
// Métricas de download
var stopwatch = Stopwatch.StartNew();
await DownloadFileAsync(file);
stopwatch.Stop();

_logger.LogInformation("Download completed in {ElapsedMs}ms for {FileName}", 
    stopwatch.ElapsedMilliseconds, file.Name);
```

### **3. Health Checks**
```csharp
public async Task<bool> CheckServerHealthAsync()
{
    try
    {
        var response = await _httpClient.GetAsync(_settings.UpdateSettings.UpdateUrl);
        return response.IsSuccessStatusCode;
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Server health check failed");
        return false;
    }
}
```

## 🛠️ Troubleshooting

### **Problemas Comuns**

#### **1. Erro de Certificado SSL**
```
Error: Invalid certificate for URL: https://your-server.com
```
**Solução:**
- Verificar certificado SSL do servidor
- Configurar `"ValidateCertificates": false` temporariamente
- Usar certificado válido no servidor

#### **2. Download Falha**
```
Error: File integrity check failed
```
**Solução:**
- Verificar hash do arquivo no servidor
- Re-upload do arquivo corrompido
- Verificar conectividade de rede

#### **3. Executável Não Encontrado**
```
Error: Game executable not found
```
**Solução:**
- Verificar caminho no `appsettings.json`
- Verificar se arquivo existe no servidor
- Verificar permissões de acesso

### **Logs de Debug**
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Debug",
      "Microsoft": "Information"
    }
  }
}
```

## 📊 Métricas de Deploy

### **Tamanhos de Arquivo**
| Configuração | Tamanho | Descrição |
|--------------|---------|-----------|
| Framework-dependent | ~15MB | Requer .NET 9 Runtime |
| Self-contained | ~80MB | Independente |
| Single File | ~20MB | Arquivo único |

### **Performance**
| Métrica | Valor |
|---------|-------|
| **Tempo de Inicialização** | < 2 segundos |
| **Download Paralelo** | 3-5 arquivos simultâneos |
| **Retry Automático** | 3 tentativas |
| **Timeout de Download** | 5 minutos |

## 🔄 Atualizações

### **1. Auto-update**
```csharp
// Verificação automática de versão
if (await _updateService.CheckForSelfUpdateAsync(updateUrl))
{
    await _updateService.PerformSelfUpdateAsync(updateUrl);
}
```

### **2. Rollback**
```csharp
// Backup antes da atualização
var backupPath = Path.Combine(savePath, "backup", DateTime.Now.ToString("yyyyMMdd_HHmmss"));
Directory.CreateDirectory(backupPath);
CopyDirectory(savePath, backupPath);
```

### **3. Versionamento**
```csharp
// Controle de versão
public const int CURRENT_VERSION = 3;
public const string VERSION_STRING = "2.1.0";
```

## 🎯 Próximos Passos

### **Melhorias de Deploy**
- [ ] CI/CD Pipeline com GitHub Actions
- [ ] Deploy automatizado
- [ ] Testes automatizados
- [ ] Monitoramento em tempo real
- [ ] Rollback automático

### **Distribuição**
- [ ] Instalador MSI
- [ ] Auto-updater silencioso
- [ ] Distribuição via CDN
- [ ] Cache inteligente
- [ ] Delta updates

---

**Nota**: Este guia garante um deploy seguro e eficiente do L2Updater, seguindo as melhores práticas de distribuição de software.
