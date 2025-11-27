namespace HotChocolate.Adapters.Mcp.Storage;

public sealed class OperationToolIconTests
{
    [Theory]
    [InlineData("http://example.com/icon.png")]
    [InlineData("https://example.com/icon.png")]
    [InlineData("data:image/svg+xml;base64,...")]
    public void OperationToolIcon_ValidSourceScheme_Succeeds(string uriString)
    {
        // arrange
        var uri = new Uri(uriString);

        // act
        var exception = Record.Exception(() => new OperationToolIcon(uri));

        // assert
        Assert.Null(exception);
    }

    [Theory]
    [InlineData("ftp://example.com/icon.png")]
    [InlineData("file:///C:/icon.png")]
    [InlineData("mailto:example@example.com")]
    public void OperationToolIcon_InvalidSourceScheme_ThrowsArgumentException(string uriString)
    {
        // arrange
        var uri = new Uri(uriString);

        // act
        var exception = Record.Exception(() => new OperationToolIcon(uri));

        // assert
        Assert.IsType<ArgumentException>(exception);
        Assert.Equal(
            "The icon source URI must use the HTTP, HTTPS, or data scheme. (Parameter 'Source')",
            exception.Message);
    }

    [Theory]
    [InlineData("image/png")]
    [InlineData("image/jpeg")]
    [InlineData("image/svg+xml")]
    [InlineData("image/webp")]
    public void OperationToolIcon_ValidMimeType_Succeeds(string mimeType)
    {
        // arrange & act
        var exception =
            Record.Exception(
                () => new OperationToolIcon(new Uri("https://example.com/icon"))
                {
                    MimeType = mimeType
                });

        // assert
        Assert.Null(exception);
    }

    [Theory]
    [InlineData("invalid-mime-type")]
    [InlineData("image\\png")]
    public void OperationToolIcon_InvalidMimeType_ThrowsArgumentException(string mimeType)
    {
        // arrange & act
        var exception =
            Record.Exception(
                () => new OperationToolIcon(new Uri("https://example.com/icon"))
                {
                    MimeType = mimeType
                });

        // assert
        Assert.IsType<ArgumentException>(exception);
        Assert.Equal(
            "The MIME type must be a valid type/subtype string. (Parameter 'MimeType')",
            exception.Message);
    }

    [Theory]
    [InlineData("48x48")]
    [InlineData("any")]
    [InlineData("32x32", "64x64", "128x128")]
    public void OperationToolIcon_ValidSizes_Succeeds(params string[] sizes)
    {
        // arrange & act
        var exception =
            Record.Exception(
                () => new OperationToolIcon(new Uri("https://example.com/icon.png"))
                {
                    Sizes = sizes
                });

        // assert
        Assert.Null(exception);
    }

    [Theory]
    [InlineData("48by48")]
    [InlineData("large")]
    [InlineData("32x32", "64by64", "128x128")]
    public void OperationToolIcon_InvalidSizes_ThrowsArgumentException(params string[] sizes)
    {
        // arrange & act
        var exception =
            Record.Exception(
                () => new OperationToolIcon(new Uri("https://example.com/icon.png"))
                {
                    Sizes = sizes
                });

        // assert
        Assert.IsType<ArgumentException>(exception);
        Assert.Equal(
            "Each size must have the format {WIDTH}x{HEIGHT} (e.g., 48x48) or be 'any'. "
            + "(Parameter 'Sizes')",
            exception.Message);
    }
}
