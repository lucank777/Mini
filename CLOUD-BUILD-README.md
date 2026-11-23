# 雲端編譯（不需在自己的電腦安裝開發環境）

本專案已附 GitHub Actions 工作流程：`.github/workflows/windows-native-aot.yml`。
它會在 GitHub 提供的 Windows Server 2022 runner 上安裝 .NET 10 SDK，並以 `win-x64`、self-contained single-file 發佈，最後將 `MiniGameBrowser.exe` 上傳為 workflow artifact。

## 首次設定
1. 登入 GitHub，建立一個新的 **Private** repository。
2. 將本專案資料夾中的所有檔案上傳到 repository 根目錄（需包含隱藏的 `.github` 資料夾）。
3. 開啟 repository 的 **Actions** 頁面，選擇 **Build Windows x64 self-contained single-file**。
4. 按 **Run workflow**。也可在推送到 `main` 或 `master` 時自動執行。
5. 工作流程成功後，在該次執行頁面底部的 **Artifacts** 下載 `MiniGameBrowser-Windows-x64`，解壓縮即可取得 `MiniGameBrowser.exe`。

## 注意
- 本流程不要求在本機安裝 Visual Studio、.NET SDK 或 C++ Build Tools；編譯由 GitHub Windows runner 執行。
- 需要 GitHub 帳號及建立／上傳 repository 的一次性操作。此流程不會自動取得你的 GitHub 帳號權限，也不會替你建立 repository。
- EXE 仍需要 Microsoft Edge WebView2 Runtime 才能執行。
- 專案尚未在 Windows 實機完成編譯與執行驗證；若 workflow 失敗，請下載 build log 提供排錯。


注意：此版本使用 .NET 自包含單檔發布，而非 Native AOT。原因是 Windows Forms 不支援目前建置流程所啟用的 trimming；此設定可避免該錯誤，並仍將 .NET 執行環境隨 EXE 一起封裝。WebView2 Runtime 仍需在目標電腦安裝。
