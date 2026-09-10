using ChilliCream.Nitro.CommandLine.Tui.Mail;

namespace ChilliCream.Nitro.CommandLine.Tests.Tui.Mail;

public sealed class MailDataLoaderTests
{
    private static readonly DateTimeOffset Now = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task LoadInboxAsync_Should_ExcludeArchived_When_FilterIsInbox()
    {
        // arrange
        var store = new FakeMailStore();
        store.Messages.Add(MailMessageBuilder.Create(
            "m-1", createdAt: Now, recipients: [MailMessageBuilder.ToRecipient("alice")]));
        store.Messages.Add(MailMessageBuilder.Create(
            "m-2",
            createdAt: Now,
            recipients: [MailMessageBuilder.ToRecipient("alice", archivedAt: Now)]));
        var loader = new MailDataLoader(store);

        // act
        var messages = await loader.LoadInboxAsync("alice", MailListFilter.Inbox, CancellationToken.None);

        // assert
        Assert.Equal(["m-1"], messages.Select(m => m.Id));
    }

    [Fact]
    public async Task LoadInboxAsync_Should_OnlyReturnUnread_When_FilterIsUnread()
    {
        // arrange
        var store = new FakeMailStore();
        store.Messages.Add(MailMessageBuilder.Create(
            "m-1", createdAt: Now, recipients: [MailMessageBuilder.ToRecipient("alice", readAt: Now)]));
        store.Messages.Add(MailMessageBuilder.Create(
            "m-2", createdAt: Now, recipients: [MailMessageBuilder.ToRecipient("alice")]));
        var loader = new MailDataLoader(store);

        // act
        var messages = await loader.LoadInboxAsync("alice", MailListFilter.Unread, CancellationToken.None);

        // assert
        Assert.Equal(["m-2"], messages.Select(m => m.Id));
    }

    [Fact]
    public async Task LoadInboxAsync_Should_OnlyReturnArchived_When_FilterIsArchived()
    {
        // arrange
        var store = new FakeMailStore();
        store.Messages.Add(MailMessageBuilder.Create(
            "m-1", createdAt: Now, recipients: [MailMessageBuilder.ToRecipient("alice", archivedAt: Now)]));
        store.Messages.Add(MailMessageBuilder.Create(
            "m-2", createdAt: Now, recipients: [MailMessageBuilder.ToRecipient("alice")]));
        var loader = new MailDataLoader(store);

        // act
        var messages = await loader.LoadInboxAsync("alice", MailListFilter.Archived, CancellationToken.None);

        // assert
        Assert.Equal(["m-1"], messages.Select(m => m.Id));
    }

    [Fact]
    public async Task LoadInboxAsync_Should_ReturnNewestFirst()
    {
        // arrange
        var store = new FakeMailStore();
        store.Messages.Add(MailMessageBuilder.Create(
            "m-1", createdAt: Now, recipients: [MailMessageBuilder.ToRecipient("alice")]));
        store.Messages.Add(MailMessageBuilder.Create(
            "m-2", createdAt: Now.AddMinutes(1), recipients: [MailMessageBuilder.ToRecipient("alice")]));
        var loader = new MailDataLoader(store);

        // act
        var messages = await loader.LoadInboxAsync("alice", MailListFilter.Inbox, CancellationToken.None);

        // assert
        Assert.Equal(["m-2", "m-1"], messages.Select(m => m.Id));
    }

    [Fact]
    public async Task LoadSentAsync_Should_ReturnMessagesTheActorSent_ExcludingReceivedMail()
    {
        // arrange
        var store = new FakeMailStore();
        store.Messages.Add(MailMessageBuilder.Create(
            "m-1", sender: "alice", createdAt: Now, recipients: [MailMessageBuilder.ToRecipient("bob")]));
        store.Messages.Add(MailMessageBuilder.Create(
            "m-2", sender: "bob", createdAt: Now, recipients: [MailMessageBuilder.ToRecipient("alice")]));
        var loader = new MailDataLoader(store);

        // act
        var messages = await loader.LoadSentAsync("alice", CancellationToken.None);

        // assert
        Assert.Equal(["m-1"], messages.Select(m => m.Id));
    }

    [Fact]
    public async Task LoadSentAsync_Should_ReturnAnUnrepliedThreadRootMessage()
    {
        // arrange: no recipient row exists for the sender, so this message
        // is otherwise unreachable in the Inbox mailbox.
        var store = new FakeMailStore();
        store.Messages.Add(MailMessageBuilder.Create(
            "m-1", sender: "alice", threadId: "m-1", createdAt: Now, recipients: [MailMessageBuilder.ToRecipient("bob")]));
        var loader = new MailDataLoader(store);

        // act
        var inbox = await loader.LoadInboxAsync("alice", MailListFilter.Inbox, CancellationToken.None);
        var sent = await loader.LoadSentAsync("alice", CancellationToken.None);

        // assert
        Assert.Empty(inbox);
        Assert.Equal(["m-1"], sent.Select(m => m.Id));
    }

    [Fact]
    public async Task LoadAllAsync_Should_ReturnMessagesTheActorSentOrReceived()
    {
        // arrange
        var store = new FakeMailStore();
        store.Messages.Add(MailMessageBuilder.Create(
            "m-1", sender: "alice", createdAt: Now, recipients: [MailMessageBuilder.ToRecipient("bob")]));
        store.Messages.Add(MailMessageBuilder.Create(
            "m-2", sender: "bob", createdAt: Now.AddMinutes(1), recipients: [MailMessageBuilder.ToRecipient("alice")]));
        store.Messages.Add(MailMessageBuilder.Create(
            "m-3", sender: "bob", createdAt: Now.AddMinutes(2), recipients: [MailMessageBuilder.ToRecipient("carol")]));
        var loader = new MailDataLoader(store);

        // act
        var messages = await loader.LoadAllAsync("alice", CancellationToken.None);

        // assert
        Assert.Equal(["m-2", "m-1"], messages.Select(m => m.Id));
    }

    [Fact]
    public async Task LoadWorkspaceAsync_Should_ReturnEveryMessage_IncludingBetweenTwoOtherAgents()
    {
        // arrange
        var store = new FakeMailStore();
        store.Messages.Add(MailMessageBuilder.Create(
            "m-1", sender: "bob", createdAt: Now, recipients: [MailMessageBuilder.ToRecipient("carol")]));
        var loader = new MailDataLoader(store);

        // act
        var inbox = await loader.LoadInboxAsync("alice", MailListFilter.Inbox, CancellationToken.None);
        var sent = await loader.LoadSentAsync("alice", CancellationToken.None);
        var all = await loader.LoadAllAsync("alice", CancellationToken.None);
        var workspace = await loader.LoadWorkspaceAsync(agent: null, CancellationToken.None);

        // assert: only reachable from the Workspace mailbox for actor alice.
        Assert.Empty(inbox);
        Assert.Empty(sent);
        Assert.Empty(all);
        Assert.Equal(["m-1"], workspace.Select(m => m.Id));
    }

    [Fact]
    public async Task LoadWorkspaceAsync_Should_ReturnMessagesTheAgentSentOrReceived_When_AgentGiven()
    {
        // arrange
        var store = new FakeMailStore();
        store.Messages.Add(MailMessageBuilder.Create(
            "m-1", sender: "alice", createdAt: Now, recipients: [MailMessageBuilder.ToRecipient("bob")]));
        store.Messages.Add(MailMessageBuilder.Create(
            "m-2", sender: "carol", createdAt: Now.AddMinutes(1), recipients: [MailMessageBuilder.ToRecipient("alice")]));
        store.Messages.Add(MailMessageBuilder.Create(
            "m-3", sender: "carol", createdAt: Now.AddMinutes(2), recipients: [MailMessageBuilder.ToRecipient("bob")]));
        var loader = new MailDataLoader(store);

        // act
        var messages = await loader.LoadWorkspaceAsync("alice", CancellationToken.None);

        // assert: m-1 (alice sent it) and m-2 (alice received it), not m-3
        // (alice is neither sender nor recipient).
        Assert.Equal(["m-2", "m-1"], messages.Select(m => m.Id));
    }

    [Fact]
    public async Task LoadWorkspaceAsync_Should_ReturnMessageExactlyOnce_When_AgentIsOneOfSeveralCcRecipients()
    {
        // arrange
        var store = new FakeMailStore();
        store.Messages.Add(MailMessageBuilder.Create(
            "m-1",
            sender: "carol",
            createdAt: Now,
            recipients:
            [
                MailMessageBuilder.ToRecipient("bob"),
                MailMessageBuilder.CcRecipient("alice", ordinal: 1),
                MailMessageBuilder.CcRecipient("dave", ordinal: 2)
            ]));
        var loader = new MailDataLoader(store);

        // act
        var messages = await loader.LoadWorkspaceAsync("alice", CancellationToken.None);

        // assert
        Assert.Equal(["m-1"], messages.Select(m => m.Id));
    }

    [Fact]
    public async Task LoadThreadAsync_Should_ReturnEveryMessageInTheThread_OldestFirst()
    {
        // arrange
        var store = new FakeMailStore();
        store.Messages.Add(MailMessageBuilder.Create(
            "m-2", threadId: "t-1", createdAt: Now.AddMinutes(1)));
        store.Messages.Add(MailMessageBuilder.Create(
            "m-1", threadId: "t-1", createdAt: Now));
        store.Messages.Add(MailMessageBuilder.Create(
            "m-3", threadId: "t-other", createdAt: Now));
        var loader = new MailDataLoader(store);

        // act
        var messages = await loader.LoadThreadAsync("t-1", CancellationToken.None);

        // assert
        Assert.Equal(["m-1", "m-2"], messages.Select(m => m.Id));
    }
}
