using System.Text.Json;
using System.Text.Json.Nodes;

namespace ChilliCream.Nitro.CommandLine.Services.Hook;

/// <summary>
/// Pure JSON-text editing for a Codex CLI <c>hooks.json</c>: no file I/O, no
/// sidecar persistence, matching <see cref="ClaudeHooksEditor"/>'s
/// no-side-effects contract. The one structural difference from
/// <c>settings.json</c>: both files use a top-level <c>"hooks"</c> map.
/// Foreign structure (other
/// events, other hook groups under our three managed events, e.g. the
/// pre-existing <c>herdr</c> <c>SessionStart</c> entry S1 observed
/// live) round-trips through <see cref="JsonNode"/> untouched; only the
/// group(s) this installer owns are added, replaced, or removed.
/// </summary>
internal static class CodexHooksEditor
{
    private const string GroupHooksKey = "hooks";
    private const string TypeKey = "type";
    private const string CommandKey = "command";
    private const string TimeoutKey = "timeout";
    private const string CommandType = "command";

    public sealed record InstallResult(
        string HooksJson,
        IReadOnlyList<HookInstallEventResult> Outcomes);

    public sealed record UninstallResult(
        string HooksJson,
        IReadOnlyList<HookUninstallEventResult> Outcomes);

    /// <summary>
    /// Adds or replaces the single Nitro-owned hook group under each managed
    /// event.
    /// </summary>
    public static InstallResult Install(
        string? existingHooksJson,
        LaunchDescriptor descriptor)
    {
        var root = ParseOrEmpty(existingHooksJson);
        var hooks = GetOrCreateHooks(root);

        var outcomes = new List<HookInstallEventResult>(CodexHooksTemplate.Events.Count);
        foreach (var codexEvent in CodexHooksTemplate.Events)
        {
            var eventArray = GetOrCreateEventArray(hooks, codexEvent);
            var desiredCommand = CodexHooksTemplate.BuildCommand(descriptor, codexEvent);
            const int desiredTimeout = CodexHooksTemplate.TimeoutSeconds;

            var ownedIndex = FindOwnedGroupIndex(eventArray);

            if (ownedIndex < 0)
            {
                AppendGroup(eventArray, BuildGroup(desiredCommand, desiredTimeout));
                outcomes.Add(new HookInstallEventResult(codexEvent, HookInstallOutcome.Installed));
            }
            else
            {
                var (existingCommand, existingTimeout) = ReadFirstHook((JsonObject)eventArray[ownedIndex]!);

                if (existingCommand == desiredCommand && existingTimeout == desiredTimeout)
                {
                    outcomes.Add(new HookInstallEventResult(codexEvent, HookInstallOutcome.Unchanged));
                }
                else
                {
                    eventArray[ownedIndex] = BuildGroup(desiredCommand, desiredTimeout);
                    outcomes.Add(new HookInstallEventResult(codexEvent, HookInstallOutcome.Updated));
                }
            }
        }

        return new InstallResult(Serialize(root), outcomes);
    }

    /// <summary>
    /// Reports, per managed event, whether an installed entry matches the
    /// current template exactly (Installed), a Nitro-owned entry exists but
    /// its text differs (Outdated), or no Nitro-owned entry exists
    /// (Missing). Never mutates.
    /// </summary>
    public static IReadOnlyList<HookStatusEventResult> Status(
        string? existingHooksJson, LaunchDescriptor descriptor)
    {
        var root = ParseOrEmpty(existingHooksJson);
        var hooks = GetHooks(root);

        var results = new List<HookStatusEventResult>(CodexHooksTemplate.Events.Count);

        foreach (var codexEvent in CodexHooksTemplate.Events)
        {
            var eventArray = hooks?[codexEvent] as JsonArray;
            var ownedIndex = eventArray is null ? -1 : FindOwnedGroupIndex(eventArray);

            if (ownedIndex < 0)
            {
                results.Add(new HookStatusEventResult(codexEvent, HookStatusOutcome.Missing, null));
                continue;
            }

            var (command, timeout) = ReadFirstHook((JsonObject)eventArray![ownedIndex]!);
            var desiredCommand = CodexHooksTemplate.BuildCommand(descriptor, codexEvent);

            var outcome = command == desiredCommand && timeout == CodexHooksTemplate.TimeoutSeconds
                ? HookStatusOutcome.Installed
                : HookStatusOutcome.Outdated;

            results.Add(new HookStatusEventResult(codexEvent, outcome, command));
        }

        return results;
    }

    /// <summary>
    /// Removes every Nitro-owned group under the events this installer owns.
    /// </summary>
    public static UninstallResult Uninstall(string? existingHooksJson)
    {
        var root = ParseOrEmpty(existingHooksJson);
        var hooks = GetHooks(root);

        var outcomes = new List<HookUninstallEventResult>(CodexHooksTemplate.Events.Count);

        foreach (var codexEvent in CodexHooksTemplate.Events)
        {
            var eventArray = hooks?[codexEvent] as JsonArray;

            if (eventArray is null)
            {
                outcomes.Add(new HookUninstallEventResult(codexEvent, HookUninstallOutcome.NotPresent));
                continue;
            }

            var removed = false;

            for (var i = eventArray.Count - 1; i >= 0; i--)
            {
                if (!IsOwnedGroup(eventArray[i]))
                {
                    continue;
                }

                eventArray.RemoveAt(i);
                removed = true;
            }

            if (!removed)
            {
                outcomes.Add(new HookUninstallEventResult(codexEvent, HookUninstallOutcome.NotPresent));
                continue;
            }

            outcomes.Add(new HookUninstallEventResult(codexEvent, HookUninstallOutcome.Removed));

            if (eventArray.Count == 0)
            {
                hooks!.Remove(codexEvent);
            }
        }

        return new UninstallResult(Serialize(root), outcomes);
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
            throw new ExitException($"hooks.json is not valid JSON: {ex.Message}");
        }

        return node as JsonObject
            ?? throw new ExitException("hooks.json's top level is not a JSON object; refusing to edit it.");
    }

    private static JsonArray GetOrCreateEventArray(JsonObject root, string codexEvent)
    {
        if (root[codexEvent] is JsonArray existing)
        {
            return existing;
        }

        var created = new JsonArray();
        root[codexEvent] = created;

        return created;
    }

    private static JsonObject GetOrCreateHooks(JsonObject root)
    {
        if (root[GroupHooksKey] is JsonObject hooks)
        {
            MigrateLegacyManagedEvents(root, hooks);
            return hooks;
        }

        var created = new JsonObject();
        root[GroupHooksKey] = created;
        MigrateLegacyManagedEvents(root, created);
        return created;
    }

    private static JsonObject? GetHooks(JsonObject root) => root[GroupHooksKey] as JsonObject;

    // Nitro versions before the Codex hooks schema was corrected wrote these
    // events beside the hooks map. Move only Nitro's managed event names so an
    // upgrade repairs that invalid layout without touching unrelated data.
    private static void MigrateLegacyManagedEvents(JsonObject root, JsonObject hooks)
    {
        foreach (var codexEvent in CodexHooksTemplate.Events)
        {
            if (root[codexEvent] is not JsonArray legacy)
            {
                continue;
            }

            var target = GetOrCreateEventArray(hooks, codexEvent);

            foreach (var group in legacy)
            {
                target.Add(group?.DeepClone());
            }

            root.Remove(codexEvent);
        }
    }

    private static void AppendGroup(JsonArray array, JsonObject group)
        => ((IList<JsonNode?>)array).Add(group);

    private static JsonObject BuildGroup(string command, int timeoutSeconds) => new()
    {
        [GroupHooksKey] = new JsonArray(
            new JsonObject
            {
                [TypeKey] = CommandType,
                [CommandKey] = command,
                [TimeoutKey] = timeoutSeconds
            })
    };

    private static int FindOwnedGroupIndex(JsonArray array)
    {
        for (var i = 0; i < array.Count; i++)
        {
            if (IsOwnedGroup(array[i]))
            {
                return i;
            }
        }

        return -1;
    }

    private static bool IsOwnedGroup(JsonNode? node)
    {
        if (node is not JsonObject group
            || group[GroupHooksKey] is not JsonArray hooks
            || hooks.Count == 0)
        {
            return false;
        }

        return hooks.All(hook =>
            (hook as JsonObject)?[CommandKey]?.GetValue<string>()?.Contains(
                CodexHooksTemplate.CommandMarker, StringComparison.Ordinal) == true);
    }

    private static (string? Command, int? Timeout) ReadFirstHook(JsonObject group)
    {
        if (group[GroupHooksKey] is not JsonArray hooks || hooks.Count == 0 || hooks[0] is not JsonObject hook)
        {
            return (null, null);
        }

        return (hook[CommandKey]?.GetValue<string>(), hook[TimeoutKey]?.GetValue<int>());
    }

    private static string Serialize(JsonObject root)
        => root.ToJsonString(new JsonSerializerOptions { WriteIndented = true });
}
