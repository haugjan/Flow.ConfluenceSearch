using System.Text.Json.Serialization;

public sealed class ContentSearchResponse
{
    [JsonPropertyName("results")]
    public List<ConfluenceSearchItemDto> Results { get; set; } = new();
}

public sealed class ConfluenceSearchItemDto
{
    [JsonPropertyName("content")]
    public required ConfluenceContentDto Content { get; set; }

    [JsonPropertyName("title")]
    public required string Title { get; set; }

    [JsonPropertyName("excerpt")]
    public required string Excerpt { get; set; }

    [JsonPropertyName("url")]
    public string? Url { get; set; }

    [JsonPropertyName("resultGlobalContainer")]
    public ResultGlobalContainerDto? ResultGlobalContainer { get; set; }

    public string BrowseUrl(string baseUrl) => $"{baseUrl}/wiki{Url}";

    public string SpaceKey
    {
        get
        {
            var spaceUrl = ResultGlobalContainer?.DisplayUrl;
            if (string.IsNullOrWhiteSpace(spaceUrl))
                return string.Empty;

            const string prefix = "/spaces/";
            int i = spaceUrl.IndexOf(prefix, StringComparison.OrdinalIgnoreCase);
            if (i < 0)
                return string.Empty;

            int start = i + prefix.Length;
            int end = spaceUrl.IndexOf('/', start);
            return end < 0 ? spaceUrl[start..] : spaceUrl.Substring(start, end - start);
        }
    }

    // --- NEU: Komfort-Getter für "letztes Update" ---
    [JsonIgnore]
    public string? LastUpdatedByName => Content?.History?.LastUpdated?.By?.DisplayName;

    /// <summary>
    /// Relativer Avatar-Pfad (z. B. "/wiki/aa-avatar/712020:...").
    /// </summary>
    [JsonIgnore]
    public string? LastUpdatedByAvatarPath =>
        Content?.History?.LastUpdated?.By?.ProfilePicture?.Path;

    /// <summary>
    /// Absoluter Avatar-Link bauen (optional zu verwenden).
    /// </summary>
    public string? LastUpdatedByAvatarUrl(string baseUrl) =>
        string.IsNullOrWhiteSpace(LastUpdatedByAvatarPath)
            ? null
            : $"{baseUrl}{LastUpdatedByAvatarPath}";
}

public sealed class ConfluenceContentDto
{
    [JsonPropertyName("id")]
    public required string Id { get; set; }

    // --- NEU: History mit "lastUpdated" ---
    [JsonPropertyName("history")]
    public ConfluenceHistoryDto? History { get; set; }
}

// --- NEU: unterstützende DTOs für "history.lastUpdated.by" ---
public sealed class ConfluenceHistoryDto
{
    [JsonPropertyName("lastUpdated")]
    public ConfluenceLastUpdatedDto? LastUpdated { get; set; }
}

public sealed class ConfluenceLastUpdatedDto
{
    [JsonPropertyName("by")]
    public ConfluenceUserDto? By { get; set; }
}

public sealed class ConfluenceUserDto
{
    [JsonPropertyName("displayName")]
    public string? DisplayName { get; set; }

    [JsonPropertyName("profilePicture")]
    public ConfluenceProfilePictureDto? ProfilePicture { get; set; }
}

public sealed class ConfluenceProfilePictureDto
{
    [JsonPropertyName("path")]
    public string? Path { get; set; }

    [JsonPropertyName("width")]
    public int? Width { get; set; }

    [JsonPropertyName("height")]
    public int? Height { get; set; }
}

public sealed class ResultGlobalContainerDto
{
    [JsonPropertyName("title")]
    public string? Title { get; set; }

    [JsonPropertyName("displayUrl")]
    public string? DisplayUrl { get; set; }
}
