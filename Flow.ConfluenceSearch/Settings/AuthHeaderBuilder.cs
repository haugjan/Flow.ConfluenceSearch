using System.Text;

namespace Flow.ConfluenceSearch.Settings;

internal static class AuthHeaderBuilder
{
    public static string Build(string? email, string? apiToken)
    {
        var trimmedEmail = email?.Trim() ?? string.Empty;
        var trimmedToken = apiToken?.Trim() ?? string.Empty;

        var credential = trimmedEmail.Length > 0
            ? $"{trimmedEmail}:{trimmedToken}"
            : trimmedToken;

        return Convert.ToBase64String(Encoding.UTF8.GetBytes(credential));
    }
}
