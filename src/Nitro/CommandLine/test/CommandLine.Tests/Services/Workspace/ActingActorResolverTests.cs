using ChilliCream.Nitro.CommandLine.Services.Workspace;

namespace ChilliCream.Nitro.CommandLine.Tests.Agents;

/// <summary>
/// Covers <see cref="ActingActorResolver"/>: a name is never inferred from
/// the ambient session, only validated against the actors this workspace
/// allocated.
/// </summary>
public sealed class ActingActorResolverTests : IDisposable
{
    private readonly DirectoryInfo _tempRoot = Directory.CreateTempSubdirectory("nitro-acting-actor-tests");

    public void Dispose() => _tempRoot.Delete(recursive: true);

    [Fact]
    public async Task ResolveAsync_Should_ReturnTheNormalizedActor_When_ItWasAllocated()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var (resolver, agents) = await CreateAsync(cancellationToken);
        await agents.EnsureImplicitAsync("maya", cancellationToken);

        // act
        var actor = await resolver.ResolveAsync("Maya", cancellationToken);

        // assert
        Assert.Equal("maya", actor);
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

    private async Task<(ActingActorResolver Resolver, AgentRegistry Agents)> CreateAsync(
        CancellationToken cancellationToken)
    {
        var fileSystem = new TestFileSystem(_tempRoot.FullName);
        var database = new AgentDatabase();
        var workspace = AgentWorkspace.GetDirectory(_tempRoot.FullName);
        Directory.CreateDirectory(workspace);

        await using (await database.InitializeAsync(workspace, cancellationToken))
        {
        }

        var agents = new AgentRegistry(fileSystem, TimeProvider.System, database);

        return (new ActingActorResolver(agents), agents);
    }
}
