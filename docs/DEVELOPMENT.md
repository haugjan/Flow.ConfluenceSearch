# Development

## Prerequisites

- Windows 10/11
- .NET SDK 9.0
- Flow Launcher (only required for end-to-end testing)

## Build & test

```powershell
dotnet restore
dotnet build Flow.ConfluenceSearch.sln -c Release
dotnet test  Flow.ConfluenceSearch.sln
```

The solution targets `net9.0-windows`, has nullable reference types enabled,
and uses WPF (`UseWpf=true`).

## Test stack

Tests live in `Flow.ConfluenceSearch.Test/`. The stack:

- **xUnit v3** (`xunit.v3`) — note that v3 uses
  `TestContext.Current.CancellationToken` inside `[Theory]`/`[Fact]` methods
  rather than injecting a `CancellationToken` parameter.
- **Shouldly** for assertions (`x.ShouldBe(...)`, `Should.ThrowAsync<...>`).
- **NSubstitute** for mocks. Internal interfaces are mockable thanks to the
  `InternalsVisibleTo` attributes on the production project.

When changing `ConfluenceQueryBuilder`, add or update `[InlineData]` rows on
`BuildTextCqlTest` / `BuildQueryForOpenInBrowserTest` in
`ConfluenceQueryBuilderTest.cs`. Those tables are the spec for the query
language.

## Local hot-swap loop

For interactive testing inside Flow Launcher, run from the project directory:

```powershell
.\Flow.ConfluenceSearch\Start.ps1
```

The script:

1. Stops the `Flow.Launcher` process.
2. Runs `dotnet build` (Debug by default; pass `-BuildConfig Release` to
   switch).
3. Copies `bin\Debug\net9.0-windows\*` into
   `%APPDATA%\FlowLauncher\Plugins\Confluence Search-1.0.1`.
4. Restarts `%LOCALAPPDATA%\FlowLauncher\Flow.Launcher.exe`.

> The default plugin folder name embeds the version (`1.0.1`). When the
> manifest version changes, either pass `-PluginFolderName "Confluence Search-x.y.z"`
> or update the script default — otherwise new builds keep landing in the
> old folder.

## Producing a release ZIP

```powershell
.\Flow.ConfluenceSearch\Build-Plugin.ps1
```

This builds in Release, copies the required DLLs and assets into
`dist\temp\Flow.ConfluenceSearch\`, zips them as
`Flow.ConfluenceSearch-v<version>.zip`, and prints the resulting path. The
ZIP can be installed via Flow Launcher → Settings → Plugins → Install Plugin.

## CI / release

- `.github/workflows/build-action.yml` — builds every PR via
  `dotnet publish ... -r win-x64 --no-self-contained` and uploads the result
  as an artifact.
- `.github/workflows/publish-action.yml` — on push to `main`, reads
  `Version` from `Flow.ConfluenceSearch/plugin.json`, publishes the build,
  and creates a GitHub release tagged `v<Version>` if the version differs
  from the latest existing release.

## Versioning

The single source of truth for the plugin version is the `Version` field in
`Flow.ConfluenceSearch/plugin.json`. Bump it before merging a release-worthy
change. The publish workflow tags `v<Version>` and uploads the ZIP under
that name. `Build-Plugin.ps1` and `Start.ps1` read `plugin.json` at runtime
for the version and id, so a single bump propagates everywhere.

## Code conventions

- Primary constructors are used throughout for DI (e.g.
  `Searcher(IConfluenceSearchClient ..., …) : ISearcher`).
- Most types are `internal` and exposed to tests via `InternalsVisibleTo`;
  prefer `internal` for new types unless they're genuinely part of the
  public surface.
- DTOs use `System.Text.Json` with `[JsonPropertyName(...)]`. Don't pull in
  Newtonsoft.Json.
