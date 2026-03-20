using System.Diagnostics;
using Velopack;
using Velopack.Sources;

namespace DailyPlanner.Services;

public sealed class UpdateService
{
    private readonly UpdateManager _manager;

    public UpdateService(string githubRepoUrl)
    {
        _manager = new UpdateManager(new GithubSource(githubRepoUrl, null, false));
    }

    public bool IsInstalled => _manager.IsInstalled;

    public async Task<UpdateInfo?> CheckForUpdatesAsync(CancellationToken ct = default)
    {
        try
        {
            return await _manager.CheckForUpdatesAsync();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[UpdateService] Check failed: {ex.Message}");
            return null;
        }
    }

    public async Task DownloadAndApplyAsync(UpdateInfo update, Action<int>? progress = null, CancellationToken ct = default)
    {
        await _manager.DownloadUpdatesAsync(update, progress);
        _manager.ApplyUpdatesAndRestart(update);
    }
}
