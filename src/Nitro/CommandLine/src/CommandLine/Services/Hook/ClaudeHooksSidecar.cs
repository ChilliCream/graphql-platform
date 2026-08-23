using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ChilliCream.Nitro.CommandLine.Services.Hook;

/// <summary>
/// One event's recorded installation: the exact command and timeout this
/// CLI wrote, and their hash. Provenance, not just detection - unlike
/// <see cref="ClaudeHooksTemplate.CommandMarker"/> matching (which any
/// Nitro-owned entry satisfies, on any machine, from any version), a
/// sidecar record is proof THIS install wrote THIS exact entry, which is
/// what makes uninstall able to remove precisely what it installed instead
/// of falling back to marker matching.
/// </summary>
internal sealed record ClaudeHooksSidecarEntry(
    [property: JsonPropertyName("command")] string Command,
    [property: JsonPropertyName("timeoutSeconds")] int TimeoutSeconds,
    [property: JsonPropertyName("hash")] string Hash,
    [property: JsonPropertyName("installedAt")] DateTimeOffset InstalledAt)
{
    public static string ComputeHash(string command, int timeoutSeconds)
    {
        var canonical = $"{command}\n{timeoutSeconds}";
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(canonical));

        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}

/// <summary>
/// The sidecar file recording exactly which Claude Code <c>settings.json</c>
/// hook entries this CLI installed, keyed by the absolute path of the
/// settings file (one machine can have both a user-scope and a
/// project-scope installation). Lives under the platform application-data
/// directory alongside the instance id, never inside a harness config
/// directory, so it survives a foreign edit or reinstall of the config file
/// itself.
/// </summary>
internal sealed record ClaudeHooksSidecarFile(
    [property: JsonPropertyName("version")] int Version,
    [property: JsonPropertyName("files")]
        Dictionary<string, Dictionary<string, ClaudeHooksSidecarEntry>> Files)
{
    public const int CurrentVersion = 1;

    public static ClaudeHooksSidecarFile Empty => new(CurrentVersion, []);

    public IReadOnlyDictionary<string, ClaudeHooksSidecarEntry> EntriesFor(string settingsFilePath)
        => Files.TryGetValue(settingsFilePath, out var events)
            ? events
            : new Dictionary<string, ClaudeHooksSidecarEntry>();
}

[JsonSourceGenerationOptions(WriteIndented = true)]
[JsonSerializable(typeof(ClaudeHooksSidecarFile))]
internal sealed partial class ClaudeHooksSidecarJsonContext : JsonSerializerContext;

/// <summary>
/// Reads and writes the Claude hooks sidecar file under the global config
/// directory. Read failures (missing file, corrupt JSON) resolve to
/// <see cref="ClaudeHooksSidecarFile.Empty"/> rather than throwing: a lost
/// or corrupted sidecar degrades install/uninstall to marker-based
/// detection, it does not fail the command.
/// </summary>
internal interface IClaudeHooksSidecarStore
{
    Task<ClaudeHooksSidecarFile> ReadAsync(CancellationToken cancellationToken);

    Task WriteAsync(ClaudeHooksSidecarFile file, CancellationToken cancellationToken);
}

internal sealed class ClaudeHooksSidecarStore(
    IFileSystem fileSystem,
    Services.Workspace.IGlobalConfigDirectoryProvider globalConfigDirectoryProvider) : IClaudeHooksSidecarStore
{
    private const string FileName = "claude-hooks-sidecar.json";

    public async Task<ClaudeHooksSidecarFile> ReadAsync(CancellationToken cancellationToken)
    {
        var path = ResolvePath();

        if (!fileSystem.FileExists(path))
        {
            return ClaudeHooksSidecarFile.Empty;
        }

        try
        {
            var json = await fileSystem.ReadAllTextAsync(path, cancellationToken);

            return JsonSerializer.Deserialize(json, ClaudeHooksSidecarJsonContext.Default.ClaudeHooksSidecarFile)
                ?? ClaudeHooksSidecarFile.Empty;
        }
        catch (JsonException)
        {
            return ClaudeHooksSidecarFile.Empty;
        }
    }

    public async Task WriteAsync(ClaudeHooksSidecarFile file, CancellationToken cancellationToken)
    {
        var directory = globalConfigDirectoryProvider.GetDirectory();

        if (!fileSystem.DirectoryExists(directory))
        {
            fileSystem.CreateDirectory(directory);
        }

        var json = JsonSerializer.Serialize(file, ClaudeHooksSidecarJsonContext.Default.ClaudeHooksSidecarFile);

        await fileSystem.ReplaceFileAtomicAsync(ResolvePath(), json, cancellationToken);
    }

    private string ResolvePath() => Path.Combine(globalConfigDirectoryProvider.GetDirectory(), FileName);
}
