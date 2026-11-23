# MiniGameBrowser 重製專案

## 建置環境
- Windows 10 x64
- .NET 10 SDK x64
- Visual C++ Build Tools（MSVC x64/x86 build tools 與 Windows SDK；Native AOT 必需）
- Microsoft Edge WebView2 Runtime（執行程式時必需）

## 一鍵編譯
在 Windows 上解壓縮整個資料夾，雙擊 `一鍵編譯.bat`（僅供已安裝本機建置環境者使用）。成功後執行檔會輸出至 `publish\MiniGameBrowser.exe`。

也可以在 PowerShell 專案根目錄執行：

```powershell
dotnet restore .\MiniGameBrowser.Rebuild.csproj
dotnet publish .\MiniGameBrowser.Rebuild.csproj -c Release -r win-x64 --self-contained true -p:PublishAot=true -p:StripSymbols=true -o .\publish
```

若 Native AOT 發佈失敗，請確認已安裝 Visual C++ Build Tools 的 MSVC x64/x86 工具與 Windows 10/11 SDK。此專案目前尚未在 Windows 實機完成建置及執行測試。

## 不想安裝本機開發環境？

請依照 [`CLOUD-BUILD-README.md`](CLOUD-BUILD-README.md) 使用 GitHub Actions 雲端編譯。

## 功能範圍
- WinForms + WebView2 極簡瀏覽器
- 啟動器、收藏管理、彈出視窗
- 群組 / Slot 編輯與批次開啟（最多 6 個）
- 預設啟用前 4 個 Slot，第 5、6 個預設關閉
- 設定與收藏儲存於 `%LOCALAPPDATA%\MiniGameBrowser-Rebuild\`

## 注意
這是依原始 EXE 可確認線索所製作的重建版本，不代表已完整還原原始程式的所有內部行為。WebView2 Runtime 仍需安裝於使用者電腦。
