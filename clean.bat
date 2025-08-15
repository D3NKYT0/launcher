@echo off
echo ========================================
echo    L2Updater - Clean Script
echo ========================================
echo.

echo [1/3] Removendo executavel da raiz...
if exist "L2Updater.exe" (
    del "L2Updater.exe"
    echo Executavel removido da raiz.
) else (
    echo Nenhum executavel encontrado na raiz.
)

echo.
echo [2/3] Limpando builds...
dotnet clean
if %errorlevel% neq 0 (
    echo ERRO: Falha ao limpar o projeto
    pause
    exit /b 1
)

echo.
echo [3/3] Removendo pastas de build...
if exist "bin" (
    rmdir /s /q "bin"
    echo Pasta bin removida.
)
if exist "obj" (
    rmdir /s /q "obj"
    echo Pasta obj removida.
)

echo.
echo ========================================
echo    LIMPEZA CONCLUIDA COM SUCESSO!
echo ========================================
echo.
pause
