using System.Reflection;
using Cascade.UI;
using Cascade.UI.Backend.Etch;
using Cascade.UI.Installer.Update;
using CascadeInstallerTest;
using IOPath = System.IO.Path;

// The install directory is wherever this exe lives; the updater swaps files here and the shim
// (cascade-update) relaunches us from here.
string installDir = IOPath.GetDirectoryName(Environment.ProcessPath) ?? AppContext.BaseDirectory;
string version = Assembly.GetExecutingAssembly().GetName().Version?.ToString(3) ?? "0.0.0";

Updater.Configure(
    new UpdateConfig
    {
        ManifestUrl = "https://github.com/Echostorm44/CascadeInstallerTest/releases/latest/download/manifest.json",
    },
    currentVersion: version,
    rid: "win-x64",
    installDirectory: installDir,
    applicationExePath: Environment.ProcessPath ?? IOPath.Combine(installDir, "CascadeInstallerTest.exe"));

MainView.Log($"boot: installDir={installDir} version={version} pid={Environment.ProcessId}");

App.Run<MainView>(config =>
{
    config.UseEtch();
    // The theme alternates each release so an update/rollback is visually obvious
    // (v1 Dark, v2 Light, v3 Dark, …). The version label also shows the exact version.
    config.Theme = new AppleTheme(ThemeMode.Dark);
});
