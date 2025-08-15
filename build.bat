@echo off
echo ========================================
echo    L2Updater - Build Script
echo ========================================
echo.

echo [1/4] Limpando builds anteriores...
dotnet clean
if %errorlevel% neq 0 (
    echo ERRO: Falha ao limpar o projeto
    pause
    exit /b 1
)

echo.
echo [2/4] Restaurando dependencias...
dotnet restore
if %errorlevel% neq 0 (
    echo ERRO: Falha ao restaurar dependencias
    pause
    exit /b 1
)

echo.
echo [3/4] Compilando projeto...
dotnet build --configuration Release --no-restore
if %errorlevel% neq 0 (
    echo ERRO: Falha ao compilar o projeto
    pause
    exit /b 1
)

echo.
echo [4/4] Gerando executavel unico...
dotnet publish --configuration Release --runtime win-x64 --self-contained false --output "publish" --verbosity normal
if %errorlevel% neq 0 (
    echo ERRO: Falha ao gerar o executavel
    pause
    exit /b 1
)

echo.
echo ========================================
echo    BUILD CONCLUIDO COM SUCESSO!
echo ========================================
echo.
echo Arquivo executavel gerado em:
echo %cd%\publish\L2Updater.exe
echo.
echo Tamanho do arquivo:
dir "publish\L2Updater.exe" | find "L2Updater.exe"
echo.
echo Para executar o launcher:
echo publish\L2Updater.exe
echo.
pause
