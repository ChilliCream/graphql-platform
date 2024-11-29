using HotChocolate.Execution;
using HotChocolate.Fusion.Clients;
using HotChocolate.Fusion.Composition;
using HotChocolate.Fusion.Shared;
using HotChocolate.Skimmed.Serialization;
using HotChocolate.Types;
using Microsoft.Extensions.DependencyInjection;
using Xunit.Abstractions;
using static HotChocolate.Fusion.Shared.DemoProjectSchemaExtensions;
using static HotChocolate.Language.Utf8GraphQLParser;
using static HotChocolate.Fusion.TestHelper;

namespace HotChocolate.Fusion;

public class UnionTests
{
    private readonly Func<ICompositionLog> _logFactory;

    public UnionTests(ITestOutputHelper output)
    {
        _logFactory = () => new TestCompositionLog(output);
    }

    [Fact]
    public async Task Error_Union_With_Inline_Fragment()
    {
        // arrange
        using var cts = new CancellationTokenSource(100_000);
        using var demoProject = await DemoProject.CreateAsync(cts.Token);

        // act
        var fusionGraph = await new FusionGraphComposer(logFactory: _logFactory).ComposeAsync(
            new[]
            {
                demoProject.Reviews2.ToConfiguration(Reviews2ExtensionSdl, onlyHttp: true),
                demoProject.Accounts.ToConfiguration(AccountsExtensionSdl, onlyHttp: true),
                demoProject.Products.ToConfiguration(ProductsExtensionSdl, onlyHttp: true),
                demoProject.Shipping.ToConfiguration(ShippingExtensionSdl, onlyHttp: true),
            },
            default,
            cts.Token);

        var executor = await new ServiceCollection()
            .AddSingleton(demoProject.HttpClientFactory)
            .AddSingleton<IWebSocketConnectionFactory>(new NoWebSockets())
            .AddFusionGatewayServer()
            .ConfigureFromDocument(SchemaFormatter.FormatAsDocument(fusionGraph))
            .BuildRequestExecutorAsync(cancellationToken: cts.Token);

        var request = Parse(
            """
            mutation Upload($input: UploadProductPictureInput!) {
                uploadProductPicture(input: $input) {
                  boolean
                  errors {
                     __typename
                     ... on ProductNotFoundError {
                       productId
                     }
                  }
                }
            }
            """);

        var input = new Dictionary<string, object?>()
        {
            ["productId"] = 1,
            ["file"] = new StreamFile("abc", () => new MemoryStream("abc"u8.ToArray())),
        };

        // act
        var result = await executor.ExecuteAsync(
            OperationRequestBuilder
                .New()
                .SetDocument(request)
                .SetVariableValues(new Dictionary<string, object?> { {"input", input }, })
                .Build(),
            cts.Token);

        // assert
        var snapshot = new Snapshot();
        CollectSnapshotData(snapshot, request, result);
        await snapshot.MatchMarkdownAsync(cts.Token);
    }

    [Fact]
    public async Task Error_Union_With_Inline_Fragment_Errors_Not_Null()
    {
        // arrange
        using var cts = new CancellationTokenSource(100_000);
        using var demoProject = await DemoProject.CreateAsync(cts.Token);

        // act
        var fusionGraph = await new FusionGraphComposer(logFactory: _logFactory).ComposeAsync(
            new[]
            {
                demoProject.Reviews2.ToConfiguration(Reviews2ExtensionSdl, onlyHttp: true),
                demoProject.Accounts.ToConfiguration(AccountsExtensionSdl, onlyHttp: true),
                demoProject.Products.ToConfiguration(ProductsExtensionSdl, onlyHttp: true),
                demoProject.Shipping.ToConfiguration(ShippingExtensionSdl, onlyHttp: true),
            },
            default,
            cts.Token);

        var executor = await new ServiceCollection()
            .AddSingleton(demoProject.HttpClientFactory)
            .AddSingleton<IWebSocketConnectionFactory>(new NoWebSockets())
            .AddFusionGatewayServer()
            .ConfigureFromDocument(SchemaFormatter.FormatAsDocument(fusionGraph))
            .BuildRequestExecutorAsync(cancellationToken: cts.Token);

        var request = Parse(
            """
            mutation Upload($input: UploadProductPictureInput!) {
                uploadProductPicture(input: $input) {
                  boolean
                  errors {
                     __typename
                     ... on ProductNotFoundError {
                       productId
                     }
                  }
                }
            }
            """);

        var input = new Dictionary<string, object?>()
        {
            ["productId"] = 0,
            ["file"] = new StreamFile("abc", () => new MemoryStream("abc"u8.ToArray())),
        };

        // act
        var result = await executor.ExecuteAsync(
            OperationRequestBuilder
                .New()
                .SetDocument(request)
                .SetVariableValues(new Dictionary<string, object?> { {"input", input}, })
                .Build(),
            cts.Token);

        // assert
        var snapshot = new Snapshot();
        CollectSnapshotData(snapshot, request, result);
        await snapshot.MatchMarkdownAsync(cts.Token);
    }

    [Fact]
    public async Task Error_Union_With_TypeName()
    {
        // arrange
        using var cts = new CancellationTokenSource(100_000);
        using var demoProject = await DemoProject.CreateAsync(cts.Token);

        // act
        var fusionGraph = await new FusionGraphComposer(logFactory: _logFactory).ComposeAsync(
            new[]
            {
                demoProject.Reviews2.ToConfiguration(Reviews2ExtensionSdl, onlyHttp: true),
                demoProject.Accounts.ToConfiguration(AccountsExtensionSdl, onlyHttp: true),
                demoProject.Products.ToConfiguration(ProductsExtensionSdl, onlyHttp: true),
                demoProject.Shipping.ToConfiguration(ShippingExtensionSdl, onlyHttp: true),
            },
            default,
            cts.Token);

        var executor = await new ServiceCollection()
            .AddSingleton(demoProject.HttpClientFactory)
            .AddSingleton<IWebSocketConnectionFactory>(new NoWebSockets())
            .AddFusionGatewayServer()
            .ConfigureFromDocument(SchemaFormatter.FormatAsDocument(fusionGraph))
            .BuildRequestExecutorAsync(cancellationToken: cts.Token);

        var request = Parse(
            """
            mutation Upload($input: UploadProductPictureInput!) {
                uploadProductPicture(input: $input) {
                  boolean
                  errors {
                     __typename
                  }
                }
            }
            """);

        var input = new Dictionary<string, object?>()
        {
            ["productId"] = 1,
            ["file"] = new StreamFile("abc", () => new MemoryStream("abc"u8.ToArray())),
        };

        // act
        var result = await executor.ExecuteAsync(
            OperationRequestBuilder
                .New()
                .SetDocument(request)
                .SetVariableValues(new Dictionary<string, object?> { {"input", input}, })
                .Build(),
            cts.Token);

        // assert
        var snapshot = new Snapshot();
        CollectSnapshotData(snapshot, request, result);
        await snapshot.MatchMarkdownAsync(cts.Token);
    }

    [Fact]
    public async Task Error_Union_With_TypeName_Errors_Not_Null()
    {
        // arrange
        using var cts = new CancellationTokenSource(100_000);
        using var demoProject = await DemoProject.CreateAsync(cts.Token);

        // act
        var fusionGraph = await new FusionGraphComposer(logFactory: _logFactory).ComposeAsync(
            new[]
            {
                demoProject.Reviews2.ToConfiguration(Reviews2ExtensionSdl, onlyHttp: true),
                demoProject.Accounts.ToConfiguration(AccountsExtensionSdl, onlyHttp: true),
                demoProject.Products.ToConfiguration(ProductsExtensionSdl, onlyHttp: true),
                demoProject.Shipping.ToConfiguration(ShippingExtensionSdl, onlyHttp: true),
            },
            default,
            cts.Token);

        var executor = await new ServiceCollection()
            .AddSingleton(demoProject.HttpClientFactory)
            .AddSingleton<IWebSocketConnectionFactory>(new NoWebSockets())
            .AddFusionGatewayServer()
            .ConfigureFromDocument(SchemaFormatter.FormatAsDocument(fusionGraph))
            .BuildRequestExecutorAsync(cancellationToken: cts.Token);

        var request = Parse(
            """
            mutation Upload($input: UploadProductPictureInput!) {
                uploadProductPicture(input: $input) {
                  boolean
                  errors {
                     __typename
                  }
                }
            }
            """);

        var input = new Dictionary<string, object?>()
        {
            ["productId"] = 0,
            ["file"] = new StreamFile("abc", () => new MemoryStream("abc"u8.ToArray())),
        };

        // act
        var result = await executor.ExecuteAsync(
            OperationRequestBuilder
                .New()
                .SetDocument(request)
                .SetVariableValues(new Dictionary<string, object?> { {"input", input}, })
                .Build(),
            cts.Token);

        // assert
        var snapshot = new Snapshot();
        CollectSnapshotData(snapshot, request, result);
        await snapshot.MatchMarkdownAsync(cts.Token);
    }

    private sealed class NoWebSockets : IWebSocketConnectionFactory
    {
        public IWebSocketConnection CreateConnection(string name)
        {
            throw new NotSupportedException();
        }
    }
}
