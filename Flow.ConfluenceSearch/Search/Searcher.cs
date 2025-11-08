using Flow.ConfluenceSearch.ConfluenceClient;
using Flow.ConfluenceSearch.Settings;
using Flow.Launcher.Plugin;

namespace Flow.ConfluenceSearch.Search;

internal interface ISearcher
{
    Task<List<Result>> QueryAsync(Query query, CancellationToken cancellationToken);
}

internal sealed class Searcher(
    IConfluenceSearchClient confluenceSearch,
    IConfluenceQueryBuilder confluenceQueryBuilder,
    IResultCreator resultCreator,
    PluginSettings settings,
    PluginInitContext context
) : ISearcher
{
    public async Task<List<Result>> QueryAsync(Query query, CancellationToken cancellationToken)
    {
        context.API.LogDebug(nameof(Searcher), $"Query: {query.Search}");

        try
        {
            await Task.Delay(300, cancellationToken);
        }
        catch (TaskCanceledException)
        {
            // Typing continued → skip this request
            return new List<Result>();
        }

        using var timeoutCts = new CancellationTokenSource(
            TimeSpan.FromSeconds(Math.Max(3, settings.Timeout.TotalSeconds))
        );
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(
            cancellationToken,
            timeoutCts.Token
        );

        if (string.IsNullOrWhiteSpace(query.Search))
            return CreateHints();

        var cql = await confluenceQueryBuilder.BuildTextCql(
            query.Search,
            settings.DefaultSpaces,
            linkedCts.Token
        );
        var searchTextInBrowser = await confluenceQueryBuilder.BuildQueryForOpenInBrowser(
            query.Search,
            settings.DefaultSpaces,
            linkedCts.Token
        );
        context.API.LogDebug(nameof(Searcher), $"CQL: {cql}");
        context.API.LogDebug(nameof(Searcher), $"Text in Browser: {searchTextInBrowser}");

        return await SearchAsync(cql, searchTextInBrowser, query.Search, linkedCts.Token);
    }

    private List<Result> CreateHints()
    {
        return
        [
            resultCreator.CreateHint(
                "@me @name",
                "Search pages by contributor: me (@me) or specific person (@name)"
            ),
            resultCreator.CreateHint("/ \" . ", "Search for folders (/), blogs (\") or pages(.)"),
            resultCreator.CreateHint(
                "*",
                "Search for all types (otherwise defaults to pages & blogs)"
            ),
            resultCreator.CreateHint("+Label1", "Pages with label 'Label1'"),
        ];
    }

    private async Task<List<Result>> SearchAsync(
        string cql,
        string searchTextInBrowser,
        string originalQuery,
        CancellationToken externalCt
    )
    {
        using var timeoutCts = new CancellationTokenSource(
            TimeSpan.FromSeconds(Math.Max(3, settings.Timeout.TotalSeconds))
        );
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(
            externalCt,
            timeoutCts.Token
        );

        var data = await confluenceSearch
            .SearchCqlAsync(cql, settings.MaxResults, linkedCts.Token)
            .ConfigureAwait(false);

        var results = new List<Result>();

        foreach (var page in data.Results.Take(settings.MaxResults))
        {
            var subtitle = page
                .Excerpt.Replace("\r\n", " · ")
                .Replace("\n", " · ")
                .Replace("\r", " · ");

            subtitle =
                $"{page.LastUpdatedByName} · {page.ResultGlobalContainer?.Title} | {subtitle}";

            var url = page.BrowseUrl(settings.BaseUrl);
            results.Add(
                resultCreator.CreateResult(
                    title: System.Net.WebUtility.HtmlDecode(page.Title),
                    subtitle: System.Net.WebUtility.HtmlDecode(subtitle),
                    icon: page.LastUpdatedByAvatarUrl(settings.BaseUrl) ?? "Images/icon.png",
                    badgeIcon: "Images/icon.png",
                    key: page.Content.Id,
                    url: url,
                    cql: cql,
                    copyText: cql
                )
            );
        }

        if (data.Results.Count == 0)
            results.Add(
                resultCreator.CreateOpenInBrowserAction(
                    "No results. Open search in browser ...",
                    originalQuery,
                    searchTextInBrowser
                )
            );
        else
            results.Add(
                resultCreator.CreateOpenInBrowserAction(
                    "More results in browser ...",
                    originalQuery,
                    searchTextInBrowser
                )
            );

        return results;
    }
}
