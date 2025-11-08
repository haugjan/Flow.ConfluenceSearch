namespace Flow.ConfluenceSearch.Settings;

public sealed class SettingsViewModel(PluginSettings settings)
{
    public PluginSettings Settings { get; } = settings;

    public string DefaultSpaces
    {
        get => string.Join(",", Settings.DefaultSpaces);
        set => Settings.DefaultSpaces = value.Split(",", StringSplitOptions.RemoveEmptyEntries).ToList();
    }
}
