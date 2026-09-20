using System.Diagnostics;
using System.Threading.Channels;
using ChilliCream.Nitro.CommandLine.Services.Workspace;
using ChilliCream.Nitro.CommandLine.Tests.Tui.Agents;
using ChilliCream.Nitro.CommandLine.Tui.Input;
using ChilliCream.Nitro.CommandLine.Tui.Mail;
using ChilliCream.Nitro.CommandLine.Tui.Runtime;
using ChilliCream.Nitro.CommandLine.Tui.Shell;
using ChilliCream.Nitro.CommandLine.Tui.Theming;
using Microsoft.Extensions.Time.Testing;
using Spectre.Console;
using Spectre.Console.Testing;
using CursorDirection = ChilliCream.Nitro.CommandLine.Tui.Input.CursorDirection;
using static ChilliCream.Nitro.CommandLine.Tests.Tui.AnsiAssertions;

namespace ChilliCream.Nitro.CommandLine.Tests.Tui.Mail;

public sealed class MailModeTests
{
    private static readonly DateTimeOffset s_now = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    private static MailMode CreateMode(
        FakeMailStore store,
        string? actor = "alice",
        FakeAgentRegistry? agentRegistry = null)
        => new(
            store,
            actor,
            agentRegistry ?? new FakeAgentRegistry(),
            new FakeTimeProvider(s_now));

    private static AgentRecord Agent(string name) => new()
    {
        Name = name,
        Role = "",
        Client = "",
        Implicit = false,
        RegisteredAt = s_now,
        LastSeenAt = s_now
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
    public void MutatingGesture_Should_BeRefused_When_TheBoardHasNoIdentity()
    {
        // arrange
        var store = new FakeMailStore();
        AddMessage(store, "m-1", s_now);
        var mode = CreateMode(store, actor: null);
        mode.OnEnter();

        // act
        var followUp = mode.Handle(new TuiMessage.ToggleReadRequested());

        // assert
        var shown = Assert.IsType<TuiMessage.ShowToast>(Assert.Single(followUp));
        Assert.Equal(BoardIdentity.NoIdentityMessage, shown.Text);
        Assert.Equal(ToastStyle.Warn, shown.Style);
    }

    [Fact]
    public void JumpToInbox_Should_BeRefused_And_StayOnWorkspace_When_TheBoardHasNoIdentity()
    {
        // arrange
        var store = new FakeMailStore();
        AddMessage(store, "m-1", s_now);
        var mode = CreateMode(store, actor: null);
        mode.OnEnter();

        // act
        var followUp = mode.Handle(new TuiMessage.SelectInboxRequested());

        // assert
        var shown = Assert.IsType<TuiMessage.ShowToast>(Assert.Single(followUp));
        Assert.Equal(BoardIdentity.NoIdentityMessage, shown.Text);
        Assert.Equal(MailMailbox.Workspace, mode.State.Mailbox);
    }

    [Fact]
    public void Workspace_Should_ListEveryAgentsMail_When_TheBoardHasNoIdentity()
    {
        // arrange
        var store = new FakeMailStore();
        AddMessage(store, "m-1", s_now, actor: "alice");
        AddMessage(store, "m-2", s_now.AddMinutes(1), actor: "bob");

        // act
        var mode = CreateMode(store, actor: null);
        mode.OnEnter();

        // assert
        Assert.Equal(MailMailbox.Workspace, mode.State.Mailbox);
        Assert.Equal(2, mode.State.Messages.Count);
        Assert.Equal(0, mode.UnreadCount);
    }

    [Fact]
    public void MoveSelection_Should_ClampAtLastRow_When_MovingDownPastEnd()
    {
        // arrange
        var store = new FakeMailStore();
        AddMessage(store, "m-1", s_now);
        AddMessage(store, "m-2", s_now.AddMinutes(1));
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
        AddMessage(store, "m-1", s_now);
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
        AddMessage(store, "m-1", s_now);
        AddMessage(store, "m-2", s_now.AddMinutes(1));
        AddMessage(store, "m-3", s_now.AddMinutes(2));
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
        AddMessage(store, "m-1", s_now);
        AddMessage(store, "m-2", s_now.AddMinutes(1));
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
        AddMessage(store, "m-1", s_now);
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
        AddMessage(store, "m-1", s_now);
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
        AddMessage(store, "m-1", s_now);
        var mode = CreateMode(store);
        mode.OnEnter();
        // flip to Flat mode, so a message row (not a thread row) is selected
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
        AddMessage(store, "m-1", s_now);
        var mode = CreateMode(store);
        mode.OnEnter();
        // flip to Flat mode first, so the arrange toggle is the one that enters Thread view
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
        AddMessage(store, "m-1", s_now);
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
        AddMessage(store, "m-1", s_now);
        var mode = CreateMode(store);
        mode.OnEnter();
        var console = new TestConsole().Width(100).Height(20);

        // act
        console.Write(mode.Render(100, 20));

        // assert
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
            "m-1", sender: "bob", createdAt: s_now, recipients: [MailMessageBuilder.ToRecipient("alice")]));
        var registry = new FakeAgentRegistry();
        registry.Agents.Add(Agent("bob") with { Client = "codex" });
        var mode = CreateMode(store, agentRegistry: registry);
        mode.OnEnter();
        mode.State.ShowMessage(); // Threads mode defaults a single-message thread's row to Thread view
        var console = new TestConsole().Width(100).Height(20);

        // act
        console.Write(mode.Render(100, 20));

        // assert
        Assert.Contains("From: bob (codex)", console.Output);
    }

    [Fact]
    public void OnEnter_Should_NotThrow_When_RegistryHasCaseVariantDuplicateNames()
    {
        // arrange
        // a registry can hold "bob" and "Bob" side by side; the case-insensitive lookup must not throw
        var store = new FakeMailStore();
        store.Messages.Add(MailMessageBuilder.Create(
            "m-1", sender: "bob", createdAt: s_now, recipients: [MailMessageBuilder.ToRecipient("alice")]));
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
            "m-1", sender: "bob", createdAt: s_now, recipients: [MailMessageBuilder.ToRecipient("alice")]));
        var mode = CreateMode(store, agentRegistry: new FakeAgentRegistry());
        mode.OnEnter();
        mode.State.ShowMessage(); // Threads mode defaults a single-message thread's row to Thread view
        var console = new TestConsole().Width(100).Height(20);

        // act
        console.Write(mode.Render(100, 20));

        // assert
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
        // arrange
        // Workspace shows every agent's mail, unlike Inbox which would show only alice's
        var store = new FakeMailStore();
        AddMessage(store, "m-1", s_now);
        AddMessage(store, "m-2", s_now, actor: "bob");
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
        AddMessage(store, "m-1", s_now);
        AddMessage(store, "m-2", s_now, actor: "bob");
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
        AddMessage(store, "m-1", s_now);
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
        AddMessage(store, "m-1", s_now);
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
        AddMessage(store, "m-1", s_now);
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
        AddMessage(store, "m-1", s_now);
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
        AddMessage(store, "m-1", s_now);
        var mode = CreateMode(store);
        mode.OnEnter();
        mode.Handle(new TuiMessage.SelectInboxRequested()); // Workspace (the default) is read-only
        mode.Handle(new TuiMessage.ArchiveRequested());

        // act
        // Enter confirms from the dialog's initially focused, empty reason field
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
        AddMessage(store, "m-1", s_now);
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
        // arrange
        // the submit key's synchronous return is only ever the immediate "Sending" toast, never the outcome
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

    [Fact]
    public async Task ComposeForm_Submit_Should_ShowErrorToast_And_WriteNothing_When_StoreRejectsTheWrite()
    {
        // arrange
        // A comma passes the nonempty To-field check but produces no recipient names.
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
        Assert.True(mode.IsInputCapturing);
        var console = new TestConsole().Width(100).Height(20);
        console.Write(mode.Render(100, 20));
        Assert.Contains("Status", console.Output);
    }

    [Fact]
    public async Task ReplyForm_Submit_Should_ShowErrorToast_And_ReopenTheForm_When_StoreRejectsTheWrite()
    {
        // arrange
        // the replied-to message is removed from the store between opening the form and submitting it
        var cancellationToken = TestContext.Current.CancellationToken;
        var store = new FakeMailStore();
        store.Messages.Add(MailMessageBuilder.Create(
            "m-1", sender: "bob", createdAt: s_now, recipients: [MailMessageBuilder.ToRecipient("alice")]));
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

        // assert
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
        // arrange
        // the second compose is submitted while the first send's store write is still gated open
        var cancellationToken = TestContext.Current.CancellationToken;
        var store = new FakeMailStore { SendGate = new TaskCompletionSource() };
        var mode = CreateMode(store);
        mode.OnEnter();
        mode.Handle(new TuiMessage.SelectInboxRequested());
        mode.Handle(new TuiMessage.ComposeRequested());
        Type(mode, "bob");
        mode.HandleRawKey(Key(ConsoleKey.Tab));
        Type(mode, "First");
        mode.HandleRawKey(Key(ConsoleKey.Tab));
        Type(mode, "Body");
        mode.HandleRawKey(CtrlKey(ConsoleKey.S));
        await WaitUntilAsync(() => store.SendGateEntered, cancellationToken);

        mode.Handle(new TuiMessage.ComposeRequested());
        Type(mode, "carol");
        mode.HandleRawKey(Key(ConsoleKey.Tab));
        Type(mode, "Second");
        mode.HandleRawKey(Key(ConsoleKey.Tab));
        Type(mode, "Body");

        // act
        var followUp = mode.HandleRawKey(CtrlKey(ConsoleKey.S));

        // assert
        var toast = Assert.Single(followUp);
        Assert.Equal(ToastStyle.Warn, Assert.IsType<TuiMessage.ShowToast>(toast).Style);
        Assert.True(mode.IsInputCapturing);
        Assert.Empty(store.Messages); // the first write is still held open

        // Release the first write and wait for completion.
        store.SendGate!.SetResult();
        await WaitForOutcomeToastAsync(mode, cancellationToken);
        Assert.Single(store.Messages);
    }

    [Fact]
    public async Task CreateQuitGate_Should_ReportPendingCount_While_ASendIsStillInFlight_And_ClearAfterwards()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var store = new FakeMailStore { SendGate = new TaskCompletionSource() };
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
        await WaitUntilAsync(() => store.SendGateEntered, cancellationToken);

        var gate = mode.CreateQuitGate();

        // act
        // bounded drain while the store write is still gated open
        var reportWhilePending = await gate(TimeSpan.FromMilliseconds(50), cancellationToken);

        // assert
        Assert.Equal(1, reportWhilePending.PendingCount);
        Assert.Equal(0, reportWhilePending.OutcomeUnknownCount);
        Assert.True(reportWhilePending.HasUnresolvedWork);

        // cleanup
        // release the gate and resume accepting, then confirm the effect drains cleanly
        store.SendGate!.SetResult();
        await WaitForOutcomeToastAsync(mode, cancellationToken);
        mode.ResumeSendAcceptance();
    }

    [Fact]
    public async Task CreateQuitGate_Should_LeaveTheStashedToastForTheNextHandle_AfterACancelledQuit()
    {
        // arrange
        // a cancelled second confirmation resuming the live TUI must still show the send's toast once it lands
        var cancellationToken = TestContext.Current.CancellationToken;
        var store = new FakeMailStore { SendGate = new TaskCompletionSource() };
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
        await WaitUntilAsync(() => store.SendGateEntered, cancellationToken);
        var report = await mode.CreateQuitGate()(TimeSpan.FromMilliseconds(50), cancellationToken);
        Assert.True(report.HasUnresolvedWork); // the second confirmation the shell would show

        // act
        // the user declines the second confirmation, and the held send then lands
        mode.ResumeSendAcceptance();
        store.SendGate!.SetResult();

        // assert
        var toast = await WaitForOutcomeToastAsync(mode, cancellationToken);
        Assert.Equal(ToastStyle.Success, toast.Style);
        Assert.Contains("Sent", toast.Text, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ComposeForm_Submit_Should_ShowOutcomeUnknownToast_Without_AssertingNotStored_When_TheSendEffectFaults()
    {
        // arrange
        // a non-ExitException from the store write reaches the send effect as Faulted, an unknown outcome
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

    /// <summary>
    /// Refreshes <see cref="MailMode"/> until a non-<see cref="ToastStyle.Info"/> outcome toast
    /// is returned. Cancels the wait after five seconds or when the supplied token is cancelled.
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
        // arrange
        // the store commit is gated open when the effect token cancels; the write itself is never cancelled
        var cancellationToken = TestContext.Current.CancellationToken;
        var gate = new TaskCompletionSource();
        var store = new FakeMailStore { SendGate = gate };
        using var effectCts = new CancellationTokenSource();
        var mode = new MailMode(
            store,
            "alice",
            new FakeAgentRegistry(),
            new FakeTimeProvider(s_now),
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
        // arrange
        // the gate is never released before the bound elapses
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

        // cleanup
        // release the gate so the still-running write can finish
        gate.SetResult();
    }

    [Fact]
    public async Task RunSendEffectEventsAsync_Should_EmitAnEffectCompletedEvent_ThatHandleDrainsIntoTheOutcomeToast()
    {
        // arrange
        // Run the send-effect event source and submit a message.
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

        // act
        // Handle completion events until a non-Info toast appears.
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
        Assert.Equal(ToastStyle.Success, toast.Style);
        Assert.Contains("Sent", toast.Text, StringComparison.Ordinal);
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

        // assert
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

        // assert
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
            "m-1", sender: "alice", createdAt: s_now, recipients: [MailMessageBuilder.ToRecipient("bob")]));
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
            "m-1", sender: "alice", createdAt: s_now, recipients: [MailMessageBuilder.ToRecipient("bob")]));
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
        // arrange
        // the filter name must stay visible in Threads mode too, not just Flat
        var store = new FakeMailStore();
        AddMessage(store, "m-1", s_now);
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
        // arrange
        // m-1's thread is unread for alice; m-2's thread is already read for her
        var store = new FakeMailStore();
        store.Messages.Add(MailMessageBuilder.Create(
            "m-1", threadId: "t-1", createdAt: s_now, recipients: [MailMessageBuilder.ToRecipient("alice")]));
        store.Messages.Add(MailMessageBuilder.Create(
            "m-2", threadId: "t-2", createdAt: s_now.AddMinutes(1), recipients: [MailMessageBuilder.ToRecipient("alice")]));
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
        AddMessage(store, "m-1", s_now);
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
        // arrange
        // alice sent m-1 to bob, so alice has no recipient row on it and the store would reject the write
        var store = new FakeMailStore();
        store.Messages.Add(MailMessageBuilder.Create(
            "m-1", sender: "alice", createdAt: s_now, recipients: [MailMessageBuilder.ToRecipient("bob")]));
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
            "m-1", sender: "alice", createdAt: s_now, recipients: [MailMessageBuilder.ToRecipient("bob")]));
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
        // arrange
        // alice addressed the message to herself, so a recipient row exists and the write goes through
        var store = new FakeMailStore();
        store.Messages.Add(MailMessageBuilder.Create(
            "m-1", sender: "alice", createdAt: s_now, recipients: [MailMessageBuilder.ToRecipient("alice")]));
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
            "m-1", sender: "alice", createdAt: s_now, recipients: [MailMessageBuilder.ToRecipient("bob")]));
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
    public async Task ReplyForm_Submit_Should_SendReply_And_ShowSuccessToast()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var store = new FakeMailStore();
        AddMessage(store, "m-1", s_now); // sender defaults to "sender", to "alice"
        var mode = CreateMode(store);
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
        // arrange
        // alice is a genuine recipient of m-1, but Workspace refuses the write regardless of recipient status
        var store = new FakeMailStore();
        AddMessage(store, "m-1", s_now);
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
        // arrange
        // alice is an unread recipient of m-1, but Workspace must stay inert regardless
        var store = new FakeMailStore();
        AddMessage(store, "m-1", s_now);
        var mode = CreateMode(store);
        mode.OnEnter();
        mode.Handle(new TuiMessage.SelectWorkspaceMailRequested());

        // act
        var followUp = mode.Handle(new TuiMessage.OpenSelected());

        // assert
        Assert.Empty(followUp);
        Assert.Null(MailRecipientView.FindRecipient(store.Messages[0], "alice")!.ReadAt);

        // act
        // flip back to List, then Right again, which must stay inert too
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
        AddMessage(store, "m-1", s_now);
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
        // arrange
        // alice sent m-1, but Workspace refuses the reply regardless of the actor's own participation
        var store = new FakeMailStore();
        store.Messages.Add(MailMessageBuilder.Create(
            "m-1", sender: "alice", createdAt: s_now, recipients: [MailMessageBuilder.ToRecipient("bob")]));
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
        // arrange
        // the header names the mailbox, and the list pane's border token is distinct from other mailboxes
        var store = new FakeMailStore();
        AddMessage(store, "m-1", s_now);
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

        // assert
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

        // act & assert
        // outside Workspace, u/a/r/c stay live and their footer hints stay visible
        Assert.Empty(mode.SuppressedGlobalHints);
    }

    [Fact]
    public void SuppressedGlobalHints_Should_HideToggleReadArchiveReplyAndCompose_When_MailboxIsWorkspace()
    {
        // arrange
        // the same four gestures refused with a toast in Workspace must also have their footer hints hidden
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

        // assert
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
            "m-1", sender: "bob", createdAt: s_now, recipients: [MailMessageBuilder.ToRecipient("carol")]));
        store.Messages.Add(MailMessageBuilder.Create(
            "m-2", sender: "carol", createdAt: s_now.AddMinutes(1), recipients: [MailMessageBuilder.ToRecipient("bob")]));
        store.Messages.Add(MailMessageBuilder.Create(
            "m-3", sender: "carol", createdAt: s_now.AddMinutes(2), recipients: [MailMessageBuilder.ToRecipient("dave")]));
        var registry = new FakeAgentRegistry();
        registry.Agents.Add(Agent("bob"));
        var mode = CreateMode(store, agentRegistry: registry);
        mode.OnEnter();
        mode.Handle(new TuiMessage.SelectWorkspaceMailRequested());
        mode.Handle(new TuiMessage.AgentFilterPickerRequested());

        // act
        // move down from "All agents" to "bob", then apply
        mode.HandleRawKey(Key(ConsoleKey.DownArrow));
        var followUp = mode.HandleRawKey(Key(ConsoleKey.Enter));

        // assert
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
            "m-1", sender: "bob", createdAt: s_now, recipients: [MailMessageBuilder.ToRecipient("carol")]));
        store.Messages.Add(MailMessageBuilder.Create(
            "m-2", sender: "carol", createdAt: s_now.AddMinutes(1), recipients: [MailMessageBuilder.ToRecipient("dave")]));
        var registry = new FakeAgentRegistry();
        registry.Agents.Add(Agent("bob"));
        var mode = CreateMode(store, agentRegistry: registry);
        mode.OnEnter();
        mode.Handle(new TuiMessage.SelectWorkspaceMailRequested());
        mode.Handle(new TuiMessage.AgentFilterPickerRequested());
        mode.HandleRawKey(Key(ConsoleKey.DownArrow));
        mode.HandleRawKey(Key(ConsoleKey.Enter));
        Assert.Equal(["m-1"], mode.State.Messages.Select(m => m.Id));

        // act
        // reopen the picker (pre-selected on "bob") and move back up to "All agents", then apply
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
            "m-1", sender: "bob", createdAt: s_now, recipients: [MailMessageBuilder.ToRecipient("carol")]));
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
            "m-1", sender: "bob", createdAt: s_now, recipients: [MailMessageBuilder.ToRecipient("carol")]));
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
            "m-1", sender: "bob", createdAt: s_now, recipients: [MailMessageBuilder.ToRecipient("alice")]));
        var registry = new FakeAgentRegistry();
        registry.Agents.Add(Agent("bob"));
        var mode = CreateMode(store, agentRegistry: registry);
        mode.OnEnter();
        mode.Handle(new TuiMessage.SelectWorkspaceMailRequested());
        mode.Handle(new TuiMessage.AgentFilterPickerRequested());
        mode.HandleRawKey(Key(ConsoleKey.DownArrow));
        mode.HandleRawKey(Key(ConsoleKey.Enter));
        Assert.Equal("bob", mode.State.AgentFilter);

        // act
        // leave Workspace for Inbox, then come back
        mode.Handle(new TuiMessage.SelectInboxRequested());
        mode.Handle(new TuiMessage.SelectWorkspaceMailRequested());

        // assert
        Assert.Null(mode.State.AgentFilter);
    }

    [Fact]
    public void Render_Should_ApplyAnsiStyling_ToRowGlyphFromToAndAgeTokens_When_MessageReceived()
    {
        // arrange
        // two messages so at least one row is unselected; a selected row merges its color with the highlight
        var store = new FakeMailStore();
        AddMessage(store, "m-1", s_now);
        AddMessage(store, "m-2", s_now.AddMinutes(1));
        var mode = CreateMode(store);
        mode.OnEnter();
        var console = new TestConsole().Colors(ColorSystem.TrueColor).EmitAnsiSequences().Width(100).Height(20);

        // act
        console.Write(mode.Render(100, 20));

        // assert
        AssertAnsiStyleApplied(console.Output, "mail.row.glyph.direct");
        AssertAnsiStyleApplied(console.Output, "mail.row.from");
        AssertAnsiStyleApplied(console.Output, "mail.row.to");
        AssertAnsiStyleApplied(console.Output, "mail.row.age");
    }

    [Fact]
    public void Render_Should_ApplyFromMeToken_NotThePlainFromToken_When_ActorSentTheMessage()
    {
        // arrange
        // two sent messages so at least one row is unselected; alice's own name in From gets the from-me token
        var store = new FakeMailStore();
        store.Messages.Add(MailMessageBuilder.Create(
            "m-1", sender: "alice", createdAt: s_now, recipients: [MailMessageBuilder.ToRecipient("bob")]));
        store.Messages.Add(MailMessageBuilder.Create(
            "m-2", sender: "alice", createdAt: s_now.AddMinutes(1), recipients: [MailMessageBuilder.ToRecipient("bob")]));
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
        // arrange
        // Spectre paints a panel's header with its BorderStyle, so the header sits inside the border's own styled run
        var store = new FakeMailStore();
        AddMessage(store, "m-1", s_now);
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
        AddMessage(store, "m-1", s_now);
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
        AddMessage(store, "m-1", s_now);
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
        AddMessage(store, "m-1", s_now);
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
        // arrange
        // a two-message thread so expanding is observable as an extra row
        var store = new FakeMailStore();
        store.Messages.Add(MailMessageBuilder.Create(
            "m-1", threadId: "t-1", createdAt: s_now, recipients: [MailMessageBuilder.ToRecipient("alice")]));
        store.Messages.Add(MailMessageBuilder.Create(
            "m-2", threadId: "t-1", createdAt: s_now.AddMinutes(1), recipients: [MailMessageBuilder.ToRecipient("alice")]));
        var mode = CreateMode(store);
        mode.OnEnter();
        Assert.Single(mode.State.Rows); // one collapsed thread row

        // act
        // z then o (open/expand)
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
            "m-1", threadId: "t-1", createdAt: s_now, recipients: [MailMessageBuilder.ToRecipient("alice")]));
        store.Messages.Add(MailMessageBuilder.Create(
            "m-2", threadId: "t-1", createdAt: s_now.AddMinutes(1), recipients: [MailMessageBuilder.ToRecipient("alice")]));
        var mode = CreateMode(store);
        mode.OnEnter();
        mode.State.ExpandThread("t-1");
        Assert.Equal(3, mode.State.Rows.Count);

        // act
        // z then c (close/collapse)
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
            "m-1", threadId: "t-1", createdAt: s_now, recipients: [MailMessageBuilder.ToRecipient("alice")]));
        store.Messages.Add(MailMessageBuilder.Create(
            "m-2", threadId: "t-1", createdAt: s_now.AddMinutes(1), recipients: [MailMessageBuilder.ToRecipient("alice")]));
        var mode = CreateMode(store);
        mode.OnEnter();

        // act
        // z then a (toggle) twice
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
        AddMessage(store, "m-1", s_now);
        AddMessage(store, "m-2", s_now.AddMinutes(1));
        var mode = CreateMode(store);
        mode.OnEnter();
        Assert.Equal(2, mode.State.Rows.Count); // two collapsed singleton threads

        // act
        // z then Shift+R (unfold all)
        mode.Handle(new TuiMessage.FoldPrefixRequested());
        mode.HandleRawKey(new ConsoleKeyInfo('R', ConsoleKey.R, shift: true, alt: false, control: false));

        // assert
        Assert.Equal(4, mode.State.Rows.Count);
        var threadRows = mode.State.Rows.OfType<MailListRow.Thread>().ToList();
        Assert.Equal(2, threadRows.Count);
        Assert.All(threadRows, row => Assert.True(row.Expanded));

        // act
        // z then Shift+M (fold all)
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
        AddMessage(store, "m-1", s_now);
        var mode = CreateMode(store);
        mode.OnEnter();

        // act
        // z then Escape, an unrecognized second key that shows no error toast
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
        // arrange
        // m-1 is unread and addressed to alice; m-2 is unread between two other agents and never addresses her
        var store = new FakeMailStore();
        store.Messages.Add(MailMessageBuilder.Create(
            "m-1", sender: "bob", createdAt: s_now, recipients: [MailMessageBuilder.ToRecipient("alice")]));
        store.Messages.Add(MailMessageBuilder.Create(
            "m-2", sender: "carol", createdAt: s_now.AddMinutes(1), recipients: [MailMessageBuilder.ToRecipient("dave")]));
        var mode = CreateMode(store);
        mode.OnEnter();
        Assert.Equal(MailMailbox.Workspace, mode.State.Mailbox);
        var console = new TestConsole().Width(100).Height(20);

        // act
        console.Write(mode.Render(100, 20));

        // assert
        var markerCount = console.Output.Split('\u25cf').Length - 1;
        Assert.Equal(1, markerCount);
    }
}
