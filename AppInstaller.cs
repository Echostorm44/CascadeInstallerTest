using Cascade.UI.Installer;

namespace CascadeInstallerTest;

#pragma warning disable CA1812 // Discovered via the [Installer] attribute by the packaging tooling.

/// <summary>Installer declaration for the dogfood app — installs to LocalAppData with desktop/start shortcuts.</summary>
[Installer]
internal sealed class AppInstaller : CascadeInstaller
{
    public override InstallerConfig Configure() => new()
    {
        AppId = "B7E3C1A4-9F2D-4E60-8A11-CC0FE2D3A456",
        AppName = "CascadeInstallerTest",
        Version = "1.0.0",
        Publisher = "Cascade UI",
        InstallDir = InstallDir.LocalAppData("CascadeInstallerTest"),
        Output = "CascadeInstallerTest-Setup",
    };

    public override IReadOnlyList<InstallFile> Files =>
    [
        InstallFile.Directory("publish/*", dest: Dir.App, recursive: true),
    ];

    public override IReadOnlyList<Shortcut> Shortcuts =>
    [
        new Shortcut
        {
            Name = "CascadeInstallerTest",
            TargetPath = "CascadeInstallerTest.exe",
            Location = ShortcutLocation.Desktop,
        },
    ];
}
