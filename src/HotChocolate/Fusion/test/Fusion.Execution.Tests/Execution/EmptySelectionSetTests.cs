using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using System.Text;
using HotChocolate.Execution;
using HotChocolate.Fusion.Configuration;
using HotChocolate.Fusion.Execution.Clients;
using HotChocolate.Fusion.Text.Json;
using HotChocolate.Fusion.Types;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Fusion.Execution;

public sealed class EmptySelectionSetTests : FusionTestBase
{
    private const string SourceSchema =
        """
        type Query {
          me: User
          character: Character
        }

        type Mutation {
          updateMe: User
        }

        type Subscription {
          onMessage: User
        }

        interface Character {
          name: String
        }

        type User implements Character {
          name: String
        }

        type Bot implements Character {
          name: String
        }
        """;

    public static TheoryData<string, string, Dictionary<string, object?>?> Shapes =>
        new()
        {
            { "EmptyQuery", "query { }", null },
            { "EmptyMutation", "mutation { }", null },
            { "EmptySubscription", "subscription { }", null },
            { "EmptyCompositeField", "{ me { } }", null },
            { "EmptyInlineFragment", "{ me { ... on User { } } }", null },
            { "EmptyInlineFragmentUntyped", "{ me { ... { } } }", null },
            { "EmptyNamedFragment", "{ me { ...f } } fragment f on User { }", null },
            { "EmptyInlineFragmentOnAbstract", "{ character { ... on Bot { } } }", null },
            {
                "EmptyNamedFragmentBesideIncludedFieldTrue",
                "query($v: Boolean!) { me { name @include(if: $v) ...f } } fragment f on User { }",
                new Dictionary<string, object?> { ["v"] = true }
            },
            {
                "EmptyNamedFragmentBesideIncludedFieldFalse",
                "query($v: Boolean!) { me { name @include(if: $v) ...f } } fragment f on User { }",
                new Dictionary<string, object?> { ["v"] = false }
            },
            {
                "EmptyInlineFragmentBesideIncludedFieldTrue",
                "query($v: Boolean!) { me { name @include(if: $v) ... on User { } } }",
                new Dictionary<string, object?> { ["v"] = true }
            },
            {
                "EmptyInlineFragmentBesideIncludedFieldFalse",
                "query($v: Boolean!) { me { name @include(if: $v) ... on User { } } }",
                new Dictionary<string, object?> { ["v"] = false }
            }
        };

    [Theory]
    [MemberData(nameof(Shapes))]
    public async Task ExecuteAsync_Should_MatchSnapshot_When_EmptySelectionSet(
        string name,
        string document,
        Dictionary<string, object?>? variables)
    {
        // arrange
        var enabledClient = new RecordingClient();
        var enabledExecutor = await CreateExecutorAsync(enabledClient, enableEmptySelectionSets: true);
        var disabledClient = new RecordingClient();
        var disabledExecutor = await CreateExecutorAsync(disabledClient, enableEmptySelectionSets: false);
        var request = OperationRequestBuilder.New()
            .SetDocument(document)
            .SetVariableValues(variables)
            .Build();

        // act
        await using var enabledResult = await enabledExecutor.ExecuteAsync(
            request,
            TestContext.Current.CancellationToken);
        var enabledSnapshot = CreateSnapshot(enabledResult, enabledClient);

        await using var disabledResult = await disabledExecutor.ExecuteAsync(
            request,
            TestContext.Current.CancellationToken);
        var disabledSnapshot = CreateSnapshot(disabledResult, disabledClient);

        // assert
        enabledSnapshot.MatchSnapshot(postFix: $"{name}_Enabled");
        disabledSnapshot.MatchSnapshot(postFix: $"{name}_Disabled");
    }

    [Fact]
    public async Task ExecuteAsync_Should_UseValidationOverrideWithoutSourceRequests_When_EmptySelectionSetsAreEnabled()
    {
        // arrange
        var client = new RecordingClient();
        var executor = await CreateExecutorAsync(
            client,
            enableEmptySelectionSets: true,
            disableEmptySelectionSetsInValidation: true);

        // act
        await using var result = await executor.ExecuteAsync(
            OperationRequestBuilder.New().SetDocument("{ me { } }").Build(),
            TestContext.Current.CancellationToken);

        // assert
        result.ToJson().MatchSnapshot(postFix: "ValidationOverride");
        Assert.Empty(client.Requests);
    }

    private static async Task<IRequestExecutor> CreateExecutorAsync(
        RecordingClient client,
        bool enableEmptySelectionSets,
        bool disableEmptySelectionSetsInValidation = false)
    {
        var services = new ServiceCollection();
        services.AddHttpClient();

        var builder = services
            .AddGraphQLGateway()
            .ModifyOptions(o => o.EnableEmptySelectionSets = enableEmptySelectionSets);

        if (disableEmptySelectionSetsInValidation)
        {
            builder.ConfigureValidation(
                (_, b) => b.ModifyOptions(o => o.EnableEmptySelectionSets = false));
        }

        builder.AddInMemoryConfiguration(ComposeSchemaDocument(SourceSchema));
        builder.Services.AddSingleton<ISourceSchemaClientFactory>(new RecordingClientFactory(client));

        FusionSetupUtilities.Configure(
            builder,
            setup => setup.ClientConfigurationModifiers.Add(_ => new RecordingClientConfiguration("a")));

        return await services.BuildGatewayAsync(TestContext.Current.CancellationToken);
    }

    private static string CreateSnapshot(IExecutionResult result, RecordingClient client)
    {
        var sourceRequests = string.Join(
            "\n\n",
            client.Requests.Select(
                request => Encoding.UTF8.GetString(request.OperationSourceText.Value.Span)));

        return $"{result.ToJson()}\n--- source requests ---\n{sourceRequests}";
    }

    private sealed class RecordingClient : ISourceSchemaClient
    {
        private static readonly byte[] s_meResponse =
            """{"data":{"me":{"__typename":"User","name":"Luke"}}}"""u8.ToArray();
        private static readonly byte[] s_characterResponse =
            """{"data":{"character":{"__typename":"Bot","name":"Bender"}}}"""u8.ToArray();
        private readonly List<SourceSchemaClientRequest> _requests = [];

        public IReadOnlyList<SourceSchemaClientRequest> Requests => _requests;

        public SourceSchemaClientCapabilities Capabilities => SourceSchemaClientCapabilities.None;

        public async IAsyncEnumerable<SourceSchemaResult> ExecuteAsync(
            OperationPlanContext context,
            SourceSchemaClientRequest request,
            [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            _requests.Add(request);
            await Task.Yield();

            var response = GetResponse(request);

            var document = SourceResultDocument.Parse(
                context.MemorySource.GetNextArena(),
                response,
                response.Length);

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

        private static byte[] GetResponse(SourceSchemaClientRequest request)
        {
            foreach (var operation in request.OperationDocument.GetOperations())
            {
                foreach (var selection in operation.SelectionSet.GetSelections())
                {
                    if (selection.GetField().Utf8Name.SequenceEqual("character"u8))
                    {
                        return s_characterResponse;
                    }
                }
            }

            return s_meResponse;
        }
    }

    private sealed class RecordingClientFactory(RecordingClient client) : ISourceSchemaClientFactory
    {
        public bool CanHandle(ISourceSchemaClientConfiguration configuration)
            => configuration is RecordingClientConfiguration;

        public ISourceSchemaClient CreateClient(
            FusionSchemaDefinition schema,
            ISourceSchemaClientConfiguration configuration)
            => client;
    }

    private sealed class RecordingClientConfiguration(string name) : ISourceSchemaClientConfiguration
    {
        public string Name { get; } = name;

        public SupportedOperationType SupportedOperations { get; } = SupportedOperationType.All;
    }
}
