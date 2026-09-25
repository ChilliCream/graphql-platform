using ChilliCream.Nitro.CommandLine.Services.Workspace;
using ChilliCream.Nitro.CommandLine.Tests.Tui.Agents;
using ChilliCream.Nitro.CommandLine.Tui.Input;
using ChilliCream.Nitro.CommandLine.Tui.Mail;
using ChilliCream.Nitro.CommandLine.Tui.Shell;
using Microsoft.Extensions.Time.Testing;
using Spectre.Console.Testing;
using CursorDirection = ChilliCream.Nitro.CommandLine.Tui.Input.CursorDirection;

namespace ChilliCream.Nitro.CommandLine.Tests.Tui.Mail;

public sealed class MailModeTests
{
    private static readonly DateTimeOffset s_now = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    private static MailMode CreateMode(FakeMailStore store, FakeAgentStore? agentStore = null)
        => new(store, agentStore ?? new FakeAgentStore(new FakeTimeProvider(s_now)), new FakeTimeProvider(s_now));

    private static AgentRow Agent(string name) => new()
    {
        Name = name,
        Role = "",
        HarnessVersion = "",
        Cwd = "",
        WorkspacePath = "",
        RegisteredAt = s_now,
        StartedAt = s_now,
        LastSeenAt = s_now,
        EndpointKind = AgentSessionEndpointKind.None,
        EndpointAddr = "",
        BlockBudgetUsed = 0,
        AnnouncementPending = false,
        IdlePushArmed = false
    };

    private static void AddThread(
        FakeMailStore store, string threadId, string sender, string recipient, DateTimeOffset createdAt)
        => store.Messages.Add(MailMessageBuilder.Create(
            threadId, sender: sender, threadId: threadId, createdAt: createdAt,
            recipients: [MailMessageBuilder.ToRecipient(recipient)]));

    private static ConsoleKeyInfo Key(char c) => new(c, ConsoleKey.NoName, false, false, false);

    private static ConsoleKeyInfo Key(ConsoleKey key) => new('\0', key, false, false, false);

    private static ConsoleKeyInfo CtrlKey(ConsoleKey key) => new('\0', key, false, false, true);

    private static void Type(MailMode mode, string text)
    {
        foreach (var c in text)
        {
            mode.HandleRawKey(Key(c));
        }
    }

    private static string RenderToText(MailMode mode, int width = 100, int height = 24)
    {
        var console = new TestConsole().Width(width);
        console.Write(mode.Render(width, height));
        return console.Output;
    }

    [Fact]
    public void Render_Should_ShowTheEmptyStateMessage_When_NoMailExists()
    {
        // arrange
        var store = new FakeMailStore();
        var mode = CreateMode(store);
        mode.OnEnter();

        // act
        var text = RenderToText(mode);

        // assert
        Assert.Contains("No mail yet.", text);
    }

    [Fact]
    public void Render_Should_ShowTheThreadCount_When_HeaderIsRendered()
    {
        // arrange
        var store = new FakeMailStore();
        AddThread(store, "t-1", "bob", "alice", s_now);
        AddThread(store, "t-2", "bob", "alice", s_now.AddMinutes(1));
        var mode = CreateMode(store);
        mode.OnEnter();

        // act
        var text = RenderToText(mode);

        // assert
        Assert.Contains("Mail (2)", text);
    }

    [Fact]
    public void Render_Should_OrderThreadsByLastActivityDescending_When_ThreeThreadsExist()
    {
        // arrange
        var store = new FakeMailStore();
        AddThread(store, "t-1", "bob", "alice", s_now);
        AddThread(store, "t-2", "carol", "alice", s_now.AddMinutes(-2));
        AddThread(store, "t-3", "dave", "alice", s_now.AddMinutes(-1));
        var mode = CreateMode(store);
        mode.OnEnter();

        // act
        var text = RenderToText(mode);

        // assert
        var t1Index = text.IndexOf("bob", StringComparison.Ordinal);
        var t3Index = text.IndexOf("dave", StringComparison.Ordinal);
        var t2Index = text.IndexOf("carol", StringComparison.Ordinal);
        Assert.True(t1Index >= 0 && t3Index > t1Index && t2Index > t3Index);
    }

    [Fact]
    public void Render_Should_ShowHeaderRuleAndRows_When_ThreeThreadsArePresent()
    {
        // arrange
        var store = new FakeMailStore();
        AddThread(store, "t-1", "bob", "alice", s_now);
        AddThread(store, "t-2", "carol", "alice", s_now.AddMinutes(-2));
        AddThread(store, "t-3", "dave", "alice", s_now.AddMinutes(-1));
        var mode = CreateMode(store);
        mode.OnEnter();
        var console = new TestConsole().Width(100);

        // act
        console.Write(mode.Render(100, 9));

        // assert
        console.Output.MatchInlineSnapshot(
            """
            ╭─Mail (3)─────────────────────────────────────────────────────────────────────────────────────────╮
            │                                                                                                  │
            │    SUBJECT                     FROM          TO            MESSAGES    LAST ACTIVITY             │
            │ ──────────────────────────────────────────────────────────────────────────────────────────────── │
            │                                                                                                  │
            │ >  Subject                     bob           alice                1    just now                  │
            │    Subject                     dave          alice                1    1m ago                    │
            │    Subject                     carol         alice                1    2m ago                    │
            ╰──────────────────────────────────────────────────────────────────────────────────────────────────╯

            """);
    }

    [Fact]
    public void MoveSelection_Should_ClampAtLastRow_When_MovingDownPastEnd()
    {
        // arrange
        var store = new FakeMailStore();
        AddThread(store, "t-1", "bob", "alice", s_now);
        AddThread(store, "t-2", "carol", "alice", s_now.AddMinutes(1));
        var mode = CreateMode(store);
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
        var store = new FakeMailStore();
        AddThread(store, "t-1", "bob", "alice", s_now);
        AddThread(store, "t-2", "carol", "alice", s_now.AddMinutes(1));
        AddThread(store, "t-3", "dave", "alice", s_now.AddMinutes(2));
        var mode = CreateMode(store);
        mode.OnEnter();

        // act
        mode.Handle(new TuiMessage.MoveToEdge(EdgeTarget.Bottom));

        // assert
        Assert.Equal(2, mode.State.SelectedRow);
    }

    [Fact]
    public void OpenSelected_Should_BeANoOp_When_AThreadIsSelected()
    {
        // arrange
        var store = new FakeMailStore();
        AddThread(store, "t-1", "bob", "alice", s_now);
        var mode = CreateMode(store);
        mode.OnEnter();

        // act
        var followUp = mode.Handle(new TuiMessage.OpenSelected());

        // assert
        Assert.Empty(followUp);
        Assert.Equal(0, mode.State.SelectedRow);
    }

    [Fact]
    public void CopySelectedId_Should_ReturnTheThreadId_When_AThreadIsSelected()
    {
        // arrange
        var store = new FakeMailStore();
        AddThread(store, "t-1", "bob", "alice", s_now);
        var mode = CreateMode(store);
        mode.OnEnter();

        // act
        var followUp = mode.Handle(new TuiMessage.CopySelectedId());

        // assert
        var toast = Assert.Single(followUp);
        var shown = Assert.IsType<TuiMessage.ShowToast>(toast);
        Assert.Equal("t-1", shown.Text);
    }

    [Fact]
    public void CopySelectedId_Should_ReturnAWarning_When_NoThreadIsSelected()
    {
        // arrange
        var store = new FakeMailStore();
        var mode = CreateMode(store);
        mode.OnEnter();

        // act
        var followUp = mode.Handle(new TuiMessage.CopySelectedId());

        // assert
        var toast = Assert.Single(followUp);
        var shown = Assert.IsType<TuiMessage.ShowToast>(toast);
        Assert.Equal(ToastStyle.Warn, shown.Style);
    }

    [Fact]
    public void TryCreatePopover_Should_ReturnNull_When_NoThreadIsSelected()
    {
        // arrange
        var store = new FakeMailStore();
        var mode = CreateMode(store);
        mode.OnEnter();

        // act
        var popover = mode.TryCreatePopover();

        // assert
        Assert.Null(popover);
    }

    [Fact]
    public void TryCreatePopover_Should_ReturnAPopoverForTheSelectedThread_When_AThreadIsSelected()
    {
        // arrange
        var store = new FakeMailStore();
        AddThread(store, "t-1", "bob", "alice", s_now);
        var mode = CreateMode(store);
        mode.OnEnter();

        // act
        var popover = mode.TryCreatePopover();
        popover?.Load(TestContext.Current.CancellationToken);
        var console = new TestConsole().Width(100);
        console.Write(popover!.Render(100, 30));

        // assert
        Assert.NotNull(popover);
        Assert.Contains("Participants: alice, bob", console.Output);
    }

    [Fact]
    public void HandlePopoverRequest_Should_ReturnACopyToast_When_TheThreadPopoverRequestsACopy()
    {
        // arrange
        var store = new FakeMailStore();
        var mode = CreateMode(store);
        var request = new PopoverResult.Request(new MailThreadPopoverRequest.CopyRequested("t-1"));

        // act
        var followUp = mode.HandlePopoverRequest(request);

        // assert
        var toast = Assert.Single(followUp);
        var shown = Assert.IsType<TuiMessage.ShowToast>(toast);
        Assert.Equal(("t-1", ToastStyle.Info), (shown.Text, shown.Style));
    }

    [Fact]
    public void RefreshRequested_Should_ReloadThreadsFromTheStore_When_ANewThreadWasAdded()
    {
        // arrange
        var store = new FakeMailStore();
        var mode = CreateMode(store);
        mode.OnEnter();
        Assert.Empty(mode.State.Threads);
        AddThread(store, "t-1", "bob", "alice", s_now);

        // act
        mode.Handle(new TuiMessage.RefreshRequested());

        // assert
        Assert.Single(mode.State.Threads);
    }

    [Fact]
    public void SearchRequested_Should_OpenTheSearchForm_When_Requested()
    {
        // arrange
        var store = new FakeMailStore();
        var mode = CreateMode(store);
        mode.OnEnter();

        // act
        var followUp = mode.Handle(new TuiMessage.SearchRequested());

        // assert
        Assert.Empty(followUp);
        Assert.True(mode.IsInputCapturing);
    }

    [Fact]
    public void SearchForm_Apply_Should_NarrowThreadsBySubject_When_TextMatchesOneThread()
    {
        // arrange
        var store = new FakeMailStore();
        store.Messages.Add(MailMessageBuilder.Create(
            "t-1", subject: "Status update", threadId: "t-1", createdAt: s_now,
            recipients: [MailMessageBuilder.ToRecipient("alice")]));
        store.Messages.Add(MailMessageBuilder.Create(
            "t-2", subject: "Lunch plans", threadId: "t-2", createdAt: s_now.AddMinutes(1),
            recipients: [MailMessageBuilder.ToRecipient("alice")]));
        var mode = CreateMode(store);
        mode.OnEnter();
        mode.Handle(new TuiMessage.SearchRequested());
        Type(mode, "status");

        // act
        mode.HandleRawKey(CtrlKey(ConsoleKey.S));

        // assert
        Assert.False(mode.IsInputCapturing);
        var thread = Assert.Single(mode.State.Threads);
        Assert.Equal("t-1", thread.ThreadId);
    }

    [Fact]
    public void SearchForm_Cancel_Should_LeaveThreadsUnfiltered_When_FormIsCancelled()
    {
        // arrange
        var store = new FakeMailStore();
        AddThread(store, "t-1", "bob", "alice", s_now);
        AddThread(store, "t-2", "carol", "alice", s_now.AddMinutes(1));
        var mode = CreateMode(store);
        mode.OnEnter();
        mode.Handle(new TuiMessage.SearchRequested());
        Type(mode, "no-match");

        // act
        mode.HandleRawKey(Key(ConsoleKey.Escape));

        // assert
        Assert.False(mode.IsInputCapturing);
        Assert.Equal(2, mode.State.Threads.Count);
    }

    [Fact]
    public void AgentFilterPickerRequested_Should_ListAgentsByName_When_StoreReturnsThemOutOfOrder()
    {
        // arrange
        var store = new FakeMailStore();
        var agentStore = new FakeAgentStore(new FakeTimeProvider(s_now));
        agentStore.Seed(Agent("zeta"));
        agentStore.Seed(Agent("alpha"));
        agentStore.Seed(Agent("mike"));
        var mode = CreateMode(store, agentStore);
        mode.OnEnter();
        mode.Handle(new TuiMessage.AgentFilterPickerRequested());
        var console = new TestConsole().Width(70).Height(6);

        // act
        console.Write(mode.Render(70, 6));

        // assert
        console.Output.MatchInlineSnapshot(
            """
            ╭─Filter by agent──────────────────────────────────────────────╮
            │ (o) All agents                                               │
            │ ( ) alpha                                                    │
            │ ( ) mike                                                     │
            │ ( ) zeta                                                     │
            ╰──────────────────────────────────────────────────────────────╯
            """);
    }

    [Fact]
    public void AgentFilterPicker_Applied_Should_NarrowThreads_ToThreadsTheAgentSentOrReceived_When_AnAgentIsSelected()
    {
        // arrange
        var store = new FakeMailStore();
        AddThread(store, "t-1", "bob", "carol", s_now);
        AddThread(store, "t-2", "dave", "erin", s_now.AddMinutes(1));
        var agentStore = new FakeAgentStore(new FakeTimeProvider(s_now));
        agentStore.Seed(Agent("bob"));
        var mode = CreateMode(store, agentStore);
        mode.OnEnter();
        mode.Handle(new TuiMessage.AgentFilterPickerRequested());

        // act
        // move down from "All agents" to "bob", then apply
        mode.HandleRawKey(Key(ConsoleKey.DownArrow));
        var followUp = mode.HandleRawKey(Key(ConsoleKey.Enter));

        // assert
        Assert.Empty(followUp);
        Assert.False(mode.IsInputCapturing);
        Assert.Equal("bob", mode.State.AgentFilter);
        Assert.Equal(["t-1"], mode.State.Threads.Select(t => t.ThreadId));
    }

    [Fact]
    public void AgentFilterPicker_Applied_AllAgents_Should_RestoreEveryThread_When_AllAgentsIsSelected()
    {
        // arrange
        var store = new FakeMailStore();
        AddThread(store, "t-1", "bob", "carol", s_now);
        AddThread(store, "t-2", "dave", "erin", s_now.AddMinutes(1));
        var agentStore = new FakeAgentStore(new FakeTimeProvider(s_now));
        agentStore.Seed(Agent("bob"));
        var mode = CreateMode(store, agentStore);
        mode.OnEnter();
        mode.Handle(new TuiMessage.AgentFilterPickerRequested());
        mode.HandleRawKey(Key(ConsoleKey.DownArrow));
        mode.HandleRawKey(Key(ConsoleKey.Enter));
        Assert.Equal(["t-1"], mode.State.Threads.Select(t => t.ThreadId));

        // act
        // reopen the picker (pre-selected on "bob") and move back up to "All agents", then apply
        mode.Handle(new TuiMessage.AgentFilterPickerRequested());
        mode.HandleRawKey(Key(ConsoleKey.UpArrow));
        var followUp = mode.HandleRawKey(Key(ConsoleKey.Enter));

        // assert
        Assert.Empty(followUp);
        Assert.Null(mode.State.AgentFilter);
        Assert.Equal(["t-2", "t-1"], mode.State.Threads.Select(t => t.ThreadId));
    }

    [Fact]
    public void AgentFilterPicker_Cancelled_Should_LeaveTheFilterUnchanged_When_PickerIsCancelled()
    {
        // arrange
        var store = new FakeMailStore();
        AddThread(store, "t-1", "bob", "carol", s_now);
        var agentStore = new FakeAgentStore(new FakeTimeProvider(s_now));
        agentStore.Seed(Agent("bob"));
        var mode = CreateMode(store, agentStore);
        mode.OnEnter();
        var threadsBeforeCancel = mode.State.Threads.Select(t => t.ThreadId).ToList();
        mode.Handle(new TuiMessage.AgentFilterPickerRequested());
        mode.HandleRawKey(Key(ConsoleKey.DownArrow));

        // act
        var followUp = mode.HandleRawKey(Key(ConsoleKey.Escape));

        // assert
        Assert.Empty(followUp);
        Assert.False(mode.IsInputCapturing);
        Assert.Null(mode.State.AgentFilter);
        Assert.Equal(threadsBeforeCancel, mode.State.Threads.Select(t => t.ThreadId));
    }

    [Fact]
    public void Render_Should_ShowTheSelectedAgentInTheHeader_When_AgentFilterIsSet()
    {
        // arrange
        var store = new FakeMailStore();
        AddThread(store, "t-1", "bob", "carol", s_now);
        var agentStore = new FakeAgentStore(new FakeTimeProvider(s_now));
        agentStore.Seed(Agent("bob"));
        var mode = CreateMode(store, agentStore);
        mode.OnEnter();
        mode.Handle(new TuiMessage.AgentFilterPickerRequested());
        mode.HandleRawKey(Key(ConsoleKey.DownArrow));
        mode.HandleRawKey(Key(ConsoleKey.Enter));

        // act
        var text = RenderToText(mode);

        // assert
        Assert.Contains("Mail: bob (1)", text);
    }

    [Fact]
    public void Render_Should_DropToColumn_When_WidthIsTooNarrowForEveryColumn()
    {
        // arrange
        var store = new FakeMailStore();
        AddThread(store, "t-1", "bob", "alice", s_now);
        var mode = CreateMode(store);
        mode.OnEnter();
        var wide = RenderToText(mode, width: 100);

        // act
        var narrow = RenderToText(mode, width: 74);
        var actual = (
            WideHasTo: wide.Contains("alice", StringComparison.Ordinal),
            NarrowHasTo: narrow.Contains("alice", StringComparison.Ordinal),
            NarrowHasFrom: narrow.Contains("bob", StringComparison.Ordinal));

        // assert
        Assert.Equal((true, false, true), actual);
    }
}
