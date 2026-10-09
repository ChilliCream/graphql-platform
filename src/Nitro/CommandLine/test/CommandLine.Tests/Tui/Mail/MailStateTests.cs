using ChilliCream.Nitro.CommandLine.Tui.Mail;

namespace ChilliCream.Nitro.CommandLine.Tests.Tui.Mail;

public sealed class MailStateTests
{
    private static readonly DateTimeOffset s_now = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    private static MailState CreateState(FakeMailStore store) => new(new MailDataLoader(store));

    [Fact]
    public async Task RefreshAsync_Should_LoadEveryWorkspaceThread_When_StoreHasThreads()
    {
        // arrange
        var store = new FakeMailStore();
        store.Messages.Add(MailMessageBuilder.Create(
            "m-1", threadId: "t-1", createdAt: s_now, recipients: [MailMessageBuilder.ToRecipient("alice")]));
        var state = CreateState(store);

        // act
        await state.RefreshAsync(CancellationToken.None);

        // assert
        Assert.Equal(["t-1"], state.Threads.Select(t => t.ThreadId));
    }

    [Fact]
    public async Task RefreshAsync_Should_OrderNewestActivityFirst_When_MultipleThreadsExist()
    {
        // arrange
        var store = new FakeMailStore();
        store.Messages.Add(MailMessageBuilder.Create(
            "m-1", threadId: "t-1", createdAt: s_now, recipients: [MailMessageBuilder.ToRecipient("alice")]));
        store.Messages.Add(MailMessageBuilder.Create(
            "m-2", threadId: "t-2", createdAt: s_now.AddMinutes(1), recipients: [MailMessageBuilder.ToRecipient("alice")]));
        var state = CreateState(store);

        // act
        await state.RefreshAsync(CancellationToken.None);

        // assert
        Assert.Equal(["t-2", "t-1"], state.Threads.Select(t => t.ThreadId));
    }

    [Fact]
    public async Task RefreshAsync_Should_KeepSelectedRowOnSameThread_When_ThreadStillPresentAfterReorder()
    {
        // arrange
        var store = new FakeMailStore();
        store.Messages.Add(MailMessageBuilder.Create(
            "m-1", threadId: "t-1", createdAt: s_now, recipients: [MailMessageBuilder.ToRecipient("alice")]));
        store.Messages.Add(MailMessageBuilder.Create(
            "m-2", threadId: "t-2", createdAt: s_now.AddMinutes(1), recipients: [MailMessageBuilder.ToRecipient("alice")]));
        var state = CreateState(store);
        await state.RefreshAsync(CancellationToken.None);
        state.SelectedRow = 1; // t-1, currently the older/last row

        // act
        // a newer message pushes t-1 to a different row on refresh
        store.Messages.Add(MailMessageBuilder.Create(
            "m-3", threadId: "t-3", createdAt: s_now.AddMinutes(2), recipients: [MailMessageBuilder.ToRecipient("alice")]));
        await state.RefreshAsync(CancellationToken.None);

        // assert
        Assert.Equal(["t-3", "t-2", "t-1"], state.Threads.Select(t => t.ThreadId));
        Assert.Equal(2, state.SelectedRow);
        Assert.Equal("t-1", state.SelectedThread?.ThreadId);
    }

    [Fact]
    public async Task RefreshAsync_Should_ClampSelectedRow_When_SelectedThreadNoLongerPresent()
    {
        // arrange
        var store = new FakeMailStore();
        store.Messages.Add(MailMessageBuilder.Create(
            "m-1", threadId: "t-1", createdAt: s_now, recipients: [MailMessageBuilder.ToRecipient("alice")]));
        store.Messages.Add(MailMessageBuilder.Create(
            "m-2", threadId: "t-2", createdAt: s_now.AddMinutes(1), recipients: [MailMessageBuilder.ToRecipient("alice")]));
        var state = CreateState(store);
        await state.RefreshAsync(CancellationToken.None);
        state.SelectedRow = 0; // t-2, newest first

        // act
        // t-2's only message is removed from the store entirely
        store.Messages.RemoveAt(1);
        await state.RefreshAsync(CancellationToken.None);

        // assert
        Assert.Equal(["t-1"], state.Threads.Select(t => t.ThreadId));
        Assert.Equal(0, state.SelectedRow);
    }

    [Fact]
    public async Task SelectAgentFilterAsync_Should_NarrowToThreadsTheAgentSentOrReceived_When_AgentIsGiven()
    {
        // arrange
        var store = new FakeMailStore();
        store.Messages.Add(MailMessageBuilder.Create(
            "m-1", sender: "alice", threadId: "t-1", createdAt: s_now, recipients: [MailMessageBuilder.ToRecipient("bob")]));
        store.Messages.Add(MailMessageBuilder.Create(
            "m-2", sender: "carol", threadId: "t-2", createdAt: s_now.AddMinutes(1), recipients: [MailMessageBuilder.ToRecipient("dave")]));
        var state = CreateState(store);
        await state.RefreshAsync(CancellationToken.None);

        // act
        await state.SelectAgentFilterAsync("alice", CancellationToken.None);

        // assert
        Assert.Equal("alice", state.AgentFilter);
        Assert.Equal(["t-1"], state.Threads.Select(t => t.ThreadId));
        Assert.Equal(0, state.SelectedRow);
    }

    [Fact]
    public async Task SelectAgentFilterAsync_Should_RestoreEveryThread_When_FilterIsClearedToNull()
    {
        // arrange
        var store = new FakeMailStore();
        store.Messages.Add(MailMessageBuilder.Create(
            "m-1", sender: "alice", threadId: "t-1", createdAt: s_now, recipients: [MailMessageBuilder.ToRecipient("bob")]));
        store.Messages.Add(MailMessageBuilder.Create(
            "m-2", sender: "carol", threadId: "t-2", createdAt: s_now.AddMinutes(1), recipients: [MailMessageBuilder.ToRecipient("dave")]));
        var state = CreateState(store);
        await state.RefreshAsync(CancellationToken.None);
        await state.SelectAgentFilterAsync("alice", CancellationToken.None);

        // act
        await state.SelectAgentFilterAsync(null, CancellationToken.None);

        // assert
        Assert.Null(state.AgentFilter);
        Assert.Equal(["t-2", "t-1"], state.Threads.Select(t => t.ThreadId));
    }

    [Fact]
    public async Task ApplySearch_Should_NarrowToThreadsMatchingSubject_When_TextMatchesOneThread()
    {
        // arrange
        var store = new FakeMailStore();
        store.Messages.Add(MailMessageBuilder.Create(
            "m-1", subject: "Status update", threadId: "t-1", createdAt: s_now,
            recipients: [MailMessageBuilder.ToRecipient("alice")]));
        store.Messages.Add(MailMessageBuilder.Create(
            "m-2", subject: "Lunch plans", threadId: "t-2", createdAt: s_now.AddMinutes(1),
            recipients: [MailMessageBuilder.ToRecipient("alice")]));
        var state = CreateState(store);
        await state.RefreshAsync(CancellationToken.None);

        // act
        state.ApplySearch("status");

        // assert
        Assert.Equal(["t-1"], state.Threads.Select(t => t.ThreadId));
        Assert.Equal(0, state.SelectedRow);
    }

    [Fact]
    public async Task ApplySearch_Should_MatchLastSenderAndLastRecipients_When_TextMatchesEither()
    {
        // arrange
        var store = new FakeMailStore();
        store.Messages.Add(MailMessageBuilder.Create(
            "m-1", sender: "bob", threadId: "t-1", createdAt: s_now, recipients: [MailMessageBuilder.ToRecipient("carol")]));
        store.Messages.Add(MailMessageBuilder.Create(
            "m-2", sender: "dave", threadId: "t-2", createdAt: s_now.AddMinutes(1), recipients: [MailMessageBuilder.ToRecipient("erin")]));
        var state = CreateState(store);
        await state.RefreshAsync(CancellationToken.None);

        // act
        state.ApplySearch("carol");

        // assert
        Assert.Equal(["t-1"], state.Threads.Select(t => t.ThreadId));
    }

    [Fact]
    public async Task ApplySearch_Should_ShowEveryThread_When_TextIsCleared()
    {
        // arrange
        var store = new FakeMailStore();
        store.Messages.Add(MailMessageBuilder.Create(
            "m-1", subject: "Status update", threadId: "t-1", createdAt: s_now,
            recipients: [MailMessageBuilder.ToRecipient("alice")]));
        store.Messages.Add(MailMessageBuilder.Create(
            "m-2", subject: "Lunch plans", threadId: "t-2", createdAt: s_now.AddMinutes(1),
            recipients: [MailMessageBuilder.ToRecipient("alice")]));
        var state = CreateState(store);
        await state.RefreshAsync(CancellationToken.None);
        state.ApplySearch("status");

        // act
        state.ApplySearch("");

        // assert
        Assert.Equal(["t-2", "t-1"], state.Threads.Select(t => t.ThreadId));
    }

    [Fact]
    public async Task TotalCount_Should_IgnoreTheSearchFilter_When_SearchIsActive()
    {
        // arrange
        var store = new FakeMailStore();
        store.Messages.Add(MailMessageBuilder.Create(
            "m-1", subject: "Status update", threadId: "t-1", createdAt: s_now,
            recipients: [MailMessageBuilder.ToRecipient("alice")]));
        store.Messages.Add(MailMessageBuilder.Create(
            "m-2", subject: "Lunch plans", threadId: "t-2", createdAt: s_now.AddMinutes(1),
            recipients: [MailMessageBuilder.ToRecipient("alice")]));
        var state = CreateState(store);
        await state.RefreshAsync(CancellationToken.None);

        // act
        state.ApplySearch("status");

        // assert
        Assert.Single(state.Threads);
        Assert.Equal(2, state.TotalCount);
    }
}
