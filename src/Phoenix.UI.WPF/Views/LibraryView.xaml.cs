using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.Win32;
using Phoenix.Core.Models;
using Phoenix.UI.WPF.ViewModels;

namespace Phoenix.UI.WPF.Views;

/// <summary>
/// Interaction logic for LibraryView.xaml
/// </summary>
public partial class LibraryView : UserControl
{
    public LibraryView()
    {
        InitializeComponent();
    }

    private void OpenFileButton_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Filter = "ePub files (*.epub)|*.epub|All files (*.*)|*.*",
            Title = "Open ePub File"
        };

        if (dialog.ShowDialog() == true)
        {
            if (DataContext is LibraryViewModel viewModel)
            {
                var mainWindow = Window.GetWindow(this);
                if (mainWindow?.DataContext is MainViewModel mainViewModel)
                {
                    _ = mainViewModel.OpenFileCommand.ExecuteAsync(dialog.FileName);
                }
            }
        }
    }

    private void BookCard_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ClickCount == 2 && sender is FrameworkElement element && element.DataContext is Book book)
        {
            if (DataContext is LibraryViewModel viewModel)
            {
                viewModel.OpenBookCommand.Execute(book);
            }
        }
    }
}
