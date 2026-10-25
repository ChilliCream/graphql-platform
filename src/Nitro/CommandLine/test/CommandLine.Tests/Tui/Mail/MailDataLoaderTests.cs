using ChilliCream.Nitro.CommandLine.Tui.Mail;

namespace ChilliCream.Nitro.CommandLine.Tests.Tui.Mail;

public sealed class MailDataLoaderTests
{
    private static readonly DateTimeOffset s_now = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task LoadWorkspaceThreadsAsync_Should_ReturnEveryThread_When_AgentIsNull()
    {
        // arrange
        var store = new FakeMailStore();
        store.Messages.Add(MailMessageBuilder.Create(
            "m-1", sender: "bob", threadId: "t-1", createdAt: s_now, recipients: [MailMessageBuilder.ToRecipient("carol")]));
        var loader = new MailDataLoader(store);

        // act
        var threads = await loader.LoadWorkspaceThreadsAsync(agent: null, CancellationToken.None);

        // assert
        Assert.Equal(["t-1"], threads.Select(t => t.ThreadId));
    }

    [Fact]
    public async Task LoadWorkspaceThreadsAsync_Should_NarrowToThreadsTheAgentSentOrReceived_When_AgentIsGiven()
    {
        // arrange
        var store = new FakeMailStore();
        store.Messages.Add(MailMessageBuilder.Create(
            "m-1", sender: "alice", threadId: "t-1", createdAt: s_now, recipients: [MailMessageBuilder.ToRecipient("bob")]));
        store.Messages.Add(MailMessageBuilder.Create(
            "m-2", sender: "carol", threadId: "t-2", createdAt: s_now.AddMinutes(1), recipients: [MailMessageBuilder.ToRecipient("bob")]));
        var loader = new MailDataLoader(store);

        // act
        var threads = await loader.LoadWorkspaceThreadsAsync("alice", CancellationToken.None);

        // assert
        Assert.Equal(["t-1"], threads.Select(t => t.ThreadId));
    }

    [Fact]
    public async Task LoadWorkspaceThreadsAsync_Should_ReturnNewestActivityFirst_When_MultipleThreadsExist()
    {
        // arrange
        var store = new FakeMailStore();
        store.Messages.Add(MailMessageBuilder.Create(
            "m-1", threadId: "t-1", createdAt: s_now, recipients: [MailMessageBuilder.ToRecipient("alice")]));
        store.Messages.Add(MailMessageBuilder.Create(
            "m-2", threadId: "t-2", createdAt: s_now.AddMinutes(1), recipients: [MailMessageBuilder.ToRecipient("alice")]));
        var loader = new MailDataLoader(store);

        // act
        var threads = await loader.LoadWorkspaceThreadsAsync(agent: null, CancellationToken.None);

        // assert
        Assert.Equal(["t-2", "t-1"], threads.Select(t => t.ThreadId));
    }
}
