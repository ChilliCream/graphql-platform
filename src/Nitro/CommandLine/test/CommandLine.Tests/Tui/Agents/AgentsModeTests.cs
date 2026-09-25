using ChilliCream.Nitro.CommandLine.Services.Workspace;
using ChilliCream.Nitro.CommandLine.Tests.Tui.Shell;
using ChilliCream.Nitro.CommandLine.Tui.Agents;
using ChilliCream.Nitro.CommandLine.Tui.Input;
using Microsoft.Extensions.Time.Testing;
using Spectre.Console;
using Spectre.Console.Testing;
using CursorDirection = ChilliCream.Nitro.CommandLine.Tui.Input.CursorDirection;
using static ChilliCream.Nitro.CommandLine.Tests.Tui.AnsiAssertions;

namespace ChilliCream.Nitro.CommandLine.Tests.Tui.Agents;

public sealed class AgentsModeTests
{
    private static readonly DateTimeOffset s_now = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    /// <summary>
    /// Builds an <see cref="AgentsMode"/>, defaulting the mail, task, and memory stores the
    /// agent detail popover needs when a test does not care about them.
    /// </summary>
    private static AgentsMode CreateMode(
        FakeAgentStore store,
        FakeTimeProvider time,
        FakeMailStore? mailStore = null,
        FakeTaskStore? taskStore = null,
        FakeMemoryStore? memoryStore = null) =>
        new(
            store,
            mailStore ?? new FakeMailStore(),
            taskStore ?? new FakeTaskStore(),
            memoryStore ?? new FakeMemoryStore(),
            time);

    private static ConsoleKeyInfo Key(char c) => new(c, ConsoleKey.NoName, false, false, false);

    private static ConsoleKeyInfo CtrlKey(ConsoleKey key) => new('\0', key, false, false, true);

    private static void Type(AgentsMode mode, string text)
    {
        foreach (var c in text)
        {
            mode.HandleRawKey(Key(c));
        }
    }

    private static string RenderToText(AgentsMode mode, int width = 100, int height = 24)
    {
        var console = new TestConsole().Width(width);
        console.Write(mode.Render(width, height));
        return console.Output;
    }

    private static string RenderToAnsiText(AgentsMode mode, int width = 100, int height = 24)
    {
        var console = new TestConsole().Colors(ColorSystem.TrueColor).EmitAnsiSequences().Width(width);
        console.Write(mode.Render(width, height));
        return console.Output;
    }

    private static AgentRow AddOnlineAgent(FakeAgentStore store, string sessionId)
        => store.StartSessionAsync(
                new AgentSessionStartRequest
                {
                    Harness = AgentSessionHarness.ClaudeCode,
                    SessionId = sessionId,
                    HarnessVersion = "1.0.0",
                    Cwd = "",
                    WorkspacePath = "",
                    EndpointKind = AgentSessionEndpointKind.ClaudePeer,
                    EndpointAddr = "peer-1"
                },
                TestContext.Current.CancellationToken)
            .GetAwaiter().GetResult().Row!;

    private static AgentRow AddUnreachableAgent(FakeAgentStore store)
        => store.LoginAsync(TestContext.Current.CancellationToken).GetAwaiter().GetResult();

    private static AgentRow AddOfflineAgent(FakeAgentStore store, string sessionId)
    {
        var row = AddOnlineAgent(store, sessionId);
        store.EndSessionAsync(row.Harness!, row.SessionId!, TestContext.Current.CancellationToken)
            .GetAwaiter().GetResult();
        return row;
    }

    private static void Touch(FakeAgentStore store, string name)
        => store.TouchAsync(name, TestContext.Current.CancellationToken).GetAwaiter().GetResult();

    /// <summary>
    /// Seeds an online agent last seen at exactly <paramref name="lastSeenAt"/>.
    /// </summary>
    private static AgentRow AddOnlineAgentSeenAt(FakeAgentStore store, string name, DateTimeOffset lastSeenAt)
    {
        var row = new AgentRow
        {
            Name = name,
            Role = string.Empty,
            Harness = AgentSessionHarness.ClaudeCode,
            HarnessVersion = "1.0.0",
            SessionId = $"session-{name}",
            Cwd = string.Empty,
            WorkspacePath = string.Empty,
            RegisteredAt = lastSeenAt,
            StartedAt = lastSeenAt,
            LastSeenAt = lastSeenAt,
            EndpointKind = AgentSessionEndpointKind.ClaudePeer,
            EndpointAddr = "peer-1",
            BlockBudgetUsed = 0,
            AnnouncementPending = false,
            IdlePushArmed = false
        };

        store.Seed(row);
        return row;
    }

    /// <summary>
    /// Seeds an offline agent (its session already ended) last seen at exactly
    /// <paramref name="lastSeenAt"/>.
    /// </summary>
    private static AgentRow AddOfflineAgentSeenAt(FakeAgentStore store, string name, DateTimeOffset lastSeenAt)
    {
        var row = new AgentRow
        {
            Name = name,
            Role = string.Empty,
            Harness = AgentSessionHarness.ClaudeCode,
            HarnessVersion = "1.0.0",
            SessionId = $"session-{name}",
            Cwd = string.Empty,
            WorkspacePath = string.Empty,
            RegisteredAt = lastSeenAt,
            StartedAt = lastSeenAt,
            LastSeenAt = lastSeenAt,
            EndedAt = lastSeenAt,
            EndpointKind = AgentSessionEndpointKind.ClaudePeer,
            EndpointAddr = "peer-1",
            BlockBudgetUsed = 0,
            AnnouncementPending = false,
            IdlePushArmed = false
        };

        store.Seed(row);
        return row;
    }

    [Fact]
    public void Render_Should_ListOnlineBeforeUnreachableBeforeOffline_When_AllThreeStatesArePresent()
    {
        // arrange
        var time = new FakeTimeProvider(s_now);
        var store = new FakeAgentStore(time);
        var offline = AddOfflineAgent(store, "s-offline");
        var unreachable = AddUnreachableAgent(store);
        var online = AddOnlineAgent(store, "s-online");
        var mode = CreateMode(store, time);
        mode.OnEnter();

        // act
        var text = RenderToText(mode);

        // assert
        var onlineIndex = text.IndexOf(online.Name, StringComparison.Ordinal);
        var unreachableIndex = text.IndexOf(unreachable.Name, StringComparison.Ordinal);
        var offlineIndex = text.IndexOf(offline.Name, StringComparison.Ordinal);
        Assert.True(onlineIndex >= 0 && unreachableIndex > onlineIndex && offlineIndex > unreachableIndex);
    }

    [Fact]
    public void Render_Should_ShowTheOnlinePresenceToken_When_AgentIsOnline()
    {
        // arrange
        var time = new FakeTimeProvider(s_now);
        var store = new FakeAgentStore(time);
        AddOnlineAgent(store, "s-online");
        AddOfflineAgent(store, "s-offline");
        var mode = CreateMode(store, time);
        mode.OnEnter();

        // act
        // Select the offline row first so the online row's bubble is not selection-highlighted.
        mode.Handle(new TuiMessage.MoveCursor(CursorDirection.Down));
        var text = RenderToAnsiText(mode);

        // assert
        AssertAnsiStylePrefixesText(text, "agents.list.presence.online", "●");
    }

    [Fact]
    public void Render_Should_ShowTheOfflinePresenceToken_When_AgentIsOffline()
    {
        // arrange
        var time = new FakeTimeProvider(s_now);
        var store = new FakeAgentStore(time);
        AddOfflineAgent(store, "s-offline");
        AddOnlineAgent(store, "s-online");
        var mode = CreateMode(store, time);
        mode.OnEnter();

        // act
        // The default selection sits on the online row, sorted first, not on this one.
        var text = RenderToAnsiText(mode);

        // assert
        AssertAnsiStylePrefixesText(text, "agents.list.presence.offline", "●");
    }

    [Fact]
    public void Render_Should_ShowTheUnreachablePresenceToken_When_AgentIsLoginOnly()
    {
        // arrange
        var time = new FakeTimeProvider(s_now);
        var store = new FakeAgentStore(time);
        AddUnreachableAgent(store);
        AddOnlineAgent(store, "s-online");
        var mode = CreateMode(store, time);
        mode.OnEnter();

        // act
        var text = RenderToAnsiText(mode);

        // assert
        AssertAnsiStylePrefixesText(text, "agents.list.presence.unreachable", "●");
    }

    [Fact]
    public void Render_Should_ShowOnlineCountAndTotal_When_HeaderIsRendered()
    {
        // arrange
        var time = new FakeTimeProvider(s_now);
        var store = new FakeAgentStore(time);
        AddOnlineAgent(store, "s-a");
        AddOnlineAgent(store, "s-b");
        AddOfflineAgent(store, "s-c");
        var mode = CreateMode(store, time);
        mode.OnEnter();

        // act
        var text = RenderToText(mode);

        // assert
        Assert.Contains("Agents (2 online / 3)", text);
    }

    [Fact]
    public void Render_Should_ShowTheEmptyStateMessage_When_NoAgentsExist()
    {
        // arrange
        var time = new FakeTimeProvider(s_now);
        var store = new FakeAgentStore(time);
        var mode = CreateMode(store, time);
        mode.OnEnter();

        // act
        var text = RenderToText(mode);

        // assert
        Assert.Contains("No agents yet.", text);
    }

    [Fact]
    public void Render_Should_KeepOnlyNameAndRole_When_WidthIsTooNarrowForEveryOtherColumn()
    {
        // arrange
        var time = new FakeTimeProvider(s_now);
        var store = new FakeAgentStore(time);
        var agent = AddOnlineAgent(store, "s-a");
        time.Advance(TimeSpan.FromMinutes(10));
        Touch(store, agent.Name);
        var mode = CreateMode(store, time);
        mode.OnEnter();
        var wide = RenderToText(mode, width: 100);

        // act
        // At this width, Started, Harness and Last Seen all drop before Name or Role would.
        var narrow = RenderToText(mode, width: 35);
        var actual = (
            HasName: narrow.Contains(agent.Name, StringComparison.Ordinal),
            HasHarness: narrow.Contains("Claude Code", StringComparison.Ordinal),
            HasStartedAge: narrow.Contains("10m ago", StringComparison.Ordinal),
            HasLastSeenAge: narrow.Contains("just now", StringComparison.Ordinal));

        // assert
        Assert.Contains("10m ago", wide);
        Assert.Equal((true, false, false, false), actual);
    }

    [Fact]
    public void Render_Should_ShowAnUpdatedAgeAndBubble_When_TimeAdvancesPastTheOnlineWindowWithoutARefresh()
    {
        // arrange
        var time = new FakeTimeProvider(s_now);
        var store = new FakeAgentStore(time);
        AddOnlineAgent(store, "s-a");
        AddOnlineAgent(store, "s-b");
        var mode = CreateMode(store, time);
        mode.OnEnter();
        // Move off row 0 so its bubble is never selection-highlighted, before or after the tick.
        mode.Handle(new TuiMessage.MoveCursor(CursorDirection.Down));
        var initialText = RenderToText(mode);

        // act
        // Advance the clock 31 minutes with no refresh in between.
        time.Advance(TimeSpan.FromMinutes(31));
        var text = RenderToAnsiText(mode);

        // assert
        Assert.Contains("just now", initialText);
        Assert.Contains("31m ago", RenderToText(mode));
        AssertAnsiStylePrefixesText(text, "agents.list.presence.offline", "●");
    }

    [Fact]
    public void Tick_Should_MoveTheSelectedAgentToTheOfflineGroupByName_When_ItsOnlineWindowElapses()
    {
        // arrange
        var time = new FakeTimeProvider(s_now);
        var store = new FakeAgentStore(time);
        var stale = AddOnlineAgent(store, "s-a");
        time.Advance(TimeSpan.FromMinutes(25));
        var alreadyOffline = AddOfflineAgent(store, "s-off");
        time.Advance(TimeSpan.FromMinutes(4));
        var fresh = AddOnlineAgent(store, "s-c");
        var mode = CreateMode(store, time);
        mode.OnEnter();
        mode.Handle(new TuiMessage.MoveCursor(CursorDirection.Down));
        Assert.Equal(stale.Name, mode.State.SelectedAgent?.Name);

        // act
        // Only the stale agent's online window elapses; the other two keep their state.
        time.Advance(TimeSpan.FromMinutes(2));
        var dirty = mode.Tick();
        var text = RenderToText(mode);

        // assert
        var freshIndex = text.IndexOf(fresh.Name, StringComparison.Ordinal);
        var offlineIndex = text.IndexOf(alreadyOffline.Name, StringComparison.Ordinal);
        var staleIndex = text.IndexOf(stale.Name, StringComparison.Ordinal);
        Assert.True(dirty && freshIndex >= 0 && offlineIndex > freshIndex && staleIndex > offlineIndex);
        Assert.Equal(stale.Name, mode.State.SelectedAgent?.Name);
    }

    [Fact]
    public void Rows_Should_OrderByNameAscending_When_TwoOnlineAgentsLastSeenFallInTheSameFiveMinuteWindow()
    {
        // arrange
        // 10:01 and 10:04 share the 10:00 window
        var windowStart = new DateTimeOffset(2026, 1, 1, 10, 0, 0, TimeSpan.Zero);
        var time = new FakeTimeProvider(windowStart.AddMinutes(6));
        var store = new FakeAgentStore(time);
        AddOnlineAgentSeenAt(store, "zed", windowStart.AddMinutes(4));
        AddOnlineAgentSeenAt(store, "ann", windowStart.AddMinutes(1));
        var mode = CreateMode(store, time);

        // act
        mode.OnEnter();

        // assert
        Assert.Collection(
            mode.State.Rows,
            row => Assert.Equal("ann", row.Name),
            row => Assert.Equal("zed", row.Name));
    }

    [Fact]
    public void Rows_Should_SortTheLaterWindowFirst_When_OneAgentsLastSeenCrossedTheNextFiveMinuteBoundary()
    {
        // arrange
        // 10:06 falls in the 10:05 window, 10:01 and 10:04 in the 10:00 window
        var windowStart = new DateTimeOffset(2026, 1, 1, 10, 0, 0, TimeSpan.Zero);
        var time = new FakeTimeProvider(windowStart.AddMinutes(7));
        var store = new FakeAgentStore(time);
        AddOnlineAgentSeenAt(store, "zzz", windowStart.AddMinutes(1));
        AddOnlineAgentSeenAt(store, "yyy", windowStart.AddMinutes(4));
        AddOnlineAgentSeenAt(store, "aaa", windowStart.AddMinutes(6));
        var mode = CreateMode(store, time);

        // act
        mode.OnEnter();

        // assert
        Assert.Equal("aaa", mode.State.Rows[0].Name);
    }

    [Fact]
    public void Rows_Should_PlaceTwoLastSeenTimesInDifferentWindows_When_OneLandsRightBeforeAndOneRightAtTheBoundary()
    {
        // arrange
        // 10:04:59 is in the 10:00 window, 10:05:00 starts the 10:05 window
        var boundary = new DateTimeOffset(2026, 1, 1, 10, 5, 0, TimeSpan.Zero);
        var time = new FakeTimeProvider(boundary.AddMinutes(1));
        var store = new FakeAgentStore(time);
        AddOnlineAgentSeenAt(store, "b", boundary);
        AddOnlineAgentSeenAt(store, "a", boundary - TimeSpan.FromSeconds(1));
        var mode = CreateMode(store, time);

        // act
        mode.OnEnter();

        // assert
        Assert.Equal("b", mode.State.Rows[0].Name);
        Assert.Equal("a", mode.State.Rows[1].Name);
    }

    [Fact]
    public void Rows_Should_KeepOnlineBeforeOffline_When_TheOfflineAgentsWindowIsLaterThanTheOnlineAgents()
    {
        // arrange
        // The offline agent's window is later than the online agent's
        var windowStart = new DateTimeOffset(2026, 1, 1, 10, 0, 0, TimeSpan.Zero);
        var time = new FakeTimeProvider(windowStart.AddMinutes(20));
        var store = new FakeAgentStore(time);
        var online = AddOnlineAgentSeenAt(store, "zzz", windowStart);
        var offline = AddOfflineAgentSeenAt(store, "aaa", windowStart.AddMinutes(15));
        var mode = CreateMode(store, time);

        // act
        mode.OnEnter();

        // assert
        Assert.Equal(online.Name, mode.State.Rows[0].Name);
        Assert.Equal(offline.Name, mode.State.Rows[1].Name);
    }

    [Fact]
    public void MoveSelection_Should_ClampAtLastRow_When_MovingDownPastEnd()
    {
        // arrange
        var time = new FakeTimeProvider(s_now);
        var store = new FakeAgentStore(time);
        AddOnlineAgent(store, "s-a");
        AddOnlineAgent(store, "s-b");
        var mode = CreateMode(store, time);
        mode.OnEnter();

        // act
        mode.Handle(new TuiMessage.MoveCursor(CursorDirection.Down));
        mode.Handle(new TuiMessage.MoveCursor(CursorDirection.Down));

        // assert
        Assert.Equal(1, mode.State.SelectedRow);
    }

    [Fact]
    public void MoveSelectionToEdge_Should_SelectLastRow_When_Bottom()
    {
        // arrange
        var time = new FakeTimeProvider(s_now);
        var store = new FakeAgentStore(time);
        AddOnlineAgent(store, "s-a");
        AddOnlineAgent(store, "s-b");
        AddOnlineAgent(store, "s-c");
        var mode = CreateMode(store, time);
        mode.OnEnter();

        // act
        mode.Handle(new TuiMessage.MoveToEdge(EdgeTarget.Bottom));

        // assert
        Assert.Equal(2, mode.State.SelectedRow);
    }

    [Fact]
    public void CopySelectedId_Should_ReturnTheSessionId_When_TheSelectedAgentHasASession()
    {
        // arrange
        var time = new FakeTimeProvider(s_now);
        var store = new FakeAgentStore(time);
        AddOnlineAgent(store, "s-a");
        var mode = CreateMode(store, time);
        mode.OnEnter();

        // act
        var followUp = mode.Handle(new TuiMessage.CopySelectedId());

        // assert
        var toast = Assert.Single(followUp);
        var shown = Assert.IsType<TuiMessage.ShowToast>(toast);
        Assert.Equal("s-a", shown.Text);
    }

    [Fact]
    public void CopySelectedId_Should_ReturnAHint_When_TheSelectedAgentIsLoginOnly()
    {
        // arrange
        var time = new FakeTimeProvider(s_now);
        var store = new FakeAgentStore(time);
        var agent = AddUnreachableAgent(store);
        var mode = CreateMode(store, time);
        mode.OnEnter();

        // act
        var followUp = mode.Handle(new TuiMessage.CopySelectedId());

        // assert
        var toast = Assert.Single(followUp);
        var shown = Assert.IsType<TuiMessage.ShowToast>(toast);
        Assert.Contains(agent.Name, shown.Text);
    }

    [Fact]
    public void SearchRequested_Should_OpenTheSearchForm_When_Requested()
    {
        // arrange
        var time = new FakeTimeProvider(s_now);
        var store = new FakeAgentStore(time);
        var mode = CreateMode(store, time);
        mode.OnEnter();

        // act
        var followUp = mode.Handle(new TuiMessage.SearchRequested());

        // assert
        Assert.Empty(followUp);
        Assert.True(mode.IsInputCapturing);
    }

    [Fact]
    public void SearchForm_Apply_Should_NarrowRowsByName_When_TextMatchesOneAgent()
    {
        // arrange
        var time = new FakeTimeProvider(s_now);
        var store = new FakeAgentStore(time);
        var first = AddOnlineAgent(store, "s-a");
        AddOnlineAgent(store, "s-b");
        var mode = CreateMode(store, time);
        mode.OnEnter();
        mode.Handle(new TuiMessage.SearchRequested());
        Type(mode, first.Name);

        // act
        mode.HandleRawKey(CtrlKey(ConsoleKey.S));

        // assert
        Assert.False(mode.IsInputCapturing);
        var row = Assert.Single(mode.State.Rows);
        Assert.Equal(first.Name, row.Name);
    }

    [Fact]
    public void CountOfflineAgents_Should_CountOnlyOfflineRows_When_ASearchFilterIsActive()
    {
        // arrange
        var time = new FakeTimeProvider(s_now);
        var store = new FakeAgentStore(time);
        AddOfflineAgent(store, "s-a");
        AddOfflineAgent(store, "s-b");
        AddOnlineAgent(store, "s-c");
        var mode = CreateMode(store, time);
        mode.OnEnter();
        mode.State.ApplySearch("no-agent-matches-this");

        // act
        var offlineCount = mode.CountOfflineAgents();

        // assert
        Assert.Equal(2, offlineCount);
    }

    [Fact]
    public void Render_Should_ScrollRowsBelowTheHeaderBlock_When_MoreAgentsThanRowLinesExist()
    {
        // arrange
        var time = new FakeTimeProvider(s_now);
        var store = new FakeAgentStore(time);
        AddOnlineAgent(store, "s-a");
        AddOnlineAgent(store, "s-b");
        AddOnlineAgent(store, "s-c");
        AddOnlineAgent(store, "s-d");
        AddOnlineAgent(store, "s-e");
        AddOnlineAgent(store, "s-f");
        var mode = CreateMode(store, time);
        mode.OnEnter();
        var console = new TestConsole().Width(100);

        // act
        console.Write(mode.Render(100, 10));

        // assert
        console.Output.MatchInlineSnapshot(
            """
            ╭─Agents (6 online / 6)────────────────────────────────────────────────────────────────────────────╮
            │                                                                                                  │
            │     NAME            ROLE            HARNESS         STARTED       LAST SEEN                      │
            │ ──────────────────────────────────────────────────────────────────────────────────────────────── │
            │ > ● ackbar          -               Claude Code     just now      just now                       │
            │   ● ahsoka          -               Claude Code     just now      just now                       │
            │   ● aladdin         -               Claude Code     just now      just now                       │
            │   ● albus           -               Claude Code     just now      just now                       │
            │   2 more below                                                                                   │
            ╰──────────────────────────────────────────────────────────────────────────────────────────────────╯

            """);
    }

    [Fact]
    public void Render_Should_ShowHeaderRuleAndRows_When_ThreeAgentsArePresent()
    {
        // arrange
        var time = new FakeTimeProvider(s_now);
        var store = new FakeAgentStore(time);
        AddOnlineAgent(store, "s-a");
        AddOnlineAgent(store, "s-b");
        AddOnlineAgent(store, "s-c");
        var mode = CreateMode(store, time);
        mode.OnEnter();
        var console = new TestConsole().Width(100);

        // act
        console.Write(mode.Render(100, 9));

        // assert
        console.Output.MatchInlineSnapshot(
            """
            ╭─Agents (3 online / 3)────────────────────────────────────────────────────────────────────────────╮
            │                                                                                                  │
            │     NAME            ROLE            HARNESS         STARTED       LAST SEEN                      │
            │ ──────────────────────────────────────────────────────────────────────────────────────────────── │
            │ > ● ackbar          -               Claude Code     just now      just now                       │
            │   ● ahsoka          -               Claude Code     just now      just now                       │
            │   ● aladdin         -               Claude Code     just now      just now                       │
            │                                                                                                  │
            ╰──────────────────────────────────────────────────────────────────────────────────────────────────╯

            """);
    }

    [Fact]
    public void RefreshRequested_Should_ReloadRowsFromTheStore_When_ANewAgentWasAdded()
    {
        // arrange
        var time = new FakeTimeProvider(s_now);
        var store = new FakeAgentStore(time);
        var mode = CreateMode(store, time);
        mode.OnEnter();
        Assert.Empty(mode.State.Rows);
        AddOnlineAgent(store, "s-a");

        // act
        mode.Handle(new TuiMessage.RefreshRequested());

        // assert
        Assert.Single(mode.State.Rows);
    }
}
