using System.Reflection;
using Cascade.UI;
using Cascade.UI.Installer.Update;

namespace CascadeInstallerTest;

#pragma warning disable CA1812 // Instantiated via App.Run<MainView> generic constraint.

/// <summary>
/// The dogfood window: shows its own version prominently (so v1 vs v2 is visible), auto-checks for
/// updates on launch, downloads + stages any available update (delta preferred), and offers
/// restart-to-apply and roll-back — both handed off to the cascade-update shim.
/// </summary>
internal sealed class MainView : Component
{
    private readonly string version = Assembly.GetExecutingAssembly().GetName().Version?.ToString(3) ?? "0.0.0";
    private string status = "Starting up…";
    private bool updateReady;

    protected override async Task OnMounted()
    {
        // We reached a first frame — defuse crash-detection for this launch.
        Updater.MarkHealthy();
        await CheckAndDownloadAsync();
    }

    private async Task CheckAndDownloadAsync()
    {
        // NOTE: the framework awaits OnMounted with ConfigureAwait(false), so continuations after
        // an await run off the UI thread, where Invalidate() is not safe. UI state is therefore
        // published through SetStatus / Dispatcher.Post. (Framework DX gap — see INSTALL-001 notes.)
        try
        {
            SetStatus("Checking for updates…");

            UpdateCheckResult result = await Updater.CheckNowAsync(LifetimeToken);
            if (!result.IsAvailable)
            {
                SetStatus("You are on the latest version.");
                Log($"check: up to date at {version}");
                return;
            }

            SetStatus($"Downloading {result.Version} ({(result.IsDelta ? "delta" : "full package")})…");
            await Updater.DownloadAsync(LifetimeToken);

            Dispatcher.Post(() =>
            {
                updateReady = true;
                status = $"Update {result.Version} ready. Restart to apply.";
                Invalidate();
            });
            Log($"staged {result.Version} via {(Updater.StagedViaDelta ? "delta" : "full")} (running {version})");
        }
        catch (Exception ex)
        {
            SetStatus($"Update check failed: {ex.Message}");
            Log($"error: {ex.Message}");
        }
    }

    private void SetStatus(string text)
    {
        status = text;
        if (Dispatcher.IsOnUiThread)
        {
            Invalidate();
        }
        else
        {
            Dispatcher.Post(Invalidate);
        }
    }

    private void Log(string line)
    {
        try
        {
            string dir = System.IO.Path.GetDirectoryName(Environment.ProcessPath) ?? AppContext.BaseDirectory;
            System.IO.File.AppendAllText(
                System.IO.Path.Combine(dir, "update-log.txt"),
                $"{DateTimeOffset.Now:s}  {line}{Environment.NewLine}");
        }
        catch (System.IO.IOException)
        {
        }
    }

    protected override Node Render() =>
        new Center(
            new Column(
                spacing: 16,
                crossAxisAlignment: CrossAxisAlignment.Center,
                children:
                [
                    new Label("CascadeInstallerTest").FontSize(30),
                    new Label($"Version {version}").FontSize(22),
                    new Label(status).FontSize(14),
                    updateReady
                        ? new Button("Restart to apply update", () => Updater.ApplyAndRestart()).Width(260f)
                        : new Button("Check for updates", () => { _ = CheckAndDownloadAsync(); }).Width(260f),
                    Updater.CanRollback
                        ? new Button("Roll back to previous", () => Updater.RollbackAndRestart()).Width(260f)
                        : Node.Empty,
                ]
            )
        );
}
