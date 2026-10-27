using System.Text.Json;
using ChilliCream.Nitro.CommandLine.Services.Workspace;
using ChilliCream.Nitro.CommandLine.Tui.Board;

namespace ChilliCream.Nitro.CommandLine.Services.Preferences;

internal sealed class BoardPreferencesStore(
    IFileSystem fileSystem,
    IGlobalConfigDirectoryProvider globalConfigDirectoryProvider) : IBoardPreferencesStore
{
    private const string FileName = "board-preferences.json";

    private readonly SemaphoreSlim _writeGate = new(1, 1);

    public async Task<BoardOrientation> ReadOrientationAsync(CancellationToken cancellationToken)
    {
        var path = ResolvePath();

        try
        {
            if (!fileSystem.FileExists(path))
            {
                return BoardOrientation.Auto;
            }

            var text = await fileSystem.ReadAllTextAsync(path, cancellationToken);
            var file = JsonSerializer.Deserialize(text, BoardPreferencesJsonContext.Default.BoardPreferencesFile);

            return Parse(file?.BoardOrientation);
        }
        catch (Exception exception) when (exception is JsonException or IOException or UnauthorizedAccessException)
        {
            return BoardOrientation.Auto;
        }
    }

    public async Task<bool> WriteOrientationAsync(BoardOrientation orientation, CancellationToken cancellationToken)
    {
        var json = JsonSerializer.Serialize(
            new BoardPreferencesFile(orientation.ToString()),
            BoardPreferencesJsonContext.Default.BoardPreferencesFile);

        // Writes land in call order so the last choice made is the one persisted.
        await _writeGate.WaitAsync(cancellationToken);

        try
        {
            var directory = globalConfigDirectoryProvider.GetDirectory();

            if (!fileSystem.DirectoryExists(directory))
            {
                fileSystem.CreateDirectory(directory);
            }

            await fileSystem.ReplaceFileAtomicAsync(ResolvePath(), json, cancellationToken);

            return true;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            return false;
        }
        finally
        {
            _writeGate.Release();
        }
    }

    private static BoardOrientation Parse(string? value)
    {
        foreach (var orientation in Enum.GetValues<BoardOrientation>())
        {
            if (string.Equals(orientation.ToString(), value, StringComparison.OrdinalIgnoreCase))
            {
                return orientation;
            }
        }

        return BoardOrientation.Auto;
    }

    private string ResolvePath() => Path.Combine(globalConfigDirectoryProvider.GetDirectory(), FileName);
}
