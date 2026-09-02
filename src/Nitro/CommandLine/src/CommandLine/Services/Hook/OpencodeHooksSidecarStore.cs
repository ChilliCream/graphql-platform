using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using ChilliCream.Nitro.CommandLine.Services;
using ChilliCream.Nitro.CommandLine.Services.Workspace;

namespace ChilliCream.Nitro.CommandLine.Services.Hook;

internal interface IOpencodeHooksSidecarStore
{
    Task<(OpencodeHooksSidecarFile File, string Hash)> ReadWithHashAsync(CancellationToken cancellationToken);

    Task<bool> WriteIfUnchangedAsync(
        OpencodeHooksSidecarFile file,
        string hashAtRead,
        CancellationToken cancellationToken);
}

internal sealed class OpencodeHooksSidecarStore(
    IFileSystem fileSystem,
    IGlobalConfigDirectoryProvider globalConfigDirectoryProvider) : IOpencodeHooksSidecarStore
{
    private const string FileName = "opencode-hooks-sidecar.json";

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
        var currentText = fileSystem.FileExists(path)
            ? await fileSystem.ReadAllTextAsync(path, cancellationToken)
            : null;

        if (Hash(currentText) != hashAtRead)
        {
            return false;
        }

        var directory = globalConfigDirectoryProvider.GetDirectory();

        if (!fileSystem.DirectoryExists(directory))
        {
            fileSystem.CreateDirectory(directory);
        }

        var json = JsonSerializer.Serialize(file, OpencodeHooksSidecarJsonContext.Default.OpencodeHooksSidecarFile);
        await fileSystem.ReplaceFileAtomicAsync(path, json, cancellationToken);

        return true;
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

internal sealed record OpencodeHooksSidecarFile(
    [property: JsonPropertyName("version")] int Version,
    [property: JsonPropertyName("files")] Dictionary<string, OpencodeHooksSidecarEntry> Files)
{
    public const int CurrentVersion = 1;

    public static OpencodeHooksSidecarFile Empty => new(CurrentVersion, []);
}

internal sealed record OpencodeHooksSidecarEntry(
    [property: JsonPropertyName("launchCommand")] string LaunchCommand,
    [property: JsonPropertyName("contentHash")] string ContentHash,
    [property: JsonPropertyName("installedAt")] DateTimeOffset InstalledAt);

[JsonSerializable(typeof(OpencodeHooksSidecarFile))]
internal sealed partial class OpencodeHooksSidecarJsonContext : JsonSerializerContext;
