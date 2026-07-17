using CommunityToolkit.Mvvm.Input;
using DailyPlanner.Services;

namespace DailyPlanner.ViewModels;

/// <summary>
/// Trello settings + connection-test commands. The Trello-related state
/// (TrelloIsEnabled / TrelloApiKey / TrelloToken / TrelloListName / etc.)
/// stays in MainViewModel.cs because it's the same object shared with
/// settings-page bindings; only the action commands move here.
/// </summary>
public partial class MainViewModel
{
    [RelayCommand]
    private async Task SaveTrelloSettingsAsync()
    {
        var s = await _service.GetTrelloSettingsAsync();
        s.IsEnabled = TrelloIsEnabled;
        s.AutoSyncOnStartup = TrelloAutoSyncOnStartup;
        s.PushCompletions = TrelloPushCompletions;
        s.ApiKey = TrelloApiKey.Trim();
        s.Token = TrelloToken.Trim();
        s.ListName = string.IsNullOrWhiteSpace(TrelloListName) ? "В работе" : TrelloListName.Trim();
        await _service.SaveTrelloSettingsAsync(s);
        TrelloStatus = Loc.Get("TrelloSaved");
    }

    [RelayCommand]
    private async Task TestTrelloConnectionAsync()
    {
        TrelloStatus = Loc.Get("TrelloTesting");
        var ok = await _trelloService.TestConnectionAsync(TrelloApiKey.Trim(), TrelloToken.Trim());
        TrelloStatus = ok ? Loc.Get("TrelloConnectionOk") : Loc.Get("TrelloConnectionFail");
    }
}
