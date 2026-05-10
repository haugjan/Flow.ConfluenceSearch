using System.Net.Http;
using Flow.ConfluenceSearch.ConfluenceClient;
using Flow.ConfluenceSearch.Search;
using Flow.ConfluenceSearch.Settings;
using Flow.Launcher.Plugin;
using Microsoft.Extensions.DependencyInjection;

namespace Flow.ConfluenceSearch;

public static class ServiceProvider
{
    public static void ConfigureServices(
        this ServiceCollection serviceCollection,
        PluginInitContext context,
        PluginSettings settings
    )
    {
        serviceCollection.AddSingleton(context);
        serviceCollection.AddSingleton(settings);
        serviceCollection.AddSingleton<Func<HttpClient>>(_ =>
        {
            return () =>
            {
                var config = settings;
                var httpClient = new HttpClient
                {
                    BaseAddress = new Uri($"{config.BaseUrl}/"),
                    Timeout = TimeSpan.FromSeconds(Math.Clamp(config.Timeout.TotalSeconds, 3, 30)),
                };
                var basic = AuthHeaderBuilder.Build(config.Email, config.ApiToken);
                httpClient.DefaultRequestHeaders.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", basic);
                httpClient.DefaultRequestHeaders.Accept.ParseAdd("application/json");
                return httpClient;
            };
        });
        serviceCollection.AddScoped<IConfluenceSearchClient, ConfluenceSearchClient>();
        serviceCollection.AddScoped<IConfluenceQueryBuilder, ConfluenceQueryBuilder>();
        serviceCollection.AddScoped<IResultCreator, ResultCreator>();
        serviceCollection.AddScoped<IConfigurator, Configurator>();
        serviceCollection.AddScoped<ISearcher, Searcher>();
        serviceCollection.AddScoped<SettingsViewModel>();
    }
}
