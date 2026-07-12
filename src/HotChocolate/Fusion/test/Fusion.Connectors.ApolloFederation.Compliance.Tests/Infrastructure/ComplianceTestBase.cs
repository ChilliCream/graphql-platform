using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
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
    /// The expected <c>data</c> payload as JSON. A <see langword="null"/> value expects the
    /// response data to be absent or explicitly <see langword="null"/>.
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

    protected async Task RunAsync(AuditTestCase testCase)
    {
        ArgumentNullException.ThrowIfNull(testCase);

        _gateway ??= await BuildGatewayAsync();
        await ExecuteAndAssertAsync(
            _gateway,
            testCase,
            TestContext.Current.CancellationToken);
    }

    internal static async Task ExecuteAndAssertAsync(
        FusionGateway gateway,
        AuditTestCase testCase,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(gateway);
        ArgumentNullException.ThrowIfNull(testCase);

        var request = OperationRequestBuilder.New().SetDocument(testCase.Query);

        if (testCase.Variables is not null)
        {
            request.SetVariableValues(ParseVariables(testCase.Variables));
        }

        var result = await gateway.Executor.ExecuteAsync(
            request.Build(),
            cancellationToken);
        var json = result.ToJson(withIndentations: false);

        AuditAssertions.Assert(
            json,
            testCase.HasExpectedData ? testCase.ExpectedData : null,
            testCase.HasExpectedErrors ? testCase.ExpectsErrors : null);
    }

    /// <inheritdoc />
    public ValueTask InitializeAsync() => ValueTask.CompletedTask;

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (_gateway is not null)
        {
            await _gateway.DisposeAsync();
            _gateway = null;
        }
    }

    private static IReadOnlyDictionary<string, object?> ParseVariables(string json)
    {
        using var document = JsonDocument.Parse(json);

        if (document.RootElement.ValueKind is not JsonValueKind.Object)
        {
            throw new InvalidOperationException("Official audit variables must be a JSON object.");
        }

        return (IReadOnlyDictionary<string, object?>)ParseValue(document.RootElement)!;
    }

    private static object? ParseValue(JsonElement element)
        => element.ValueKind switch
        {
            JsonValueKind.Object => element
                .EnumerateObject()
                .ToDictionary(property => property.Name, property => ParseValue(property.Value)),
            JsonValueKind.Array => element.EnumerateArray().Select(ParseValue).ToArray(),
            JsonValueKind.String => element.GetString(),
            JsonValueKind.Number when element.TryGetInt64(out var value) => value,
            JsonValueKind.Number => element.GetDouble(),
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.Null => null,
            _ => throw new InvalidOperationException(
                $"Unsupported official audit variable value kind '{element.ValueKind}'.")
        };
}
