# Architecture

Flow.ConfluenceSearch is a [Flow Launcher](https://flowlauncher.com/) plugin
written in C# / .NET 9 (WPF). It searches Confluence Cloud through the REST
API and is invoked with the `conf` action keyword.

## Solution layout

```
Flow.ConfluenceSearch.sln
├── Flow.ConfluenceSearch/           Plugin assembly
│   ├── Main.cs                      Entry point (IAsyncPlugin, ISettingProvider)
│   ├── ServiceProvider.cs           DI container wiring
│   ├── plugin.json                  Flow Launcher manifest
│   ├── ConfluenceClient/            HTTP + CQL builder
│   │   ├── ConfluenceSearchClient.cs
│   │   ├── ConfluenceQueryBuilder.cs
│   │   ├── QueryBuilderExtensions.cs
│   │   └── ConfluenceSearchPage.cs  Response DTOs
│   ├── Search/
│   │   ├── Searcher.cs              Orchestrates query → CQL → results
│   │   ├── ResultCreator.cs         Builds Flow Launcher Result items
│   │   └── ResultContext.cs
│   ├── Settings/                    WPF settings panel
│   │   ├── SettingsView.xaml(.cs)
│   │   ├── SettingsViewModel.cs
│   │   ├── PluginSettings.cs
│   │   └── Configurator.cs
│   ├── Build-Plugin.ps1             Packages plugin into a ZIP
│   └── Start.ps1                    Local hot-swap dev script
└── Flow.ConfluenceSearch.Test/      xUnit v3 + Shouldly + NSubstitute
```

> The folder `Flow.ConfluenceSearch.Tests/` (with an `s`) sits next to the
> real test project, contains an empty file, and is **not** referenced by the
> solution. It can be removed.

## Request lifecycle

1. Flow Launcher invokes `Main.QueryAsync(query, ct)`.
2. `Searcher.QueryAsync`
   - waits 300 ms (debounce — typing further cancels via `TaskCanceledException`)
   - links the caller's cancellation token with a per-call timeout
     (`PluginSettings.Timeout`, clamped to 3–30 s)
   - if the query is empty, returns the static "hint" results
3. `ConfluenceQueryBuilder.BuildTextCql` parses the user input into CQL.
   `BuildQueryForOpenInBrowser` produces a fallback URL for the
   "open search in browser" action.
4. `ConfluenceSearchClient.SearchCqlAsync` calls
   `GET /wiki/rest/api/search?cql=…&limit=…&expand=content.history.lastUpdated`
   and deserializes `ContentSearchResponse`.
5. `ResultCreator.CreateResult` wraps each hit as a Flow Launcher `Result`.
   Activating a result opens its URL via `Process.Start(... UseShellExecute=true)`.

## Dependency injection

`ServiceProvider.ConfigureServices` (called from `Main.InitAsync`) registers:

| Lifetime  | Registration |
|-----------|--------------|
| Singleton | `PluginInitContext`, `PluginSettings` |
| Singleton | `Func<HttpClient>` factory (see below) |
| Scoped    | `IConfluenceSearchClient`, `IConfluenceQueryBuilder`, `IResultCreator`, `IConfigurator`, `ISearcher`, `SettingsViewModel` |

### Why a `Func<HttpClient>`?

`BaseUrl`, `ApiToken`, and `Timeout` come from settings the user can edit at
runtime. A captured singleton `HttpClient` would freeze stale values, so the
factory creates a fresh client per call, configured with the current
settings. Each created client is `using`-disposed after the request.

Authentication uses HTTP Basic with the API token base64-encoded in the
`Authorization` header.

## Visibility & testability

Most types in the plugin assembly are `internal`. The csproj exposes them to
the test assembly and to NSubstitute via `InternalsVisibleTo`:

- `Flow.ConfluenceSearch.Test`
- `Castle.Core`
- `DynamicProxyGenAssembly2`

This lets tests substitute `internal` interfaces (e.g. `IConfluenceSearchClient`)
without making them public.

## Settings

`PluginSettings` is loaded via Flow Launcher's `LoadSettingJsonStorage<T>()`
and contains:

| Field            | Default                   |
|------------------|---------------------------|
| `BaseUrl`        | `"https://www.example.com"` |
| `ApiToken`       | `""`                      |
| `Timeout`        | `00:00:10`                |
| `MaxResults`     | `10`                      |
| `DefaultSpaces`  | `[]`                      |

The settings UI (`SettingsView.xaml`) binds directly to `Settings.*`. The
API token uses a `PasswordBox` whose `PasswordChanged` handler writes to
`Settings.ApiToken` in the code-behind (passwords cannot be two-way-bound
in WPF without unsafe workarounds).
