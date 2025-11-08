using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Flow.ConfluenceSearch.Settings;

public partial class SettingsView
{
    public SettingsView()
    {
        InitializeComponent();
    }

    private void TokenBox_PasswordChanged(object sender, RoutedEventArgs e)
    {
        if (DataContext is SettingsViewModel vm)
            vm.Settings.ApiToken = ((PasswordBox)sender).Password;
    }

    private void NumberValidationTextBox(object sender, TextCompositionEventArgs e)
    {
        e.Handled = !int.TryParse(e.Text, out _);
    }
}
