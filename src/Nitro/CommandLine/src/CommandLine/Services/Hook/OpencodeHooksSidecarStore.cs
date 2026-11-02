using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using ChilliCream.Nitro.CommandLine.Services.Workspace;

namespace ChilliCream.Nitro.CommandLine.Services.Hook;

internal sealed class OpencodeHooksSidecarStore(
    IFileSystem fileSystem,
    IGlobalConfigDirectoryProvider globalConfigDirectoryProvider) : IOpencodeHooksSidecarStore
{
    private const string FileName = "opencode-hooks-sidecar.json";
    private const int MaxLockAttempts = 5;
    private static readonly TimeSpan s_lockRetryDelay = TimeSpan.FromMilliseconds(10);

    public async Task<(OpencodeHooksSidecarFile File, string Hash)> ReadWithHashAsync(
        CancellationToken cancellationToken)
    {
        var path = ResolvePath();
        var text = fileSystem.FileExists(path) ? await fileSystem.ReadAllTextAsync(path, cancellationToken) : null;

        return (Parse(text), Hash(text));
    }

    public async Task<bool> WriteIfUnchangedAsync(
        OpencodeHooksSidecarFile file,
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

        var json = JsonSerializer.Serialize(file, OpencodeHooksSidecarJsonContext.Default.OpencodeHooksSidecarFile);
        await fileSystem.ReplaceFileAtomicAsync(path, json, cancellationToken);

        return true;
    }

    private async Task<FileStream> AcquireWriteLockAsync(string path, CancellationToken cancellationToken)
    {
        var lockPath = path + ".lock";

        IOException? lockException = null;

        for (var attempt = 1; attempt <= MaxLockAttempts; attempt++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                return File.Open(lockPath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
            }
            catch (IOException exception)
            {
                lockException = exception;

                if (attempt < MaxLockAttempts)
                {
                    await Task.Delay(s_lockRetryDelay, cancellationToken);
                }
            }
        }

        throw ThrowHelper.Exit(
            $"Could not acquire write lock '{lockPath}' for sidecar '{path}' after {MaxLockAttempts} attempts: {lockException!.Message}");
    }

    internal static string Hash(string? text)
        => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(text ?? string.Empty))).ToLowerInvariant();

    private static OpencodeHooksSidecarFile Parse(string? text)
    {
        if (text is null)
        {
            return OpencodeHooksSidecarFile.Empty;
        }

        try
        {
            return JsonSerializer.Deserialize(text, OpencodeHooksSidecarJsonContext.Default.OpencodeHooksSidecarFile)
                ?? OpencodeHooksSidecarFile.Empty;
        }
        catch (JsonException)
        {
            return OpencodeHooksSidecarFile.Empty;
        }
    }

    private string ResolvePath() => Path.Combine(globalConfigDirectoryProvider.GetDirectory(), FileName);
}
