using System.Diagnostics.CodeAnalysis;
using HotChocolate.Execution;

namespace HotChocolate.Fusion;

/// <summary>
/// Base class for <c>graphql-hive/federation-gateway-audit</c> compliance suites.
/// Each derived suite builds its gateway (via <see cref="BuildGatewayAsync"/>) and
/// declares one <c>[Fact]</c> per audit test case that calls <see cref="RunAsync"/>
/// with the inline query and expected response. The composed
/// <see cref="FusionGateway"/> (subgraph <c>TestServer</c>s and the gateway service
/// provider) is disposed automatically at the end of each test.
/// </summary>
public abstract class ComplianceTestBase : IAsyncLifetime
{
    private FusionGateway? _gateway;

    /// <summary>
    /// Builds the Fusion gateway for this suite. Called lazily from
    /// <see cref="RunAsync"/> on the first invocation per test.
    /// </summary>
    protected abstract Task<FusionGateway> BuildGatewayAsync();

    /// <summary>
    /// Executes <paramref name="query"/> against the gateway and asserts the response
    /// matches the expected data payload and/or error presence.
    /// </summary>
    /// <param name="query">The GraphQL operation to execute.</param>
    /// <param name="expectedData">
    /// The expected <c>data</c> payload as a JSON object. When <see langword="null"/>,
    /// the <c>data</c> payload is not asserted.
    /// </param>
    /// <param name="expectsErrors">
    /// When not <see langword="null"/>, asserts whether an <c>errors</c> array is
    /// present on the response.
    /// </param>
    protected async Task RunAsync(
        [StringSyntax("GraphQL")] string query,
        [StringSyntax("Json")] string? expectedData = null,
        bool? expectsErrors = null)
    {
        _gateway ??= await BuildGatewayAsync();
        var result = await _gateway.Executor.ExecuteAsync(query);
        var json = result.ToJson(withIndentations: false);

        AuditAssertions.Assert(json, expectedData, expectsErrors);
    }

    /// <inheritdoc />
    public Task InitializeAsync() => Task.CompletedTask;

    /// <inheritdoc />
    public async Task DisposeAsync()
    {
        if (_gateway is not null)
        {
            await _gateway.DisposeAsync();
            _gateway = null;
        }
    }
}
