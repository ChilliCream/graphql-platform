using System.Text.Json;

namespace ChilliCream.Nitro.CommandLine.Services.Hook;

internal sealed class CodexHooksSidecarStore(
    IFileSystem fileSystem,
    Workspace.IGlobalConfigDirectoryProvider globalConfigDirectoryProvider)
    : ICodexHooksSidecarStore
{
    private const string FileName = "codex-hooks-sidecar.json";

    public async Task<CodexHooksSidecarFile> ReadAsync(CancellationToken cancellationToken)
    {
        var path = ResolvePath();

        if (!fileSystem.FileExists(path))
        {
            return CodexHooksSidecarFile.Empty;
        }

        try
        {
            var json = await fileSystem.ReadAllTextAsync(path, cancellationToken);

            return JsonSerializer.Deserialize(json, CodexHooksSidecarJsonContext.Default.CodexHooksSidecarFile)
                ?? CodexHooksSidecarFile.Empty;
        }
        catch (JsonException)
        {
            return CodexHooksSidecarFile.Empty;
        }
    }

    public async Task WriteAsync(CodexHooksSidecarFile file, CancellationToken cancellationToken)
    {
        var directory = globalConfigDirectoryProvider.GetDirectory();

        if (!fileSystem.DirectoryExists(directory))
        {
            fileSystem.CreateDirectory(directory);
        }

        var json = JsonSerializer.Serialize(file, CodexHooksSidecarJsonContext.Default.CodexHooksSidecarFile);

        await fileSystem.ReplaceFileAtomicAsync(ResolvePath(), json, cancellationToken);
    }

    private string ResolvePath() => Path.Combine(globalConfigDirectoryProvider.GetDirectory(), FileName);
}
