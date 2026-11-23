# MiniGameBrowser.exe 逆向分析紀錄

## 樣本識別

- 檔名：`MiniGameBrowser.exe`
- SHA-256：`c4c7defc39bdd820f52218178d344aafc430876e7891fa339f15fdf969969535`
- PE：PE32+ GUI，x86-64，9 sections
- FileVersion：`0.4.2.0`
- ProductVersion：`0.4.2+5864783a66db6cee3305eaffe155a1a8a09c0b58`
- Assembly：`MiniGameBrowser.dll`
- .NET runtime pack：`10.0.15`
- Framework：`Microsoft.WindowsDesktop.App`
- UI framework：`System.Windows.Forms`
- Browser control：`Microsoft.Web.WebView2` `1.0.2903.40`
- Binary is Native AOT compiled (no conventional CLR metadata directory; native code plus embedded runtime data). This limits ordinary IL decompilation.
- Manifest requests `asInvoker` and declares Windows 10/11 compatibility.

## Windows / main types recovered from symbols

| Type | Evidence / recovered responsibilities |
|---|---|
| `BrowserForm` | `InitializeWebViewAsync`, `Core_NavigationCompleted`, `RefreshCharacterNameAsync`, `ShowFavoritesMenu`, address/title update symbols, `_webView`, `_webViewReady` |
| `LauncherForm` | `OpenManyAsync`, `SlotsToOpenAll`, `SlotsToOpen`, `_slotButtons`, `_bulkButtons`, `UpdateOpenAllText` |
| `FavoritesForm` | `OpenSelected`, `DeleteSelected`, `ReloadList`, list view members |
| `PopupForm` | `InitializeAsync`, popup/WebView2 initialization |
| `Profiles` | `CreateAsync`, `ProfilesRoot` references |
| `WebViewSecurity` | `Attach`, `OpenPopup`, permission/navigation/download related event symbols |
| `JsonStore` | `Load`, `Save`, `Serialize`, `Deserialize`, `SettingsFile`, `FavoritesFile` |
| `AppPaths` | `Root`, `ProfilesRoot`, `PersonalEditionRoot`, `SettingsFile`, `FavoritesFile` |
| `UrlHelper` | `TryNormalize`, `IsWebUrl`, scheme prefix regex |

The exact pixel layout and all control labels are not recoverable from these names alone. Dynamic execution or richer UI resource inspection is needed to verify those details.

## Functional evidence

- Navigation: `Navigate`, `NavigateTo`, `ReloadPage`, `SelectedUrl`, `StartUrl`, `DefaultStartUrl`.
- Favorites: `FavoritesStore`, `FavoriteItem`, `AddCurrentToFavorites`, `ShowFavoritesMenu`, `OpenSelected`, `DeleteSelected`.
- Launcher / multi-window: `OpenManyAsync`, `SlotsToOpenAll`, `SlotsToOpen`, `SlotId`, `Group`, `Number`, `IsStallSlot`, `StallSlotNumber`, `WindowsPerGroup`, `OpenFifthWindow`.
- Popups: `OpenPopup`, `PopupForm`, `CoreWebView2NewWindowRequestedEventArgs`, `ApplyWindowFeatures`.
- Downloads: `CoreWebView2DownloadStartingEventArgs`, `ConfirmDownload`, `DownloadOperation`.
- Permissions: `CoreWebView2PermissionRequestedEventArgs`, `DescribePermission`, `CoreWebView2PermissionState`.
- Web security: `IsAllowedNavigation`, `IsTrustedGamePage`, `BlockedSchemes`, `TrustedGameHost`, `LaunchingExternalUriScheme`.
- Site-specific character-name extraction: `RefreshCharacterNameAsync`, `CharacterNameText`, and embedded JavaScript which only runs on `https://ro-idle.app` with no non-default port. The script searches visible page text around a `Base Lv` label and returns a nearby character name.
- Single-instance mutex string: `Local\MiniGameBrowser-Public.SingleInstance`.

## Data / settings evidence

Recovered exact file names:

- `settings.json`
- `favorites.json`

Recovered model/property names include:

- `SchemaVersion`, `Settings`, `Favorites`, `Windows`, `UnknownFields`
- `StartUrl`, `SelectedUrl`, `OpenFifthWindow`, `CharacterNameText`
- `X`, `Y`, `Width`, `Height`, `Maximized`, `Title`, `Url`
- `Group`, `Number`, `AddedAt`, `Root`

The original also references `Environment.SpecialFolder.LocalApplicationData`, `ProfilesRoot`, and a `Profiles` type. Exact original on-disk directory composition is not fully established by static strings alone.

## Embedded / PE resources

- PE resource tree contains RT_ICON resources (IDs 1–7), RT_GROUP_ICON, VERSIONINFO, and RT_MANIFEST.
- The icon group was reconstructed into `app.ico` in this project.
- Manifest: `asInvoker`; supported OS GUID is Windows 10+.
- Embedded strings identify WebView2 Runtime download/installer guidance and `https://developer.microsoft.com/microsoft-edge/webview2/`.

## Site / policy-specific evidence

- Default start URL string: `https://ro-idle.app/`.
- Trusted host string: `ro-idle.app`.
- The embedded character-name JavaScript explicitly checks HTTPS, hostname `ro-idle.app`, and empty port before inspecting visible text around `Base Lv`.
- The scheme block list includes `javascript`, `vbscript`, `data`, `file`, `filesystem`, `view-source`, `chrome`, `edge`, `devtools`, `ms-settings`, `ms-appinstaller`, `search-ms`, `shell`, `res`, `jar`, `ftp`, and `wss`.
- The rebuild applies conservative defaults: HTTP/HTTPS navigation only, deny web permissions by default, disable host objects, password autosave, autofill, and web messaging. These are implementation choices based on security-related evidence, not a byte-for-byte recreation of the original behavior.

## Rebuild scope implemented in this first source pass

- WinForms + WebView2 x64 project targeting `net10.0-windows`.
- Launcher, browser, favorites manager, popup forms.
- JSON persistence with temp-file replacement and unknown-field preservation.
- Basic URL normalization, multi-window opening, favorites, popup and download confirmation hooks.
- Extracted original application icon for the rebuild project.

## Known gaps / verification required

1. Native AOT means this static pass does not recover full original method bodies or exact control layout.
2. The original profile creation / per-profile WebView2 data behavior needs runtime testing and deeper metadata mapping.
3. Slot layout and exact batch-opening rules need screenshots or a running original executable to reproduce faithfully.
4. The original default WebView2 user-data folder and exact JSON schema nesting need runtime observation. The rebuild intentionally uses its own `MiniGameBrowser-Rebuild` data directory to avoid overwriting original user data.
5. The project has not been compiled in this Linux analysis environment. Final Windows build and runtime testing are still required.

## 2026-11-23：群組 / Slot 與六視窗批次規則更新

依使用者確認，重製版的批次開啟上限提高為 **6 個視窗**。啟動器已由「群組數 × 每組視窗數」的簡化計算改為明確的 Slot 清單：

- 每筆 Slot 可設定 `Group`、`Number`、`Enabled`、`Url`、`IsStallSlot`、`StallSlotNumber`。
- 批次開啟依 `Group`、`Number` 排序，只開啟已啟用的 Slot，最多開啟 6 個。
- 預設先維持四個 Slot 啟用；第 5、6 個 Slot 可在清單勾選後加入批次。
- 可新增或刪除 Slot；同一群組內的 Slot 編號不可重複。
- 舊版 `OpenFifthWindow` 設定會遷移成 Group 1 / Slot 5 的啟用狀態，並在儲存時同步回寫舊欄位以維持相容性。

**還原界線：** 原始 EXE 的靜態符號可確認存在 `SlotsToOpenAll`、`SlotsToOpen`、`_slotButtons`、`_bulkButtons`、`Group`、`Number`、`IsStallSlot`、`StallSlotNumber`、`WindowsPerGroup`、`OpenFifthWindow` 等概念，但無法僅憑符號確定原始群組數量、各群組 Slot 數、預設勾選狀態、擺攤 Slot 的具體對應規則。因此本次完成的是可配置的重製資料模型與六視窗開啟策略；預設四開、第五與第六可選是依使用者描述採用的相容性設定，不宣稱為原始 EXE 已被完全證實的預設值。

**驗證狀態：** 已更新原始碼並重新封裝 ZIP；執行環境沒有 .NET SDK，尚未能編譯或在 Windows 上實際執行驗證。
