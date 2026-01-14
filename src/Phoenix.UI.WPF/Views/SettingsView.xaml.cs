using System.Windows.Controls;
using Phoenix.UI.WPF.ViewModels;

namespace Phoenix.UI.WPF.Views;

/// <summary>
/// Interaction logic for SettingsView.xaml
/// </summary>
public partial class SettingsView : UserControl
{
    public SettingsView()
    {
        InitializeComponent();
    }

    private async void UserControl_Loaded(object sender, System.Windows.RoutedEventArgs e)
    {
        if (DataContext is SettingsViewModel viewModel)
        {
            await viewModel.LoadSettingsAsync();
        }
    }
}
