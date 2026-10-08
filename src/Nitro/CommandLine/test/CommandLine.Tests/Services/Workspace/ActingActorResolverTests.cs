using ChilliCream.Nitro.CommandLine.Services.Workspace;
using Microsoft.Extensions.Time.Testing;

namespace ChilliCream.Nitro.CommandLine.Tests.Agents;

/// <summary>
/// Covers <see cref="ActingActorResolver"/>: a name is never inferred from
/// the ambient session, only validated against the actors this workspace
/// allocated, and every resolve is a presence beat.
/// </summary>
public sealed class ActingActorResolverTests : IDisposable
{
    private readonly DirectoryInfo _tempRoot = Directory.CreateTempSubdirectory("nitro-acting-actor-tests");
    private readonly FakeTimeProvider _timeProvider =
        new(new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero));

    public void Dispose() => _tempRoot.Delete(recursive: true);

    [Fact]
    public async Task ResolveAsync_Should_ReturnTheNormalizedActor_When_ItWasAllocated()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var (resolver, agents) = await CreateAsync(cancellationToken);
        var agent = await agents.LoginAsync(cancellationToken);

        // act
        var actor = await resolver.ResolveAsync(agent.Name.ToUpperInvariant(), cancellationToken);

        // assert
        Assert.Equal(agent.Name, actor);
    }

    [Fact]
    public async Task ResolveAsync_Should_Throw_When_TheActorWasNeverAllocated()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var (resolver, _) = await CreateAsync(cancellationToken);

        // act
        var exception = await Assert.ThrowsAsync<ExitException>(
            () => resolver.ResolveAsync("never-allocated", cancellationToken));

        // assert
        Assert.Equal(
            "Unknown actor 'never-allocated'. Run `nitro agent login` to allocate one, "
            + "or `nitro agent list` to see the actors this workspace knows.",
            exception.Message);
    }

    [Fact]
    public async Task ResolveAsync_Should_Throw_When_NoActorIsGiven()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var (resolver, _) = await CreateAsync(cancellationToken);

        // act
        var exception = await Assert.ThrowsAsync<ExitException>(
            () => resolver.ResolveAsync(optionValue: null, cancellationToken));

        // assert
        Assert.Equal(
            "No actor given. Pass '--actor <actor>', and run `nitro agent login` to "
            + "allocate one if this session has none.",
            exception.Message);
    }

    [Fact]
    public async Task ResolveAsync_Should_Throw_When_TheActorWasDeleted()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var (resolver, agents) = await CreateAsync(cancellationToken);
        var agent = await agents.LoginAsync(cancellationToken);
        await MarkDeletedAsync(agent.Name, cancellationToken);

        // act
        var exception = await Assert.ThrowsAsync<ExitException>(
            () => resolver.ResolveAsync(agent.Name, cancellationToken));

        // assert
        Assert.Equal($"Agent '{agent.Name}' was deleted.", exception.Message);
    }

    [Fact]
    public async Task ResolveAsync_Should_BumpLastSeenAt_When_TheActorWasAllocated()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var (resolver, agents) = await CreateAsync(cancellationToken);
        var agent = await agents.LoginAsync(cancellationToken);
        _timeProvider.Advance(TimeSpan.FromMinutes(5));

        // act
        await resolver.ResolveAsync(agent.Name, cancellationToken);

        // assert
        var row = await agents.FindAsync(agent.Name, cancellationToken);
        Assert.Equal(_timeProvider.GetUtcNow(), row?.LastSeenAt);
    }

    private async Task<(ActingActorResolver Resolver, AgentStore Agents)> CreateAsync(
        CancellationToken cancellationToken)
    {
        var fileSystem = new TestFileSystem(_tempRoot.FullName);
        var database = new AgentDatabase();
        var workspace = AgentWorkspace.GetDirectory(_tempRoot.FullName);
        Directory.CreateDirectory(workspace);

        await using (await database.InitializeAsync(workspace, cancellationToken))
        {
        }

        var agents = new AgentStore(fileSystem, _timeProvider, database);

        return (new ActingActorResolver(agents), agents);
    }

    /// <summary>
    /// Soft-deletes the named agent directly, bypassing the store, since delete mechanics
    /// are a separate ticket.
    /// </summary>
    private async Task MarkDeletedAsync(string name, CancellationToken cancellationToken)
    {
        var workspace = AgentWorkspace.GetDirectory(_tempRoot.FullName);

        await using var connection = await new AgentDatabase().ConnectAsync(workspace, cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = "UPDATE agents SET deleted_at = @now WHERE name = @name";
        command.Parameters.AddWithValue("@now", _timeProvider.GetUtcNow());
        command.Parameters.AddWithValue("@name", name);

        await command.ExecuteNonQueryAsync(cancellationToken);
    }
}
