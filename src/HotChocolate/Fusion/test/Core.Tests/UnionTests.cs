using HotChocolate.Execution;
using HotChocolate.Fusion.Clients;
using HotChocolate.Fusion.Composition;
using HotChocolate.Fusion.Shared;
using HotChocolate.Skimmed.Serialization;
using HotChocolate.Types;
using HotChocolate.Types.Relay;
using Microsoft.Extensions.DependencyInjection;
using Xunit.Abstractions;
using static HotChocolate.Fusion.Shared.DemoProjectSchemaExtensions;
using static HotChocolate.Language.Utf8GraphQLParser;
using static HotChocolate.Fusion.TestHelper;

namespace HotChocolate.Fusion;

public class UnionTests(ITestOutputHelper output)
{
    private readonly Func<ICompositionLog> _logFactory = () => new TestCompositionLog(output);

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

    [Fact]
    public async Task Union_Two_Branches_With_Differing_Resolve_Nodes_Item1()
    {
        // arrange
        var subgraphA = await TestSubgraph.CreateAsync(
            configure: builder =>
            {
                builder
                    .AddQueryType<SubgraphA_Query>()
                    .AddType<ISomeUnion>()
                    .AddType<SubgraphA_Item1>()
                    .AddType<SubgraphA_Item2>()
                    .AddType<SubgraphA_Item3>()
                    .AddType<SubgraphA_Product>()
                    .AddType<SubgraphA_Review>()
                    .ModifyOptions(o => o.EnsureAllNodesCanBeResolved = false)
                    .AddGlobalObjectIdentification();
            });

        var subgraphB = await TestSubgraph.CreateAsync(
            configure: builder =>
            {
                builder
                    .AddQueryType()
                    .AddType<SubgraphB_Product>()
                    .AddGlobalObjectIdentification();
            });

        var subgraphC = await TestSubgraph.CreateAsync(
            configure: builder =>
            {
                builder
                    .AddQueryType()
                    .AddType<SubgraphB_Review>()
                    .AddGlobalObjectIdentification();
            });

        using var subgraphs = new TestSubgraphCollection(output, [subgraphA, subgraphB, subgraphC]);
        var executor = await subgraphs.GetExecutorAsync();
        var request = Parse("""
                      query {
                        union(item: 1) {
                          ... on Item1 {
                            something
                            product {
                              id
                              name
                            }
                          }
                          ... on Item3 {
                            another
                            review {
                              id
                              score
                            }
                          }
                        }
                      }
                      """);

        // act
        var result = await executor.ExecuteAsync(
            OperationRequestBuilder
                .New()
                .SetDocument(request)
                .Build());

        // assert
        var snapshot = new Snapshot();
        CollectSnapshotData(snapshot, request, result);
        await snapshot.MatchMarkdownAsync();
        Assert.False(subgraphC.HasReceivedRequest);
    }

    [Fact]
    public async Task Union_Two_Branches_With_Differing_Resolve_Nodes_Item2()
    {
        // arrange
        var subgraphA = await TestSubgraph.CreateAsync(
            configure: builder =>
            {
                builder
                    .AddQueryType<SubgraphA_Query>()
                    .AddType<ISomeUnion>()
                    .AddType<SubgraphA_Item1>()
                    .AddType<SubgraphA_Item2>()
                    .AddType<SubgraphA_Item3>()
                    .AddType<SubgraphA_Product>()
                    .AddType<SubgraphA_Review>()
                    .ModifyOptions(o => o.EnsureAllNodesCanBeResolved = false)
                    .AddGlobalObjectIdentification();
            });

        var subgraphB = await TestSubgraph.CreateAsync(
            configure: builder =>
            {
                builder
                    .AddQueryType()
                    .AddType<SubgraphB_Product>()
                    .AddGlobalObjectIdentification();
            });

        var subgraphC = await TestSubgraph.CreateAsync(
            configure: builder =>
            {
                builder
                    .AddQueryType()
                    .AddType<SubgraphB_Review>()
                    .AddGlobalObjectIdentification();
            });

        using var subgraphs = new TestSubgraphCollection(output, [subgraphA, subgraphB, subgraphC]);
        var executor = await subgraphs.GetExecutorAsync();
        var request = Parse("""
                      query {
                        union(item: 3) {
                          ... on Item1 {
                            something
                            product {
                              id
                              name
                            }
                          }
                          ... on Item3 {
                            another
                            review {
                              id
                              score
                            }
                          }
                        }
                      }
                      """);

        // act
        var result = await executor.ExecuteAsync(
            OperationRequestBuilder
                .New()
                .SetDocument(request)
                .Build());

        // assert
        var snapshot = new Snapshot();
        CollectSnapshotData(snapshot, request, result);
        await snapshot.MatchMarkdownAsync();
        Assert.False(subgraphB.HasReceivedRequest);
    }

    [Fact(Skip = "Fix with new planner")]
    public async Task Union_List_With_Differing_Union_Item_Dependencies_SameSelections()
    {
        // arrange
        var subgraphA = await TestSubgraph.CreateAsync(
            configure: builder =>
            {
                builder
                    .AddQueryType<SubgraphA_Query>()
                    .AddType<ISomeUnion>()
                    .AddType<SubgraphA_Item1>()
                    .AddType<SubgraphA_Item2>()
                    .AddType<SubgraphA_Item3>()
                    .AddType<SubgraphA_Product>()
                    .AddType<SubgraphA_Review>()
                    .ModifyOptions(o => o.EnsureAllNodesCanBeResolved = false)
                    .AddGlobalObjectIdentification();
            });

        var subgraphB = await TestSubgraph.CreateAsync(
            configure: builder =>
            {
                builder
                    .AddQueryType()
                    .AddType<SubgraphB_Product>()
                    .AddType<SubgraphB_Review>()
                    .AddGlobalObjectIdentification();
            });

        using var subgraphs = new TestSubgraphCollection(output, [subgraphA, subgraphB]);
        var executor = await subgraphs.GetExecutorAsync();
        var request = Parse("""
                      query {
                        listOfUnion {
                          __typename
                          ... on Item1 {
                            something
                            product {
                              id
                              name
                            }
                          }
                          ... on Item2 {
                            other
                            product {
                              id
                              name
                            }
                          }
                          ... on Item3 {
                            another
                            review {
                              id
                              score
                            }
                          }
                        }
                      }
                      """);

        // act
        var result = await executor.ExecuteAsync(
            OperationRequestBuilder
                .New()
                .SetDocument(request)
                .Build());

        // assert
        var snapshot = new Snapshot();
        CollectSnapshotData(snapshot, request, result);
        await snapshot.MatchMarkdownAsync();
        // Ideally it would just be one request, but that's for another day...
        Assert.Equal(3, subgraphB.NumberOfReceivedRequests);
    }

    [Fact]
    public async Task Union_List_With_Differing_Union_Item_Dependencies()
    {
        // arrange
        var subgraphA = await TestSubgraph.CreateAsync(
            configure: builder =>
            {
                builder
                    .AddQueryType<SubgraphA_Query>()
                    .AddType<ISomeUnion>()
                    .AddType<SubgraphA_Item1>()
                    .AddType<SubgraphA_Item2>()
                    .AddType<SubgraphA_Item3>()
                    .AddType<SubgraphA_Product>()
                    .AddType<SubgraphA_Review>()
                    .ModifyOptions(o => o.EnsureAllNodesCanBeResolved = false)
                    .AddGlobalObjectIdentification();
            });

        var subgraphB = await TestSubgraph.CreateAsync(
            configure: builder =>
            {
                builder
                    .AddQueryType()
                    .AddType<SubgraphB_Product>()
                    .AddType<SubgraphB_Review>()
                    .AddGlobalObjectIdentification();
            });

        using var subgraphs = new TestSubgraphCollection(output, [subgraphA, subgraphB]);
        var executor = await subgraphs.GetExecutorAsync();
        var request = Parse("""
                      query {
                        listOfUnion {
                          __typename
                          ... on Item1 {
                            something
                            product {
                              id
                              name
                            }
                          }
                          ... on Item2 {
                            other
                            product {
                              name
                            }
                          }
                          ... on Item3 {
                            another
                            review {
                              id
                              score
                            }
                          }
                        }
                      }
                      """);

        // act
        var result = await executor.ExecuteAsync(
            OperationRequestBuilder
                .New()
                .SetDocument(request)
                .Build());

        // assert
        var snapshot = new Snapshot();
        CollectSnapshotData(snapshot, request, result);
        await snapshot.MatchMarkdownAsync();
        // Ideally it would just be one request, but that's for another day...
        Assert.Equal(3, subgraphB.NumberOfReceivedRequests);
    }

    [ObjectType("Query")]
    public class SubgraphA_Query
    {
        public ISomeUnion GetUnion(int item)
        {
            return item switch
            {
                1 => new SubgraphA_Item1("Something", new SubgraphA_Product(1)),
                _ => new SubgraphA_Item3(true, new SubgraphA_Review(2))
            };
        }

        public List<ISomeUnion> GetListOfUnion()
        {
            return
            [
                new SubgraphA_Item1("Something", new SubgraphA_Product(1)),
                new SubgraphA_Item2(123, new SubgraphA_Product(2)),
                new SubgraphA_Item3(true, new SubgraphA_Review(3)),
                new SubgraphA_Item1("Something", new SubgraphA_Product(4)),
                new SubgraphA_Item2(123, new SubgraphA_Product(5)),
                new SubgraphA_Item3(true, new SubgraphA_Review(6))
            ];
        }
    }

    [UnionType("SomeUnion")]
    public interface ISomeUnion
    {
    }

    [ObjectType("Item1")]
    public record SubgraphA_Item1(string Something, SubgraphA_Product Product) : ISomeUnion;

    [ObjectType("Item2")]
    public record SubgraphA_Item2(int Other, SubgraphA_Product Product) : ISomeUnion;

    [ObjectType("Item3")]
    public record SubgraphA_Item3(bool Another, SubgraphA_Review Review) : ISomeUnion;

    [Node]
    [ObjectType("Product")]
    public record SubgraphA_Product(int Id);

    [Node]
    [ObjectType("Product")]
    public record SubgraphB_Product(int Id, string Name)
    {
        [NodeResolver]
        public static SubgraphB_Product Get(int id)
            => new SubgraphB_Product(id, "Product_" + id);
    }

    [Node]
    [ObjectType("Review")]
    public record SubgraphA_Review(int Id);

    [Node]
    [ObjectType("Review")]
    public record SubgraphB_Review(int Id, int Score)
    {
        [NodeResolver]
        public static SubgraphB_Review Get(int id)
            => new SubgraphB_Review(id, id % 5);
    }

    private sealed class NoWebSockets : IWebSocketConnectionFactory
    {
        public IWebSocketConnection CreateConnection(string name)
        {
            throw new NotSupportedException();
        }
    }
}
