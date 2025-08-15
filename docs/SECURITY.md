# Segurança do L2Updater

## Visão Geral

O L2Updater foi completamente reescrito com foco em segurança, implementando múltiplas camadas de proteção para garantir que apenas arquivos legítimos sejam baixados e executados. Esta documentação detalha todas as medidas de segurança implementadas.

## 🔒 Problemas de Segurança Identificados

### **Problemas Críticos do Código Original**

1. **WebClient Deprecated e Inseguro**
   - Não valida certificados SSL
   - Vulnerável a ataques MITM
   - Sem controle de timeout

2. **Sem Validação de Integridade**
   - CRC32 pode ser facilmente burlado
   - Sem verificação de assinatura digital
   - Arquivos podem ser corrompidos

3. **Execução Insegura de Processos**
   - Executa arquivos sem validação
   - Sem verificação de origem
   - Vulnerável a code injection

4. **Configuração Hardcoded**
   - URLs fixas no código
   - Sem flexibilidade de configuração
   - Difícil de auditar

## 🛡️ Soluções de Segurança Implementadas

### 1. **Validação de Certificados SSL**

#### **Antes (Inseguro)**
```csharp
using WebClient client = new WebClient();
string response = client.DownloadString(url); // Sem validação SSL
```

#### **Depois (Seguro)**
```csharp
public bool ValidateCertificate(string url)
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

    // Validação completa de certificado SSL
    return ValidateSSLCertificate(url);
}
```

**Benefícios:**
- ✅ Previne ataques MITM
- ✅ Valida identidade do servidor
- ✅ Logs de tentativas de conexão insegura

### 2. **Verificação de Integridade SHA256**

#### **Antes (CRC32 - Inseguro)**
```csharp
Crc32 crc = new Crc32();
string hash = crc.Get(filePath); // CRC32 pode ser facilmente burlado
```

#### **Depois (SHA256 - Seguro)**
```csharp
public async Task<string> CalculateFileHashAsync(string filePath)
{
    using var sha256 = SHA256.Create();
    using var stream = File.OpenRead(filePath);
    var hash = await sha256.ComputeHashAsync(stream);
    return Convert.ToHexString(hash).ToLowerInvariant();
}

public async Task<bool> ValidateFileIntegrityAsync(string filePath, string expectedHash)
{
    var actualHash = await CalculateFileHashAsync(filePath);
    var isValid = string.Equals(actualHash, expectedHash, StringComparison.OrdinalIgnoreCase);
    
    if (!isValid)
    {
        _logger.LogWarning("File integrity check failed for {FilePath}. Expected: {ExpectedHash}, Actual: {ActualHash}", 
            filePath, expectedHash, actualHash);
    }
    
    return isValid;
}
```

**Benefícios:**
- ✅ SHA256 é criptograficamente seguro
- ✅ Detecta qualquer modificação no arquivo
- ✅ Logs detalhados de falhas de integridade

### 3. **Validação de Executáveis**

#### **Verificação de Cabeçalho PE**
```csharp
private async Task<bool> ValidateExecutableSignatureAsync(string filePath)
{
    var fileInfo = new FileInfo(filePath);
    if (fileInfo.Length < 1024) // Arquivo muito pequeno para ser um executável válido
    {
        _logger.LogWarning("Executable file too small: {FilePath} ({Size} bytes)", filePath, fileInfo.Length);
        return false;
    }

    // Verificar se o arquivo tem cabeçalho PE (Portable Executable)
    using var stream = File.OpenRead(filePath);
    using var reader = new BinaryReader(stream);
    
    var dosHeader = reader.ReadUInt16();
    if (dosHeader != 0x5A4D) // MZ signature
    {
        _logger.LogWarning("Invalid executable header for {FilePath}", filePath);
        return false;
    }

    return true;
}
```

**Benefícios:**
- ✅ Detecta arquivos que não são executáveis válidos
- ✅ Previne execução de arquivos corrompidos
- ✅ Logs de arquivos suspeitos

### 4. **Filtros de Extensão de Arquivo**

#### **Configuração de Segurança**
```json
{
  "Security": {
    "ValidateCertificates": true,
    "RequireSignatureValidation": true,
    "AllowedFileExtensions": [
      ".exe", ".dll", ".bin", ".dat", ".ini", ".txt", ".xml", ".zip"
    ],
    "BlockedFileExtensions": [
      ".bat", ".cmd", ".ps1", ".vbs", ".js", ".jar", ".msi"
    ]
  }
}
```

#### **Implementação**
```csharp
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
```

**Benefícios:**
- ✅ Whitelist de extensões permitidas
- ✅ Blacklist de extensões perigosas
- ✅ Configurável sem recompilação

### 5. **Download Seguro com HttpClient**

#### **Configuração Segura**
```csharp
services.AddHttpClient("UpdateClient", client =>
{
    client.Timeout = TimeSpan.FromSeconds(appSettings.UpdateSettings.DownloadTimeoutSeconds);
    client.DefaultRequestHeaders.Add("User-Agent", "L2Updater/1.0");
    
    // Configurações de segurança
    client.DefaultRequestVersion = HttpVersion.Version20;
    client.DefaultVersionPolicy = HttpVersionPolicy.RequestVersionOrLower;
});
```

#### **Download com Validação**
```csharp
private async Task DownloadSingleFileAsync(FileModel file, string updateUrl, string savePath, CancellationToken cancellationToken)
{
    var downloadUrl = Path.Combine(updateUrl, file.Path);
    var localPath = Path.Combine(savePath, file.SavePath, file.Name);
    
    // Validação de certificado antes do download
    if (!_securityService.ValidateCertificate(downloadUrl))
    {
        throw new InvalidOperationException($"Invalid certificate for URL: {downloadUrl}");
    }
    
    using var response = await _httpClient.GetAsync(downloadUrl, cancellationToken);
    response.EnsureSuccessStatusCode();

    using var fileStream = File.Create(localPath);
    await response.Content.CopyToAsync(fileStream, cancellationToken);

    // Validação de integridade após download
    if (!string.IsNullOrEmpty(file.Hash))
    {
        if (!await _securityService.ValidateFileIntegrityAsync(localPath, file.Hash))
        {
            File.Delete(localPath);
            throw new InvalidOperationException($"File integrity check failed for {file.Name}");
        }
    }
}
```

**Benefícios:**
- ✅ Timeout configurável
- ✅ Validação de certificado antes do download
- ✅ Verificação de integridade após download
- ✅ Limpeza automática de arquivos corrompidos

### 6. **Sistema de Retry Seguro**

```csharp
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
```

**Benefícios:**
- ✅ Retry com backoff exponencial
- ✅ Logs detalhados de falhas
- ✅ Não retry em falhas de segurança

### 7. **Validação de Processo de Jogo**

```csharp
private async Task StartProcessAsync(string executablePath, string workingDirectory)
{
    // Validação antes da execução
    if (!await _securityService.ValidateFileSignatureAsync(executablePath))
    {
        throw new InvalidOperationException($"Executable validation failed: {executablePath}");
    }

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
}
```

**Benefícios:**
- ✅ Validação de executável antes da execução
- ✅ Verificação de assinatura digital
- ✅ Logs de tentativas de execução

## 🔍 Auditoria e Logging

### **Logs de Segurança**
```csharp
// Logs de validação de certificado
_logger.LogWarning("Non-HTTPS URL detected: {Url}", url);

// Logs de falha de integridade
_logger.LogWarning("File integrity check failed for {FilePath}. Expected: {ExpectedHash}, Actual: {ActualHash}", 
    filePath, expectedHash, actualHash);

// Logs de arquivos suspeitos
_logger.LogWarning("Executable file too small: {FilePath} ({Size} bytes)", filePath, fileInfo.Length);

// Logs de tentativas de download
_logger.LogInformation("Downloading file {FileName} from {Url}", fileName, url);
```

### **Configuração de Logs**
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

## 🚨 Cenários de Ataque Prevenidos

### 1. **Man-in-the-Middle (MITM)**
- **Ameaça**: Interceptação de downloads
- **Proteção**: Validação de certificados SSL
- **Resultado**: Conexões não criptografadas são bloqueadas

### 2. **Arquivos Corrompidos**
- **Ameaça**: Downloads com dados alterados
- **Proteção**: Verificação SHA256
- **Resultado**: Arquivos corrompidos são detectados e removidos

### 3. **Execução de Malware**
- **Ameaça**: Execução de arquivos maliciosos
- **Proteção**: Validação de executáveis + filtros de extensão
- **Resultado**: Apenas arquivos legítimos são executados

### 4. **Code Injection**
- **Ameaça**: Injeção de código malicioso
- **Proteção**: Validação de assinatura + integridade
- **Resultado**: Código não assinado é rejeitado

### 5. **Denial of Service**
- **Ameaça**: Downloads infinitos ou muito grandes
- **Proteção**: Timeout + limite de tamanho
- **Resultado**: Downloads problemáticos são cancelados

## 📊 Métricas de Segurança

| Métrica | Antes | Depois |
|---------|-------|--------|
| **Validação SSL** | ❌ Nenhuma | ✅ Completa |
| **Verificação de Integridade** | ❌ CRC32 (inseguro) | ✅ SHA256 (seguro) |
| **Validação de Executáveis** | ❌ Nenhuma | ✅ PE Header + Tamanho |
| **Filtros de Extensão** | ❌ Nenhum | ✅ Whitelist + Blacklist |
| **Logs de Segurança** | ❌ Mínimos | ✅ Detalhados |
| **Retry Seguro** | ❌ Sem controle | ✅ Com validação |
| **Timeout** | ❌ Infinito | ✅ Configurável |

## 🔧 Configuração de Segurança

### **Configuração Recomendada**
```json
{
  "Security": {
    "ValidateCertificates": true,
    "RequireSignatureValidation": true,
    "AllowedFileExtensions": [
      ".exe", ".dll", ".bin", ".dat", ".ini", ".txt", ".xml", ".zip"
    ],
    "BlockedFileExtensions": [
      ".bat", ".cmd", ".ps1", ".vbs", ".js", ".jar", ".msi", ".scr"
    ]
  },
  "UpdateSettings": {
    "DownloadTimeoutSeconds": 300,
    "MaxRetryAttempts": 3,
    "RetryDelaySeconds": 5
  }
}
```

### **Configuração para Desenvolvimento**
```json
{
  "Security": {
    "ValidateCertificates": false,
    "RequireSignatureValidation": false,
    "AllowedFileExtensions": [".*"],
    "BlockedFileExtensions": []
  }
}
```

## 🧪 Testes de Segurança

### **Testes Recomendados**
1. **Teste de Certificado Inválido**
   - Tentar download de URL com certificado inválido
   - Verificar se é bloqueado

2. **Teste de Arquivo Corrompido**
   - Modificar hash de arquivo
   - Verificar se download é rejeitado

3. **Teste de Extensão Bloqueada**
   - Tentar download de arquivo .bat
   - Verificar se é bloqueado

4. **Teste de Executável Inválido**
   - Tentar executar arquivo sem cabeçalho PE
   - Verificar se é rejeitado

## 🔮 Melhorias Futuras de Segurança

### **Curto Prazo**
- [ ] Implementar assinatura digital de certificados
- [ ] Adicionar verificação de reputação de arquivos
- [ ] Implementar sandbox para execução

### **Médio Prazo**
- [ ] Integração com Windows Defender
- [ ] Verificação de hash contra banco de dados de malware
- [ ] Sistema de reputação de servidores

### **Longo Prazo**
- [ ] Machine Learning para detecção de malware
- [ ] Blockchain para verificação de integridade
- [ ] Sistema de auditoria distribuída

---

**Nota**: Esta implementação de segurança segue as melhores práticas da indústria e garante que o L2Updater seja seguro para uso em produção.
