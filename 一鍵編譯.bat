@echo off
setlocal
chcp 65001 >nul
cd /d "%~dp0"
title MiniGameBrowser - Build

echo ==============================================
echo  MiniGameBrowser - Windows x64 Native AOT Build
echo ==============================================
echo.

where dotnet >nul 2>nul
if errorlevel 1 (
  echo [錯誤] 找不到 dotnet。請先安裝 .NET 10 SDK x64。
  echo https://dotnet.microsoft.com/zh-tw/download/dotnet/10.0
  pause
  exit /b 1
)

for /f "tokens=1" %%v in ('dotnet --version') do set SDK=%%v
echo 偵測到 .NET SDK：%SDK%
echo.

if not exist "MiniGameBrowser.Rebuild.csproj" (
  echo [錯誤] 找不到 MiniGameBrowser.Rebuild.csproj，請確認此 BAT 位於專案根目錄。
  pause
  exit /b 1
)

echo [1/3] 還原 NuGet 套件...
dotnet restore "MiniGameBrowser.Rebuild.csproj"
if errorlevel 1 goto :failed

echo.
echo [2/3] 編譯並發佈 Windows x64 Native AOT...
dotnet publish "MiniGameBrowser.Rebuild.csproj" -c Release -r win-x64 --self-contained true -p:PublishAot=true -p:StripSymbols=true -o "%~dp0publish"
if errorlevel 1 goto :failed

echo.
echo [3/3] 完成。
if exist "%~dp0publish\MiniGameBrowser.exe" (
  echo 成品位置：%~dp0publish\MiniGameBrowser.exe
  echo 請注意：執行時仍需安裝 Microsoft Edge WebView2 Runtime。
  explorer "%~dp0publish"
  pause
  exit /b 0
)

echo [錯誤] publish 資料夾中沒有找到預期的 EXE。
goto :failed

:failed
echo.
echo [失敗] 編譯未完成。請將上方完整錯誤訊息複製給 ChatGPT 協助排除。
pause
exit /b 1
