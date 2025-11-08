using System.Windows.Controls;

namespace Flow.ConfluenceSearch.Settings;

internal interface IConfigurator
{
    Control CreateSettingPanel();
}

public class Configurator(SettingsViewModel viewModel) : IConfigurator
{
    public Control CreateSettingPanel() => new SettingsView { DataContext = viewModel };
}
