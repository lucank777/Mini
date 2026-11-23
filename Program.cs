using MiniGameBrowser.Core;
using MiniGameBrowser.Forms;
using System.Runtime.InteropServices;

namespace MiniGameBrowser;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        Application.SetHighDpiMode(HighDpiMode.PerMonitorV2);
        ApplicationConfiguration.Initialize();
        AppPaths.Initialize();
        using var mutex = new Mutex(true, @"Local\MiniGameBrowser-Rebuild.SingleInstance", out bool created);
        if (!created)
        {
            MessageBox.Show("MiniGameBrowser 已經在執行中。", "MiniGameBrowser", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }
        Application.Run(new LauncherForm());
    }
}
