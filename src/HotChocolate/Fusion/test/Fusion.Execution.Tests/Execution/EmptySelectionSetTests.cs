using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using System.Text;
using HotChocolate.Execution;
using HotChocolate.Fusion.Configuration;
using HotChocolate.Fusion.Execution.Clients;
using HotChocolate.Fusion.Text.Json;
using HotChocolate.Fusion.Types;
using HotChocolate.Language;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Fusion.Execution;

public sealed class EmptySelectionSetTests : FusionTestBase
{
    private const string SourceSchema =
        """
        type Query {
          me: User
        }

        type Mutation {
          updateMe: User
        }

        type Subscription {
          onMessage: User
        }

        type User {
          name: String
        }
        """;

    [Fact]
    public async Task ExecuteAsync_Should_InjectAndHideTypeName_When_CompositeFieldSelectionSetIsEmpty()
    {
        // arrange
        var client = new RecordingClient();
        var executor = await CreateExecutorAsync(client, enableEmptySelectionSets: true);

        // act
        await using var result = await executor.ExecuteAsync(
            OperationRequestBuilder.New().SetDocument("{ me { } }").Build(),
            TestContext.Current.CancellationToken);

        // assert
        result.MatchInlineSnapshot(
            """
            {
              "data": {
                "me": {}
              }
            }
            """);
        Assert.Equal("__typename", GetOnlyNestedSelectionName(Assert.Single(client.Requests)));
    }

    [Theory]
    [InlineData("query { }")]
    [InlineData("mutation { }")]
    public async Task ExecuteAsync_Should_ReturnEmptyDataWithoutSourceRequests_When_RootSelectionSetIsEmpty(
        string document)
    {
        // arrange
        var client = new RecordingClient();
        var executor = await CreateExecutorAsync(client, enableEmptySelectionSets: true);

        // act
        await using var result = await executor.ExecuteAsync(
            OperationRequestBuilder.New().SetDocument(document).Build(),
            TestContext.Current.CancellationToken);

        // assert
        result.MatchInlineSnapshot(
            """
            {
              "data": {}
            }
            """);
        Assert.Empty(client.Requests);
    }

    [Fact]
    public async Task ExecuteAsync_Should_ReturnValidationErrorWithoutSourceRequests_When_EmptySubscriptionIsEnabled()
    {
        // arrange
        var client = new RecordingClient();
        var executor = await CreateExecutorAsync(client, enableEmptySelectionSets: true);

        // act
        await using var result = await executor.ExecuteAsync(
            OperationRequestBuilder.New().SetDocument("subscription { }").Build(),
            TestContext.Current.CancellationToken);

        // assert
        GetErrorMessages(result).MatchInlineSnapshots(
        [
            "Operation `Unnamed` has an empty selection set. Root types without selections are disallowed.",
            "Subscription operations must have exactly one root field."
        ]);
        Assert.Empty(client.Requests);
    }

    [Fact]
    public async Task ExecuteAsync_Should_ReturnValidationErrorWithoutSourceRequests_When_EmptySelectionSetsAreDisabled()
    {
        // arrange
        var client = new RecordingClient();
        var executor = await CreateExecutorAsync(client, enableEmptySelectionSets: false);

        // act
        await using var result = await executor.ExecuteAsync(
            OperationRequestBuilder.New().SetDocument("{ me { } }").Build(),
            TestContext.Current.CancellationToken);

        // assert
        GetErrorMessages(result).MatchInlineSnapshots(
        [
            "Field \"me\" of type \"User\" must have a selection of subfields. Did you mean \"me { ... }\"?"
        ]);
        Assert.Empty(client.Requests);
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
        GetErrorMessages(result).MatchInlineSnapshots(
        [
            "Field \"me\" of type \"User\" must have a selection of subfields. Did you mean \"me { ... }\"?"
        ]);
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

    private static string GetOnlyNestedSelectionName(SourceSchemaClientRequest request)
    {
        var rootSelections = new List<Utf8SelectionNode>();

        foreach (var operation in request.OperationDocument.GetOperations())
        {
            foreach (var selection in operation.SelectionSet.GetSelections())
            {
                rootSelections.Add(selection);
            }
        }

        var rootField = Assert.Single(rootSelections).GetField();
        var nestedSelections = new List<Utf8SelectionNode>();

        foreach (var selection in rootField.SelectionSet.GetSelections())
        {
            nestedSelections.Add(selection);
        }

        return Encoding.UTF8.GetString(Assert.Single(nestedSelections).GetField().Utf8Name);
    }

    private static IEnumerable<string> GetErrorMessages(IExecutionResult result)
        => result.ExpectOperationResult().Errors!.Select(error => error.Message);

    private sealed class RecordingClient : ISourceSchemaClient
    {
        private static readonly byte[] s_response = """{"data":{"me":{"__typename":"User"}}}"""u8.ToArray();
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

            var document = SourceResultDocument.Parse(
                context.MemorySource.GetNextArena(),
                s_response,
                s_response.Length);

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
