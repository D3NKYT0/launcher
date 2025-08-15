# L2Updater

Launcher e atualizador para servidor Lineage 2.

## O que é

Aplicação WPF que gerencia downloads, atualizações e execução do cliente Lineage 2.

## Funcionalidades

- ✅ Download automático de patches
- ✅ Verificação de integridade de arquivos (CRC32)
- ✅ Atualização automática do próprio launcher
- ✅ Interface multilíngue (Russo/Inglês)
- ✅ Links para redes sociais e recursos do servidor
- ✅ Logs detalhados de operações
- ✅ Validação de segurança de arquivos

## Requisitos

- Windows 10/11
- .NET 9.0 Runtime
- Conexão com internet

## Instalação

1. Baixe o executável
2. Execute `L2Updater.exe`
3. Configure o caminho do jogo na primeira execução

## Configuração

O launcher agora usa configurações embutidas no código, eliminando a necessidade do arquivo `appsettings.json`. 

### Configurações Padrão

As configurações padrão estão embutidas no código e incluem:
- URLs do servidor PDL
- Configurações de download e atualização
- Links para redes sociais
- Configurações de segurança

### Configuração Personalizada (Opcional)

Para personalizar as configurações, você pode criar um arquivo `launcher_config.json` no mesmo diretório do executável:

```json
{
  "UpdateSettings": {
    "UpdateUrl": "https://seu-servidor.com/update/",
    "GameStartPath": "system",
    "MaxRetryAttempts": 3
  },
  "Links": {
    "Site": "https://seu-servidor.com",
    "Discord": "https://discord.gg/seuserver"
  }
}
```

Se o arquivo não existir, o launcher usará as configurações embutidas automaticamente.

**Exemplo de configuração:** Veja o arquivo `launcher_config_example.json` para um exemplo completo de como personalizar todas as configurações.

## Estrutura do Projeto

```
L2Updater/
├── Updater/
│   ├── ViewModels/     # MVVM ViewModels
│   ├── Services/       # Lógica de negócio
│   ├── Models/         # Modelos de dados
│   ├── HashZip/        # Biblioteca de compressão
│   ├── HashCalc/       # Cálculo de hash
│   └── Localization/   # Arquivos de idioma
├── App.xaml.cs         # Ponto de entrada
└── launcher_config.json # Configurações opcionais
```

## Build

```bash
dotnet restore
dotnet build
dotnet run
```

## Tecnologias

- **.NET 9.0** - Framework
- **WPF** - Interface gráfica
- **MVVM** - Padrão de arquitetura
- **Serilog** - Logging
- **Microsoft.Extensions** - Injeção de dependência

## Licença

Proprietário - Todos os direitos reservados.
