using System.Diagnostics;
using System.Threading.Channels;
using ChilliCream.Nitro.CommandLine.Services.Notify;
using ChilliCream.Nitro.CommandLine.Services.Workspace;
using ChilliCream.Nitro.CommandLine.Tests.Tui.Agents;
using ChilliCream.Nitro.CommandLine.Tui.Input;
using ChilliCream.Nitro.CommandLine.Tui.Mail;
using ChilliCream.Nitro.CommandLine.Tui.Runtime;
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
        FakeMailWakeReceiptObserver? wakeObserver = null,
        IActorWakeDispatcher? wakeDispatcher = null)
        => new(
            store,
            actor,
            agentRegistry ?? new FakeAgentRegistry(),
            wakeDispatcher ?? new DaemonOwnedActorWakeDispatcher(),
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
    [InlineData("partial", "Error")]
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

        if (wakeStatus == "partial")
        {
            Assert.Contains("Notification partial", toast.Text, StringComparison.Ordinal);
        }
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

        // assert: the compose form reopens with the typed subject intact
        // behind the error toast, rather than losing the draft.
        Assert.Equal(ToastStyle.Error, toast.Style);
        Assert.Empty(store.Messages);
        Assert.True(mode.IsInputCapturing);
        var console = new TestConsole().Width(100).Height(20);
        console.Write(mode.Render(100, 20));
        Assert.Contains("Status", console.Output);
    }

    [Fact]
    public async Task ReplyForm_Submit_Should_ShowErrorToast_And_ReopenTheForm_When_StoreRejectsTheWrite()
    {
        // arrange: the replied-to message is removed from the store between
        // opening the reply form and submitting it, so
        // FakeMailStore.ReplyMessageAsync rejects the write with an
        // ExitException the same way an unknown message id would against
        // the real store.
        var cancellationToken = TestContext.Current.CancellationToken;
        var store = new FakeMailStore();
        store.Messages.Add(MailMessageBuilder.Create(
            "m-1", sender: "bob", createdAt: Now, recipients: [MailMessageBuilder.ToRecipient("alice")]));
        var mode = CreateMode(store);
        mode.OnEnter();
        mode.Handle(new TuiMessage.SelectInboxRequested());
        mode.State.SelectedRow = 0;
        mode.Handle(new TuiMessage.ReplyRequested());
        Type(mode, "On it.");
        store.Messages.Clear();
        mode.HandleRawKey(CtrlKey(ConsoleKey.S));

        // act
        var toast = await WaitForOutcomeToastAsync(mode, cancellationToken);

        // assert: the reply form reopens with the typed body intact behind
        // the error toast, rather than losing the draft.
        Assert.Equal(ToastStyle.Error, toast.Style);
        Assert.Empty(store.Messages);
        Assert.True(mode.IsInputCapturing);
        var console = new TestConsole().Width(100).Height(20);
        console.Write(mode.Render(100, 20));
        Assert.Contains("On it.", console.Output);
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
    public async Task CreateQuitGate_Should_CountAPendingNotification_ForACompletionTheDrainItselfObserves()
    {
        // arrange: the wake observer's own default (no StatusByActor entry)
        // is MailWakeTargetStatus.Pending, so the send completes on its own
        // during the gate's bounded drain, but its notification is still
        // owed - the gate must count that truthfully even though
        // TuiEffectQueue itself already reports PendingCount 0 by the time
        // the gate inspects it.
        var cancellationToken = TestContext.Current.CancellationToken;
        var store = new FakeMailStore();
        var mode = CreateMode(store);
        mode.OnEnter();
        mode.Handle(new TuiMessage.SelectInboxRequested());
        mode.Handle(new TuiMessage.ComposeRequested());
        Type(mode, "bob");
        mode.HandleRawKey(Key(ConsoleKey.Tab));
        Type(mode, "Status");
        mode.HandleRawKey(Key(ConsoleKey.Tab));
        Type(mode, "Body");
        mode.HandleRawKey(CtrlKey(ConsoleKey.S));

        // act: the gate's own bounded drain observes the completion itself,
        // never delivered through Handle.
        var report = await mode.CreateQuitGate()(TimeSpan.FromSeconds(5), cancellationToken);

        // assert
        Assert.Equal(1, report.PendingCount);
        Assert.Equal(0, report.OutcomeUnknownCount);
        Assert.True(report.HasUnresolvedWork);
    }

    [Fact]
    public async Task CreateQuitGate_Should_CountOutcomeUnknown_ForACompletionTheDrainItselfObserves_When_TheWakeStepFaults()
    {
        // arrange: the standalone board's real dispatcher failing after the
        // commit reconciles to an unknown outcome (see ReconcileWakeAsync),
        // never a failure - the gate must report that as outcome-unknown,
        // not pending.
        var cancellationToken = TestContext.Current.CancellationToken;
        var store = new FakeMailStore();
        var mode = CreateMode(store, wakeDispatcher: new ThrowingActorWakeDispatcher());
        mode.OnEnter();
        mode.Handle(new TuiMessage.SelectInboxRequested());
        mode.Handle(new TuiMessage.ComposeRequested());
        Type(mode, "bob");
        mode.HandleRawKey(Key(ConsoleKey.Tab));
        Type(mode, "Status");
        mode.HandleRawKey(Key(ConsoleKey.Tab));
        Type(mode, "Body");
        mode.HandleRawKey(CtrlKey(ConsoleKey.S));

        // act
        var report = await mode.CreateQuitGate()(TimeSpan.FromSeconds(5), cancellationToken);

        // assert
        Assert.Equal(0, report.PendingCount);
        Assert.Equal(1, report.OutcomeUnknownCount);
        Assert.True(report.HasUnresolvedWork);
    }

    [Fact]
    public async Task CreateQuitGate_Should_LeaveTheStashedToastForTheNextHandle_AfterACancelledQuit()
    {
        // arrange: mirrors TuiShell.QuitCancelled - the gate already drained
        // and classified this completion, so a cancelled second
        // confirmation resuming the live TUI must still show its toast on
        // the next Handle, rather than losing it because it never reached
        // Handle's own drain.
        var cancellationToken = TestContext.Current.CancellationToken;
        var store = new FakeMailStore();
        var mode = CreateMode(store);
        mode.OnEnter();
        mode.Handle(new TuiMessage.SelectInboxRequested());
        mode.Handle(new TuiMessage.ComposeRequested());
        Type(mode, "bob");
        mode.HandleRawKey(Key(ConsoleKey.Tab));
        Type(mode, "Status");
        mode.HandleRawKey(Key(ConsoleKey.Tab));
        Type(mode, "Body");
        mode.HandleRawKey(CtrlKey(ConsoleKey.S));
        var report = await mode.CreateQuitGate()(TimeSpan.FromSeconds(5), cancellationToken);
        Assert.True(report.HasUnresolvedWork); // the second confirmation the shell would show

        // act: mirrors TuiShell.QuitCancelled firing after the user declines
        // the second confirmation.
        mode.ResumeSendAcceptance();
        var followUp = mode.Handle(new TuiMessage.RefreshRequested());

        // assert
        var toast = Assert.IsType<TuiMessage.ShowToast>(Assert.Single(followUp));
        Assert.Equal(ToastStyle.Warn, toast.Style);
        Assert.Contains("pending", toast.Text, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ComposeForm_Submit_Should_ShowOutcomeUnknownToast_When_TheStandaloneDispatcherFails()
    {
        // arrange: the standalone board's real dispatcher (never a daemon)
        // throwing after the store write already committed must still be
        // reported as sent, only with its wake outcome unresolved - this is
        // the foreground-failure path BoardMailCommandTests cannot drive,
        // since there is no interactive loop there to submit a compose
        // through.
        var cancellationToken = TestContext.Current.CancellationToken;
        var store = new FakeMailStore();
        var mode = CreateMode(store, wakeDispatcher: new ThrowingActorWakeDispatcher());
        mode.OnEnter();
        mode.Handle(new TuiMessage.SelectInboxRequested());
        mode.Handle(new TuiMessage.ComposeRequested());
        Type(mode, "bob");
        mode.HandleRawKey(Key(ConsoleKey.Tab));
        Type(mode, "Status");
        mode.HandleRawKey(Key(ConsoleKey.Tab));
        Type(mode, "Body");
        mode.HandleRawKey(CtrlKey(ConsoleKey.S));

        // act
        var toast = await WaitForOutcomeToastAsync(mode, cancellationToken);

        // assert
        Assert.Equal(ToastStyle.Warn, toast.Style);
        Assert.Contains("Notification outcome unknown", toast.Text, StringComparison.Ordinal);
        var sent = Assert.Single(store.Messages);
        Assert.Single(sent.WakeReceipts);
    }

    [Fact]
    public async Task ComposeForm_Submit_Should_ShowStoredToast_Before_TheWakeStepResolves()
    {
        // arrange: the wake observer is gated open, so the commit has
        // already landed but the dispatch-and-observe step has not resolved
        // yet - the truthful intermediate "Stored" toast (perles-net-4mn
        // comment 211, step 1) must already be visible in that window, well
        // ahead of the terminal outcome toast.
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

        // act: the terminal outcome cannot have resolved yet, since the
        // observer is still gated open.
        var followUp = mode.Handle(new TuiMessage.RefreshRequested());

        // assert
        var stored = Assert.IsType<TuiMessage.ShowToast>(Assert.Single(followUp));
        Assert.Equal(ToastStyle.Info, stored.Style);
        var sent = Assert.Single(store.Messages);
        Assert.Equal($"Stored '{sent.Id}' to bob.", stored.Text);

        // cleanup: release the gate so the send resolves.
        wakeObserver.Gate!.SetResult();
        await WaitForOutcomeToastAsync(mode, cancellationToken);
    }

    [Fact]
    public async Task ComposeForm_Submit_Should_ShowOutcomeUnknownToast_Without_AssertingNotStored_When_TheSendEffectFaults()
    {
        // arrange: a non-ExitException from the store write itself (a
        // genuine bug, not a rejected write) reaches the send effect as
        // Faulted. Only an observed ExitException proves a rejected write
        // (see MailSendOutcome.Failed), so this toast must state the
        // commit's outcome as unknown rather than asserting the message was
        // not stored (perles-net-4mn comment 211, step 3).
        var cancellationToken = TestContext.Current.CancellationToken;
        var store = new FakeMailStore { SendFault = new InvalidOperationException("boom") };
        var mode = CreateMode(store);
        mode.OnEnter();
        mode.Handle(new TuiMessage.SelectInboxRequested());
        mode.Handle(new TuiMessage.ComposeRequested());
        Type(mode, "bob");
        mode.HandleRawKey(Key(ConsoleKey.Tab));
        Type(mode, "Status");
        mode.HandleRawKey(Key(ConsoleKey.Tab));
        Type(mode, "Body");
        mode.HandleRawKey(CtrlKey(ConsoleKey.S));

        // act
        var toast = await WaitForOutcomeToastAsync(mode, cancellationToken);

        // assert
        Assert.Equal(ToastStyle.Error, toast.Style);
        Assert.Equal("Sending did not complete. The message's outcome is unknown.", toast.Text);
        Assert.Empty(store.Messages);
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
    /// <summary>
    /// Polls <see cref="MailMode.Handle"/> until a terminal outcome toast
    /// (<see cref="MailSendOutcome.Succeeded"/>, <see cref="MailSendOutcome.Reconciled"/>,
    /// or <see cref="MailSendOutcome.Failed"/>) surfaces, skipping over the
    /// intermediate <see cref="MailSendOutcome.Stored"/> notice, which can
    /// land in the same drain as, on its own ahead of, or (for a write the
    /// store rejects outright) never at all relative to the terminal toast.
    /// Every terminal outcome shows <see cref="ToastStyle.Success"/>,
    /// <see cref="ToastStyle.Warn"/>, or <see cref="ToastStyle.Error"/>; only
    /// the transient "Sending…" and "Stored" toasts ever show
    /// <see cref="ToastStyle.Info"/>, so that style alone distinguishes them
    /// without depending on drain timing.
    /// </summary>
    private static async Task<TuiMessage.ShowToast> WaitForOutcomeToastAsync(
        MailMode mode, CancellationToken cancellationToken)
    {
        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCts.CancelAfter(TimeSpan.FromSeconds(5));

        while (true)
        {
            var result = mode.Handle(new TuiMessage.RefreshRequested());

            foreach (var message in result)
            {
                var toast = Assert.IsType<TuiMessage.ShowToast>(message);

                if (toast.Style != ToastStyle.Info)
                {
                    return toast;
                }
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
    public async Task ShieldPendingSendsAsync_Should_LetACommittedWriteLand_When_TheEffectTokenIsCancelled()
    {
        // arrange: the store commit is gated open when the effect token
        // cancels, mirroring a Ctrl+C landing strictly after a submitted
        // send's store write has already started; ShieldPendingSendsAsync
        // is the only drain a host-cancelled exit gets, and the write
        // itself is never cancelled by the effect token.
        var cancellationToken = TestContext.Current.CancellationToken;
        var gate = new TaskCompletionSource();
        var store = new FakeMailStore { SendGate = gate };
        using var effectCts = new CancellationTokenSource();
        var mode = new MailMode(
            store,
            "alice",
            new FakeAgentRegistry(),
            new DaemonOwnedActorWakeDispatcher(),
            new FakeMailWakeReceiptObserver(),
            new FakeTimeProvider(Now),
            effectCts.Token);
        mode.OnEnter();
        mode.Handle(new TuiMessage.SelectInboxRequested());
        mode.Handle(new TuiMessage.ComposeRequested());
        Type(mode, "bob");
        mode.HandleRawKey(Key(ConsoleKey.Tab));
        Type(mode, "Status");
        mode.HandleRawKey(Key(ConsoleKey.Tab));
        Type(mode, "Body");
        mode.HandleRawKey(CtrlKey(ConsoleKey.S));

        // act
        await effectCts.CancelAsync();
        gate.SetResult();
        await mode.ShieldPendingSendsAsync(TimeSpan.FromSeconds(2), cancellationToken);

        // assert
        Assert.Single(store.Messages);
    }

    [Fact]
    public async Task ShieldPendingSendsAsync_Should_ReturnWithinTheBound_When_TheWriteHasNotLanded()
    {
        // arrange: the gate is never released before the bound elapses, so
        // ShieldPendingSendsAsync must still return promptly rather than
        // blocking a host-cancelled exit indefinitely.
        var cancellationToken = TestContext.Current.CancellationToken;
        var gate = new TaskCompletionSource();
        var store = new FakeMailStore { SendGate = gate };
        var mode = CreateMode(store);
        mode.OnEnter();
        mode.Handle(new TuiMessage.SelectInboxRequested());
        mode.Handle(new TuiMessage.ComposeRequested());
        Type(mode, "bob");
        mode.HandleRawKey(Key(ConsoleKey.Tab));
        Type(mode, "Status");
        mode.HandleRawKey(Key(ConsoleKey.Tab));
        Type(mode, "Body");
        mode.HandleRawKey(CtrlKey(ConsoleKey.S));

        // act
        var stopwatch = Stopwatch.StartNew();
        await mode.ShieldPendingSendsAsync(TimeSpan.FromMilliseconds(200), cancellationToken);
        stopwatch.Stop();

        // assert
        Assert.True(stopwatch.Elapsed < TimeSpan.FromSeconds(2));
        Assert.Empty(store.Messages);

        // cleanup: release the gate so the still-running write can finish.
        gate.SetResult();
    }

    [Fact]
    public async Task RunSendEffectEventsAsync_Should_EmitAnEffectCompletedEvent_ThatHandleDrainsIntoTheOutcomeToast()
    {
        // arrange: proves the wiring registered at BoardMailCommand.cs and
        // AgentTuiLauncher.cs (mailMode.RunSendEffectEventsAsync feeding the
        // hosting shell's own event channel) delivers a send completion
        // without a keypress or db-watcher tick to drive it.
        var cancellationToken = TestContext.Current.CancellationToken;
        var store = new FakeMailStore();
        var mode = CreateMode(store);
        mode.OnEnter();
        mode.Handle(new TuiMessage.SelectInboxRequested());
        var channel = Channel.CreateUnbounded<TuiEvent>();
        using var eventSourceCts = new CancellationTokenSource();
        var eventSourceTask = mode.RunSendEffectEventsAsync(channel.Writer, eventSourceCts.Token);
        mode.Handle(new TuiMessage.ComposeRequested());
        Type(mode, "bob");
        mode.HandleRawKey(Key(ConsoleKey.Tab));
        Type(mode, "Status");
        mode.HandleRawKey(Key(ConsoleKey.Tab));
        Type(mode, "Body");
        mode.HandleRawKey(CtrlKey(ConsoleKey.S));

        // act: the intermediate "Stored" notice now signals its own wake, so
        // the channel can deliver an EffectCompletedEvent for it strictly
        // ahead of the terminal completion's own event; poll channel events
        // into Handle until a non-Info (terminal) toast surfaces, rather
        // than assuming the first event read is already the terminal one.
        using var readCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        readCts.CancelAfter(TimeSpan.FromSeconds(5));
        TuiMessage.ShowToast? toast = null;

        while (toast is null)
        {
            var effectEvent = await channel.Reader.ReadAsync(readCts.Token);
            Assert.IsType<TuiEvent.EffectCompletedEvent>(effectEvent);
            var followUp = mode.Handle(new TuiMessage.EffectCompleted());

            foreach (var message in followUp)
            {
                var shown = Assert.IsType<TuiMessage.ShowToast>(message);

                if (shown.Style != ToastStyle.Info)
                {
                    toast = shown;
                }
            }
        }

        await eventSourceCts.CancelAsync();
        await eventSourceTask;

        // assert
        Assert.Contains("Notification pending", toast.Text, StringComparison.Ordinal);
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
