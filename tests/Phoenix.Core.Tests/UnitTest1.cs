using Phoenix.Core.Models;

namespace Phoenix.Core.Tests;

public class BookTests
{
    [Fact]
    public void Book_NewInstance_HasDefaultValues()
    {
        // Arrange & Act
        var book = new Book();

        // Assert
        Assert.NotEqual(Guid.Empty, book.Id);
        Assert.Equal(string.Empty, book.Title);
        Assert.Equal(string.Empty, book.Author);
        Assert.NotNull(book.Tags);
        Assert.Empty(book.Tags);
        Assert.NotNull(book.Chapters);
        Assert.Empty(book.Chapters);
    }

    [Fact]
    public void Book_SetProperties_ReturnsCorrectValues()
    {
        // Arrange
        var book = new Book
        {
            Title = "Test Book",
            Author = "Test Author",
            FilePath = @"C:\Books\test.epub"
        };

        // Assert
        Assert.Equal("Test Book", book.Title);
        Assert.Equal("Test Author", book.Author);
        Assert.Equal(@"C:\Books\test.epub", book.FilePath);
    }
}

public class ChapterTests
{
    [Fact]
    public void Chapter_NewInstance_HasDefaultValues()
    {
        // Arrange & Act
        var chapter = new Chapter();

        // Assert
        Assert.NotEqual(Guid.Empty, chapter.Id);
        Assert.Equal(string.Empty, chapter.Title);
        Assert.Equal(0, chapter.Level);
        Assert.NotNull(chapter.Children);
        Assert.Empty(chapter.Children);
    }
}

public class BookmarkTests
{
    [Fact]
    public void Bookmark_NewInstance_HasDefaultValues()
    {
        // Arrange & Act
        var bookmark = new Bookmark();

        // Assert
        Assert.NotEqual(Guid.Empty, bookmark.Id);
        Assert.Equal(string.Empty, bookmark.Title);
        Assert.True(bookmark.CreatedAt <= DateTime.UtcNow);
    }
}

public class UserSettingsTests
{
    [Fact]
    public void UserSettings_NewInstance_HasDefaultValues()
    {
        // Arrange & Act
        var settings = new UserSettings();

        // Assert
        Assert.Equal(1, settings.Id);
        Assert.Equal("Segoe UI", settings.FontFamily);
        Assert.Equal(16, settings.FontSize);
        Assert.Equal(1.6, settings.LineHeight);
        Assert.Equal(ReaderTheme.Default, settings.Theme);
        Assert.True(settings.RememberPosition);
    }
}
