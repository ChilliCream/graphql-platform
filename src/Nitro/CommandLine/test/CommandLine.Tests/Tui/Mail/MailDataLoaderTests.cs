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
