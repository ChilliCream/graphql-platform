using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ChilliCream.Nitro.CommandLine.Services.Hook;

/// <summary>
/// One hooks-dir event's recorded installation: the exact command and
/// timeout this CLI wrote, and their hash. The Copilot analog of
/// <see cref="ClaudeHooksSidecarEntry"/>/<see cref="CodexHooksSidecarEntry"/>
/// - kept as a distinct type rather than reused across harnesses, matching
/// this codebase's per-harness type convention.
/// </summary>
internal sealed record CopilotHooksSidecarEntry(
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
/// The sidecar file recording exactly which Copilot CLI hooks-dir entries
/// this CLI installed, keyed by the absolute path of each hooks file (one
/// machine has exactly one <c>COPILOT_HOME</c> in the common case, but this
/// mirrors <see cref="ClaudeHooksSidecarFile"/>/<see cref="CodexHooksSidecarFile"/>'s
/// per-path keying rather than assuming that). A separate file from the
/// Claude and Codex sidecars (own version, own filename): the three
/// installers must never share failure modes.
/// </summary>
internal sealed record CopilotHooksSidecarFile(
    [property: JsonPropertyName("version")] int Version,
    [property: JsonPropertyName("hooksFiles")]
        Dictionary<string, Dictionary<string, CopilotHooksSidecarEntry>> HooksFiles)
{
    public const int CurrentVersion = 1;

    public static CopilotHooksSidecarFile Empty => new(CurrentVersion, []);

    public IReadOnlyDictionary<string, CopilotHooksSidecarEntry> HooksEntriesFor(string hooksJsonPath)
        => HooksFiles.TryGetValue(hooksJsonPath, out var events)
            ? events
            : new Dictionary<string, CopilotHooksSidecarEntry>();
}

[JsonSourceGenerationOptions(WriteIndented = true)]
[JsonSerializable(typeof(CopilotHooksSidecarFile))]
internal sealed partial class CopilotHooksSidecarJsonContext : JsonSerializerContext;

/// <summary>
/// Reads and writes the Copilot hooks sidecar file under the global config
/// directory. Read failures (missing file, corrupt JSON) resolve to
/// <see cref="CopilotHooksSidecarFile.Empty"/> rather than throwing, same
/// degrade-not-fail contract as <see cref="ClaudeHooksSidecarStore"/>/<see cref="CodexHooksSidecarStore"/>.
/// </summary>
internal interface ICopilotHooksSidecarStore
{
    Task<CopilotHooksSidecarFile> ReadAsync(CancellationToken cancellationToken);

    Task WriteAsync(CopilotHooksSidecarFile file, CancellationToken cancellationToken);
}

internal sealed class CopilotHooksSidecarStore(
    IFileSystem fileSystem,
    Services.Workspace.IGlobalConfigDirectoryProvider globalConfigDirectoryProvider) : ICopilotHooksSidecarStore
{
    private const string FileName = "copilot-hooks-sidecar.json";

    public async Task<CopilotHooksSidecarFile> ReadAsync(CancellationToken cancellationToken)
    {
        var path = ResolvePath();

        if (!fileSystem.FileExists(path))
        {
            return CopilotHooksSidecarFile.Empty;
        }

        try
        {
            var json = await fileSystem.ReadAllTextAsync(path, cancellationToken);

            return JsonSerializer.Deserialize(json, CopilotHooksSidecarJsonContext.Default.CopilotHooksSidecarFile)
                ?? CopilotHooksSidecarFile.Empty;
        }
        catch (JsonException)
        {
            return CopilotHooksSidecarFile.Empty;
        }
    }

    public async Task WriteAsync(CopilotHooksSidecarFile file, CancellationToken cancellationToken)
    {
        var directory = globalConfigDirectoryProvider.GetDirectory();

        if (!fileSystem.DirectoryExists(directory))
        {
            fileSystem.CreateDirectory(directory);
        }

        var json = JsonSerializer.Serialize(file, CopilotHooksSidecarJsonContext.Default.CopilotHooksSidecarFile);

        await fileSystem.ReplaceFileAtomicAsync(ResolvePath(), json, cancellationToken);
    }

    private string ResolvePath() => Path.Combine(globalConfigDirectoryProvider.GetDirectory(), FileName);
}
