using System.Text.Json;
using System.Text.Json.Nodes;

namespace ChilliCream.Nitro.CommandLine.Services.Hook;

/// <summary>
/// Pure JSON-text editing for a Claude Code <c>settings.json</c>'s
/// <c>hooks</c> section: no file I/O, no sidecar persistence, so golden
/// fixture tests can feed a "before" text and a prior sidecar snapshot in
/// and assert the exact "after" text and outcomes out. Foreign structure
/// (other top-level keys, other events, other hook groups under our four
/// events) round-trips through <see cref="JsonNode"/> untouched; only the
/// group(s) this installer owns are added, replaced, or removed.
/// </summary>
internal static class ClaudeHooksEditor
{
    private const string HooksKey = "hooks";
    private const string GroupHooksKey = "hooks";
    private const string TypeKey = "type";
    private const string CommandKey = "command";
    private const string TimeoutKey = "timeout";
    private const string CommandType = "command";

    public sealed record InstallResult(
        string SettingsJson,
        IReadOnlyDictionary<string, ClaudeHooksSidecarEntry> Sidecar,
        IReadOnlyList<HookInstallEventResult> Outcomes);

    public sealed record UninstallResult(
        string SettingsJson,
        IReadOnlyDictionary<string, ClaudeHooksSidecarEntry> Sidecar,
        IReadOnlyList<HookUninstallEventResult> Outcomes);

    /// <summary>
    /// Adds or replaces the single Nitro-owned hook group under each managed
    /// event. A group counts as Nitro-owned when every hook entry inside it
    /// carries <see cref="ClaudeHooksTemplate.CommandMarker"/> - this
    /// installer only ever writes single-hook groups, so any group failing
    /// that test is left alone as foreign, even if one of its hooks happens
    /// to match by coincidence.
    /// </summary>
    public static InstallResult Install(
        string? existingSettingsJson,
        LaunchDescriptor descriptor,
        DateTimeOffset now)
    {
        var root = ParseOrEmpty(existingSettingsJson);
        var hooksNode = GetOrCreateHooksObject(root);

        var outcomes = new List<HookInstallEventResult>(ClaudeHooksTemplate.Events.Count);
        var sidecar = new Dictionary<string, ClaudeHooksSidecarEntry>();

        foreach (var claudeEvent in ClaudeHooksTemplate.Events)
        {
            var eventArray = GetOrCreateEventArray(hooksNode, claudeEvent);
            var desiredCommand = ClaudeHooksTemplate.BuildCommand(descriptor, claudeEvent);
            const int desiredTimeout = ClaudeHooksTemplate.TimeoutSeconds;

            var ownedIndex = FindOwnedGroupIndex(eventArray);

            if (ownedIndex < 0)
            {
                AppendGroup(eventArray, BuildGroup(desiredCommand, desiredTimeout));
                outcomes.Add(new HookInstallEventResult(claudeEvent, HookInstallOutcome.Installed));
            }
            else
            {
                var (existingCommand, existingTimeout) = ReadFirstHook((JsonObject)eventArray[ownedIndex]!);

                if (existingCommand == desiredCommand && existingTimeout == desiredTimeout)
                {
                    outcomes.Add(new HookInstallEventResult(claudeEvent, HookInstallOutcome.Unchanged));
                }
                else
                {
                    eventArray[ownedIndex] = BuildGroup(desiredCommand, desiredTimeout);
                    outcomes.Add(new HookInstallEventResult(claudeEvent, HookInstallOutcome.Updated));
                }
            }

            sidecar[claudeEvent] = new ClaudeHooksSidecarEntry(
                desiredCommand,
                desiredTimeout,
                ClaudeHooksSidecarEntry.ComputeHash(desiredCommand, desiredTimeout),
                now);
        }

        return new InstallResult(Serialize(root), sidecar, outcomes);
    }

    /// <summary>
    /// Reports, per managed event, whether an installed entry matches the
    /// current template exactly (Installed), a Nitro-owned entry exists but
    /// its text differs (Outdated - a stale launch descriptor, a changed
    /// timeout, or a manual edit all land here identically), or no
    /// Nitro-owned entry exists (Missing). Never mutates.
    /// </summary>
    public static IReadOnlyList<HookStatusEventResult> Status(
        string? existingSettingsJson, LaunchDescriptor descriptor)
    {
        var root = ParseOrEmpty(existingSettingsJson);
        var hooksNode = root[HooksKey] as JsonObject;

        var results = new List<HookStatusEventResult>(ClaudeHooksTemplate.Events.Count);

        foreach (var claudeEvent in ClaudeHooksTemplate.Events)
        {
            var eventArray = hooksNode?[claudeEvent] as JsonArray;
            var ownedIndex = eventArray is null ? -1 : FindOwnedGroupIndex(eventArray);

            if (ownedIndex < 0)
            {
                results.Add(new HookStatusEventResult(claudeEvent, HookStatusOutcome.Missing, null));
                continue;
            }

            var (command, timeout) = ReadFirstHook((JsonObject)eventArray![ownedIndex]!);
            var desiredCommand = ClaudeHooksTemplate.BuildCommand(descriptor, claudeEvent);

            var outcome = command == desiredCommand && timeout == ClaudeHooksTemplate.TimeoutSeconds
                ? HookStatusOutcome.Installed
                : HookStatusOutcome.Outdated;

            results.Add(new HookStatusEventResult(claudeEvent, outcome, command));
        }

        return results;
    }

    /// <summary>
    /// Removes only this installer's own entries. For each event, prefers
    /// removing the group whose command exactly matches
    /// <paramref name="priorSidecar"/>'s recorded text (provenance-precise:
    /// proof this install wrote it); falls back to marker-based group
    /// removal when the sidecar has no record or the recorded text no
    /// longer matches anything on disk (a manual edit since install, or a
    /// sidecar that predates this event). A foreign entry sharing the same
    /// event (for example another tool's <c>SessionStart</c> hook) is never
    /// touched, because it never satisfies either match.
    /// </summary>
    public static UninstallResult Uninstall(
        string? existingSettingsJson,
        IReadOnlyDictionary<string, ClaudeHooksSidecarEntry> priorSidecar)
    {
        var root = ParseOrEmpty(existingSettingsJson);
        var hooksNode = root[HooksKey] as JsonObject;

        var outcomes = new List<HookUninstallEventResult>(ClaudeHooksTemplate.Events.Count);

        foreach (var claudeEvent in ClaudeHooksTemplate.Events)
        {
            var eventArray = hooksNode?[claudeEvent] as JsonArray;

            if (eventArray is null)
            {
                outcomes.Add(new HookUninstallEventResult(claudeEvent, HookUninstallOutcome.NotPresent));
                continue;
            }

            var removeIndex = priorSidecar.TryGetValue(claudeEvent, out var recorded)
                ? FindGroupIndexByCommand(eventArray, recorded.Command)
                : -1;

            if (removeIndex < 0)
            {
                removeIndex = FindOwnedGroupIndex(eventArray);
            }

            if (removeIndex < 0)
            {
                outcomes.Add(new HookUninstallEventResult(claudeEvent, HookUninstallOutcome.NotPresent));
                continue;
            }

            eventArray.RemoveAt(removeIndex);
            outcomes.Add(new HookUninstallEventResult(claudeEvent, HookUninstallOutcome.Removed));

            if (eventArray.Count == 0)
            {
                hooksNode!.Remove(claudeEvent);
            }
        }

        if (hooksNode?.Count == 0)
        {
            root.Remove(HooksKey);
        }

        // After uninstall this settings file has nothing left for this
        // installer to track; the caller persists an empty entry set (or
        // drops the file's key entirely) for it.
        return new UninstallResult(
            Serialize(root), new Dictionary<string, ClaudeHooksSidecarEntry>(), outcomes);
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
            throw new ExitException($"settings.json is not valid JSON: {ex.Message}");
        }

        return node as JsonObject
            ?? throw new ExitException("settings.json's top level is not a JSON object; refusing to edit it.");
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

    private static JsonArray GetOrCreateEventArray(JsonObject hooksNode, string claudeEvent)
    {
        if (hooksNode[claudeEvent] is JsonArray existing)
        {
            return existing;
        }

        var created = new JsonArray();
        hooksNode[claudeEvent] = created;

        return created;
    }

    /// <summary>
    /// Appends a node built at compile-time as a <see cref="JsonNode"/>
    /// (never a raw primitive) through <see cref="JsonArray"/>'s
    /// <see cref="IList{T}"/> implementation rather than its convenience
    /// <c>Add&lt;T&gt;</c> overload: the generic overload exists to wrap
    /// arbitrary primitive values into a new <see cref="JsonValue"/>, which
    /// is unnecessary here and carries a trim/AOT warning this call does not
    /// need to take on.
    /// </summary>
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
            if (array[i] is not JsonObject group
                || group[GroupHooksKey] is not JsonArray hooks
                || hooks.Count == 0)
            {
                continue;
            }

            var allOwned = true;

            foreach (var hook in hooks)
            {
                var command = (hook as JsonObject)?[CommandKey]?.GetValue<string>();

                if (command is null || !command.Contains(ClaudeHooksTemplate.CommandMarker, StringComparison.Ordinal))
                {
                    allOwned = false;
                    break;
                }
            }

            if (allOwned)
            {
                return i;
            }
        }

        return -1;
    }

    private static int FindGroupIndexByCommand(JsonArray array, string command)
    {
        for (var i = 0; i < array.Count; i++)
        {
            if (array[i] is JsonObject group
                && group[GroupHooksKey] is JsonArray hooks
                && hooks.Count == 1
                && (hooks[0] as JsonObject)?[CommandKey]?.GetValue<string>() == command)
            {
                return i;
            }
        }

        return -1;
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
