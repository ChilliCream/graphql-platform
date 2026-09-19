using System.Text.Json;
using System.Text.Json.Nodes;

namespace ChilliCream.Nitro.CommandLine.Services.Hook;

/// <summary>
/// Edits Nitro-owned hook groups in Claude settings JSON.
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
    /// Adds a Nitro hook group for each managed event or updates the first existing
    /// Nitro-owned group. A nonempty group is Nitro-owned when every hook command
    /// contains the ownership marker.
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
    /// Reports Installed when the first hook in the first Nitro-owned group has the
    /// expected command and timeout, Outdated when either differs, or Missing when
    /// no owned group exists.
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
    /// Removes one matching group per managed event, preferring the recorded command
    /// and falling back to the ownership marker. Other groups are preserved.
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
            if (hooksNode?[claudeEvent] is not JsonArray eventArray)
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
                hooksNode.Remove(claudeEvent);
            }
        }

        if (hooksNode?.Count == 0)
        {
            root.Remove(HooksKey);
        }

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
