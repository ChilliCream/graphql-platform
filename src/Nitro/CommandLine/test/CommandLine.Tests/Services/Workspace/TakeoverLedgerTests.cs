using ChilliCream.Nitro.CommandLine.Services.Workspace;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Time.Testing;

namespace ChilliCream.Nitro.CommandLine.Tests.Agents;

public sealed class TakeoverLedgerTests : IDisposable
{
    private readonly DirectoryInfo _tempRoot;
    private readonly string _workspaceDirectory;
    private readonly FakeTimeProvider _timeProvider;
    private readonly TakeoverLedger _ledger;

    public TakeoverLedgerTests()
    {
        _tempRoot = Directory.CreateTempSubdirectory("nitro-takeover-ledger-tests");
        var workingDirectory = Path.Combine(_tempRoot.FullName, "acme");
        Directory.CreateDirectory(workingDirectory);
        _workspaceDirectory = AgentWorkspace.GetDirectory(workingDirectory);
        _timeProvider = new FakeTimeProvider(new DateTimeOffset(2026, 1, 10, 12, 0, 0, TimeSpan.Zero));
        _ledger = new TakeoverLedger(
            new TestFileSystem(workingDirectory),
            _timeProvider,
            new AgentDatabase());
    }

    public void Dispose() => _tempRoot.Delete(recursive: true);

    [Fact]
    public async Task RecordAsync_Should_RecordHeaderAndItems_When_ItemsAreProvided()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitializeWorkspaceAsync(cancellationToken);

        // act
        var record = await _ledger.RecordAsync(
            CreateRecord("maya", "nora", "maya"),
            [
                new TakeoverItem { Kind = TakeoverItemKinds.MessageSender, ItemId = "m-1" },
                new TakeoverItem { Kind = TakeoverItemKinds.Task, ItemId = "repo-cpf" }
            ],
            cancellationToken);

        // assert
        Assert.StartsWith("to-", record.Id);
        Assert.Equal("maya", record.FromActor);
        Assert.Equal("nora", record.ToActor);
        Assert.Collection(
            record.Items,
            item => Assert.Equal((TakeoverItemKinds.MessageSender, "m-1"), (item.Kind, item.ItemId)),
            item => Assert.Equal((TakeoverItemKinds.Task, "repo-cpf"), (item.Kind, item.ItemId)));
    }

    [Fact]
    public async Task RecordAsync_Should_RecordHeader_When_ItemsAreEmpty()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitializeWorkspaceAsync(cancellationToken);

        // act
        var record = await _ledger.RecordAsync(
            CreateRecord("maya", "nora", "maya"), [], cancellationToken);
        var records = await _ledger.QueryAsync(new TakeoverFilter(), cancellationToken);

        // assert
        Assert.Equal(record.Id, Assert.Single(records).Id);
        Assert.Empty(records[0].Items);
    }

    [Fact]
    public async Task QueryAsync_Should_MatchActorOnEitherSide_When_ActorIsFiltered()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitializeWorkspaceAsync(cancellationToken);
        await _ledger.RecordAsync(CreateRecord("maya", "nora", "maya"), [], cancellationToken);
        _timeProvider.Advance(TimeSpan.FromMinutes(1));
        await _ledger.RecordAsync(CreateRecord("nora", "jules", "nora"), [], cancellationToken);
        _timeProvider.Advance(TimeSpan.FromMinutes(1));
        await _ledger.RecordAsync(CreateRecord("jules", "sam", "jules"), [], cancellationToken);

        // act
        var records = await _ledger.QueryAsync(new TakeoverFilter { Actor = "nora" }, cancellationToken);

        // assert
        Assert.Collection(
            records,
            record => Assert.Equal(("nora", "jules"), (record.FromActor, record.ToActor)),
            record => Assert.Equal(("maya", "nora"), (record.FromActor, record.ToActor)));
    }

    [Fact]
    public async Task QueryAsync_Should_ReturnEveryMessageHop_When_MessageIsFiltered()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitializeWorkspaceAsync(cancellationToken);
        await _ledger.RecordAsync(
            CreateRecord("maya", "nora", "maya"),
            [new TakeoverItem { Kind = TakeoverItemKinds.MessageSender, ItemId = "m-chain" }],
            cancellationToken);
        _timeProvider.Advance(TimeSpan.FromMinutes(1));
        await _ledger.RecordAsync(
            CreateRecord("nora", "jules", "nora"),
            [new TakeoverItem { Kind = TakeoverItemKinds.MessageRecipient, ItemId = "m-chain" }],
            cancellationToken);

        // act
        var records = await _ledger.QueryAsync(new TakeoverFilter { MessageId = "m-chain" }, cancellationToken);

        // assert
        Assert.Collection(
            records,
            record => Assert.Equal(("nora", "jules"), (record.FromActor, record.ToActor)),
            record => Assert.Equal(("maya", "nora"), (record.FromActor, record.ToActor)));
    }

    [Fact]
    public async Task QueryAsync_Should_ReturnTaskTakeovers_When_TaskIsFiltered()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitializeWorkspaceAsync(cancellationToken);
        await _ledger.RecordAsync(
            CreateRecord("maya", "nora", "maya"),
            [new TakeoverItem { Kind = TakeoverItemKinds.Task, ItemId = "repo-cpf" }],
            cancellationToken);
        await _ledger.RecordAsync(
            CreateRecord("nora", "jules", "nora"),
            [new TakeoverItem { Kind = TakeoverItemKinds.Task, ItemId = "repo-other" }],
            cancellationToken);

        // act
        var records = await _ledger.QueryAsync(new TakeoverFilter { TaskId = "repo-cpf" }, cancellationToken);

        // assert
        Assert.Equal(("maya", "nora"), (Assert.Single(records).FromActor, records[0].ToActor));
    }

    [Fact]
    public void TakeoverFilter_Should_Throw_When_LimitIsNegative()
    {
        // act & assert
        Assert.Throws<ArgumentOutOfRangeException>(() => new TakeoverFilter { Limit = -1 });
    }

    [Fact]
    public async Task QueryAsync_Should_ReturnNoRecords_When_LimitIsZero()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitializeWorkspaceAsync(cancellationToken);
        await _ledger.RecordAsync(CreateRecord("maya", "nora", "maya"), [], cancellationToken);

        // act
        var records = await _ledger.QueryAsync(new TakeoverFilter { Limit = 0 }, cancellationToken);

        // assert
        Assert.Empty(records);
    }

    [Fact]
    public async Task QueryAsync_Should_ReturnNewestRecords_When_LimitIsPositive()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitializeWorkspaceAsync(cancellationToken);
        await _ledger.RecordAsync(CreateRecord("maya", "nora", "maya"), [], cancellationToken);
        _timeProvider.Advance(TimeSpan.FromMinutes(1));
        await _ledger.RecordAsync(CreateRecord("nora", "jules", "nora"), [], cancellationToken);
        _timeProvider.Advance(TimeSpan.FromMinutes(1));
        await _ledger.RecordAsync(CreateRecord("jules", "sam", "jules"), [], cancellationToken);

        // act
        var records = await _ledger.QueryAsync(new TakeoverFilter { Limit = 2 }, cancellationToken);

        // assert
        Assert.Collection(
            records,
            record => Assert.Equal(("jules", "sam"), (record.FromActor, record.ToActor)),
            record => Assert.Equal(("nora", "jules"), (record.FromActor, record.ToActor)));
    }

    [Fact]
    public async Task RecordAsync_Should_RollBackHeaderAndItems_When_ItemInsertFails()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitializeWorkspaceAsync(cancellationToken);
        var duplicateItem = new TakeoverItem { Kind = TakeoverItemKinds.Task, ItemId = "repo-cpf" };

        // act
        var exception = await Record.ExceptionAsync(
            () => _ledger.RecordAsync(
                CreateRecord("maya", "nora", "maya"),
                [duplicateItem, duplicateItem],
                cancellationToken));

        // assert
        Assert.IsType<SqliteException>(exception);
        await using var connection = await new AgentDatabase().ConnectAsync(
            _workspaceDirectory,
            cancellationToken);
        var headerCount = await ExecuteScalarAsync(
            connection,
            "SELECT COUNT(*) FROM agent_takeovers",
            cancellationToken);
        var itemCount = await ExecuteScalarAsync(
            connection,
            "SELECT COUNT(*) FROM agent_takeover_items",
            cancellationToken);
        Assert.Equal((0L, 0L), (headerCount, itemCount));
    }

    private async Task InitializeWorkspaceAsync(CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(_workspaceDirectory);
        await new AgentDatabase().InitializeAsync(_workspaceDirectory, cancellationToken);
    }

    private static TakeoverRecordCreation CreateRecord(string fromActor, string toActor, string actor)
        => new()
        {
            FromActor = fromActor,
            ToActor = toActor,
            Actor = actor,
            Forced = true,
            Role = "implementer",
            Reason = "handoff"
        };

    private static async Task<long> ExecuteScalarAsync(
        SqliteConnection connection,
        string commandText,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = commandText;
        return (long)(await command.ExecuteScalarAsync(cancellationToken))!;
    }
}
