using System.Diagnostics;
using System.Net;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Flow.ConfluenceSearch.Settings;
using Flow.Launcher.Plugin;

namespace Flow.ConfluenceSearch.Search;

internal interface IResultCreator
{
    Result CreateResult(
        string title,
        string subtitle,
        string? icon,
        string badgeIcon,
        string key,
        string url,
        string cql,
        string copyText
    );

    Result CreateHint(string title, string sub);

    Result CreateOpenInBrowserAction(
        string title,
        string originalQuery,
        string searchTextInBrowser
    );

    Result CreateAuthError();

    Result CreateApiError(HttpStatusCode? status, string message);
}

internal class ResultCreator(PluginSettings settings) : IResultCreator
{
    public Result CreateResult(
        string title,
        string subtitle,
        string? icon,
        string badgeIcon,
        string key,
        string url,
        string cql,
        string copyText
    ) =>
        new()
        {
            Title = title,
            SubTitle = subtitle,
            BadgeIcoPath = badgeIcon,
            IcoPath = icon,
            ShowBadge = true,
            Action = _ => Open(url),
            ContextData = new ResultContext(key, url, cql),
            CopyText = copyText,
        };

    public Result CreateHint(string title, string sub) =>
        new()
        {
            Title = title,
            SubTitle = sub,
            IcoPath = "Images/gray.png",
            Action = _ => false,
        };

    public Result CreateOpenInBrowserAction(
        string title,
        string originalQuery,
        string searchTextInBrowser
    ) =>
        new()
        {
            Title = title,
            SubTitle = originalQuery,
            IcoPath = "Images/icon.png",
            Action = _ =>
            {
                var url = $"{settings.BaseUrl}/wiki/search?{searchTextInBrowser}";
                return Open(url);
            },
            CopyText = $"{settings.BaseUrl}/wiki/search?{searchTextInBrowser}",
        };

    public Result CreateAuthError() =>
        new()
        {
            Title = "Confluence authentication failed (HTTP 401)",
            SubTitle =
                "Did you paste the API Token as 'your-email@example.com:apitoken'? "
                + "Click for setup help.",
            IcoPath = "Images/gray.png",
            Action = _ => Open("https://github.com/haugjan/Flow.ConfluenceSearch#configuration"),
        };

    public Result CreateApiError(HttpStatusCode? status, string message) =>
        new()
        {
            Title = status.HasValue
                ? $"Confluence request failed (HTTP {(int)status.Value} {status.Value})"
                : "Confluence request failed",
            SubTitle = message,
            IcoPath = "Images/gray.png",
            Action = _ => false,
        };

    private bool Open(string url)
    {
        Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
        return true;
    }
}
