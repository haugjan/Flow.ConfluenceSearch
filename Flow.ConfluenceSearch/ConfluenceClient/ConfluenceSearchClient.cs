using System.Net.Http;
using System.Net.Http.Json;
using Flow.Launcher.Plugin;

namespace Flow.ConfluenceSearch.ConfluenceClient;

internal interface IConfluenceSearchClient
{
    Task<ContentSearchResponse> SearchCqlAsync(string cql, int maxResults, CancellationToken ct);
}

internal sealed class ConfluenceSearchClient(Func<HttpClient> httpFactory) : IConfluenceSearchClient
{
    public async Task<ContentSearchResponse> SearchCqlAsync(
        string cql,
        int maxResults,
        CancellationToken ct
    )
    {
        var url =
            $"/wiki/rest/api/search?cql={Uri.EscapeDataString(cql)}&limit={maxResults}&expand=content.history.lastUpdated";

        using var http = httpFactory();
        using var req = new HttpRequestMessage(HttpMethod.Get, url);
        using var resp = await http.SendAsync(req, ct).ConfigureAwait(false);

        if (!resp.IsSuccessStatusCode)
            throw new ApplicationException(
                $"Error in API Call: {await resp.Content.ReadAsStringAsync(ct)}"
            );

        return await resp
            .Content.ReadFromJsonAsync<ContentSearchResponse>(cancellationToken: ct)
            .ConfigureAwait(false);
    }
}
