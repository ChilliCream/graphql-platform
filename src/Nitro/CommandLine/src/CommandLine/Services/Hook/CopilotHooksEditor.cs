using System.Text.Json;
using System.Text.Json.Nodes;

namespace ChilliCream.Nitro.CommandLine.Services.Hook;

/// <summary>
/// Pure JSON-text editing for a Copilot CLI hooks-dir file, matching
/// <see cref="ClaudeHooksEditor"/>/<see cref="CodexHooksEditor"/>'s
/// no-side-effects contract. There are two structural differences from
/// Codex's <c>hooks.json</c>: the top level must be
/// <c>{"hooks": {"&lt;event&gt;": [...]}}</c> (a bare, unwrapped event map
/// is a live, logged parse error that silently loads zero hooks), and each
/// event's array holds hook objects directly (no Codex-style nested
/// per-group <c>"hooks"</c> array). Because this installer always targets
/// its own dedicated filename (<see cref="ICopilotPathResolver"/>), there is
/// no other tool's content to round-trip through this file in the normal
/// case; the foreign-entry handling below exists defensively (a user could
/// still hand-edit this exact file) rather than as the expected case.
/// </summary>
internal static class CopilotHooksEditor
{
    private const string HooksKey = "hooks";
    private const string TypeKey = "type";
    private const string CommandKey = "command";
    private const string TimeoutKey = "timeoutSec";
    private const string CommandType = "command";

    public sealed record InstallResult(
        string HooksJson,
        IReadOnlyDictionary<string, CopilotHooksSidecarEntry> Sidecar,
        IReadOnlyList<HookInstallEventResult> Outcomes);

    public sealed record UninstallResult(
        string HooksJson,
        IReadOnlyDictionary<string, CopilotHooksSidecarEntry> Sidecar,
        IReadOnlyList<HookUninstallEventResult> Outcomes);

    /// <summary>
    /// Adds or replaces the single Nitro-owned hook entry under each managed
    /// event, inside the required <c>"hooks"</c> wrapper.
    /// </summary>
    public static InstallResult Install(
        string? existingJson,
        LaunchDescriptor descriptor,
        DateTimeOffset now)
    {
        var root = ParseOrEmpty(existingJson);
        var hooks = GetOrCreateHooksObject(root);

        var outcomes = new List<HookInstallEventResult>(CopilotHooksTemplate.Events.Count);
        var sidecar = new Dictionary<string, CopilotHooksSidecarEntry>();

        foreach (var copilotEvent in CopilotHooksTemplate.Events)
        {
            var eventArray = GetOrCreateEventArray(hooks, copilotEvent);
            var desiredCommand = CopilotHooksTemplate.BuildCommand(descriptor, copilotEvent);
            const int desiredTimeout = CopilotHooksTemplate.TimeoutSeconds;

            var ownedIndex = FindOwnedHookIndex(eventArray);

            if (ownedIndex < 0)
            {
                AppendHook(eventArray, BuildHook(desiredCommand, desiredTimeout));
                outcomes.Add(new HookInstallEventResult(copilotEvent, HookInstallOutcome.Installed));
            }
            else
            {
                var (existingCommand, existingTimeout) = ReadHook((JsonObject)eventArray[ownedIndex]!);

                if (existingCommand == desiredCommand && existingTimeout == desiredTimeout)
                {
                    outcomes.Add(new HookInstallEventResult(copilotEvent, HookInstallOutcome.Unchanged));
                }
                else
                {
                    eventArray[ownedIndex] = BuildHook(desiredCommand, desiredTimeout);
                    outcomes.Add(new HookInstallEventResult(copilotEvent, HookInstallOutcome.Updated));
                }
            }

            sidecar[copilotEvent] = new CopilotHooksSidecarEntry(
                desiredCommand,
                desiredTimeout,
                CopilotHooksSidecarEntry.ComputeHash(desiredCommand, desiredTimeout),
                now);
        }

        return new InstallResult(Serialize(root), sidecar, outcomes);
    }

    /// <summary>
    /// Reports, per managed event, whether an installed entry matches the
    /// current template exactly (Installed), a Nitro-owned entry exists but
    /// its text differs (Outdated), or no Nitro-owned entry exists
    /// (Missing). Never mutates.
    /// </summary>
    public static IReadOnlyList<HookStatusEventResult> Status(
        string? existingJson, LaunchDescriptor descriptor)
    {
        var root = ParseOrEmpty(existingJson);
        var hooks = root[HooksKey] as JsonObject;

        var results = new List<HookStatusEventResult>(CopilotHooksTemplate.Events.Count);

        foreach (var copilotEvent in CopilotHooksTemplate.Events)
        {
            var eventArray = hooks?[copilotEvent] as JsonArray;
            var ownedIndex = eventArray is null ? -1 : FindOwnedHookIndex(eventArray);

            if (ownedIndex < 0)
            {
                results.Add(new HookStatusEventResult(copilotEvent, HookStatusOutcome.Missing, null));
                continue;
            }

            var (command, timeout) = ReadHook((JsonObject)eventArray![ownedIndex]!);
            var desiredCommand = CopilotHooksTemplate.BuildCommand(descriptor, copilotEvent);

            var outcome = command == desiredCommand && timeout == CopilotHooksTemplate.TimeoutSeconds
                ? HookStatusOutcome.Installed
                : HookStatusOutcome.Outdated;

            results.Add(new HookStatusEventResult(copilotEvent, outcome, command));
        }

        return results;
    }

    /// <summary>
    /// Removes only this installer's own entries, same provenance-first /
    /// marker-fallback strategy as <see cref="ClaudeHooksEditor.Uninstall"/>.
    /// </summary>
    public static UninstallResult Uninstall(
        string? existingJson,
        IReadOnlyDictionary<string, CopilotHooksSidecarEntry> priorSidecar)
    {
        var root = ParseOrEmpty(existingJson);
        var hooks = root[HooksKey] as JsonObject;

        var outcomes = new List<HookUninstallEventResult>(CopilotHooksTemplate.Events.Count);

        foreach (var copilotEvent in CopilotHooksTemplate.Events)
        {
            var eventArray = hooks?[copilotEvent] as JsonArray;

            if (eventArray is null)
            {
                outcomes.Add(new HookUninstallEventResult(copilotEvent, HookUninstallOutcome.NotPresent));
                continue;
            }

            var removeIndex = priorSidecar.TryGetValue(copilotEvent, out var recorded)
                ? FindHookIndexByCommand(eventArray, recorded.Command)
                : -1;

            if (removeIndex < 0)
            {
                removeIndex = FindOwnedHookIndex(eventArray);
            }

            if (removeIndex < 0)
            {
                outcomes.Add(new HookUninstallEventResult(copilotEvent, HookUninstallOutcome.NotPresent));
                continue;
            }

            eventArray.RemoveAt(removeIndex);
            outcomes.Add(new HookUninstallEventResult(copilotEvent, HookUninstallOutcome.Removed));

            if (eventArray.Count == 0)
            {
                hooks!.Remove(copilotEvent);
            }
        }

        if (hooks is { Count: 0 })
        {
            root.Remove(HooksKey);
        }

        return new UninstallResult(
            Serialize(root), new Dictionary<string, CopilotHooksSidecarEntry>(), outcomes);
    }

    private static JsonObject ParseOrEmpty(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return [];
        }

        JsonNode? node;

        try
        {
            node = JsonNode.Parse(
                json,
                documentOptions: new JsonDocumentOptions
                {
                    CommentHandling = JsonCommentHandling.Skip,
                    AllowTrailingCommas = true
                });
        }
        catch (JsonException ex)
        {
            throw new ExitException($"The Copilot hooks file is not valid JSON: {ex.Message}");
        }

        return node as JsonObject
            ?? throw new ExitException("The Copilot hooks file's top level is not a JSON object; refusing to edit it.");
    }

    private static JsonObject GetOrCreateHooksObject(JsonObject root)
    {
        if (root[HooksKey] is JsonObject existing)
        {
            return existing;
        }

        var created = new JsonObject();
        root[HooksKey] = created;

        return created;
    }

    private static JsonArray GetOrCreateEventArray(JsonObject hooks, string copilotEvent)
    {
        if (hooks[copilotEvent] is JsonArray existing)
        {
            return existing;
        }

        var created = new JsonArray();
        hooks[copilotEvent] = created;

        return created;
    }

    private static void AppendHook(JsonArray array, JsonObject hook) => ((IList<JsonNode?>)array).Add(hook);

    private static JsonObject BuildHook(string command, int timeoutSeconds) => new()
    {
        [TypeKey] = CommandType,
        [CommandKey] = command,
        [TimeoutKey] = timeoutSeconds
    };

    private static int FindOwnedHookIndex(JsonArray array)
    {
        for (var i = 0; i < array.Count; i++)
        {
            var command = (array[i] as JsonObject)?[CommandKey]?.GetValue<string>();

            if (command?.Contains(CopilotHooksTemplate.CommandMarker, StringComparison.Ordinal) == true)
            {
                return i;
            }
        }

        return -1;
    }

    private static int FindHookIndexByCommand(JsonArray array, string command)
    {
        for (var i = 0; i < array.Count; i++)
        {
            if ((array[i] as JsonObject)?[CommandKey]?.GetValue<string>() == command)
            {
                return i;
            }
        }

        return -1;
    }

    private static (string? Command, int? Timeout) ReadHook(JsonObject hook)
        => (hook[CommandKey]?.GetValue<string>(), hook[TimeoutKey]?.GetValue<int>());

    private static string Serialize(JsonObject root)
        => root.ToJsonString(new JsonSerializerOptions { WriteIndented = true });
}
