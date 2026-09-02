using ChilliCream.Nitro.CommandLine.Services.Mail;
using ChilliCream.Nitro.CommandLine.Services.Workspace;
using ChilliCream.Nitro.CommandLine.Tests.Commands;
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
    private const string InstanceId = "instance-a";

    private readonly DirectoryInfo _tempRoot;
    private readonly string _workingDirectory;
    private readonly string _workspaceDirectory;
    private readonly FakeTimeProvider _timeProvider;
    private readonly AgentRegistry _registry;
    private readonly AgentDatabase _database;
    private readonly MailStore _store;

    public MailStoreTests()
    {
        _tempRoot = Directory.CreateTempSubdirectory("nitro-mail-store-tests");
        _workingDirectory = Path.Combine(_tempRoot.FullName, "acme");
        Directory.CreateDirectory(_workingDirectory);
        _workspaceDirectory = AgentWorkspace.GetDirectory(_workingDirectory);

        _timeProvider = new FakeTimeProvider(
            new DateTimeOffset(2026, 1, 10, 12, 0, 0, TimeSpan.Zero));

        _database = new AgentDatabase();
        _registry = new AgentRegistry(new TestFileSystem(_workingDirectory), _timeProvider, _database);
        _store = CreateStore();
    }

    /// <summary>
    /// Creates a new <see cref="MailStore"/> bound to this test's file
    /// system, clock, database, and registry, with a fixed instance id and
    /// global config directory so <see cref="MailWakePolicy.Enqueue"/> can
    /// resolve without touching the real machine. Concurrency tests create
    /// one of these per racing caller, mirroring
    /// <c>MailWakeBatchStoreTests</c>' "separate connections racing the same
    /// file" shape.
    /// </summary>
    private MailStore CreateStore()
        => new(
            new TestFileSystem(_workingDirectory),
            _timeProvider,
            _database,
            _registry,
            new FixedInstanceIdProvider(InstanceId),
            new FixedGlobalConfigDirectoryProvider(_workingDirectory));

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
        string body = "body",
        MailWakePolicy wakePolicy = MailWakePolicy.Skip)
        => _store.SendMessageAsync(
            new MailMessageCreation
            {
                Sender = sender,
                Subject = subject,
                Body = body,
                To = to,
                Cc = cc ?? [],
                WakePolicy = wakePolicy
            },
            cancellationToken);

    /// <summary>
    /// Reads one <c>mail_wake_outbox</c> row's generation and due columns
    /// directly, for asserting the transactional side effect
    /// <see cref="MailWakePolicy.Enqueue"/> has no public read surface for.
    /// Returns null when no row exists for (<see cref="InstanceId"/>,
    /// <paramref name="actor"/>).
    /// </summary>
    private async Task<(long RequestedGeneration, long SettledGeneration, DateTimeOffset DueAt)?> ReadOutboxRowAsync(
        string actor, CancellationToken cancellationToken)
    {
        await using var connection = await SeedAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText =
            """
            SELECT requested_generation, settled_generation, due_at FROM mail_wake_outbox
            WHERE nitro_instance_id = @instanceId AND actor = @actor
            """;
        command.Parameters.AddWithValue("@instanceId", InstanceId);
        command.Parameters.AddWithValue("@actor", actor);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        if (!await reader.ReadAsync(cancellationToken))
        {
            return null;
        }

        return (
            reader.GetInt64(0),
            reader.GetInt64(1),
            DateTimeOffset.Parse(reader.GetString(2), System.Globalization.CultureInfo.InvariantCulture));
    }

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
    public async Task SendMessageAsync_Should_ReturnWakeReceiptForEachRecipient_When_PolicyIsEnqueue()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitWorkspaceAsync(cancellationToken);
        await SeedAgentAsync("bob", cancellationToken);
        await SeedAgentAsync("carol", cancellationToken);

        // act
        var message = await SendAsync(
            "claude", "hello", ["bob"], ["carol"], cancellationToken, wakePolicy: MailWakePolicy.Enqueue);

        // assert
        Assert.Equal(
            [("bob", 1L), ("carol", 1L)],
            message.WakeReceipts.Select(r => (r.Actor, r.Generation)));
        var bobRow = await ReadOutboxRowAsync("bob", cancellationToken);
        Assert.Equal((1L, 0L), (bobRow!.Value.RequestedGeneration, bobRow.Value.SettledGeneration));
    }

    [Fact]
    public async Task SendMessageAsync_Should_ReturnNoWakeReceiptsAndCreateNoOutboxRow_When_PolicyIsSkip()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitWorkspaceAsync(cancellationToken);
        await SeedAgentAsync("bob", cancellationToken);

        // act
        var message = await SendAsync("claude", "hello", ["bob"], null, cancellationToken);

        // assert: the default policy is Skip, matching every other test in
        // this file that does not pass wakePolicy explicitly.
        Assert.Empty(message.WakeReceipts);
        Assert.Null(await ReadOutboxRowAsync("bob", cancellationToken));
    }

    [Fact]
    public async Task SendMessageAsync_Should_AdvanceGenerationMonotonically_When_SentTwiceToSameRecipient()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitWorkspaceAsync(cancellationToken);
        await SeedAgentAsync("bob", cancellationToken);
        var first = await SendAsync(
            "claude", "first", ["bob"], null, cancellationToken, wakePolicy: MailWakePolicy.Enqueue);

        // act
        var second = await SendAsync(
            "claude", "second", ["bob"], null, cancellationToken, wakePolicy: MailWakePolicy.Enqueue);

        // assert
        Assert.Equal(1, Assert.Single(first.WakeReceipts).Generation);
        Assert.Equal(2, Assert.Single(second.WakeReceipts).Generation);
    }

    [Fact]
    public async Task ReplyMessageAsync_Should_ReturnWakeReceipts_When_PolicyIsEnqueue()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitWorkspaceAsync(cancellationToken);
        await SeedAgentAsync("bob", cancellationToken);
        var original = await SendAsync("claude", "hello", ["bob"], null, cancellationToken);

        // act
        var reply = await _store.ReplyMessageAsync(
            original.Id, "bob", "reply body", MailWakePolicy.Enqueue, cancellationToken);

        // assert: bob replies, so the reply's only recipient is claude.
        var receipt = Assert.Single(reply.WakeReceipts);
        Assert.Equal("claude", receipt.Actor);
        Assert.Equal(1, receipt.Generation);
    }

    [Fact]
    public async Task SendMessageAsync_Should_PreserveEarliestDueAt_When_EnqueuedTwiceBeforeSettling()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitWorkspaceAsync(cancellationToken);
        await SeedAgentAsync("bob", cancellationToken);
        await SendAsync("claude", "first", ["bob"], null, cancellationToken, wakePolicy: MailWakePolicy.Enqueue);
        var earliestDueAt = _timeProvider.GetUtcNow();
        _timeProvider.Advance(TimeSpan.FromMinutes(5));

        // act: settled_generation is still 0 (no batch has completed), so
        // this second enqueue must not push due_at later than the first.
        await SendAsync("claude", "second", ["bob"], null, cancellationToken, wakePolicy: MailWakePolicy.Enqueue);

        // assert
        var row = await ReadOutboxRowAsync("bob", cancellationToken);
        Assert.Equal(2L, row!.Value.RequestedGeneration);
        Assert.Equal(earliestDueAt, row.Value.DueAt);
    }

    [Fact]
    public async Task SendMessageAsync_Should_AdvanceGenerationsWithoutLoss_When_ConcurrentSendsRaceTheSameRecipient()
    {
        // arrange: separate MailStore instances (Pooling=False, matching
        // production) racing the same file for the same recipient, mirroring
        // MailWakeBatchStoreTests' concurrency shape.
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitWorkspaceAsync(cancellationToken);
        await SeedAgentAsync("bob", cancellationToken);
        const int concurrentSends = 5;

        // act
        var messages = await Task.WhenAll(Enumerable.Range(1, concurrentSends).Select(i =>
            CreateStore().SendMessageAsync(
                new MailMessageCreation
                {
                    Sender = "claude",
                    Subject = $"concurrent {i}",
                    Body = "body",
                    To = ["bob"],
                    WakePolicy = MailWakePolicy.Enqueue
                },
                cancellationToken)));

        // assert: every generation from 1 through concurrentSends was
        // handed out exactly once - no lost update collapsed two sends onto
        // the same generation.
        var generations = messages.Select(m => Assert.Single(m.WakeReceipts).Generation).Order().ToArray();
        Assert.Equal(Enumerable.Range(1, concurrentSends).Select(i => (long)i), generations);
        var row = await ReadOutboxRowAsync("bob", cancellationToken);
        Assert.Equal((long)concurrentSends, row!.Value.RequestedGeneration);
    }

    [Fact]
    public async Task SendMessageAsync_Should_RollBackMessageAndRecipients_When_WakeOutboxWriteFailsMidTransaction()
    {
        // arrange: an injected constraint failure - a trigger that aborts
        // the wake-outbox write for one specific actor - proves the message,
        // its recipient row, and the wake increment commit as a single unit:
        // none of them exist afterward, not just the outbox row.
        var cancellationToken = TestContext.Current.CancellationToken;
        await using (var connection = await SeedAsync(cancellationToken))
        {
            await ExecuteAsync(
                connection,
                """
                CREATE TRIGGER trg_fail_wake_outbox_insert
                BEFORE INSERT ON mail_wake_outbox
                FOR EACH ROW WHEN NEW.actor = 'boom'
                BEGIN
                    SELECT RAISE(ABORT, 'injected failure');
                END;
                """);
        }

        await InitWorkspaceAsync(cancellationToken);
        await SeedAgentAsync("boom", cancellationToken);

        // act & assert
        await Assert.ThrowsAsync<SqliteException>(
            () => SendAsync(
                "claude", "doomed", ["boom"], null, cancellationToken, wakePolicy: MailWakePolicy.Enqueue));

        Assert.Equal(0L, await CountAsync("messages", "subject = 'doomed'", cancellationToken));
        Assert.Equal(0L, await CountAsync("message_recipients", "recipient = 'boom'", cancellationToken));
        Assert.Null(await ReadOutboxRowAsync("boom", cancellationToken));
    }

    [Fact]
    public async Task SendMessageAsync_Should_PersistNothing_When_CancelledBeforeAnyWriteObservesIt()
    {
        // arrange: an already-cancelled token proves the "cancel before
        // rollback" case - nothing about this send, including the wake
        // increment, is observable afterward.
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitWorkspaceAsync(cancellationToken);
        await SeedAgentAsync("bob", cancellationToken);
        using var cancelled = new CancellationTokenSource();
        await cancelled.CancelAsync();

        // act & assert
        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => SendAsync(
                "claude", "never sent", ["bob"], null, cancelled.Token, wakePolicy: MailWakePolicy.Enqueue));

        Assert.Equal(0L, await CountAsync("messages", "subject = 'never sent'", cancellationToken));
        Assert.Null(await ReadOutboxRowAsync("bob", cancellationToken));
    }

    [Fact]
    public async Task SendMessageAsync_Should_BeReconcilableByItsReturnedId_When_CommitSucceeds()
    {
        // arrange: proves the mechanism a caller uses to reconcile an
        // ambiguous outcome (a cancellation or transport failure observed
        // only after the commit already landed) - re-querying by the id the
        // store handed back always reflects the true committed state,
        // message, recipients, and wake generation alike.
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitWorkspaceAsync(cancellationToken);
        await SeedAgentAsync("bob", cancellationToken);

        // act
        var sent = await SendAsync(
            "claude", "hello", ["bob"], null, cancellationToken, wakePolicy: MailWakePolicy.Enqueue);
        var reconciled = await _store.GetRequiredMessageAsync(sent.Id, cancellationToken);
        var row = await ReadOutboxRowAsync("bob", cancellationToken);

        // assert
        Assert.Equal(sent.Id, reconciled.Id);
        Assert.Equal(sent.Subject, reconciled.Subject);
        Assert.Equal(["bob"], reconciled.Recipients.Select(r => r.Name));
        Assert.Equal(1L, Assert.Single(sent.WakeReceipts).Generation);
        Assert.Equal(1L, row!.Value.RequestedGeneration);
    }

    /// <summary>
    /// Counts rows in <paramref name="table"/> matching
    /// <paramref name="whereClause"/>, both compile-time literals at every
    /// call site (never user input), for asserting a rolled-back write left
    /// nothing behind.
    /// </summary>
    private async Task<long> CountAsync(string table, string whereClause, CancellationToken cancellationToken)
    {
        await using var connection = await SeedAsync(cancellationToken);

        return await ExecuteScalarAsync(connection, $"SELECT COUNT(*) FROM {table} WHERE {whereClause}");
    }

    private static async Task<long> ExecuteScalarAsync(SqliteConnection connection, string sql)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = sql;

        return (long)(await command.ExecuteScalarAsync())!;
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
    public async Task TransferParticipationAsync_Should_MoveRecipientAndPreserveReadState()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitWorkspaceAsync(cancellationToken);
        await SeedAgentAsync("old", cancellationToken);
        await SeedAgentAsync("target", cancellationToken);
        var targetMessage = await SendAsync("claude", "target", ["target"], null, cancellationToken);
        var unreadMessage = await SendAsync("claude", "unread", ["old"], null, cancellationToken);
        var readMessage = await SendAsync("claude", "read", ["old"], null, cancellationToken);
        await _store.MarkReadAsync([targetMessage.Id], "target", cancellationToken);
        await _store.MarkReadAsync([readMessage.Id], "old", cancellationToken);

        // act
        var result = await _store.TransferParticipationAsync("OLD", "TARGET", cancellationToken);
        var inbox = await _store.QueryInboxAsync(new MailInboxFilter { Actor = "target" }, cancellationToken);
        var sourceInbox = await _store.QueryInboxAsync(new MailInboxFilter { Actor = "old" }, cancellationToken);

        // assert
        Assert.Equal(new MailTransferResult(2, 0, 0), result);
        Assert.Equal(
            [readMessage.Id, targetMessage.Id, unreadMessage.Id].Order(),
            inbox.Select(t => t.Id).Order());
        Assert.Equal(
            [true, true, false],
            inbox.OrderBy(t => t.Subject).Select(t => t.Recipients.Single().ReadAt is not null));
        Assert.Empty(sourceInbox);
    }

    [Fact]
    public async Task TransferParticipationAsync_Should_AllowThirdPartyReplyToTransferredSentMessage()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitWorkspaceAsync(cancellationToken);
        await SeedAgentAsync("old", cancellationToken);
        await SeedAgentAsync("target", cancellationToken);
        await SeedAgentAsync("bob", cancellationToken);
        var message = await SendAsync("old", "sent", ["bob"], null, cancellationToken);
        var transfer = await _store.TransferParticipationAsync("old", "target", cancellationToken);

        // act
        var reply = await _store.ReplyMessageAsync(message.Id, "bob", "reply", cancellationToken);

        // assert
        Assert.Equal(new MailTransferResult(0, 1, 0), transfer);
        Assert.Equal("bob", reply.Sender);
        Assert.Equal(["target"], reply.Recipients.Select(t => t.Name));
        Assert.Equal(message.ThreadId, reply.ThreadId);
    }

    [Fact]
    public async Task TransferParticipationAsync_Should_AllowTargetReplyToTransferredReceivedMessage()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitWorkspaceAsync(cancellationToken);
        await SeedAgentAsync("old", cancellationToken);
        await SeedAgentAsync("target", cancellationToken);
        var message = await SendAsync("claude", "received", ["old"], null, cancellationToken);
        await _store.TransferParticipationAsync("old", "target", cancellationToken);

        // act
        var reply = await _store.ReplyMessageAsync(message.Id, "target", "reply", cancellationToken);

        // assert
        Assert.Equal("target", reply.Sender);
        Assert.Equal(["claude"], reply.Recipients.Select(t => t.Name));
        Assert.Equal(message.ThreadId, reply.ThreadId);
    }

    [Fact]
    public async Task TransferParticipationAsync_Should_DropConflictingRecipientAndPreserveTargetState()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitWorkspaceAsync(cancellationToken);
        await SeedAgentAsync("old", cancellationToken);
        await SeedAgentAsync("target", cancellationToken);
        var message = await SendAsync("claude", "shared", ["old", "target"], null, cancellationToken);
        await _store.MarkReadAsync([message.Id], "target", cancellationToken);

        // act
        var result = await _store.TransferParticipationAsync("old", "target", cancellationToken);
        var reloaded = await _store.GetRequiredMessageAsync(message.Id, cancellationToken);

        // assert
        Assert.Equal(new MailTransferResult(0, 0, 1), result);
        var recipient = Assert.Single(reloaded.Recipients);
        Assert.Equal("target", recipient.Name);
        Assert.NotNull(recipient.ReadAt);
    }

    [Fact]
    public async Task TransferParticipationAsync_Should_IncludeTransferredParticipationInThreads()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitWorkspaceAsync(cancellationToken);
        await SeedAgentAsync("old", cancellationToken);
        await SeedAgentAsync("target", cancellationToken);
        await SeedAgentAsync("bob", cancellationToken);
        var sent = await SendAsync("old", "sent", ["bob"], null, cancellationToken);
        var received = await SendAsync("claude", "received", ["old"], null, cancellationToken);
        await _store.TransferParticipationAsync("old", "target", cancellationToken);

        // act
        var threads = await _store.QueryThreadsAsync("target", cancellationToken);

        // assert
        Assert.Equal(2, threads.Count);
        Assert.Contains(threads, t => t.ThreadId == sent.ThreadId);
        Assert.Contains(threads, t => t.ThreadId == received.ThreadId);
    }

    [Fact]
    public async Task TransferParticipationAsync_Should_IncludeTransferredParticipationInSearch()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitWorkspaceAsync(cancellationToken);
        await SeedAgentAsync("old", cancellationToken);
        await SeedAgentAsync("target", cancellationToken);
        await SeedAgentAsync("bob", cancellationToken);
        var sent = await SendAsync("old", "matching", ["bob"], null, cancellationToken);
        var received = await SendAsync("claude", "matching", ["old"], null, cancellationToken);
        await _store.TransferParticipationAsync("old", "target", cancellationToken);

        // act
        var results = await _store.SearchAsync("target", "matching", cancellationToken);

        // assert
        Assert.Equal(2, results.Count);
        Assert.Contains(results, m => m.Id == sent.Id);
        Assert.Contains(results, m => m.Id == received.Id);
    }

    [Fact]
    public async Task TransferParticipationAsync_Should_ReturnZeros_WhenRepeated()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitWorkspaceAsync(cancellationToken);
        await SeedAgentAsync("old", cancellationToken);
        await SeedAgentAsync("target", cancellationToken);
        await SeedAgentAsync("bob", cancellationToken);
        await SendAsync("old", "sent", ["bob"], null, cancellationToken);
        await SendAsync("claude", "received", ["old"], null, cancellationToken);
        await _store.TransferParticipationAsync("old", "target", cancellationToken);

        // act
        var result = await _store.TransferParticipationAsync("old", "target", cancellationToken);

        // assert
        Assert.Equal(new MailTransferResult(0, 0, 0), result);
    }

    [Fact]
    public async Task TransferParticipationAsync_Should_Throw_When_AgentsAreTheSame()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitWorkspaceAsync(cancellationToken);
        await SeedAgentAsync("target", cancellationToken);

        // act & assert
        await Assert.ThrowsAsync<ExitException>(
            () => _store.TransferParticipationAsync("target", "TARGET", cancellationToken));
    }

    [Fact]
    public async Task TransferParticipationAsync_Should_Throw_When_TargetAgentDoesNotExist()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitWorkspaceAsync(cancellationToken);
        await SeedAgentAsync("old", cancellationToken);

        // act & assert
        await Assert.ThrowsAsync<ExitException>(
            () => _store.TransferParticipationAsync("old", "target", cancellationToken));
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
        var threads = await _store.QueryInboxThreadsAsync("bob", includeArchived: false, cancellationToken);

        // assert
        var summary = Assert.Single(threads);
        Assert.Equal(addressedToBob.ThreadId, summary.ThreadId);
        Assert.Equal(1, summary.UnreadCount);
        Assert.Equal(0, summary.ArchivedCount);
        Assert.Equal(1, summary.MessageCount);
        Assert.Equal("body", summary.BodyPreview);
    }

    [Fact]
    public async Task QueryInboxThreadsAsync_Should_ExcludeThread_When_OnlyMessageToActorIsArchived()
    {
        // arrange: bob's only message in this thread is archived for him, so
        // the default (includeArchived: false) query excludes the thread,
        // matching BuildInboxQuery's message-level semantics.
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitWorkspaceAsync(cancellationToken);
        await SeedAgentAsync("bob", cancellationToken);
        var message = await SendAsync("claude", "for bob", ["bob"], null, cancellationToken);
        await _store.ArchiveAsync([message.Id], "bob", cancellationToken);

        // act
        var threads = await _store.QueryInboxThreadsAsync("bob", includeArchived: false, cancellationToken);

        // assert
        Assert.Empty(threads);
    }

    [Fact]
    public async Task QueryInboxThreadsAsync_Should_IncludeArchivedThread_When_IncludeArchivedTrue()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitWorkspaceAsync(cancellationToken);
        await SeedAgentAsync("bob", cancellationToken);
        var message = await SendAsync("claude", "for bob", ["bob"], null, cancellationToken);
        await _store.ArchiveAsync([message.Id], "bob", cancellationToken);

        // act
        var threads = await _store.QueryInboxThreadsAsync("bob", includeArchived: true, cancellationToken);

        // assert
        var summary = Assert.Single(threads);
        Assert.Equal(message.ThreadId, summary.ThreadId);
        Assert.Equal(1, summary.ArchivedCount);
    }

    [Fact]
    public async Task QueryInboxThreadsAsync_Should_IncludeThread_When_SomeButNotAllMessagesToActorAreArchived()
    {
        // arrange: bob is addressed by two separate messages in the same
        // thread (claude replying into his own first message, still to
        // bob), only one archived - the default query keeps the thread (it
        // is not the case that "only" his messages are archived) and
        // ArchivedCount reports 1.
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitWorkspaceAsync(cancellationToken);
        await SeedAgentAsync("bob", cancellationToken);
        var first = await SendAsync("claude", "for bob", ["bob"], null, cancellationToken);
        var reply = await _store.ReplyMessageAsync(first.Id, "claude", "following up", cancellationToken);
        await _store.ArchiveAsync([first.Id], "bob", cancellationToken);

        // act
        var threads = await _store.QueryInboxThreadsAsync("bob", includeArchived: false, cancellationToken);

        // assert
        var summary = Assert.Single(threads);
        Assert.Equal(reply.ThreadId, summary.ThreadId);
        Assert.Equal(2, summary.MessageCount);
        Assert.Equal(1, summary.ArchivedCount);
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
        Assert.Equal(1, summary.MessageCount);
        Assert.Equal("body", summary.BodyPreview);
    }

    [Fact]
    public async Task QuerySentThreadsAsync_Should_ReportNonZeroUnreadCount_When_OtherAgentRepliedInThread()
    {
        // arrange: bob's unread count on his own Sent thread is normally 0
        // (see QuerySentThreadsAsync_Should_ReturnOnlyThreadsActorSentInto),
        // but the doc contract on IMailStore.QuerySentThreadsAsync says it
        // "can be non-zero when other agents replied in a thread the actor
        // started" - carol's reply addresses bob, so bob has an unread
        // recipient row on the thread he sent into.
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitWorkspaceAsync(cancellationToken);
        await SeedAgentAsync("bob", cancellationToken);
        await SeedAgentAsync("carol", cancellationToken);
        var sentByBob = await SendAsync("bob", "from bob", ["carol"], null, cancellationToken);
        await _store.ReplyMessageAsync(sentByBob.Id, "carol", "reply body", cancellationToken);

        // act
        var threads = await _store.QuerySentThreadsAsync("bob", cancellationToken);

        // assert
        var summary = Assert.Single(threads);
        Assert.Equal(sentByBob.ThreadId, summary.ThreadId);
        Assert.Equal(1, summary.UnreadCount);
        Assert.Equal(2, summary.MessageCount);
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
        Assert.Equal(1, summary.MessageCount);
        Assert.Equal("body", summary.BodyPreview);
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
