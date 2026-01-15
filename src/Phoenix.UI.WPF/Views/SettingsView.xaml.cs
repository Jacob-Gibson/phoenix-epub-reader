using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
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

    private void UserControl_Loaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is SettingsViewModel viewModel)
        {
            viewModel.RefreshSettings();
        }
    }

    /// <summary>
    /// Auto-save when a setting changes via combo box.
    /// </summary>
    private async void SettingsComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (DataContext is SettingsViewModel viewModel && e.AddedItems.Count > 0)
        {
            await viewModel.SaveAndApplyAsync();
        }
    }

    /// <summary>
    /// Auto-save when a checkbox changes.
    /// </summary>
    private async void SettingsCheckBox_Changed(object sender, RoutedEventArgs e)
    {
        if (DataContext is SettingsViewModel viewModel)
        {
            await viewModel.SaveAndApplyAsync();
        }
    }

    /// <summary>
    /// Auto-save when a radio button changes.
    /// </summary>
    private async void ThemeRadioButton_Checked(object sender, RoutedEventArgs e)
    {
        if (DataContext is SettingsViewModel viewModel)
        {
            await viewModel.SaveAndApplyAsync();
        }
    }
}
