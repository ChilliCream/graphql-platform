using System.Text.Json.Nodes;
using ChilliCream.Nitro.CommandLine.Services.Hook;

namespace ChilliCream.Nitro.CommandLine.Tests.Hook;

/// <summary>
/// Exercises <see cref="ClaudeHooksEditor"/> against the golden "before"
/// settings.json fixtures under
/// <c>test/fixtures/hooks/claude/install/</c> and
/// <c>test/fixtures/hooks/claude/uninstall/</c>: missing, foreign-only,
/// mixed, already-installed, outdated, manually-edited, and (for uninstall)
/// a sidecar-drifted entry. No file I/O, no real <c>~/.claude</c> - pure
/// JSON-text-in, JSON-text-out.
/// </summary>
public sealed class ClaudeHooksEditorTests
{
    private static readonly LaunchDescriptor Descriptor =
        new("/home/agent/.dotnet/tools/nitro", []);

    [Fact]
    public void Install_MissingFile_CreatesAllFourEventsAsInstalled()
    {
        var result = ClaudeHooksEditor.Install(null, Descriptor, DateTimeOffset.UnixEpoch);

        Assert.All(result.Outcomes, o => Assert.Equal(HookInstallOutcome.Installed, o.Outcome));
        Assert.Equal(ClaudeHooksTemplate.Events, result.Outcomes.Select(o => o.Event));

        var root = Parse(result.SettingsJson);
        var hooks = (JsonObject)root["hooks"]!;

        foreach (var claudeEvent in ClaudeHooksTemplate.Events)
        {
            var group = SingleGroup(hooks, claudeEvent);
            AssertCommand(group, ClaudeHooksTemplate.BuildCommand(Descriptor, claudeEvent), 10);
        }

        Assert.Equal(4, result.Sidecar.Count);
        Assert.All(result.Sidecar.Values, e => Assert.Equal(DateTimeOffset.UnixEpoch, e.InstalledAt));
    }

    [Fact]
    public void Install_ForeignOnly_AddsAlongsideWithoutTouchingForeignEntryOrOtherTopLevelKeys()
    {
        var before = ClaudeHooksInstallFixtures.Read("install", "foreign-only.json");
        var beforeRoot = Parse(before);
        var herdrBefore = ((JsonObject)beforeRoot["hooks"]!)["SessionStart"]![0];

        var result = ClaudeHooksEditor.Install(before, Descriptor, DateTimeOffset.UnixEpoch);

        Assert.All(result.Outcomes, o => Assert.Equal(HookInstallOutcome.Installed, o.Outcome));

        var afterRoot = Parse(result.SettingsJson);
        var sessionStart = (JsonArray)((JsonObject)afterRoot["hooks"]!)["SessionStart"]!;

        Assert.Equal(2, sessionStart.Count);
        Assert.True(JsonNode.DeepEquals(herdrBefore, sessionStart[0]));
        AssertCommand(
            (JsonObject)sessionStart[1]!, ClaudeHooksTemplate.BuildCommand(Descriptor, "SessionStart"), 10);

        // Untouched foreign top-level content round-trips unchanged.
        Assert.True(JsonNode.DeepEquals(beforeRoot["someOtherTopLevelKey"], afterRoot["someOtherTopLevelKey"]));
    }

    [Fact]
    public void Install_Mixed_UpdatesOutdatedAddsMissingLeavesForeignAndCurrentAlone()
    {
        var before = ClaudeHooksInstallFixtures.Read("install", "mixed.json");
        var beforeRoot = Parse(before);
        var beforeHooks = (JsonObject)beforeRoot["hooks"]!;
        var herdrBefore = ((JsonArray)beforeHooks["SessionStart"]!)[0];
        var foreignSessionEndBefore = ((JsonArray)beforeHooks["SessionEnd"]!)[0];

        var result = ClaudeHooksEditor.Install(before, Descriptor, DateTimeOffset.UnixEpoch);

        var byEvent = result.Outcomes.ToDictionary(o => o.Event, o => o.Outcome);
        Assert.Equal(HookInstallOutcome.Updated, byEvent["SessionStart"]);
        Assert.Equal(HookInstallOutcome.Unchanged, byEvent["UserPromptSubmit"]);
        Assert.Equal(HookInstallOutcome.Installed, byEvent["Stop"]);
        Assert.Equal(HookInstallOutcome.Installed, byEvent["SessionEnd"]);

        var afterHooks = (JsonObject)Parse(result.SettingsJson)["hooks"]!;

        var sessionStart = (JsonArray)afterHooks["SessionStart"]!;
        Assert.Equal(2, sessionStart.Count);
        Assert.True(JsonNode.DeepEquals(herdrBefore, sessionStart[0]));
        AssertCommand(
            (JsonObject)sessionStart[1]!, ClaudeHooksTemplate.BuildCommand(Descriptor, "SessionStart"), 10);

        var sessionEnd = (JsonArray)afterHooks["SessionEnd"]!;
        Assert.Equal(2, sessionEnd.Count);
        Assert.True(JsonNode.DeepEquals(foreignSessionEndBefore, sessionEnd[0]));
        AssertCommand(
            (JsonObject)sessionEnd[1]!, ClaudeHooksTemplate.BuildCommand(Descriptor, "SessionEnd"), 10);

        AssertCommand(
            SingleGroup(afterHooks, "UserPromptSubmit"),
            ClaudeHooksTemplate.BuildCommand(Descriptor, "UserPromptSubmit"),
            10);
        AssertCommand(SingleGroup(afterHooks, "Stop"), ClaudeHooksTemplate.BuildCommand(Descriptor, "Stop"), 10);
    }

    [Fact]
    public void Install_AlreadyInstalled_AllUnchanged()
    {
        var before = ClaudeHooksInstallFixtures.Read("install", "already-installed.json");

        var result = ClaudeHooksEditor.Install(before, Descriptor, DateTimeOffset.UnixEpoch);

        Assert.All(result.Outcomes, o => Assert.Equal(HookInstallOutcome.Unchanged, o.Outcome));
        Assert.True(JsonNode.DeepEquals(Parse(before), Parse(result.SettingsJson)));
    }

    [Fact]
    public void Install_Outdated_ReplacesStaleCommandOnlyForThatEvent()
    {
        var before = ClaudeHooksInstallFixtures.Read("install", "outdated.json");

        var result = ClaudeHooksEditor.Install(before, Descriptor, DateTimeOffset.UnixEpoch);

        var byEvent = result.Outcomes.ToDictionary(o => o.Event, o => o.Outcome);
        Assert.Equal(HookInstallOutcome.Updated, byEvent["SessionStart"]);
        Assert.Equal(HookInstallOutcome.Installed, byEvent["UserPromptSubmit"]);
        Assert.Equal(HookInstallOutcome.Installed, byEvent["Stop"]);
        Assert.Equal(HookInstallOutcome.Installed, byEvent["SessionEnd"]);

        var hooks = (JsonObject)Parse(result.SettingsJson)["hooks"]!;
        AssertCommand(
            SingleGroup(hooks, "SessionStart"), ClaudeHooksTemplate.BuildCommand(Descriptor, "SessionStart"), 10);
    }

    [Fact]
    public void Install_ManuallyEdited_TreatedIdenticallyToOutdated()
    {
        var before = ClaudeHooksInstallFixtures.Read("install", "manually-edited.json");

        var result = ClaudeHooksEditor.Install(before, Descriptor, DateTimeOffset.UnixEpoch);

        var outcome = Assert.Single(result.Outcomes, o => o.Event == "SessionStart");
        Assert.Equal(HookInstallOutcome.Updated, outcome.Outcome);

        var hooks = (JsonObject)Parse(result.SettingsJson)["hooks"]!;
        AssertCommand(
            SingleGroup(hooks, "SessionStart"), ClaudeHooksTemplate.BuildCommand(Descriptor, "SessionStart"), 10);
    }

    [Fact]
    public void Install_Twice_IsIdempotent()
    {
        var once = ClaudeHooksEditor.Install(null, Descriptor, DateTimeOffset.UnixEpoch).SettingsJson;
        var twice = ClaudeHooksEditor.Install(once, Descriptor, DateTimeOffset.UnixEpoch).SettingsJson;

        Assert.Equal(once, twice);
    }

    [Fact]
    public void Status_ReportsMissingInstalledAndOutdated()
    {
        var missing = ClaudeHooksEditor.Status(null, Descriptor);
        Assert.All(missing, r => Assert.Equal(HookStatusOutcome.Missing, r.Outcome));

        var installed = ClaudeHooksEditor.Status(
            ClaudeHooksInstallFixtures.Read("install", "already-installed.json"), Descriptor);
        Assert.All(installed, r => Assert.Equal(HookStatusOutcome.Installed, r.Outcome));

        var outdated = ClaudeHooksEditor.Status(
            ClaudeHooksInstallFixtures.Read("install", "outdated.json"), Descriptor);
        var sessionStart = Assert.Single(outdated, r => r.Event == "SessionStart");
        Assert.Equal(HookStatusOutcome.Outdated, sessionStart.Outcome);
        Assert.Equal("/opt/old/nitro agent hook claude session-start", sessionStart.InstalledCommand);
        Assert.All(outdated.Where(r => r.Event != "SessionStart"), r => Assert.Equal(HookStatusOutcome.Missing, r.Outcome));

        var manuallyEdited = ClaudeHooksEditor.Status(
            ClaudeHooksInstallFixtures.Read("install", "manually-edited.json"), Descriptor);
        Assert.Equal(HookStatusOutcome.Outdated, Assert.Single(manuallyEdited, r => r.Event == "SessionStart").Outcome);
    }

    [Fact]
    public void Uninstall_MissingFile_AllNotPresent()
    {
        var result = ClaudeHooksEditor.Uninstall(null, new Dictionary<string, ClaudeHooksSidecarEntry>());

        Assert.All(result.Outcomes, o => Assert.Equal(HookUninstallOutcome.NotPresent, o.Outcome));
        Assert.Empty(result.Sidecar);
    }

    [Fact]
    public void Uninstall_WithForeignEntry_RemovesOnlyOwnAndLeavesForeignEntryByteForByte()
    {
        var before = ClaudeHooksInstallFixtures.Read("uninstall", "with-foreign.json");
        var beforeRoot = Parse(before);
        var beforeHooks = (JsonObject)beforeRoot["hooks"]!;
        var herdrBefore = ((JsonArray)beforeHooks["SessionStart"]!)[0];

        var sidecar = SidecarFor(before);

        var result = ClaudeHooksEditor.Uninstall(before, sidecar);

        Assert.All(result.Outcomes, o => Assert.Equal(HookUninstallOutcome.Removed, o.Outcome));
        Assert.Empty(result.Sidecar);

        var afterRoot = Parse(result.SettingsJson);
        var afterHooks = (JsonObject)afterRoot["hooks"]!;

        // Only the herdr-owning event survives, and only the herdr group in it.
        Assert.Equal(["SessionStart"], afterHooks.Select(kv => kv.Key));
        var sessionStart = (JsonArray)afterHooks["SessionStart"]!;
        var herdrAfter = Assert.Single(sessionStart);

        Assert.True(JsonNode.DeepEquals(herdrBefore, herdrAfter));
        Assert.Equal(herdrBefore!.ToJsonString(), herdrAfter!.ToJsonString());
    }

    [Fact]
    public void Uninstall_SidecarDrifted_FallsBackToMarkerMatch()
    {
        var before = ClaudeHooksInstallFixtures.Read("uninstall", "drifted.json");

        // The sidecar remembers the ORIGINAL install's command text, which no
        // longer matches what is on disk (drifted.json's command was hand-
        // edited to a different path after install, while staying
        // marker-owned).
        var staleSidecar = new Dictionary<string, ClaudeHooksSidecarEntry>
        {
            ["SessionStart"] = new(
                ClaudeHooksTemplate.BuildCommand(Descriptor, "SessionStart"),
                10,
                ClaudeHooksSidecarEntry.ComputeHash(ClaudeHooksTemplate.BuildCommand(Descriptor, "SessionStart"), 10),
                DateTimeOffset.UnixEpoch)
        };

        var result = ClaudeHooksEditor.Uninstall(before, staleSidecar);

        var sessionStart = Assert.Single(result.Outcomes, o => o.Event == "SessionStart");
        Assert.Equal(HookUninstallOutcome.Removed, sessionStart.Outcome);

        var root = Parse(result.SettingsJson);
        Assert.Null(root["hooks"]);
    }

    [Fact]
    public void Uninstall_ForeignSessionStartWithNoOwnedGroup_LeavesItInPlace()
    {
        var before = ClaudeHooksInstallFixtures.Read("install", "foreign-only.json");

        var result = ClaudeHooksEditor.Uninstall(before, new Dictionary<string, ClaudeHooksSidecarEntry>());

        var sessionStart = Assert.Single(result.Outcomes, o => o.Event == "SessionStart");
        Assert.Equal(HookUninstallOutcome.NotPresent, sessionStart.Outcome);
        Assert.True(JsonNode.DeepEquals(Parse(before), Parse(result.SettingsJson)));
    }

    private static Dictionary<string, ClaudeHooksSidecarEntry> SidecarFor(string installedJson)
    {
        var hooks = (JsonObject)Parse(installedJson)["hooks"]!;
        var sidecar = new Dictionary<string, ClaudeHooksSidecarEntry>();

        foreach (var claudeEvent in ClaudeHooksTemplate.Events)
        {
            var group = SingleOwnedGroup(hooks, claudeEvent);
            var hook = (JsonObject)((JsonArray)group["hooks"]!)[0]!;
            var command = hook["command"]!.GetValue<string>();
            var timeout = hook["timeout"]!.GetValue<int>();

            sidecar[claudeEvent] = new ClaudeHooksSidecarEntry(
                command, timeout, ClaudeHooksSidecarEntry.ComputeHash(command, timeout), DateTimeOffset.UnixEpoch);
        }

        return sidecar;
    }

    private static JsonObject SingleOwnedGroup(JsonObject hooks, string claudeEvent)
        => (JsonObject)((JsonArray)hooks[claudeEvent]!)
            .Single(g => ((JsonObject)g!)["hooks"]!.AsArray()
                .All(h => ((JsonObject)h!)["command"]!.GetValue<string>()
                    .Contains(ClaudeHooksTemplate.CommandMarker, StringComparison.Ordinal)))!;

    private static JsonObject SingleGroup(JsonObject hooks, string claudeEvent)
    {
        var array = (JsonArray)hooks[claudeEvent]!;
        return (JsonObject)Assert.Single(array)!;
    }

    private static void AssertCommand(JsonObject group, string expectedCommand, int expectedTimeout)
    {
        var hooksArray = (JsonArray)group["hooks"]!;
        var hook = (JsonObject)Assert.Single(hooksArray)!;

        Assert.Equal("command", hook["type"]!.GetValue<string>());
        Assert.Equal(expectedCommand, hook["command"]!.GetValue<string>());
        Assert.Equal(expectedTimeout, hook["timeout"]!.GetValue<int>());
    }

    private static JsonObject Parse(string json) => (JsonObject)JsonNode.Parse(json)!;
}
