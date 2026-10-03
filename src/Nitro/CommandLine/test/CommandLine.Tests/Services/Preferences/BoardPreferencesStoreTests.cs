using ChilliCream.Nitro.CommandLine.Services;
using ChilliCream.Nitro.CommandLine.Services.Preferences;
using ChilliCream.Nitro.CommandLine.Services.Workspace;
using ChilliCream.Nitro.CommandLine.Tui.Board;

namespace ChilliCream.Nitro.CommandLine.Tests.Preferences;

public sealed class BoardPreferencesStoreTests : IDisposable
{
    private readonly DirectoryInfo _tempRoot = Directory.CreateTempSubdirectory("nitro-board-preferences-tests");

    public void Dispose() => _tempRoot.Delete(recursive: true);

    private string ConfigDirectory => Path.Combine(_tempRoot.FullName, "config");

    private string PreferencesPath => Path.Combine(ConfigDirectory, "board-preferences.json");

    private BoardPreferencesStore CreateStore()
        => new(new FileSystem(), new FixedGlobalConfigDirectoryProvider(ConfigDirectory));

    [Theory]
    [InlineData("Auto")]
    [InlineData("SideBySide")]
    [InlineData("Stacked")]
    public async Task WriteOrientationAsync_Should_RoundTrip_When_ReadBack(string orientationName)
    {
        // arrange
        var orientation = Enum.Parse<BoardOrientation>(orientationName);
        var ct = TestContext.Current.CancellationToken;
        var store = CreateStore();

        // act
        var written = await store.WriteOrientationAsync(orientation, ct);
        var read = await CreateStore().ReadOrientationAsync(ct);

        // assert
        Assert.True(written);
        Assert.Equal(orientation, read);
    }

    [Fact]
    public async Task WriteOrientationAsync_Should_PersistReadableJson_When_DirectoryDoesNotExist()
    {
        // arrange
        var ct = TestContext.Current.CancellationToken;

        // act
        await CreateStore().WriteOrientationAsync(BoardOrientation.SideBySide, ct);

        // assert
        (await File.ReadAllTextAsync(PreferencesPath, ct)).ReplaceLineEndings("\n").MatchInlineSnapshot(
            """
            {
              "boardOrientation": "SideBySide"
            }
            """);
    }

    [Fact]
    public async Task ReadOrientationAsync_Should_ReturnAuto_When_FileIsMissing()
    {
        // act
        var orientation = await CreateStore().ReadOrientationAsync(TestContext.Current.CancellationToken);

        // assert
        Assert.Equal(BoardOrientation.Auto, orientation);
    }

    [Theory]
    [InlineData("{ not json")]
    [InlineData("")]
    [InlineData("null")]
    [InlineData("[]")]
    [InlineData("{}")]
    [InlineData("""{"boardOrientation":"Diagonal"}""")]
    [InlineData("""{"boardOrientation":"1"}""")]
    [InlineData("""{"boardOrientation":7}""")]
    public async Task ReadOrientationAsync_Should_ReturnAuto_When_FileIsCorruptOrUnrecognized(string content)
    {
        // arrange
        var ct = TestContext.Current.CancellationToken;
        Directory.CreateDirectory(ConfigDirectory);
        await File.WriteAllTextAsync(PreferencesPath, content, ct);

        // act
        var orientation = await CreateStore().ReadOrientationAsync(ct);

        // assert
        Assert.Equal(BoardOrientation.Auto, orientation);
    }

    [Fact]
    public async Task ReadOrientationAsync_Should_ReturnAuto_When_PreferencesPathIsADirectory()
    {
        // arrange
        Directory.CreateDirectory(PreferencesPath);

        // act
        var orientation = await CreateStore().ReadOrientationAsync(TestContext.Current.CancellationToken);

        // assert
        Assert.Equal(BoardOrientation.Auto, orientation);
    }

    [Fact]
    public async Task WriteOrientationAsync_Should_ReturnFalse_When_PreferencesPathIsADirectory()
    {
        // arrange
        Directory.CreateDirectory(PreferencesPath);

        // act
        var written = await CreateStore().WriteOrientationAsync(
            BoardOrientation.Stacked, TestContext.Current.CancellationToken);

        // assert
        Assert.False(written);
    }

    [Fact]
    public async Task WriteOrientationAsync_Should_PersistLastChoice_When_CalledRepeatedlyWithoutAwaiting()
    {
        // arrange
        var ct = TestContext.Current.CancellationToken;
        var store = CreateStore();
        var orientations = new[]
        {
            BoardOrientation.SideBySide,
            BoardOrientation.Stacked,
            BoardOrientation.Auto,
            BoardOrientation.SideBySide
        };

        // act
        var writes = orientations.Select(o => store.WriteOrientationAsync(o, ct)).ToArray();
        await Task.WhenAll(writes);
        var read = await CreateStore().ReadOrientationAsync(ct);

        // assert
        Assert.Equal(BoardOrientation.SideBySide, read);
    }

    private sealed class FixedGlobalConfigDirectoryProvider(string directory) : IGlobalConfigDirectoryProvider
    {
        public string GetDirectory() => directory;
    }
}
