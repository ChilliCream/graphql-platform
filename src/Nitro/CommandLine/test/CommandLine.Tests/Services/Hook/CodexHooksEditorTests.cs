using System.Text.Json.Nodes;
using ChilliCream.Nitro.CommandLine.Services.Hook;

namespace ChilliCream.Nitro.CommandLine.Tests.Hook;

/// <summary>
/// Exercises <see cref="CodexHooksEditor"/> against the golden "before"
/// <c>hooks.json</c> fixtures under <c>test/fixtures/hooks/codex/install/</c>
/// and <c>test/fixtures/hooks/codex/uninstall/</c>, mirroring
/// <c>ClaudeHooksEditorTests</c>. The one structural difference exercised
/// here: <c>hooks.json</c> contains its event map under a top-level
/// <c>"hooks"</c> key.
/// </summary>
public sealed class CodexHooksEditorTests
{
    private static readonly LaunchDescriptor Descriptor =
        new("/home/agent/.dotnet/tools/nitro", []);

    [Fact]
    public void Install_MissingFile_CreatesAllThreeEventsAsInstalled()
    {
        var result = CodexHooksEditor.Install(null, Descriptor, DateTimeOffset.UnixEpoch);

        Assert.All(result.Outcomes, o => Assert.Equal(HookInstallOutcome.Installed, o.Outcome));
        Assert.Equal(CodexHooksTemplate.Events, result.Outcomes.Select(o => o.Event));

        var root = Parse(result.HooksJson);

        foreach (var codexEvent in CodexHooksTemplate.Events)
        {
            var group = SingleGroup(root, codexEvent);
            AssertCommand(group, CodexHooksTemplate.BuildCommand(Descriptor, codexEvent), 10);
        }

        Assert.Equal(3, result.Sidecar.Count);
        Assert.All(result.Sidecar.Values, e => Assert.Equal(DateTimeOffset.UnixEpoch, e.InstalledAt));

        Assert.NotNull(root["hooks"]);
    }

    [Fact]
    public void Install_ForeignOnly_AddsAlongsideWithoutTouchingForeignEntryOrOtherTopLevelKeys()
    {
        var before = CodexHooksInstallFixtures.Read("install", "foreign-only.json");
        var beforeRoot = Parse(before);
        var herdrBefore = ((JsonArray)Hooks(beforeRoot)["SessionStart"]!)[0];

        var result = CodexHooksEditor.Install(before, Descriptor, DateTimeOffset.UnixEpoch);

        Assert.All(result.Outcomes, o => Assert.Equal(HookInstallOutcome.Installed, o.Outcome));

        var afterRoot = Parse(result.HooksJson);
        var sessionStart = (JsonArray)Hooks(afterRoot)["SessionStart"]!;

        Assert.Equal(2, sessionStart.Count);
        Assert.True(JsonNode.DeepEquals(herdrBefore, sessionStart[0]));
        AssertCommand(
            (JsonObject)sessionStart[1]!, CodexHooksTemplate.BuildCommand(Descriptor, "SessionStart"), 10);

        Assert.True(JsonNode.DeepEquals(beforeRoot["someOtherTopLevelKey"], afterRoot["someOtherTopLevelKey"]));
    }

    [Fact]
    public void Install_Mixed_UpdatesOutdatedAddsMissingLeavesForeignAlone()
    {
        var before = CodexHooksInstallFixtures.Read("install", "mixed.json");
        var beforeRoot = Parse(before);
        var herdrBefore = ((JsonArray)Hooks(beforeRoot)["SessionStart"]!)[0];

        var result = CodexHooksEditor.Install(before, Descriptor, DateTimeOffset.UnixEpoch);

        var byEvent = result.Outcomes.ToDictionary(o => o.Event, o => o.Outcome);
        Assert.Equal(HookInstallOutcome.Updated, byEvent["SessionStart"]);
        Assert.Equal(HookInstallOutcome.Unchanged, byEvent["UserPromptSubmit"]);
        Assert.Equal(HookInstallOutcome.Installed, byEvent["SessionEnd"]);

        var afterRoot = Parse(result.HooksJson);

        var sessionStart = (JsonArray)Hooks(afterRoot)["SessionStart"]!;
        Assert.Equal(2, sessionStart.Count);
        Assert.True(JsonNode.DeepEquals(herdrBefore, sessionStart[0]));
        AssertCommand(
            (JsonObject)sessionStart[1]!, CodexHooksTemplate.BuildCommand(Descriptor, "SessionStart"), 10);

        AssertCommand(
            SingleGroup(afterRoot, "UserPromptSubmit"),
            CodexHooksTemplate.BuildCommand(Descriptor, "UserPromptSubmit"),
            10);
        AssertCommand(
            SingleGroup(afterRoot, "SessionEnd"), CodexHooksTemplate.BuildCommand(Descriptor, "SessionEnd"), 10);
    }

    [Fact]
    public void Install_AlreadyInstalled_AllUnchanged()
    {
        var before = CodexHooksInstallFixtures.Read("install", "already-installed.json");

        var result = CodexHooksEditor.Install(before, Descriptor, DateTimeOffset.UnixEpoch);

        Assert.All(result.Outcomes, o => Assert.Equal(HookInstallOutcome.Unchanged, o.Outcome));
        Assert.True(JsonNode.DeepEquals(Parse(before), Parse(result.HooksJson)));
    }

    [Fact]
    public void Install_Outdated_ReplacesStaleCommandOnlyForThatEvent()
    {
        var before = CodexHooksInstallFixtures.Read("install", "outdated.json");

        var result = CodexHooksEditor.Install(before, Descriptor, DateTimeOffset.UnixEpoch);

        var byEvent = result.Outcomes.ToDictionary(o => o.Event, o => o.Outcome);
        Assert.Equal(HookInstallOutcome.Updated, byEvent["SessionStart"]);
        Assert.Equal(HookInstallOutcome.Installed, byEvent["UserPromptSubmit"]);
        Assert.Equal(HookInstallOutcome.Installed, byEvent["SessionEnd"]);

        var root = Parse(result.HooksJson);
        AssertCommand(
            SingleGroup(root, "SessionStart"), CodexHooksTemplate.BuildCommand(Descriptor, "SessionStart"), 10);
    }

    [Fact]
    public void Install_ManuallyEdited_TreatedIdenticallyToOutdated()
    {
        var before = CodexHooksInstallFixtures.Read("install", "manually-edited.json");

        var result = CodexHooksEditor.Install(before, Descriptor, DateTimeOffset.UnixEpoch);

        var outcome = Assert.Single(result.Outcomes, o => o.Event == "SessionStart");
        Assert.Equal(HookInstallOutcome.Updated, outcome.Outcome);

        var root = Parse(result.HooksJson);
        AssertCommand(
            SingleGroup(root, "SessionStart"), CodexHooksTemplate.BuildCommand(Descriptor, "SessionStart"), 10);
    }

    [Fact]
    public void Install_Twice_IsIdempotent()
    {
        var once = CodexHooksEditor.Install(null, Descriptor, DateTimeOffset.UnixEpoch).HooksJson;
        var twice = CodexHooksEditor.Install(once, Descriptor, DateTimeOffset.UnixEpoch).HooksJson;

        Assert.Equal(once, twice);
    }

    [Fact]
    public void Status_ReportsMissingInstalledAndOutdated()
    {
        var missing = CodexHooksEditor.Status(null, Descriptor);
        Assert.All(missing, r => Assert.Equal(HookStatusOutcome.Missing, r.Outcome));

        var installed = CodexHooksEditor.Status(
            CodexHooksInstallFixtures.Read("install", "already-installed.json"), Descriptor);
        Assert.All(installed, r => Assert.Equal(HookStatusOutcome.Installed, r.Outcome));

        var outdated = CodexHooksEditor.Status(
            CodexHooksInstallFixtures.Read("install", "outdated.json"), Descriptor);
        var sessionStart = Assert.Single(outdated, r => r.Event == "SessionStart");
        Assert.Equal(HookStatusOutcome.Outdated, sessionStart.Outcome);
        Assert.Equal("/opt/old/nitro agent hook codex session-start", sessionStart.InstalledCommand);
        Assert.All(
            outdated.Where(r => r.Event != "SessionStart"), r => Assert.Equal(HookStatusOutcome.Missing, r.Outcome));
    }

    [Fact]
    public void Uninstall_MissingFile_AllNotPresent()
    {
        var result = CodexHooksEditor.Uninstall(null, new Dictionary<string, CodexHooksSidecarEntry>());

        Assert.All(result.Outcomes, o => Assert.Equal(HookUninstallOutcome.NotPresent, o.Outcome));
        Assert.Empty(result.Sidecar);
    }

    [Fact]
    public void Uninstall_WithForeignEntry_RemovesOnlyOwnAndLeavesForeignEntryByteForByte()
    {
        var before = CodexHooksInstallFixtures.Read("uninstall", "with-foreign.json");
        var beforeRoot = Parse(before);
        var herdrBefore = ((JsonArray)Hooks(beforeRoot)["SessionStart"]!)[0];

        var sidecar = SidecarFor(before);

        var result = CodexHooksEditor.Uninstall(before, sidecar);

        Assert.All(result.Outcomes, o => Assert.Equal(HookUninstallOutcome.Removed, o.Outcome));
        Assert.Empty(result.Sidecar);

        var afterRoot = Parse(result.HooksJson);

        Assert.Equal(["hooks"], afterRoot.Select(kv => kv.Key));
        var sessionStart = (JsonArray)Hooks(afterRoot)["SessionStart"]!;
        var herdrAfter = Assert.Single(sessionStart);

        Assert.True(JsonNode.DeepEquals(herdrBefore, herdrAfter));
        Assert.Equal(herdrBefore!.ToJsonString(), herdrAfter!.ToJsonString());
    }

    [Fact]
    public void Uninstall_ForeignSessionStartWithNoOwnedGroup_LeavesItInPlace()
    {
        var before = CodexHooksInstallFixtures.Read("install", "foreign-only.json");

        var result = CodexHooksEditor.Uninstall(before, new Dictionary<string, CodexHooksSidecarEntry>());

        var sessionStart = Assert.Single(result.Outcomes, o => o.Event == "SessionStart");
        Assert.Equal(HookUninstallOutcome.NotPresent, sessionStart.Outcome);
        Assert.True(JsonNode.DeepEquals(Parse(before), Parse(result.HooksJson)));
    }

    private static Dictionary<string, CodexHooksSidecarEntry> SidecarFor(string installedJson)
    {
        var root = Parse(installedJson);
        var sidecar = new Dictionary<string, CodexHooksSidecarEntry>();

        foreach (var codexEvent in CodexHooksTemplate.Events)
        {
            var group = SingleOwnedGroup(root, codexEvent);
            var hook = (JsonObject)((JsonArray)group["hooks"]!)[0]!;
            var command = hook["command"]!.GetValue<string>();
            var timeout = hook["timeout"]!.GetValue<int>();

            sidecar[codexEvent] = new CodexHooksSidecarEntry(
                command, timeout, CodexHooksSidecarEntry.ComputeHash(command, timeout), DateTimeOffset.UnixEpoch);
        }

        return sidecar;
    }

    private static JsonObject SingleOwnedGroup(JsonObject root, string codexEvent)
        => (JsonObject)((JsonArray)Hooks(root)[codexEvent]!)
            .Single(g => ((JsonObject)g!)["hooks"]!.AsArray()
                .All(h => ((JsonObject)h!)["command"]!.GetValue<string>()
                    .Contains(CodexHooksTemplate.CommandMarker, StringComparison.Ordinal)))!;

    private static JsonObject SingleGroup(JsonObject root, string codexEvent)
    {
        var array = (JsonArray)Hooks(root)[codexEvent]!;
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

    private static JsonObject Hooks(JsonObject root) => (JsonObject)root["hooks"]!;
}
