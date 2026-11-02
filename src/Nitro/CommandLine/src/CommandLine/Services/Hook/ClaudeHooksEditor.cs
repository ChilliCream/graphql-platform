using System.Text.Json;
using System.Text.Json.Nodes;

namespace ChilliCream.Nitro.CommandLine.Services.Hook;

/// <summary>
/// Edits Nitro-owned hook handlers in settings JSON following Claude Code's hook format. A
/// handler is Nitro-owned when its command contains the template's ownership marker, in any
/// group, including groups that also hold other hooks.
/// </summary>
internal static class ClaudeHooksEditor
{
    private const string HooksKey = "hooks";
    private const string GroupHooksKey = "hooks";
    private const string MatcherKey = "matcher";
    private const string TypeKey = "type";
    private const string CommandKey = "command";
    private const string TimeoutKey = "timeout";
    private const string AsyncKey = "async";
    private const string EnabledKey = "enabled";
    private const string StatusMessageKey = "statusMessage";
    private const string DisableAllHooksKey = "disableAllHooks";
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
    /// Leaves exactly one Nitro-owned handler per managed event with the expected command, timeout,
    /// and matcher, preserving sibling hooks and group metadata. A retained disabled handler stays
    /// disabled and is reported as <see cref="HookInstallOutcome.Disabled"/>.
    /// </summary>
    public static InstallResult Install(
        string? existingSettingsJson,
        ClaudeHooksTemplate template,
        LaunchDescriptor descriptor,
        DateTimeOffset now)
    {
        var root = ParseOrEmpty(existingSettingsJson);
        var hooksNode = GetOrCreateHooksObject(root);

        var outcomes = new List<HookInstallEventResult>(template.Events.Count);
        var sidecar = new Dictionary<string, ClaudeHooksSidecarEntry>();

        foreach (var hookEvent in template.Events)
        {
            var eventArray = GetOrCreateEventArray(hooksNode, hookEvent);
            var before = eventArray.DeepClone();
            var desiredCommand = template.BuildCommand(descriptor, hookEvent);
            var desiredMatcher = ClaudeHooksTemplate.Matcher(hookEvent);
            const int desiredTimeout = ClaudeHooksTemplate.TimeoutSeconds;

            var owned = FindOwnedHandlers(eventArray, template.CommandMarker, recordedCommand: null);
            var kept = owned.FirstOrDefault(handler => HasMatcher(handler.Group, desiredMatcher))
                ?? owned.FirstOrDefault();

            RemoveHandlers(eventArray, owned.Where(handler => !ReferenceEquals(handler, kept)));

            if (kept is null)
            {
                AppendGroup(
                    eventArray,
                    BuildGroup(
                        desiredMatcher,
                        new JsonObject
                        {
                            [TypeKey] = CommandType,
                            [CommandKey] = desiredCommand,
                            [TimeoutKey] = desiredTimeout
                        }));
            }
            else
            {
                if (!HasMatcher(kept.Group, desiredMatcher))
                {
                    RemoveHandlers(eventArray, [kept]);
                    AppendGroup(eventArray, BuildGroup(desiredMatcher, kept.Hook));
                }

                if (!IsCurrent(kept.Hook, template, desiredCommand))
                {
                    Rewrite(kept.Hook, template, desiredCommand, desiredTimeout);
                }
            }

            HookInstallOutcome outcome;

            if (kept is not null && IsSwitchedOff(kept.Hook, template))
            {
                outcome = HookInstallOutcome.Disabled;
            }
            else if (owned.Count == 0)
            {
                outcome = HookInstallOutcome.Installed;
            }
            else
            {
                outcome = JsonNode.DeepEquals(before, eventArray)
                    ? HookInstallOutcome.Unchanged
                    : HookInstallOutcome.Updated;
            }

            outcomes.Add(new HookInstallEventResult(hookEvent, outcome));

            sidecar[hookEvent] = new ClaudeHooksSidecarEntry(
                desiredCommand,
                desiredTimeout,
                ClaudeHooksSidecarEntry.ComputeHash(desiredCommand, desiredTimeout),
                now);
        }

        return new InstallResult(Serialize(root), sidecar, outcomes);
    }

    /// <summary>
    /// Reports Missing when an event has no Nitro-owned handler, Installed when it has exactly
    /// one with the expected command and timeout in a group with the event's expected matcher,
    /// Disabled when that one handler is switched off, and Outdated otherwise.
    /// </summary>
    public static IReadOnlyList<HookStatusEventResult> Status(
        string? existingSettingsJson, ClaudeHooksTemplate template, LaunchDescriptor descriptor)
    {
        var root = ParseOrEmpty(existingSettingsJson);
        var hooksNode = root[HooksKey] as JsonObject;

        var results = new List<HookStatusEventResult>(template.Events.Count);

        foreach (var hookEvent in template.Events)
        {
            var owned = hooksNode?[hookEvent] is JsonArray eventArray
                ? FindOwnedHandlers(eventArray, template.CommandMarker, recordedCommand: null)
                : [];

            if (owned.Count == 0)
            {
                results.Add(new HookStatusEventResult(hookEvent, HookStatusOutcome.Missing, null));
                continue;
            }

            var first = owned[0];
            HookStatusOutcome outcome;

            if (owned.Count > 1
                || !HasMatcher(first.Group, ClaudeHooksTemplate.Matcher(hookEvent))
                || !IsCurrent(first.Hook, template, template.BuildCommand(descriptor, hookEvent)))
            {
                outcome = HookStatusOutcome.Outdated;
            }
            else
            {
                outcome = IsSwitchedOff(first.Hook, template)
                    ? HookStatusOutcome.Disabled
                    : HookStatusOutcome.Installed;
            }

            results.Add(new HookStatusEventResult(hookEvent, outcome, ReadString(first.Hook, CommandKey)));
        }

        return results;
    }

    /// <summary>
    /// Removes every Nitro-owned handler of each managed event, and every handler with the
    /// recorded command, keeping sibling hooks and their group metadata. A group or event
    /// the removal leaves without hooks is removed too.
    /// </summary>
    public static UninstallResult Uninstall(
        string? existingSettingsJson,
        ClaudeHooksTemplate template,
        IReadOnlyDictionary<string, ClaudeHooksSidecarEntry> priorSidecar)
    {
        var root = ParseOrEmpty(existingSettingsJson);
        var hooksNode = root[HooksKey] as JsonObject;

        var outcomes = new List<HookUninstallEventResult>(template.Events.Count);

        foreach (var hookEvent in template.Events)
        {
            if (hooksNode?[hookEvent] is not JsonArray eventArray)
            {
                outcomes.Add(new HookUninstallEventResult(hookEvent, HookUninstallOutcome.NotPresent));
                continue;
            }

            var recordedCommand = priorSidecar.TryGetValue(hookEvent, out var recorded) ? recorded.Command : null;
            var owned = FindOwnedHandlers(eventArray, template.CommandMarker, recordedCommand);

            if (owned.Count == 0)
            {
                outcomes.Add(new HookUninstallEventResult(hookEvent, HookUninstallOutcome.NotPresent));
                continue;
            }

            RemoveHandlers(eventArray, owned);
            outcomes.Add(new HookUninstallEventResult(hookEvent, HookUninstallOutcome.Removed));

            if (eventArray.Count == 0)
            {
                hooksNode.Remove(hookEvent);
            }
        }

        if (hooksNode?.Count == 0)
        {
            root.Remove(HooksKey);
        }

        return new UninstallResult(
            Serialize(root), new Dictionary<string, ClaudeHooksSidecarEntry>(), outcomes);
    }

    /// <summary>
    /// Returns, in settings order, every hook event with at least one hook whose command
    /// contains <paramref name="commandMarker"/>, in any group.
    /// </summary>
    public static IReadOnlyList<string> FindEventsWithCommandMarker(
        string? existingSettingsJson, string commandMarker)
    {
        if (ParseOrEmpty(existingSettingsJson)[HooksKey] is not JsonObject hooksNode)
        {
            return [];
        }

        var events = new List<string>();

        foreach (var (hookEvent, node) in hooksNode)
        {
            if (node is JsonArray groups
                && FindOwnedHandlers(groups, commandMarker, recordedCommand: null).Count > 0)
            {
                events.Add(hookEvent);
            }
        }

        return events;
    }

    /// <summary>
    /// Returns whether the settings set the top-level <c>disableAllHooks</c> flag to <c>true</c>.
    /// </summary>
    public static bool DisablesAllHooks(string? existingSettingsJson)
        => ParseOrEmpty(existingSettingsJson)[DisableAllHooksKey] is JsonValue value
            && value.TryGetValue<bool>(out var disabled)
            && disabled;

    private static List<OwnedHandler> FindOwnedHandlers(
        JsonArray eventArray, string commandMarker, string? recordedCommand)
    {
        var owned = new List<OwnedHandler>();

        foreach (var node in eventArray)
        {
            if (node is not JsonObject group || group[GroupHooksKey] is not JsonArray hooks)
            {
                continue;
            }

            foreach (var hookNode in hooks)
            {
                if (hookNode is JsonObject hook
                    && ReadString(hook, CommandKey) is { } command
                    && (command.Contains(commandMarker, StringComparison.Ordinal) || command == recordedCommand))
                {
                    owned.Add(new OwnedHandler(group, hook));
                }
            }
        }

        return owned;
    }

    /// <summary>
    /// Removes <paramref name="handlers"/> from their groups, and each group the removal empties.
    /// </summary>
    private static void RemoveHandlers(JsonArray eventArray, IEnumerable<OwnedHandler> handlers)
    {
        foreach (var handler in handlers.ToList())
        {
            var hooks = (JsonArray)handler.Group[GroupHooksKey]!;
            hooks.Remove(handler.Hook);

            if (hooks.Count == 0)
            {
                eventArray.Remove(handler.Group);
            }
        }
    }

    /// <summary>
    /// Whether <paramref name="group"/> has the matcher <paramref name="matcher"/>. A null
    /// <paramref name="matcher"/> stands for every occurrence of the event, which a matcher that
    /// is absent, empty, or <c>*</c> selects; a matcher that is present but not text,
    /// <c>null</c> included, keeps the group from running at all.
    /// </summary>
    private static bool HasMatcher(JsonObject group, string? matcher)
        => matcher is null
            ? IsAbsentOr(group, MatcherKey, node => ReadText(node) is "" or "*")
            : ReadString(group, MatcherKey) == matcher;

    /// <summary>
    /// Whether <paramref name="hook"/> is a synchronous command handler running
    /// <paramref name="desiredCommand"/> with the expected timeout, a status message that is
    /// absent or text and, for a harness with hook switches, an <c>enabled</c> flag that is
    /// absent or a boolean. A property set to <c>null</c> is present, not absent.
    /// </summary>
    private static bool IsCurrent(JsonObject hook, ClaudeHooksTemplate template, string desiredCommand)
        => ReadString(hook, TypeKey) == CommandType
            && ReadString(hook, CommandKey) == desiredCommand
            && hook[TimeoutKey] is JsonValue timeout
            && timeout.TryGetValue<int>(out var seconds)
            && seconds == ClaudeHooksTemplate.TimeoutSeconds
            && IsAbsentOr(hook, AsyncKey, node => node is JsonValue value
                && value.TryGetValue<bool>(out var runsAsync)
                && !runsAsync)
            && IsAbsentOr(hook, StatusMessageKey, node => ReadText(node) is not null)
            && (!template.HasHookSwitches || IsAbsentOr(hook, EnabledKey, IsBoolean));

    /// <summary>
    /// Sets the expected type, command, and timeout on <paramref name="hook"/> and removes what
    /// keeps it from running synchronously or at all, keeping a boolean <c>enabled</c> flag, a
    /// text status message, and any other property.
    /// </summary>
    private static void Rewrite(JsonObject hook, ClaudeHooksTemplate template, string command, int timeoutSeconds)
    {
        hook[TypeKey] = CommandType;
        hook[CommandKey] = command;
        hook[TimeoutKey] = timeoutSeconds;
        hook.Remove(AsyncKey);

        if (!IsAbsentOr(hook, StatusMessageKey, node => ReadText(node) is not null))
        {
            hook.Remove(StatusMessageKey);
        }

        if (template.HasHookSwitches && !IsAbsentOr(hook, EnabledKey, IsBoolean))
        {
            hook.Remove(EnabledKey);
        }
    }

    private static bool IsSwitchedOff(JsonObject hook, ClaudeHooksTemplate template)
        => template.HasHookSwitches
            && hook[EnabledKey] is JsonValue value
            && value.TryGetValue<bool>(out var enabled)
            && !enabled;

    private static bool IsAbsentOr(JsonObject node, string key, Func<JsonNode?, bool> isValid)
        => !node.TryGetPropertyValue(key, out var value) || isValid(value);

    private static bool IsBoolean(JsonNode? node)
        => node is JsonValue value && value.TryGetValue<bool>(out _);

    private static string? ReadText(JsonNode? node)
        => node is JsonValue value && value.TryGetValue<string>(out var text) ? text : null;

    private static string? ReadString(JsonObject node, string key) => ReadText(node[key]);

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

    private static JsonArray GetOrCreateEventArray(JsonObject hooksNode, string hookEvent)
    {
        if (hooksNode[hookEvent] is JsonArray existing)
        {
            return existing;
        }

        var created = new JsonArray();
        hooksNode[hookEvent] = created;

        return created;
    }

    private static void AppendGroup(JsonArray array, JsonObject group)
        => ((IList<JsonNode?>)array).Add(group);

    private static JsonObject BuildGroup(string? matcher, JsonObject hook)
    {
        var group = new JsonObject();

        if (matcher is not null)
        {
            group[MatcherKey] = matcher;
        }

        group[GroupHooksKey] = new JsonArray(hook);

        return group;
    }

    private static string Serialize(JsonObject root)
        => root.ToJsonString(new JsonSerializerOptions { WriteIndented = true });

    private sealed class OwnedHandler(JsonObject group, JsonObject hook)
    {
        public JsonObject Group { get; } = group;

        public JsonObject Hook { get; } = hook;
    }
}
