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

public class FileUploadTests
{
    private readonly Func<ICompositionLog> _logFactory;

    public FileUploadTests(ITestOutputHelper output)
    {
        _logFactory = () => new TestCompositionLog(output);
    }

    [Fact]
    public async Task AutoCompose()
    {
        // arrange
        using var demoProject = await DemoProject.CreateAsync();

        // act
        var fusionGraph = await new FusionGraphComposer(logFactory: _logFactory).ComposeAsync(
            new[]
            {
                demoProject.Reviews2.ToConfiguration(Reviews2ExtensionSdl, onlyHttp: true),
                demoProject.Accounts.ToConfiguration(AccountsExtensionSdl, onlyHttp: true),
                demoProject.Products.ToConfiguration(ProductsExtensionSdl, onlyHttp: true),
                demoProject.Shipping.ToConfiguration(ShippingExtensionSdl, onlyHttp: true),
            });

        // assert
        fusionGraph.MatchSnapshot(extension: ".graphql");
    }

    [Fact]
    public async Task UploadFile()
    {
        // arrange
        using var cts = new CancellationTokenSource(10_000);
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
            mutation Upload($file: Upload!) {
                uploadProductPicture(input: { productId: 1, file: $file }) {
                  boolean
                }
            }
            """);

        var stream = new MemoryStream("abc"u8.ToArray());

        // act
        var result = await executor.ExecuteAsync(
            OperationRequestBuilder
                .New()
                .SetDocument(request)
                .SetVariableValues(new Dictionary<string, object?> { {"file", new StreamFile("abc", () => stream) }, })
                .Build(),
            cts.Token);

        // assert
        var snapshot = new Snapshot();
        CollectSnapshotData(snapshot, request, result);
        await snapshot.MatchMarkdownAsync(cts.Token);
    }

    [Fact]
    public async Task UploadFile_2()
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
    public async Task UploadFile_Multiple_In_List()
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
            mutation UploadMultiple($input: UploadMultipleProductPicturesInput!) {
                uploadMultipleProductPictures(input: $input) {
                  boolean
                }
            }
            """);

        var input = new Dictionary<string, object?>
        {
            ["products"] = new List<Dictionary<string, object?>> {
                new () {
                    ["productId"] = 1,
                    ["file"] = new StreamFile("abc", () => new MemoryStream("abc"u8.ToArray())),
                },
                new () {
                    ["productId"] = 2,
                    ["file"] = new StreamFile("abc", () => new MemoryStream("abc"u8.ToArray())),
                },
            },
        };

        // act
        var result = await executor.ExecuteAsync(
            OperationRequestBuilder
                .New()
                .SetDocument(request)
                .SetVariableValues(new Dictionary<string, object?> { { "input", input } })
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
