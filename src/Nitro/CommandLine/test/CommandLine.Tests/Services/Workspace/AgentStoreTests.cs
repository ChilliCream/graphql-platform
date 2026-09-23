using ChilliCream.Nitro.CommandLine.Services.Workspace;
using Microsoft.Extensions.Time.Testing;

namespace ChilliCream.Nitro.CommandLine.Tests.Agents;

/// <summary>
/// Exercises <see cref="AgentStore"/> against a real SQLite workspace: login, harness
/// session start, touch, end, role, and lookup semantics over the unified agents table.
/// </summary>
public sealed class AgentStoreTests : IDisposable
{
    private const string Harness = AgentSessionHarness.ClaudeCode;
    private const string SessionId = "session-1";

    private readonly DirectoryInfo _tempRoot;
    private readonly string _workingDirectory;
    private readonly string _workspaceDirectory;
    private readonly FakeTimeProvider _timeProvider;
    private readonly AgentStore _store;

    public AgentStoreTests()
    {
        _tempRoot = Directory.CreateTempSubdirectory("nitro-agent-store-tests");
        _workingDirectory = Path.Combine(_tempRoot.FullName, "acme");
        Directory.CreateDirectory(_workingDirectory);
        _workspaceDirectory = AgentWorkspace.GetDirectory(_workingDirectory);

        _timeProvider = new FakeTimeProvider(
            new DateTimeOffset(2026, 1, 10, 12, 0, 0, TimeSpan.Zero));

        _store = new AgentStore(new TestFileSystem(_workingDirectory), _timeProvider, new AgentDatabase());
    }

    public void Dispose() => _tempRoot.Delete(recursive: true);

    private async Task InitWorkspaceAsync(CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(_workspaceDirectory);
        await new AgentDatabase().InitializeAsync(_workspaceDirectory, cancellationToken);
    }

    private static AgentSessionStartRequest CreateRequest(
        string harness = Harness,
        string sessionId = SessionId,
        string harnessVersion = "1.0.0",
        string cwd = "/repo",
        string workspacePath = "/repo/.nitro/agents",
        string endpointKind = AgentSessionEndpointKind.ClaudePeer,
        string endpointAddr = "peer-addr",
        string? endpointSecret = null) => new()
        {
            Harness = harness,
            SessionId = sessionId,
            HarnessVersion = harnessVersion,
            Cwd = cwd,
            WorkspacePath = workspacePath,
            EndpointKind = endpointKind,
            EndpointAddr = endpointAddr,
            EndpointSecret = endpointSecret
        };

    /// <summary>
    /// Soft-deletes the named agent directly, bypassing the store, so tests can exercise
    /// the deleted-row behavior this ticket's delete mechanics have not implemented yet.
    /// </summary>
    private async Task MarkDeletedAsync(string name, CancellationToken cancellationToken)
    {
        await using var connection = await new AgentDatabase().ConnectAsync(_workspaceDirectory, cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = "UPDATE agents SET deleted_at = @now WHERE name = @name";
        command.Parameters.AddWithValue("@now", _timeProvider.GetUtcNow());
        command.Parameters.AddWithValue("@name", name);

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    [Fact]
    public async Task LoginAsync_Should_MintLoginOnlyAgent_When_Called()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitWorkspaceAsync(cancellationToken);

        // act
        var agent = await _store.LoginAsync(cancellationToken);

        // assert
        Assert.Null(agent.Harness);
        Assert.Null(agent.SessionId);
        Assert.Equal("", agent.Role);
        Assert.Equal(_timeProvider.GetUtcNow(), agent.RegisteredAt);
        Assert.Equal(agent.RegisteredAt, agent.StartedAt);
    }

    [Fact]
    public async Task LoginAsync_Should_MintDistinctNames_When_CalledTwice()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitWorkspaceAsync(cancellationToken);

        // act
        var first = await _store.LoginAsync(cancellationToken);
        var second = await _store.LoginAsync(cancellationToken);

        // assert
        Assert.NotEqual(first.Name, second.Name);
    }

    [Fact]
    public async Task StartSessionAsync_Should_MintAgent_When_SessionUnknown()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitWorkspaceAsync(cancellationToken);

        // act
        var result = await _store.StartSessionAsync(CreateRequest(), cancellationToken);

        // assert
        Assert.Equal(AgentSessionStartKind.Minted, result.Kind);
        Assert.Equal(Harness, result.Row?.Harness);
        Assert.Equal(SessionId, result.Row?.SessionId);
        Assert.Equal("", result.Row?.Role);
        Assert.Equal(_timeProvider.GetUtcNow(), result.Row?.StartedAt);
    }

    [Fact]
    public async Task StartSessionAsync_Should_ReuseRowAndRefreshFields_When_SessionKnown()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitWorkspaceAsync(cancellationToken);
        var minted = await _store.StartSessionAsync(CreateRequest(harnessVersion: "1.0.0"), cancellationToken);
        _timeProvider.Advance(TimeSpan.FromMinutes(5));

        // act
        var result = await _store.StartSessionAsync(
            CreateRequest(harnessVersion: "2.0.0", cwd: "/repo2"), cancellationToken);

        // assert
        Assert.Equal(AgentSessionStartKind.Reused, result.Kind);
        Assert.Equal(minted.Row?.Name, result.Row?.Name);
        Assert.Equal("2.0.0", result.Row?.HarnessVersion);
        Assert.Equal("/repo2", result.Row?.Cwd);
        Assert.Equal(_timeProvider.GetUtcNow(), result.Row?.LastSeenAt);
    }

    [Fact]
    public async Task StartSessionAsync_Should_KeepStartedAtAndRegisteredAt_When_SessionKnown()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitWorkspaceAsync(cancellationToken);
        var minted = await _store.StartSessionAsync(CreateRequest(), cancellationToken);
        _timeProvider.Advance(TimeSpan.FromMinutes(5));

        // act
        var result = await _store.StartSessionAsync(CreateRequest(), cancellationToken);

        // assert
        Assert.Equal(minted.Row?.StartedAt, result.Row?.StartedAt);
        Assert.Equal(minted.Row?.RegisteredAt, result.Row?.RegisteredAt);
    }

    [Fact]
    public async Task StartSessionAsync_Should_KeepHarnessVersion_When_RequestVersionEmpty()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitWorkspaceAsync(cancellationToken);
        await _store.StartSessionAsync(CreateRequest(harnessVersion: "1.0.0"), cancellationToken);

        // act
        var result = await _store.StartSessionAsync(CreateRequest(harnessVersion: ""), cancellationToken);

        // assert
        Assert.Equal("1.0.0", result.Row?.HarnessVersion);
    }

    [Fact]
    public async Task StartSessionAsync_Should_ClearEndedAtAndKeepStartedAt_When_ResumingEndedSession()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitWorkspaceAsync(cancellationToken);
        var minted = await _store.StartSessionAsync(CreateRequest(), cancellationToken);
        await _store.EndSessionAsync(Harness, SessionId, cancellationToken);
        _timeProvider.Advance(TimeSpan.FromMinutes(5));

        // act
        var result = await _store.StartSessionAsync(CreateRequest(), cancellationToken);

        // assert
        Assert.Equal(AgentSessionStartKind.Reused, result.Kind);
        Assert.Null(result.Row?.EndedAt);
        Assert.Equal(minted.Row?.StartedAt, result.Row?.StartedAt);
        Assert.Equal(minted.Row?.Name, result.Row?.Name);
    }

    [Fact]
    public async Task StartSessionAsync_Should_ReturnIgnoredAndWriteNothing_When_SessionBelongsToDeletedAgent()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitWorkspaceAsync(cancellationToken);
        var minted = await _store.StartSessionAsync(CreateRequest(), cancellationToken);
        await MarkDeletedAsync(minted.Row!.Name, cancellationToken);
        _timeProvider.Advance(TimeSpan.FromMinutes(5));

        // act
        var result = await _store.StartSessionAsync(CreateRequest(harnessVersion: "9.9.9"), cancellationToken);
        var stored = await _store.FindBySessionAsync(Harness, SessionId, cancellationToken);

        // assert
        Assert.Equal(AgentSessionStartKind.Ignored, result.Kind);
        Assert.Null(result.Row);
        Assert.Equal(minted.Row.LastSeenAt, stored?.LastSeenAt);
        Assert.Equal(minted.Row.HarnessVersion, stored?.HarnessVersion);
    }

    [Fact]
    public async Task StartSessionAsync_Should_Throw_When_HarnessUnknown()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitWorkspaceAsync(cancellationToken);

        // act
        var exception = await Assert.ThrowsAsync<ArgumentException>(
            () => _store.StartSessionAsync(CreateRequest(harness: "typo"), cancellationToken));

        // assert
        Assert.Equal("harness", exception.ParamName);
        Assert.Empty(await _store.ListAsync(cancellationToken));
    }

    [Fact]
    public async Task StartSessionAsync_Should_Throw_When_HarnessIsNitroBoard()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitWorkspaceAsync(cancellationToken);

        // act
        var exception = await Assert.ThrowsAsync<ArgumentException>(
            () => _store.StartSessionAsync(
                CreateRequest(harness: AgentSessionHarness.NitroBoard), cancellationToken));

        // assert
        Assert.Equal("harness", exception.ParamName);
        Assert.Empty(await _store.ListAsync(cancellationToken));
    }

    [Fact]
    public async Task TouchSessionAsync_Should_UpdateLastSeenAt_When_RowExists()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitWorkspaceAsync(cancellationToken);
        await _store.StartSessionAsync(CreateRequest(), cancellationToken);
        _timeProvider.Advance(TimeSpan.FromMinutes(5));

        // act
        var touched = await _store.TouchSessionAsync(Harness, SessionId, cancellationToken);
        var row = await _store.FindBySessionAsync(Harness, SessionId, cancellationToken);

        // assert
        Assert.True(touched);
        Assert.Equal(_timeProvider.GetUtcNow(), row?.LastSeenAt);
    }

    [Fact]
    public async Task TouchSessionAsync_Should_ReturnFalse_When_RowMissing()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitWorkspaceAsync(cancellationToken);

        // act
        var touched = await _store.TouchSessionAsync(Harness, SessionId, cancellationToken);

        // assert
        Assert.False(touched);
    }

    [Fact]
    public async Task TouchSessionAsync_Should_ReturnFalse_When_RowDeleted()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitWorkspaceAsync(cancellationToken);
        var minted = await _store.StartSessionAsync(CreateRequest(), cancellationToken);
        await MarkDeletedAsync(minted.Row!.Name, cancellationToken);

        // act
        var touched = await _store.TouchSessionAsync(Harness, SessionId, cancellationToken);

        // assert
        Assert.False(touched);
    }

    [Fact]
    public async Task TouchSessionAsync_Should_Throw_When_HarnessUnknown()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitWorkspaceAsync(cancellationToken);

        // act
        var exception = await Assert.ThrowsAsync<ArgumentException>(
            () => _store.TouchSessionAsync("typo", SessionId, cancellationToken));

        // assert
        Assert.Equal("harness", exception.ParamName);
    }

    [Fact]
    public async Task TouchAsync_Should_UpdateLastSeenAt_When_RowExists()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitWorkspaceAsync(cancellationToken);
        var agent = await _store.LoginAsync(cancellationToken);
        _timeProvider.Advance(TimeSpan.FromMinutes(5));

        // act
        var touched = await _store.TouchAsync(agent.Name, cancellationToken);
        var row = await _store.FindAsync(agent.Name, cancellationToken);

        // assert
        Assert.True(touched);
        Assert.Equal(_timeProvider.GetUtcNow(), row?.LastSeenAt);
    }

    [Fact]
    public async Task TouchAsync_Should_ReturnFalse_When_RowMissing()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitWorkspaceAsync(cancellationToken);

        // act
        var touched = await _store.TouchAsync("nobody", cancellationToken);

        // assert
        Assert.False(touched);
    }

    [Fact]
    public async Task TouchAsync_Should_ReturnFalse_When_RowDeleted()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitWorkspaceAsync(cancellationToken);
        var agent = await _store.LoginAsync(cancellationToken);
        await MarkDeletedAsync(agent.Name, cancellationToken);

        // act
        var touched = await _store.TouchAsync(agent.Name, cancellationToken);

        // assert
        Assert.False(touched);
    }

    [Fact]
    public async Task EndSessionAsync_Should_SetEndedAt_When_RowExists()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitWorkspaceAsync(cancellationToken);
        await _store.StartSessionAsync(CreateRequest(), cancellationToken);

        // act
        var ended = await _store.EndSessionAsync(Harness, SessionId, cancellationToken);
        var row = await _store.FindBySessionAsync(Harness, SessionId, cancellationToken);

        // assert
        Assert.True(ended);
        Assert.Equal(_timeProvider.GetUtcNow(), row?.EndedAt);
    }

    [Fact]
    public async Task EndSessionAsync_Should_ReturnFalse_When_RowMissing()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitWorkspaceAsync(cancellationToken);

        // act
        var ended = await _store.EndSessionAsync(Harness, SessionId, cancellationToken);

        // assert
        Assert.False(ended);
    }

    [Fact]
    public async Task EndSessionAsync_Should_ReturnFalse_When_RowDeleted()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitWorkspaceAsync(cancellationToken);
        var minted = await _store.StartSessionAsync(CreateRequest(), cancellationToken);
        await MarkDeletedAsync(minted.Row!.Name, cancellationToken);

        // act
        var ended = await _store.EndSessionAsync(Harness, SessionId, cancellationToken);

        // assert
        Assert.False(ended);
    }

    [Fact]
    public async Task SetRoleAsync_Should_WriteRoleAndBumpLastSeenAt_When_RowExists()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitWorkspaceAsync(cancellationToken);
        var agent = await _store.LoginAsync(cancellationToken);
        _timeProvider.Advance(TimeSpan.FromMinutes(5));

        // act
        var updated = await _store.SetRoleAsync(agent.Name, "Implementer", cancellationToken);

        // assert
        Assert.Equal("implementer", updated?.Role);
        Assert.Equal(_timeProvider.GetUtcNow(), updated?.LastSeenAt);
    }

    [Fact]
    public async Task SetRoleAsync_Should_BeNoOpExceptLastSeenAt_When_RoleUnchanged()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitWorkspaceAsync(cancellationToken);
        var agent = await _store.LoginAsync(cancellationToken);
        var first = await _store.SetRoleAsync(agent.Name, "implementer", cancellationToken);
        _timeProvider.Advance(TimeSpan.FromMinutes(5));

        // act
        var second = await _store.SetRoleAsync(agent.Name, "implementer", cancellationToken);

        // assert
        Assert.Equal(first?.Role, second?.Role);
        Assert.Equal(first?.StartedAt, second?.StartedAt);
        Assert.Equal(_timeProvider.GetUtcNow(), second?.LastSeenAt);
        Assert.NotEqual(first?.LastSeenAt, second?.LastSeenAt);
    }

    [Fact]
    public async Task SetRoleAsync_Should_ReturnNull_When_RowMissing()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitWorkspaceAsync(cancellationToken);

        // act
        var updated = await _store.SetRoleAsync("nobody", "implementer", cancellationToken);

        // assert
        Assert.Null(updated);
    }

    [Fact]
    public async Task SetRoleAsync_Should_ReturnNull_When_RowDeleted()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitWorkspaceAsync(cancellationToken);
        var agent = await _store.LoginAsync(cancellationToken);
        await MarkDeletedAsync(agent.Name, cancellationToken);

        // act
        var updated = await _store.SetRoleAsync(agent.Name, "implementer", cancellationToken);

        // assert
        Assert.Null(updated);
    }

    [Fact]
    public async Task FindAsync_Should_ReturnRow_When_Exists()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitWorkspaceAsync(cancellationToken);
        var agent = await _store.LoginAsync(cancellationToken);

        // act
        var found = await _store.FindAsync(agent.Name, cancellationToken);

        // assert
        Assert.Equal(agent.Name, found?.Name);
    }

    [Fact]
    public async Task FindAsync_Should_ReturnNull_When_NotFound()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitWorkspaceAsync(cancellationToken);

        // act
        var found = await _store.FindAsync("nobody", cancellationToken);

        // assert
        Assert.Null(found);
    }

    [Fact]
    public async Task FindAsync_Should_ReturnDeletedRow_When_RowDeleted()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitWorkspaceAsync(cancellationToken);
        var agent = await _store.LoginAsync(cancellationToken);
        await MarkDeletedAsync(agent.Name, cancellationToken);

        // act
        var found = await _store.FindAsync(agent.Name, cancellationToken);

        // assert
        Assert.NotNull(found);
        Assert.True(found.IsDeleted);
    }

    [Fact]
    public async Task FindBySessionAsync_Should_ReturnRow_When_Exists()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitWorkspaceAsync(cancellationToken);
        var minted = await _store.StartSessionAsync(CreateRequest(), cancellationToken);

        // act
        var found = await _store.FindBySessionAsync(Harness, SessionId, cancellationToken);

        // assert
        Assert.Equal(minted.Row?.Name, found?.Name);
    }

    [Fact]
    public async Task FindBySessionAsync_Should_ReturnNull_When_NotFound()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitWorkspaceAsync(cancellationToken);

        // act
        var found = await _store.FindBySessionAsync(Harness, SessionId, cancellationToken);

        // assert
        Assert.Null(found);
    }

    [Fact]
    public async Task FindBySessionAsync_Should_ReturnDeletedRow_When_RowDeleted()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitWorkspaceAsync(cancellationToken);
        var minted = await _store.StartSessionAsync(CreateRequest(), cancellationToken);
        await MarkDeletedAsync(minted.Row!.Name, cancellationToken);

        // act
        var found = await _store.FindBySessionAsync(Harness, SessionId, cancellationToken);

        // assert
        Assert.NotNull(found);
        Assert.True(found.IsDeleted);
    }

    [Fact]
    public async Task ListAsync_Should_ExcludeDeletedRows_When_SomeAreDeleted()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitWorkspaceAsync(cancellationToken);
        var active = await _store.LoginAsync(cancellationToken);
        var deleted = await _store.LoginAsync(cancellationToken);
        await MarkDeletedAsync(deleted.Name, cancellationToken);

        // act
        var agents = await _store.ListAsync(cancellationToken);

        // assert
        var agent = Assert.Single(agents);
        Assert.Equal(active.Name, agent.Name);
    }

    [Fact]
    public async Task ListAsync_Should_ReturnEmpty_When_NoAgents()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitWorkspaceAsync(cancellationToken);

        // act
        var agents = await _store.ListAsync(cancellationToken);

        // assert
        Assert.Empty(agents);
    }

    [Fact]
    public async Task SetEndpointAsync_Should_WriteEndpoint_When_KindIsNotOpencodeServer()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitWorkspaceAsync(cancellationToken);
        var minted = await _store.StartSessionAsync(CreateRequest(), cancellationToken);

        // act
        var updated = await _store.SetEndpointAsync(
            minted.Row!.Name, AgentSessionEndpointKind.ClaudePeer, "peer-2", "ignored", cancellationToken);
        var row = await _store.FindAsync(minted.Row.Name, cancellationToken);

        // assert
        Assert.True(updated);
        Assert.Equal(AgentSessionEndpointKind.ClaudePeer, row?.EndpointKind);
        Assert.Equal("peer-2", row?.EndpointAddr);
        Assert.Null(row?.EndpointSecret);
    }

    [Fact]
    public async Task SetEndpointAsync_Should_KeepCredential_When_OpencodeServerBelongsToOpencodeHarness()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitWorkspaceAsync(cancellationToken);
        var minted = await _store.StartSessionAsync(
            CreateRequest(harness: AgentSessionHarness.Opencode), cancellationToken);

        // act
        var updated = await _store.SetEndpointAsync(
            minted.Row!.Name,
            AgentSessionEndpointKind.OpencodeServer,
            "http://127.0.0.1:4096",
            "server-password",
            cancellationToken);
        var row = await _store.FindAsync(minted.Row.Name, cancellationToken);

        // assert
        Assert.True(updated);
        Assert.Equal("server-password", row?.EndpointSecret);
    }

    [Fact]
    public async Task SetEndpointAsync_Should_DropCredential_When_OpencodeServerBelongsToNonOpencodeHarness()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitWorkspaceAsync(cancellationToken);
        var minted = await _store.StartSessionAsync(CreateRequest(), cancellationToken);

        // act
        var updated = await _store.SetEndpointAsync(
            minted.Row!.Name,
            AgentSessionEndpointKind.OpencodeServer,
            "http://127.0.0.1:4096",
            "server-password",
            cancellationToken);
        var row = await _store.FindAsync(minted.Row.Name, cancellationToken);

        // assert
        Assert.True(updated);
        Assert.Null(row?.EndpointSecret);
    }

    [Fact]
    public async Task SetEndpointAsync_Should_ReturnFalse_When_RowDeleted()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitWorkspaceAsync(cancellationToken);
        var minted = await _store.StartSessionAsync(CreateRequest(), cancellationToken);
        await MarkDeletedAsync(minted.Row!.Name, cancellationToken);

        // act
        var updated = await _store.SetEndpointAsync(
            minted.Row.Name, AgentSessionEndpointKind.ClaudePeer, "peer-2", null, cancellationToken);

        // assert
        Assert.False(updated);
    }

    [Fact]
    public async Task ResetBlockBudgetAsync_Should_ResetToZero_When_RowExists()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitWorkspaceAsync(cancellationToken);
        var minted = await _store.StartSessionAsync(CreateRequest(), cancellationToken);
        await _store.IncrementBlockBudgetAsync(minted.Row!.Name, cancellationToken);
        await _store.IncrementBlockBudgetAsync(minted.Row.Name, cancellationToken);

        // act
        var result = await _store.ResetBlockBudgetAsync(minted.Row.Name, cancellationToken);
        var row = await _store.FindAsync(minted.Row.Name, cancellationToken);

        // assert
        Assert.Equal(0, result);
        Assert.Equal(0, row?.BlockBudgetUsed);
    }

    [Fact]
    public async Task ResetBlockBudgetAsync_Should_BeNoOp_When_RowDeleted()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitWorkspaceAsync(cancellationToken);
        var minted = await _store.StartSessionAsync(CreateRequest(), cancellationToken);
        await MarkDeletedAsync(minted.Row!.Name, cancellationToken);

        // act
        var result = await _store.ResetBlockBudgetAsync(minted.Row.Name, cancellationToken);

        // assert
        Assert.Equal(0, result);
    }

    [Fact]
    public async Task IncrementBlockBudgetAsync_Should_ReturnIncrementedValue_When_RowExists()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitWorkspaceAsync(cancellationToken);
        var minted = await _store.StartSessionAsync(CreateRequest(), cancellationToken);

        // act
        var first = await _store.IncrementBlockBudgetAsync(minted.Row!.Name, cancellationToken);
        var second = await _store.IncrementBlockBudgetAsync(minted.Row.Name, cancellationToken);

        // assert
        Assert.Equal(1, first);
        Assert.Equal(2, second);
    }

    [Fact]
    public async Task IncrementBlockBudgetAsync_Should_ReturnZero_When_RowDeleted()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitWorkspaceAsync(cancellationToken);
        var minted = await _store.StartSessionAsync(CreateRequest(), cancellationToken);
        await MarkDeletedAsync(minted.Row!.Name, cancellationToken);

        // act
        var result = await _store.IncrementBlockBudgetAsync(minted.Row.Name, cancellationToken);

        // assert
        Assert.Equal(0, result);
    }

    [Fact]
    public async Task TryClaimPingCooldownAsync_Should_Claim_When_NoPriorAttempt()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitWorkspaceAsync(cancellationToken);
        var minted = await _store.StartSessionAsync(CreateRequest(), cancellationToken);

        // act
        var claimed = await _store.TryClaimPingCooldownAsync(
            minted.Row!.Name, TimeSpan.FromSeconds(60), "attempt-1", cancellationToken);
        var row = await _store.FindAsync(minted.Row.Name, cancellationToken);

        // assert
        Assert.True(claimed);
        Assert.Equal("attempt-1", row?.LastPingAttempt);
        Assert.Equal(_timeProvider.GetUtcNow(), row?.LastPingAt);
    }

    [Fact]
    public async Task TryClaimPingCooldownAsync_Should_ReturnFalse_When_StillWithinCooldown()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitWorkspaceAsync(cancellationToken);
        var minted = await _store.StartSessionAsync(CreateRequest(), cancellationToken);
        await _store.TryClaimPingCooldownAsync(
            minted.Row!.Name, TimeSpan.FromSeconds(60), "attempt-1", cancellationToken);
        _timeProvider.Advance(TimeSpan.FromSeconds(30));

        // act
        var claimed = await _store.TryClaimPingCooldownAsync(
            minted.Row.Name, TimeSpan.FromSeconds(60), "attempt-2", cancellationToken);

        // assert
        Assert.False(claimed);
    }

    [Fact]
    public async Task TryClaimPingCooldownAsync_Should_ReturnTrue_When_CooldownElapsed()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitWorkspaceAsync(cancellationToken);
        var minted = await _store.StartSessionAsync(CreateRequest(), cancellationToken);
        await _store.TryClaimPingCooldownAsync(
            minted.Row!.Name, TimeSpan.FromSeconds(60), "attempt-1", cancellationToken);
        _timeProvider.Advance(TimeSpan.FromSeconds(61));

        // act
        var claimed = await _store.TryClaimPingCooldownAsync(
            minted.Row.Name, TimeSpan.FromSeconds(60), "attempt-2", cancellationToken);
        var row = await _store.FindAsync(minted.Row.Name, cancellationToken);

        // assert
        Assert.True(claimed);
        Assert.Equal("attempt-2", row?.LastPingAttempt);
    }

    [Fact]
    public async Task TryClaimPingCooldownAsync_Should_ReturnFalse_When_RowDeleted()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitWorkspaceAsync(cancellationToken);
        var minted = await _store.StartSessionAsync(CreateRequest(), cancellationToken);
        await MarkDeletedAsync(minted.Row!.Name, cancellationToken);

        // act
        var claimed = await _store.TryClaimPingCooldownAsync(
            minted.Row.Name, TimeSpan.FromSeconds(60), "attempt-1", cancellationToken);

        // assert
        Assert.False(claimed);
    }

    [Fact]
    public async Task TryClaimPingCooldownAsync_Should_HaveExactlyOneWinner_When_ConcurrentClaimsRaceTheSameRow()
    {
        // arrange
        // each call opens its own connection against the same agent row
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitWorkspaceAsync(cancellationToken);
        var minted = await _store.StartSessionAsync(CreateRequest(), cancellationToken);

        // act
        var results = await ConcurrentTestHarness.RunAsync(
            5,
            i => _store.TryClaimPingCooldownAsync(
                minted.Row!.Name, TimeSpan.FromSeconds(60), $"attempt-{i}", cancellationToken));

        // assert
        Assert.Equal(1, results.Count(claimed => claimed));
    }

    [Fact]
    public async Task WritePingResultAsync_Should_Write_When_AttemptIdMatches()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitWorkspaceAsync(cancellationToken);
        var minted = await _store.StartSessionAsync(CreateRequest(), cancellationToken);
        await _store.TryClaimPingCooldownAsync(
            minted.Row!.Name, TimeSpan.FromSeconds(60), "attempt-1", cancellationToken);

        // act
        await _store.WritePingResultAsync(
            minted.Row.Name, "attempt-1", AgentPingResult.Ok, "all good", cancellationToken);
        var row = await _store.FindAsync(minted.Row.Name, cancellationToken);

        // assert
        Assert.Equal(AgentPingResult.Ok, row?.LastPingResult);
        Assert.Equal("all good", row?.LastPingDetail);
    }

    [Fact]
    public async Task WritePingResultAsync_Should_BeANoOp_When_AttemptIdIsStale()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitWorkspaceAsync(cancellationToken);
        var minted = await _store.StartSessionAsync(CreateRequest(), cancellationToken);
        await _store.TryClaimPingCooldownAsync(
            minted.Row!.Name, TimeSpan.Zero, "attempt-1", cancellationToken);
        await _store.TryClaimPingCooldownAsync(
            minted.Row.Name, TimeSpan.Zero, "attempt-2", cancellationToken);

        // act
        await _store.WritePingResultAsync(
            minted.Row.Name, "attempt-1", AgentPingResult.Timeout, null, cancellationToken);
        var row = await _store.FindAsync(minted.Row.Name, cancellationToken);

        // assert
        Assert.Null(row?.LastPingResult);
    }

    [Fact]
    public async Task WritePingResultAsync_Should_BeANoOp_When_RowDeleted()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitWorkspaceAsync(cancellationToken);
        var minted = await _store.StartSessionAsync(CreateRequest(), cancellationToken);
        await _store.TryClaimPingCooldownAsync(
            minted.Row!.Name, TimeSpan.FromSeconds(60), "attempt-1", cancellationToken);
        await MarkDeletedAsync(minted.Row.Name, cancellationToken);

        // act
        await _store.WritePingResultAsync(
            minted.Row.Name, "attempt-1", AgentPingResult.Ok, null, cancellationToken);
        var row = await _store.FindAsync(minted.Row.Name, cancellationToken);

        // assert
        Assert.Null(row?.LastPingResult);
    }

    [Fact]
    public async Task ArmAnnouncementAsync_Should_SetPending_When_RowExists()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitWorkspaceAsync(cancellationToken);
        var agent = await _store.LoginAsync(cancellationToken);

        // act
        await _store.ArmAnnouncementAsync(agent.Name, cancellationToken);
        var pending = await _store.IsAnnouncementPendingAsync(agent.Name, cancellationToken);

        // assert
        Assert.True(pending);
    }

    [Fact]
    public async Task ArmAnnouncementAsync_Should_BeNoOp_When_RowDeleted()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitWorkspaceAsync(cancellationToken);
        var agent = await _store.LoginAsync(cancellationToken);
        await MarkDeletedAsync(agent.Name, cancellationToken);

        // act
        await _store.ArmAnnouncementAsync(agent.Name, cancellationToken);
        var pending = await _store.IsAnnouncementPendingAsync(agent.Name, cancellationToken);

        // assert
        Assert.False(pending);
    }

    [Fact]
    public async Task ClaimAnnouncementAsync_Should_ClearPendingAndReturnTrue_When_Pending()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitWorkspaceAsync(cancellationToken);
        var agent = await _store.LoginAsync(cancellationToken);
        await _store.ArmAnnouncementAsync(agent.Name, cancellationToken);

        // act
        var claimed = await _store.ClaimAnnouncementAsync(agent.Name, cancellationToken);
        var pending = await _store.IsAnnouncementPendingAsync(agent.Name, cancellationToken);

        // assert
        Assert.True(claimed);
        Assert.False(pending);
    }

    [Fact]
    public async Task ClaimAnnouncementAsync_Should_ReturnFalse_When_NothingPending()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitWorkspaceAsync(cancellationToken);
        var agent = await _store.LoginAsync(cancellationToken);

        // act
        var claimed = await _store.ClaimAnnouncementAsync(agent.Name, cancellationToken);

        // assert
        Assert.False(claimed);
    }

    [Fact]
    public async Task ClaimAnnouncementAsync_Should_ReturnFalse_When_RowDeleted()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitWorkspaceAsync(cancellationToken);
        var agent = await _store.LoginAsync(cancellationToken);
        await _store.ArmAnnouncementAsync(agent.Name, cancellationToken);
        await MarkDeletedAsync(agent.Name, cancellationToken);

        // act
        var claimed = await _store.ClaimAnnouncementAsync(agent.Name, cancellationToken);

        // assert
        Assert.False(claimed);
    }

    [Fact]
    public async Task IsAnnouncementPendingAsync_Should_ReturnFalse_When_RowDeleted()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitWorkspaceAsync(cancellationToken);
        var agent = await _store.LoginAsync(cancellationToken);
        await _store.ArmAnnouncementAsync(agent.Name, cancellationToken);
        await MarkDeletedAsync(agent.Name, cancellationToken);

        // act
        var pending = await _store.IsAnnouncementPendingAsync(agent.Name, cancellationToken);

        // assert
        Assert.False(pending);
    }

    [Fact]
    public async Task RearmIdlePushAsync_Should_SetArmed_When_RowExists()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitWorkspaceAsync(cancellationToken);
        var agent = await _store.LoginAsync(cancellationToken);

        // act
        await _store.RearmIdlePushAsync(agent.Name, cancellationToken);
        var claimed = await _store.ClaimIdlePushAsync(agent.Name, cancellationToken);

        // assert
        Assert.True(claimed);
    }

    [Fact]
    public async Task RearmIdlePushAsync_Should_BeNoOp_When_RowDeleted()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitWorkspaceAsync(cancellationToken);
        var agent = await _store.LoginAsync(cancellationToken);
        await MarkDeletedAsync(agent.Name, cancellationToken);

        // act
        await _store.RearmIdlePushAsync(agent.Name, cancellationToken);
        var claimed = await _store.ClaimIdlePushAsync(agent.Name, cancellationToken);

        // assert
        Assert.False(claimed);
    }

    [Fact]
    public async Task ClaimIdlePushAsync_Should_ClaimOnceAndReturnFalseAfter_When_Armed()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitWorkspaceAsync(cancellationToken);
        var agent = await _store.LoginAsync(cancellationToken);
        await _store.RearmIdlePushAsync(agent.Name, cancellationToken);

        // act
        var first = await _store.ClaimIdlePushAsync(agent.Name, cancellationToken);
        var second = await _store.ClaimIdlePushAsync(agent.Name, cancellationToken);

        // assert
        Assert.True(first);
        Assert.False(second);
    }

    [Fact]
    public async Task ClaimIdlePushAsync_Should_ReturnFalse_When_RowDeleted()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitWorkspaceAsync(cancellationToken);
        var agent = await _store.LoginAsync(cancellationToken);
        await _store.RearmIdlePushAsync(agent.Name, cancellationToken);
        await MarkDeletedAsync(agent.Name, cancellationToken);

        // act
        var claimed = await _store.ClaimIdlePushAsync(agent.Name, cancellationToken);

        // assert
        Assert.False(claimed);
    }

    [Fact]
    public async Task RecordHarnessVersionAsync_Should_WriteVersion_When_RowExists()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitWorkspaceAsync(cancellationToken);
        var minted = await _store.StartSessionAsync(CreateRequest(harnessVersion: "1.0.0"), cancellationToken);

        // act
        var updated = await _store.RecordHarnessVersionAsync(minted.Row!.Name, "2.0.0", cancellationToken);
        var row = await _store.FindAsync(minted.Row.Name, cancellationToken);

        // assert
        Assert.True(updated);
        Assert.Equal("2.0.0", row?.HarnessVersion);
    }

    [Fact]
    public async Task RecordHarnessVersionAsync_Should_ReturnFalse_When_RowDeleted()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await InitWorkspaceAsync(cancellationToken);
        var minted = await _store.StartSessionAsync(CreateRequest(harnessVersion: "1.0.0"), cancellationToken);
        await MarkDeletedAsync(minted.Row!.Name, cancellationToken);

        // act
        var updated = await _store.RecordHarnessVersionAsync(minted.Row.Name, "2.0.0", cancellationToken);

        // assert
        Assert.False(updated);
    }
}
