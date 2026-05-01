# Flow.ConfluenceSearch

Flow Launcher plugin (C# / .NET 9, WPF) that searches Confluence Cloud via the
REST API and opens results in the browser. Triggered with the `conf` action
keyword.

For deeper docs see [`docs/ARCHITECTURE.md`](docs/ARCHITECTURE.md),
[`docs/DEVELOPMENT.md`](docs/DEVELOPMENT.md), and
[`docs/QUERY-SYNTAX.md`](docs/QUERY-SYNTAX.md).

## Repository layout

- `Flow.ConfluenceSearch/` — plugin assembly
  - `Main.cs` — entry point, implements `IAsyncPlugin`, `ISettingProvider`
  - `ServiceProvider.cs` — DI registration (`Microsoft.Extensions.DependencyInjection`)
  - `ConfluenceClient/`
    - `ConfluenceSearchClient.cs` — HTTP call to `/wiki/rest/api/search`
    - `ConfluenceQueryBuilder.cs` — parses the user query into CQL
      (for the API call) and into URL parameters (for the "open in browser"
      fallback). Uses the fluent pipeline in `QueryBuilderExtensions.cs`.
    - `ConfluenceSearchPage.cs` — DTOs for the API response
  - `Search/`
    - `Searcher.cs` — orchestrates query → CQL → API call → `Result` list,
      with a 300 ms debounce and per-call timeout
    - `ResultCreator.cs` — builds `Flow.Launcher.Plugin.Result` items, opens
      URLs via `Process.Start(... UseShellExecute=true)`
    - `ResultContext.cs` — data carried on each result
  - `Settings/` — WPF settings panel (`SettingsView.xaml(.cs)`,
    `SettingsViewModel.cs`, `PluginSettings.cs`, `Configurator.cs`)
  - `plugin.json` — Flow Launcher manifest (action keyword `conf`, ID,
    version, icon)
  - `Build-Plugin.ps1` — packages the plugin into a ZIP for manual install
  - `Start.ps1` — local dev helper: stops Flow Launcher, builds, copies the
    output into `%APPDATA%\FlowLauncher\Plugins\Confluence Search-1.0.1`,
    restarts Flow Launcher
- `Flow.ConfluenceSearch.Test/` — xUnit v3 + Shouldly + NSubstitute tests
  (the only test project referenced by the solution)
- `Flow.ConfluenceSearch.Tests/` — empty placeholder, **not** part of the
  solution; ignore it
- `.github/workflows/`
  - `build-action.yml` — PR build (`dotnet publish` win-x64)
  - `publish-action.yml` — release on push to `main`, tags `v<plugin.json
    Version>`, attaches the published ZIP

## Build & test

```powershell
dotnet restore
dotnet build Flow.ConfluenceSearch.sln -c Release
dotnet test  Flow.ConfluenceSearch.sln
```

For interactive plugin development, run `Flow.ConfluenceSearch\Start.ps1`
from the project directory — it stops Flow Launcher, rebuilds, copies the
DLLs into the plugin folder and relaunches.

For producing an installable ZIP, run `Flow.ConfluenceSearch\Build-Plugin.ps1`.

The publish workflow tags releases from the `Version` field in
`Flow.ConfluenceSearch/plugin.json` — bumping that field on `main` is what
triggers a new GitHub release.

## Query language (handled by `ConfluenceQueryBuilder`)

The user types a query after `conf`. Tokens are space-separated and
order-independent:

| Token         | Effect                                                               |
|---------------|----------------------------------------------------------------------|
| `#all`        | Ignore default-spaces filter                                         |
| `#KEY`        | Restrict to space `KEY` (repeatable)                                 |
| `@me`         | `contributor = currentUser()`                                        |
| `@name`       | `contributor.fullname ~ name` (repeatable, OR-combined with `@me`)   |
| `+label`      | `label IN (...)` (repeatable)                                        |
| `/`           | type=folder                                                          |
| `"`           | type=blogpost                                                        |
| `.`           | type=page                                                            |
| `*`           | All types (skip the default `type IN(page,blogpost)` filter)         |
| anything else | Free-text → `(title ~ "tok*" OR text ~ "tok*")`                      |

If no `#KEY` is given, `PluginSettings.DefaultSpaces` is applied. CQL is
always suffixed with `order by lastmodified DESC`.

`ConfluenceQueryBuilderTest.cs` is the canonical reference for expected CQL
strings — when changing the builder, update or add an `[InlineData]` row
there.

## Architecture notes

- DI is wired in `ServiceProvider.ConfigureServices`. `PluginInitContext` and
  `PluginSettings` are singletons; everything else is scoped.
- `HttpClient` is created per call via a `Func<HttpClient>` factory because
  `BaseUrl`, `ApiToken` and `Timeout` come from settings that the user can
  edit at runtime — capturing a single `HttpClient` would freeze stale
  values.
- Auth is HTTP Basic with the API token base64-encoded in
  `Authorization: Basic`. For Atlassian Cloud the username portion is the
  account email; the plugin currently sends only the token, which works
  because Atlassian also accepts `Bearer`-style tokens in the basic header
  for some endpoints — verify against the user's tenant if 401s appear.
- `Searcher.QueryAsync` does a 300 ms `Task.Delay` debounce, then runs the
  request under a linked CTS that combines Flow Launcher's cancellation
  token with a per-call timeout (clamped to 3–30 s).
- `internalsVisibleTo` is set for the test assembly and for Castle/DynamicProxy
  so NSubstitute can mock `internal` interfaces.

## Conventions

- Target framework: `net9.0-windows`, nullable enabled, WPF (`UseWpf`).
- Public API surface is intentionally small — most types are `internal` and
  exposed to the test project via `InternalsVisibleTo`.
- Tests use **xUnit v3** (`xunit.v3` package, `TestContext.Current.CancellationToken`),
  Shouldly for assertions, NSubstitute for mocks. Don't introduce other
  frameworks.
- Code style follows the default .NET conventions; primary constructors are
  used throughout for DI (e.g. `Searcher(...)`, `ResultCreator(...)`).
- `Build-Plugin.ps1` and `Start.ps1` derive the plugin id, version, and
  folder name from `plugin.json` at runtime — don't add hard-coded
  versions when extending those scripts.
