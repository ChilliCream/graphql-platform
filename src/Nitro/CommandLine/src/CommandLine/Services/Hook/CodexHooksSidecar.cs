using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ChilliCream.Nitro.CommandLine.Services.Hook;

/// <summary>
/// One <c>hooks.json</c> event's recorded installation: the exact command
/// and timeout this CLI wrote, and their hash. The Codex analog of
/// <see cref="ClaudeHooksSidecarEntry"/> - kept as a distinct type rather
/// than reused across harnesses, matching this codebase's per-harness type
/// convention (<see cref="CodexHookPayload"/> vs
/// <see cref="ClaudeHookPayload"/>) even though the shape is identical.
/// </summary>
internal sealed record CodexHooksSidecarEntry(
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
/// The <c>config.toml</c> <c>notify</c> key's recorded installation:
/// provenance for the foreign-wrapping contract. <see cref="PriorForeign"/>
/// is null when no foreign <c>notify</c> was configured at install time (so
/// uninstall removes the key entirely rather than writing back an empty
/// array); non-null preserves exactly what was there so uninstall can
/// restore it verbatim (semantic restoration, same non-goal as
/// <see cref="ClaudeHooksSidecarFile"/>: not a byte-accurate restore of the
/// whole config file).
/// </summary>
internal sealed record CodexNotifySidecarEntry(
    [property: JsonPropertyName("ourArgv")] IReadOnlyList<string> OurArgv,
    [property: JsonPropertyName("priorForeign")] IReadOnlyList<string>? PriorForeign,
    [property: JsonPropertyName("installedAt")] DateTimeOffset InstalledAt);

/// <summary>
/// The sidecar file recording exactly which Codex CLI <c>hooks.json</c>
/// entries and <c>config.toml</c> <c>notify</c> wrapping this CLI installed,
/// keyed by the absolute path of each config file (one machine has exactly
/// one <c>CODEX_HOME</c> in the common case, but this mirrors
/// <see cref="ClaudeHooksSidecarFile"/>'s per-path keying rather than
/// assuming that). A separate file from the Claude sidecar (own version,
/// own filename): the two installers must never share failure modes.
/// </summary>
internal sealed record CodexHooksSidecarFile(
    [property: JsonPropertyName("version")] int Version,
    [property: JsonPropertyName("hooksFiles")]
        Dictionary<string, Dictionary<string, CodexHooksSidecarEntry>> HooksFiles,
    [property: JsonPropertyName("notifyFiles")]
        Dictionary<string, CodexNotifySidecarEntry> NotifyFiles)
{
    public const int CurrentVersion = 1;

    public static CodexHooksSidecarFile Empty => new(CurrentVersion, [], []);

    public IReadOnlyDictionary<string, CodexHooksSidecarEntry> HooksEntriesFor(string hooksJsonPath)
        => HooksFiles.TryGetValue(hooksJsonPath, out var events)
            ? events
            : new Dictionary<string, CodexHooksSidecarEntry>();

    public CodexNotifySidecarEntry? NotifyEntryFor(string configTomlPath)
        => NotifyFiles.GetValueOrDefault(configTomlPath);
}

[JsonSourceGenerationOptions(WriteIndented = true)]
[JsonSerializable(typeof(CodexHooksSidecarFile))]
internal sealed partial class CodexHooksSidecarJsonContext : JsonSerializerContext;

/// <summary>
/// Reads and writes the Codex hooks sidecar file under the global config
/// directory. Read failures (missing file, corrupt JSON) resolve to
/// <see cref="CodexHooksSidecarFile.Empty"/> rather than throwing, same
/// degrade-not-fail contract as <see cref="ClaudeHooksSidecarStore"/>.
/// </summary>
internal interface ICodexHooksSidecarStore
{
    Task<CodexHooksSidecarFile> ReadAsync(CancellationToken cancellationToken);

    Task WriteAsync(CodexHooksSidecarFile file, CancellationToken cancellationToken);
}

internal sealed class CodexHooksSidecarStore(
    IFileSystem fileSystem,
    Services.Workspace.IGlobalConfigDirectoryProvider globalConfigDirectoryProvider) : ICodexHooksSidecarStore
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
