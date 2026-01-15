# Phoenix ePub Reader

An open-source ePub reader designed for personal use and built with C# and WPF.

![.NET](https://img.shields.io/badge/.NET-10.0-512BD4)
![License](https://img.shields.io/badge/license-MIT-green)

## Features

- **ePub Support** - Full support for ePub 2.0 and ePub 3.x formats
- **Library Management** - Import, organize, and browse your book collection
- **Bookmarks** - Save your favorite passages with notes
- **Reading Progress** - Track your reading progress across all books
- **Customizable Themes** - Light, Dark, and Sepia reading modes
- **Typography Controls** - Adjust font, size, line height, and margins
- **Search** - Search your library by title or author
- **Drag & Drop** - Simply drag ePub files into the app to import

## Screenshots

*Coming soon*

## Getting Started

### Prerequisites

- [.NET 10.0 SDK](https://dotnet.microsoft.com/download) or later
- Windows 10/11 (WPF application)
- Microsoft Edge WebView2 Runtime (usually pre-installed on Windows 10/11; [download here](https://developer.microsoft.com/en-us/microsoft-edge/webview2) if needed)

### Building from Source

1. Clone the repository:
   ```bash
   git clone https://github.com/Jacob-Gibson/phoenix-epub-reader.git
   cd phoenix-epub-reader
   ```

2. Restore dependencies and build:
   ```bash
   dotnet restore
   dotnet build
   ```

3. Run the application:
   ```bash
   dotnet run --project src/Phoenix.UI.WPF
   ```

**Using Visual Studio:**
Open `PhoenixEpubReader.sln` in Visual Studio and press F5 to run.

### Running Tests

```bash
dotnet test
```

## Project Structure Overview

```
phoenix-epub-reader/
├── src/
│   ├── Phoenix.Core/          # Core library (models, interfaces, services)
│   │   ├── Models/            # Data models (Book, Chapter, Bookmark, etc.)
│   │   ├── Interfaces/        # Service interfaces
│   │   └── Services/          # ePub parsing service
│   │
│   ├── Phoenix.Data/          # Data persistence layer (LiteDB)
│   │   ├── PhoenixDatabase.cs # LiteDB database context
│   │   └── Repositories/      # Repository implementations
│   │
│   └── Phoenix.UI.WPF/        # WPF desktop application
│       ├── ViewModels/        # MVVM ViewModels
│       ├── Views/             # XAML Views
│       ├── Converters/        # Value converters
│       └── Resources/         # Themes and styles
│
└── tests/
    └── Phoenix.Core.Tests/    # Unit tests
```

## Technology Stack

- **Framework**: .NET 10.0 with WPF
- **ePub Parsing**: [VersOne.Epub](https://github.com/vers-one/EpubReader)
- **HTML Rendering**: Microsoft WebView2
- **MVVM Toolkit**: [CommunityToolkit.Mvvm](https://github.com/CommunityToolkit/dotnet)
- **Database**: [LiteDB](https://www.litedb.org/) (embedded NoSQL database)

## Architecture

The application follows the **MVVM (Model-View-ViewModel)** pattern with a clean separation of concerns:

- **Phoenix.Core**: Contains all business logic, models, and interfaces with no UI dependencies
- **Phoenix.Data**: Handles data persistence using LiteDB
- **Phoenix.UI.WPF**: The WPF presentation layer

This architecture makes it easy to:
- Add new UI platforms (e.g., Avalonia for cross-platform support)
- Write unit tests for business logic
- Maintain and extend the codebase

## Configuration

User settings and library data are stored in:
```
%APPDATA%\PhoenixEpubReader\phoenix.db
```

## Contributing

Contributions are welcome! Please feel free to submit a Pull Request.

1. Fork the repository
2. Create your feature branch (`git checkout -b feature/amazing-feature`)
3. Commit your changes (`git commit -m 'Add some amazing feature'`)
4. Push to the branch (`git push origin feature/amazing-feature`)
5. Open a Pull Request

## Roadmap

- [ ] Full-text search within books
- [x] Annotations and highlights
- [ ] Export bookmarks and notes
- [ ] Reading statistics
- [ ] Keyboard shortcuts
- [ ] Text-to-speech support
- [ ] Avalonia port for cross-platform support

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.
