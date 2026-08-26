using ChilliCream.Nitro.CommandLine.Services;
using ChilliCream.Nitro.CommandLine.Services.Workspace;
using ChilliCream.Nitro.CommandLine.Tests.Commands;
using Microsoft.Extensions.Time.Testing;

namespace ChilliCream.Nitro.CommandLine.Tests.Agents;

public sealed class ActingActorResolverTests : IDisposable
{
    private const string Host = "host-acting-actor-tests";

    private readonly DirectoryInfo _tempRoot =
        Directory.CreateTempSubdirectory("nitro-acting-actor-tests");

    public void Dispose() => _tempRoot.Delete(recursive: true);

    [Fact]
    public async Task ResolveAsync_Should_InferTheCurrentSessionsActor_When_Omitted()
    {
        var (resolver, sessions, generation, _) = await CreateAsync();
        await sessions.RegisterAsync(
            generation, "nova", actorGiven: true, role: null, roleGiven: false,
            TestContext.Current.CancellationToken);

        var actor = await resolver.ResolveAsync(
            optionValue: null, TestContext.Current.CancellationToken);

        Assert.Equal("nova", actor);
    }

    [Fact]
    public async Task ResolveAsync_Should_AllowAnExplicitDifferentActor_WithoutChangingTheSession()
    {
        var (resolver, sessions, generation, agentRegistry) = await CreateAsync();
        await sessions.RegisterAsync(
            generation, "nova", actorGiven: true, role: null, roleGiven: false,
            TestContext.Current.CancellationToken);

        // The name must already be allocated: `--actor` binds an existing
        // actor, it never invents one.
        await agentRegistry.EnsureImplicitAsync("maya", TestContext.Current.CancellationToken);

        var actor = await resolver.ResolveAsync(
            optionValue: "Maya", TestContext.Current.CancellationToken);
        var current = await sessions.FindByGenerationAsync(
            generation, TestContext.Current.CancellationToken);

        Assert.Equal("maya", actor);
        Assert.Equal("nova", current!.AgentName);
    }

    private async Task<(ActingActorResolver Resolver, AgentSessionRegistry Sessions, AgentSessionGeneration Generation, AgentRegistry AgentRegistry)>
        CreateAsync()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var workspace = AgentWorkspace.GetDirectory(_tempRoot.FullName);
        Directory.CreateDirectory(workspace);
        var fileSystem = new TestFileSystem(_tempRoot.FullName);
        var timeProvider = new FakeTimeProvider(
            new DateTimeOffset(2026, 1, 10, 12, 0, 0, TimeSpan.Zero));
        var database = new AgentDatabase();
        await using (await database.InitializeAsync(workspace, cancellationToken))
        {
        }

        var processInfo = new ProcessInfoProvider();
        var pid = Environment.ProcessId;
        var processStart = processInfo.GetStartTicks(pid)!;
        var ancestor = new ClaudeAncestorSession(pid, "session-1", _tempRoot.FullName, "peer-a");
        var claudeResolver = new FixedClaudeAncestorSessionResolver(ancestor);
        var instanceId = new FixedInstanceIdProvider(Host);
        var globalConfig = new FixedGlobalConfigDirectoryProvider(_tempRoot.FullName);
        var agentRegistry = new AgentRegistry(fileSystem, timeProvider, database);
        var sessions = new AgentSessionRegistry(
            fileSystem,
            timeProvider,
            database,
            agentRegistry,
            instanceId,
            globalConfig,
            processInfo,
            claudeResolver);
        var generation = new AgentSessionGeneration(
            AgentSessionHarness.ClaudeCode, "session-1", Host, pid, processStart);
        await sessions.StartAsync(
            generation,
            _tempRoot.FullName,
            workspace,
            AgentSessionEndpointKind.ClaudePeer,
            "peer-a",
            envActor: null,
            cancellationToken);
        var resolver = new ActingActorResolver(
            fileSystem,
            new EmptyEnvironmentVariableProvider(),
            processInfo,
            claudeResolver,
            new FixedCodexAncestorSessionResolver(null),
            new FixedCopilotAncestorSessionResolver(null),
            instanceId,
            globalConfig,
            agentRegistry,
            sessions);

        return (resolver, sessions, generation, agentRegistry);
    }

    private sealed class EmptyEnvironmentVariableProvider : IEnvironmentVariableProvider
    {
        public string? GetEnvironmentVariable(string name) => null;
    }
}
