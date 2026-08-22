using ChilliCream.Nitro.CommandLine.Services.Workspace;
using ChilliCream.Nitro.CommandLine.Tests.Tui.Agents;
using ChilliCream.Nitro.CommandLine.Tui.Input;
using ChilliCream.Nitro.CommandLine.Tui.Mail;
using ChilliCream.Nitro.CommandLine.Tui.Theming;
using Microsoft.Extensions.Time.Testing;
using Spectre.Console;
using Spectre.Console.Testing;
using CursorDirection = ChilliCream.Nitro.CommandLine.Tui.Input.CursorDirection;

namespace ChilliCream.Nitro.CommandLine.Tests.Tui.Mail;

public sealed class MailModeTests
{
    private static readonly DateTimeOffset Now = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    private static MailMode CreateMode(FakeMailStore store, string actor = "alice", FakeAgentRegistry? agentRegistry = null)
        => new(store, actor, agentRegistry ?? new FakeAgentRegistry(), new FakeTimeProvider(Now));

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

    /// <summary>
    /// Asserts the ANSI escape sequence for <paramref name="token"/>'s style
    /// appears in <paramref name="output"/>. A plain <see cref="TestConsole"/>
    /// strips markup entirely, so a wrong or missing token name would still
    /// leave every plain-text <c>Contains</c> assertion elsewhere green;
    /// <paramref name="output"/> must come from a console built with
    /// <c>.Colors(ColorSystem.TrueColor)</c> and <c>.EmitAnsiSequences()</c>.
    /// </summary>
    private static void AssertAnsiStyleApplied(string output, string token)
    {
        var style = ThemeTokens.GetStyle(token);
        var styleConsole = new TestConsole().Colors(ColorSystem.TrueColor).EmitAnsiSequences().Width(1).Height(1);
        styleConsole.Write(new Markup("x", style));
        var ansiPrefix = styleConsole.Output[..styleConsole.Output.IndexOf('x')];
        Assert.Contains(ansiPrefix, output);
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

        // assert
        Assert.Contains("Inbox (1)", console.Output);
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
    public void OnEnter_Should_LoadTheActorsInbox()
    {
        // arrange
        var store = new FakeMailStore();
        AddMessage(store, "m-1", Now);
        AddMessage(store, "m-2", Now, actor: "bob");
        var mode = CreateMode(store, actor: "alice");

        // act
        mode.OnEnter();

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

        // act
        var followUp = mode.Handle(new TuiMessage.ComposeRequested());

        // assert
        Assert.Empty(followUp);
        Assert.True(mode.IsInputCapturing);
    }

    [Fact]
    public void ComposeForm_Submit_Should_SendMessage_And_ShowSuccessToast()
    {
        // arrange
        var store = new FakeMailStore();
        var mode = CreateMode(store);
        mode.OnEnter();
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
        Assert.Equal(ToastStyle.Success, Assert.IsType<TuiMessage.ShowToast>(toast).Style);
        Assert.False(mode.IsInputCapturing);
        Assert.Contains(store.Messages, m => m.Sender == "alice" && m.Subject == "Status");
    }

    [Fact]
    public void ComposeForm_Submit_Should_ShowErrorToast_And_WriteNothing_When_StoreRejectsTheWrite()
    {
        // arrange: FakeMailStore.SendMessageAsync throws when there are no
        // recipients; the form's own validator normally prevents an empty
        // To field, so this exercises the write-failure toast path the same
        // way a store-level rejection (for example an unknown recipient
        // against the real store) would surface.
        var store = new FakeMailStore();
        var mode = CreateMode(store);
        mode.OnEnter();
        mode.Handle(new TuiMessage.ComposeRequested());
        Type(mode, ",");
        mode.HandleRawKey(Key(ConsoleKey.Tab));
        Type(mode, "Status");
        mode.HandleRawKey(Key(ConsoleKey.Tab));
        Type(mode, "Body");

        // act
        var followUp = mode.HandleRawKey(CtrlKey(ConsoleKey.S));

        // assert
        var toast = Assert.Single(followUp);
        Assert.Equal(ToastStyle.Error, Assert.IsType<TuiMessage.ShowToast>(toast).Style);
        Assert.True(mode.IsInputCapturing);
        Assert.Empty(store.Messages);
    }

    [Fact]
    public void ComposeForm_Cancel_Should_CloseImmediately_When_NotDirty()
    {
        // arrange
        var store = new FakeMailStore();
        var mode = CreateMode(store);
        mode.OnEnter();
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
    public void ReplyForm_Submit_Should_SendReply_And_ShowSuccessToast()
    {
        // arrange
        var store = new FakeMailStore();
        AddMessage(store, "m-1", Now);
        var mode = CreateMode(store);
        mode.OnEnter();
        mode.Handle(new TuiMessage.ReplyRequested());
        Type(mode, "On it.");

        // act
        var followUp = mode.HandleRawKey(CtrlKey(ConsoleKey.S));

        // assert
        var toast = Assert.Single(followUp);
        Assert.Equal(ToastStyle.Success, Assert.IsType<TuiMessage.ShowToast>(toast).Style);
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

        // act & assert: Inbox is the default mailbox on OnEnter, so u/a/r/c
        // stay live and their footer hints stay visible.
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
    public void Render_Should_ApplyAnsiStyling_ToRowGlyphPeerAndAgeTokens_When_MessageReceived()
    {
        // arrange: alice receives two messages, each as its sole recipient,
        // so both rows carry the direct glyph, the plain peer token (no
        // "To " prefix), and the age token. A second message so at least
        // one row is unselected: the default-selected row 0 merges its
        // token color with selection.highlight's background into one ANSI
        // sequence, which would not match a token's style checked in
        // isolation.
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
        AssertAnsiStyleApplied(console.Output, "mail.row.peer");
        AssertAnsiStyleApplied(console.Output, "mail.row.age");
    }

    [Fact]
    public void Render_Should_ApplyAnsiStyling_ToToPrefixToken_When_ActorSentTheMessage()
    {
        // arrange: two sent messages so at least one row is unselected; see
        // Render_Should_ApplyAnsiStyling_ToRowGlyphPeerAndAgeTokens_When_MessageReceived
        // for why the selected row's merged style would not match.
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

        // assert: mail.row.peer.to-prefix's escape sequence can already be
        // present elsewhere in the frame regardless of whether the "To "
        // label itself carries the style, so pin the assertion to the
        // escape sequence appearing immediately before the "To " text.
        var style = ThemeTokens.GetStyle("mail.row.peer.to-prefix");
        var styleConsole = new TestConsole().Colors(ColorSystem.TrueColor).EmitAnsiSequences().Width(1).Height(1);
        styleConsole.Write(new Markup("x", style));
        var ansiPrefix = styleConsole.Output[..styleConsole.Output.IndexOf('x')];
        Assert.Contains(ansiPrefix + "To ", console.Output);
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
}
