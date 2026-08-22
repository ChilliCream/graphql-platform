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

        // assert: the inactive tab's mode is entered too, so its tab-strip
        // title (for example an unread badge) is accurate before it is ever
        // switched to.
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
        // arrange: the tasks tab (index 0) is active by default, the agents
        // tab (index 2) is inactive, covering both the active and inactive
        // tab-strip styles the mnemonic bracket is rendered under.
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

        // assert: tasks was never switched back to, so it still has no
        // resize call at all (only its constructor-time OnEnter).
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

        // assert: the mnemonic resolves to the tasks tab, but it is already
        // active, so the switch (and the resulting repaint) is a no-op.
        Assert.False(dirty);
        Assert.Empty(tasksMode.ResizeCalls);
    }

    [Fact]
    public void Handle_Should_StillReachTheModeKey_When_ItsLowercaseCounterpartIsNotAMnemonic()
    {
        // arrange: proves the mnemonic resolution does not shadow an
        // unrelated, already-bound key. 'r' (refresh) stays reachable on the
        // tasks tab's global table even with tab mnemonics installed.
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
        // arrange: the single-tab constructor reserves only the status row
        // (covered by TuiShellTests), so a second reserved row here isolates
        // the tab strip's own contribution.
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
        // arrange: plain 'r' means refresh on the tasks tab's global table,
        // but reply on the mail tab's own key table, so it doubles as proof
        // each tab's dispatcher is checked instead of a shared one.
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

        // assert: the mail tab's own table wins now, and the tasks tab
        // (inactive) receives nothing.
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
        // arrange: Linux's Console.ReadKey never sets ConsoleKey.Oem4/Oem6
        // for '['/']', only KeyChar (see TabSwitchKeysTests), so a
        // shell-level case with ConsoleKey.None closes the gap the
        // Oem4/Oem6-only tests above leave for that platform shape.
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
        // arrange: the mail tab (inactive) is scripted to return a
        // follow-up from its own RefreshRequested handling; that follow-up
        // must reach only the mail tab's own mode, never the active tasks
        // tab's mode via the shell's shared HandleMessage.
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

        // assert: the mail tab saw only its own RefreshRequested (its
        // follow-up was not fed back into it), and the active tasks tab saw
        // only its own RefreshRequested too, the mail tab's follow-up never
        // reaching it.
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

        // act: ']' is swallowed by the editor form's focused text field
        // rather than routed to tab switching.
        shell.Handle(new TuiEvent.KeyEvent(KeyInfo(']', ConsoleKey.Oem6)));

        // assert: the other tab was never activated, and the editor is
        // still open (its title field now holds the typed ']').
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
        // arrange: a single watcher notification must reach every tab, not
        // only the active one, so an inactive tab's data (and any badge
        // derived from it) stays current.
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
        var mailMode = new MailMode(mailStore, "actor");
        var mailTab = new TuiTab(
            () => mailMode.UnreadCount > 0 ? $"Mail ({mailMode.UnreadCount})" : "Mail",
            mnemonic: 'M',
            mailMode,
            new KeyDispatcher(MailKeyMap.CreateDefault()));
        var shell = new TuiShell([CreateTasksTab("Tasks", new FakeTuiMode()), mailTab], 80, 24);

        // assert: computed at construction, before the mail tab is ever
        // active; the badge suffix is untouched by the bracketed mnemonic.
        Assert.Contains("[M]ail (1)", RenderToText(shell));

        // act
        mailStore.Messages.Add(MailMessageBuilder.Create("m2"));
        shell.Handle(new TuiEvent.DataChangedEvent());

        // assert
        Assert.Contains("[M]ail (2)", RenderToText(shell));
    }

    [Fact]
    public void Handle_Should_OpenTaskDetail_When_EnterPressedOnBoardSelection_ThroughATabbedShell()
    {
        // arrange: an existing single-mode board interaction, exercised
        // through a tabbed shell to prove tasks-tab parity.
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
        // arrange: the detail pane sits next to the list, so the selected
        // agent's identity is already on screen before any key is pressed.
        var registry = new ChilliCream.Nitro.CommandLine.Tests.Tui.Agents.FakeAgentRegistry();
        registry.Agents.Add(Agent("agent-a", role: "backend"));
        var taskStore = new FakeTaskStore();
        var mailStore = new ChilliCream.Nitro.CommandLine.Tests.Tui.Agents.FakeMailStore();
        var agentsMode = new AgentsMode(registry, taskStore, mailStore);
        var shell = new TuiShell(
            [CreateAgentsTab("Agents", agentsMode)],
            100,
            24,
            tasksTabIndex: 0,
            store: taskStore,
            mailStore: mailStore);
        Assert.Contains("backend", RenderToText(shell, width: 100));

        // act: Enter no longer pushes a full-screen detail mode; it focuses
        // the already-visible detail pane instead.
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
        // arrange: there is no pushed mode to pop anymore, so Escape on the
        // Agents tab is inert rather than navigating anywhere.
        var registry = new ChilliCream.Nitro.CommandLine.Tests.Tui.Agents.FakeAgentRegistry();
        registry.Agents.Add(Agent("agent-a"));
        var taskStore = new FakeTaskStore();
        var mailStore = new ChilliCream.Nitro.CommandLine.Tests.Tui.Agents.FakeMailStore();
        var agentsMode = new AgentsMode(registry, taskStore, mailStore);
        var shell = new TuiShell(
            [CreateAgentsTab("Agents", agentsMode)],
            80,
            24,
            tasksTabIndex: 0,
            store: taskStore,
            mailStore: mailStore);

        // act
        var dirty = shell.Handle(new TuiEvent.KeyEvent(KeyInfo('\x1b', ConsoleKey.Escape)));

        // assert
        Assert.True(dirty);
        Assert.Equal("agent-a", agentsMode.State.SelectedAgent?.Name);
        Assert.Contains("Agents (1)", RenderToText(shell));
    }

    [Fact]
    public void TaskOverlayGesture_Should_DoNothing_When_TheTasksTabIsNotActive()
    {
        // arrange: a store is present (so it is not merely a null-store
        // no-op), but the active tab is not the one that owns it.
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

        // move focus off the query input (Tab), then switch to the other
        // tab and back.
        shell.Handle(new TuiEvent.KeyEvent(KeyInfo('\t', ConsoleKey.Tab)));
        shell.Handle(new TuiEvent.KeyEvent(KeyInfo(']', ConsoleKey.Oem6)));
        Assert.Contains("other", RenderToText(shell));
        shell.Handle(new TuiEvent.KeyEvent(KeyInfo('[', ConsoleKey.Oem4)));

        // assert: the tasks tab is still on search, its nested state
        // (including the focus the earlier Tab left it on) untouched by
        // the trip through the other tab.
        Assert.Contains("Results", RenderToText(shell));
        Assert.Equal(SearchFocus.List, searchMode.Focus);

        // act: Escape walks focus List -> Input (mirroring h), then a
        // second Escape at Input leaves search mode, popping the tasks
        // tab's own stack back to its board root.
        shell.Handle(new TuiEvent.KeyEvent(KeyInfo('', ConsoleKey.Escape)));
        shell.Handle(new TuiEvent.KeyEvent(KeyInfo('', ConsoleKey.Escape)));

        // assert
        Assert.Contains("board", RenderToText(shell));
    }
}
