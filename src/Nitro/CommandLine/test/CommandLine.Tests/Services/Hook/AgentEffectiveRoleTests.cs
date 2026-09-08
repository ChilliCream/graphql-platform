using ChilliCream.Nitro.CommandLine.Services.Hook;
using ChilliCream.Nitro.CommandLine.Services.Workspace;
using Moq;

namespace ChilliCream.Nitro.CommandLine.Tests.Hook;

public sealed class AgentEffectiveRoleTests
{
    [Fact]
    public async Task ResolveAsync_Should_ReturnSessionRole_When_SessionRoleIsNonEmpty()
    {
        // arrange
        var agentRegistry = new Mock<IAgentRegistry>(MockBehavior.Strict);

        // act
        var role = await AgentEffectiveRole.ResolveAsync(
            "planner", "maya", agentRegistry.Object, TestContext.Current.CancellationToken);

        // assert
        Assert.Equal("planner", role);
    }

    [Fact]
    public async Task ResolveAsync_Should_PropagateCancellation_When_LookupIsCanceled()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var agentRegistry = new Mock<IAgentRegistry>(MockBehavior.Strict);
        agentRegistry
            .Setup(registry => registry.GetAsync("maya", cancellationToken))
            .ThrowsAsync(new TaskCanceledException());

        // act
        var exception = await Record.ExceptionAsync(
            () => AgentEffectiveRole.ResolveAsync(
                string.Empty, "maya", agentRegistry.Object, cancellationToken));

        // assert
        Assert.IsAssignableFrom<OperationCanceledException>(exception);
    }

    [Fact]
    public async Task ResolveAsync_Should_PropagateSchemaMismatch_When_LookupFindsIncompatibleSchema()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var agentRegistry = new Mock<IAgentRegistry>(MockBehavior.Strict);
        agentRegistry
            .Setup(registry => registry.GetAsync("maya", cancellationToken))
            .ThrowsAsync(new AgentWorkspaceSchemaMismatchException("schema mismatch"));

        // act
        var exception = await Record.ExceptionAsync(
            () => AgentEffectiveRole.ResolveAsync(
                string.Empty, "maya", agentRegistry.Object, cancellationToken));

        // assert
        Assert.IsType<AgentWorkspaceSchemaMismatchException>(exception);
    }
}
