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
        Log($"OnMounted: entry (thread={Environment.CurrentManagedThreadId})");
        try
        {
            // We reached a first frame — defuse crash-detection for this launch.
            Updater.MarkHealthy();
            Log("OnMounted: marked healthy, starting check");
            await CheckAndDownloadAsync();
            Log("OnMounted: returned");
        }
        catch (Exception ex)
        {
            Log($"OnMounted: EXCEPTION {ex.GetType().Name}: {ex.Message}");
        }
    }

    private async Task CheckAndDownloadAsync()
    {
        // The framework installs a UI SynchronizationContext, so continuations after each await
        // resume on the UI thread — plain field mutation + Invalidate() is all that is needed.
        try
        {
            status = "Checking for updates…";
            Invalidate();
            Log("check: starting");

            UpdateCheckResult result = await Updater.CheckNowAsync(LifetimeToken);
            Log($"check: returned available={result.IsAvailable} version={result.Version} isDelta={result.IsDelta} reason={result.Reason}");
            if (!result.IsAvailable)
            {
                status = "You are on the latest version.";
                Invalidate();
                Log($"check: up to date at {version}");
                return;
            }

            status = $"Downloading {result.Version} ({(result.IsDelta ? "delta" : "full package")})…";
            Invalidate();
            Log("download: starting");

            await Updater.DownloadAsync(LifetimeToken);
            Log($"download: returned state={Updater.State} staged={Updater.HasStagedUpdate} viaDelta={Updater.StagedViaDelta}");

            updateReady = true;
            status = $"Update {result.Version} ready. Restart to apply.";
            Invalidate();
            Log($"staged {result.Version} via {(Updater.StagedViaDelta ? "delta" : "full")} (running {version})");
        }
        catch (Exception ex)
        {
            status = $"Update check failed: {ex.Message}";
            Invalidate();
            Log($"error: {ex.GetType().Name}: {ex.Message}");
        }
    }

    internal static void Log(string line)
    {
        try
        {
            string dir = System.IO.Path.GetDirectoryName(Environment.ProcessPath) ?? AppContext.BaseDirectory;
            System.IO.File.AppendAllText(
                System.IO.Path.Combine(dir, "update-log.txt"),
                $"{DateTimeOffset.Now:HH:mm:ss.fff}  {line}{Environment.NewLine}");
        }
        catch (Exception)
        {
        }
        Console.Error.WriteLine($"[update] {line}");
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
