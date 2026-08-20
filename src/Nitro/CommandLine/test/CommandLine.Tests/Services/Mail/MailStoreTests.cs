using ChilliCream.Nitro.CommandLine.Services.Mail;
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
    private readonly MailStore _store;

    public MailStoreTests()
    {
        _tempRoot = Directory.CreateTempSubdirectory("nitro-mail-store-tests");
        _workingDirectory = Path.Combine(_tempRoot.FullName, "acme");
        Directory.CreateDirectory(_workingDirectory);
        _workspaceDirectory = MailWorkspace.GetDirectory(_workingDirectory);

        _timeProvider = new FakeTimeProvider(
            new DateTimeOffset(2026, 1, 10, 12, 0, 0, TimeSpan.Zero));

        _store = new MailStore(new TestFileSystem(_workingDirectory), _timeProvider);
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
    public async Task GetAgentsAsync_Should_Throw_When_NoWorkspaceExists()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;

        // act & assert
        await Assert.ThrowsAsync<ExitException>(
            () => _store.GetAgentsAsync(cancellationToken));
    }

    [Fact]
    public async Task RegisterAgentAsync_Should_InsertNewAgent_When_NotRegistered()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitWorkspaceAsync(cancellationToken);

        // act
        var agent = await _store.RegisterAgentAsync("Claude", cancellationToken);

        // assert
        Assert.Equal("claude", agent.Name);
        Assert.Equal(_timeProvider.GetUtcNow(), agent.RegisteredAt);
        Assert.Equal(_timeProvider.GetUtcNow(), agent.LastSeenAt);
    }

    [Fact]
    public async Task RegisterAgentAsync_Should_BumpLastSeenAt_When_AlreadyRegistered()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitWorkspaceAsync(cancellationToken);
        var first = await _store.RegisterAgentAsync("claude", cancellationToken);
        _timeProvider.Advance(TimeSpan.FromMinutes(5));

        // act
        var second = await _store.RegisterAgentAsync("claude", cancellationToken);

        // assert
        Assert.Equal(first.RegisteredAt, second.RegisteredAt);
        Assert.Equal(_timeProvider.GetUtcNow(), second.LastSeenAt);
        Assert.NotEqual(second.RegisteredAt, second.LastSeenAt);
    }

    [Fact]
    public async Task GetAgentAsync_Should_ReturnNull_When_NotRegistered()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitWorkspaceAsync(cancellationToken);

        // act
        var agent = await _store.GetAgentAsync("nobody", cancellationToken);

        // assert
        Assert.Null(agent);
    }

    [Fact]
    public async Task GetAgentsAsync_Should_ReturnAllOrderedByName()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitWorkspaceAsync(cancellationToken);
        await _store.RegisterAgentAsync("bob", cancellationToken);
        await _store.RegisterAgentAsync("alice", cancellationToken);

        // act
        var agents = await _store.GetAgentsAsync(cancellationToken);

        // assert
        Assert.Equal(["alice", "bob"], agents.Select(a => a.Name));
    }

    [Fact]
    public async Task SendMessageAsync_Should_Throw_When_RecipientUnknown()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitWorkspaceAsync(cancellationToken);

        // act
        var exception = await Assert.ThrowsAsync<ExitException>(
            () => SendAsync("claude", "hello", ["bob", "alice"], null, cancellationToken));

        // assert
        Assert.Contains("bob", exception.Message);
        Assert.Contains("alice", exception.Message);
    }

    [Fact]
    public async Task SendMessageAsync_Should_AutoRegisterSender_When_NotAlreadyRegistered()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitWorkspaceAsync(cancellationToken);
        await _store.RegisterAgentAsync("bob", cancellationToken);

        // act
        await SendAsync("claude", "hello", ["bob"], null, cancellationToken);

        // assert
        var sender = await _store.GetAgentAsync("claude", cancellationToken);
        Assert.NotNull(sender);
    }

    [Fact]
    public async Task SendMessageAsync_Should_CollapseDuplicatesWithToWinning_AndPreserveOrdinalOrder()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitWorkspaceAsync(cancellationToken);
        await _store.RegisterAgentAsync("bob", cancellationToken);
        await _store.RegisterAgentAsync("alice", cancellationToken);
        await _store.RegisterAgentAsync("carol", cancellationToken);

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
        await _store.RegisterAgentAsync("bob", cancellationToken);

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
        await _store.RegisterAgentAsync("bob", cancellationToken);
        await _store.RegisterAgentAsync("carol", cancellationToken);
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
        await _store.RegisterAgentAsync("bob", cancellationToken);
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
        await _store.RegisterAgentAsync("bob", cancellationToken);
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
        await _store.RegisterAgentAsync("bob", cancellationToken);
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
        await _store.RegisterAgentAsync("bob", cancellationToken);
        await _store.RegisterAgentAsync("carol", cancellationToken);
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
        await _store.RegisterAgentAsync("bob", cancellationToken);
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
        await _store.RegisterAgentAsync("bob", cancellationToken);
        await _store.RegisterAgentAsync("dave", cancellationToken);
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
        await _store.RegisterAgentAsync("bob", cancellationToken);
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
        await _store.RegisterAgentAsync("bob", cancellationToken);
        await SendAsync("claude", "first", ["bob"], null, cancellationToken);
        await SendAsync("claude", "second", ["bob"], null, cancellationToken);

        // act
        var inbox = await _store.QueryInboxAsync(
            new MailInboxFilter { Actor = "bob", Limit = 1 }, cancellationToken);

        // assert
        Assert.Single(inbox);
    }

    [Fact]
    public async Task MarkReadAsync_Should_SetReadAt_OnlyForActorRecipientRow()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitWorkspaceAsync(cancellationToken);
        await _store.RegisterAgentAsync("bob", cancellationToken);
        await _store.RegisterAgentAsync("carol", cancellationToken);
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
        await _store.RegisterAgentAsync("bob", cancellationToken);
        await _store.RegisterAgentAsync("carol", cancellationToken);
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
        await _store.RegisterAgentAsync("bob", cancellationToken);
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
        await _store.RegisterAgentAsync("bob", cancellationToken);
        await _store.RegisterAgentAsync("carol", cancellationToken);
        var thread = await SendAsync("claude", "shared thread", ["bob"], null, cancellationToken);
        await SendAsync("claude", "other thread", ["carol"], null, cancellationToken);

        // act
        var threads = await _store.QueryThreadsAsync("bob", cancellationToken);

        // assert
        var summary = Assert.Single(threads);
        Assert.Equal(thread.ThreadId, summary.ThreadId);
        Assert.Equal(1, summary.UnreadCount);
    }

    [Fact]
    public async Task SearchAsync_Should_MatchSubjectOrBody_CaseInsensitive()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitWorkspaceAsync(cancellationToken);
        await _store.RegisterAgentAsync("bob", cancellationToken);
        await SendAsync("claude", "Deploy the PARSER fix", ["bob"], null, cancellationToken);
        await SendAsync("claude", "unrelated", ["bob"], null, cancellationToken);

        // act
        var results = await _store.SearchAsync("bob", "parser", cancellationToken);

        // assert
        var message = Assert.Single(results);
        Assert.Equal("Deploy the PARSER fix", message.Subject);
    }

    [Fact]
    public async Task CountUnreadAsync_Should_CountOnlyUnreadAndNotArchived()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitWorkspaceAsync(cancellationToken);
        await _store.RegisterAgentAsync("bob", cancellationToken);
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
    public async Task GetAgentsAsync_Should_Throw_When_WorkspaceVersionIsNewer()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await using (var connection = await SeedAsync(cancellationToken))
        {
            await ExecuteAsync(connection, "PRAGMA user_version = 999;");
        }

        // act & assert
        await Assert.ThrowsAsync<ExitException>(
            () => _store.GetAgentsAsync(cancellationToken));
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
