# Arquitetura do L2Updater

## Visão Geral

O L2Updater foi completamente refatorado seguindo princípios de arquitetura limpa, padrões modernos de desenvolvimento e boas práticas de segurança. Esta documentação descreve a nova arquitetura e como os componentes interagem.

## 🏗️ Diagrama da Arquitetura

```
┌─────────────────────────────────────────────────────────────┐
│                        Presentation Layer                   │
├─────────────────────────────────────────────────────────────┤
│  MainWindow.xaml    │  MainWindow.xaml.cs  │  MainStyleDict │
│  (View)            │  (View Code-Behind)   │  (Resources)   │
└─────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────┐
│                      ViewModel Layer                        │
├─────────────────────────────────────────────────────────────┤
│                    MainViewModel                            │
│  • UI State Management                                      │
│  • User Interaction Handling                                │
│  • Progress Reporting                                       │
│  • Command Execution                                        │
└─────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────┐
│                      Service Layer                          │
├─────────────────────────────────────────────────────────────┤
│  IUpdateService    │  ISecurityService     │  IGameService  │
│  UpdateService     │  SecurityService      │  GameService   │
│  • Update Logic    │  • Security Validation│  • Game Launch │
│  • File Download   │  • File Integrity     │  • Process Mgmt│
│  • Progress Report │  • Certificate Check  │  • Path Validation│
└─────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────┐
│                       Model Layer                           │
├─────────────────────────────────────────────────────────────┤
│  AppSettings       │  UpdateProgress       │  LocString     │
│  • Configuration   │  • Progress Tracking  │  • Localization│
│  • Settings        │  • Status Updates     │  • Multi-Lang  │
└─────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────┐
│                    Infrastructure Layer                     │
├─────────────────────────────────────────────────────────────┤
│  HttpClient        │  File System          │  Logging       │
│  • HTTP Requests   │  • File Operations    │  • Serilog     │
│  • Download Logic  │  • Path Management    │  • Structured  │
└─────────────────────────────────────────────────────────────┘
```

## 📁 Estrutura de Pastas

```
L2Updater/
├── 📄 L2Updater.csproj          # Projeto principal (.NET 6)
├── 📄 appsettings.json          # Configuração centralizada
├── 📄 App.xaml                  # Ponto de entrada da aplicação
├── 📄 App.xaml.cs               # Configuração de DI e startup
├── 📄 l2.ico                    # Ícone da aplicação
├── 📄 app.manifest              # Manifesto da aplicação
│
├── 📁 Updater/                  # Namespace principal
│   ├── 📁 Services/             # Camada de serviços
│   │   ├── 📄 IUpdateService.cs
│   │   ├── 📄 UpdateService.cs
│   │   ├── 📄 ISecurityService.cs
│   │   └── 📄 SecurityService.cs
│   │
│   ├── 📁 ViewModels/           # Camada de ViewModels
│   │   └── 📄 MainViewModel.cs
│   │
│   ├── 📁 Models/               # Camada de modelos
│   │   └── 📄 AppSettings.cs
│   │
│   ├── 📁 DataContractModels/   # Modelos legados (compatibilidade)
│   │   ├── 📄 FileModel.cs
│   │   ├── 📄 UpdateConfig.cs
│   │   └── 📄 UpdateInfoModel.cs
│   │
│   ├── 📁 UtillsClasses/        # Classes utilitárias
│   │   ├── 📄 UpdateUtills.cs
│   │   ├── 📄 FolderUtills.cs
│   │   └── 📄 DownloadUtills.cs
│   │
│   ├── 📁 Localization/         # Sistema de localização
│   │   ├── 📄 LangInfo.cs
│   │   ├── 📄 Languages.cs
│   │   └── 📄 LocString.cs
│   │
│   ├── 📁 pictures/             # Recursos visuais
│   │   ├── 📄 background.png
│   │   ├── 📄 logo.png
│   │   └── 📄 *.png
│   │
│   ├── 📄 MainWindow.xaml       # Interface principal
│   ├── 📄 MainWindow.xaml.cs    # Code-behind da interface
│   └── 📄 MainStyleDict.xaml    # Estilos e templates
│
├── 📁 docs/                     # Documentação
│   ├── 📄 CHANGELOG.md
│   ├── 📄 ARCHITECTURE.md
│   ├── 📄 SECURITY.md
│   └── 📄 DEPLOYMENT.md
│
└── 📁 logs/                     # Logs da aplicação (gerado em runtime)
    └── 📄 l2updater.log
```

## 🔧 Componentes Principais

### 1. **App.xaml.cs** - Ponto de Entrada
```csharp
public partial class App : Application
{
    private ServiceProvider? _serviceProvider;

    protected override void OnStartup(StartupEventArgs e)
    {
        // Configuração do Serilog
        Log.Logger = new LoggerConfiguration()
            .ReadFrom.Configuration(GetConfiguration())
            .CreateLogger();

        // Configuração de serviços
        var services = new ServiceCollection();
        ConfigureServices(services);
        _serviceProvider = services.BuildServiceProvider();

        // Criação da janela principal
        var mainWindow = _serviceProvider.GetRequiredService<MainWindow>();
        mainWindow.Show();
    }
}
```

**Responsabilidades:**
- Configuração do sistema de logging
- Setup da injeção de dependência
- Inicialização da aplicação
- Gerenciamento do ciclo de vida

### 2. **MainViewModel** - Lógica da Interface
```csharp
public partial class MainViewModel : ObservableObject
{
    private readonly ILogger<MainViewModel> _logger;
    private readonly IUpdateService _updateService;
    private readonly ISecurityService _securityService;
    private readonly AppSettings _settings;

    [ObservableProperty]
    private LocString _info = new("Готов к работе", "Ready to work");

    [RelayCommand]
    private async Task StartQuickUpdateAsync()
    {
        await StartUpdateAsync(UpdateTypes.Quick);
    }
}
```

**Responsabilidades:**
- Gerenciamento do estado da UI
- Execução de comandos do usuário
- Comunicação com serviços
- Atualização de propriedades observáveis

### 3. **UpdateService** - Lógica de Atualização
```csharp
public class UpdateService : IUpdateService
{
    private readonly ILogger<UpdateService> _logger;
    private readonly HttpClient _httpClient;
    private readonly ISecurityService _securityService;
    private readonly AppSettings _settings;

    public async Task<bool> DownloadAndUpdateFilesAsync(
        IEnumerable<FileModel> files, 
        string updateUrl, 
        string savePath, 
        IProgress<UpdateProgress> progress, 
        CancellationToken cancellationToken = default)
    {
        // Implementação do download paralelo
    }
}
```

**Responsabilidades:**
- Download de arquivos
- Verificação de atualizações
- Gerenciamento de progresso
- Validação de integridade

### 4. **SecurityService** - Validações de Segurança
```csharp
public class SecurityService : ISecurityService
{
    public async Task<bool> ValidateFileSignatureAsync(string filePath)
    {
        // Validação de assinatura de arquivos
    }

    public async Task<bool> ValidateFileIntegrityAsync(string filePath, string expectedHash)
    {
        // Verificação de integridade SHA256
    }
}
```

**Responsabilidades:**
- Validação de certificados SSL
- Verificação de integridade de arquivos
- Filtros de extensão
- Validação de executáveis

## 🔄 Fluxo de Dados

### 1. **Inicialização da Aplicação**
```
App.xaml.cs → ConfigureServices() → DI Container → MainWindow → MainViewModel
```

### 2. **Processo de Atualização**
```
User Click → MainViewModel → UpdateService → SecurityService → HttpClient → File System
```

### 3. **Fluxo de Progresso**
```
UpdateService → IProgress<UpdateProgress> → MainViewModel → UI Update
```

## 🎯 Padrões Utilizados

### 1. **MVVM (Model-View-ViewModel)**
- **View**: `MainWindow.xaml` - Interface do usuário
- **ViewModel**: `MainViewModel` - Lógica de apresentação
- **Model**: `AppSettings`, `UpdateProgress` - Dados e estado

### 2. **Dependency Injection**
```csharp
private void ConfigureServices(IServiceCollection services)
{
    // Configuration
    services.AddSingleton<IConfiguration>(configuration);
    services.AddSingleton(appSettings);

    // Logging
    services.AddLogging(builder => builder.AddSerilog(dispose: true));

    // HTTP Client
    services.AddHttpClient("UpdateClient", client =>
    {
        client.Timeout = TimeSpan.FromSeconds(appSettings.UpdateSettings.DownloadTimeoutSeconds);
    });

    // Services
    services.AddSingleton<ISecurityService, SecurityService>();
    services.AddSingleton<IUpdateService, UpdateService>();

    // ViewModels
    services.AddTransient<MainViewModel>();

    // Views
    services.AddTransient<MainWindow>();
}
```

### 3. **Repository Pattern** (implícito)
- `IUpdateService` atua como repositório para operações de atualização
- `ISecurityService` atua como repositório para validações de segurança

### 4. **Observer Pattern**
- `IProgress<UpdateProgress>` para notificação de progresso
- `INotifyPropertyChanged` para atualização da UI

### 5. **Command Pattern**
```csharp
[RelayCommand]
private async Task StartQuickUpdateAsync()
{
    await StartUpdateAsync(UpdateTypes.Quick);
}
```

## 🔒 Segurança na Arquitetura

### 1. **Validação em Múltiplas Camadas**
```
UI Layer → ViewModel → Service → Security Service → Infrastructure
```

### 2. **Separação de Responsabilidades**
- **SecurityService**: Apenas validações de segurança
- **UpdateService**: Apenas lógica de atualização
- **MainViewModel**: Apenas lógica de apresentação

### 3. **Configuração Segura**
```json
{
  "Security": {
    "ValidateCertificates": true,
    "RequireSignatureValidation": true,
    "AllowedFileExtensions": [".exe", ".dll", ".bin"],
    "BlockedFileExtensions": [".bat", ".cmd", ".ps1"]
  }
}
```

## ⚡ Performance na Arquitetura

### 1. **Download Paralelo**
```csharp
using var semaphore = new SemaphoreSlim(_settings.MaxConcurrentDownloads);
var tasks = files.Select(file => DownloadFileAsync(file, semaphore));
await Task.WhenAll(tasks);
```

### 2. **Async/Await Pattern**
- Todas as operações I/O são assíncronas
- UI não é bloqueada durante downloads
- Cancellation support em todas as operações

### 3. **Lazy Loading**
- Serviços são instanciados apenas quando necessário
- Configuração é carregada sob demanda

## 🧪 Testabilidade

### 1. **Interfaces para Mocking**
```csharp
public interface IUpdateService
{
    Task<UpdateInfoModel> GetUpdateInfoAsync(string updateUrl, CancellationToken cancellationToken = default);
    Task<bool> DownloadAndUpdateFilesAsync(IEnumerable<FileModel> files, string updateUrl, string savePath, IProgress<UpdateProgress> progress, CancellationToken cancellationToken = default);
}
```

### 2. **Dependency Injection**
- Facilita a substituição de implementações
- Permite injeção de mocks para testes
- Isola dependências externas

### 3. **Separação de Responsabilidades**
- Cada componente tem uma responsabilidade única
- Facilita testes unitários
- Reduz acoplamento

## 📊 Métricas da Arquitetura

| Métrica | Valor |
|---------|-------|
| **Linhas de Código** | ~2000 (vs 4000+ original) |
| **Complexidade Ciclomática** | < 10 por método |
| **Acoplamento** | Baixo (DI) |
| **Coesão** | Alta (SRP) |
| **Testabilidade** | Alta (Interfaces + DI) |
| **Manutenibilidade** | Alta (Clean Architecture) |

## 🔮 Evolução da Arquitetura

### **Futuras Melhorias**
1. **CQRS Pattern** para separar leitura e escrita
2. **Event Sourcing** para auditoria completa
3. **Microservices** para escalabilidade
4. **API Gateway** para múltiplos servidores
5. **Message Queue** para operações assíncronas

### **Extensibilidade**
- Fácil adição de novos serviços
- Plugins system para funcionalidades customizadas
- Configuração dinâmica sem recompilação
- Suporte a múltiplos formatos de configuração

---

Esta arquitetura garante que o L2Updater seja **seguro**, **performático**, **manutenível** e **escalável**, seguindo as melhores práticas de desenvolvimento moderno.
