using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using System.Security.Claims;
using System.Text.Json;
using HotChocolate.Buffers;
using HotChocolate.Execution;
using HotChocolate.Fusion.Configuration;
using HotChocolate.Fusion.Execution.Clients;
using HotChocolate.Fusion.Policies.Rego;
using HotChocolate.Fusion.Text.Json;
using HotChocolate.Fusion.Types;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Fusion.Execution;

public sealed class RegoPolicyGatewayTests : FusionTestBase
{
    [Fact]
    public async Task ExecuteAsync_Should_EvaluateInMemoryRegoPolicy_When_RequestIsAuthorizedOrDenied()
    {
        // arrange
        var services = new ServiceCollection();
        services.AddHttpClient();

        var builder = services
            .AddGraphQLGateway()
            .AddInMemoryConfiguration(
                ComposeSchemaDocument(
                    """
                    # name: a
                    enum PolicyDenialBehavior { NULL ERROR ABORT }

                    directive @policy(names: [[String!]!]!, onDenied: PolicyDenialBehavior)
                      repeatable on OBJECT | FIELD_DEFINITION

                    type Query {
                      secret: String @policy(names: "CanReadSecret.allow", onDenied: NULL)
                    }
                    """),
                new JsonDocumentOwner(JsonDocument.Parse("{}")),
                CreatePolicies())
            .AddRegoPolicies();

        builder.Services.AddSingleton<ISourceSchemaClientFactory>(new TestClientFactory());
        FusionSetupUtilities.Configure(
            builder,
            setup => setup.ClientConfigurationModifiers.Add(
                _ => new TestClientConfiguration("a")));

        await using var serviceProvider = services.BuildServiceProvider();
        var executor = await serviceProvider
            .GetRequiredService<IRequestExecutorProvider>()
            .GetExecutorAsync(cancellationToken: TestContext.Current.CancellationToken);
        var allowedRequest = OperationRequestBuilder.New()
            .SetDocument("{ secret }")
            .SetUser(new ClaimsPrincipal(
                new ClaimsIdentity([new Claim(ClaimTypes.Role, "secret-reader")], "test")))
            .Build();
        var deniedRequest = OperationRequestBuilder.New()
            .SetDocument("{ secret }")
            .SetUser(new ClaimsPrincipal(new ClaimsIdentity()))
            .Build();

        // act
        await using var allowed = await executor.ExecuteAsync(
            allowedRequest,
            TestContext.Current.CancellationToken);
        await using var denied = await executor.ExecuteAsync(
            deniedRequest,
            TestContext.Current.CancellationToken);

        // assert
        allowed.ToJson().MatchInlineSnapshot(
            """
            {
              "data": {
                "secret": "classified"
              }
            }
            """);
        denied.ToJson().MatchInlineSnapshot(
            """
            {
              "data": {
                "secret": null
              }
            }
            """);
    }

    private static PolicyContentSnapshot CreatePolicies()
        => new(
            "rego",
            new Version(1, 0, 0),
            ImmutableArray.Create(
                new PolicyContent(
                    "CanReadSecret.allow",
                    PolicyContentType.Rego,
                    """
                    package CanReadSecret
                    import rego.v1

                    default allow := false
                    allow if { "secret-reader" in input.subject.roles }
                    """u8.ToArray(),
                    PolicyRequirements.Empty,
                    "CanReadSecret.allow"u8.ToArray())),
            "{}"u8.ToArray(),
            "{}"u8.ToArray(),
            dataOwner: null);

    private sealed class TestClientFactory : ISourceSchemaClientFactory
    {
        public bool CanHandle(ISourceSchemaClientConfiguration configuration)
            => configuration is TestClientConfiguration;

        public ISourceSchemaClient CreateClient(
            FusionSchemaDefinition schema,
            ISourceSchemaClientConfiguration configuration)
            => new TestClient();
    }

    private sealed class TestClient : ISourceSchemaClient
    {
        private static readonly byte[] s_payload = """{"data":{"secret":"classified"}}"""u8.ToArray();

        public SourceSchemaClientCapabilities Capabilities => SourceSchemaClientCapabilities.None;

        public async IAsyncEnumerable<SourceSchemaResult> ExecuteAsync(
            OperationPlanContext context,
            SourceSchemaClientRequest request,
            [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            var arena = context.MemorySource.GetNextArena();
            var document = SourceResultDocument.Parse(arena, s_payload, s_payload.Length);
            await Task.Yield();
            yield return new SourceSchemaResult(CompactPath.Root, document);
        }

        public IAsyncEnumerable<SourceSchemaBatchResult> ExecuteBatchAsync(
            OperationPlanContext context,
            ImmutableArray<SourceSchemaClientRequest> requests,
            CancellationToken cancellationToken)
            => throw new NotSupportedException();

        public IAsyncEnumerable<SourceSchemaResult> SubscribeAsync(
            OperationPlanContext context,
            SourceSchemaClientRequest request,
            CancellationToken cancellationToken)
            => throw new NotSupportedException();

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }

    private sealed class TestClientConfiguration(string name) : ISourceSchemaClientConfiguration
    {
        public string Name { get; } = name;

        public SupportedOperationType SupportedOperations => SupportedOperationType.Query;
    }
}
