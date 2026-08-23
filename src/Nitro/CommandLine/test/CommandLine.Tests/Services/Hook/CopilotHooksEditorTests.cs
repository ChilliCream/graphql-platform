using System.Text.Json.Nodes;
using ChilliCream.Nitro.CommandLine.Services.Hook;

namespace ChilliCream.Nitro.CommandLine.Tests.Hook;

/// <summary>
/// Exercises <see cref="CopilotHooksEditor"/> against the golden "before"
/// hooks-dir fixtures under <c>test/fixtures/hooks/copilot/install/</c> and
/// <c>test/fixtures/hooks/copilot/uninstall/</c>, mirroring
/// <c>CodexHooksEditorTests</c>. The two structural differences exercised
/// here (spike S5 redo, perles-net-k3j.4): the required <c>"hooks"</c>
/// wrapper, and a flat per-event array of hook objects (no Codex-style
/// nested per-group <c>"hooks"</c> array).
/// </summary>
public sealed class CopilotHooksEditorTests
{
    private static readonly LaunchDescriptor Descriptor =
        new("/home/agent/.dotnet/tools/nitro", []);

    [Fact]
    public void Install_MissingFile_CreatesAllThreeEventsAsInstalled()
    {
        var result = CopilotHooksEditor.Install(null, Descriptor, DateTimeOffset.UnixEpoch);

        Assert.All(result.Outcomes, o => Assert.Equal(HookInstallOutcome.Installed, o.Outcome));
        Assert.Equal(CopilotHooksTemplate.Events, result.Outcomes.Select(o => o.Event));

        var root = Parse(result.HooksJson);
        var hooks = (JsonObject)root["hooks"]!;

        foreach (var copilotEvent in CopilotHooksTemplate.Events)
        {
            var hook = SingleHook(hooks, copilotEvent);
            AssertHook(hook, CopilotHooksTemplate.BuildCommand(Descriptor, copilotEvent), 10);
        }

        Assert.Equal(3, result.Sidecar.Count);
        Assert.All(result.Sidecar.Values, e => Assert.Equal(DateTimeOffset.UnixEpoch, e.InstalledAt));
    }

    [Fact]
    public void Install_ForeignOnly_AddsAlongsideWithoutTouchingForeignEntryOrOtherTopLevelKeys()
    {
        var before = CopilotHooksInstallFixtures.Read("install", "foreign-only.json");
        var beforeRoot = Parse(before);
        var herdrBefore = ((JsonArray)((JsonObject)beforeRoot["hooks"]!)["sessionStart"]!)[0];

        var result = CopilotHooksEditor.Install(before, Descriptor, DateTimeOffset.UnixEpoch);

        Assert.All(result.Outcomes, o => Assert.Equal(HookInstallOutcome.Installed, o.Outcome));

        var afterRoot = Parse(result.HooksJson);
        var sessionStart = (JsonArray)((JsonObject)afterRoot["hooks"]!)["sessionStart"]!;

        Assert.Equal(2, sessionStart.Count);
        Assert.True(JsonNode.DeepEquals(herdrBefore, sessionStart[0]));
        AssertHook(
            (JsonObject)sessionStart[1]!, CopilotHooksTemplate.BuildCommand(Descriptor, "sessionStart"), 10);

        Assert.True(JsonNode.DeepEquals(beforeRoot["someOtherTopLevelKey"], afterRoot["someOtherTopLevelKey"]));
    }

    [Fact]
    public void Install_Mixed_UpdatesOutdatedAddsMissingLeavesForeignAlone()
    {
        var before = CopilotHooksInstallFixtures.Read("install", "mixed.json");
        var beforeRoot = Parse(before);
        var herdrBefore = ((JsonArray)((JsonObject)beforeRoot["hooks"]!)["sessionStart"]!)[0];

        var result = CopilotHooksEditor.Install(before, Descriptor, DateTimeOffset.UnixEpoch);

        var byEvent = result.Outcomes.ToDictionary(o => o.Event, o => o.Outcome);
        Assert.Equal(HookInstallOutcome.Updated, byEvent["sessionStart"]);
        Assert.Equal(HookInstallOutcome.Unchanged, byEvent["userPromptSubmitted"]);
        Assert.Equal(HookInstallOutcome.Installed, byEvent["sessionEnd"]);

        var afterRoot = Parse(result.HooksJson);
        var hooks = (JsonObject)afterRoot["hooks"]!;

        var sessionStart = (JsonArray)hooks["sessionStart"]!;
        Assert.Equal(2, sessionStart.Count);
        Assert.True(JsonNode.DeepEquals(herdrBefore, sessionStart[0]));
        AssertHook(
            (JsonObject)sessionStart[1]!, CopilotHooksTemplate.BuildCommand(Descriptor, "sessionStart"), 10);

        AssertHook(
            SingleHook(hooks, "userPromptSubmitted"),
            CopilotHooksTemplate.BuildCommand(Descriptor, "userPromptSubmitted"),
            10);
        AssertHook(
            SingleHook(hooks, "sessionEnd"), CopilotHooksTemplate.BuildCommand(Descriptor, "sessionEnd"), 10);
    }

    [Fact]
    public void Install_AlreadyInstalled_AllUnchanged()
    {
        var before = CopilotHooksInstallFixtures.Read("install", "already-installed.json");

        var result = CopilotHooksEditor.Install(before, Descriptor, DateTimeOffset.UnixEpoch);

        Assert.All(result.Outcomes, o => Assert.Equal(HookInstallOutcome.Unchanged, o.Outcome));
        Assert.True(JsonNode.DeepEquals(Parse(before), Parse(result.HooksJson)));
    }

    [Fact]
    public void Install_Outdated_ReplacesStaleCommandOnlyForThatEvent()
    {
        var before = CopilotHooksInstallFixtures.Read("install", "outdated.json");

        var result = CopilotHooksEditor.Install(before, Descriptor, DateTimeOffset.UnixEpoch);

        var byEvent = result.Outcomes.ToDictionary(o => o.Event, o => o.Outcome);
        Assert.Equal(HookInstallOutcome.Updated, byEvent["sessionStart"]);
        Assert.Equal(HookInstallOutcome.Installed, byEvent["userPromptSubmitted"]);
        Assert.Equal(HookInstallOutcome.Installed, byEvent["sessionEnd"]);

        var root = Parse(result.HooksJson);
        AssertHook(
            SingleHook((JsonObject)root["hooks"]!, "sessionStart"),
            CopilotHooksTemplate.BuildCommand(Descriptor, "sessionStart"),
            10);
    }

    [Fact]
    public void Install_ManuallyEdited_TreatedIdenticallyToOutdated()
    {
        var before = CopilotHooksInstallFixtures.Read("install", "manually-edited.json");

        var result = CopilotHooksEditor.Install(before, Descriptor, DateTimeOffset.UnixEpoch);

        var outcome = Assert.Single(result.Outcomes, o => o.Event == "sessionStart");
        Assert.Equal(HookInstallOutcome.Updated, outcome.Outcome);

        var root = Parse(result.HooksJson);
        AssertHook(
            SingleHook((JsonObject)root["hooks"]!, "sessionStart"),
            CopilotHooksTemplate.BuildCommand(Descriptor, "sessionStart"),
            10);
    }

    [Fact]
    public void Install_Twice_IsIdempotent()
    {
        var once = CopilotHooksEditor.Install(null, Descriptor, DateTimeOffset.UnixEpoch).HooksJson;
        var twice = CopilotHooksEditor.Install(once, Descriptor, DateTimeOffset.UnixEpoch).HooksJson;

        Assert.Equal(once, twice);
    }

    [Fact]
    public void Install_BareUnwrappedShape_FixesItIntoTheRequiredHooksWrapper()
    {
        // Spike S5 redo, perles-net-k3j.4: a bare {"<event>": [...]} file (no
        // "hooks" wrapper) is a live, logged Copilot parse error that loads
        // zero hooks - this installer must never write that shape, and must
        // repair it if it somehow finds it on disk (this file is always
        // Nitro's own dedicated filename).
        const string bare = """{"sessionStart": [{"type": "command", "command": "/bin/true", "timeoutSec": 5}]}""";

        var result = CopilotHooksEditor.Install(bare, Descriptor, DateTimeOffset.UnixEpoch);

        var root = Parse(result.HooksJson);
        Assert.NotNull(root["hooks"]);
        Assert.All(result.Outcomes, o => Assert.Equal(HookInstallOutcome.Installed, o.Outcome));
    }

    [Fact]
    public void Status_ReportsMissingInstalledAndOutdated()
    {
        var missing = CopilotHooksEditor.Status(null, Descriptor);
        Assert.All(missing, r => Assert.Equal(HookStatusOutcome.Missing, r.Outcome));

        var installed = CopilotHooksEditor.Status(
            CopilotHooksInstallFixtures.Read("install", "already-installed.json"), Descriptor);
        Assert.All(installed, r => Assert.Equal(HookStatusOutcome.Installed, r.Outcome));

        var outdated = CopilotHooksEditor.Status(
            CopilotHooksInstallFixtures.Read("install", "outdated.json"), Descriptor);
        var sessionStart = Assert.Single(outdated, r => r.Event == "sessionStart");
        Assert.Equal(HookStatusOutcome.Outdated, sessionStart.Outcome);
        Assert.Equal("/opt/old/nitro agent hook copilot session-start", sessionStart.InstalledCommand);
        Assert.All(
            outdated.Where(r => r.Event != "sessionStart"), r => Assert.Equal(HookStatusOutcome.Missing, r.Outcome));
    }

    [Fact]
    public void Uninstall_MissingFile_AllNotPresent()
    {
        var result = CopilotHooksEditor.Uninstall(null, new Dictionary<string, CopilotHooksSidecarEntry>());

        Assert.All(result.Outcomes, o => Assert.Equal(HookUninstallOutcome.NotPresent, o.Outcome));
        Assert.Empty(result.Sidecar);
    }

    [Fact]
    public void Uninstall_WithForeignEntry_RemovesOnlyOwnAndLeavesForeignEntryByteForByte()
    {
        var before = CopilotHooksInstallFixtures.Read("uninstall", "with-foreign.json");
        var beforeRoot = Parse(before);
        var herdrBefore = ((JsonArray)((JsonObject)beforeRoot["hooks"]!)["sessionStart"]!)[0];

        var sidecar = SidecarFor(before);

        var result = CopilotHooksEditor.Uninstall(before, sidecar);

        Assert.All(result.Outcomes, o => Assert.Equal(HookUninstallOutcome.Removed, o.Outcome));
        Assert.Empty(result.Sidecar);

        var afterRoot = Parse(result.HooksJson);
        var hooks = (JsonObject)afterRoot["hooks"]!;

        Assert.Equal(["sessionStart"], hooks.Select(kv => kv.Key));
        var sessionStart = (JsonArray)hooks["sessionStart"]!;
        var herdrAfter = Assert.Single(sessionStart);

        Assert.True(JsonNode.DeepEquals(herdrBefore, herdrAfter));
        Assert.Equal(herdrBefore!.ToJsonString(), herdrAfter!.ToJsonString());
    }

    [Fact]
    public void Uninstall_ForeignSessionStartWithNoOwnedEntry_LeavesItInPlace()
    {
        var before = CopilotHooksInstallFixtures.Read("install", "foreign-only.json");

        var result = CopilotHooksEditor.Uninstall(before, new Dictionary<string, CopilotHooksSidecarEntry>());

        var sessionStart = Assert.Single(result.Outcomes, o => o.Event == "sessionStart");
        Assert.Equal(HookUninstallOutcome.NotPresent, sessionStart.Outcome);
        Assert.True(JsonNode.DeepEquals(Parse(before), Parse(result.HooksJson)));
    }

    private static Dictionary<string, CopilotHooksSidecarEntry> SidecarFor(string installedJson)
    {
        var root = Parse(installedJson);
        var hooks = (JsonObject)root["hooks"]!;
        var sidecar = new Dictionary<string, CopilotHooksSidecarEntry>();

        foreach (var copilotEvent in CopilotHooksTemplate.Events)
        {
            var hook = SingleOwnedHook(hooks, copilotEvent);
            var command = hook["command"]!.GetValue<string>();
            var timeout = hook["timeoutSec"]!.GetValue<int>();

            sidecar[copilotEvent] = new CopilotHooksSidecarEntry(
                command, timeout, CopilotHooksSidecarEntry.ComputeHash(command, timeout), DateTimeOffset.UnixEpoch);
        }

        return sidecar;
    }

    private static JsonObject SingleOwnedHook(JsonObject hooks, string copilotEvent)
        => (JsonObject)((JsonArray)hooks[copilotEvent]!)
            .Single(h => ((JsonObject)h!)["command"]!.GetValue<string>()
                .Contains(CopilotHooksTemplate.CommandMarker, StringComparison.Ordinal))!;

    private static JsonObject SingleHook(JsonObject hooks, string copilotEvent)
    {
        var array = (JsonArray)hooks[copilotEvent]!;
        return (JsonObject)Assert.Single(array)!;
    }

    private static void AssertHook(JsonObject hook, string expectedCommand, int expectedTimeout)
    {
        Assert.Equal("command", hook["type"]!.GetValue<string>());
        Assert.Equal(expectedCommand, hook["command"]!.GetValue<string>());
        Assert.Equal(expectedTimeout, hook["timeoutSec"]!.GetValue<int>());
    }

    private static JsonObject Parse(string json) => (JsonObject)JsonNode.Parse(json)!;
}
