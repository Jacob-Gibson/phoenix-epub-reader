using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Phoenix.UI.WPF.ViewModels;

namespace Phoenix.UI.WPF;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    private async void Window_Drop(object sender, DragEventArgs e)
    {
        if (e.Data.GetDataPresent(DataFormats.FileDrop))
        {
            var files = (string[])e.Data.GetData(DataFormats.FileDrop);
            var epubFile = files?.FirstOrDefault(f => f.EndsWith(".epub", System.StringComparison.OrdinalIgnoreCase));
            
            if (epubFile != null && DataContext is MainViewModel viewModel)
            {
                await viewModel.OpenFileCommand.ExecuteAsync(epubFile);
            }
        }
    }

    private void Window_DragOver(object sender, DragEventArgs e)
    {
        if (e.Data.GetDataPresent(DataFormats.FileDrop))
        {
            var files = (string[])e.Data.GetData(DataFormats.FileDrop);
            var hasEpub = files?.Any(f => f.EndsWith(".epub", System.StringComparison.OrdinalIgnoreCase)) ?? false;
            
            e.Effects = hasEpub ? DragDropEffects.Copy : DragDropEffects.None;
        }
        else
        {
            e.Effects = DragDropEffects.None;
        }
        e.Handled = true;
    }

    private void SettingsComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        // Notify that settings changed to refresh reader content
        if (DataContext is MainViewModel viewModel && e.AddedItems.Count > 0)
        {
            viewModel.OnSettingsChanged();
        }
    }

    private void CloseSettingsPopup(object sender, RoutedEventArgs e)
    {
        // Close the settings popup when navigating to full settings
        SettingsToggle.IsChecked = false;
    }
}