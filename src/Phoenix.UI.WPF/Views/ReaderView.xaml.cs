using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Controls;
using Microsoft.Web.WebView2.Core;
using Phoenix.UI.WPF.ViewModels;

namespace Phoenix.UI.WPF.Views;

/// <summary>
/// Interaction logic for ReaderView.xaml
/// </summary>
public partial class ReaderView : UserControl
{
    private bool _webViewReady = false;
    private const string VirtualHostName = "epub.local";
    
    public ReaderView()
    {
        InitializeComponent();
        DataContextChanged += ReaderView_DataContextChanged;
        Loaded += ReaderView_Loaded;
    }

    private async void ReaderView_Loaded(object sender, System.Windows.RoutedEventArgs e)
    {
        Debug.WriteLine("[ReaderView] Loaded event fired");
        
        // Initialize WebView2
        await ContentWebView.EnsureCoreWebView2Async();
        _webViewReady = true;
        
        Debug.WriteLine("[ReaderView] WebView2 ready");
        
        // Set WebView2 default background to white
        ContentWebView.DefaultBackgroundColor = System.Drawing.Color.White;
        
        // Set up WebView2 settings
        ContentWebView.CoreWebView2.Settings.IsScriptEnabled = true;
        ContentWebView.CoreWebView2.Settings.AreDefaultContextMenusEnabled = false;
        ContentWebView.CoreWebView2.Settings.IsZoomControlEnabled = false;
        
        // Set up resource request handler for images
        ContentWebView.CoreWebView2.WebResourceRequested += CoreWebView2_WebResourceRequested;
        ContentWebView.CoreWebView2.AddWebResourceRequestedFilter($"https://{VirtualHostName}/*", CoreWebView2WebResourceContext.Image);
        
        // Load initial content if available
        if (DataContext is ReaderViewModel viewModel && !string.IsNullOrEmpty(viewModel.HtmlContent))
        {
            Debug.WriteLine($"[ReaderView] Loading initial content, length: {viewModel.HtmlContent.Length}");
            ContentWebView.NavigateToString(viewModel.HtmlContent);
        }
    }

    private async void CoreWebView2_WebResourceRequested(object? sender, CoreWebView2WebResourceRequestedEventArgs e)
    {
        Debug.WriteLine($"[ReaderView] WebResourceRequested: {e.Request.Uri}");
        
        if (DataContext is not ReaderViewModel viewModel || viewModel.CurrentBook == null)
        {
            Debug.WriteLine("[ReaderView] No viewmodel or book available");
            return;
        }

        // Get deferral for async work
        var deferral = e.GetDeferral();

        try
        {
            // Extract the image path from the request URL
            var uri = new Uri(e.Request.Uri);
            var imagePath = uri.AbsolutePath.TrimStart('/');
            
            Debug.WriteLine($"[ReaderView] Image requested, path: {imagePath}");
            
            // Get the image from the ePub
            var imageData = await viewModel.GetImageAsync(imagePath);
            
            Debug.WriteLine($"[ReaderView] Image data received: {imageData?.Length ?? 0} bytes");
            
            if (imageData != null)
            {
                var mimeType = GetMimeType(imagePath);
                var stream = new MemoryStream(imageData);
                
                e.Response = ContentWebView.CoreWebView2.Environment.CreateWebResourceResponse(
                    stream, 
                    200, 
                    "OK", 
                    $"Content-Type: {mimeType}");
                
                Debug.WriteLine($"[ReaderView] Response created with {mimeType}");
            }
            else
            {
                Debug.WriteLine("[ReaderView] Image not found");
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[ReaderView] Error loading image: {ex.Message}");
        }
        finally
        {
            deferral.Complete();
        }
    }

    private static string GetMimeType(string path)
    {
        var extension = Path.GetExtension(path).ToLowerInvariant();
        return extension switch
        {
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            ".gif" => "image/gif",
            ".svg" => "image/svg+xml",
            ".webp" => "image/webp",
            _ => "image/png"
        };
    }

    private void ReaderView_DataContextChanged(object sender, System.Windows.DependencyPropertyChangedEventArgs e)
    {
        Debug.WriteLine($"[ReaderView] DataContextChanged: {e.NewValue?.GetType().Name}");
        
        if (e.OldValue is ReaderViewModel oldViewModel)
        {
            oldViewModel.PropertyChanged -= ViewModel_PropertyChanged;
            oldViewModel.RequestScrollPosition -= GetScrollPositionAsync;
            oldViewModel.ScrollToPositionRequested -= OnScrollToPositionRequested;
        }
        
        if (e.NewValue is ReaderViewModel newViewModel)
        {
            newViewModel.PropertyChanged += ViewModel_PropertyChanged;
            newViewModel.RequestScrollPosition += GetScrollPositionAsync;
            newViewModel.ScrollToPositionRequested += OnScrollToPositionRequested;
        }
    }

    private void OnScrollToPositionRequested(double position)
    {
        ScrollToPosition(position);
    }

    private void ViewModel_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ReaderViewModel.HtmlContent) && 
            sender is ReaderViewModel viewModel)
        {
            Debug.WriteLine($"[ReaderView] HtmlContent changed, length: {viewModel.HtmlContent?.Length ?? 0}, WebViewReady: {_webViewReady}");
            
            if (_webViewReady && ContentWebView.CoreWebView2 != null && !string.IsNullOrEmpty(viewModel.HtmlContent))
            {
                Debug.WriteLine("[ReaderView] Calling NavigateToString");
                ContentWebView.NavigateToString(viewModel.HtmlContent);
            }
        }
        else if (e.PropertyName == nameof(ReaderViewModel.ScrollPosition) && 
                 sender is ReaderViewModel vm)
        {
            // When ScrollPosition changes, scroll the WebView to that position
            if (_webViewReady && ContentWebView.CoreWebView2 != null && vm.ScrollPosition > 0)
            {
                ScrollToPosition(vm.ScrollPosition);
            }
        }
    }

    private async void ScrollToPosition(double percentage)
    {
        if (!_webViewReady || ContentWebView.CoreWebView2 == null) return;
        
        try
        {
            // Wait a bit for the content to fully render
            await Task.Delay(100);
            
            var script = $@"
                (function() {{
                    var scrollHeight = document.body.scrollHeight - window.innerHeight;
                    var scrollTo = scrollHeight * ({percentage} / 100);
                    window.scrollTo(0, scrollTo);
                }})();
            ";
            await ContentWebView.CoreWebView2.ExecuteScriptAsync(script);
            Debug.WriteLine($"[ReaderView] Scrolled to position: {percentage}%");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[ReaderView] Error scrolling: {ex.Message}");
        }
    }

    /// <summary>
    /// Gets the current scroll position as a percentage.
    /// </summary>
    public async Task<double> GetScrollPositionAsync()
    {
        if (!_webViewReady || ContentWebView.CoreWebView2 == null) return 0;
        
        try
        {
            var script = @"
                (function() {
                    var scrollHeight = document.body.scrollHeight - window.innerHeight;
                    if (scrollHeight <= 0) return 0;
                    return (window.scrollY / scrollHeight) * 100;
                })();
            ";
            var result = await ContentWebView.CoreWebView2.ExecuteScriptAsync(script);
            if (double.TryParse(result, out var position))
            {
                return position;
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[ReaderView] Error getting scroll position: {ex.Message}");
        }
        return 0;
    }
}
