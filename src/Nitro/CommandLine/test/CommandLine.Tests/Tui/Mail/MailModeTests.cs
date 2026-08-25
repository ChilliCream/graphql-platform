using ChilliCream.Nitro.CommandLine.Services.Workspace;
using ChilliCream.Nitro.CommandLine.Tests.Tui.Agents;
using ChilliCream.Nitro.CommandLine.Tui.Input;
using ChilliCream.Nitro.CommandLine.Tui.Mail;
using ChilliCream.Nitro.CommandLine.Tui.Theming;
using Microsoft.Extensions.Time.Testing;
using Spectre.Console;
using Spectre.Console.Testing;
using CursorDirection = ChilliCream.Nitro.CommandLine.Tui.Input.CursorDirection;
using static ChilliCream.Nitro.CommandLine.Tests.Tui.AnsiAssertions;

namespace ChilliCream.Nitro.CommandLine.Tests.Tui.Mail;

public sealed class MailModeTests
{
    private static readonly DateTimeOffset Now = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    private static MailMode CreateMode(
        FakeMailStore store,
        string actor = "alice",
        FakeAgentRegistry? agentRegistry = null,
        FakeMailWakeReceiptObserver? wakeObserver = null)
        => new(
            store,
            actor,
            agentRegistry ?? new FakeAgentRegistry(),
            new DaemonOwnedActorWakeDispatcher(),
            wakeObserver ?? new FakeMailWakeReceiptObserver(),
            new FakeTimeProvider(Now));

    private static AgentRecord Agent(string name) => new()
    {
        Name = name,
        Role = "",
        Client = "",
        Implicit = false,
        RegisteredAt = Now,
        LastSeenAt = Now
    };

    private static void AddMessage(FakeMailStore store, string id, DateTimeOffset createdAt, string actor = "alice")
        => store.Messages.Add(MailMessageBuilder.Create(
            id, createdAt: createdAt, recipients: [MailMessageBuilder.ToRecipient(actor)]));

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

    [Fact]
    public void MoveSelection_Should_ClampAtLastRow_When_MovingDownPastEnd()
    {
        // arrange
        var store = new FakeMailStore();
        AddMessage(store, "m-1", Now);
        AddMessage(store, "m-2", Now.AddMinutes(1));
        var mode = CreateMode(store);
        mode.OnEnter();

        // act
        mode.Handle(new TuiMessage.MoveCursor(CursorDirection.Down));
        mode.Handle(new TuiMessage.MoveCursor(CursorDirection.Down));
        mode.Handle(new TuiMessage.MoveCursor(CursorDirection.Down));

        // assert
        Assert.Equal(1, mode.State.SelectedRow);
    }

    [Fact]
    public void MoveSelection_Should_ClampAtFirstRow_When_MovingUpPastStart()
    {
        // arrange
        var store = new FakeMailStore();
        AddMessage(store, "m-1", Now);
        var mode = CreateMode(store);
        mode.OnEnter();

        // act
        mode.Handle(new TuiMessage.MoveCursor(CursorDirection.Up));

        // assert
        Assert.Equal(0, mode.State.SelectedRow);
    }

    [Fact]
    public void MoveToEdge_Should_SelectLastRow_When_Bottom()
    {
        // arrange
        var store = new FakeMailStore();
        AddMessage(store, "m-1", Now);
        AddMessage(store, "m-2", Now.AddMinutes(1));
        AddMessage(store, "m-3", Now.AddMinutes(2));
        var mode = CreateMode(store);
        mode.OnEnter();

        // act
        mode.Handle(new TuiMessage.MoveToEdge(EdgeTarget.Bottom));

        // assert
        Assert.Equal(2, mode.State.SelectedRow);
    }

    [Fact]
    public void MoveToEdge_Should_SelectFirstRow_When_Top()
    {
        // arrange
        var store = new FakeMailStore();
        AddMessage(store, "m-1", Now);
        AddMessage(store, "m-2", Now.AddMinutes(1));
        var mode = CreateMode(store);
        mode.OnEnter();
        mode.Handle(new TuiMessage.MoveToEdge(EdgeTarget.Bottom));

        // act
        mode.Handle(new TuiMessage.MoveToEdge(EdgeTarget.Top));

        // assert
        Assert.Equal(0, mode.State.SelectedRow);
    }

    [Fact]
    public void MoveCursor_Should_TogglePaneFocus_When_Left()
    {
        // arrange
        var store = new FakeMailStore();
        var mode = CreateMode(store);
        mode.OnEnter();
        Assert.Equal(MailFocus.List, mode.State.Focus);

        // act
        mode.Handle(new TuiMessage.MoveCursor(CursorDirection.Left));

        // assert
        Assert.Equal(MailFocus.Detail, mode.State.Focus);
    }

    [Fact]
    public void MoveCursor_Should_TogglePaneFocus_When_Right()
    {
        // arrange
        var store = new FakeMailStore();
        var mode = CreateMode(store);
        mode.OnEnter();
        Assert.Equal(MailFocus.List, mode.State.Focus);

        // act
        mode.Handle(new TuiMessage.MoveCursor(CursorDirection.Right));

        // assert
        Assert.Equal(MailFocus.Detail, mode.State.Focus);
    }

    [Fact]
    public void OpenSelected_Should_FocusDetailPane()
    {
        // arrange
        var store = new FakeMailStore();
        AddMessage(store, "m-1", Now);
        var mode = CreateMode(store);
        mode.OnEnter();

        // act
        mode.Handle(new TuiMessage.OpenSelected());

        // assert
        Assert.Equal(MailFocus.Detail, mode.State.Focus);
    }

    [Fact]
    public void RefreshRequested_Should_ReloadMessages()
    {
        // arrange
        var store = new FakeMailStore();
        var mode = CreateMode(store);
        mode.OnEnter();
        Assert.Empty(mode.State.Messages);

        // act
        AddMessage(store, "m-1", Now);
        mode.Handle(new TuiMessage.RefreshRequested());

        // assert
        Assert.Equal(["m-1"], mode.State.Messages.Select(m => m.Id));
    }

    [Fact]
    public void CycleView_Should_AdvanceTheListFilter()
    {
        // arrange
        var store = new FakeMailStore();
        var mode = CreateMode(store);
        mode.OnEnter();

        // act
        mode.Handle(new TuiMessage.CycleView(1));

        // assert
        Assert.Equal(MailListFilter.Unread, mode.State.Filter);
    }

    [Fact]
    public void ToggleMaximize_Should_SwitchToThreadView_When_MessageSelected()
    {
        // arrange
        var store = new FakeMailStore();
        AddMessage(store, "m-1", Now);
        var mode = CreateMode(store);
        mode.OnEnter();
        // Threads mode (the default) already defaults a single-message
        // thread's row to Thread view; flip to Flat mode so this exercises
        // the message-selected-then-toggle-to-thread path.
        mode.State.ToggleListMode();

        // act
        var followUp = mode.Handle(new TuiMessage.ToggleMaximize());

        // assert
        Assert.Empty(followUp);
        Assert.Equal(MailViewMode.Thread, mode.State.ViewMode);
    }

    [Fact]
    public void ToggleMaximize_Should_ShowWarningToast_When_NoMessageSelected()
    {
        // arrange
        var store = new FakeMailStore();
        var mode = CreateMode(store);
        mode.OnEnter();

        // act
        var followUp = mode.Handle(new TuiMessage.ToggleMaximize());

        // assert
        var toast = Assert.Single(followUp);
        var shown = Assert.IsType<TuiMessage.ShowToast>(toast);
        Assert.Equal(ToastStyle.Warn, shown.Style);
    }

    [Fact]
    public void ToggleMaximize_Should_SwitchBackToMessageView_When_AlreadyShowingThread()
    {
        // arrange
        var store = new FakeMailStore();
        AddMessage(store, "m-1", Now);
        var mode = CreateMode(store);
        mode.OnEnter();
        // Threads mode (the default) already defaults a single-message
        // thread's row to Thread view; flip to Flat mode first so the
        // arrange toggle is the one that enters Thread view.
        mode.State.ToggleListMode();
        mode.Handle(new TuiMessage.ToggleMaximize());

        // act
        mode.Handle(new TuiMessage.ToggleMaximize());

        // assert
        Assert.Equal(MailViewMode.Message, mode.State.ViewMode);
    }

    [Fact]
    public void CopySelectedId_Should_ShowInfoToast_When_MessageSelected()
    {
        // arrange
        var store = new FakeMailStore();
        AddMessage(store, "m-1", Now);
        var mode = CreateMode(store);
        mode.OnEnter();

        // act
        var followUp = mode.Handle(new TuiMessage.CopySelectedId());

        // assert
        var toast = Assert.Single(followUp);
        var shown = Assert.IsType<TuiMessage.ShowToast>(toast);
        Assert.Equal("m-1", shown.Text);
        Assert.Equal(ToastStyle.Info, shown.Style);
    }

    [Fact]
    public void CopySelectedId_Should_ShowWarningToast_When_NoMessageSelected()
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
    public void Render_Should_IncludeListHeaderAndBadges()
    {
        // arrange
        var store = new FakeMailStore();
        AddMessage(store, "m-1", Now);
        var mode = CreateMode(store);
        mode.OnEnter();
        var console = new TestConsole().Width(100).Height(20);

        // act
        console.Write(mode.Render(100, 20));

        // assert: the default mailbox is Workspace, rendered as a threaded
        // table with a heading row above the sender/subject/etc. columns.
        Assert.Contains("Workspace (1)", console.Output);
        Assert.Contains("From", console.Output);
        Assert.Contains("Subject", console.Output);
        Assert.Contains("sender", console.Output);
    }

    [Fact]
    public void Render_Should_AttributeSenderClient_InTheDetailPane_When_SenderHasOne()
    {
        // arrange
        var store = new FakeMailStore();
        store.Messages.Add(MailMessageBuilder.Create(
            "m-1", sender: "bob", createdAt: Now, recipients: [MailMessageBuilder.ToRecipient("alice")]));
        var registry = new FakeAgentRegistry();
        registry.Agents.Add(Agent("bob") with { Client = "codex" });
        var mode = CreateMode(store, agentRegistry: registry);
        mode.OnEnter();
        mode.State.ShowMessage(); // Threads mode defaults a single-message thread's row to Thread view
        var console = new TestConsole().Width(100).Height(20);

        // act
        console.Write(mode.Render(100, 20));

        // assert: RefreshBlocking loaded the registry once and MailMode
        // threaded the lookup into the detail pane without a per-row query.
        Assert.Contains("From: bob (codex)", console.Output);
    }

    [Fact]
    public void OnEnter_Should_NotThrow_When_RegistryHasCaseVariantDuplicateNames()
    {
        // arrange - an externally written registry could hold "bob" and
        // "Bob" side by side; the case-insensitive lookup used elsewhere
        // means these collide as one key, so building it must not throw on
        // the duplicate the way ToDictionary would.
        var store = new FakeMailStore();
        store.Messages.Add(MailMessageBuilder.Create(
            "m-1", sender: "bob", createdAt: Now, recipients: [MailMessageBuilder.ToRecipient("alice")]));
        var registry = new FakeAgentRegistry();
        registry.Agents.Add(Agent("bob") with { Client = "codex" });
        registry.Agents.Add(Agent("Bob") with { Client = "claude-code" });
        var mode = CreateMode(store, agentRegistry: registry);

        // act
        var exception = Record.Exception(mode.OnEnter);

        // assert
        Assert.Null(exception);
    }

    [Fact]
    public void Render_Should_ShowNoAttribution_InTheDetailPane_When_SenderHasNoRegisteredClient()
    {
        // arrange
        var store = new FakeMailStore();
        store.Messages.Add(MailMessageBuilder.Create(
            "m-1", sender: "bob", createdAt: Now, recipients: [MailMessageBuilder.ToRecipient("alice")]));
        var mode = CreateMode(store, agentRegistry: new FakeAgentRegistry());
        mode.OnEnter();
        mode.State.ShowMessage(); // Threads mode defaults a single-message thread's row to Thread view
        var console = new TestConsole().Width(100).Height(20);

        // act
        console.Write(mode.Render(100, 20));

        // assert: bob is not even in the registry, so no attribution shows.
        Assert.Contains("From: bob", console.Output);
        Assert.DoesNotContain("From: bob (", console.Output);
    }

    [Fact]
    public void Render_Should_NotThrow_When_WidthOrHeightIsZero()
    {
        // arrange
        var store = new FakeMailStore();
        var mode = CreateMode(store);
        mode.OnEnter();

        // act
        var exception = Record.Exception(() => mode.Render(0, 0));

        // assert
        Assert.Null(exception);
    }

    [Fact]
    public void OnEnter_Should_DefaultToWorkspaceMailbox()
    {
        // arrange: Workspace shows every agent's mail, unlike Inbox which
        // would show only alice's; this is the epic's user ruling that
        // Workspace, not Inbox, is the mail board's default mailbox.
        var store = new FakeMailStore();
        AddMessage(store, "m-1", Now);
        AddMessage(store, "m-2", Now, actor: "bob");
        var mode = CreateMode(store, actor: "alice");

        // act
        mode.OnEnter();

        // assert
        Assert.Equal(MailMailbox.Workspace, mode.State.Mailbox);
        Assert.Equal(["m-2", "m-1"], mode.State.Messages.Select(m => m.Id));
    }

    [Fact]
    public void OnEnter_Should_LoadTheActorsInbox_When_InboxIsSelected()
    {
        // arrange
        var store = new FakeMailStore();
        AddMessage(store, "m-1", Now);
        AddMessage(store, "m-2", Now, actor: "bob");
        var mode = CreateMode(store, actor: "alice");
        mode.OnEnter();

        // act
        mode.Handle(new TuiMessage.SelectInboxRequested());

        // assert
        Assert.Equal(["m-1"], mode.State.Messages.Select(m => m.Id));
    }

    [Fact]
    public void OpenSelected_Should_MarkMessageRead_When_MessageIsUnread()
    {
        // arrange
        var store = new FakeMailStore();
        AddMessage(store, "m-1", Now);
        var mode = CreateMode(store);
        mode.OnEnter();
        mode.Handle(new TuiMessage.SelectInboxRequested()); // Workspace (the default) is read-only

        // act
        mode.Handle(new TuiMessage.OpenSelected());

        // assert
        var recipient = MailRecipientView.FindRecipient(store.Messages[0], "alice");
        Assert.NotNull(recipient!.ReadAt);
    }

    [Fact]
    public void ToggleReadRequested_Should_MarkRead_And_ShowSuccessToast_When_MessageIsUnread()
    {
        // arrange
        var store = new FakeMailStore();
        AddMessage(store, "m-1", Now);
        var mode = CreateMode(store);
        mode.OnEnter();
        mode.Handle(new TuiMessage.SelectInboxRequested()); // Workspace (the default) is read-only

        // act
        var followUp = mode.Handle(new TuiMessage.ToggleReadRequested());

        // assert
        var toast = Assert.Single(followUp);
        Assert.Equal(ToastStyle.Success, Assert.IsType<TuiMessage.ShowToast>(toast).Style);
        var recipient = MailRecipientView.FindRecipient(store.Messages[0], "alice");
        Assert.NotNull(recipient!.ReadAt);
    }

    [Fact]
    public void ToggleReadRequested_Should_MarkUnread_When_MessageIsRead()
    {
        // arrange
        var store = new FakeMailStore();
        AddMessage(store, "m-1", Now);
        var mode = CreateMode(store);
        mode.OnEnter();
        mode.Handle(new TuiMessage.SelectInboxRequested()); // Workspace (the default) is read-only
        mode.Handle(new TuiMessage.ToggleReadRequested());

        // act
        mode.Handle(new TuiMessage.ToggleReadRequested());

        // assert
        var recipient = MailRecipientView.FindRecipient(store.Messages[0], "alice");
        Assert.Null(recipient!.ReadAt);
    }

    [Fact]
    public void ToggleReadRequested_Should_ShowWarnToast_When_NoMessageSelected()
    {
        // arrange
        var store = new FakeMailStore();
        var mode = CreateMode(store);
        mode.OnEnter();

        // act
        var followUp = mode.Handle(new TuiMessage.ToggleReadRequested());

        // assert
        var toast = Assert.Single(followUp);
        Assert.Equal(ToastStyle.Warn, Assert.IsType<TuiMessage.ShowToast>(toast).Style);
    }

    [Fact]
    public void ArchiveRequested_Should_OpenConfirmation_Without_ArchivingYet()
    {
        // arrange
        var store = new FakeMailStore();
        AddMessage(store, "m-1", Now);
        var mode = CreateMode(store);
        mode.OnEnter();
        mode.Handle(new TuiMessage.SelectInboxRequested()); // Workspace (the default) is read-only

        // act
        var followUp = mode.Handle(new TuiMessage.ArchiveRequested());

        // assert
        Assert.Empty(followUp);
        Assert.True(mode.IsInputCapturing);
        Assert.Null(MailRecipientView.FindRecipient(store.Messages[0], "alice")!.ArchivedAt);
    }

    [Fact]
    public void ArchiveRequested_Should_ShowWarnToast_When_NoMessageSelected()
    {
        // arrange
        var store = new FakeMailStore();
        var mode = CreateMode(store);
        mode.OnEnter();

        // act
        var followUp = mode.Handle(new TuiMessage.ArchiveRequested());

        // assert
        var toast = Assert.Single(followUp);
        Assert.Equal(ToastStyle.Warn, Assert.IsType<TuiMessage.ShowToast>(toast).Style);
    }

    [Fact]
    public void ArchiveConfirmation_Confirmed_Should_ArchiveMessage_And_RemoveItFromTheDefaultInboxList()
    {
        // arrange
        var store = new FakeMailStore();
        AddMessage(store, "m-1", Now);
        var mode = CreateMode(store);
        mode.OnEnter();
        mode.Handle(new TuiMessage.SelectInboxRequested()); // Workspace (the default) is read-only
        mode.Handle(new TuiMessage.ArchiveRequested());

        // act: Enter confirms from the dialog's initially focused (empty) reason field.
        var followUp = mode.HandleRawKey(Key(ConsoleKey.Enter));

        // assert
        var toast = Assert.Single(followUp);
        Assert.Equal(ToastStyle.Success, Assert.IsType<TuiMessage.ShowToast>(toast).Style);
        Assert.False(mode.IsInputCapturing);
        Assert.Empty(mode.State.Messages);
    }

    [Fact]
    public void ArchiveConfirmation_Cancelled_Should_LeaveTheMessageAndListUntouched()
    {
        // arrange
        var store = new FakeMailStore();
        AddMessage(store, "m-1", Now);
        var mode = CreateMode(store);
        mode.OnEnter();
        mode.Handle(new TuiMessage.SelectInboxRequested()); // Workspace (the default) is read-only
        mode.Handle(new TuiMessage.ArchiveRequested());

        // act
        var followUp = mode.HandleRawKey(Key(ConsoleKey.Escape));

        // assert
        Assert.Empty(followUp);
        Assert.False(mode.IsInputCapturing);
        Assert.Null(MailRecipientView.FindRecipient(store.Messages[0], "alice")!.ArchivedAt);
        Assert.Equal(["m-1"], mode.State.Messages.Select(m => m.Id));
    }

    [Fact]
    public void ComposeRequested_Should_OpenComposeForm()
    {
        // arrange
        var store = new FakeMailStore();
        var mode = CreateMode(store);
        mode.OnEnter();
        mode.Handle(new TuiMessage.SelectInboxRequested()); // Workspace (the default) is read-only

        // act
        var followUp = mode.Handle(new TuiMessage.ComposeRequested());

        // assert
        Assert.Empty(followUp);
        Assert.True(mode.IsInputCapturing);
    }

    [Fact]
    public void ComposeForm_Submit_Should_ShowSendingToast_Immediately_And_CloseTheForm()
    {
        // arrange: the store-plus-wake workflow runs off the input thread
        // (see MailMode.SubmitCompose), so the synchronous return from the
        // submit key is only ever the immediate "Sending" toast, never the
        // eventual outcome.
        var store = new FakeMailStore();
        var mode = CreateMode(store);
        mode.OnEnter();
        mode.Handle(new TuiMessage.SelectInboxRequested()); // Workspace (the default) is read-only
        mode.Handle(new TuiMessage.ComposeRequested());
        Type(mode, "bob");
        mode.HandleRawKey(Key(ConsoleKey.Tab));
        Type(mode, "Status");
        mode.HandleRawKey(Key(ConsoleKey.Tab));
        Type(mode, "All good.");

        // act
        var followUp = mode.HandleRawKey(CtrlKey(ConsoleKey.S));

        // assert
        var toast = Assert.Single(followUp);
        var shown = Assert.IsType<TuiMessage.ShowToast>(toast);
        Assert.Equal(ToastStyle.Info, shown.Style);
        Assert.False(mode.IsInputCapturing);
    }

    [Theory]
    [InlineData("delivered", "Success")]
    [InlineData("satisfied", "Success")]
    [InlineData("delegated", "Success")]
    [InlineData("pending", "Warn")]
    [InlineData("failed", "Error")]
    public async Task ComposeForm_Submit_Should_StoreTheMessage_And_ReportTheObservedWakeStatusTruthfully(
        string wakeStatus, string expectedStyleName)
    {
        // arrange: a store write alone is never reported delivered; only an
        // IsZero wake status (delivered/satisfied/delegated) shows green,
        // matching the truthful-receipt design (perles-net-4mn comment 156).
        // InlineData cannot carry the internal ToastStyle enum directly (a
        // public test method's parameter types must be at least as
        // accessible as the method), so the expected style is named and
        // parsed back into the real enum below instead.
        var expectedStyle = Enum.Parse<ToastStyle>(expectedStyleName);
        var cancellationToken = TestContext.Current.CancellationToken;
        var store = new FakeMailStore();
        var wakeObserver = new FakeMailWakeReceiptObserver();
        wakeObserver.StatusByActor["bob"] = FakeMailWakeReceiptObserver.Observation("bob", wakeStatus);
        var mode = CreateMode(store, wakeObserver: wakeObserver);
        mode.OnEnter();
        mode.Handle(new TuiMessage.SelectInboxRequested());
        mode.Handle(new TuiMessage.ComposeRequested());
        Type(mode, "bob");
        mode.HandleRawKey(Key(ConsoleKey.Tab));
        Type(mode, "Status");
        mode.HandleRawKey(Key(ConsoleKey.Tab));
        Type(mode, "All good.");
        mode.HandleRawKey(CtrlKey(ConsoleKey.S));

        // act
        var toast = await WaitForOutcomeToastAsync(mode, cancellationToken);

        // assert
        Assert.Equal(expectedStyle, toast.Style);
        var sent = Assert.Single(store.Messages);
        Assert.Equal("alice", sent.Sender);
        Assert.Equal("Status", sent.Subject);
        Assert.Single(sent.WakeReceipts); // exactly one transactional wake generation, for bob
    }

    [Fact]
    public async Task ComposeForm_Submit_Should_ShowErrorToast_And_WriteNothing_When_StoreRejectsTheWrite()
    {
        // arrange: FakeMailStore.SendMessageAsync throws when there are no
        // recipients; the form's own validator normally prevents an empty
        // To field, so this exercises the write-failure toast path the same
        // way a store-level rejection (for example an unknown recipient
        // against the real store) would surface. A rejected write creates no
        // wake generation.
        var cancellationToken = TestContext.Current.CancellationToken;
        var store = new FakeMailStore();
        var mode = CreateMode(store);
        mode.OnEnter();
        mode.Handle(new TuiMessage.SelectInboxRequested()); // Workspace (the default) is read-only
        mode.Handle(new TuiMessage.ComposeRequested());
        Type(mode, ",");
        mode.HandleRawKey(Key(ConsoleKey.Tab));
        Type(mode, "Status");
        mode.HandleRawKey(Key(ConsoleKey.Tab));
        Type(mode, "Body");
        mode.HandleRawKey(CtrlKey(ConsoleKey.S));

        // act
        var toast = await WaitForOutcomeToastAsync(mode, cancellationToken);

        // assert
        Assert.Equal(ToastStyle.Error, toast.Style);
        Assert.Empty(store.Messages);
    }

    [Fact]
    public async Task ComposeForm_Submit_Should_RefuseADuplicateSubmit_While_TheFirstIsStillInFlight()
    {
        // arrange: the second compose is submitted while the first send's
        // wake observation is still gated open, so exactly one transactional
        // send may be in flight at a time (TuiEffectQueue's dedupe key).
        var cancellationToken = TestContext.Current.CancellationToken;
        var store = new FakeMailStore();
        var wakeObserver = new FakeMailWakeReceiptObserver { Gate = new TaskCompletionSource() };
        var mode = CreateMode(store, wakeObserver: wakeObserver);
        mode.OnEnter();
        mode.Handle(new TuiMessage.SelectInboxRequested());
        mode.Handle(new TuiMessage.ComposeRequested());
        Type(mode, "bob");
        mode.HandleRawKey(Key(ConsoleKey.Tab));
        Type(mode, "First");
        mode.HandleRawKey(Key(ConsoleKey.Tab));
        Type(mode, "Body");
        mode.HandleRawKey(CtrlKey(ConsoleKey.S));
        await WaitUntilAsync(() => wakeObserver.ObserveCallCount > 0, cancellationToken);

        mode.Handle(new TuiMessage.ComposeRequested());
        Type(mode, "carol");
        mode.HandleRawKey(Key(ConsoleKey.Tab));
        Type(mode, "Second");
        mode.HandleRawKey(Key(ConsoleKey.Tab));
        Type(mode, "Body");

        // act
        var followUp = mode.HandleRawKey(CtrlKey(ConsoleKey.S));

        // assert: refused with a warning, the second form stays open with
        // its values intact, and only the first message was ever stored.
        var toast = Assert.Single(followUp);
        Assert.Equal(ToastStyle.Warn, Assert.IsType<TuiMessage.ShowToast>(toast).Style);
        Assert.True(mode.IsInputCapturing);
        Assert.Single(store.Messages);

        // cleanup: release the gated observation so the first send resolves.
        wakeObserver.Gate!.SetResult();
        await WaitForOutcomeToastAsync(mode, cancellationToken);
    }

    [Fact]
    public async Task CreateQuitGate_Should_ReportPendingCount_While_ASendIsStillInFlight_And_ClearAfterwards()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var store = new FakeMailStore();
        var wakeObserver = new FakeMailWakeReceiptObserver { Gate = new TaskCompletionSource() };
        var mode = CreateMode(store, wakeObserver: wakeObserver);
        mode.OnEnter();
        mode.Handle(new TuiMessage.SelectInboxRequested());
        mode.Handle(new TuiMessage.ComposeRequested());
        Type(mode, "bob");
        mode.HandleRawKey(Key(ConsoleKey.Tab));
        Type(mode, "Status");
        mode.HandleRawKey(Key(ConsoleKey.Tab));
        Type(mode, "Body");
        mode.HandleRawKey(CtrlKey(ConsoleKey.S));
        await WaitUntilAsync(() => wakeObserver.ObserveCallCount > 0, cancellationToken);

        var gate = mode.CreateQuitGate();

        // act: bounded drain while the send is still gated open.
        var reportWhilePending = await gate(TimeSpan.FromMilliseconds(50), cancellationToken);

        // assert
        Assert.Equal(1, reportWhilePending.PendingCount);
        Assert.Equal(0, reportWhilePending.OutcomeUnknownCount);
        Assert.True(reportWhilePending.HasUnresolvedWork);

        // cleanup: release the gate, resume accepting (mirroring
        // TuiShell.QuitCancelled), and confirm the effect drains cleanly.
        wakeObserver.Gate!.SetResult();
        await WaitForOutcomeToastAsync(mode, cancellationToken);
        mode.ResumeSendAcceptance();
    }

    [Fact]
    public async Task ReplyForm_Submit_Should_ReconcileToAnUnknownNotification_When_TheWakeStepIsCancelledAfterCommit()
    {
        // arrange: the token cancels while the wake observation is gated
        // open, i.e. strictly after the store write already committed. The
        // message must still exist (only an observed rollback ever means
        // unsent), with its notification outcome reported unresolved rather
        // than a hard failure.
        var cancellationToken = TestContext.Current.CancellationToken;
        var store = new FakeMailStore();
        store.Messages.Add(MailMessageBuilder.Create(
            "m-1", sender: "bob", createdAt: Now, recipients: [MailMessageBuilder.ToRecipient("alice")]));
        var wakeObserver = new FakeMailWakeReceiptObserver { Gate = new TaskCompletionSource() };
        using var effectCts = new CancellationTokenSource();
        var mode = new MailMode(
            store,
            "alice",
            new FakeAgentRegistry(),
            new DaemonOwnedActorWakeDispatcher(),
            wakeObserver,
            new FakeTimeProvider(Now),
            effectCts.Token);
        mode.OnEnter();
        mode.Handle(new TuiMessage.SelectInboxRequested());
        mode.State.SelectedRow = 0;
        mode.Handle(new TuiMessage.ReplyRequested());
        Type(mode, "On it.");
        mode.HandleRawKey(CtrlKey(ConsoleKey.S));
        await WaitUntilAsync(() => wakeObserver.ObserveCallCount > 0, cancellationToken);

        // act: cancelled while the observation is gated open, strictly after
        // the reply already committed; the gate itself is never released,
        // so only the cancellation can resolve the wait.
        await effectCts.CancelAsync();
        var toast = await WaitForOutcomeToastAsync(mode, cancellationToken);

        // assert
        Assert.Equal(ToastStyle.Warn, toast.Style);
        Assert.Contains("outcome unknown", toast.Text, StringComparison.Ordinal);
        Assert.Contains(store.Messages, m => m.Sender == "alice" && m.InReplyTo == "m-1" && m.Body == "On it.");
    }

    /// <summary>
    /// Polls <see cref="MailMode.Handle"/> with a <see cref="TuiMessage.RefreshRequested"/>
    /// (the same message the workspace database watcher's <c>DataChangedEvent</c>
    /// drives in the real TUI loop) until a compose/reply outcome toast
    /// drains, mirroring how <see cref="MailMode"/>'s own send effect
    /// surfaces asynchronously in production.
    /// </summary>
    private static async Task<TuiMessage.ShowToast> WaitForOutcomeToastAsync(
        MailMode mode, CancellationToken cancellationToken)
    {
        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCts.CancelAfter(TimeSpan.FromSeconds(5));

        while (true)
        {
            var result = mode.Handle(new TuiMessage.RefreshRequested());

            if (result.Count > 0)
            {
                return Assert.IsType<TuiMessage.ShowToast>(Assert.Single(result));
            }

            await Task.Delay(5, timeoutCts.Token);
        }
    }

    private static async Task WaitUntilAsync(Func<bool> condition, CancellationToken cancellationToken)
    {
        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCts.CancelAfter(TimeSpan.FromSeconds(5));

        while (!condition())
        {
            await Task.Delay(5, timeoutCts.Token);
        }
    }

    [Fact]
    public void ComposeForm_Cancel_Should_CloseImmediately_When_NotDirty()
    {
        // arrange
        var store = new FakeMailStore();
        var mode = CreateMode(store);
        mode.OnEnter();
        mode.Handle(new TuiMessage.SelectInboxRequested()); // Workspace (the default) is read-only
        mode.Handle(new TuiMessage.ComposeRequested());

        // act
        var followUp = mode.HandleRawKey(Key(ConsoleKey.Escape));

        // assert
        Assert.Empty(followUp);
        Assert.False(mode.IsInputCapturing);
    }

    [Fact]
    public void ComposeForm_Cancel_Should_OpenDiscardConfirmation_When_Dirty()
    {
        // arrange
        var store = new FakeMailStore();
        var mode = CreateMode(store);
        mode.OnEnter();
        mode.Handle(new TuiMessage.SelectInboxRequested()); // Workspace (the default) is read-only
        mode.Handle(new TuiMessage.ComposeRequested());
        Type(mode, "bob");

        // act
        var followUp = mode.HandleRawKey(Key(ConsoleKey.Escape));

        // assert: still capturing input (now the discard confirmation).
        Assert.Empty(followUp);
        Assert.True(mode.IsInputCapturing);
    }

    [Fact]
    public void ComposeForm_DiscardConfirmed_Should_CloseTheForm()
    {
        // arrange
        var store = new FakeMailStore();
        var mode = CreateMode(store);
        mode.OnEnter();
        mode.Handle(new TuiMessage.SelectInboxRequested()); // Workspace (the default) is read-only
        mode.Handle(new TuiMessage.ComposeRequested());
        Type(mode, "bob");
        mode.HandleRawKey(Key(ConsoleKey.Escape));

        // act
        var followUp = mode.HandleRawKey(Key(ConsoleKey.Enter));

        // assert
        Assert.Empty(followUp);
        Assert.False(mode.IsInputCapturing);
    }

    [Fact]
    public void ComposeForm_DiscardCancelled_Should_ReturnToTheFormWithItsValuesIntact()
    {
        // arrange
        var store = new FakeMailStore();
        var mode = CreateMode(store);
        mode.OnEnter();
        mode.Handle(new TuiMessage.SelectInboxRequested()); // Workspace (the default) is read-only
        mode.Handle(new TuiMessage.ComposeRequested());
        Type(mode, "bob");
        mode.HandleRawKey(Key(ConsoleKey.Escape));

        // act
        var followUp = mode.HandleRawKey(Key(ConsoleKey.Escape));

        // assert: back to the compose form, still capturing, nothing sent.
        Assert.Empty(followUp);
        Assert.True(mode.IsInputCapturing);
        Assert.Empty(store.Messages);
    }

    [Fact]
    public void SelectSentRequested_Should_SwitchToSentMailboxAndLoadTheActorsSentMessages()
    {
        // arrange
        var store = new FakeMailStore();
        store.Messages.Add(MailMessageBuilder.Create(
            "m-1", sender: "alice", createdAt: Now, recipients: [MailMessageBuilder.ToRecipient("bob")]));
        var mode = CreateMode(store);
        mode.OnEnter();
        mode.Handle(new TuiMessage.SelectInboxRequested());
        Assert.Empty(mode.State.Messages); // alice is not a recipient of m-1

        // act
        var followUp = mode.Handle(new TuiMessage.SelectSentRequested());

        // assert
        Assert.Empty(followUp);
        Assert.Equal(MailMailbox.Sent, mode.State.Mailbox);
        Assert.Equal(["m-1"], mode.State.Messages.Select(m => m.Id));
    }

    [Fact]
    public void Render_Should_ShowTheMailboxName_NotTheFilterName_When_MailboxIsNotInbox()
    {
        // arrange
        var store = new FakeMailStore();
        store.Messages.Add(MailMessageBuilder.Create(
            "m-1", sender: "alice", createdAt: Now, recipients: [MailMessageBuilder.ToRecipient("bob")]));
        var mode = CreateMode(store);
        mode.OnEnter();
        mode.Handle(new TuiMessage.SelectSentRequested());
        var console = new TestConsole().Width(100).Height(20);

        // act
        console.Write(mode.Render(100, 20));

        // assert
        Assert.Contains("Sent (1)", console.Output);
    }

    [Fact]
    public void Render_Should_ShowTheFilterName_When_MailboxIsInboxAndListModeIsThreads()
    {
        // arrange: cycling f/F must stay visible in Threads mode too, not
        // just Flat - MailState.RefreshAsync applies Unread to Threads
        // client-side now, and even Archived (which still has no row-level
        // effect there - the store exposes no filtered thread query) at
        // least names itself in the header.
        var store = new FakeMailStore();
        AddMessage(store, "m-1", Now);
        var mode = CreateMode(store);
        mode.OnEnter();
        mode.Handle(new TuiMessage.SelectInboxRequested());
        mode.Handle(new TuiMessage.CycleView(1)); // Inbox -> Unread
        Assert.Equal(MailListMode.Threads, mode.State.ListMode);
        var console = new TestConsole().Width(100).Height(20);

        // act
        console.Write(mode.Render(100, 20));

        // assert
        Assert.Contains("Unread", console.Output);
    }

    [Fact]
    public async Task CycleView_Should_HideFullyReadThreads_When_MailboxIsInboxAndListModeIsThreads()
    {
        // arrange: m-1's thread is unread for alice; m-2's thread is already
        // read for her.
        var store = new FakeMailStore();
        store.Messages.Add(MailMessageBuilder.Create(
            "m-1", threadId: "t-1", createdAt: Now, recipients: [MailMessageBuilder.ToRecipient("alice")]));
        store.Messages.Add(MailMessageBuilder.Create(
            "m-2", threadId: "t-2", createdAt: Now.AddMinutes(1), recipients: [MailMessageBuilder.ToRecipient("alice")]));
        var mode = CreateMode(store);
        mode.OnEnter();
        mode.Handle(new TuiMessage.SelectInboxRequested());
        await mode.State.RefreshAsync(CancellationToken.None);
        await store.MarkReadAsync(["m-2"], "alice", CancellationToken.None);

        // act
        mode.Handle(new TuiMessage.CycleView(1)); // Inbox -> Unread

        // assert
        Assert.Equal(["t-1"], mode.State.Threads.Select(t => t.ThreadId));
    }

    [Fact]
    public void Render_Should_ShowTheFilterName_When_MailboxIsInboxAndListModeIsFlat()
    {
        // arrange
        var store = new FakeMailStore();
        AddMessage(store, "m-1", Now);
        var mode = CreateMode(store);
        mode.OnEnter();
        mode.Handle(new TuiMessage.SelectInboxRequested());
        mode.Handle(new TuiMessage.ToggleListModeRequested()); // Threads -> Flat
        mode.Handle(new TuiMessage.CycleView(1)); // Inbox -> Unread
        Assert.Equal(MailListMode.Flat, mode.State.ListMode);
        var console = new TestConsole().Width(100).Height(20);

        // act
        console.Write(mode.Render(100, 20));

        // assert
        Assert.Contains("Unread", console.Output);
    }

    [Fact]
    public void ToggleReadRequested_Should_ShowWarnToast_NotAnError_When_ActorIsNotARecipient_InSentMailbox()
    {
        // arrange: alice sent m-1 to bob, so alice has no message_recipients
        // row on it and the store would reject a read/unread write.
        var store = new FakeMailStore();
        store.Messages.Add(MailMessageBuilder.Create(
            "m-1", sender: "alice", createdAt: Now, recipients: [MailMessageBuilder.ToRecipient("bob")]));
        var mode = CreateMode(store);
        mode.OnEnter();
        mode.Handle(new TuiMessage.SelectSentRequested());

        // act
        var followUp = mode.Handle(new TuiMessage.ToggleReadRequested());

        // assert
        var toast = Assert.Single(followUp);
        Assert.Equal(ToastStyle.Warn, Assert.IsType<TuiMessage.ShowToast>(toast).Style);
    }

    [Fact]
    public void ArchiveRequested_Should_ShowWarnToast_NotAnError_When_ActorIsNotARecipient_InSentMailbox()
    {
        // arrange
        var store = new FakeMailStore();
        store.Messages.Add(MailMessageBuilder.Create(
            "m-1", sender: "alice", createdAt: Now, recipients: [MailMessageBuilder.ToRecipient("bob")]));
        var mode = CreateMode(store);
        mode.OnEnter();
        mode.Handle(new TuiMessage.SelectSentRequested());

        // act
        var followUp = mode.Handle(new TuiMessage.ArchiveRequested());

        // assert
        var toast = Assert.Single(followUp);
        Assert.Equal(ToastStyle.Warn, Assert.IsType<TuiMessage.ShowToast>(toast).Style);
        Assert.False(mode.IsInputCapturing);
    }

    [Fact]
    public void ToggleReadRequested_Should_Succeed_When_TheSentMessageIsSelfAddressed()
    {
        // arrange: alice addressed the message to herself, so a real
        // message_recipients row exists and the write should go through.
        var store = new FakeMailStore();
        store.Messages.Add(MailMessageBuilder.Create(
            "m-1", sender: "alice", createdAt: Now, recipients: [MailMessageBuilder.ToRecipient("alice")]));
        var mode = CreateMode(store);
        mode.OnEnter();
        mode.Handle(new TuiMessage.SelectSentRequested());

        // act
        var followUp = mode.Handle(new TuiMessage.ToggleReadRequested());

        // assert
        var toast = Assert.Single(followUp);
        Assert.Equal(ToastStyle.Success, Assert.IsType<TuiMessage.ShowToast>(toast).Style);
    }

    [Fact]
    public void ReplyRequested_Should_StillOpenTheReplyForm_ForTheActorsOwnSentMessage()
    {
        // arrange
        var store = new FakeMailStore();
        store.Messages.Add(MailMessageBuilder.Create(
            "m-1", sender: "alice", createdAt: Now, recipients: [MailMessageBuilder.ToRecipient("bob")]));
        var mode = CreateMode(store);
        mode.OnEnter();
        mode.Handle(new TuiMessage.SelectSentRequested());

        // act
        var followUp = mode.Handle(new TuiMessage.ReplyRequested());

        // assert
        Assert.Empty(followUp);
        Assert.True(mode.IsInputCapturing);
    }

    [Fact]
    public void ReplyRequested_Should_ShowWarnToast_When_NoMessageSelected()
    {
        // arrange
        var store = new FakeMailStore();
        var mode = CreateMode(store);
        mode.OnEnter();

        // act
        var followUp = mode.Handle(new TuiMessage.ReplyRequested());

        // assert
        var toast = Assert.Single(followUp);
        Assert.Equal(ToastStyle.Warn, Assert.IsType<TuiMessage.ShowToast>(toast).Style);
    }

    [Fact]
    public async Task ReplyForm_Submit_Should_SendReply_And_ShowSuccessToast_When_TheWakeDelivers()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var store = new FakeMailStore();
        AddMessage(store, "m-1", Now); // sender defaults to "sender", to "alice"
        var wakeObserver = new FakeMailWakeReceiptObserver();
        wakeObserver.StatusByActor["sender"] = FakeMailWakeReceiptObserver.Observation("sender", "delivered");
        var mode = CreateMode(store, wakeObserver: wakeObserver);
        mode.OnEnter();
        mode.Handle(new TuiMessage.SelectInboxRequested()); // Workspace (the default) is read-only
        mode.Handle(new TuiMessage.ReplyRequested());
        Type(mode, "On it.");
        mode.HandleRawKey(CtrlKey(ConsoleKey.S));

        // act
        var toast = await WaitForOutcomeToastAsync(mode, cancellationToken);

        // assert
        Assert.Equal(ToastStyle.Success, toast.Style);
        Assert.False(mode.IsInputCapturing);
        Assert.Contains(store.Messages, m => m.InReplyTo == "m-1" && m.Body == "On it.");
    }

    [Fact]
    public void ToggleReadRequested_Should_ShowReadOnlyToast_And_NotMutate_When_MailboxIsWorkspace()
    {
        // arrange: alice is a genuine recipient of m-1, so the write would
        // otherwise succeed; Workspace refuses it anyway, regardless of
        // recipient status, since it shows every agent's mail.
        var store = new FakeMailStore();
        AddMessage(store, "m-1", Now);
        var mode = CreateMode(store);
        mode.OnEnter();
        mode.Handle(new TuiMessage.SelectWorkspaceMailRequested());

        // act
        var followUp = mode.Handle(new TuiMessage.ToggleReadRequested());

        // assert
        var toast = Assert.Single(followUp);
        var shown = Assert.IsType<TuiMessage.ShowToast>(toast);
        Assert.Equal(ToastStyle.Warn, shown.Style);
        Assert.Contains("read-only", shown.Text, StringComparison.OrdinalIgnoreCase);
        Assert.Null(MailRecipientView.FindRecipient(store.Messages[0], "alice")!.ReadAt);
    }

    [Fact]
    public void OpenSelected_Should_NotMarkRead_When_MailboxIsWorkspace()
    {
        // arrange: alice is an unread recipient of m-1, so the write would
        // otherwise succeed; Workspace must stay inert regardless, since
        // opening a message there is an implicit side effect, not a gesture.
        var store = new FakeMailStore();
        AddMessage(store, "m-1", Now);
        var mode = CreateMode(store);
        mode.OnEnter();
        mode.Handle(new TuiMessage.SelectWorkspaceMailRequested());

        // act
        var followUp = mode.Handle(new TuiMessage.OpenSelected());

        // assert
        Assert.Empty(followUp);
        Assert.Null(MailRecipientView.FindRecipient(store.Messages[0], "alice")!.ReadAt);

        // act: flip back to List, then Right again so the MoveCursor(Right)
        // TogglePane path (List -> Detail) reaches the same
        // MaybeMarkSelectedRead gate and must stay inert too.
        mode.Handle(new TuiMessage.MoveCursor(CursorDirection.Left));
        var moveFollowUp = mode.Handle(new TuiMessage.MoveCursor(CursorDirection.Right));

        // assert
        Assert.Empty(moveFollowUp);
        Assert.Null(MailRecipientView.FindRecipient(store.Messages[0], "alice")!.ReadAt);
    }

    [Fact]
    public void ArchiveRequested_Should_ShowReadOnlyToast_And_NotOpenDialog_When_MailboxIsWorkspace()
    {
        // arrange
        var store = new FakeMailStore();
        AddMessage(store, "m-1", Now);
        var mode = CreateMode(store);
        mode.OnEnter();
        mode.Handle(new TuiMessage.SelectWorkspaceMailRequested());

        // act
        var followUp = mode.Handle(new TuiMessage.ArchiveRequested());

        // assert
        var toast = Assert.Single(followUp);
        var shown = Assert.IsType<TuiMessage.ShowToast>(toast);
        Assert.Equal(ToastStyle.Warn, shown.Style);
        Assert.False(mode.IsInputCapturing);
        Assert.Null(MailRecipientView.FindRecipient(store.Messages[0], "alice")!.ArchivedAt);
    }

    [Fact]
    public void ComposeRequested_Should_ShowReadOnlyToast_And_NotOpenForm_When_MailboxIsWorkspace()
    {
        // arrange
        var store = new FakeMailStore();
        var mode = CreateMode(store);
        mode.OnEnter();
        mode.Handle(new TuiMessage.SelectWorkspaceMailRequested());

        // act
        var followUp = mode.Handle(new TuiMessage.ComposeRequested());

        // assert
        var toast = Assert.Single(followUp);
        Assert.Equal(ToastStyle.Warn, Assert.IsType<TuiMessage.ShowToast>(toast).Style);
        Assert.False(mode.IsInputCapturing);
    }

    [Fact]
    public void ReplyRequested_Should_ShowReadOnlyToast_And_NotOpenForm_When_MailboxIsWorkspace_EvenForTheActorsOwnThread()
    {
        // arrange: alice sent m-1, so she participates in the thread and the
        // store's ResolveReplyAsync check would otherwise allow the reply;
        // Workspace refuses it anyway rather than special-casing threads
        // the actor participates in.
        var store = new FakeMailStore();
        store.Messages.Add(MailMessageBuilder.Create(
            "m-1", sender: "alice", createdAt: Now, recipients: [MailMessageBuilder.ToRecipient("bob")]));
        var mode = CreateMode(store);
        mode.OnEnter();
        mode.Handle(new TuiMessage.SelectWorkspaceMailRequested());

        // act
        var followUp = mode.Handle(new TuiMessage.ReplyRequested());

        // assert
        var toast = Assert.Single(followUp);
        Assert.Equal(ToastStyle.Warn, Assert.IsType<TuiMessage.ShowToast>(toast).Style);
        Assert.False(mode.IsInputCapturing);
    }

    [Fact]
    public void Render_Should_CarryTwoRedundantWorkspaceIndicators_When_MailboxIsWorkspace()
    {
        // arrange: the header names the mailbox, and the list pane's border
        // token is distinct from the plain board tokens every other
        // mailbox uses, so neither is the only signal the mode has
        // changed meaning.
        var store = new FakeMailStore();
        AddMessage(store, "m-1", Now);
        var mode = CreateMode(store);
        mode.OnEnter();
        mode.Handle(new TuiMessage.SelectWorkspaceMailRequested());
        var console = new TestConsole().Colors(ColorSystem.TrueColor).EmitAnsiSequences().Width(100).Height(20);

        // act
        console.Write(mode.Render(100, 20));

        // assert
        Assert.Contains("Workspace (1)", console.Output);
        Assert.NotEqual(
            ThemeTokens.GetStyle("board.column.border"),
            ThemeTokens.GetStyle(MailMode.ResolveListBorderToken(MailMailbox.Workspace, focused: true)));
        Assert.NotEqual(
            ThemeTokens.GetStyle("board.column.border.focused"),
            ThemeTokens.GetStyle(MailMode.ResolveListBorderToken(MailMailbox.Workspace, focused: true)));

        // assert: the border pane's output actually carries the ANSI
        // sequence for the Workspace-focused border style, not just an
        // unequal token in the abstract.
        var borderStyle = ThemeTokens.GetStyle(MailMode.ResolveListBorderToken(MailMailbox.Workspace, focused: true));
        var styleConsole = new TestConsole().Colors(ColorSystem.TrueColor).EmitAnsiSequences().Width(1).Height(1);
        styleConsole.Write(new Markup("x", borderStyle));
        var ansiPrefix = styleConsole.Output[..styleConsole.Output.IndexOf('x')];
        Assert.Contains(ansiPrefix, console.Output);
    }

    [Fact]
    public void ResolveListBorderToken_Should_UseThePlainBoardTokens_When_MailboxIsNotWorkspace()
    {
        // act & assert
        Assert.Equal("board.column.border", MailMode.ResolveListBorderToken(MailMailbox.Inbox, focused: false));
        Assert.Equal("board.column.border.focused", MailMode.ResolveListBorderToken(MailMailbox.Sent, focused: true));
    }

    [Fact]
    public void SuppressedGlobalHints_Should_BeEmpty_When_MailboxIsNotWorkspace()
    {
        // arrange
        var store = new FakeMailStore();
        var mode = CreateMode(store);
        mode.OnEnter();
        mode.Handle(new TuiMessage.SelectInboxRequested()); // Workspace (the default) is read-only

        // act & assert: outside Workspace, u/a/r/c stay live and their
        // footer hints stay visible.
        Assert.Empty(mode.SuppressedGlobalHints);
    }

    [Fact]
    public void SuppressedGlobalHints_Should_HideToggleReadArchiveReplyAndCompose_When_MailboxIsWorkspace()
    {
        // arrange: the same four gestures RefuseIfReadOnly refuses with a
        // toast in Workspace (see ToggleReadRequested_Should_ShowReadOnlyToast_...
        // and its siblings above), so their footer hints must go with them.
        var store = new FakeMailStore();
        var mode = CreateMode(store);
        mode.OnEnter();
        mode.Handle(new TuiMessage.SelectWorkspaceMailRequested());

        // act
        var suppressed = mode.SuppressedGlobalHints;

        // assert
        Assert.Equal(
        [
            MailKeyMap.ToggleReadHint,
            MailKeyMap.ArchiveHint,
            MailKeyMap.ReplyHint,
            MailKeyMap.ComposeHint
        ],
            suppressed);
    }

    [Fact]
    public void AgentFilterPickerRequested_Should_ShowWarnToast_When_MailboxIsNotWorkspace()
    {
        // arrange
        var store = new FakeMailStore();
        var mode = CreateMode(store);
        mode.OnEnter();
        mode.Handle(new TuiMessage.SelectInboxRequested()); // Workspace is the default mailbox

        // act
        var followUp = mode.Handle(new TuiMessage.AgentFilterPickerRequested());

        // assert
        var toast = Assert.Single(followUp);
        var shown = Assert.IsType<TuiMessage.ShowToast>(toast);
        Assert.Equal(ToastStyle.Warn, shown.Style);
        Assert.Contains("Workspace", shown.Text);
        Assert.False(mode.IsInputCapturing);
    }

    [Fact]
    public void AgentFilterPickerRequested_Should_OpenPicker_When_MailboxIsWorkspace()
    {
        // arrange
        var store = new FakeMailStore();
        var registry = new FakeAgentRegistry();
        registry.Agents.Add(Agent("bob"));
        var mode = CreateMode(store, agentRegistry: registry);
        mode.OnEnter();
        mode.Handle(new TuiMessage.SelectWorkspaceMailRequested());

        // act
        var followUp = mode.Handle(new TuiMessage.AgentFilterPickerRequested());

        // assert
        Assert.Empty(followUp);
        Assert.True(mode.IsInputCapturing);
    }

    [Fact]
    public void AgentFilterPickerRequested_Should_ShowClientNextToName_When_AgentHasOne()
    {
        // arrange
        var store = new FakeMailStore();
        var registry = new FakeAgentRegistry();
        registry.Agents.Add(Agent("bob") with { Client = "codex" });
        registry.Agents.Add(Agent("carol"));
        var mode = CreateMode(store, agentRegistry: registry);
        mode.OnEnter();
        mode.Handle(new TuiMessage.SelectWorkspaceMailRequested());
        mode.Handle(new TuiMessage.AgentFilterPickerRequested());
        var console = new TestConsole().Width(100).Height(20);

        // act
        console.Write(mode.Render(100, 20));

        // assert: bob's client is shown, carol's empty client shows nothing
        Assert.Contains("bob (codex)", console.Output);
        Assert.Contains("carol", console.Output);
        Assert.DoesNotContain("carol (", console.Output);
    }

    [Fact]
    public void AgentFilterPicker_Applied_Should_NarrowWorkspaceMessages_ToMessagesTheAgentSentOrReceived()
    {
        // arrange
        var store = new FakeMailStore();
        store.Messages.Add(MailMessageBuilder.Create(
            "m-1", sender: "bob", createdAt: Now, recipients: [MailMessageBuilder.ToRecipient("carol")]));
        store.Messages.Add(MailMessageBuilder.Create(
            "m-2", sender: "carol", createdAt: Now.AddMinutes(1), recipients: [MailMessageBuilder.ToRecipient("bob")]));
        store.Messages.Add(MailMessageBuilder.Create(
            "m-3", sender: "carol", createdAt: Now.AddMinutes(2), recipients: [MailMessageBuilder.ToRecipient("dave")]));
        var registry = new FakeAgentRegistry();
        registry.Agents.Add(Agent("bob"));
        var mode = CreateMode(store, agentRegistry: registry);
        mode.OnEnter();
        mode.Handle(new TuiMessage.SelectWorkspaceMailRequested());
        mode.Handle(new TuiMessage.AgentFilterPickerRequested());

        // act: move down from "All agents" to "bob", then apply
        mode.HandleRawKey(Key(ConsoleKey.DownArrow));
        var followUp = mode.HandleRawKey(Key(ConsoleKey.Enter));

        // assert: m-1 (bob sent it) and m-2 (bob received it), not m-3
        // (bob is neither sender nor recipient)
        Assert.Empty(followUp);
        Assert.False(mode.IsInputCapturing);
        Assert.Equal("bob", mode.State.AgentFilter);
        Assert.Equal(["m-2", "m-1"], mode.State.Messages.Select(m => m.Id));
    }

    [Fact]
    public void AgentFilterPicker_Applied_AllAgents_Should_RestoreTheFullWorkspaceStream()
    {
        // arrange
        var store = new FakeMailStore();
        store.Messages.Add(MailMessageBuilder.Create(
            "m-1", sender: "bob", createdAt: Now, recipients: [MailMessageBuilder.ToRecipient("carol")]));
        store.Messages.Add(MailMessageBuilder.Create(
            "m-2", sender: "carol", createdAt: Now.AddMinutes(1), recipients: [MailMessageBuilder.ToRecipient("dave")]));
        var registry = new FakeAgentRegistry();
        registry.Agents.Add(Agent("bob"));
        var mode = CreateMode(store, agentRegistry: registry);
        mode.OnEnter();
        mode.Handle(new TuiMessage.SelectWorkspaceMailRequested());
        mode.Handle(new TuiMessage.AgentFilterPickerRequested());
        mode.HandleRawKey(Key(ConsoleKey.DownArrow));
        mode.HandleRawKey(Key(ConsoleKey.Enter));
        Assert.Equal(["m-1"], mode.State.Messages.Select(m => m.Id));

        // act: reopen the picker (pre-selected on "bob") and move back up to
        // "All agents", the picker's first row, then apply
        mode.Handle(new TuiMessage.AgentFilterPickerRequested());
        mode.HandleRawKey(Key(ConsoleKey.UpArrow));
        var followUp = mode.HandleRawKey(Key(ConsoleKey.Enter));

        // assert
        Assert.Empty(followUp);
        Assert.Null(mode.State.AgentFilter);
        Assert.Equal(["m-2", "m-1"], mode.State.Messages.Select(m => m.Id));
    }

    [Fact]
    public void AgentFilterPicker_Cancelled_Should_LeaveTheFilterUnchanged()
    {
        // arrange
        var store = new FakeMailStore();
        store.Messages.Add(MailMessageBuilder.Create(
            "m-1", sender: "bob", createdAt: Now, recipients: [MailMessageBuilder.ToRecipient("carol")]));
        var registry = new FakeAgentRegistry();
        registry.Agents.Add(Agent("bob"));
        var mode = CreateMode(store, agentRegistry: registry);
        mode.OnEnter();
        mode.Handle(new TuiMessage.SelectWorkspaceMailRequested());
        var messagesBeforeCancel = mode.State.Messages.Select(m => m.Id).ToList();
        mode.Handle(new TuiMessage.AgentFilterPickerRequested());
        mode.HandleRawKey(Key(ConsoleKey.DownArrow));

        // act
        var followUp = mode.HandleRawKey(Key(ConsoleKey.Escape));

        // assert
        Assert.Empty(followUp);
        Assert.False(mode.IsInputCapturing);
        Assert.Null(mode.State.AgentFilter);
        Assert.Equal(messagesBeforeCancel, mode.State.Messages.Select(m => m.Id));
    }

    [Fact]
    public void Render_Should_ShowTheSelectedAgentInTheHeader_When_AgentFilterIsSetInWorkspace()
    {
        // arrange
        var store = new FakeMailStore();
        store.Messages.Add(MailMessageBuilder.Create(
            "m-1", sender: "bob", createdAt: Now, recipients: [MailMessageBuilder.ToRecipient("carol")]));
        var registry = new FakeAgentRegistry();
        registry.Agents.Add(Agent("bob"));
        var mode = CreateMode(store, agentRegistry: registry);
        mode.OnEnter();
        mode.Handle(new TuiMessage.SelectWorkspaceMailRequested());
        mode.Handle(new TuiMessage.AgentFilterPickerRequested());
        mode.HandleRawKey(Key(ConsoleKey.DownArrow));
        mode.HandleRawKey(Key(ConsoleKey.Enter));
        var console = new TestConsole().Width(100).Height(20);

        // act
        console.Write(mode.Render(100, 20));

        // assert
        Assert.Contains("Workspace: bob (1)", console.Output);
    }

    [Fact]
    public void SelectInboxRequested_Should_ClearAgentFilter_When_LeavingWorkspace()
    {
        // arrange
        var store = new FakeMailStore();
        store.Messages.Add(MailMessageBuilder.Create(
            "m-1", sender: "bob", createdAt: Now, recipients: [MailMessageBuilder.ToRecipient("alice")]));
        var registry = new FakeAgentRegistry();
        registry.Agents.Add(Agent("bob"));
        var mode = CreateMode(store, agentRegistry: registry);
        mode.OnEnter();
        mode.Handle(new TuiMessage.SelectWorkspaceMailRequested());
        mode.Handle(new TuiMessage.AgentFilterPickerRequested());
        mode.HandleRawKey(Key(ConsoleKey.DownArrow));
        mode.HandleRawKey(Key(ConsoleKey.Enter));
        Assert.Equal("bob", mode.State.AgentFilter);

        // act: leave Workspace for Inbox, then come back
        mode.Handle(new TuiMessage.SelectInboxRequested());
        mode.Handle(new TuiMessage.SelectWorkspaceMailRequested());

        // assert
        Assert.Null(mode.State.AgentFilter);
    }

    [Fact]
    public void Render_Should_ApplyAnsiStyling_ToRowGlyphFromToAndAgeTokens_When_MessageReceived()
    {
        // arrange: alice receives two messages, each as its sole recipient,
        // so both rows carry the direct glyph and the From/To/age tokens. A
        // second message so at least one row is unselected: the
        // default-selected row 0 merges its token color with
        // selection.highlight's background into one ANSI sequence, which
        // would not match a token's style checked in isolation.
        var store = new FakeMailStore();
        AddMessage(store, "m-1", Now);
        AddMessage(store, "m-2", Now.AddMinutes(1));
        var mode = CreateMode(store);
        mode.OnEnter();
        var console = new TestConsole().Colors(ColorSystem.TrueColor).EmitAnsiSequences().Width(100).Height(20);

        // act
        console.Write(mode.Render(100, 20));

        // assert: a plain TestConsole strips markup entirely, so a wrong or
        // missing token name on any of these columns would still leave
        // every plain-text Contains assertion elsewhere green.
        AssertAnsiStyleApplied(console.Output, "mail.row.glyph.direct");
        AssertAnsiStyleApplied(console.Output, "mail.row.from");
        AssertAnsiStyleApplied(console.Output, "mail.row.to");
        AssertAnsiStyleApplied(console.Output, "mail.row.age");
    }

    [Fact]
    public void Render_Should_ApplyFromMeToken_NotThePlainFromToken_When_ActorSentTheMessage()
    {
        // arrange: two sent messages so at least one row is unselected; see
        // Render_Should_ApplyAnsiStyling_ToRowGlyphFromToAndAgeTokens_When_MessageReceived
        // for why the selected row's merged style would not match. The
        // table shows literal From/To columns now (no swapped Peer column),
        // so alice's own name in the From column gets the distinct
        // mail.row.from.me token instead of the plain mail.row.from one.
        var store = new FakeMailStore();
        store.Messages.Add(MailMessageBuilder.Create(
            "m-1", sender: "alice", createdAt: Now, recipients: [MailMessageBuilder.ToRecipient("bob")]));
        store.Messages.Add(MailMessageBuilder.Create(
            "m-2", sender: "alice", createdAt: Now.AddMinutes(1), recipients: [MailMessageBuilder.ToRecipient("bob")]));
        var mode = CreateMode(store);
        mode.OnEnter();
        mode.Handle(new TuiMessage.SelectSentRequested());
        var console = new TestConsole().Colors(ColorSystem.TrueColor).EmitAnsiSequences().Width(100).Height(20);

        // act
        console.Write(mode.Render(100, 20));

        // assert
        AssertAnsiStyleApplied(console.Output, "mail.row.from.me");
        AssertAnsiStyleApplied(console.Output, "mail.row.glyph.from-me");
    }

    [Fact]
    public void Render_Should_ApplyAnsiStyling_ToWorkspaceHeaderText_When_MailboxIsWorkspace()
    {
        // arrange: Spectre paints a panel's header text with its
        // BorderStyle, so the Workspace header text sits inside the same
        // styled run as the border characters (which
        // Render_Should_CarryTwoRedundantWorkspaceIndicators_When_MailboxIsWorkspace
        // already covers) rather than a fresh escape sequence opened right
        // before the header text; this asserts the styled run reaches the
        // header text uninterrupted by any escape sequence, rather than
        // requiring the escape sequence literally right in front of it.
        var store = new FakeMailStore();
        AddMessage(store, "m-1", Now);
        var mode = CreateMode(store);
        mode.OnEnter();
        mode.Handle(new TuiMessage.SelectWorkspaceMailRequested());
        var console = new TestConsole().Colors(ColorSystem.TrueColor).EmitAnsiSequences().Width(100).Height(20);

        // act
        console.Write(mode.Render(100, 20));

        // assert
        var borderToken = MailMode.ResolveListBorderToken(MailMailbox.Workspace, focused: true);
        var style = ThemeTokens.GetStyle(borderToken);
        var styleConsole = new TestConsole().Colors(ColorSystem.TrueColor).EmitAnsiSequences().Width(1).Height(1);
        styleConsole.Write(new Markup("x", style));
        var ansiPrefix = styleConsole.Output[..styleConsole.Output.IndexOf('x')];

        var ansiIndex = console.Output.IndexOf(ansiPrefix, StringComparison.Ordinal);
        var textIndex = console.Output.IndexOf("Workspace (1)", StringComparison.Ordinal);
        Assert.True(ansiIndex >= 0, "Expected the Workspace border/header ANSI sequence to appear.");
        Assert.True(textIndex > ansiIndex, "Expected the header text to follow the styled run.");
        var runStart = ansiIndex + ansiPrefix.Length;
        Assert.Equal(-1, console.Output.IndexOf('\u001b', runStart, textIndex - runStart));
    }

    [Fact]
    public void ToggleListModeRequested_Should_SwitchBetweenThreadsAndFlatListMode()
    {
        // arrange
        var store = new FakeMailStore();
        AddMessage(store, "m-1", Now);
        var mode = CreateMode(store);
        mode.OnEnter();
        Assert.Equal(MailListMode.Threads, mode.State.ListMode);

        // act
        mode.Handle(new TuiMessage.ToggleListModeRequested());

        // assert
        Assert.Equal(MailListMode.Flat, mode.State.ListMode);

        // act again
        mode.Handle(new TuiMessage.ToggleListModeRequested());

        // assert
        Assert.Equal(MailListMode.Threads, mode.State.ListMode);
    }

    [Fact]
    public void FoldPrefixRequested_Should_EnterCapturingState()
    {
        // arrange
        var store = new FakeMailStore();
        AddMessage(store, "m-1", Now);
        var mode = CreateMode(store);
        mode.OnEnter();

        // act
        var followUp = mode.Handle(new TuiMessage.FoldPrefixRequested());

        // assert
        Assert.Empty(followUp);
        Assert.True(mode.IsInputCapturing);
    }

    [Fact]
    public void FoldPrefixRequested_Should_ShowWarnToast_When_ListModeIsFlat()
    {
        // arrange
        var store = new FakeMailStore();
        AddMessage(store, "m-1", Now);
        var mode = CreateMode(store);
        mode.OnEnter();
        mode.Handle(new TuiMessage.ToggleListModeRequested());

        // act
        var followUp = mode.Handle(new TuiMessage.FoldPrefixRequested());

        // assert
        var toast = Assert.Single(followUp);
        var shown = Assert.IsType<TuiMessage.ShowToast>(toast);
        Assert.Equal(ToastStyle.Warn, shown.Style);
        Assert.False(mode.IsInputCapturing);
    }

    [Fact]
    public void FoldPrefixThenO_Should_ExpandTheSelectedThread()
    {
        // arrange: a two-message thread so expanding is observable as an
        // extra row.
        var store = new FakeMailStore();
        store.Messages.Add(MailMessageBuilder.Create(
            "m-1", threadId: "t-1", createdAt: Now, recipients: [MailMessageBuilder.ToRecipient("alice")]));
        store.Messages.Add(MailMessageBuilder.Create(
            "m-2", threadId: "t-1", createdAt: Now.AddMinutes(1), recipients: [MailMessageBuilder.ToRecipient("alice")]));
        var mode = CreateMode(store);
        mode.OnEnter();
        Assert.Single(mode.State.Rows); // one collapsed thread row

        // act: z then o (open/expand)
        mode.Handle(new TuiMessage.FoldPrefixRequested());
        mode.HandleRawKey(Key('o'));

        // assert
        Assert.False(mode.IsInputCapturing);
        Assert.Equal(3, mode.State.Rows.Count); // thread row + its two messages
    }

    [Fact]
    public void FoldPrefixThenC_Should_CollapseAnExpandedThread()
    {
        // arrange
        var store = new FakeMailStore();
        store.Messages.Add(MailMessageBuilder.Create(
            "m-1", threadId: "t-1", createdAt: Now, recipients: [MailMessageBuilder.ToRecipient("alice")]));
        store.Messages.Add(MailMessageBuilder.Create(
            "m-2", threadId: "t-1", createdAt: Now.AddMinutes(1), recipients: [MailMessageBuilder.ToRecipient("alice")]));
        var mode = CreateMode(store);
        mode.OnEnter();
        mode.State.ExpandThread("t-1");
        Assert.Equal(3, mode.State.Rows.Count);

        // act: z then c (close/collapse)
        mode.Handle(new TuiMessage.FoldPrefixRequested());
        mode.HandleRawKey(Key('c'));

        // assert
        Assert.Single(mode.State.Rows);
    }

    [Fact]
    public void FoldPrefixThenA_Should_ToggleTheSelectedThread()
    {
        // arrange
        var store = new FakeMailStore();
        store.Messages.Add(MailMessageBuilder.Create(
            "m-1", threadId: "t-1", createdAt: Now, recipients: [MailMessageBuilder.ToRecipient("alice")]));
        store.Messages.Add(MailMessageBuilder.Create(
            "m-2", threadId: "t-1", createdAt: Now.AddMinutes(1), recipients: [MailMessageBuilder.ToRecipient("alice")]));
        var mode = CreateMode(store);
        mode.OnEnter();

        // act: z then a (toggle) twice
        mode.Handle(new TuiMessage.FoldPrefixRequested());
        mode.HandleRawKey(Key('a'));
        Assert.Equal(3, mode.State.Rows.Count);

        mode.Handle(new TuiMessage.FoldPrefixRequested());
        mode.HandleRawKey(Key('a'));

        // assert
        Assert.Single(mode.State.Rows);
    }

    [Fact]
    public void FoldPrefixThenShiftRAndShiftM_Should_ExpandAndCollapseEveryThread()
    {
        // arrange
        var store = new FakeMailStore();
        AddMessage(store, "m-1", Now);
        AddMessage(store, "m-2", Now.AddMinutes(1));
        var mode = CreateMode(store);
        mode.OnEnter();
        Assert.Equal(2, mode.State.Rows.Count); // two collapsed singleton threads

        // act: z then Shift+R (unfold all)
        mode.Handle(new TuiMessage.FoldPrefixRequested());
        mode.HandleRawKey(new ConsoleKeyInfo('R', ConsoleKey.R, shift: true, alt: false, control: false));

        // assert: each thread row now has its one message as an indented
        // child row too, so the row count doubles.
        Assert.Equal(4, mode.State.Rows.Count);
        var threadRows = mode.State.Rows.OfType<MailListRow.Thread>().ToList();
        Assert.Equal(2, threadRows.Count);
        Assert.All(threadRows, row => Assert.True(row.Expanded));

        // act: z then Shift+M (fold all)
        mode.Handle(new TuiMessage.FoldPrefixRequested());
        mode.HandleRawKey(new ConsoleKeyInfo('M', ConsoleKey.M, shift: true, alt: false, control: false));

        // assert
        Assert.All(mode.State.Rows, row => Assert.False(((MailListRow.Thread)row).Expanded));
    }

    [Fact]
    public void FoldPrefixThenUnrecognizedKey_Should_CancelWithNoAction()
    {
        // arrange
        var store = new FakeMailStore();
        AddMessage(store, "m-1", Now);
        var mode = CreateMode(store);
        mode.OnEnter();

        // act: z then Escape - vim's own za/zo/zc/zR/zM has no error toast
        // for an unrecognized second key.
        mode.Handle(new TuiMessage.FoldPrefixRequested());
        var followUp = mode.HandleRawKey(Key(ConsoleKey.Escape));

        // assert
        Assert.Empty(followUp);
        Assert.False(mode.IsInputCapturing);
        Assert.Single(mode.State.Rows);
    }

    [Fact]
    public void Render_Should_ShowUnreadToMeHighlight_InWorkspace_ForAMessageAddressedToTheActor_And_NotForAThirdPartyMessage()
    {
        // arrange: m-1 is unread and addressed to alice (the actor); m-2 is
        // unread between two other agents and never addresses alice at all.
        // Workspace shows both, but only m-1's row may carry the
        // unread-to-me highlight (epic wi3 convention 8: never another
        // agent's read state).
        var store = new FakeMailStore();
        store.Messages.Add(MailMessageBuilder.Create(
            "m-1", sender: "bob", createdAt: Now, recipients: [MailMessageBuilder.ToRecipient("alice")]));
        store.Messages.Add(MailMessageBuilder.Create(
            "m-2", sender: "carol", createdAt: Now.AddMinutes(1), recipients: [MailMessageBuilder.ToRecipient("dave")]));
        var mode = CreateMode(store);
        mode.OnEnter();
        Assert.Equal(MailMailbox.Workspace, mode.State.Mailbox);
        var console = new TestConsole().Width(100).Height(20);

        // act
        console.Write(mode.Render(100, 20));

        // assert: exactly one unread-to-me marker, not two.
        var markerCount = console.Output.Split('\u25cf').Length - 1;
        Assert.Equal(1, markerCount);
    }
}
