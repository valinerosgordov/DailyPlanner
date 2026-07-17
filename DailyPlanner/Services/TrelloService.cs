using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Reflection;
using System.Text.Json.Serialization;

namespace DailyPlanner.Services;

public sealed class TrelloService
{
    private static readonly HttpClient Client = CreateClient();
    private const string ApiBase = "https://api.trello.com/1";

    private static HttpClient CreateClient()
    {
        var handler = new SocketsHttpHandler
        {
            PooledConnectionLifetime = TimeSpan.FromMinutes(15),
            PooledConnectionIdleTimeout = TimeSpan.FromMinutes(5),
            AutomaticDecompression = System.Net.DecompressionMethods.All
        };
        var version = Assembly.GetExecutingAssembly().GetName().Version?.ToString(3) ?? "0.0.0";
        var client = new HttpClient(handler, disposeHandler: true)
        {
            Timeout = TimeSpan.FromSeconds(30)
        };
        client.DefaultRequestHeaders.UserAgent.ParseAdd($"DailyPlanner/{version}");
        client.DefaultRequestHeaders.AcceptEncoding.ParseAdd("gzip, deflate, br");
        return client;
    }

    /// <summary>
    /// Builds an authenticated GET request. Trello accepts both query-string
    /// auth (?key=...&amp;token=...) and an OAuth-style Authorization header.
    /// We use the header so credentials don't end up in URLs (and from there
    /// in crash logs, browser history if anything pastes the URL, server
    /// access logs, or referer headers from any redirect).
    /// </summary>
    private static HttpRequestMessage AuthorizedGet(string url, string apiKey, string token)
    {
        var req = new HttpRequestMessage(HttpMethod.Get, url);
        req.Headers.Authorization = new AuthenticationHeaderValue(
            "OAuth",
            $"oauth_consumer_key=\"{apiKey}\", oauth_token=\"{token}\"");
        return req;
    }

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
            using var req = AuthorizedGet($"{ApiBase}/members/me", apiKey, token);
            using var response = await Client.SendAsync(req, ct).ConfigureAwait(false);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    public async Task<List<TrelloBoard>> GetBoardsAsync(string apiKey, string token, CancellationToken ct = default)
    {
        using var req = AuthorizedGet($"{ApiBase}/members/me/boards?fields=name", apiKey, token);
        using var response = await Client.SendAsync(req, ct).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<List<TrelloBoard>>(ct).ConfigureAwait(false);
        // Non-nullable record fields are a compiler fiction at the JSON boundary —
        // a null Id would poison the sync dedup HashSet downstream.
        return result?.Where(b => !string.IsNullOrEmpty(b.Id) && !string.IsNullOrEmpty(b.Name)).ToList() ?? [];
    }

    public async Task<List<TrelloList>> GetListsAsync(string boardId, string apiKey, string token, CancellationToken ct = default)
    {
        using var req = AuthorizedGet($"{ApiBase}/boards/{boardId}/lists?fields=name", apiKey, token);
        using var response = await Client.SendAsync(req, ct).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<List<TrelloList>>(ct).ConfigureAwait(false);
        return result?.Where(l => !string.IsNullOrEmpty(l.Id) && !string.IsNullOrEmpty(l.Name)).ToList() ?? [];
    }

    public async Task<List<TrelloCard>> GetCardsAsync(string listId, string apiKey, string token, CancellationToken ct = default)
    {
        using var req = AuthorizedGet($"{ApiBase}/lists/{listId}/cards?fields=name,idBoard,idList,due,shortUrl", apiKey, token);
        using var response = await Client.SendAsync(req, ct).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<List<TrelloCard>>(ct).ConfigureAwait(false);
        return result?.Where(c => !string.IsNullOrEmpty(c.Id) && !string.IsNullOrEmpty(c.Name)).ToList() ?? [];
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