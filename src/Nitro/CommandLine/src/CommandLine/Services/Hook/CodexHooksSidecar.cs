using System.Text.Json;
using System.Text.Json.Serialization;

namespace ChilliCream.Nitro.CommandLine.Services.Hook;

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
/// The sidecar file recording only the <c>config.toml</c> <c>notify</c>
/// value Nitro replaced, keyed by the absolute config path. <c>hooks.json</c>
/// ownership is deterministic: uninstall removes managed-event groups whose
/// commands are Nitro Codex hook invocations, so it needs no provenance file.
/// </summary>
internal sealed record CodexHooksSidecarFile(
    [property: JsonPropertyName("version")] int Version,
    [property: JsonPropertyName("notifyFiles")]
        Dictionary<string, CodexNotifySidecarEntry> NotifyFiles)
{
    public const int CurrentVersion = 2;

    public static CodexHooksSidecarFile Empty => new(CurrentVersion, []);

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
