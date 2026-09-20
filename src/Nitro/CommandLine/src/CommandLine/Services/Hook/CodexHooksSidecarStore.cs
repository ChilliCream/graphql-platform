using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace ChilliCream.Nitro.CommandLine.Services.Hook;

internal sealed class CodexHooksSidecarStore(
    IFileSystem fileSystem,
    Workspace.IGlobalConfigDirectoryProvider globalConfigDirectoryProvider)
    : ICodexHooksSidecarStore
{
    private const string FileName = "codex-hooks-sidecar.json";
    private const int MaxLockAttempts = 5;
    private static readonly TimeSpan s_lockRetryDelay = TimeSpan.FromMilliseconds(10);

    public async Task<CodexHooksSidecarFile> ReadAsync(CancellationToken cancellationToken)
    {
        var (file, _) = await ReadWithHashAsync(cancellationToken);

        return file;
    }

    public async Task<(CodexHooksSidecarFile File, string Hash)> ReadWithHashAsync(
        CancellationToken cancellationToken)
    {
        var path = ResolvePath();
        var text = fileSystem.FileExists(path) ? await fileSystem.ReadAllTextAsync(path, cancellationToken) : null;

        return (Parse(text), Hash(text));
    }

    public async Task<bool> WriteIfUnchangedAsync(
        CodexHooksSidecarFile file,
        string hashAtRead,
        CancellationToken cancellationToken)
    {
        var path = ResolvePath();
        var directory = globalConfigDirectoryProvider.GetDirectory();

        if (!fileSystem.DirectoryExists(directory))
        {
            fileSystem.CreateDirectory(directory);
        }

        await using var sidecarLock = await AcquireWriteLockAsync(path, cancellationToken);
        var currentText = fileSystem.FileExists(path)
            ? await fileSystem.ReadAllTextAsync(path, cancellationToken)
            : null;

        if (Hash(currentText) != hashAtRead)
        {
            return false;
        }

        var json = JsonSerializer.Serialize(file, CodexHooksSidecarJsonContext.Default.CodexHooksSidecarFile);

        await fileSystem.ReplaceFileAtomicAsync(path, json, cancellationToken);

        return true;
    }

    private async Task<FileStream> AcquireWriteLockAsync(string path, CancellationToken cancellationToken)
    {
        var lockPath = path + ".lock";

        for (var attempt = 1; attempt <= MaxLockAttempts; attempt++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                return File.Open(lockPath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
            }
            catch (IOException)
            {
                if (attempt < MaxLockAttempts)
                {
                    await Task.Delay(s_lockRetryDelay, cancellationToken);
                }
            }
        }

        throw new ExitException(
            $"The '{FileName}' sidecar record is locked by another nitro process. Re-run the command.");
    }

    private static CodexHooksSidecarFile Parse(string? text)
    {
        if (text is null)
        {
            return CodexHooksSidecarFile.Empty;
        }

        try
        {
            return JsonSerializer.Deserialize(text, CodexHooksSidecarJsonContext.Default.CodexHooksSidecarFile)
                ?? CodexHooksSidecarFile.Empty;
        }
        catch (JsonException)
        {
            return CodexHooksSidecarFile.Empty;
        }
    }

    private static string Hash(string? text)
        => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(text ?? string.Empty)));

    private string ResolvePath() => Path.Combine(globalConfigDirectoryProvider.GetDirectory(), FileName);
}
