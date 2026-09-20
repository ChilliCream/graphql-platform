using ChilliCream.Nitro.CommandLine.Services.Workspace;
using ChilliCream.Nitro.CommandLine.Tui.Agents;
using ChilliCream.Nitro.CommandLine.Tui.Input;
using ChilliCream.Nitro.CommandLine.Tui.Theming;
using Microsoft.Extensions.Time.Testing;
using Spectre.Console;
using Spectre.Console.Testing;
using CursorDirection = ChilliCream.Nitro.CommandLine.Tui.Input.CursorDirection;
using static ChilliCream.Nitro.CommandLine.Tests.Tui.AnsiAssertions;

namespace ChilliCream.Nitro.CommandLine.Tests.Tui.Agents;

public sealed class AgentsModeTests
{
    private static readonly DateTimeOffset s_now = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    private static AgentRecord Agent(
        string name, string role = "", string client = "", bool isImplicit = false)
        => new()
        {
            Name = name,
            Role = role,
            Client = client,
            Implicit = isImplicit,
            RegisteredAt = s_now,
            LastSeenAt = s_now
        };

    private static AgentsMode CreateMode(
        FakeAgentSessionRegistry sessionRegistry, FakeClaudeSessionActivityReader? activityReader = null)
        => new(
            new FakeTaskStore(),
            new FakeMailStore(),
            sessionRegistry,
            activityReader ?? new FakeClaudeSessionActivityReader(),
            new FakeTimeProvider(s_now));

    private static AgentsMode CreateMode(
        FakeAgentSessionRegistry sessionRegistry, FakeTaskStore taskStore, FakeMailStore mailStore)
        => new(
            taskStore,
            mailStore,
            sessionRegistry,
            new FakeClaudeSessionActivityReader(),
            new FakeTimeProvider(s_now));

    private static string RenderToText(AgentsMode mode, int width = 100, int height = 24)
    {
        var console = new TestConsole().Width(width);
        console.Write(mode.Render(width, height));
        return console.Output;
    }

    [Fact]
    public void MoveSelection_Should_ClampAtLastRow_When_MovingDownPastEnd()
    {
        // arrange
        var sessions = new FakeAgentSessionRegistry();
        sessions.Participants.Add(AgentSessionParticipantBuilder.Participant(sessionId: "s-a", agentName: "agent-a"));
        sessions.Participants.Add(AgentSessionParticipantBuilder.Participant(sessionId: "s-b", agentName: "agent-b"));
        var mode = CreateMode(sessions);
        mode.OnEnter();

        // act
        mode.Handle(new TuiMessage.MoveCursor(CursorDirection.Down));
        mode.Handle(new TuiMessage.MoveCursor(CursorDirection.Down));

        // assert
        Assert.Equal(1, mode.State.SelectedRow);
    }

    [Fact]
    public void MoveSelection_Should_ClampAtFirstRow_When_MovingUpPastStart()
    {
        // arrange
        var sessions = new FakeAgentSessionRegistry();
        sessions.Participants.Add(AgentSessionParticipantBuilder.Participant(sessionId: "s-a", agentName: "agent-a"));
        var mode = CreateMode(sessions);
        mode.OnEnter();

        // act
        mode.Handle(new TuiMessage.MoveCursor(CursorDirection.Up));

        // assert
        Assert.Equal(0, mode.State.SelectedRow);
    }

    [Fact]
    public void MoveSelectionToEdge_Should_SelectLastRow_When_Bottom()
    {
        // arrange
        var sessions = new FakeAgentSessionRegistry();
        sessions.Participants.Add(AgentSessionParticipantBuilder.Participant(sessionId: "s-a", agentName: "agent-a"));
        sessions.Participants.Add(AgentSessionParticipantBuilder.Participant(sessionId: "s-b", agentName: "agent-b"));
        sessions.Participants.Add(AgentSessionParticipantBuilder.Participant(sessionId: "s-c", agentName: "agent-c"));
        var mode = CreateMode(sessions);
        mode.OnEnter();

        // act
        mode.Handle(new TuiMessage.MoveToEdge(EdgeTarget.Bottom));

        // assert
        Assert.Equal(2, mode.State.SelectedRow);
    }

    [Fact]
    public void MoveSelectionToEdge_Should_SelectFirstRow_When_Top()
    {
        // arrange
        var sessions = new FakeAgentSessionRegistry();
        sessions.Participants.Add(AgentSessionParticipantBuilder.Participant(sessionId: "s-a", agentName: "agent-a"));
        sessions.Participants.Add(AgentSessionParticipantBuilder.Participant(sessionId: "s-b", agentName: "agent-b"));
        var mode = CreateMode(sessions);
        mode.OnEnter();
        mode.Handle(new TuiMessage.MoveToEdge(EdgeTarget.Bottom));

        // act
        mode.Handle(new TuiMessage.MoveToEdge(EdgeTarget.Top));

        // assert
        Assert.Equal(0, mode.State.SelectedRow);
    }

    [Fact]
    public void Refresh_Should_PreserveSelection_ByHarnessAndSessionId_When_ReloadedListReorders()
    {
        // arrange
        var sessions = new FakeAgentSessionRegistry();
        sessions.Participants.Add(
            AgentSessionParticipantBuilder.Participant(sessionId: "s-bravo", agentName: "bravo"));
        sessions.Participants.Add(
            AgentSessionParticipantBuilder.Participant(sessionId: "s-charlie", agentName: "charlie"));
        var mode = CreateMode(sessions);
        mode.OnEnter();
        mode.Handle(new TuiMessage.MoveToEdge(EdgeTarget.Bottom));
        Assert.Equal("s-charlie", mode.State.SelectedParticipant?.Participant.Session.SessionId);

        // act
        // Insert a row before the selected session, preserving its harness and session id.
        sessions.Participants.Insert(
            0, AgentSessionParticipantBuilder.Participant(sessionId: "s-alpha", agentName: "alpha"));
        mode.Handle(new TuiMessage.RefreshRequested());

        // assert
        Assert.Equal("s-charlie", mode.State.SelectedParticipant?.Participant.Session.SessionId);
        Assert.Equal(2, mode.State.SelectedRow);
    }

    [Fact]
    public void Refresh_Should_RemoveRow_When_TheSessionEndsOrIsReaped()
    {
        // arrange
        var sessions = new FakeAgentSessionRegistry();
        sessions.Participants.Add(AgentSessionParticipantBuilder.Participant(sessionId: "s-a", agentName: "agent-a"));
        var mode = CreateMode(sessions);
        mode.OnEnter();
        Assert.Single(mode.State.Rows);

        // act
        sessions.Participants.Clear();
        mode.Handle(new TuiMessage.RefreshRequested());

        // assert
        Assert.Empty(mode.State.Rows);
        Assert.Null(mode.State.SelectedParticipant);
    }

    [Fact]
    public void Rows_Should_ShowTwoSeparateRows_When_TwoSessionsShareOneActor()
    {
        // arrange
        var sessions = new FakeAgentSessionRegistry();
        sessions.Participants.Add(AgentSessionParticipantBuilder.Participant(
            sessionId: "s-1", agentName: "bravo", harness: "claude-code"));
        sessions.Participants.Add(AgentSessionParticipantBuilder.Participant(
            sessionId: "s-2", agentName: "bravo", harness: "codex"));
        var mode = CreateMode(sessions);

        // act
        mode.OnEnter();

        // assert
        Assert.Equal(2, mode.State.Rows.Count);
        Assert.Equal(
            ["s-1", "s-2"],
            mode.State.Rows.Select(r => r.Participant.Session.SessionId));
        Assert.All(mode.State.Rows, r => Assert.Equal("bravo", r.Participant.Session.AgentName));
    }

    [Fact]
    public void Refresh_Should_UpdateRoleInPlace_When_TheSameSessionIsPromoted_WithoutDuplicatingTheRow()
    {
        // arrange
        var sessions = new FakeAgentSessionRegistry();
        sessions.Participants.Add(
            AgentSessionParticipantBuilder.Participant(sessionId: "s-1", agentName: "alpha", role: ""));
        var mode = CreateMode(sessions);
        mode.OnEnter();
        Assert.Single(mode.State.Rows);

        // act
        sessions.Participants[0] = AgentSessionParticipantBuilder.Participant(
            sessionId: "s-1", agentName: "alpha", role: "orchestrator");
        mode.Handle(new TuiMessage.RefreshRequested());

        // assert
        var row = Assert.Single(mode.State.Rows);
        Assert.Equal("orchestrator", row.Participant.Session.Role);
    }

    [Fact]
    public void OpenSelected_Should_FocusDetailPane_And_ReturnEmpty()
    {
        // arrange
        var sessions = new FakeAgentSessionRegistry();
        sessions.Participants.Add(AgentSessionParticipantBuilder.Participant(sessionId: "s-a", agentName: "agent-a"));
        var mode = CreateMode(sessions);
        mode.OnEnter();

        // act
        var messages = mode.Handle(new TuiMessage.OpenSelected());

        // assert
        Assert.Empty(messages);
        Assert.Equal(AgentsFocus.Detail, mode.State.Focus);
    }

    [Fact]
    public void CopySelectedId_Should_ShowSessionIdInToast_When_ParticipantSelected()
    {
        // arrange
        var sessions = new FakeAgentSessionRegistry();
        sessions.Participants.Add(AgentSessionParticipantBuilder.Participant(sessionId: "s-a", agentName: "agent-a"));
        var mode = CreateMode(sessions);
        mode.OnEnter();

        // act
        var messages = mode.Handle(new TuiMessage.CopySelectedId());

        // assert
        var toast = Assert.IsType<TuiMessage.ShowToast>(Assert.Single(messages));
        Assert.Equal("s-a", toast.Text);
    }

    [Fact]
    public void CopySelectedId_Should_WarnNoSessionSelected_When_ListEmpty()
    {
        // arrange
        var mode = CreateMode(new FakeAgentSessionRegistry());
        mode.OnEnter();

        // act
        var messages = mode.Handle(new TuiMessage.CopySelectedId());

        // assert
        var toast = Assert.IsType<TuiMessage.ShowToast>(Assert.Single(messages));
        Assert.Equal(ToastStyle.Warn, toast.Style);
    }

    [Fact]
    public void Render_Should_IncludeActorRoleAndPanelTitle()
    {
        // arrange
        var sessions = new FakeAgentSessionRegistry();
        sessions.Participants.Add(AgentSessionParticipantBuilder.Participant(
            sessionId: "s-a", agentName: "agent-a", role: "backend"));
        var mode = CreateMode(sessions);
        mode.OnEnter();
        var console = new TestConsole().Width(80).Height(20);

        // act
        console.Write(mode.Render(80, 20));

        // assert
        Assert.Contains("Agents (1)", console.Output);
        Assert.Contains("agent-a", console.Output);
        Assert.Contains("backend", console.Output);
    }

    [Fact]
    public void Render_Should_ShowUnboundLabel_When_TheSessionHasNoBoundActor()
    {
        // arrange
        var sessions = new FakeAgentSessionRegistry();
        sessions.Participants.Add(AgentSessionParticipantBuilder.Participant(sessionId: "s-a", agentName: null));
        var mode = CreateMode(sessions);
        mode.OnEnter();

        // act
        var row = Assert.Single(RenderToText(mode, 100, 20).Split('\n'), l => l.Contains("started "));

        // assert
        Assert.Contains(AgentParticipantRow.UnboundLabel, row);
    }

    [Fact]
    public void Render_Should_ShowDash_When_RoleEmpty()
    {
        // arrange
        var sessions = new FakeAgentSessionRegistry();
        sessions.Participants.Add(AgentSessionParticipantBuilder.Participant(
            sessionId: "s-a", agentName: "agent-a", harness: "claude-code"));
        var mode = CreateMode(sessions);
        mode.OnEnter();

        // act
        var text = RenderToText(mode, 120, 20);
        var row = Assert.Single(text.Split('\n'), l => l.Contains("agent-a") && l.Contains("started "));

        // assert
        Assert.Contains("claude-code -", row);
    }

    [Fact]
    public void Render_Should_ShowHarness_In_ListRow()
    {
        // arrange
        var sessions = new FakeAgentSessionRegistry();
        sessions.Participants.Add(AgentSessionParticipantBuilder.Participant(
            sessionId: "s-a", agentName: "agent-a", role: "backend", harness: "codex"));
        var mode = CreateMode(sessions);
        mode.OnEnter();

        // act
        var text = RenderToText(mode, 100, 20);
        var row = Assert.Single(text.Split('\n'), l => l.Contains("agent-a") && l.Contains("started "));

        // assert
        Assert.Contains("codex", row);
    }

    [Fact]
    public void Render_Should_ShowOnlineGlyph_When_TheSessionHasAnEndpoint()
    {
        // arrange
        var sessions = new FakeAgentSessionRegistry();
        sessions.Participants.Add(AgentSessionParticipantBuilder.Participant(
            sessionId: "s-a", agentName: "agent-a", state: AgentSessionState.Online));
        var mode = CreateMode(sessions);
        mode.OnEnter();

        // act
        var row = Assert.Single(
            RenderToText(mode, 100, 20).Split('\n'), l => l.Contains("agent-a") && l.Contains("started "));

        // assert
        Assert.Contains("agent-a ● ", row);
    }

    [Fact]
    public void Render_Should_ShowUnreachableGlyph_When_TheSessionHasNoEndpoint()
    {
        // arrange
        var sessions = new FakeAgentSessionRegistry();
        sessions.Participants.Add(AgentSessionParticipantBuilder.Participant(
            sessionId: "s-a", agentName: "agent-a", state: AgentSessionState.Unreachable));
        var mode = CreateMode(sessions);
        mode.OnEnter();

        // act
        var row = Assert.Single(
            RenderToText(mode, 100, 20).Split('\n'), l => l.Contains("agent-a") && l.Contains("started "));

        // assert
        Assert.Contains("agent-a ◐ ", row);
    }

    [Fact]
    public void Render_Should_ShowUnobservableGlyph_When_TheSessionCannotBeVerified()
    {
        // arrange
        var sessions = new FakeAgentSessionRegistry();
        sessions.Participants.Add(AgentSessionParticipantBuilder.Participant(
            sessionId: "s-a", agentName: "agent-a", state: AgentSessionState.Unobservable));
        var mode = CreateMode(sessions);
        mode.OnEnter();

        // act
        var row = Assert.Single(
            RenderToText(mode, 100, 20).Split('\n'), l => l.Contains("agent-a") && l.Contains("started "));

        // assert: distinct from both the online and unreachable glyphs.
        Assert.Contains("agent-a ◌ ", row);
    }

    [Fact]
    public void Render_Should_ShowRemoteGlyph_When_TheSessionIsOnAnotherInstance()
    {
        // arrange
        var sessions = new FakeAgentSessionRegistry();
        sessions.Participants.Add(AgentSessionParticipantBuilder.Participant(
            sessionId: "s-a", agentName: "agent-a", state: AgentSessionState.Remote));
        var mode = CreateMode(sessions);
        mode.OnEnter();

        // act
        var row = Assert.Single(
            RenderToText(mode, 100, 20).Split('\n'), l => l.Contains("agent-a") && l.Contains("started "));

        // assert
        Assert.Contains("agent-a ◇ ", row);
    }

    [Fact]
    public void Render_Should_ShowActivityLetter_When_TheOnlineClaudeSessionHasKnownActivity()
    {
        // arrange
        var sessions = new FakeAgentSessionRegistry();
        sessions.Participants.Add(AgentSessionParticipantBuilder.Participant(
            sessionId: "s-a", agentName: "agent-a", state: AgentSessionState.Online));
        var activityReader = new FakeClaudeSessionActivityReader();
        activityReader.StatusBySessionId["s-a"] = "busy";
        var mode = CreateMode(sessions, activityReader);
        mode.OnEnter();

        // act
        var row = Assert.Single(
            RenderToText(mode, 100, 20).Split('\n'), l => l.Contains("agent-a") && l.Contains("started "));

        // assert
        Assert.Contains("agent-a ●(b) ", row);
    }

    [Fact]
    public void Render_Should_ShowIdentitySection_When_TheSelectedSessionIsBound()
    {
        // arrange
        var sessions = new FakeAgentSessionRegistry();
        sessions.Participants.Add(AgentSessionParticipantBuilder.Participant(
            sessionId: "s-a", agentName: "agent-a", agent: Agent("agent-a", client: "claude-code")));
        var mode = CreateMode(sessions);
        mode.OnEnter();

        // act
        var text = RenderToText(mode);

        // assert
        Assert.Contains("Identity", text);
        Assert.Contains("Name: agent-a", text);
        Assert.Contains("Client: claude-code", text);
    }

    [Fact]
    public void Render_Should_OmitIdentityTasksAndSentMailSections_When_TheSelectedSessionIsUnbound()
    {
        // arrange
        var sessions = new FakeAgentSessionRegistry();
        sessions.Participants.Add(AgentSessionParticipantBuilder.Participant(sessionId: "s-a", agentName: null));
        var mode = CreateMode(sessions);
        mode.OnEnter();

        // act
        var text = RenderToText(mode);

        // assert
        Assert.Contains("Session", text);
        Assert.Contains(AgentParticipantRow.UnboundLabel, text);
        Assert.DoesNotContain("Identity", text);
        Assert.DoesNotContain("Tasks", text);
        Assert.DoesNotContain("Sent mail", text);
    }

    [Fact]
    public void Render_Should_ShowFullSessionIdAndHarnessVersion_In_DetailPane()
    {
        // arrange
        var sessions = new FakeAgentSessionRegistry();
        sessions.Participants.Add(AgentSessionParticipantBuilder.Participant(
            sessionId: "s-full-session-id", agentName: "agent-a", harness: "claude-code", harnessVersion: "2.1.241"));
        var mode = CreateMode(sessions);
        mode.OnEnter();

        // act
        var text = RenderToText(mode);

        // assert
        Assert.Contains("Session id: s-full-session-id", text);
        Assert.Contains("Harness: claude-code 2.1.241", text);
    }

    [Fact]
    public void Render_Should_NotThrow_When_WidthOrHeightIsZero()
    {
        // arrange
        var mode = CreateMode(new FakeAgentSessionRegistry());
        mode.OnEnter();

        // act
        var exception = Record.Exception(() => mode.Render(0, 0));

        // assert
        Assert.Null(exception);
    }

    [Fact]
    public void Render_Should_FitDisplayWidth_When_RoleContainsCjkAndEmoji()
    {
        // arrange
        const int maxWidth = 60;
        var row = new AgentParticipantRow(
            AgentSessionParticipantBuilder.Participant(agentName: "agent-a", role: "漢😀 a-very-long-mutable-role-value"),
            Activity: null);
        var widths = AgentRowBadge.ComputeWidths([row], s_now);

        // act
        var line = AgentRowBadge.Render(row, s_now, selected: false, maxWidth, widths);

        // assert
        Assert.True(Markup.Remove(line).GetCellWidth() <= maxWidth);
    }

    [Fact]
    public void Render_Should_ShowMoreBelowIndicator_When_ParticipantsExceedPanelHeight()
    {
        // arrange
        var sessions = new FakeAgentSessionRegistry();
        for (var i = 1; i <= 15; i++)
        {
            sessions.Participants.Add(AgentSessionParticipantBuilder.Participant(
                sessionId: $"s-{i:D2}", agentName: $"agent-{i:D2}"));
        }

        var mode = CreateMode(sessions);
        mode.OnEnter();
        var console = new TestConsole().Width(80).Height(12);

        // act
        console.Write(mode.Render(80, 12));

        // assert
        Assert.Contains("more below", console.Output);
        Assert.DoesNotContain("agent-15", console.Output);
    }

    [Fact]
    public void Render_Should_ShowMoreAboveIndicator_And_SelectedParticipant_When_MovedToBottom()
    {
        // arrange
        var sessions = new FakeAgentSessionRegistry();
        for (var i = 1; i <= 15; i++)
        {
            sessions.Participants.Add(AgentSessionParticipantBuilder.Participant(
                sessionId: $"s-{i:D2}", agentName: $"agent-{i:D2}"));
        }

        var mode = CreateMode(sessions);
        mode.OnEnter();
        var console = new TestConsole().Width(80).Height(12);

        // act
        mode.Handle(new TuiMessage.MoveToEdge(EdgeTarget.Bottom));
        console.Write(mode.Render(80, 12));

        // assert
        Assert.Contains("more above", console.Output);
        Assert.Contains("agent-15", console.Output);
    }

    [Fact]
    public void OnEnter_Should_LoadEveryParticipant_ThroughTheRegistry_InItsOwnOrder()
    {
        // arrange
        var sessions = new FakeAgentSessionRegistry();
        sessions.Participants.Add(AgentSessionParticipantBuilder.Participant(sessionId: "s-z", agentName: "zeta"));
        sessions.Participants.Add(AgentSessionParticipantBuilder.Participant(sessionId: "s-a", agentName: "alpha"));
        var mode = CreateMode(sessions);

        // act
        mode.OnEnter();

        // assert
        Assert.Equal(["s-z", "s-a"], mode.State.Rows.Select(r => r.Participant.Session.SessionId));
    }

    [Fact]
    public void Render_Should_ShowListAndDetailPanesSideBySide_When_ParticipantSelected()
    {
        // arrange
        var sessions = new FakeAgentSessionRegistry();
        sessions.Participants.Add(AgentSessionParticipantBuilder.Participant(
            sessionId: "s-a", agentName: "agent-a", role: "backend"));
        var mode = CreateMode(sessions);
        mode.OnEnter();

        // act
        var text = RenderToText(mode);

        // assert
        Assert.Contains("Agents (1)", text);
        Assert.Contains("agent-a", text);
        Assert.Contains("Session", text);
        Assert.Contains("Role: backend", text);
    }

    [Fact]
    public void MoveSelection_Should_ReloadDetailPane_When_SelectionChanges()
    {
        // arrange
        var sessions = new FakeAgentSessionRegistry();
        sessions.Participants.Add(AgentSessionParticipantBuilder.Participant(sessionId: "s-a", agentName: "agent-a"));
        sessions.Participants.Add(AgentSessionParticipantBuilder.Participant(sessionId: "s-b", agentName: "agent-b"));
        var taskStore = new FakeTaskStore();
        taskStore.Tasks.Add(TaskItemBuilder.Create("b-1", assignee: "agent-b"));
        var mode = CreateMode(sessions, taskStore, new FakeMailStore());
        mode.OnEnter();
        Assert.DoesNotContain("b-1", RenderToText(mode));

        // act
        mode.Handle(new TuiMessage.MoveCursor(CursorDirection.Down));

        // assert
        Assert.Contains("b-1", RenderToText(mode));
    }

    [Fact]
    public void TogglePane_Should_FlipFocusBetweenListAndDetail()
    {
        // arrange
        var sessions = new FakeAgentSessionRegistry();
        sessions.Participants.Add(AgentSessionParticipantBuilder.Participant(sessionId: "s-a", agentName: "agent-a"));
        var mode = CreateMode(sessions);
        mode.OnEnter();
        Assert.Equal(AgentsFocus.List, mode.State.Focus);

        // act
        mode.Handle(new TuiMessage.MoveCursor(CursorDirection.Right));

        // assert
        Assert.Equal(AgentsFocus.Detail, mode.State.Focus);

        // act
        mode.Handle(new TuiMessage.MoveCursor(CursorDirection.Left));

        // assert
        Assert.Equal(AgentsFocus.List, mode.State.Focus);
    }

    [Fact]
    public void Refresh_Should_ReloadDetailPane_For_SelectedParticipant()
    {
        // arrange
        var sessions = new FakeAgentSessionRegistry();
        sessions.Participants.Add(AgentSessionParticipantBuilder.Participant(sessionId: "s-a", agentName: "agent-a"));
        var taskStore = new FakeTaskStore();
        var mode = CreateMode(sessions, taskStore, new FakeMailStore());
        mode.OnEnter();
        Assert.DoesNotContain("a-1", RenderToText(mode));

        // act
        taskStore.Tasks.Add(TaskItemBuilder.Create("a-1", assignee: "agent-a"));
        mode.Handle(new TuiMessage.RefreshRequested());

        // assert
        Assert.Contains("a-1", RenderToText(mode));
    }

    [Fact]
    public void Refresh_Should_ClearDetailPane_When_ListEmpties()
    {
        // arrange
        var sessions = new FakeAgentSessionRegistry();
        sessions.Participants.Add(AgentSessionParticipantBuilder.Participant(
            sessionId: "s-a", agentName: "agent-a", role: "backend"));
        var mode = CreateMode(sessions);
        mode.OnEnter();
        Assert.Contains("Role: backend", RenderToText(mode));

        // act: the selected session ends before the next refresh.
        sessions.Participants.Clear();
        mode.Handle(new TuiMessage.RefreshRequested());
        var text = RenderToText(mode);

        // assert
        Assert.Contains("Agents (0)", text);
        Assert.DoesNotContain("Role: backend", text);
    }

    [Fact]
    public void Render_Should_AlignColumns_Across_Rows()
    {
        // arrange
        var sessions = new FakeAgentSessionRegistry();
        sessions.Participants.Add(AgentSessionParticipantBuilder.Participant(
            sessionId: "s-1", agentName: "a", role: "backend"));
        sessions.Participants.Add(AgentSessionParticipantBuilder.Participant(
            sessionId: "s-2", agentName: "longer-name", role: "qa"));
        var mode = CreateMode(sessions);
        mode.OnEnter();

        // act
        var text = RenderToText(mode, 140, 24);
        var rows = text.Split('\n').Where(l => l.Contains("started ") && l.Contains("heard ")).ToList();
        var shortNameRow = Assert.Single(rows, l => l.Contains("backend"));
        var longNameRow = Assert.Single(rows, l => l.Contains("qa"));

        // assert
        Assert.Equal(
            shortNameRow.IndexOf("backend", StringComparison.Ordinal),
            longNameRow.IndexOf("qa", StringComparison.Ordinal));
        Assert.Equal(
            shortNameRow.IndexOf("started ", StringComparison.Ordinal),
            longNameRow.IndexOf("started ", StringComparison.Ordinal));
    }

    [Fact]
    public void Render_Should_ApplyAnsiStyling_ToActorAndHarnessTokens_InTheListRow()
    {
        // arrange
        var sessions = new FakeAgentSessionRegistry();
        sessions.Participants.Add(AgentSessionParticipantBuilder.Participant(sessionId: "s-a", agentName: "agent-a"));
        sessions.Participants.Add(AgentSessionParticipantBuilder.Participant(
            sessionId: "s-b", agentName: "agent-b", role: "backend", harness: "codex"));
        var mode = CreateMode(sessions);
        mode.OnEnter();
        var console = new TestConsole().Colors(ColorSystem.TrueColor).EmitAnsiSequences().Width(80).Height(20);

        // act
        console.Write(mode.Render(80, 20));

        // assert
        // The actor name immediately follows its style escape sequence.
        var row = Assert.Single(console.Output.Split('\n'), l => l.Contains("agent-b"));
        var style = ThemeTokens.GetStyle("agents.list.name");
        var styleConsole = new TestConsole().Colors(ColorSystem.TrueColor).EmitAnsiSequences().Width(1).Height(1);
        styleConsole.Write(new Markup("x", style));
        var ansiPrefix = styleConsole.Output[..styleConsole.Output.IndexOf('x')];
        Assert.Contains(ansiPrefix + "agent-b", row);
        AssertAnsiStyleApplied(row, "agents.list.harness");
    }

    [Fact]
    public void Render_Should_ApplyAnsiStyling_ToPerRoleToken_When_RoleHasADedicatedColor()
    {
        // arrange
        var sessions = new FakeAgentSessionRegistry();
        sessions.Participants.Add(AgentSessionParticipantBuilder.Participant(sessionId: "s-a", agentName: "agent-a"));
        sessions.Participants.Add(AgentSessionParticipantBuilder.Participant(
            sessionId: "s-b", agentName: "agent-b", role: "orchestrator"));
        var mode = CreateMode(sessions);
        mode.OnEnter();
        var console = new TestConsole().Colors(ColorSystem.TrueColor).EmitAnsiSequences().Width(140).Height(20);

        // act
        console.Write(mode.Render(140, 20));

        // assert
        Assert.NotEqual(
            ThemeTokens.GetStyle("agents.list.role"),
            ThemeTokens.GetStyle("agents.list.role.orchestrator"));
        var row = Assert.Single(console.Output.Split('\n'), l => l.Contains("agent-b"));
        AssertAnsiStyleApplied(row, "agents.list.role.orchestrator");
    }

    [Fact]
    public void Render_Should_ApplyAnsiStyling_ToPerRoleToken_When_RoleIsResearcher()
    {
        // arrange
        var sessions = new FakeAgentSessionRegistry();
        sessions.Participants.Add(AgentSessionParticipantBuilder.Participant(sessionId: "s-a", agentName: "agent-a"));
        sessions.Participants.Add(AgentSessionParticipantBuilder.Participant(
            sessionId: "s-b", agentName: "agent-b", role: "researcher"));
        var mode = CreateMode(sessions);
        mode.OnEnter();
        var console = new TestConsole().Colors(ColorSystem.TrueColor).EmitAnsiSequences().Width(140).Height(20);

        // act
        console.Write(mode.Render(140, 20));

        // assert: pin to agent-b's row rather than the whole frame.
        Assert.Equal(
            new Style(Color.Blue),
            ThemeTokens.GetStyle("agents.list.role.researcher"));
        var row = Assert.Single(console.Output.Split('\n'), l => l.Contains("agent-b"));
        AssertAnsiStylePrefixesText(row, "agents.list.role.researcher", "researcher");
    }

    [Fact]
    public void Render_Should_ApplyAnsiStyling_ToBaseRoleToken_When_RoleHasNoDedicatedColor()
    {
        // arrange
        var sessions = new FakeAgentSessionRegistry();
        sessions.Participants.Add(AgentSessionParticipantBuilder.Participant(sessionId: "s-a", agentName: "agent-a"));
        sessions.Participants.Add(AgentSessionParticipantBuilder.Participant(
            sessionId: "s-b", agentName: "agent-b", role: "backend"));
        var mode = CreateMode(sessions);
        mode.OnEnter();
        var console = new TestConsole().Colors(ColorSystem.TrueColor).EmitAnsiSequences().Width(140).Height(20);

        // act
        console.Write(mode.Render(140, 20));

        // assert: pin to agent-b's row rather than the whole frame.
        var row = Assert.Single(console.Output.Split('\n'), l => l.Contains("agent-b"));
        AssertAnsiStyleApplied(row, "agents.list.role");
    }
}
