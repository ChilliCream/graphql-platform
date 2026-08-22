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
    private static readonly DateTimeOffset Now = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    private static AgentRecord Agent(
        string name, string role = "", string client = "", bool isImplicit = false, DateTimeOffset? registeredAt = null, DateTimeOffset? lastSeenAt = null)
        => new()
        {
            Name = name,
            Role = role,
            Client = client,
            Implicit = isImplicit,
            RegisteredAt = registeredAt ?? Now,
            LastSeenAt = lastSeenAt ?? Now
        };

    private static AgentsMode CreateMode(FakeAgentRegistry registry)
        => new(registry, new FakeTaskStore(), new FakeMailStore(), new FakeTimeProvider(Now));

    private static AgentsMode CreateMode(FakeAgentRegistry registry, FakeTaskStore taskStore, FakeMailStore mailStore)
        => new(registry, taskStore, mailStore, new FakeTimeProvider(Now));

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
        var registry = new FakeAgentRegistry();
        registry.Agents.Add(Agent("agent-a"));
        registry.Agents.Add(Agent("agent-b"));
        var mode = CreateMode(registry);
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
        var registry = new FakeAgentRegistry();
        registry.Agents.Add(Agent("agent-a"));
        var mode = CreateMode(registry);
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
        var registry = new FakeAgentRegistry();
        registry.Agents.Add(Agent("agent-a"));
        registry.Agents.Add(Agent("agent-b"));
        registry.Agents.Add(Agent("agent-c"));
        var mode = CreateMode(registry);
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
        var registry = new FakeAgentRegistry();
        registry.Agents.Add(Agent("agent-a"));
        registry.Agents.Add(Agent("agent-b"));
        var mode = CreateMode(registry);
        mode.OnEnter();
        mode.Handle(new TuiMessage.MoveToEdge(EdgeTarget.Bottom));

        // act
        mode.Handle(new TuiMessage.MoveToEdge(EdgeTarget.Top));

        // assert
        Assert.Equal(0, mode.State.SelectedRow);
    }

    [Fact]
    public void Refresh_Should_PreserveSelectedAgent_When_ReloadedListReorders()
    {
        // arrange
        var registry = new FakeAgentRegistry();
        registry.Agents.Add(Agent("bravo"));
        registry.Agents.Add(Agent("charlie"));
        var mode = CreateMode(registry);
        mode.OnEnter();
        mode.Handle(new TuiMessage.MoveToEdge(EdgeTarget.Bottom));
        Assert.Equal("charlie", mode.State.SelectedAgent?.Name);

        // act: a new, alphabetically-earlier agent pushes charlie to a
        // different row on refresh
        registry.Agents.Add(Agent("alpha"));
        mode.Handle(new TuiMessage.RefreshRequested());

        // assert
        Assert.Equal("charlie", mode.State.SelectedAgent?.Name);
        Assert.Equal(2, mode.State.SelectedRow);
    }

    [Fact]
    public void OpenSelected_Should_FocusDetailPane_And_ReturnEmpty()
    {
        // arrange
        var registry = new FakeAgentRegistry();
        registry.Agents.Add(Agent("agent-a"));
        var mode = CreateMode(registry);
        mode.OnEnter();

        // act
        var messages = mode.Handle(new TuiMessage.OpenSelected());

        // assert: focus moves to the detail pane (no separate pushed mode
        // to open; the detail pane is always visible next to the list).
        Assert.Empty(messages);
        Assert.Equal(AgentsFocus.Detail, mode.State.Focus);
    }

    [Fact]
    public void CopySelectedId_Should_ShowNameInToast_When_AgentSelected()
    {
        // arrange
        var registry = new FakeAgentRegistry();
        registry.Agents.Add(Agent("agent-a"));
        var mode = CreateMode(registry);
        mode.OnEnter();

        // act
        var messages = mode.Handle(new TuiMessage.CopySelectedId());

        // assert
        var toast = Assert.IsType<TuiMessage.ShowToast>(Assert.Single(messages));
        Assert.Equal("agent-a", toast.Text);
    }

    [Fact]
    public void CopySelectedId_Should_WarnNoAgentSelected_When_ListEmpty()
    {
        // arrange
        var registry = new FakeAgentRegistry();
        var mode = CreateMode(registry);
        mode.OnEnter();

        // act
        var messages = mode.Handle(new TuiMessage.CopySelectedId());

        // assert
        var toast = Assert.IsType<TuiMessage.ShowToast>(Assert.Single(messages));
        Assert.Equal(ToastStyle.Warn, toast.Style);
    }

    [Fact]
    public void Render_Should_IncludeAgentNameRoleAndPanelTitle()
    {
        // arrange
        var registry = new FakeAgentRegistry();
        registry.Agents.Add(Agent("agent-a", role: "backend"));
        var mode = CreateMode(registry);
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
    public void Render_Should_ShowDash_When_RoleEmpty()
    {
        // arrange: a non-empty client so the role's own dash is the second
        // dash on the row, distinct from the client column's dash (which
        // "agent-a" alone would also match here now that client sits
        // between name and role). A wider console than the other row tests
        // use, so the long client value doesn't eat the role column's
        // truncation budget down to nothing.
        var registry = new FakeAgentRegistry();
        registry.Agents.Add(Agent("agent-a", client: "claude-code"));
        var mode = CreateMode(registry);
        mode.OnEnter();

        // act
        var text = RenderToText(mode, 100, 20);
        var row = Assert.Single(text.Split('\n'), l => l.Contains("agent-a") && l.Contains("reg "));

        // assert: the client column shows its own value, followed by a dash
        // for the empty role column.
        Assert.Contains("claude-code -", row);
    }

    [Fact]
    public void Render_Should_ShowClient_In_ListRow()
    {
        // arrange
        var registry = new FakeAgentRegistry();
        registry.Agents.Add(Agent("agent-a", role: "backend", client: "claude-code"));
        var mode = CreateMode(registry);
        mode.OnEnter();

        // act
        var text = RenderToText(mode, 100, 20);
        var row = Assert.Single(text.Split('\n'), l => l.Contains("agent-a") && l.Contains("reg "));

        // assert: the client shows in the list row itself, not just the
        // detail pane's Identity section (which would also satisfy a
        // whole-frame Contains).
        Assert.Contains("claude-code", row);
    }

    [Fact]
    public void Render_Should_ShowDashForClient_When_ClientEmpty_In_ListRow()
    {
        // arrange
        var registry = new FakeAgentRegistry();
        registry.Agents.Add(Agent("agent-a", role: "backend"));
        var mode = CreateMode(registry);
        mode.OnEnter();

        // act
        var text = RenderToText(mode, 80, 20);
        var row = Assert.Single(text.Split('\n'), l => l.Contains("agent-a") && l.Contains("reg "));

        // assert: the name column is immediately followed by a dash for the
        // empty client column, then the (possibly truncated) role.
        Assert.Contains("agent-a - backe", row);
    }

    [Fact]
    public void Render_Should_ShowClient_In_IdentitySection()
    {
        // arrange
        var registry = new FakeAgentRegistry();
        registry.Agents.Add(Agent("agent-a", client: "claude-code"));
        var mode = CreateMode(registry);
        mode.OnEnter();

        // act
        var text = RenderToText(mode);

        // assert
        Assert.Contains("Client: claude-code", text);
    }

    [Fact]
    public void Render_Should_ShowDashForClient_When_ClientEmpty_In_IdentitySection()
    {
        // arrange
        var registry = new FakeAgentRegistry();
        registry.Agents.Add(Agent("agent-a"));
        var mode = CreateMode(registry);
        mode.OnEnter();

        // act
        var text = RenderToText(mode);

        // assert
        Assert.Contains("Client: -", text);
    }

    [Fact]
    public void Render_Should_NotThrow_When_WidthOrHeightIsZero()
    {
        // arrange
        var registry = new FakeAgentRegistry();
        var mode = CreateMode(registry);
        mode.OnEnter();

        // act
        var exception = Record.Exception(() => mode.Render(0, 0));

        // assert
        Assert.Null(exception);
    }

    [Fact]
    public void Render_Should_ShowMoreBelowIndicator_When_AgentsExceedPanelHeight()
    {
        // arrange
        var registry = new FakeAgentRegistry();
        for (var i = 1; i <= 15; i++)
        {
            registry.Agents.Add(Agent($"agent-{i:D2}"));
        }

        var mode = CreateMode(registry);
        mode.OnEnter();
        var console = new TestConsole().Width(80).Height(12);

        // act
        console.Write(mode.Render(80, 12));

        // assert
        Assert.Contains("more below", console.Output);
        Assert.DoesNotContain("agent-15", console.Output);
    }

    [Fact]
    public void Render_Should_ShowMoreAboveIndicator_And_SelectedAgent_When_MovedToBottom()
    {
        // arrange
        var registry = new FakeAgentRegistry();
        for (var i = 1; i <= 15; i++)
        {
            registry.Agents.Add(Agent($"agent-{i:D2}"));
        }

        var mode = CreateMode(registry);
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
    public void OnEnter_Should_LoadEveryAgent_ThroughRegistry()
    {
        // arrange
        var registry = new FakeAgentRegistry();
        registry.Agents.Add(Agent("zeta"));
        registry.Agents.Add(Agent("alpha"));
        var mode = CreateMode(registry);

        // act
        mode.OnEnter();

        // assert: FakeAgentRegistry.ListAsync orders by name, mirroring the
        // real registry's ORDER BY name.
        Assert.Equal(["alpha", "zeta"], mode.State.Agents.Select(a => a.Name));
    }

    [Fact]
    public void Render_Should_ShowListAndDetailPanesSideBySide_When_AgentSelected()
    {
        // arrange: the detail pane is always visible next to the list, with
        // no Enter press needed to see the selected agent's identity.
        var registry = new FakeAgentRegistry();
        registry.Agents.Add(Agent("agent-a", role: "backend"));
        var mode = CreateMode(registry);
        mode.OnEnter();

        // act
        var text = RenderToText(mode);

        // assert: the list pane's row and the detail pane's identity
        // section both render in the same frame.
        Assert.Contains("Agents (1)", text);
        Assert.Contains("agent-a", text);
        Assert.Contains("Identity", text);
        Assert.Contains("Role: backend", text);
    }

    [Fact]
    public void MoveSelection_Should_ReloadDetailPane_When_SelectionChanges()
    {
        // arrange
        var registry = new FakeAgentRegistry();
        registry.Agents.Add(Agent("agent-a"));
        registry.Agents.Add(Agent("agent-b"));
        var taskStore = new FakeTaskStore();
        taskStore.Tasks.Add(TaskItemBuilder.Create("b-1", assignee: "agent-b"));
        var mode = CreateMode(registry, taskStore, new FakeMailStore());
        mode.OnEnter();
        Assert.DoesNotContain("b-1", RenderToText(mode));

        // act: moving onto agent-b reloads the detail pane with its tasks.
        mode.Handle(new TuiMessage.MoveCursor(CursorDirection.Down));

        // assert
        Assert.Contains("b-1", RenderToText(mode));
    }

    [Fact]
    public void TogglePane_Should_FlipFocusBetweenListAndDetail()
    {
        // arrange
        var registry = new FakeAgentRegistry();
        registry.Agents.Add(Agent("agent-a"));
        var mode = CreateMode(registry);
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
    public void Refresh_Should_ReloadDetailPane_For_SelectedAgent()
    {
        // arrange
        var registry = new FakeAgentRegistry();
        registry.Agents.Add(Agent("agent-a"));
        var taskStore = new FakeTaskStore();
        var mode = CreateMode(registry, taskStore, new FakeMailStore());
        mode.OnEnter();
        Assert.DoesNotContain("a-1", RenderToText(mode));

        // act: a task assigned to the still-selected agent shows up only
        // once RefreshRequested re-loads the detail pane, not just the list.
        taskStore.Tasks.Add(TaskItemBuilder.Create("a-1", assignee: "agent-a"));
        mode.Handle(new TuiMessage.RefreshRequested());

        // assert
        Assert.Contains("a-1", RenderToText(mode));
    }

    [Fact]
    public void Refresh_Should_ClearDetailPane_When_ListEmpties()
    {
        // arrange
        var registry = new FakeAgentRegistry();
        registry.Agents.Add(Agent("agent-a", role: "backend"));
        var mode = CreateMode(registry);
        mode.OnEnter();
        Assert.Contains("Role: backend", RenderToText(mode));

        // act: the selected agent vanishes from the registry before the
        // next refresh.
        registry.Agents.Clear();
        mode.Handle(new TuiMessage.RefreshRequested());
        var text = RenderToText(mode);

        // assert: the detail pane falls back to its empty state instead of
        // continuing to show the vanished agent's stale identity.
        Assert.Contains("Agents (0)", text);
        Assert.DoesNotContain("Role: backend", text);
    }

    [Fact]
    public void Render_Should_AlignColumns_Across_Rows()
    {
        // arrange: names of different lengths so the role and age columns
        // only line up if they're padded to a shared width rather than
        // following each name immediately.
        var registry = new FakeAgentRegistry();
        registry.Agents.Add(Agent("a", role: "backend"));
        registry.Agents.Add(Agent("longer-name", role: "qa"));
        var mode = CreateMode(registry);
        mode.OnEnter();

        // act
        var text = RenderToText(mode);
        var rows = text.Split('\n').Where(l => l.Contains("reg ") && l.Contains("seen ")).ToList();
        var shortNameRow = Assert.Single(rows, l => l.Contains("backend"));
        var longNameRow = Assert.Single(rows, l => l.Contains("qa"));

        // assert: the role column and the reg-age column start at the same
        // character offset on both rows.
        Assert.Equal(
            shortNameRow.IndexOf("backend", StringComparison.Ordinal),
            longNameRow.IndexOf("qa", StringComparison.Ordinal));
        Assert.Equal(
            shortNameRow.IndexOf("reg ", StringComparison.Ordinal),
            longNameRow.IndexOf("reg ", StringComparison.Ordinal));
    }

    [Fact]
    public void Render_Should_ApplyAnsiStyling_ToNameAndClientTokens_InTheListRow()
    {
        // arrange: agent-a stays unstyled at the selected row (row 0); the
        // attribute under test lives on agent-b's unselected row instead, so
        // the assertion can be pinned to a single line.
        var registry = new FakeAgentRegistry();
        registry.Agents.Add(Agent("agent-a"));
        registry.Agents.Add(Agent("agent-b", role: "backend", client: "claude-code"));
        var mode = CreateMode(registry);
        mode.OnEnter();
        var console = new TestConsole().Colors(ColorSystem.TrueColor).EmitAnsiSequences().Width(80).Height(20);

        // act
        console.Write(mode.Render(80, 20));

        // assert: agents.list.name collides with board.column.border.focused
        // and agents.list.client's token can appear elsewhere in the frame,
        // so a whole-frame Contains would stay vacuous; pin to agent-b's row.
        var row = Assert.Single(console.Output.Split('\n'), l => l.Contains("agent-b"));
        AssertAnsiStyleApplied(row, "agents.list.name");
        AssertAnsiStyleApplied(row, "agents.list.client");
    }

    [Fact]
    public void Render_Should_ApplyAnsiStyling_ToPerRoleToken_When_RoleHasADedicatedColor()
    {
        // arrange: "orchestrator" is one of the roles with its own token
        // (agents.list.role.orchestrator); no existing test exercises that
        // per-role branch of AgentRowBadge.RoleStyle end to end, only the
        // plain fallback. agent-a stays plain at the selected row; the role
        // under test lives on agent-b's unselected row.
        var registry = new FakeAgentRegistry();
        registry.Agents.Add(Agent("agent-a"));
        registry.Agents.Add(Agent("agent-b", role: "orchestrator"));
        var mode = CreateMode(registry);
        mode.OnEnter();
        var console = new TestConsole().Colors(ColorSystem.TrueColor).EmitAnsiSequences().Width(80).Height(20);

        // act
        console.Write(mode.Render(80, 20));

        // assert: pin to agent-b's row rather than the whole frame, since
        // agents.list.role.orchestrator's token could otherwise be satisfied
        // by an unrelated part of the render.
        Assert.NotEqual(
            ThemeTokens.GetStyle("agents.list.role"),
            ThemeTokens.GetStyle("agents.list.role.orchestrator"));
        var row = Assert.Single(console.Output.Split('\n'), l => l.Contains("agent-b"));
        AssertAnsiStyleApplied(row, "agents.list.role.orchestrator");
    }

    [Fact]
    public void Render_Should_ApplyAnsiStyling_ToBaseRoleToken_When_RoleHasNoDedicatedColor()
    {
        // arrange: "backend" has no per-role token, so RoleStyle must fall
        // back to the plain agents.list.role token rather than rendering
        // unstyled. agent-a stays plain at the selected row; the role under
        // test lives on agent-b's unselected row.
        var registry = new FakeAgentRegistry();
        registry.Agents.Add(Agent("agent-a"));
        registry.Agents.Add(Agent("agent-b", role: "backend"));
        var mode = CreateMode(registry);
        mode.OnEnter();
        var console = new TestConsole().Colors(ColorSystem.TrueColor).EmitAnsiSequences().Width(80).Height(20);

        // act
        console.Write(mode.Render(80, 20));

        // assert: pin to agent-b's row rather than the whole frame.
        var row = Assert.Single(console.Output.Split('\n'), l => l.Contains("agent-b"));
        AssertAnsiStyleApplied(row, "agents.list.role");
    }
}
