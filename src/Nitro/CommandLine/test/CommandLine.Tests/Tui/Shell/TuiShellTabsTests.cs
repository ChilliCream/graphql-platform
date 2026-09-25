using ChilliCream.Nitro.CommandLine.Services.Memory;
using ChilliCream.Nitro.CommandLine.Services.Tasks;
using ChilliCream.Nitro.CommandLine.Services.Workspace;
using ChilliCream.Nitro.CommandLine.Tests.Memory;
using ChilliCream.Nitro.CommandLine.Tests.Tui.Mail;
using ChilliCream.Nitro.CommandLine.Tui.Agents;
using ChilliCream.Nitro.CommandLine.Tui.Board;
using ChilliCream.Nitro.CommandLine.Tui.Input;
using ChilliCream.Nitro.CommandLine.Tui.Mail;
using ChilliCream.Nitro.CommandLine.Tui.Memory;
using ChilliCream.Nitro.CommandLine.Tui.Runtime;
using ChilliCream.Nitro.CommandLine.Tui.Search;
using ChilliCream.Nitro.CommandLine.Tui.Shell;
using Microsoft.Extensions.Time.Testing;
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
    private static readonly DateTimeOffset s_now = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

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

    private static TuiTab CreateMemoryTab(string title, ITuiMode mode, char mnemonic = 'e') =>
        new(title, mnemonic, mode, new KeyDispatcher(MemoryKeyMap.CreateDefault()));

    private static void LoginAgent(Agents.FakeAgentStore store)
        => store.LoginAsync(TestContext.Current.CancellationToken).GetAwaiter().GetResult();

    /// <summary>
    /// A real <see cref="MemoryStore"/> over a freshly initialized workspace in its own
    /// temporary directory, returned in <paramref name="root"/> for the caller to delete.
    /// </summary>
    private static MemoryStore CreateMemoryStore(TimeProvider time, out DirectoryInfo root)
    {
        root = Directory.CreateTempSubdirectory("nitro-shell-memory-tests");
        var workspaceDirectory = AgentWorkspace.GetDirectory(root.FullName);
        Directory.CreateDirectory(workspaceDirectory);

        using var connection = new AgentDatabase()
            .InitializeAsync(workspaceDirectory, CancellationToken.None)
            .GetAwaiter()
            .GetResult();

        return new MemoryStore(new TestFileSystem(root.FullName), time, new AgentDatabase());
    }

    private static void SaveMemory(MemoryStore store, string text) => store.SaveAsync(
        new MemoryRecordCreation { Text = text, Type = "fact", Actor = "test-agent" },
        TestContext.Current.CancellationToken).GetAwaiter().GetResult();

    private static void LogMemory(MemoryStore store, string text) => store.LogAsync(
        new MemoryJournalEntryCreation { Text = text, Actor = "test-agent" },
        TestContext.Current.CancellationToken).GetAwaiter().GetResult();

    [Fact]
    public void Constructor_Should_CallOnEnter_When_EveryHostedTabIsConstructed()
    {
        // arrange
        var tab1Mode = new FakeTuiMode();
        var tab2Mode = new FakeTuiMode();
        var time = new FakeTimeProvider(s_now);

        // act
        _ = new TuiShell(
            [CreateTasksTab("Tasks", tab1Mode), CreateMailTab("Mail", tab2Mode)],
            80,
            24,
            agentStore: new Agents.FakeAgentStore(time));

        // assert
        Assert.True(tab1Mode.EnterCalled);
        Assert.True(tab2Mode.EnterCalled);
    }

    [Fact]
    public void Render_Should_ShowATabStrip_WithEveryTabsTitle_When_MoreThanOneTabIsHosted()
    {
        // arrange
        var time = new FakeTimeProvider(s_now);
        var shell = new TuiShell(
            [CreateTasksTab("Tasks", new FakeTuiMode()), CreateMailTab("Mail", new FakeTuiMode())],
            80,
            24,
            agentStore: new Agents.FakeAgentStore(time));

        // act
        var text = RenderToText(shell);

        // assert
        Assert.Contains("[T]asks", text);
        Assert.Contains("[M]ail", text);
    }

    [Fact]
    public void Render_Should_BracketEachTabsMnemonic_When_TabsAreActiveOrInactive()
    {
        // arrange
        var time = new FakeTimeProvider(s_now);
        var shell = new TuiShell(
            [
                CreateTasksTab("Tasks", new FakeTuiMode()),
                CreateMailTab("Mail", new FakeTuiMode()),
                CreateAgentsTab("Agents", new FakeTuiMode())
            ],
            80,
            24,
            agentStore: new Agents.FakeAgentStore(time));

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
        var time = new FakeTimeProvider(s_now);
        var shell = new TuiShell(
            [
                CreateTasksTab("Tasks", tasksMode),
                CreateMailTab("Mail", mailMode),
                CreateAgentsTab("Agents", agentsMode)
            ],
            80,
            24,
            agentStore: new Agents.FakeAgentStore(time));

        // act
        // Shift+A jumps straight from the (active) tasks tab to agents.
        var dirty = shell.Handle(new TuiEvent.KeyEvent(KeyInfo('A', ConsoleKey.A, ConsoleModifiers.Shift)));

        // assert
        Assert.True(dirty);
        Assert.Single(agentsMode.ResizeCalls);

        // act
        // Shift+M jumps from agents straight to mail, skipping tasks.
        shell.Handle(new TuiEvent.KeyEvent(KeyInfo('M', ConsoleKey.M, ConsoleModifiers.Shift)));

        // assert
        Assert.Single(mailMode.ResizeCalls);
        Assert.Empty(tasksMode.ResizeCalls);

        // act
        // Shift+T jumps back to tasks.
        shell.Handle(new TuiEvent.KeyEvent(KeyInfo('T', ConsoleKey.T, ConsoleModifiers.Shift)));

        // assert
        Assert.Single(tasksMode.ResizeCalls);
    }

    [Fact]
    public void Handle_Should_DoNothing_When_ShiftPlusLetterMatchesTheAlreadyActiveTab()
    {
        // arrange
        var tasksMode = new FakeTuiMode();
        var time = new FakeTimeProvider(s_now);
        var shell = new TuiShell(
            [CreateTasksTab("Tasks", tasksMode), CreateMailTab("Mail", new FakeTuiMode())],
            80,
            24,
            agentStore: new Agents.FakeAgentStore(time));

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
        var time = new FakeTimeProvider(s_now);
        var shell = new TuiShell(
            [
                CreateTasksTab("Tasks", tasksMode),
                CreateMailTab("Mail", new FakeTuiMode()),
                CreateAgentsTab("Agents", new FakeTuiMode())
            ],
            80,
            24,
            agentStore: new Agents.FakeAgentStore(time));

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
        var time = new FakeTimeProvider(s_now);
        var shell = new TuiShell(
            [CreateTasksTab("Tasks", mode), CreateMailTab("Mail", new FakeTuiMode())],
            80,
            24,
            agentStore: new Agents.FakeAgentStore(time));

        // act
        shell.Handle(new TuiEvent.ResizeEvent(100, 30));

        // assert
        Assert.Equal((100, 28), Assert.Single(mode.ResizeCalls));
    }

    [Fact]
    public void Handle_Should_RouteKeysOnlyToTheActiveTabsDispatcherAndMode_When_AKeyIsPressed()
    {
        // arrange
        var tasksMode = new FakeTuiMode();
        var mailMode = new FakeTuiMode();
        var time = new FakeTimeProvider(s_now);
        var shell = new TuiShell(
            [CreateTasksTab("Tasks", tasksMode), CreateMailTab("Mail", mailMode)],
            80,
            24,
            agentStore: new Agents.FakeAgentStore(time));

        // act
        shell.Handle(new TuiEvent.KeyEvent(KeyInfo('r', ConsoleKey.R)));

        // assert
        Assert.Contains(tasksMode.HandledMessages, m => m is TuiMessage.RefreshRequested);
        Assert.Empty(mailMode.HandledMessages);

        // act
        // Switch to the mail tab, where 'p' resolves through the Mail key map instead.
        shell.Handle(new TuiEvent.KeyEvent(KeyInfo(']', ConsoleKey.Oem6)));
        tasksMode.HandledMessages.Clear();
        shell.Handle(new TuiEvent.KeyEvent(KeyInfo('p', ConsoleKey.P)));

        // assert
        Assert.Contains(mailMode.HandledMessages, m => m is TuiMessage.AgentFilterPickerRequested);
        Assert.Empty(tasksMode.HandledMessages);
    }

    [Fact]
    public void Handle_Should_WrapAround_When_SwitchingPastTheLastOrFirstTab()
    {
        // arrange
        var tab1Mode = new FakeTuiMode();
        var tab2Mode = new FakeTuiMode();
        var time = new FakeTimeProvider(s_now);
        var shell = new TuiShell(
            [CreateTasksTab("Tasks", tab1Mode), CreateMailTab("Mail", tab2Mode)],
            80,
            24,
            agentStore: new Agents.FakeAgentStore(time));

        // act
        // '[' from the first tab wraps to the last.
        shell.Handle(new TuiEvent.KeyEvent(KeyInfo('[', ConsoleKey.Oem4)));

        // assert
        Assert.Single(tab2Mode.ResizeCalls);

        // act
        // ']' from the last tab wraps back to the first.
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
        var time = new FakeTimeProvider(s_now);
        var shell = new TuiShell(
            [CreateTasksTab("Tasks", tab1Mode), CreateMailTab("Mail", tab2Mode)],
            80,
            24,
            agentStore: new Agents.FakeAgentStore(time));

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
        var time = new FakeTimeProvider(s_now);

        // act
        // assert
        Assert.Throws<ArgumentOutOfRangeException>(() => new TuiShell(
            tabs,
            80,
            24,
            agentStore: new Agents.FakeAgentStore(time),
            tasksTabIndex: 2));
    }

    [Fact]
    public void HandleDataChanged_Should_NotRouteAnInactiveTabsFollowUpToTheActiveTab_When_DataChanges()
    {
        // arrange
        // The inactive mail mode returns a toast when refreshed.
        var mailFollowUp = new TuiMessage.ShowToast("mail refreshed", ToastStyle.Info);
        var tasksMode = new FakeTuiMode();
        var mailMode = new FakeTuiMode
        {
            HandleResult = message => message is TuiMessage.RefreshRequested ? [mailFollowUp] : []
        };
        var time = new FakeTimeProvider(s_now);
        var shell = new TuiShell(
            [CreateTasksTab("Tasks", tasksMode), CreateMailTab("Mail", mailMode)],
            80,
            24,
            agentStore: new Agents.FakeAgentStore(time));
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
        var time = new FakeTimeProvider(s_now);
        var shell = new TuiShell(
            [CreateTasksTab("Tasks", board), CreateTasksTab("Other", otherMode, mnemonic: 'O')],
            80,
            24,
            agentStore: new Agents.FakeAgentStore(time),
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
    public void Handle_Should_SwitchTab_When_TheOverlayThatBlockedItCloses()
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
        var time = new FakeTimeProvider(s_now);
        var shell = new TuiShell(
            [CreateTasksTab("Tasks", board), CreateTasksTab("Other", otherMode, mnemonic: 'O')],
            80,
            24,
            agentStore: new Agents.FakeAgentStore(time),
            tasksTabIndex: 0,
            store: store,
            actor: "tester");
        shell.Handle(new TuiEvent.KeyEvent(KeyInfo('e', ConsoleKey.E)));

        // act
        // Escape closes the (non-dirty) editor, then ']' switches tabs.
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
        var time = new FakeTimeProvider(s_now);
        var shell = new TuiShell(
            [CreateTasksTab("Tasks", tab1Mode), CreateMailTab("Mail", tab2Mode)],
            80,
            24,
            agentStore: new Agents.FakeAgentStore(time));

        // act
        var dirty = shell.Handle(new TuiEvent.DataChangedEvent());

        // assert
        Assert.True(dirty);
        Assert.Contains(tab1Mode.HandledMessages, m => m is TuiMessage.RefreshRequested);
        Assert.Contains(tab2Mode.HandledMessages, m => m is TuiMessage.RefreshRequested);
    }

    [Fact]
    public void Render_Should_ShowTheMailTabsThreadCount_When_MailTabIsSelected()
    {
        // arrange
        var mailStore = new FakeMailStore();
        mailStore.Messages.Add(MailMessageBuilder.Create("m1"));
        var mailMode = new MailMode(mailStore, new Agents.FakeAgentStore(new FakeTimeProvider(s_now)));
        var mailTab = new TuiTab("Mail", mnemonic: 'M', mailMode, new KeyDispatcher(MailKeyMap.CreateDefault()));
        var time = new FakeTimeProvider(s_now);
        var shell = new TuiShell(
            [CreateTasksTab("Tasks", new FakeTuiMode()), mailTab],
            80,
            24,
            agentStore: new Agents.FakeAgentStore(time));

        // act
        shell.Handle(new TuiEvent.KeyEvent(KeyInfo(']', ConsoleKey.Oem6)));

        // assert
        Assert.Contains("Mail (1)", RenderToText(shell));
    }

    [Fact]
    public void Render_Should_UpdateTheMailTabsThreadCount_When_DataChangedEventArrives()
    {
        // arrange
        var mailStore = new FakeMailStore();
        mailStore.Messages.Add(MailMessageBuilder.Create("m1"));
        var mailMode = new MailMode(mailStore, new Agents.FakeAgentStore(new FakeTimeProvider(s_now)));
        var mailTab = new TuiTab("Mail", mnemonic: 'M', mailMode, new KeyDispatcher(MailKeyMap.CreateDefault()));
        var time = new FakeTimeProvider(s_now);
        var shell = new TuiShell(
            [CreateTasksTab("Tasks", new FakeTuiMode()), mailTab],
            80,
            24,
            agentStore: new Agents.FakeAgentStore(time));
        shell.Handle(new TuiEvent.KeyEvent(KeyInfo(']', ConsoleKey.Oem6)));

        // act
        mailStore.Messages.Add(MailMessageBuilder.Create("m2", threadId: "m2"));
        shell.Handle(new TuiEvent.DataChangedEvent());

        // assert
        Assert.Contains("Mail (2)", RenderToText(shell));
    }

    [Fact]
    public void Handle_Should_NarrowMailTabToPickedAgent_When_AgentFilterPickerAppliedThroughTheShell()
    {
        // arrange
        var mailStore = new FakeMailStore();
        var time = new FakeTimeProvider(s_now);
        var agentStore = new Agents.FakeAgentStore(time);
        LoginAgent(agentStore);
        var agentName = agentStore.Rows[0].Name;
        mailStore.Messages.Add(MailMessageBuilder.Create("m1", sender: agentName, threadId: "t-1"));
        mailStore.Messages.Add(MailMessageBuilder.Create("m2", sender: "outsider", threadId: "t-2"));
        var mailMode = new MailMode(mailStore, agentStore, time);
        var mailTab = CreateMailTab("Mail", mailMode);
        var shell = new TuiShell(
            [CreateTasksTab("Tasks", new FakeTuiMode()), mailTab],
            80,
            24,
            agentStore: agentStore);

        // act
        // Switch to the Mail tab, open the agent picker, move to the seeded agent, and apply it.
        shell.Handle(new TuiEvent.KeyEvent(KeyInfo(']', ConsoleKey.Oem6)));
        shell.Handle(new TuiEvent.KeyEvent(KeyInfo('p', ConsoleKey.P)));
        shell.Handle(new TuiEvent.KeyEvent(KeyInfo('\0', ConsoleKey.DownArrow)));
        shell.Handle(new TuiEvent.KeyEvent(KeyInfo('\r', ConsoleKey.Enter)));

        // assert
        Assert.Contains($"Mail: {agentName} (1)", RenderToText(shell));
    }

    [Fact]
    public void Handle_Should_NarrowMailTabRows_When_SearchSubmittedThroughTheShell()
    {
        // arrange
        var mailStore = new FakeMailStore();
        mailStore.Messages.Add(MailMessageBuilder.Create(
            "m1", sender: "alice", subject: "Status update", threadId: "t-1"));
        mailStore.Messages.Add(MailMessageBuilder.Create(
            "m2", sender: "bob", subject: "Lunch plans", threadId: "t-2"));
        var time = new FakeTimeProvider(s_now);
        var mailMode = new MailMode(mailStore, new Agents.FakeAgentStore(time), time);
        var mailTab = CreateMailTab("Mail", mailMode);
        var shell = new TuiShell(
            [CreateTasksTab("Tasks", new FakeTuiMode()), mailTab],
            80,
            24,
            agentStore: new Agents.FakeAgentStore(time));

        // act
        // Switch to the Mail tab, open search, type a subject fragment, and submit with the save chord.
        shell.Handle(new TuiEvent.KeyEvent(KeyInfo(']', ConsoleKey.Oem6)));
        shell.Handle(new TuiEvent.KeyEvent(KeyInfo('/', ConsoleKey.Oem2)));

        foreach (var c in "status")
        {
            shell.Handle(new TuiEvent.KeyEvent(KeyInfo(c, ConsoleKey.NoName)));
        }

        shell.Handle(new TuiEvent.KeyEvent(KeyInfo('s', ConsoleKey.S, ConsoleModifiers.Control)));

        // assert
        Assert.Collection(mailMode.State.Threads, t => Assert.Equal("Status update", t.Subject));
        Assert.Contains("Status update", RenderToText(shell));
    }

    [Fact]
    public void Handle_Should_OpenTheMailThreadPopover_When_EnterIsPressedWithAThreadSelected()
    {
        // arrange
        var mailStore = new FakeMailStore();
        mailStore.Messages.Add(MailMessageBuilder.Create(
            "m1", sender: "alice", subject: "Status update", threadId: "t-1",
            recipients: [MailMessageBuilder.ToRecipient("bob")]));
        var time = new FakeTimeProvider(s_now);
        var mailMode = new MailMode(mailStore, new Agents.FakeAgentStore(time), time);
        var mailTab = CreateMailTab("Mail", mailMode);
        var shell = new TuiShell(
            [CreateTasksTab("Tasks", new FakeTuiMode()), mailTab],
            100,
            24,
            agentStore: new Agents.FakeAgentStore(time));
        shell.Handle(new TuiEvent.KeyEvent(KeyInfo(']', ConsoleKey.Oem6)));

        // act
        var dirty = shell.Handle(new TuiEvent.KeyEvent(KeyInfo('\r', ConsoleKey.Enter)));
        var rendered = RenderToText(shell, width: 100);

        // assert
        Assert.True(dirty);
        Assert.Contains("Status update", rendered);
        Assert.Contains("Participants:", rendered);
    }

    [Fact]
    public void Handle_Should_ShowTheThreadIdToast_When_YIsPressedInTheMailThreadPopover()
    {
        // arrange
        var mailStore = new FakeMailStore();
        mailStore.Messages.Add(MailMessageBuilder.Create("m1", threadId: "t-1"));
        var time = new FakeTimeProvider(s_now);
        var mailMode = new MailMode(mailStore, new Agents.FakeAgentStore(time), time);
        var mailTab = CreateMailTab("Mail", mailMode);
        var shell = new TuiShell(
            [CreateTasksTab("Tasks", new FakeTuiMode()), mailTab],
            100,
            24,
            agentStore: new Agents.FakeAgentStore(time));
        shell.Handle(new TuiEvent.KeyEvent(KeyInfo(']', ConsoleKey.Oem6)));
        shell.Handle(new TuiEvent.KeyEvent(KeyInfo('\r', ConsoleKey.Enter)));

        // act
        shell.Handle(new TuiEvent.KeyEvent(KeyInfo('y', ConsoleKey.Y)));
        var rendered = RenderToText(shell, width: 100);

        // assert
        var statusLine = rendered.TrimEnd().Split('\n')[^1].Trim();
        Assert.Equal("i t-1", statusLine);
    }

    [Fact]
    public void Handle_Should_CloseTheMailThreadPopover_When_EscapeIsPressed()
    {
        // arrange
        var mailStore = new FakeMailStore();
        mailStore.Messages.Add(MailMessageBuilder.Create(
            "m1", subject: "Status update", threadId: "t-1"));
        var time = new FakeTimeProvider(s_now);
        var mailMode = new MailMode(mailStore, new Agents.FakeAgentStore(time), time);
        var mailTab = CreateMailTab("Mail", mailMode);
        var shell = new TuiShell(
            [CreateTasksTab("Tasks", new FakeTuiMode()), mailTab],
            100,
            24,
            agentStore: new Agents.FakeAgentStore(time));
        shell.Handle(new TuiEvent.KeyEvent(KeyInfo(']', ConsoleKey.Oem6)));
        shell.Handle(new TuiEvent.KeyEvent(KeyInfo('\r', ConsoleKey.Enter)));

        // act
        var dirty = shell.Handle(new TuiEvent.KeyEvent(KeyInfo('\u001b', ConsoleKey.Escape)));
        var rendered = RenderToText(shell, width: 100);

        // assert
        Assert.True(dirty);
        Assert.Contains("enter open", rendered);
        Assert.Contains("SUBJECT", rendered);
        Assert.Contains("Status update", rendered);
    }

    [Fact]
    public void Handle_Should_ReloadTheMailThreadPopover_When_ADataChangedEventArrives()
    {
        // arrange
        var mailStore = new FakeMailStore();
        mailStore.Messages.Add(MailMessageBuilder.Create(
            "m1", body: "Original body", threadId: "t-1", createdAt: s_now));
        var time = new FakeTimeProvider(s_now);
        var mailMode = new MailMode(mailStore, new Agents.FakeAgentStore(time), time);
        var mailTab = CreateMailTab("Mail", mailMode);
        var shell = new TuiShell(
            [CreateTasksTab("Tasks", new FakeTuiMode()), mailTab],
            100,
            24,
            agentStore: new Agents.FakeAgentStore(time));
        shell.Handle(new TuiEvent.KeyEvent(KeyInfo(']', ConsoleKey.Oem6)));
        shell.Handle(new TuiEvent.KeyEvent(KeyInfo('\r', ConsoleKey.Enter)));

        // act
        mailStore.Messages.Add(MailMessageBuilder.Create(
            "m2", body: "Distinctnewbody", threadId: "t-1", createdAt: s_now.AddMinutes(1)));
        var dirty = shell.Handle(new TuiEvent.DataChangedEvent());
        var rendered = RenderToText(shell, width: 100);

        // assert
        Assert.True(dirty);
        Assert.Contains("Distinctnewbody", rendered);
    }

    [Fact]
    public void Handle_Should_ShowNoThreadSelectedToast_When_EnterIsPressedOnAnEmptyMailTab()
    {
        // arrange
        var mailStore = new FakeMailStore();
        var time = new FakeTimeProvider(s_now);
        var mailMode = new MailMode(mailStore, new Agents.FakeAgentStore(time), time);
        var mailTab = CreateMailTab("Mail", mailMode);
        var shell = new TuiShell(
            [CreateTasksTab("Tasks", new FakeTuiMode()), mailTab],
            100,
            24,
            agentStore: new Agents.FakeAgentStore(time));
        shell.Handle(new TuiEvent.KeyEvent(KeyInfo(']', ConsoleKey.Oem6)));

        // act
        var dirty = shell.Handle(new TuiEvent.KeyEvent(KeyInfo('\r', ConsoleKey.Enter)));

        // assert
        Assert.True(dirty);
        Assert.Contains("No thread selected.", RenderToText(shell, width: 100));
    }

    [Fact]
    public void Handle_Should_ShowTheMemoryTabsHeaderRowAndCount_When_ShiftEIsPressed()
    {
        // arrange
        var time = new FakeTimeProvider(s_now);
        var store = CreateMemoryStore(time, out var root);

        try
        {
            SaveMemory(store, "Ops note.");
            var memoryMode = new MemoryMode(store, time);
            var memoryTab = CreateMemoryTab("Memory", memoryMode);
            var shell = new TuiShell(
                [CreateTasksTab("Tasks", new FakeTuiMode()), memoryTab],
                80,
                24,
                agentStore: new Agents.FakeAgentStore(time));

            // act
            shell.Handle(new TuiEvent.KeyEvent(KeyInfo('E', ConsoleKey.E, ConsoleModifiers.Shift)));

            // assert
            var rendered = RenderToText(shell);
            Assert.Contains("Memory (1)", rendered);
            Assert.Contains("KIND", rendered);
            Assert.Contains("BODY", rendered);
        }
        finally
        {
            root.Delete(recursive: true);
        }
    }

    [Fact]
    public void Handle_Should_NarrowMemoryTabToCurated_When_FIsPressedAfterSwitchingWithShiftE()
    {
        // arrange
        var time = new FakeTimeProvider(s_now);
        var store = CreateMemoryStore(time, out var root);

        try
        {
            SaveMemory(store, "Ops note.");
            LogMemory(store, "Follow up needed.");
            var memoryMode = new MemoryMode(store, time);
            var memoryTab = CreateMemoryTab("Memory", memoryMode);
            var shell = new TuiShell(
                [CreateTasksTab("Tasks", new FakeTuiMode()), memoryTab],
                80,
                24,
                agentStore: new Agents.FakeAgentStore(time));
            shell.Handle(new TuiEvent.KeyEvent(KeyInfo('E', ConsoleKey.E, ConsoleModifiers.Shift)));

            // act
            shell.Handle(new TuiEvent.KeyEvent(KeyInfo('f', ConsoleKey.F)));

            // assert
            Assert.Contains("Curated (1)", RenderToText(shell));
            Assert.Equal(MemoryCollectionFilter.Curated, memoryMode.State.Filter);
        }
        finally
        {
            root.Delete(recursive: true);
        }
    }

    [Fact]
    public void Handle_Should_NarrowMemoryTabRows_When_SearchSubmittedThroughTheShell()
    {
        // arrange
        var time = new FakeTimeProvider(s_now);
        var store = CreateMemoryStore(time, out var root);

        try
        {
            SaveMemory(store, "Deploy note.");
            SaveMemory(store, "Lunch plan.");
            var memoryMode = new MemoryMode(store, time);
            var memoryTab = CreateMemoryTab("Memory", memoryMode);
            var shell = new TuiShell(
                [CreateTasksTab("Tasks", new FakeTuiMode()), memoryTab],
                80,
                24,
                agentStore: new Agents.FakeAgentStore(time));

            // act
            // Switch to the Memory tab, open search, type a body fragment, and submit with the save chord.
            shell.Handle(new TuiEvent.KeyEvent(KeyInfo('E', ConsoleKey.E, ConsoleModifiers.Shift)));
            shell.Handle(new TuiEvent.KeyEvent(KeyInfo('/', ConsoleKey.Oem2)));

            foreach (var c in "deploy")
            {
                shell.Handle(new TuiEvent.KeyEvent(KeyInfo(c, ConsoleKey.NoName)));
            }

            shell.Handle(new TuiEvent.KeyEvent(KeyInfo('s', ConsoleKey.S, ConsoleModifiers.Control)));

            // assert
            Assert.Collection(memoryMode.State.Rows, r => Assert.Equal("Deploy note.", r.Body));
            Assert.Contains("Deploy note.", RenderToText(shell));
        }
        finally
        {
            root.Delete(recursive: true);
        }
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
        var time = new FakeTimeProvider(s_now);
        var shell = new TuiShell(
            [CreateTasksTab("Tasks", board), CreateTasksTab("Other", new FakeTuiMode(), mnemonic: 'O')],
            80,
            24,
            agentStore: new Agents.FakeAgentStore(time),
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
    public void Handle_Should_OpenTheAgentPopover_When_EnterIsPressed_ThroughATabbedShell()
    {
        // arrange
        var time = new FakeTimeProvider(s_now);
        var agentStore = new Agents.FakeAgentStore(time);
        LoginAgent(agentStore);
        var agentsMode = new AgentsMode(
            agentStore, new Agents.FakeMailStore(), new FakeTaskStore(), new Agents.FakeMemoryStore(), time);
        var shell = new TuiShell(
            [CreateAgentsTab("Agents", agentsMode)],
            100,
            24,
            agentStore: agentStore,
            tasksTabIndex: 0,
            store: new FakeTaskStore());
        var agentName = agentsMode.State.Rows[0].Name;

        // act
        var dirty = shell.Handle(new TuiEvent.KeyEvent(KeyInfo('\r', ConsoleKey.Enter)));

        // assert
        var rendered = RenderToText(shell, width: 100);
        Assert.True(dirty);
        Assert.Equal(0, agentsMode.State.SelectedRow);
        Assert.Contains(agentName, rendered);
        Assert.Contains("Harness:", rendered);
    }

    [Fact]
    public void Handle_Should_LeaveAgentsListSelectionUntouched_When_EscapePressed()
    {
        // arrange
        var time = new FakeTimeProvider(s_now);
        var agentStore = new Agents.FakeAgentStore(time);
        LoginAgent(agentStore);
        var agentsMode = new AgentsMode(
            agentStore, new Agents.FakeMailStore(), new FakeTaskStore(), new Agents.FakeMemoryStore(), time);
        var shell = new TuiShell(
            [CreateAgentsTab("Agents", agentsMode)],
            80,
            24,
            agentStore: agentStore,
            tasksTabIndex: 0);
        var agentName = Assert.Single(agentsMode.State.Rows).Name;

        // act
        var dirty = shell.Handle(new TuiEvent.KeyEvent(KeyInfo('\x1b', ConsoleKey.Escape)));

        // assert
        Assert.True(dirty);
        Assert.Equal(agentName, agentsMode.State.SelectedAgent?.Name);
        Assert.Contains("Agents (0 online / 1)", RenderToText(shell));
    }

    [Fact]
    public void TaskOverlayGesture_Should_DoNothing_When_TheTasksTabIsNotActive()
    {
        // arrange
        var store = new FakeTaskStore();
        store.Tasks["a"] = TaskItemBuilder.Create("a");
        var otherMode = new FakeTuiMode { SelectedTaskId = "a" };
        var time = new FakeTimeProvider(s_now);
        var shell = new TuiShell(
            [CreateTasksTab("Tasks", new FakeTuiMode()), CreateTasksTab("Other", otherMode, mnemonic: 'O')],
            80,
            24,
            agentStore: new Agents.FakeAgentStore(time),
            tasksTabIndex: 0,
            store: store,
            actor: "tester");
        shell.Handle(new TuiEvent.KeyEvent(KeyInfo(']', ConsoleKey.Oem6)));

        // act
        // 'e' would open the task editor on the tasks tab.
        shell.Handle(new TuiEvent.KeyEvent(KeyInfo('e', ConsoleKey.E)));

        // assert
        // No editor opened on the (non-tasks) active tab.
        Assert.DoesNotContain("Edit Task", RenderToText(shell));
    }

    [Fact]
    public void Handle_Should_PreserveEachTabsNavigationStack_When_SwitchingTabs()
    {
        // arrange
        var store = new FakeTaskStore();
        var searchMode = new SearchMode(store);
        var board = new FakeTuiMode { RenderText = "board" };
        var time = new FakeTimeProvider(s_now);
        var shell = new TuiShell(
            [CreateTasksTab("Tasks", board), CreateTasksTab("Other", new FakeTuiMode { RenderText = "other" }, mnemonic: 'O')],
            80,
            24,
            agentStore: new Agents.FakeAgentStore(time),
            tasksTabIndex: 0,
            searchMode: searchMode,
            store: store,
            actor: "tester");

        // act
        // Enter search from the tasks tab's board root.
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
