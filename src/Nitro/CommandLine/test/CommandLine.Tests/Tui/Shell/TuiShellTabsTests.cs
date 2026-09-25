using ChilliCream.Nitro.CommandLine.Services.Tasks;
using ChilliCream.Nitro.CommandLine.Services.Workspace;
using ChilliCream.Nitro.CommandLine.Tests.Tui.Mail;
using ChilliCream.Nitro.CommandLine.Tui.Agents;
using ChilliCream.Nitro.CommandLine.Tui.Board;
using ChilliCream.Nitro.CommandLine.Tui.Input;
using ChilliCream.Nitro.CommandLine.Tui.Mail;
using ChilliCream.Nitro.CommandLine.Tui.Runtime;
using ChilliCream.Nitro.CommandLine.Tui.Search;
using ChilliCream.Nitro.CommandLine.Tui.Shell;
using Spectre.Console.Testing;

namespace ChilliCream.Nitro.CommandLine.Tests.Tui.Shell;

/// <summary>
/// Covers <see cref="TuiShell"/>'s tabbed-hosting constructor: the tab
/// strip, per-tab dispatcher and mode-stack isolation, tab-switch key
/// handling, and the shell-level broadcast of a data-changed refresh to
/// every hosted tab. <see cref="TuiShellTests"/> covers the single-mode
/// constructor and the shell-level overlay machinery those tabbed tests
/// reuse.
/// </summary>
public sealed class TuiShellTabsTests
{
    private static ConsoleKeyInfo KeyInfo(char keyChar, ConsoleKey key, ConsoleModifiers modifiers = ConsoleModifiers.None) =>
        new(
            keyChar,
            key,
            modifiers.HasFlag(ConsoleModifiers.Shift),
            modifiers.HasFlag(ConsoleModifiers.Alt),
            modifiers.HasFlag(ConsoleModifiers.Control));

    private static string RenderToText(TuiShell shell, int width = 80)
    {
        var console = new TestConsole().Width(width);
        console.Write(shell.Render());
        return console.Output;
    }

    private static TuiTab CreateTasksTab(string title, ITuiMode mode, char mnemonic = 'T') =>
        new(title, mnemonic, mode, new KeyDispatcher(KeyMap.CreateDefaultGlobal()));

    private static TuiTab CreateMailTab(string title, ITuiMode mode, char mnemonic = 'M') =>
        new(title, mnemonic, mode, new KeyDispatcher(MailKeyMap.CreateDefault()));

    private static TuiTab CreateAgentsTab(string title, ITuiMode mode, char mnemonic = 'A') =>
        new(title, mnemonic, mode, new KeyDispatcher(KeyMap.CreateDefaultGlobal()));

    private static AgentRecord Agent(string name, string role = "") => new()
    {
        Name = name,
        Role = role,
        Client = "",
        Implicit = false,
        RegisteredAt = DateTimeOffset.UnixEpoch,
        LastSeenAt = DateTimeOffset.UnixEpoch
    };

    [Fact]
    public void Constructor_Should_CallOnEnter_OnEveryHostedTab_NotOnlyTheActiveOne()
    {
        // arrange
        var tab1Mode = new FakeTuiMode();
        var tab2Mode = new FakeTuiMode();

        // act
        _ = new TuiShell([CreateTasksTab("Tasks", tab1Mode), CreateMailTab("Mail", tab2Mode)], 80, 24);

        // assert
        Assert.True(tab1Mode.EnterCalled);
        Assert.True(tab2Mode.EnterCalled);
    }

    [Fact]
    public void Render_Should_ShowATabStrip_WithEveryTabsTitle_When_MoreThanOneTabIsHosted()
    {
        // arrange
        var shell = new TuiShell(
            [CreateTasksTab("Tasks", new FakeTuiMode()), CreateMailTab("Mail", new FakeTuiMode())], 80, 24);

        // act
        var text = RenderToText(shell);

        // assert
        Assert.Contains("[T]asks", text);
        Assert.Contains("[M]ail", text);
    }

    [Fact]
    public void Render_Should_BracketEachTabsMnemonic_InBothActiveAndInactiveState()
    {
        // arrange
        var shell = new TuiShell(
            [
                CreateTasksTab("Tasks", new FakeTuiMode()),
                CreateMailTab("Mail", new FakeTuiMode()),
                CreateAgentsTab("Agents", new FakeTuiMode())
            ],
            80,
            24);

        // act
        var text = RenderToText(shell);

        // assert
        Assert.Contains("[T]asks", text);
        Assert.Contains("[M]ail", text);
        Assert.Contains("[A]gents", text);
    }

    [Fact]
    public void Handle_Should_JumpToTheMnemonicsTab_When_ShiftPlusItsLetterIsPressed_FromAnyTab()
    {
        // arrange
        var tasksMode = new FakeTuiMode();
        var mailMode = new FakeTuiMode();
        var agentsMode = new FakeTuiMode();
        var shell = new TuiShell(
            [
                CreateTasksTab("Tasks", tasksMode),
                CreateMailTab("Mail", mailMode),
                CreateAgentsTab("Agents", agentsMode)
            ],
            80,
            24);

        // act: Shift+A jumps straight from the (active) tasks tab to agents.
        var dirty = shell.Handle(new TuiEvent.KeyEvent(KeyInfo('A', ConsoleKey.A, ConsoleModifiers.Shift)));

        // assert
        Assert.True(dirty);
        Assert.Single(agentsMode.ResizeCalls);

        // act: Shift+M jumps from agents straight to mail, skipping tasks.
        shell.Handle(new TuiEvent.KeyEvent(KeyInfo('M', ConsoleKey.M, ConsoleModifiers.Shift)));

        // assert
        Assert.Single(mailMode.ResizeCalls);
        Assert.Empty(tasksMode.ResizeCalls);

        // act: Shift+T jumps back to tasks.
        shell.Handle(new TuiEvent.KeyEvent(KeyInfo('T', ConsoleKey.T, ConsoleModifiers.Shift)));

        // assert
        Assert.Single(tasksMode.ResizeCalls);
    }

    [Fact]
    public void Handle_Should_DoNothing_When_ShiftPlusLetterMatchesTheAlreadyActiveTab()
    {
        // arrange
        var tasksMode = new FakeTuiMode();
        var shell = new TuiShell(
            [CreateTasksTab("Tasks", tasksMode), CreateMailTab("Mail", new FakeTuiMode())], 80, 24);

        // act
        var dirty = shell.Handle(new TuiEvent.KeyEvent(KeyInfo('T', ConsoleKey.T, ConsoleModifiers.Shift)));

        // assert
        Assert.False(dirty);
        Assert.Empty(tasksMode.ResizeCalls);
    }

    [Fact]
    public void Handle_Should_StillReachTheModeKey_When_ItsLowercaseCounterpartIsNotAMnemonic()
    {
        // arrange
        var tasksMode = new FakeTuiMode();
        var shell = new TuiShell(
            [
                CreateTasksTab("Tasks", tasksMode),
                CreateMailTab("Mail", new FakeTuiMode()),
                CreateAgentsTab("Agents", new FakeTuiMode())
            ],
            80,
            24);

        // act
        shell.Handle(new TuiEvent.KeyEvent(KeyInfo('r', ConsoleKey.R)));

        // assert
        Assert.Contains(tasksMode.HandledMessages, m => m is TuiMessage.RefreshRequested);
    }

    [Fact]
    public void Handle_Should_ReserveOneExtraRow_ForTheTabStrip_When_MultipleTabsHosted()
    {
        // arrange
        var mode = new FakeTuiMode();
        var shell = new TuiShell([CreateTasksTab("Tasks", mode), CreateMailTab("Mail", new FakeTuiMode())], 80, 24);

        // act
        shell.Handle(new TuiEvent.ResizeEvent(100, 30));

        // assert
        Assert.Equal((100, 28), Assert.Single(mode.ResizeCalls));
    }

    [Fact]
    public void Handle_Should_RouteKeysOnlyToTheActiveTabsDispatcherAndMode()
    {
        // arrange
        var tasksMode = new FakeTuiMode();
        var mailMode = new FakeTuiMode();
        var shell = new TuiShell([CreateTasksTab("Tasks", tasksMode), CreateMailTab("Mail", mailMode)], 80, 24);

        // act
        shell.Handle(new TuiEvent.KeyEvent(KeyInfo('r', ConsoleKey.R)));

        // assert
        Assert.Contains(tasksMode.HandledMessages, m => m is TuiMessage.RefreshRequested);
        Assert.Empty(mailMode.HandledMessages);

        // act: switch to the mail tab and press 'r' again.
        shell.Handle(new TuiEvent.KeyEvent(KeyInfo(']', ConsoleKey.Oem6)));
        tasksMode.HandledMessages.Clear();
        shell.Handle(new TuiEvent.KeyEvent(KeyInfo('r', ConsoleKey.R)));

        // assert
        Assert.Contains(mailMode.HandledMessages, m => m is TuiMessage.ReplyRequested);
        Assert.Empty(tasksMode.HandledMessages);
    }

    [Fact]
    public void Handle_Should_WrapAround_When_SwitchingPastTheLastOrFirstTab()
    {
        // arrange
        var tab1Mode = new FakeTuiMode();
        var tab2Mode = new FakeTuiMode();
        var shell = new TuiShell([CreateTasksTab("Tasks", tab1Mode), CreateMailTab("Mail", tab2Mode)], 80, 24);

        // act: '[' from the first tab wraps to the last.
        shell.Handle(new TuiEvent.KeyEvent(KeyInfo('[', ConsoleKey.Oem4)));

        // assert
        Assert.Single(tab2Mode.ResizeCalls);

        // act: ']' from the last tab wraps back to the first.
        shell.Handle(new TuiEvent.KeyEvent(KeyInfo(']', ConsoleKey.Oem6)));

        // assert
        Assert.Single(tab1Mode.ResizeCalls);
    }

    [Fact]
    public void Handle_Should_SwitchTab_When_TheKeyCarriesNoConsoleKey()
    {
        // arrange
        var tab1Mode = new FakeTuiMode();
        var tab2Mode = new FakeTuiMode();
        var shell = new TuiShell([CreateTasksTab("Tasks", tab1Mode), CreateMailTab("Mail", tab2Mode)], 80, 24);

        // act
        shell.Handle(new TuiEvent.KeyEvent(KeyInfo(']', ConsoleKey.None)));

        // assert
        Assert.Single(tab2Mode.ResizeCalls);
    }

    [Fact]
    public void Constructor_Should_Throw_When_TasksTabIndexIsOutOfRange()
    {
        // arrange
        var tabs = new[] { CreateTasksTab("Tasks", new FakeTuiMode()), CreateMailTab("Mail", new FakeTuiMode()) };

        // act & assert
        Assert.Throws<ArgumentOutOfRangeException>(() => new TuiShell(tabs, 80, 24, tasksTabIndex: 2));
    }

    [Fact]
    public void HandleDataChanged_Should_NotRouteAnInactiveTabsFollowUp_ToTheActiveTab()
    {
        // arrange
        // The inactive mail mode returns a toast when refreshed.
        var mailFollowUp = new TuiMessage.ShowToast("mail refreshed", ToastStyle.Info);
        var tasksMode = new FakeTuiMode();
        var mailMode = new FakeTuiMode
        {
            HandleResult = message => message is TuiMessage.RefreshRequested ? [mailFollowUp] : []
        };
        var shell = new TuiShell([CreateTasksTab("Tasks", tasksMode), CreateMailTab("Mail", mailMode)], 80, 24);
        tasksMode.HandledMessages.Clear();
        mailMode.HandledMessages.Clear();

        // act
        shell.Handle(new TuiEvent.DataChangedEvent());

        // assert
        Assert.Equal([new TuiMessage.RefreshRequested()], mailMode.HandledMessages);
        Assert.Equal([new TuiMessage.RefreshRequested()], tasksMode.HandledMessages);
    }

    [Fact]
    public void Handle_Should_NotSwitchTab_When_ATaskOverlayIsCapturingInput()
    {
        // arrange
        var store = new FakeTaskStore();
        store.Tasks["a-1"] = TaskItemBuilder.Create("a-1", "Board task");
        var view = new BoardView
        {
            Name = "Test",
            Columns = [new ColumnDefinition { Name = "Open", Statuses = [TaskStates.Open] }]
        };
        var board = new BoardMode(new BoardDataLoader(store, TimeProvider.System), [view]);
        var otherMode = new FakeTuiMode();
        var shell = new TuiShell(
            [CreateTasksTab("Tasks", board), CreateTasksTab("Other", otherMode, mnemonic: 'O')],
            80,
            24,
            tasksTabIndex: 0,
            store: store,
            actor: "tester");

        shell.Handle(new TuiEvent.KeyEvent(KeyInfo('e', ConsoleKey.E)));
        Assert.Contains("Edit Task", RenderToText(shell));

        // act
        // ']' is swallowed by the editor form's focused text field, not routed to tab switching.
        shell.Handle(new TuiEvent.KeyEvent(KeyInfo(']', ConsoleKey.Oem6)));

        // assert
        Assert.Empty(otherMode.ResizeCalls);
        Assert.Contains("Edit Task", RenderToText(shell));
    }

    [Fact]
    public void Handle_Should_SwitchTab_Again_OnceTheOverlayThatBlockedItCloses()
    {
        // arrange
        var store = new FakeTaskStore();
        store.Tasks["a-1"] = TaskItemBuilder.Create("a-1", "Board task");
        var view = new BoardView
        {
            Name = "Test",
            Columns = [new ColumnDefinition { Name = "Open", Statuses = [TaskStates.Open] }]
        };
        var board = new BoardMode(new BoardDataLoader(store, TimeProvider.System), [view]);
        var otherMode = new FakeTuiMode();
        var shell = new TuiShell(
            [CreateTasksTab("Tasks", board), CreateTasksTab("Other", otherMode, mnemonic: 'O')],
            80,
            24,
            tasksTabIndex: 0,
            store: store,
            actor: "tester");
        shell.Handle(new TuiEvent.KeyEvent(KeyInfo('e', ConsoleKey.E)));

        // act: Escape closes the (non-dirty) editor, then ']' switches tabs.
        shell.Handle(new TuiEvent.KeyEvent(KeyInfo('', ConsoleKey.Escape)));
        shell.Handle(new TuiEvent.KeyEvent(KeyInfo(']', ConsoleKey.Oem6)));

        // assert
        Assert.Single(otherMode.ResizeCalls);
    }

    [Fact]
    public void Handle_Should_RefreshEveryTabsActiveMode_When_DataChangedEventFires()
    {
        // arrange
        var tab1Mode = new FakeTuiMode();
        var tab2Mode = new FakeTuiMode();
        var shell = new TuiShell([CreateTasksTab("Tasks", tab1Mode), CreateMailTab("Mail", tab2Mode)], 80, 24);

        // act
        var dirty = shell.Handle(new TuiEvent.DataChangedEvent());

        // assert
        Assert.True(dirty);
        Assert.Contains(tab1Mode.HandledMessages, m => m is TuiMessage.RefreshRequested);
        Assert.Contains(tab2Mode.HandledMessages, m => m is TuiMessage.RefreshRequested);
    }

    [Fact]
    public void Render_Should_ShowTheMailTabsUnreadBadge_RefreshedOnDataChanged()
    {
        // arrange
        var mailStore = new FakeMailStore();
        mailStore.Messages.Add(MailMessageBuilder.Create("m1"));
        var mailMode = new MailMode(
            mailStore,
            "actor",
            new Agents.FakeAgentRegistry());
        var mailTab = new TuiTab(
            () => mailMode.UnreadCount > 0 ? $"Mail ({mailMode.UnreadCount})" : "Mail",
            mnemonic: 'M',
            mailMode,
            new KeyDispatcher(MailKeyMap.CreateDefault()));
        var shell = new TuiShell([CreateTasksTab("Tasks", new FakeTuiMode()), mailTab], 80, 24);

        // assert
        Assert.Contains("[M]ail (1)", RenderToText(shell));

        // act
        mailStore.Messages.Add(MailMessageBuilder.Create("m2"));
        shell.Handle(new TuiEvent.DataChangedEvent());

        // assert
        Assert.Contains("[M]ail (2)", RenderToText(shell));
    }

    [Fact]
    public async Task HandleDataChanged_Should_ShowTheMailTabsSendOutcomeToast_When_TheTasksTabIsActive()
    {
        // arrange
        var testToken = TestContext.Current.CancellationToken;
        var mailStore = new FakeMailStore();
        var mailMode = new MailMode(
            mailStore,
            "alice",
            new Agents.FakeAgentRegistry());
        var shell = new TuiShell([CreateTasksTab("Tasks", new FakeTuiMode()), CreateMailTab("Mail", mailMode)], 80, 24);
        mailMode.Handle(new TuiMessage.SelectInboxRequested());
        mailMode.Handle(new TuiMessage.ComposeRequested());

        foreach (var c in "bob")
        {
            mailMode.HandleRawKey(KeyInfo(c, ConsoleKey.NoName));
        }

        mailMode.HandleRawKey(KeyInfo('\0', ConsoleKey.Tab));

        foreach (var c in "Status")
        {
            mailMode.HandleRawKey(KeyInfo(c, ConsoleKey.NoName));
        }

        mailMode.HandleRawKey(KeyInfo('\0', ConsoleKey.Tab));

        foreach (var c in "Body")
        {
            mailMode.HandleRawKey(KeyInfo(c, ConsoleKey.NoName));
        }

        mailMode.HandleRawKey(KeyInfo('\0', ConsoleKey.S, ConsoleModifiers.Control));

        // act
        // Expire the current toast before each refresh until the terminal toast renders.
        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(testToken);
        timeoutCts.CancelAfter(TimeSpan.FromSeconds(5));
        var dirty = false;
        var rendered = RenderToText(shell);

        while (!rendered.Contains("Sent", StringComparison.Ordinal))
        {
            shell.Handle(new TuiEvent.TickEvent(DateTimeOffset.UtcNow + Toaster.s_duration));
            dirty = shell.Handle(new TuiEvent.DataChangedEvent());
            rendered = RenderToText(shell);

            if (!rendered.Contains("Sent", StringComparison.Ordinal))
            {
                await Task.Delay(5, timeoutCts.Token);
            }
        }

        // assert
        Assert.True(dirty);
        Assert.Contains("Sent", rendered);
    }

    [Fact]
    public void Handle_Should_OpenTaskDetail_When_EnterPressedOnBoardSelection_ThroughATabbedShell()
    {
        // arrange
        var store = new FakeTaskStore();
        store.Tasks["a-1"] = TaskItemBuilder.Create("a-1", "Board task");
        var view = new BoardView
        {
            Name = "Test",
            Columns = [new ColumnDefinition { Name = "Open", Statuses = [TaskStates.Open] }]
        };
        var board = new BoardMode(new BoardDataLoader(store, TimeProvider.System), [view]);
        var shell = new TuiShell(
            [CreateTasksTab("Tasks", board), CreateTasksTab("Other", new FakeTuiMode(), mnemonic: 'O')],
            80,
            24,
            tasksTabIndex: 0,
            store: store,
            actor: "tester");

        // act
        var dirty = shell.Handle(new TuiEvent.KeyEvent(KeyInfo('\r', ConsoleKey.Enter)));

        // assert
        var rendered = RenderToText(shell);
        Assert.True(dirty);
        Assert.Contains("Board task", rendered);
    }

    [Fact]
    public void Handle_Should_AlwaysShowSelectedAgentDetail_WithoutOpeningAnything_ThroughATabbedShell()
    {
        // arrange
        var sessions = new Agents.FakeAgentSessionRegistry();
        sessions.Participants.Add(
            Agents.AgentSessionParticipantBuilder.Participant(
                sessionId: "s-a", agentName: "agent-a", role: "backend", agent: Agent("agent-a", role: "backend")));
        var taskStore = new FakeTaskStore();
        var mailStore = new Agents.FakeMailStore();
        var agentsMode = new AgentsMode(
            taskStore,
            mailStore,
            sessions,
            new Agents.FakeClaudeSessionActivityReader());
        var shell = new TuiShell(
            [CreateAgentsTab("Agents", agentsMode)],
            100,
            24,
            tasksTabIndex: 0,
            store: taskStore);
        Assert.Contains("backend", RenderToText(shell, width: 100));

        // act
        // Enter focuses the already-visible detail pane instead of pushing a full-screen mode.
        var dirty = shell.Handle(new TuiEvent.KeyEvent(KeyInfo('\r', ConsoleKey.Enter)));

        // assert
        var rendered = RenderToText(shell, width: 100);
        Assert.True(dirty);
        Assert.Equal(AgentsFocus.Detail, agentsMode.State.Focus);
        Assert.Contains("agent-a", rendered);
        Assert.Contains("backend", rendered);
    }

    [Fact]
    public void Handle_Should_LeaveAgentsListSelectionUntouched_When_EscapePressed()
    {
        // arrange
        var sessions = new Agents.FakeAgentSessionRegistry();
        sessions.Participants.Add(
            Agents.AgentSessionParticipantBuilder.Participant(
                sessionId: "s-a", agentName: "agent-a"));
        var taskStore = new FakeTaskStore();
        var mailStore = new Agents.FakeMailStore();
        var agentsMode = new AgentsMode(
            taskStore,
            mailStore,
            sessions,
            new Agents.FakeClaudeSessionActivityReader());
        var shell = new TuiShell(
            [CreateAgentsTab("Agents", agentsMode)],
            80,
            24,
            tasksTabIndex: 0,
            store: taskStore);

        // act
        var dirty = shell.Handle(new TuiEvent.KeyEvent(KeyInfo('\x1b', ConsoleKey.Escape)));

        // assert
        Assert.True(dirty);
        Assert.Equal("agent-a", agentsMode.State.SelectedParticipant?.Participant.Session.AgentName);
        Assert.Contains("Agents (1)", RenderToText(shell));
    }

    [Fact]
    public void TaskOverlayGesture_Should_DoNothing_When_TheTasksTabIsNotActive()
    {
        // arrange
        var store = new FakeTaskStore();
        store.Tasks["a"] = TaskItemBuilder.Create("a");
        var otherMode = new FakeTuiMode { SelectedTaskId = "a" };
        var shell = new TuiShell(
            [CreateTasksTab("Tasks", new FakeTuiMode()), CreateTasksTab("Other", otherMode, mnemonic: 'O')],
            80,
            24,
            tasksTabIndex: 0,
            store: store,
            actor: "tester");
        shell.Handle(new TuiEvent.KeyEvent(KeyInfo(']', ConsoleKey.Oem6)));

        // act: 'e' would open the task editor on the tasks tab.
        shell.Handle(new TuiEvent.KeyEvent(KeyInfo('e', ConsoleKey.E)));

        // assert: no editor opened on the (non-tasks) active tab.
        Assert.DoesNotContain("Edit Task", RenderToText(shell));
    }

    [Fact]
    public void Handle_Should_PreserveEachTabsNavigationStack_AcrossTabSwitches_AndKeepBackWithinTheTab()
    {
        // arrange
        var store = new FakeTaskStore();
        var searchMode = new SearchMode(store);
        var board = new FakeTuiMode { RenderText = "board" };
        var shell = new TuiShell(
            [CreateTasksTab("Tasks", board), CreateTasksTab("Other", new FakeTuiMode { RenderText = "other" }, mnemonic: 'O')],
            80,
            24,
            tasksTabIndex: 0,
            searchMode: searchMode,
            store: store,
            actor: "tester");

        // act: enter search from the tasks tab's board root.
        shell.Handle(new TuiEvent.KeyEvent(KeyInfo('/', ConsoleKey.Oem2)));
        Assert.Contains("Results", RenderToText(shell));

        // act
        // Move focus off the query input (Tab), then switch to the other tab and back.
        shell.Handle(new TuiEvent.KeyEvent(KeyInfo('\t', ConsoleKey.Tab)));
        shell.Handle(new TuiEvent.KeyEvent(KeyInfo(']', ConsoleKey.Oem6)));
        Assert.Contains("other", RenderToText(shell));
        shell.Handle(new TuiEvent.KeyEvent(KeyInfo('[', ConsoleKey.Oem4)));

        // assert
        Assert.Contains("Results", RenderToText(shell));
        Assert.Equal(SearchFocus.List, searchMode.Focus);

        // act
        // Escape walks focus List -> Input (mirroring h), a second Escape leaves search to the board root.
        shell.Handle(new TuiEvent.KeyEvent(KeyInfo('', ConsoleKey.Escape)));
        shell.Handle(new TuiEvent.KeyEvent(KeyInfo('', ConsoleKey.Escape)));

        // assert
        Assert.Contains("board", RenderToText(shell));
    }
}
