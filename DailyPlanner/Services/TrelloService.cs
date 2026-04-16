using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace DailyPlanner.Services;

public sealed class TrelloService
{
    private static readonly HttpClient Client = new();
    private const string ApiBase = "https://api.trello.com/1";

    public sealed record TrelloBoard(
        [property: JsonPropertyName("id")] string Id,
        [property: JsonPropertyName("name")] string Name);

    public sealed record TrelloList(
        [property: JsonPropertyName("id")] string Id,
        [property: JsonPropertyName("name")] string Name);

    public sealed record TrelloCard(
        [property: JsonPropertyName("id")] string Id,
        [property: JsonPropertyName("name")] string Name,
        [property: JsonPropertyName("idBoard")] string IdBoard,
        [property: JsonPropertyName("idList")] string IdList,
        [property: JsonPropertyName("due")] DateTime? Due,
        [property: JsonPropertyName("shortUrl")] string? ShortUrl);

    public async Task<bool> TestConnectionAsync(string apiKey, string token, CancellationToken ct = default)
    {
        try
        {
            var url = $"{ApiBase}/members/me?key={apiKey}&token={token}";
            using var response = await Client.GetAsync(url, ct).ConfigureAwait(false);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    public async Task<List<TrelloBoard>> GetBoardsAsync(string apiKey, string token, CancellationToken ct = default)
    {
        var url = $"{ApiBase}/members/me/boards?fields=name&key={apiKey}&token={token}";
        var result = await Client.GetFromJsonAsync<List<TrelloBoard>>(url, ct).ConfigureAwait(false);
        return result ?? [];
    }

    public async Task<List<TrelloList>> GetListsAsync(string boardId, string apiKey, string token, CancellationToken ct = default)
    {
        var url = $"{ApiBase}/boards/{boardId}/lists?fields=name&key={apiKey}&token={token}";
        var result = await Client.GetFromJsonAsync<List<TrelloList>>(url, ct).ConfigureAwait(false);
        return result ?? [];
    }

    public async Task<List<TrelloCard>> GetCardsAsync(string listId, string apiKey, string token, CancellationToken ct = default)
    {
        var url = $"{ApiBase}/lists/{listId}/cards?fields=name,idBoard,idList,due,shortUrl&key={apiKey}&token={token}";
        var result = await Client.GetFromJsonAsync<List<TrelloCard>>(url, ct).ConfigureAwait(false);
        return result ?? [];
    }

    public async Task<List<(TrelloCard Card, string BoardName, string ListName)>> GetCardsInListByNameAsync(
        string listName, string apiKey, string token, CancellationToken ct = default)
    {
        var boards = await GetBoardsAsync(apiKey, token, ct).ConfigureAwait(false);
        var results = new List<(TrelloCard, string, string)>();

        foreach (var board in boards)
        {
            var lists = await GetListsAsync(board.Id, apiKey, token, ct).ConfigureAwait(false);
            var matchedList = lists.FirstOrDefault(l =>
                string.Equals(l.Name, listName, StringComparison.OrdinalIgnoreCase));

            if (matchedList is null) continue;

            var cards = await GetCardsAsync(matchedList.Id, apiKey, token, ct).ConfigureAwait(false);
            foreach (var card in cards)
                results.Add((card, board.Name, matchedList.Name));
        }

        return results;
    }
}
