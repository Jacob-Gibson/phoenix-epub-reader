using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Web.WebView2.Core;
using Phoenix.Core.Models;
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
        // Initialize WebView2
        await ContentWebView.EnsureCoreWebView2Async();
        _webViewReady = true;
        
        // Set WebView2 default background to white
        ContentWebView.DefaultBackgroundColor = System.Drawing.Color.White;
        
        // Configure WebView2 settings
        ContentWebView.CoreWebView2.Settings.IsScriptEnabled = true;
        ContentWebView.CoreWebView2.Settings.AreDefaultContextMenusEnabled = false;
        ContentWebView.CoreWebView2.Settings.IsZoomControlEnabled = false;
        
        // Set up event handlers
        ContentWebView.CoreWebView2.WebResourceRequested += CoreWebView2_WebResourceRequested;
        ContentWebView.CoreWebView2.AddWebResourceRequestedFilter($"https://{VirtualHostName}/*", CoreWebView2WebResourceContext.Image);
        ContentWebView.CoreWebView2.WebMessageReceived += CoreWebView2_WebMessageReceived;
        ContentWebView.CoreWebView2.NavigationCompleted += CoreWebView2_NavigationCompleted;
        
        // Load initial content if available
        if (DataContext is ReaderViewModel viewModel && !string.IsNullOrEmpty(viewModel.HtmlContent))
        {
            ContentWebView.NavigateToString(viewModel.HtmlContent);
        }
    }

    private async void CoreWebView2_WebResourceRequested(object? sender, CoreWebView2WebResourceRequestedEventArgs e)
    {
        if (DataContext is not ReaderViewModel viewModel || viewModel.CurrentBook == null) return;

        var deferral = e.GetDeferral();

        try
        {
            var uri = new Uri(e.Request.Uri);
            var imagePath = uri.AbsolutePath.TrimStart('/');
            var imageData = await viewModel.GetImageAsync(imagePath);
            
            if (imageData != null)
            {
                var mimeType = GetMimeType(imagePath);
                var stream = new MemoryStream(imageData);
                
                e.Response = ContentWebView.CoreWebView2.Environment.CreateWebResourceResponse(
                    stream, 200, "OK", $"Content-Type: {mimeType}");
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

    private async void CoreWebView2_NavigationCompleted(object? sender, CoreWebView2NavigationCompletedEventArgs e)
    {
        if (!e.IsSuccess) return;
        
        try
        {
            // Inject the highlight JavaScript
            await InjectHighlightScriptAsync();
            
            // Apply existing highlights
            if (DataContext is ReaderViewModel viewModel)
            {
                var highlightsJson = viewModel.GetHighlightsJson();
                await ApplyHighlightsAsync(highlightsJson);
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[ReaderView] Error in NavigationCompleted: {ex.Message}");
        }
    }

    private async Task InjectHighlightScriptAsync()
    {
        if (!_webViewReady || ContentWebView.CoreWebView2 == null) return;

        var script = @"
            // Highlight functionality with cross-element support
            window.phoenixHighlights = {
                // Clear all existing highlights from DOM
                clearAllHighlights: function() {
                    const marks = document.querySelectorAll('mark[data-highlight-id]');
                    marks.forEach(mark => {
                        const parent = mark.parentNode;
                        while (mark.firstChild) {
                            parent.insertBefore(mark.firstChild, mark);
                        }
                        parent.removeChild(mark);
                    });
                    // Normalize to merge adjacent text nodes
                    document.body.normalize();
                },
                
                // Remove a specific highlight by ID (handles multi-segment highlights)
                removeHighlight: function(highlightId) {
                    const marks = document.querySelectorAll(`mark[data-highlight-id=""${highlightId}""]`);
                    marks.forEach(mark => {
                        const parent = mark.parentNode;
                        while (mark.firstChild) {
                            parent.insertBefore(mark.firstChild, mark);
                        }
                        parent.removeChild(mark);
                        parent.normalize();
                    });
                },
                
                // Apply a list of highlights to the document (clears existing first)
                applyHighlights: function(highlightsJson) {
                    try {
                        this.clearAllHighlights();
                        const highlights = JSON.parse(highlightsJson);
                        highlights.forEach(h => this.applyHighlight(h));
                    } catch(e) {
                        console.error('Error applying highlights:', e);
                    }
                },
                
                // Apply a single highlight (supports cross-element)
                applyHighlight: function(highlight) {
                    try {
                        if (document.querySelector(`mark[data-highlight-id=""${highlight.id}""]`)) {
                            return;
                        }
                        
                        const result = this.findTextNodes(highlight.text, highlight.textBefore, highlight.textAfter);
                        if (!result) return;
                        
                        // Wrap each text node segment
                        this.wrapTextNodes(result.segments, highlight);
                    } catch(e) {
                        console.error('Error applying highlight:', e);
                    }
                },
                
                // Create mark element with event handler
                createMarkElement: function(highlight) {
                    const mark = document.createElement('mark');
                    mark.style.backgroundColor = highlight.color || '#ffeb3b80';
                    mark.style.cursor = 'pointer';
                    mark.style.padding = '0';
                    mark.style.margin = '0';
                    mark.style.display = 'inline';
                    mark.style.borderRadius = '2px';
                    mark.dataset.highlightId = highlight.id;
                    mark.dataset.note = highlight.note || '';
                    mark.title = highlight.note || 'Click to edit';
                    
                    mark.addEventListener('click', (e) => {
                        e.stopPropagation();
                        window.chrome.webview.postMessage({
                            type: 'highlightClicked',
                            id: highlight.id,
                            note: highlight.note || ''
                        });
                    });
                    
                    return mark;
                },
                
                // Wrap text node segments with mark elements
                wrapTextNodes: function(segments, highlight) {
                    // Process in reverse order to avoid invalidating offsets
                    for (let i = segments.length - 1; i >= 0; i--) {
                        const seg = segments[i];
                        const node = seg.node;
                        const start = seg.startOffset;
                        const end = seg.endOffset;
                        
                        if (!node || !node.parentNode) continue;
                        
                        // Split the text node and wrap the middle part
                        const textContent = node.textContent || '';
                        const beforeText = textContent.substring(0, start);
                        const highlightText = textContent.substring(start, end);
                        const afterText = textContent.substring(end);
                        
                        const mark = this.createMarkElement(highlight);
                        mark.textContent = highlightText;
                        
                        const parent = node.parentNode;
                        
                        if (afterText) {
                            const afterNode = document.createTextNode(afterText);
                            parent.insertBefore(afterNode, node.nextSibling);
                        }
                        
                        parent.insertBefore(mark, node.nextSibling);
                        
                        if (beforeText) {
                            node.textContent = beforeText;
                        } else {
                            parent.removeChild(node);
                        }
                    }
                },
                
                // Find text nodes that contain the target text (supports cross-element)
                findTextNodes: function(text, textBefore, textAfter) {
                    // Handle null/undefined values
                    text = text || '';
                    textBefore = textBefore || '';
                    textAfter = textAfter || '';
                    
                    if (!text) return null;
                    
                    // Block elements that should have implicit spacing
                    const blockElements = ['P', 'DIV', 'H1', 'H2', 'H3', 'H4', 'H5', 'H6', 'LI', 'TR', 'BLOCKQUOTE', 'PRE', 'SECTION', 'ARTICLE', 'HEADER', 'FOOTER', 'BR'];
                    
                    const walker = document.createTreeWalker(
                        document.body,
                        NodeFilter.SHOW_TEXT,
                        null,
                        false
                    );
                    
                    // Collect ALL text nodes with their positions, adding spaces at block boundaries
                    const allNodes = [];
                    let rawText = '';
                    let lastNode = null;
                    
                    while (walker.nextNode()) {
                        const currentNode = walker.currentNode;
                        const nodeText = currentNode.textContent;
                        
                        // Check if we need to add a space before this node
                        if (lastNode && rawText.length > 0) {
                            // Find the common ancestor and check for block element boundaries
                            let needsSpace = false;
                            
                            // Check if this node is in a different block element than the last node
                            const lastBlock = lastNode.parentElement ? lastNode.parentElement.closest(blockElements.join(',')) : null;
                            const currentBlock = currentNode.parentElement ? currentNode.parentElement.closest(blockElements.join(',')) : null;
                            
                            if (lastBlock !== currentBlock && lastBlock && currentBlock) {
                                needsSpace = true;
                            }
                            
                            // Also check if there are block elements between the nodes
                            if (!needsSpace && lastNode.parentElement && currentNode.parentElement) {
                                let checkNode = lastNode;
                                while (checkNode && checkNode !== currentNode) {
                                    if (checkNode.nodeType === 1 && blockElements.includes(checkNode.nodeName)) {
                                        needsSpace = true;
                                        break;
                                    }
                                    checkNode = checkNode.nextSibling || (checkNode.parentNode ? checkNode.parentNode.nextSibling : null);
                                    if (checkNode === document.body || !checkNode) break;
                                }
                            }
                            
                            if (needsSpace && !rawText.endsWith(' ') && !nodeText.startsWith(' ')) {
                                rawText += ' ';
                            }
                        }
                        
                        allNodes.push({
                            node: currentNode,
                            rawStart: rawText.length,
                            rawLength: nodeText.length,
                            text: nodeText
                        });
                        rawText += nodeText;
                        lastNode = currentNode;
                    }
                    
                    // Helper to normalize text (collapse whitespace to single space)
                    const normalize = (s) => (s || '').replace(/\s+/g, ' ');
                    
                    // Normalize both the full document text and search terms
                    const normalizedRaw = normalize(rawText);
                    const normalizedTarget = normalize(text);
                    const normalizedBefore = normalize(textBefore);
                    const normalizedAfter = normalize(textAfter);
                    
                    // Try to find the text with context first
                    let searchPattern = normalizedBefore + normalizedTarget + normalizedAfter;
                    let foundIndex = normalizedRaw.indexOf(searchPattern);
                    let targetStartInNorm = foundIndex === -1 ? -1 : foundIndex + normalizedBefore.length;
                    
                    // Try without context if not found
                    if (targetStartInNorm === -1) {
                        targetStartInNorm = normalizedRaw.indexOf(normalizedTarget);
                    }
                    
                    // Try case-insensitive search
                    if (targetStartInNorm === -1) {
                        targetStartInNorm = normalizedRaw.toLowerCase().indexOf(normalizedTarget.toLowerCase());
                    }
                    
                    if (targetStartInNorm === -1) return null;
                    
                    const targetEndInNorm = targetStartInNorm + normalizedTarget.length;
                    
                    // Map normalized positions back to raw positions
                    // Walk through raw text and track which normalized position each raw position corresponds to
                    let normPos = 0;
                    let prevWasSpace = false;
                    const rawToNorm = []; // rawToNorm[rawPos] = normPos
                    
                    for (let i = 0; i < rawText.length; i++) {
                        const isSpace = /\s/.test(rawText[i]);
                        if (isSpace) {
                            if (!prevWasSpace) {
                                rawToNorm.push(normPos);
                                normPos++;
                            } else {
                                rawToNorm.push(normPos - 1); // Still part of the collapsed space
                            }
                            prevWasSpace = true;
                        } else {
                            rawToNorm.push(normPos);
                            normPos++;
                            prevWasSpace = false;
                        }
                    }
                    
                    // Find raw start: first raw position where normPos >= targetStartInNorm
                    let rawStart = 0;
                    for (let i = 0; i < rawToNorm.length; i++) {
                        if (rawToNorm[i] >= targetStartInNorm) {
                            rawStart = i;
                            break;
                        }
                    }
                    
                    // Find raw end: first raw position where normPos >= targetEndInNorm
                    let rawEnd = rawText.length;
                    for (let i = rawStart; i < rawToNorm.length; i++) {
                        if (rawToNorm[i] >= targetEndInNorm) {
                            rawEnd = i;
                            break;
                        }
                    }
                    
                    console.log('Mapped to raw positions:', rawStart, '-', rawEnd);
                    
                    // Find all text nodes that overlap with our range
                    const segments = [];
                    
                    for (const nodeInfo of allNodes) {
                        const nodeStart = nodeInfo.rawStart;
                        const nodeEnd = nodeInfo.rawStart + nodeInfo.rawLength;
                        
                        // Skip nodes before our range
                        if (nodeEnd <= rawStart) continue;
                        // Stop after our range
                        if (nodeStart >= rawEnd) break;
                        
                        // Skip whitespace-only nodes (nothing to highlight)
                        if (nodeInfo.text.trim().length === 0) continue;
                        
                        // Calculate the overlap within this node
                        const overlapStart = Math.max(rawStart, nodeStart) - nodeStart;
                        const overlapEnd = Math.min(rawEnd, nodeEnd) - nodeStart;
                        
                        if (overlapEnd > overlapStart) {
                            segments.push({
                                node: nodeInfo.node,
                                startOffset: overlapStart,
                                endOffset: overlapEnd
                            });
                        }
                    }
                    
                    if (segments.length === 0) return null;
                    
                    return { segments };
                },
                
                // Helper: Get full document text with proper spacing at block boundaries
                getDocumentText: function() {
                    const blockElements = ['P', 'DIV', 'H1', 'H2', 'H3', 'H4', 'H5', 'H6', 'LI', 'TR', 'BLOCKQUOTE', 'PRE', 'SECTION', 'ARTICLE', 'HEADER', 'FOOTER', 'BR'];
                    const walker = document.createTreeWalker(document.body, NodeFilter.SHOW_TEXT, null, false);
                    let fullText = '';
                    let lastNode = null;
                    
                    while (walker.nextNode()) {
                        const currentNode = walker.currentNode;
                        const nodeText = currentNode.textContent;
                        
                        // Add space between different block elements
                        if (lastNode && fullText.length > 0) {
                            const lastBlock = lastNode.parentElement ? lastNode.parentElement.closest(blockElements.join(',')) : null;
                            const currentBlock = currentNode.parentElement ? currentNode.parentElement.closest(blockElements.join(',')) : null;
                            
                            if (lastBlock !== currentBlock && lastBlock && currentBlock) {
                                if (!fullText.endsWith(' ') && !nodeText.startsWith(' ')) {
                                    fullText += ' ';
                                }
                            }
                        }
                        
                        fullText += nodeText;
                        lastNode = currentNode;
                    }
                    return fullText;
                },
                
                // Get selected text info for creating a highlight
                getSelection: function() {
                    const selection = window.getSelection();
                    if (!selection || selection.isCollapsed) return null;
                    
                    // Get raw selection text and normalize it (collapse whitespace, trim)
                    const rawText = selection.toString();
                    const text = rawText.replace(/\s+/g, ' ').trim();
                    if (!text) return null;
                    
                    // Get document text with proper block spacing
                    const fullText = this.getDocumentText();
                    
                    // Normalize full text
                    const normalizedFull = fullText.replace(/\s+/g, ' ');
                    const textIndex = normalizedFull.indexOf(text);
                    
                    if (textIndex === -1) {
                        return { text: text, textBefore: '', textAfter: '' };
                    }
                    
                    const textBefore = normalizedFull.substring(Math.max(0, textIndex - 50), textIndex);
                    const textAfter = normalizedFull.substring(textIndex + text.length, textIndex + text.length + 50);
                    
                    return { text, textBefore, textAfter };
                },
                
                // Diagnostic function to debug text matching
                diagnose: function(searchText) {
                    const fullText = this.getDocumentText();
                    const normalized = fullText.replace(/\s+/g, ' ');
                    const searchNorm = (searchText || '').replace(/\s+/g, ' ').trim();
                    
                    // Try different search strategies
                    const exactIndex = normalized.indexOf(searchNorm);
                    const lowerIndex = normalized.toLowerCase().indexOf(searchNorm.toLowerCase());
                    
                    // Find first 20 chars of search text
                    const first20 = searchNorm.substring(0, 20);
                    const first20Index = normalized.indexOf(first20);
                    const first20LowerIndex = normalized.toLowerCase().indexOf(first20.toLowerCase());
                    
                    return {
                        docLength: normalized.length,
                        docPreview: normalized.substring(0, 200),
                        searchLength: searchNorm.length,
                        searchPreview: searchNorm.substring(0, 100),
                        exactMatch: exactIndex,
                        caseInsensitiveMatch: lowerIndex,
                        first20Match: first20Index,
                        first20LowerMatch: first20LowerIndex,
                        foundAt: lowerIndex >= 0 ? normalized.substring(lowerIndex, lowerIndex + 100) : 'NOT FOUND'
                    };
                }
            };
            
            // Listen for right-click on text selection
            document.addEventListener('contextmenu', function(e) {
                const selection = window.phoenixHighlights.getSelection();
                if (selection) {
                    e.preventDefault(); // Prevent default context menu
                    window.chrome.webview.postMessage({
                        type: 'showContextMenu',
                        text: selection.text,
                        textBefore: selection.textBefore,
                        textAfter: selection.textAfter,
                        x: e.screenX,
                        y: e.screenY
                    });
                }
            });
            
            console.log('Phoenix highlight script loaded');
        ";

        await ContentWebView.CoreWebView2.ExecuteScriptAsync(script);
    }

    private async Task ApplyHighlightsAsync(string highlightsJson)
    {
        if (!_webViewReady || ContentWebView.CoreWebView2 == null) return;

        try
        {
            var escapedJson = JsonSerializer.Serialize(highlightsJson);
            await ContentWebView.CoreWebView2.ExecuteScriptAsync(
                $"window.phoenixHighlights.applyHighlights({escapedJson});");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[ReaderView] Error applying highlights: {ex.Message}");
        }
    }

    private void CoreWebView2_WebMessageReceived(object? sender, CoreWebView2WebMessageReceivedEventArgs e)
    {
        try
        {
            var message = e.WebMessageAsJson;
            using var doc = JsonDocument.Parse(message);
            var root = doc.RootElement;
            var messageType = root.GetProperty("type").GetString();
            
            switch (messageType)
            {
                case "showContextMenu":
                    HandleShowContextMenu(root);
                    break;
                case "highlightClicked":
                    HandleHighlightClicked(root);
                    break;
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[ReaderView] Error processing message: {ex.Message}");
        }
    }

    private void HandleShowContextMenu(JsonElement message)
    {
        var text = message.GetProperty("text").GetString() ?? "";
        var textBefore = message.GetProperty("textBefore").GetString() ?? "";
        var textAfter = message.GetProperty("textAfter").GetString() ?? "";
        
        if (string.IsNullOrWhiteSpace(text)) return;
        
        // Show context menu
        Application.Current.Dispatcher.Invoke(() =>
        {
            ShowHighlightContextMenu(text, textBefore, textAfter);
        });
    }

    private void HandleHighlightClicked(JsonElement message)
    {
        var idString = message.GetProperty("id").GetString() ?? "";
        var note = message.GetProperty("note").GetString() ?? "";
        
        if (!Guid.TryParse(idString, out var id)) return;
        
        Application.Current.Dispatcher.Invoke(() =>
        {
            ShowEditHighlightPopup(id, note);
        });
    }

    private void ShowHighlightContextMenu(string text, string textBefore, string textAfter)
    {
        if (DataContext is not ReaderViewModel viewModel) return;

        var contextMenu = new ContextMenu();

        // Highlight submenu with color options
        var highlightMenu = new MenuItem { Header = "🖍️ Highlight" };
        
        var colors = new[] 
        { 
            (HighlightColor.Yellow, "#FFEB3B", "Yellow"),
            (HighlightColor.Green, "#4CAF50", "Green"),
            (HighlightColor.Blue, "#2196F3", "Blue"),
            (HighlightColor.Pink, "#E91E63", "Pink"),
            (HighlightColor.Orange, "#FF9800", "Orange")
        };

        foreach (var (color, hex, name) in colors)
        {
            var colorItem = new MenuItem
            {
                Header = name,
                Icon = new Border
                {
                    Width = 16,
                    Height = 16,
                    Background = (System.Windows.Media.Brush)new System.Windows.Media.BrushConverter().ConvertFromString(hex)!,
                    CornerRadius = new CornerRadius(2),
                    BorderBrush = System.Windows.Media.Brushes.Gray,
                    BorderThickness = new Thickness(1)
                }
            };
            
            var capturedColor = color;
            colorItem.Click += async (s, e) =>
            {
                var highlightData = new HighlightData
                {
                    SelectedText = text,
                    TextBefore = textBefore,
                    TextAfter = textAfter,
                    Color = capturedColor
                };
                
                await viewModel.AddHighlightCommand.ExecuteAsync(highlightData);
            };
            
            highlightMenu.Items.Add(colorItem);
        }
        
        contextMenu.Items.Add(highlightMenu);

        // Add Note option
        var addNoteItem = new MenuItem { Header = "📝 Add Note..." };
        addNoteItem.Click += (s, e) =>
        {
            ShowAddNoteDialog(text, textBefore, textAfter);
        };
        contextMenu.Items.Add(addNoteItem);

        contextMenu.Items.Add(new Separator());

        // Copy text option
        var copyItem = new MenuItem { Header = "📋 Copy" };
        copyItem.Click += (s, e) =>
        {
            Clipboard.SetText(text);
        };
        contextMenu.Items.Add(copyItem);

        // Open the context menu
        contextMenu.IsOpen = true;
    }

    private void ShowAddNoteDialog(string text, string textBefore, string textAfter, HighlightColor defaultColor = HighlightColor.Yellow)
    {
        if (DataContext is not ReaderViewModel viewModel) return;

        var dialog = new Window
        {
            Title = "Add Highlight with Note",
            Width = 420,
            Height = 380,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            Owner = Window.GetWindow(this)
        };

        var panel = new StackPanel { Margin = new Thickness(15) };

        // Selected text preview
        panel.Children.Add(new TextBlock 
        { 
            Text = "Selected text:", 
            FontWeight = FontWeights.Bold,
            Margin = new Thickness(0, 0, 0, 5)
        });
        panel.Children.Add(new TextBlock 
        { 
            Text = text.Length > 100 ? text.Substring(0, 100) + "..." : text,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 0, 0, 10),
            FontStyle = FontStyles.Italic
        });

        // Color selection
        panel.Children.Add(new TextBlock { Text = "Color:", FontWeight = FontWeights.Bold, Margin = new Thickness(0, 0, 0, 5) });
        var colorPanel = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 10) };
        var selectedColor = defaultColor;
        
        var colors = new[] 
        { 
            (HighlightColor.Yellow, "#FFEB3B"),
            (HighlightColor.Green, "#4CAF50"),
            (HighlightColor.Blue, "#2196F3"),
            (HighlightColor.Pink, "#E91E63"),
            (HighlightColor.Orange, "#FF9800")
        };

        var colorButtons = new List<Button>();
        foreach (var (color, hex) in colors)
        {
            var btn = new Button
            {
                Width = 30,
                Height = 30,
                Margin = new Thickness(2),
                Background = (System.Windows.Media.Brush)new System.Windows.Media.BrushConverter().ConvertFromString(hex)!,
                BorderThickness = color == selectedColor ? new Thickness(3) : new Thickness(1),
                BorderBrush = color == selectedColor ? System.Windows.Media.Brushes.Black : System.Windows.Media.Brushes.Gray
            };
            
            btn.Click += (s, e) =>
            {
                selectedColor = color;
                foreach (var b in colorButtons)
                {
                    b.BorderThickness = new Thickness(1);
                    b.BorderBrush = System.Windows.Media.Brushes.Gray;
                }
                btn.BorderThickness = new Thickness(3);
                btn.BorderBrush = System.Windows.Media.Brushes.Black;
            };
            
            colorButtons.Add(btn);
            colorPanel.Children.Add(btn);
        }
        panel.Children.Add(colorPanel);

        // Note input
        panel.Children.Add(new TextBlock { Text = "Note (optional):", FontWeight = FontWeights.Bold, Margin = new Thickness(0, 0, 0, 5) });
        var noteBox = new TextBox 
        { 
            Height = 60, 
            TextWrapping = TextWrapping.Wrap,
            AcceptsReturn = true,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto
        };
        panel.Children.Add(noteBox);

        // Buttons
        var buttonPanel = new StackPanel 
        { 
            Orientation = Orientation.Horizontal, 
            HorizontalAlignment = HorizontalAlignment.Right,
            Margin = new Thickness(0, 15, 0, 0)
        };
        
        var saveBtn = new Button { Content = "Save", Width = 80, Margin = new Thickness(5, 0, 0, 0) };
        saveBtn.Click += async (s, e) =>
        {
            var highlightData = new HighlightData
            {
                SelectedText = text,
                TextBefore = textBefore,
                TextAfter = textAfter,
                Color = selectedColor,
                Note = noteBox.Text
            };
            
            await viewModel.AddHighlightCommand.ExecuteAsync(highlightData);
            dialog.Close();
        };
        
        var cancelBtn = new Button { Content = "Cancel", Width = 80 };
        cancelBtn.Click += (s, e) => dialog.Close();
        
        buttonPanel.Children.Add(cancelBtn);
        buttonPanel.Children.Add(saveBtn);
        panel.Children.Add(buttonPanel);

        dialog.Content = panel;
        dialog.ShowDialog();
    }

    private void ShowEditHighlightPopup(Guid highlightId, string currentNote)
    {
        if (DataContext is not ReaderViewModel viewModel) return;

        var highlight = viewModel.Highlights.FirstOrDefault(h => h.Id == highlightId);
        if (highlight == null) return;

        var dialog = new Window
        {
            Title = "Edit Highlight",
            Width = 420,
            Height = 320,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            Owner = Window.GetWindow(this)
        };

        var panel = new StackPanel { Margin = new Thickness(15) };

        // Show the highlighted text
        var previewText = highlight.SelectedText.Length > 80 
            ? highlight.SelectedText.Substring(0, 80) + "..." 
            : highlight.SelectedText;
        panel.Children.Add(new TextBlock 
        { 
            Text = $"\"{previewText}\"",
            FontStyle = FontStyles.Italic,
            TextWrapping = TextWrapping.Wrap,
            Foreground = System.Windows.Media.Brushes.Gray,
            Margin = new Thickness(0, 0, 0, 10)
        });

        // Note input
        panel.Children.Add(new TextBlock { Text = "Note:", FontWeight = FontWeights.Bold, Margin = new Thickness(0, 0, 0, 5) });
        var noteBox = new TextBox 
        { 
            Text = currentNote,
            Height = 80, 
            TextWrapping = TextWrapping.Wrap,
            AcceptsReturn = true,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto
        };
        panel.Children.Add(noteBox);

        // Buttons
        var buttonPanel = new StackPanel 
        { 
            Orientation = Orientation.Horizontal, 
            HorizontalAlignment = HorizontalAlignment.Right,
            Margin = new Thickness(0, 15, 0, 0)
        };
        
        var deleteBtn = new Button { Content = "Delete Highlight", Width = 100, Foreground = System.Windows.Media.Brushes.Red };
        deleteBtn.Click += async (s, e) =>
        {
            dialog.Close();
            
            // Delete from database and collection - NotifyHighlightsChanged will refresh DOM
            await viewModel.DeleteHighlightCommand.ExecuteAsync(highlight);
        };
        
        var saveBtn = new Button { Content = "Save", Width = 80, Margin = new Thickness(5, 0, 0, 0) };
        saveBtn.Click += async (s, e) =>
        {
            highlight.Note = noteBox.Text;
            await viewModel.UpdateHighlightCommand.ExecuteAsync(highlight);
            dialog.Close();
        };
        
        var cancelBtn = new Button { Content = "Cancel", Width = 80 };
        cancelBtn.Click += (s, e) => dialog.Close();
        
        buttonPanel.Children.Add(deleteBtn);
        buttonPanel.Children.Add(new Border { Width = 30 }); // Spacer
        buttonPanel.Children.Add(cancelBtn);
        buttonPanel.Children.Add(saveBtn);
        panel.Children.Add(buttonPanel);

        dialog.Content = panel;
        dialog.ShowDialog();
    }

    private async Task RemoveHighlightFromDomAsync(Guid highlightId)
    {
        if (!_webViewReady || ContentWebView.CoreWebView2 == null) return;

        try
        {
            var script = $"window.phoenixHighlights.removeHighlight('{highlightId}');";
            await ContentWebView.CoreWebView2.ExecuteScriptAsync(script);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[ReaderView] Error removing highlight: {ex.Message}");
        }
    }

    private void ReaderView_DataContextChanged(object sender, System.Windows.DependencyPropertyChangedEventArgs e)
    {
        
        if (e.OldValue is ReaderViewModel oldViewModel)
        {
            oldViewModel.PropertyChanged -= ViewModel_PropertyChanged;
            oldViewModel.RequestScrollPosition -= GetScrollPositionAsync;
            oldViewModel.ScrollToPositionRequested -= OnScrollToPositionRequested;
            oldViewModel.ApplyHighlightsRequested -= OnApplyHighlightsRequested;
            oldViewModel.TestJavaScriptRequested -= OnTestJavaScriptRequested;
        }
        
        if (e.NewValue is ReaderViewModel newViewModel)
        {
            newViewModel.PropertyChanged += ViewModel_PropertyChanged;
            newViewModel.RequestScrollPosition += GetScrollPositionAsync;
            newViewModel.ScrollToPositionRequested += OnScrollToPositionRequested;
            newViewModel.ApplyHighlightsRequested += OnApplyHighlightsRequested;
            newViewModel.TestJavaScriptRequested += OnTestJavaScriptRequested;
        }
    }

    private async void OnTestJavaScriptRequested(string json)
    {
        if (!_webViewReady || ContentWebView.CoreWebView2 == null)
        {
            MessageBox.Show("WebView not ready!", "Debug", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        try
        {
            // Check if phoenixHighlights exists
            var checkResult = await ContentWebView.CoreWebView2.ExecuteScriptAsync(
                "typeof window.phoenixHighlights");
            
            if (checkResult.Contains("undefined"))
            {
                MessageBox.Show("phoenixHighlights is undefined! Script not injected.", "Debug", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // Parse the JSON to get the first highlight's text for diagnosis
            string searchText = "";
            try
            {
                using var doc = JsonDocument.Parse(json);
                var arr = doc.RootElement;
                if (arr.GetArrayLength() > 0)
                {
                    searchText = arr[0].GetProperty("text").GetString() ?? "";
                }
            }
            catch { }

            // Run diagnostic on first highlight
            var diagEscaped = JsonSerializer.Serialize(searchText);
            var diagResult = await ContentWebView.CoreWebView2.ExecuteScriptAsync(
                $"JSON.stringify(window.phoenixHighlights.diagnose({diagEscaped}))");

            // Get mark count before and after applying
            var beforeCount = await ContentWebView.CoreWebView2.ExecuteScriptAsync(
                "document.querySelectorAll('mark[data-highlight-id]').length");
            
            var escapedJson = JsonSerializer.Serialize(json);
            var result = await ContentWebView.CoreWebView2.ExecuteScriptAsync(
                $"(function() {{ try {{ window.phoenixHighlights.applyHighlights({escapedJson}); return 'success'; }} catch(e) {{ return 'error: ' + e.message; }} }})()");
            
            var afterCount = await ContentWebView.CoreWebView2.ExecuteScriptAsync(
                "document.querySelectorAll('mark[data-highlight-id]').length");
            
            // Parse diagnostic for display
            string diagDisplay = FormatDiagnosticResult(diagResult);
            
            MessageBox.Show($"Result: {result}\n\nMarks before: {beforeCount}\nMarks after: {afterCount}\n\n--- DIAGNOSTIC ---\n{diagDisplay}", 
                "JavaScript Test", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error: {ex.Message}", "Debug Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private static string FormatDiagnosticResult(string diagResult)
    {
        try
        {
            using var diagDoc = JsonDocument.Parse(diagResult.Trim('"').Replace("\\\"", "\"").Replace("\\\\", "\\"));
            var diag = diagDoc.RootElement;
            var docPreview = diag.GetProperty("docPreview").GetString() ?? "";
            var searchPreview = diag.GetProperty("searchPreview").GetString() ?? "";
            
            return $"Doc length: {diag.GetProperty("docLength").GetInt32()}\n" +
                   $"Search length: {diag.GetProperty("searchLength").GetInt32()}\n" +
                   $"Exact match at: {diag.GetProperty("exactMatch").GetInt32()}\n" +
                   $"Case-insensitive at: {diag.GetProperty("caseInsensitiveMatch").GetInt32()}\n" +
                   $"First 20 chars at: {diag.GetProperty("first20Match").GetInt32()}\n\n" +
                   $"Doc preview:\n{docPreview.Substring(0, Math.Min(100, docPreview.Length))}...\n\n" +
                   $"Search preview:\n{searchPreview.Substring(0, Math.Min(60, searchPreview.Length))}...";
        }
        catch (Exception ex)
        {
            return $"Could not parse diagnostic: {ex.Message}\nRaw: {diagResult.Substring(0, Math.Min(200, diagResult.Length))}";
        }
    }

    private void OnScrollToPositionRequested(double position)
    {
        ScrollToPosition(position);
    }

    private async void OnApplyHighlightsRequested(string highlightsJson)
    {
        if (!string.IsNullOrEmpty(highlightsJson))
        {
            await ApplyHighlightsAsync(highlightsJson);
        }
    }

    private void ViewModel_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ReaderViewModel.HtmlContent) && 
            sender is ReaderViewModel viewModel)
        {
            if (_webViewReady && ContentWebView.CoreWebView2 != null && !string.IsNullOrEmpty(viewModel.HtmlContent))
            {
                ContentWebView.NavigateToString(viewModel.HtmlContent);
            }
        }
        else if (e.PropertyName == nameof(ReaderViewModel.ScrollPosition) && 
                 sender is ReaderViewModel vm)
        {
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
