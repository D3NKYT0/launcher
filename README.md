# L2Updater - Launcher para Lineage 2

Um launcher moderno e eficiente para servidores de Lineage 2, desenvolvido em C# com WPF.

## 🚀 Scripts de Build

### `build.bat` - Build Completo
Executa todo o processo de build e copia o executável para a raiz:
1. Limpa builds anteriores
2. Restaura dependências
3. Compila o projeto
4. Gera executável único
5. **Copia o executável para a raiz do projeto**

**Resultado:** `L2Updater.exe` na pasta raiz

### `copy-to-root.bat` - Apenas Copiar
Copia o executável da pasta publish para a raiz (requer build prévio):
- Útil quando você já fez o build e só quer mover o arquivo
- Verifica se o executável existe antes de copiar

### `clean.bat` - Limpeza Completa
Remove todos os arquivos de build e o executável da raiz:
1. Remove executável da raiz
2. Limpa o projeto
3. Remove pastas bin e obj

## 📁 Estrutura de Arquivos

```
launcher/
├── L2Updater.exe          # Executável final (após build)
├── build.bat              # Script de build completo
├── copy-to-root.bat       # Script para copiar para raiz
├── clean.bat              # Script de limpeza
├── L2Updater.csproj       # Arquivo de projeto
├── bin/                   # Pasta de build (removida pelo clean)
│   └── Release/
│       └── net9.0-windows/
│           └── win-x64/
│               └── publish/
│                   └── L2Updater.exe  # Executável original
└── src/                   # Código fonte
```

## 🔧 Como Usar

### Build Completo
```bash
build.bat
```

### Limpeza
```bash
clean.bat
```

## ⚙️ Configurações

O projeto está configurado para gerar um executável único (`PublishSingleFile=true`) que inclui todas as dependências necessárias.

### Configurações do Projeto (.csproj)
- **Target Framework:** .NET 9.0 Windows
- **PublishSingleFile:** true
- **SelfContained:** false
- **RuntimeIdentifier:** win-x64

## 🎯 Benefícios

1. **Executável Único:** Tudo em um arquivo .exe
2. **Fácil Distribuição:** Arquivo na raiz do projeto
3. **Scripts Organizados:** Diferentes opções para diferentes necessidades
4. **Limpeza Automática:** Remove arquivos desnecessários

## 📝 Notas

- O executável final fica na raiz do projeto como `L2Updater.exe`
- A pasta `bin` contém apenas arquivos temporários de build
- Use `clean.bat` para remover todos os arquivos de build
- O executável é otimizado para Windows x64
