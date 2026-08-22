using ChilliCream.Nitro.CommandLine.Tui.Mail;

namespace ChilliCream.Nitro.CommandLine.Tests.Tui.Mail;

public sealed class MailStateTests
{
    private static readonly DateTimeOffset Now = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    private static MailState CreateState(FakeMailStore store, string actor = "alice")
        => new(actor, new MailDataLoader(store));

    [Fact]
    public async Task RefreshAsync_Should_LoadInboxForTheCurrentFilter()
    {
        // arrange
        var store = new FakeMailStore();
        store.Messages.Add(MailMessageBuilder.Create(
            "m-1", createdAt: Now, recipients: [MailMessageBuilder.ToRecipient("alice")]));
        var state = CreateState(store);

        // act
        await state.RefreshAsync(CancellationToken.None);

        // assert
        Assert.Equal(["m-1"], state.Messages.Select(m => m.Id));
    }

    [Fact]
    public async Task RefreshAsync_Should_KeepSelectedRowOnSameMessage_When_MessageStillPresentAfterReorder()
    {
        // arrange
        var store = new FakeMailStore();
        store.Messages.Add(MailMessageBuilder.Create(
            "m-1", createdAt: Now, recipients: [MailMessageBuilder.ToRecipient("alice")]));
        store.Messages.Add(MailMessageBuilder.Create(
            "m-2", createdAt: Now.AddMinutes(1), recipients: [MailMessageBuilder.ToRecipient("alice")]));
        var state = CreateState(store);
        await state.RefreshAsync(CancellationToken.None);
        state.SelectedRow = 1; // m-1, currently the older/last row

        // act: a newer message pushes m-1 to a different row on refresh
        store.Messages.Add(MailMessageBuilder.Create(
            "m-3", createdAt: Now.AddMinutes(2), recipients: [MailMessageBuilder.ToRecipient("alice")]));
        await state.RefreshAsync(CancellationToken.None);

        // assert
        Assert.Equal(["m-3", "m-2", "m-1"], state.Messages.Select(m => m.Id));
        Assert.Equal(2, state.SelectedRow);
        Assert.Equal("m-1", state.SelectedMessage?.Id);
    }

    [Fact]
    public async Task RefreshAsync_Should_ClampSelectedRow_When_SelectedMessageNoLongerPresent()
    {
        // arrange
        var store = new FakeMailStore();
        store.Messages.Add(MailMessageBuilder.Create(
            "m-1", createdAt: Now, recipients: [MailMessageBuilder.ToRecipient("alice")]));
        store.Messages.Add(MailMessageBuilder.Create(
            "m-2", createdAt: Now.AddMinutes(1), recipients: [MailMessageBuilder.ToRecipient("alice")]));
        var state = CreateState(store);
        await state.RefreshAsync(CancellationToken.None);
        state.SelectedRow = 0; // m-2, newest first

        // act: m-2 is removed from the store entirely
        store.Messages.RemoveAt(1);
        await state.RefreshAsync(CancellationToken.None);

        // assert
        Assert.Equal(["m-1"], state.Messages.Select(m => m.Id));
        Assert.Equal(0, state.SelectedRow);
    }

    [Fact]
    public async Task CycleFilterAsync_Should_AdvanceThroughFiltersAndWrap()
    {
        // arrange
        var store = new FakeMailStore();
        var state = CreateState(store);
        Assert.Equal(MailListFilter.Inbox, state.Filter);

        // act & assert: Inbox -> Unread -> Archived -> Inbox
        await state.CycleFilterAsync(1, CancellationToken.None);
        Assert.Equal(MailListFilter.Unread, state.Filter);

        await state.CycleFilterAsync(1, CancellationToken.None);
        Assert.Equal(MailListFilter.Archived, state.Filter);

        await state.CycleFilterAsync(1, CancellationToken.None);
        Assert.Equal(MailListFilter.Inbox, state.Filter);
    }

    [Fact]
    public async Task CycleFilterAsync_Should_WrapBackward_When_DeltaIsNegative()
    {
        // arrange
        var store = new FakeMailStore();
        var state = CreateState(store);

        // act
        await state.CycleFilterAsync(-1, CancellationToken.None);

        // assert
        Assert.Equal(MailListFilter.Archived, state.Filter);
    }

    [Fact]
    public async Task SelectMailboxAsync_Should_SwitchMailboxAndLoadFromItsOwnStoreMethod()
    {
        // arrange
        var store = new FakeMailStore();
        store.Messages.Add(MailMessageBuilder.Create(
            "m-1", sender: "alice", createdAt: Now, recipients: [MailMessageBuilder.ToRecipient("bob")]));
        var state = CreateState(store);
        await state.SelectMailboxAsync(MailMailbox.Inbox, CancellationToken.None);
        Assert.Empty(state.Messages); // alice is not a recipient of m-1, so the Inbox is empty

        // act
        await state.SelectMailboxAsync(MailMailbox.Sent, CancellationToken.None);

        // assert
        Assert.Equal(MailMailbox.Sent, state.Mailbox);
        Assert.Equal(["m-1"], state.Messages.Select(m => m.Id));
    }

    [Fact]
    public async Task SelectMailboxAsync_Should_ResetSelectedRowToTop()
    {
        // arrange
        var store = new FakeMailStore();
        store.Messages.Add(MailMessageBuilder.Create(
            "m-1", createdAt: Now, recipients: [MailMessageBuilder.ToRecipient("alice")]));
        store.Messages.Add(MailMessageBuilder.Create(
            "m-2", createdAt: Now.AddMinutes(1), recipients: [MailMessageBuilder.ToRecipient("alice")]));
        var state = CreateState(store);
        await state.SelectMailboxAsync(MailMailbox.Inbox, CancellationToken.None);
        state.SelectedRow = 1;

        // act: Workspace differs from the Inbox set up above (the default
        // mailbox is Workspace, so re-selecting it from the start would be
        // the no-op SelectMailboxAsync documents).
        await state.SelectMailboxAsync(MailMailbox.Workspace, CancellationToken.None);

        // assert
        Assert.Equal(0, state.SelectedRow);
    }

    [Fact]
    public async Task SelectMailboxAsync_Should_BeNoOp_When_MailboxIsAlreadyActive()
    {
        // arrange
        var store = new FakeMailStore();
        store.Messages.Add(MailMessageBuilder.Create(
            "m-1", createdAt: Now, recipients: [MailMessageBuilder.ToRecipient("alice")]));
        var state = CreateState(store);
        await state.RefreshAsync(CancellationToken.None);
        state.SelectedRow = 0;

        // act: already in the default Inbox mailbox
        await state.SelectMailboxAsync(MailMailbox.Inbox, CancellationToken.None);

        // assert
        Assert.Equal(MailMailbox.Inbox, state.Mailbox);
        Assert.Equal(0, state.SelectedRow);
    }

    [Fact]
    public async Task ShowThreadAsync_Should_LoadSelectedMessagesThread()
    {
        // arrange
        var store = new FakeMailStore();
        store.Messages.Add(MailMessageBuilder.Create(
            "m-1", threadId: "t-1", createdAt: Now, recipients: [MailMessageBuilder.ToRecipient("alice")]));
        store.Messages.Add(MailMessageBuilder.Create(
            "m-2", threadId: "t-1", createdAt: Now.AddMinutes(1)));
        var state = CreateState(store);
        await state.RefreshAsync(CancellationToken.None);

        // act
        var opened = await state.ShowThreadAsync(CancellationToken.None);

        // assert
        Assert.True(opened);
        Assert.Equal(MailViewMode.Thread, state.ViewMode);
        Assert.Equal(["m-1", "m-2"], state.ThreadMessages.Select(m => m.Id));
    }

    [Fact]
    public async Task ShowThreadAsync_Should_ReturnFalse_When_NoMessageSelected()
    {
        // arrange
        var store = new FakeMailStore();
        var state = CreateState(store);

        // act
        var opened = await state.ShowThreadAsync(CancellationToken.None);

        // assert
        Assert.False(opened);
        Assert.Equal(MailViewMode.Message, state.ViewMode);
    }

    [Fact]
    public async Task ShowMessage_Should_ClearThreadMessagesAndSwitchBack()
    {
        // arrange
        var store = new FakeMailStore();
        store.Messages.Add(MailMessageBuilder.Create(
            "m-1", threadId: "t-1", createdAt: Now, recipients: [MailMessageBuilder.ToRecipient("alice")]));
        var state = CreateState(store);
        await state.RefreshAsync(CancellationToken.None);
        await state.ShowThreadAsync(CancellationToken.None);

        // act
        state.ShowMessage();

        // assert
        Assert.Equal(MailViewMode.Message, state.ViewMode);
        Assert.Empty(state.ThreadMessages);
    }

    [Fact]
    public async Task SelectAgentFilterAsync_Should_NarrowWorkspaceMessages_ToTheGivenAgent()
    {
        // arrange
        var store = new FakeMailStore();
        store.Messages.Add(MailMessageBuilder.Create(
            "m-1", sender: "alice", createdAt: Now, recipients: [MailMessageBuilder.ToRecipient("bob")]));
        store.Messages.Add(MailMessageBuilder.Create(
            "m-2", sender: "carol", createdAt: Now.AddMinutes(1), recipients: [MailMessageBuilder.ToRecipient("bob")]));
        var state = CreateState(store);
        await state.RefreshAsync(CancellationToken.None); // already the default Workspace mailbox
        Assert.Equal(["m-2", "m-1"], state.Messages.Select(m => m.Id));

        // act
        await state.SelectAgentFilterAsync("alice", CancellationToken.None);

        // assert
        Assert.Equal("alice", state.AgentFilter);
        Assert.Equal(["m-1"], state.Messages.Select(m => m.Id));
    }

    [Fact]
    public async Task SelectAgentFilterAsync_Should_ResetSelectedRowToTop()
    {
        // arrange
        var store = new FakeMailStore();
        store.Messages.Add(MailMessageBuilder.Create(
            "m-1", sender: "alice", createdAt: Now, recipients: [MailMessageBuilder.ToRecipient("bob")]));
        store.Messages.Add(MailMessageBuilder.Create(
            "m-2", sender: "alice", createdAt: Now.AddMinutes(1), recipients: [MailMessageBuilder.ToRecipient("bob")]));
        var state = CreateState(store);
        await state.RefreshAsync(CancellationToken.None); // already the default Workspace mailbox
        state.SelectedRow = 1;

        // act
        await state.SelectAgentFilterAsync("alice", CancellationToken.None);

        // assert
        Assert.Equal(0, state.SelectedRow);
    }

    [Fact]
    public async Task SelectAgentFilterAsync_Should_RestoreTheFullWorkspaceStream_When_AgentIsNull()
    {
        // arrange
        var store = new FakeMailStore();
        store.Messages.Add(MailMessageBuilder.Create(
            "m-1", sender: "alice", createdAt: Now, recipients: [MailMessageBuilder.ToRecipient("bob")]));
        store.Messages.Add(MailMessageBuilder.Create(
            "m-2", sender: "carol", createdAt: Now.AddMinutes(1), recipients: [MailMessageBuilder.ToRecipient("bob")]));
        var state = CreateState(store);
        await state.SelectMailboxAsync(MailMailbox.Workspace, CancellationToken.None);
        await state.SelectAgentFilterAsync("alice", CancellationToken.None);
        Assert.Equal(["m-1"], state.Messages.Select(m => m.Id));

        // act
        await state.SelectAgentFilterAsync(null, CancellationToken.None);

        // assert
        Assert.Null(state.AgentFilter);
        Assert.Equal(["m-2", "m-1"], state.Messages.Select(m => m.Id));
    }

    [Fact]
    public async Task SelectMailboxAsync_Should_ClearAgentFilter_When_LeavingWorkspace()
    {
        // arrange
        var store = new FakeMailStore();
        store.Messages.Add(MailMessageBuilder.Create(
            "m-1", sender: "alice", createdAt: Now, recipients: [MailMessageBuilder.ToRecipient("bob")]));
        var state = CreateState(store);
        await state.SelectMailboxAsync(MailMailbox.Workspace, CancellationToken.None);
        await state.SelectAgentFilterAsync("alice", CancellationToken.None);
        Assert.Equal("alice", state.AgentFilter);

        // act: leave Workspace for Sent, then come back
        await state.SelectMailboxAsync(MailMailbox.Sent, CancellationToken.None);
        await state.SelectMailboxAsync(MailMailbox.Workspace, CancellationToken.None);

        // assert
        Assert.Null(state.AgentFilter);
        Assert.Equal(["m-1"], state.Messages.Select(m => m.Id)); // unfiltered again
    }

    [Fact]
    public void SelectedMessage_Should_ReturnNull_When_MessagesIsEmpty()
    {
        // arrange
        var store = new FakeMailStore();
        var state = CreateState(store);

        // act & assert
        Assert.Null(state.SelectedMessage);
    }

    [Fact]
    public void MailState_Should_DefaultToWorkspaceMailboxAndThreadsListMode()
    {
        // arrange & act
        var state = CreateState(new FakeMailStore());

        // assert: the epic's user ruling.
        Assert.Equal(MailMailbox.Workspace, state.Mailbox);
        Assert.Equal(MailListMode.Threads, state.ListMode);
    }

    [Fact]
    public async Task RefreshAsync_Should_PopulateThreadRollups_ForTheCurrentMailbox()
    {
        // arrange
        var store = new FakeMailStore();
        store.Messages.Add(MailMessageBuilder.Create(
            "m-1", threadId: "t-1", createdAt: Now, recipients: [MailMessageBuilder.ToRecipient("alice")]));
        store.Messages.Add(MailMessageBuilder.Create(
            "m-2", threadId: "t-1", createdAt: Now.AddMinutes(1), recipients: [MailMessageBuilder.ToRecipient("alice")]));
        var state = CreateState(store);

        // act
        await state.RefreshAsync(CancellationToken.None);

        // assert: one thread rollup for the two messages, one Rows entry
        // (collapsed) rather than two.
        Assert.Equal(["t-1"], state.Threads.Select(t => t.ThreadId));
        Assert.Equal(2, state.Threads[0].MessageCount);
        var row = Assert.Single(state.Rows);
        var thread = Assert.IsType<MailListRow.Thread>(row);
        Assert.False(thread.Expanded);
    }

    [Fact]
    public async Task ExpandThread_Should_InsertTheThreadsMessages_AsIndentedRowsAfterIt()
    {
        // arrange
        var store = new FakeMailStore();
        store.Messages.Add(MailMessageBuilder.Create(
            "m-1", threadId: "t-1", createdAt: Now, recipients: [MailMessageBuilder.ToRecipient("alice")]));
        store.Messages.Add(MailMessageBuilder.Create(
            "m-2", threadId: "t-1", createdAt: Now.AddMinutes(1), recipients: [MailMessageBuilder.ToRecipient("alice")]));
        var state = CreateState(store);
        await state.RefreshAsync(CancellationToken.None);

        // act
        state.ExpandThread("t-1");

        // assert: the thread row, now expanded, followed by its two
        // messages oldest first as indented children.
        Assert.Equal(3, state.Rows.Count);
        var threadRow = Assert.IsType<MailListRow.Thread>(state.Rows[0]);
        Assert.True(threadRow.Expanded);
        var first = Assert.IsType<MailListRow.MessageRow>(state.Rows[1]);
        var second = Assert.IsType<MailListRow.MessageRow>(state.Rows[2]);
        Assert.True(first.ThreadChild);
        Assert.True(second.ThreadChild);
        Assert.Equal("m-1", first.Message.Id);
        Assert.Equal("m-2", second.Message.Id);
    }

    [Fact]
    public async Task CollapseThread_Should_RemoveTheIndentedChildRows()
    {
        // arrange
        var store = new FakeMailStore();
        store.Messages.Add(MailMessageBuilder.Create(
            "m-1", threadId: "t-1", createdAt: Now, recipients: [MailMessageBuilder.ToRecipient("alice")]));
        store.Messages.Add(MailMessageBuilder.Create(
            "m-2", threadId: "t-1", createdAt: Now.AddMinutes(1), recipients: [MailMessageBuilder.ToRecipient("alice")]));
        var state = CreateState(store);
        await state.RefreshAsync(CancellationToken.None);
        state.ExpandThread("t-1");

        // act
        state.CollapseThread("t-1");

        // assert
        var row = Assert.Single(state.Rows);
        var thread = Assert.IsType<MailListRow.Thread>(row);
        Assert.False(thread.Expanded);
    }

    [Fact]
    public async Task ToggleThreadFold_Should_AlternateBetweenExpandedAndCollapsed()
    {
        // arrange
        var store = new FakeMailStore();
        store.Messages.Add(MailMessageBuilder.Create(
            "m-1", threadId: "t-1", createdAt: Now, recipients: [MailMessageBuilder.ToRecipient("alice")]));
        var state = CreateState(store);
        await state.RefreshAsync(CancellationToken.None);

        // act & assert
        state.ToggleThreadFold("t-1");
        Assert.Equal(2, state.Rows.Count); // thread row + its one message

        state.ToggleThreadFold("t-1");
        Assert.Single(state.Rows);
    }

    [Fact]
    public async Task ExpandAllThreads_And_CollapseAllThreads_Should_ActOnEveryThread()
    {
        // arrange
        var store = new FakeMailStore();
        store.Messages.Add(MailMessageBuilder.Create(
            "m-1", threadId: "t-1", createdAt: Now, recipients: [MailMessageBuilder.ToRecipient("alice")]));
        store.Messages.Add(MailMessageBuilder.Create(
            "m-2", threadId: "t-2", createdAt: Now.AddMinutes(1), recipients: [MailMessageBuilder.ToRecipient("alice")]));
        var state = CreateState(store);
        await state.RefreshAsync(CancellationToken.None);

        // act
        state.ExpandAllThreads();

        // assert: two thread rows, each with its one message.
        Assert.Equal(4, state.Rows.Count);

        // act
        state.CollapseAllThreads();

        // assert
        Assert.Equal(2, state.Rows.Count);
        Assert.All(state.Rows, row => Assert.False(((MailListRow.Thread)row).Expanded));
    }

    [Fact]
    public async Task IsThreadUnreadToMe_Should_UseTheRollupsUnreadCount_OutsideWorkspace()
    {
        // arrange
        var store = new FakeMailStore();
        store.Messages.Add(MailMessageBuilder.Create(
            "m-1", threadId: "t-1", createdAt: Now, recipients: [MailMessageBuilder.ToRecipient("alice")]));
        var state = CreateState(store);
        await state.SelectMailboxAsync(MailMailbox.Inbox, CancellationToken.None);

        // act & assert
        Assert.True(state.IsThreadUnreadToMe(state.Threads[0]));

        await store.MarkReadAsync(["m-1"], "alice", CancellationToken.None);
        await state.RefreshAsync(CancellationToken.None);
        Assert.False(state.IsThreadUnreadToMe(state.Threads[0]));
    }

    [Fact]
    public async Task IsThreadUnreadToMe_Should_BeTrue_InWorkspace_When_UnreadAndAddressedToTheActor()
    {
        // arrange: already the default Workspace mailbox.
        var store = new FakeMailStore();
        store.Messages.Add(MailMessageBuilder.Create(
            "m-1", sender: "bob", threadId: "t-1", createdAt: Now, recipients: [MailMessageBuilder.ToRecipient("alice")]));
        var state = CreateState(store);

        // act
        await state.RefreshAsync(CancellationToken.None);

        // assert
        Assert.True(state.IsThreadUnreadToMe(state.Threads[0]));
    }

    [Fact]
    public async Task IsThreadUnreadToMe_Should_BeFalse_InWorkspace_ForAnUnreadThreadBetweenTwoOtherAgents()
    {
        // arrange: bob and carol's thread never addresses alice - it must
        // never render as unread-to-me for her, even though it is genuinely
        // unread for carol (epic wi3 convention 8: never another agent's
        // read state).
        var store = new FakeMailStore();
        store.Messages.Add(MailMessageBuilder.Create(
            "m-1", sender: "bob", threadId: "t-1", createdAt: Now, recipients: [MailMessageBuilder.ToRecipient("carol")]));
        var state = CreateState(store);

        // act
        await state.RefreshAsync(CancellationToken.None);

        // assert
        Assert.False(state.IsThreadUnreadToMe(state.Threads[0]));
    }
}
