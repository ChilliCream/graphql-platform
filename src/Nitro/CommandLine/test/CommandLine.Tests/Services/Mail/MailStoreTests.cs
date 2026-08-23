using ChilliCream.Nitro.CommandLine.Services.Mail;
using ChilliCream.Nitro.CommandLine.Services.Workspace;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Time.Testing;

namespace ChilliCream.Nitro.CommandLine.Tests.Mail;

/// <summary>
/// Exercises <see cref="MailStore"/> against a real SQLite workspace: the
/// full write and read surface through the public API, plus the raw-SQL
/// paths (schema constraints, version guard) that the API cannot reach.
/// </summary>
public sealed class MailStoreTests : IAsyncDisposable
{
    private readonly DirectoryInfo _tempRoot;
    private readonly string _workingDirectory;
    private readonly string _workspaceDirectory;
    private readonly FakeTimeProvider _timeProvider;
    private readonly AgentRegistry _registry;
    private readonly MailStore _store;

    public MailStoreTests()
    {
        _tempRoot = Directory.CreateTempSubdirectory("nitro-mail-store-tests");
        _workingDirectory = Path.Combine(_tempRoot.FullName, "acme");
        Directory.CreateDirectory(_workingDirectory);
        _workspaceDirectory = AgentWorkspace.GetDirectory(_workingDirectory);

        _timeProvider = new FakeTimeProvider(
            new DateTimeOffset(2026, 1, 10, 12, 0, 0, TimeSpan.Zero));

        _registry = new AgentRegistry(new TestFileSystem(_workingDirectory), _timeProvider, new AgentDatabase());
        _store = new MailStore(
            new TestFileSystem(_workingDirectory), _timeProvider, new AgentDatabase(), _registry);
    }

    public async ValueTask DisposeAsync()
    {
        await Task.CompletedTask;
        _tempRoot.Delete(recursive: true);
    }

    private async Task InitWorkspaceAsync(CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(_workspaceDirectory);
        await _store.InitializeWorkspaceAsync(_workspaceDirectory, cancellationToken);
    }

    private async Task<SqliteConnection> SeedAsync(CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(_workspaceDirectory);
        return await _store.InitializeAsync(_workspaceDirectory, cancellationToken);
    }

    private Task<AgentRecord> SeedAgentAsync(string name, CancellationToken cancellationToken)
        => _registry.RegisterAsync(name, role: "", client: "", cancellationToken);

    private Task<MailMessage> SendAsync(
        string sender,
        string subject,
        IReadOnlyList<string> to,
        IReadOnlyList<string>? cc,
        CancellationToken cancellationToken,
        string body = "body")
        => _store.SendMessageAsync(
            new MailMessageCreation
            {
                Sender = sender,
                Subject = subject,
                Body = body,
                To = to,
                Cc = cc ?? []
            },
            cancellationToken);

    [Fact]
    public async Task SendMessageAsync_Should_CreateImplicitRow_When_RecipientUnknown()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitWorkspaceAsync(cancellationToken);

        // act
        var message = await SendAsync("claude", "hello", ["bob", "alice"], null, cancellationToken);

        // assert
        Assert.Equal(["bob", "alice"], message.Unregistered);
        var bob = await _registry.GetAsync("bob", cancellationToken);
        var alice = await _registry.GetAsync("alice", cancellationToken);
        Assert.True(bob?.Implicit);
        Assert.True(alice?.Implicit);
    }

    [Fact]
    public async Task SendMessageAsync_Should_ReportOnlyUnknownRecipients_When_Mixed()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitWorkspaceAsync(cancellationToken);
        await SeedAgentAsync("bob", cancellationToken);

        // act
        var message = await SendAsync("claude", "hello", ["bob", "dave"], null, cancellationToken);

        // assert
        Assert.Equal(["dave"], message.Unregistered);
    }

    [Fact]
    public async Task SendMessageAsync_Should_ReportStillImplicitRecipient_When_AlreadyImplicit()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitWorkspaceAsync(cancellationToken);
        await SendAsync("claude", "first", ["dave"], null, cancellationToken);

        // act
        var second = await SendAsync("claude", "second", ["dave"], null, cancellationToken);

        // assert
        Assert.Equal(["dave"], second.Unregistered);
    }

    [Fact]
    public async Task SendMessageAsync_Should_NotReportRegisteredRecipient_When_ImplicitRegistersAfterwards()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitWorkspaceAsync(cancellationToken);
        await SendAsync("claude", "first", ["dave"], null, cancellationToken);
        await _registry.RegisterAsync("dave", role: "", client: "", cancellationToken);

        // act
        var second = await SendAsync("claude", "second", ["dave"], null, cancellationToken);

        // assert
        Assert.Empty(second.Unregistered);
    }

    [Fact]
    public async Task SendMessageAsync_Should_Throw_When_RecipientNameInvalid()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitWorkspaceAsync(cancellationToken);

        // act & assert
        await Assert.ThrowsAsync<ExitException>(
            () => SendAsync("claude", "hello", ["Dave!"], null, cancellationToken));
    }

    [Fact]
    public async Task SendMessageAsync_Should_AutoRegisterSender_When_NotAlreadyRegistered()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitWorkspaceAsync(cancellationToken);
        await SeedAgentAsync("bob", cancellationToken);

        // act
        await SendAsync("claude", "hello", ["bob"], null, cancellationToken);

        // assert
        var sender = await _registry.GetAsync("claude", cancellationToken);
        Assert.NotNull(sender);
    }

    [Fact]
    public async Task SendMessageAsync_Should_CollapseDuplicatesWithToWinning_AndPreserveOrdinalOrder()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitWorkspaceAsync(cancellationToken);
        await SeedAgentAsync("bob", cancellationToken);
        await SeedAgentAsync("alice", cancellationToken);
        await SeedAgentAsync("carol", cancellationToken);

        // act
        var message = await SendAsync(
            "claude", "hello", to: ["bob", "alice"], cc: ["alice", "carol"], cancellationToken);

        // assert
        Assert.Equal(["bob", "alice", "carol"], message.Recipients.Select(r => r.Name));
        Assert.Equal([0, 1, 2], message.Recipients.Select(r => r.Ordinal));
        Assert.Equal(
            [MailRecipientKinds.To, MailRecipientKinds.To, MailRecipientKinds.Cc],
            message.Recipients.Select(r => r.Kind));
    }

    [Fact]
    public async Task SendMessageAsync_Should_Throw_When_NoRecipients()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitWorkspaceAsync(cancellationToken);

        // act & assert
        await Assert.ThrowsAsync<ExitException>(
            () => SendAsync("claude", "hello", [], null, cancellationToken));
    }

    [Fact]
    public async Task SendMessageAsync_Should_Throw_When_SubjectEmpty()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitWorkspaceAsync(cancellationToken);
        await SeedAgentAsync("bob", cancellationToken);

        // act & assert
        await Assert.ThrowsAsync<ExitException>(
            () => SendAsync("claude", "  ", ["bob"], null, cancellationToken));
    }

    [Fact]
    public async Task ReplyMessageAsync_Should_ComputeRecipients_AsSenderAndRecipientsMinusActor()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitWorkspaceAsync(cancellationToken);
        await SeedAgentAsync("bob", cancellationToken);
        await SeedAgentAsync("carol", cancellationToken);
        var original = await SendAsync("claude", "hello", ["bob"], ["carol"], cancellationToken);

        // act
        var reply = await _store.ReplyMessageAsync(original.Id, "bob", "reply body", cancellationToken);

        // assert
        Assert.Equal(["claude", "carol"], reply.Recipients.Select(r => r.Name));
        Assert.Equal(original.ThreadId, reply.ThreadId);
        Assert.Equal(original.Id, reply.InReplyTo);
    }

    [Fact]
    public async Task ReplyMessageAsync_Should_Throw_When_ActorNotParticipant()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitWorkspaceAsync(cancellationToken);
        await SeedAgentAsync("bob", cancellationToken);
        var original = await SendAsync("claude", "hello", ["bob"], null, cancellationToken);

        // act & assert
        await Assert.ThrowsAsync<ExitException>(
            () => _store.ReplyMessageAsync(original.Id, "stranger", "reply body", cancellationToken));
    }

    [Fact]
    public async Task ReplyMessageAsync_Should_Throw_When_OnlyParticipantIsActor()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitWorkspaceAsync(cancellationToken);
        var original = await SendAsync("claude", "note to self", ["claude"], null, cancellationToken);

        // act
        var exception = await Assert.ThrowsAsync<ExitException>(
            () => _store.ReplyMessageAsync(original.Id, "claude", "reply", cancellationToken));

        // assert
        Assert.Contains("claude", exception.Message);
    }

    [Fact]
    public async Task ReplyMessageAsync_Should_InheritSubjectFromThreadRoot_ThroughMultipleReplies()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitWorkspaceAsync(cancellationToken);
        await SeedAgentAsync("bob", cancellationToken);
        var original = await SendAsync("claude", "root subject", ["bob"], null, cancellationToken);
        var firstReply = await _store.ReplyMessageAsync(original.Id, "bob", "reply 1", cancellationToken);

        // act
        var secondReply = await _store.ReplyMessageAsync(
            firstReply.Id, "claude", "reply 2", cancellationToken);

        // assert
        Assert.Equal("root subject", secondReply.Subject);
        Assert.Equal(original.ThreadId, secondReply.ThreadId);
    }

    [Fact]
    public async Task GetThreadMessagesAsync_Should_ReturnMessagesOrderedByCreatedAt()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitWorkspaceAsync(cancellationToken);
        await SeedAgentAsync("bob", cancellationToken);
        var original = await SendAsync("claude", "root", ["bob"], null, cancellationToken);
        _timeProvider.Advance(TimeSpan.FromMinutes(1));
        var reply = await _store.ReplyMessageAsync(original.Id, "bob", "reply", cancellationToken);

        // act
        var thread = await _store.GetThreadMessagesAsync(original.ThreadId, cancellationToken);

        // assert
        Assert.Equal([original.Id, reply.Id], thread.Select(m => m.Id));
    }

    [Fact]
    public async Task QueryInboxAsync_Should_ReturnOnlyMessagesAddressedToActor()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitWorkspaceAsync(cancellationToken);
        await SeedAgentAsync("bob", cancellationToken);
        await SeedAgentAsync("carol", cancellationToken);
        await SendAsync("claude", "for bob", ["bob"], null, cancellationToken);
        await SendAsync("claude", "for carol", ["carol"], null, cancellationToken);

        // act
        var inbox = await _store.QueryInboxAsync(
            new MailInboxFilter { Actor = "bob" }, cancellationToken);

        // assert
        var message = Assert.Single(inbox);
        Assert.Equal("for bob", message.Subject);
    }

    [Fact]
    public async Task QueryInboxAsync_Should_FilterByUnreadOnly()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitWorkspaceAsync(cancellationToken);
        await SeedAgentAsync("bob", cancellationToken);
        var first = await SendAsync("claude", "first", ["bob"], null, cancellationToken);
        await SendAsync("claude", "second", ["bob"], null, cancellationToken);
        await _store.MarkReadAsync([first.Id], "bob", cancellationToken);

        // act
        var inbox = await _store.QueryInboxAsync(
            new MailInboxFilter { Actor = "bob", UnreadOnly = true }, cancellationToken);

        // assert
        var message = Assert.Single(inbox);
        Assert.Equal("second", message.Subject);
    }

    [Fact]
    public async Task QueryInboxAsync_Should_FilterByFrom()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitWorkspaceAsync(cancellationToken);
        await SeedAgentAsync("bob", cancellationToken);
        await SeedAgentAsync("dave", cancellationToken);
        await SendAsync("claude", "from claude", ["bob"], null, cancellationToken);
        await SendAsync("dave", "from dave", ["bob"], null, cancellationToken);

        // act
        var inbox = await _store.QueryInboxAsync(
            new MailInboxFilter { Actor = "bob", From = "dave" }, cancellationToken);

        // assert
        var message = Assert.Single(inbox);
        Assert.Equal("from dave", message.Subject);
    }

    [Fact]
    public async Task QueryInboxAsync_Should_ExcludeArchived_When_IncludeArchivedFalse()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitWorkspaceAsync(cancellationToken);
        await SeedAgentAsync("bob", cancellationToken);
        var message = await SendAsync("claude", "hello", ["bob"], null, cancellationToken);
        await _store.ArchiveAsync([message.Id], "bob", cancellationToken);

        // act
        var inbox = await _store.QueryInboxAsync(
            new MailInboxFilter { Actor = "bob" }, cancellationToken);

        // assert
        Assert.Empty(inbox);
    }

    [Fact]
    public async Task QueryInboxAsync_Should_ApplyLimit()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitWorkspaceAsync(cancellationToken);
        await SeedAgentAsync("bob", cancellationToken);
        await SendAsync("claude", "first", ["bob"], null, cancellationToken);
        await SendAsync("claude", "second", ["bob"], null, cancellationToken);

        // act
        var inbox = await _store.QueryInboxAsync(
            new MailInboxFilter { Actor = "bob", Limit = 1 }, cancellationToken);

        // assert
        Assert.Single(inbox);
    }

    [Fact]
    public async Task QueryWorkspaceMessagesAsync_Should_ReturnCrossAgentTraffic_When_NoAgentFilter()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitWorkspaceAsync(cancellationToken);
        await SeedAgentAsync("bob", cancellationToken);
        await SeedAgentAsync("carol", cancellationToken);
        var message = await SendAsync("bob", "bob to carol", ["carol"], null, cancellationToken);

        // act
        var workspace = await _store.QueryWorkspaceMessagesAsync(
            new MailWorkspaceFilter(), cancellationToken);

        // assert
        var single = Assert.Single(workspace);
        Assert.Equal(message.Id, single.Id);
    }

    [Fact]
    public async Task QueryWorkspaceMessagesAsync_Should_OrderNewestFirst()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitWorkspaceAsync(cancellationToken);
        await SeedAgentAsync("bob", cancellationToken);
        var first = await SendAsync("claude", "first", ["bob"], null, cancellationToken);
        _timeProvider.Advance(TimeSpan.FromMinutes(1));
        var second = await SendAsync("claude", "second", ["bob"], null, cancellationToken);

        // act
        var workspace = await _store.QueryWorkspaceMessagesAsync(
            new MailWorkspaceFilter(), cancellationToken);

        // assert
        Assert.Equal([second.Id, first.Id], workspace.Select(m => m.Id));
    }

    [Fact]
    public async Task QueryWorkspaceMessagesAsync_Should_MatchAgent_When_SenderSide()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitWorkspaceAsync(cancellationToken);
        await SeedAgentAsync("bob", cancellationToken);
        await SeedAgentAsync("carol", cancellationToken);
        await SeedAgentAsync("dave", cancellationToken);
        var message = await SendAsync("bob", "from bob", ["carol"], null, cancellationToken);
        await SendAsync("carol", "unrelated", ["dave"], null, cancellationToken);

        // act
        var workspace = await _store.QueryWorkspaceMessagesAsync(
            new MailWorkspaceFilter { Agent = "bob" }, cancellationToken);

        // assert
        var single = Assert.Single(workspace);
        Assert.Equal(message.Id, single.Id);
    }

    [Fact]
    public async Task QueryWorkspaceMessagesAsync_Should_MatchAgent_When_RecipientSide()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitWorkspaceAsync(cancellationToken);
        await SeedAgentAsync("bob", cancellationToken);
        await SeedAgentAsync("carol", cancellationToken);
        await SeedAgentAsync("dave", cancellationToken);
        var message = await SendAsync("carol", "to bob", ["bob"], null, cancellationToken);
        await SendAsync("carol", "unrelated", ["dave"], null, cancellationToken);

        // act
        var workspace = await _store.QueryWorkspaceMessagesAsync(
            new MailWorkspaceFilter { Agent = "bob" }, cancellationToken);

        // assert
        var single = Assert.Single(workspace);
        Assert.Equal(message.Id, single.Id);
    }

    [Fact]
    public async Task QueryWorkspaceMessagesAsync_Should_ReturnSingleRow_When_AgentIsBothToAndCc()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitWorkspaceAsync(cancellationToken);
        await SeedAgentAsync("bob", cancellationToken);
        await SeedAgentAsync("carol", cancellationToken);
        var message = await SendAsync("carol", "hello", ["bob"], ["bob"], cancellationToken);

        // act
        var workspace = await _store.QueryWorkspaceMessagesAsync(
            new MailWorkspaceFilter { Agent = "bob" }, cancellationToken);

        // assert
        var single = Assert.Single(workspace);
        Assert.Equal(message.Id, single.Id);
    }

    [Fact]
    public async Task QueryWorkspaceMessagesAsync_Should_FilterBySince()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitWorkspaceAsync(cancellationToken);
        await SeedAgentAsync("bob", cancellationToken);
        await SendAsync("claude", "first", ["bob"], null, cancellationToken);
        _timeProvider.Advance(TimeSpan.FromSeconds(30));
        var since = _timeProvider.GetUtcNow();
        _timeProvider.Advance(TimeSpan.FromSeconds(30));
        var second = await SendAsync("claude", "second", ["bob"], null, cancellationToken);

        // act
        var workspace = await _store.QueryWorkspaceMessagesAsync(
            new MailWorkspaceFilter { Since = since }, cancellationToken);

        // assert
        var single = Assert.Single(workspace);
        Assert.Equal(second.Id, single.Id);
    }

    [Fact]
    public async Task QueryWorkspaceMessagesAsync_Should_ApplyLimit()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitWorkspaceAsync(cancellationToken);
        await SeedAgentAsync("bob", cancellationToken);
        await SendAsync("claude", "first", ["bob"], null, cancellationToken);
        await SendAsync("claude", "second", ["bob"], null, cancellationToken);

        // act
        var workspace = await _store.QueryWorkspaceMessagesAsync(
            new MailWorkspaceFilter { Limit = 1 }, cancellationToken);

        // assert
        Assert.Single(workspace);
    }

    [Fact]
    public async Task MarkReadAsync_Should_SetReadAt_OnlyForActorRecipientRow()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitWorkspaceAsync(cancellationToken);
        await SeedAgentAsync("bob", cancellationToken);
        await SeedAgentAsync("carol", cancellationToken);
        var message = await SendAsync("claude", "hello", ["bob", "carol"], null, cancellationToken);

        // act
        await _store.MarkReadAsync([message.Id], "bob", cancellationToken);

        // assert
        var reloaded = await _store.GetRequiredMessageAsync(message.Id, cancellationToken);
        var bob = reloaded.Recipients.Single(r => r.Name == "bob");
        var carol = reloaded.Recipients.Single(r => r.Name == "carol");
        Assert.NotNull(bob.ReadAt);
        Assert.Null(carol.ReadAt);
    }

    [Fact]
    public async Task ArchiveAsync_Should_SetArchivedAt_OnlyForActorRecipientRow()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitWorkspaceAsync(cancellationToken);
        await SeedAgentAsync("bob", cancellationToken);
        await SeedAgentAsync("carol", cancellationToken);
        var message = await SendAsync("claude", "hello", ["bob", "carol"], null, cancellationToken);

        // act
        await _store.ArchiveAsync([message.Id], "bob", cancellationToken);

        // assert
        var reloaded = await _store.GetRequiredMessageAsync(message.Id, cancellationToken);
        var bob = reloaded.Recipients.Single(r => r.Name == "bob");
        var carol = reloaded.Recipients.Single(r => r.Name == "carol");
        Assert.NotNull(bob.ArchivedAt);
        Assert.Null(carol.ArchivedAt);
    }

    [Fact]
    public async Task MarkReadAsync_Should_RollBackAll_When_AnyIdNotAddressedToActor()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitWorkspaceAsync(cancellationToken);
        await SeedAgentAsync("bob", cancellationToken);
        var validMessage = await SendAsync("claude", "hello", ["bob"], null, cancellationToken);

        // act
        await Assert.ThrowsAsync<ExitException>(
            () => _store.MarkReadAsync(
                [validMessage.Id, "m-does-not-exist"], "bob", cancellationToken));

        // assert
        var reloaded = await _store.GetRequiredMessageAsync(validMessage.Id, cancellationToken);
        Assert.Null(reloaded.Recipients.Single(r => r.Name == "bob").ReadAt);
    }

    [Fact]
    public async Task QueryThreadsAsync_Should_ReturnThreadsActorParticipatesIn_WithUnreadCount()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitWorkspaceAsync(cancellationToken);
        await SeedAgentAsync("bob", cancellationToken);
        await SeedAgentAsync("carol", cancellationToken);
        var thread = await SendAsync("claude", "shared thread", ["bob"], null, cancellationToken);
        await SendAsync("claude", "other thread", ["carol"], null, cancellationToken);

        // act
        var threads = await _store.QueryThreadsAsync("bob", cancellationToken);

        // assert
        var summary = Assert.Single(threads);
        Assert.Equal(thread.ThreadId, summary.ThreadId);
        Assert.Equal(1, summary.UnreadCount);
        Assert.Equal(1, summary.MessageCount);
        Assert.Equal("claude", summary.LastSender);
        Assert.Equal(["bob"], summary.LastRecipients);
        Assert.Equal("body", summary.BodyPreview);
    }

    [Fact]
    public async Task QueryInboxThreadsAsync_Should_ReturnOnlyThreadsAddressedToActor()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitWorkspaceAsync(cancellationToken);
        await SeedAgentAsync("bob", cancellationToken);
        await SeedAgentAsync("carol", cancellationToken);
        var addressedToBob = await SendAsync("claude", "for bob", ["bob"], null, cancellationToken);
        await SendAsync("bob", "sent by bob to carol", ["carol"], null, cancellationToken);

        // act
        var threads = await _store.QueryInboxThreadsAsync("bob", cancellationToken);

        // assert
        var summary = Assert.Single(threads);
        Assert.Equal(addressedToBob.ThreadId, summary.ThreadId);
        Assert.Equal(1, summary.UnreadCount);
    }

    [Fact]
    public async Task QuerySentThreadsAsync_Should_ReturnOnlyThreadsActorSentInto()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitWorkspaceAsync(cancellationToken);
        await SeedAgentAsync("bob", cancellationToken);
        await SeedAgentAsync("carol", cancellationToken);
        var sentByBob = await SendAsync("bob", "from bob", ["carol"], null, cancellationToken);
        await SendAsync("carol", "for bob only", ["bob"], null, cancellationToken);

        // act
        var threads = await _store.QuerySentThreadsAsync("bob", cancellationToken);

        // assert
        var summary = Assert.Single(threads);
        Assert.Equal(sentByBob.ThreadId, summary.ThreadId);
        Assert.Equal(0, summary.UnreadCount);
    }

    [Fact]
    public async Task QueryWorkspaceThreadsAsync_Should_IncludeThreadsBetweenThirdPartyAgents()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitWorkspaceAsync(cancellationToken);
        await SeedAgentAsync("bob", cancellationToken);
        await SeedAgentAsync("carol", cancellationToken);
        var thirdParty = await SendAsync("bob", "between third parties", ["carol"], null, cancellationToken);

        // act
        var threads = await _store.QueryWorkspaceThreadsAsync(null, cancellationToken);

        // assert
        var summary = Assert.Single(threads);
        Assert.Equal(thirdParty.ThreadId, summary.ThreadId);
        Assert.Null(summary.UnreadCount);
    }

    [Fact]
    public async Task QueryWorkspaceThreadsAsync_Should_NeverExposeActorUnreadState_When_NarrowedToAgent()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitWorkspaceAsync(cancellationToken);
        await SeedAgentAsync("bob", cancellationToken);
        await SendAsync("claude", "for bob", ["bob"], null, cancellationToken);

        // act
        var threads = await _store.QueryWorkspaceThreadsAsync("bob", cancellationToken);

        // assert
        var summary = Assert.Single(threads);
        Assert.Null(summary.UnreadCount);
    }

    [Fact]
    public async Task ThreadRollup_Should_CollapseWhitespaceAndTruncate_InBodyPreview()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitWorkspaceAsync(cancellationToken);
        await SeedAgentAsync("bob", cancellationToken);
        var longBody = string.Concat(Enumerable.Repeat("word ", 40)) + "\n\ttrailing\r\nnewlines   here";
        await SendAsync("claude", "long body", ["bob"], null, cancellationToken, body: longBody);

        // act
        var threads = await _store.QueryThreadsAsync("bob", cancellationToken);

        // assert
        var summary = Assert.Single(threads);
        Assert.DoesNotContain('\n', summary.BodyPreview);
        Assert.DoesNotContain('\r', summary.BodyPreview);
        Assert.DoesNotContain("  ", summary.BodyPreview);
        Assert.True(summary.BodyPreview.Length <= MailThreadSummary.BodyPreviewMaxLength + 1);
        Assert.EndsWith("…", summary.BodyPreview);
    }

    [Fact]
    public async Task SearchAsync_Should_MatchSubjectOrBody_CaseInsensitive()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitWorkspaceAsync(cancellationToken);
        await SeedAgentAsync("bob", cancellationToken);
        await SendAsync("claude", "Deploy the PARSER fix", ["bob"], null, cancellationToken);
        await SendAsync("claude", "unrelated", ["bob"], null, cancellationToken);

        // act
        var results = await _store.SearchAsync("bob", "parser", cancellationToken);

        // assert
        var message = Assert.Single(results);
        Assert.Equal("Deploy the PARSER fix", message.Subject);
    }

    [Fact]
    public async Task SearchAsync_Should_MatchSender_CaseInsensitive()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitWorkspaceAsync(cancellationToken);
        await SeedAgentAsync("bob", cancellationToken);
        await SendAsync("claude", "unrelated subject", ["bob"], null, cancellationToken);

        // act
        var results = await _store.SearchAsync("bob", "CLAUDE", cancellationToken);

        // assert
        var message = Assert.Single(results);
        Assert.Equal("claude", message.Sender);
    }

    [Fact]
    public async Task SearchAsync_Should_NotMatch_When_TextNotInSubjectBodyOrSender()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitWorkspaceAsync(cancellationToken);
        await SeedAgentAsync("bob", cancellationToken);
        await SendAsync("claude", "unrelated subject", ["bob"], null, cancellationToken);

        // act
        var results = await _store.SearchAsync("bob", "nomatch", cancellationToken);

        // assert
        Assert.Empty(results);
    }

    [Fact]
    public async Task CountUnreadAsync_Should_CountOnlyUnreadAndNotArchived()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitWorkspaceAsync(cancellationToken);
        await SeedAgentAsync("bob", cancellationToken);
        var first = await SendAsync("claude", "first", ["bob"], null, cancellationToken);
        await SendAsync("claude", "second", ["bob"], null, cancellationToken);
        var third = await SendAsync("claude", "third", ["bob"], null, cancellationToken);
        await _store.ArchiveAsync([third.Id], "bob", cancellationToken);

        // act
        var unread = await _store.CountUnreadAsync("bob", cancellationToken);

        // assert
        Assert.Equal(2, unread);
        Assert.NotNull(first);
    }

    [Fact]
    public async Task Schema_Should_RejectSubjectOutOfLength_ViaCheckConstraint()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var connection = await SeedAsync(cancellationToken);
        await ExecuteAsync(
            connection,
            "INSERT INTO agents (name, registered_at, last_seen_at) VALUES (@n, @t, @t)",
            ("@n", "claude"), ("@t", "2026-01-10T12:00:00+00:00"));

        // act & assert
        await Assert.ThrowsAsync<SqliteException>(() => ExecuteAsync(
            connection,
            """
            INSERT INTO messages (id, thread_id, sender, subject, body, created_at)
            VALUES (@id, @id, @sender, @subject, @body, @createdAt)
            """,
            ("@id", "m-bad"), ("@sender", "claude"), ("@subject", ""),
            ("@body", "body"), ("@createdAt", "2026-01-10T12:00:00+00:00")));
    }

    [Fact]
    public async Task Schema_Should_RejectUnknownSender_ViaForeignKey()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var connection = await SeedAsync(cancellationToken);

        // act & assert
        await Assert.ThrowsAsync<SqliteException>(() => ExecuteAsync(
            connection,
            """
            INSERT INTO messages (id, thread_id, sender, subject, body, created_at)
            VALUES (@id, @id, @sender, @subject, @body, @createdAt)
            """,
            ("@id", "m-orphan"), ("@sender", "ghost"), ("@subject", "hi"),
            ("@body", "body"), ("@createdAt", "2026-01-10T12:00:00+00:00")));
    }

    [Fact]
    public async Task CountUnreadAsync_Should_Throw_When_WorkspaceVersionIsNewer()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await using (var connection = await SeedAsync(cancellationToken))
        {
            await ExecuteAsync(connection, "PRAGMA user_version = 999;");
        }

        // act & assert
        await Assert.ThrowsAsync<ExitException>(
            () => _store.CountUnreadAsync("claude", cancellationToken));
    }

    [Fact]
    public async Task QuerySentAsync_Should_ReturnMessagesSentByAgent_NewestFirst()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitWorkspaceAsync(cancellationToken);
        await SeedAgentAsync("bob", cancellationToken);
        await SeedAgentAsync("carol", cancellationToken);
        var first = await SendAsync("claude", "first", ["bob"], null, cancellationToken);
        _timeProvider.Advance(TimeSpan.FromMinutes(1));
        var second = await SendAsync("claude", "second", ["carol"], null, cancellationToken);
        await SendAsync("bob", "not from claude", ["carol"], null, cancellationToken);

        // act
        var sent = await _store.QuerySentAsync("claude", limit: null, cancellationToken);

        // assert
        Assert.Equal([second.Id, first.Id], sent.Select(m => m.Id));
    }

    [Fact]
    public async Task QuerySentAsync_Should_ApplyLimit()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitWorkspaceAsync(cancellationToken);
        await SeedAgentAsync("bob", cancellationToken);
        await SendAsync("claude", "first", ["bob"], null, cancellationToken);
        await SendAsync("claude", "second", ["bob"], null, cancellationToken);

        // act
        var sent = await _store.QuerySentAsync("claude", limit: 1, cancellationToken);

        // assert
        Assert.Single(sent);
    }

    [Fact]
    public async Task QuerySentAsync_Should_NormalizeSenderName()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitWorkspaceAsync(cancellationToken);
        await SeedAgentAsync("bob", cancellationToken);
        var message = await SendAsync("claude", "hello", ["bob"], null, cancellationToken);

        // act
        var sent = await _store.QuerySentAsync("CLAUDE", limit: null, cancellationToken);

        // assert
        var single = Assert.Single(sent);
        Assert.Equal(message.Id, single.Id);
    }

    [Fact]
    public async Task QuerySentAsync_Should_ReturnEmpty_When_AgentHasNoSentMail()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitWorkspaceAsync(cancellationToken);
        await SeedAgentAsync("bob", cancellationToken);
        await SendAsync("claude", "hello", ["bob"], null, cancellationToken);

        // act
        var sent = await _store.QuerySentAsync("bob", limit: null, cancellationToken);

        // assert
        Assert.Empty(sent);
    }

    [Fact]
    public async Task QuerySentAsync_Should_ReturnMessage_When_ActorHasNoOtherCorrespondenceWithRecipient()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitWorkspaceAsync(cancellationToken);
        await SeedAgentAsync("stranger", cancellationToken);
        var message = await SendAsync("claude", "hello", ["stranger"], null, cancellationToken);

        // act
        var sent = await _store.QuerySentAsync("claude", limit: null, cancellationToken);

        // assert
        var single = Assert.Single(sent);
        Assert.Equal(message.Id, single.Id);
    }

    [Fact]
    public async Task QuerySentAsync_Should_ExcludeMessages_When_ActorOnlyReceivedThem()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitWorkspaceAsync(cancellationToken);
        await SeedAgentAsync("bob", cancellationToken);
        var ownMessage = await SendAsync("claude", "mine", ["bob"], null, cancellationToken);
        await SendAsync("bob", "not mine", ["claude"], null, cancellationToken);

        // act
        var sent = await _store.QuerySentAsync("claude", limit: null, cancellationToken);

        // assert
        var single = Assert.Single(sent);
        Assert.Equal(ownMessage.Id, single.Id);
    }

    [Fact]
    public async Task QuerySentAsync_Should_ReturnSelfAddressedMessage_ExactlyOnce()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitWorkspaceAsync(cancellationToken);
        var message = await SendAsync("claude", "note to self", ["claude"], null, cancellationToken);

        // act
        var sent = await _store.QuerySentAsync("claude", limit: null, cancellationToken);

        // assert
        var single = Assert.Single(sent);
        Assert.Equal(message.Id, single.Id);
    }

    [Fact]
    public async Task QuerySentAsync_Should_ReturnUnrepliedThreadRoot()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitWorkspaceAsync(cancellationToken);
        await SeedAgentAsync("bob", cancellationToken);
        var root = await SendAsync("claude", "unanswered", ["bob"], null, cancellationToken);

        // act
        var sent = await _store.QuerySentAsync("claude", limit: null, cancellationToken);

        // assert
        var single = Assert.Single(sent);
        Assert.Equal(root.Id, single.Id);
        Assert.Equal(root.Id, single.ThreadId);
    }

    private static async Task ExecuteAsync(
        SqliteConnection connection,
        string sql,
        params (string Name, object Value)[] parameters)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = sql;

        foreach (var (name, value) in parameters)
        {
            command.Parameters.AddWithValue(name, value);
        }

        await command.ExecuteNonQueryAsync();
    }
}
