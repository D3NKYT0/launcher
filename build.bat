@echo off
echo ========================================
echo    L2Updater - Build Script
echo ========================================
echo.

echo [1/5] Limpando builds anteriores...
dotnet clean
if %errorlevel% neq 0 (
    echo ERRO: Falha ao limpar o projeto
    pause
    exit /b 1
)

echo.
echo [2/5] Restaurando dependencias...
dotnet restore
if %errorlevel% neq 0 (
    echo ERRO: Falha ao restaurar dependencias
    pause
    exit /b 1
)

echo.
echo [3/5] Compilando projeto...
dotnet build --configuration Release --no-restore
if %errorlevel% neq 0 (
    echo ERRO: Falha ao compilar o projeto
    pause
    exit /b 1
)

echo.
echo [4/5] Gerando executavel unico...
dotnet publish --configuration Release --runtime win-x64 --self-contained false --verbosity normal
if %errorlevel% neq 0 (
    echo ERRO: Falha ao gerar o executavel
    pause
    exit /b 1
)

echo.
echo [5/5] Copiando executavel para a raiz...
if exist "L2Updater.exe" (
    del "L2Updater.exe"
    echo Arquivo anterior removido.
)
copy "bin\Release\net9.0-windows\win-x64\publish\L2Updater.exe" "L2Updater.exe"
if %errorlevel% neq 0 (
    echo ERRO: Falha ao copiar o executavel para a raiz
    pause
    exit /b 1
)

echo.
echo ========================================
echo    BUILD CONCLUIDO COM SUCESSO!
echo ========================================
echo.
echo Arquivo executavel gerado em:
echo %cd%\L2Updater.exe
echo.
echo Tamanho do arquivo:
dir "L2Updater.exe" | find "L2Updater.exe"
echo.
echo Para executar o launcher:
echo L2Updater.exe
echo.
pause
