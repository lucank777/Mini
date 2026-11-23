# GitHub 雲端編譯

1. 將本專案全部檔案（包含隱藏的 `.github` 資料夾）上傳至 GitHub repository。
2. 開啟 Actions，選擇 `Build Windows x64 Single File`。
3. 按下 Run workflow，等待 Windows 2022 runner 完成建置。
4. 在執行紀錄底部下載 `MiniGameBrowser-Windows-x64` Artifact。

此流程產生 Windows x64 自包含單檔，並非 Native AOT。Windows Forms 建置關閉 trimming。目標電腦不需安裝 .NET SDK；仍需 Microsoft Edge WebView2 Runtime。

## 啟動器行為

每組固定開啟六個一般視窗。各組上方的勾選框控制第七個商人擺攤視窗：勾選 A 組商人後，開啟 A 組會依序開啟 A1～A6、A7；未勾選則只開 A1～A6。B、C 組相同。全部開啟會依 A、B、C 順序開啟所有一般視窗，並依各組商人勾選狀態決定是否開啟 A7、B7、C7。
