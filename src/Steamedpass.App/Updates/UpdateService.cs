using Serilog;
using Velopack;
using Velopack.Sources;

namespace Steamedpass.App.Updates;

internal enum UpdateApplyResult
{
    UpToDate,
    Applied,
    Failed,
}

/// <summary>
/// Best-effort wrapper around Velopack's UpdateManager, pointed at this repo's GitHub
/// Releases feed. Never throws out to callers - a failed/impossible update check (no
/// network, GitHub unreachable, or running an un-packaged dev build) must never block
/// normal app usage.
/// </summary>
internal static class UpdateService
{
    private const string RepoUrl = "https://github.com/Arcadeguy/steamedpass";

    private static UpdateManager CreateManager() =>
        new(new GithubSource(RepoUrl, accessToken: null, prerelease: false));

    /// <summary>
    /// Returns update info if a newer version is available, or null if already up to
    /// date, not running as a Velopack-installed copy (e.g. a dev/source build), or the
    /// check itself failed (logged, not thrown).
    /// </summary>
    public static async Task<UpdateInfo?> CheckAsync()
    {
        try
        {
            UpdateManager manager = CreateManager();
            if (!manager.IsInstalled)
            {
                return null;
            }

            return await manager.CheckForUpdatesAsync();
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Update check failed");
            return null;
        }
    }

    /// <summary>
    /// Downloads and applies <paramref name="update"/>, then restarts the app.
    /// Does not return on success - ApplyUpdatesAndRestart replaces the running process.
    /// Returns false (never throws) if anything fails, leaving the old version running.
    /// </summary>
    public static async Task<bool> DownloadAndApplyAsync(UpdateInfo update, Action<int>? onProgress = null)
    {
        try
        {
            UpdateManager manager = CreateManager();
            await manager.DownloadUpdatesAsync(update, onProgress);
            manager.ApplyUpdatesAndRestart(update);
            return true;
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Update download/apply failed");
            return false;
        }
    }

    /// <summary>Checks for an update and, if one is available, downloads and applies it.</summary>
    public static async Task<UpdateApplyResult> CheckAndApplyAsync(Action<int>? onProgress = null)
    {
        UpdateInfo? update = await CheckAsync();
        if (update is null)
        {
            return UpdateApplyResult.UpToDate;
        }

        return await DownloadAndApplyAsync(update, onProgress) ? UpdateApplyResult.Applied : UpdateApplyResult.Failed;
    }
}
