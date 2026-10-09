using System.Net;
using System.Net.Http.Headers;
using System.Text;
using HotChocolate.Execution;
using HotChocolate.Language;
using HotChocolate.Resolvers;
using HotChocolate.Transport.Http;
using HotChocolate.Types.Composite;
using Microsoft.Extensions.DependencyInjection;
using OperationRequest = HotChocolate.Transport.OperationRequest;

namespace HotChocolate.Fusion;

public class SourceSchemaErrorTests : FusionTestBase
{
    #region Root

    [Theory]
    [InlineData(ErrorHandlingMode.Propagate)]
    [InlineData(ErrorHandlingMode.Null)]
    public async Task Error_On_Root_Field(ErrorHandlingMode onError)
    {
        // arrange
        using var server1 = CreateSourceSchema(
            "A",
            b => b.AddQueryType<SourceSchema3.Query>());

        using var gateway = await CreateCompositeSchemaAsync(
        [
            ("A", server1)
        ]);

        // act
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        var request = new OperationRequest(
            """
            {
              productById(id: 1) {
                name
              }
            }
            """,
            onError: onError);

        using var result = await client.PostAsync(
            request,
            new Uri("http://localhost:5000/graphql"),
            TestContext.Current.CancellationToken);

        // assert
        await MatchSnapshotAsync(gateway, request, result, postFix: "OnError_" + onError);
    }

    [Fact]
    public async Task OnError_SchemaDefault_Null_AppliesWithoutPerRequestOverride()
    {
        // arrange
        using var server1 = CreateSourceSchema(
            "A",
            b => b.AddQueryType<SourceSchema3.Query>());

        using var gateway = await CreateCompositeSchemaAsync(
        [
            ("A", server1)
        ],
        configureGatewayBuilder: builder =>
            builder.ModifyRequestOptions(o => o.DefaultErrorHandlingMode = ErrorHandlingMode.Null));

        // act — no per-request onError override
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        var request = new OperationRequest(
            """
            {
              productById(id: 1) {
                name
              }
            }
            """);

        using var result = await client.PostAsync(
            request,
            new Uri("http://localhost:5000/graphql"),
            TestContext.Current.CancellationToken);

        // assert
        await MatchSnapshotAsync(gateway, request, result);
    }

    [Fact]
    public async Task OnError_PerRequestOverride_IsIgnored_When_AllowErrorHandlingModeOverride_IsDisabled()
    {
        // arrange
        using var server1 = CreateSourceSchema(
            "A",
            b => b.AddQueryType<SourceSchema1.Query>());

        using var server2 = CreateSourceSchema(
            "B",
            b => b.AddQueryType<SourceSchema3.Query>());

        using var gateway = await CreateCompositeSchemaAsync(
        [
            ("A", server1),
            ("B", server2)
        ],
        configureGatewayBuilder: builder =>
            builder.ModifyRequestOptions(o =>
            {
                o.DefaultErrorHandlingMode = ErrorHandlingMode.Propagate;
                o.AllowErrorHandlingModeOverride = false;
            }));

        // act
        // Even though the request asks for Null, the gateway must ignore the override
        // and apply the configured Propagate mode (so data is fully omitted).
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        var request = new OperationRequest(
            """
            {
              topProduct {
                price
                name
              }
            }
            """,
            onError: ErrorHandlingMode.Null);

        using var result = await client.PostAsync(
            request,
            new Uri("http://localhost:5000/graphql"),
            TestContext.Current.CancellationToken);

        // assert
        await MatchSnapshotAsync(gateway, request, result);
    }

    [Fact]
    public async Task OnError_Null_OnSourceSchema_Forwards_To_Subgraph_Request()
    {
        // arrange
        using var server1 = CreateSourceSchema(
            "A",
            b => b.AddQueryType<SourceSchema3.Query>(),
            onError: ErrorHandlingMode.Null);

        using var gateway = await CreateCompositeSchemaAsync(
        [
            ("A", server1)
        ]);

        // act
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        var request = new OperationRequest(
            """
            {
              productById(id: 1) {
                name
              }
            }
            """);

        using var result = await client.PostAsync(
            request,
            new Uri("http://localhost:5000/graphql"),
            TestContext.Current.CancellationToken);

        // assert
        await MatchSnapshotAsync(gateway, request, result);
    }

    [Theory]
    [InlineData(ErrorHandlingMode.Propagate)]
    [InlineData(ErrorHandlingMode.Null)]
    public async Task Error_On_Root_Leaf(ErrorHandlingMode onError)
    {
        // arrange
        using var server1 = CreateSourceSchema(
            "A",
            b => b.AddQueryType<SourceSchema2.Query>());

        using var gateway = await CreateCompositeSchemaAsync(
        [
            ("A", server1)
        ]);

        // act
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        var request = new OperationRequest(
            """
            {
              productById(id: 1) {
                name
              }
            }
            """,
            onError: onError);

        using var result = await client.PostAsync(
            request,
            new Uri("http://localhost:5000/graphql"),
            TestContext.Current.CancellationToken);

        // assert
        await MatchSnapshotAsync(gateway, request, result, postFix: "OnError_" + onError);
    }

    [Theory]
    [InlineData(ErrorHandlingMode.Propagate)]
    [InlineData(ErrorHandlingMode.Null)]
    public async Task No_Data_And_Error_With_Path_For_Root_Field_NonNull(ErrorHandlingMode onError)
    {
        // arrange
        using var server1 = CreateSourceSchema(
            "A",
            b => b.AddQueryType<SourceSchema4.Query>());

        using var gateway = await CreateCompositeSchemaAsync(
        [
            ("A", server1)
        ]);

        // act
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        var request = new OperationRequest(
            """
            {
              productById(id: 1) {
                name
              }
            }
            """,
            onError: onError);

        using var result = await client.PostAsync(
            request,
            new Uri("http://localhost:5000/graphql"),
            TestContext.Current.CancellationToken);

        // assert
        await MatchSnapshotAsync(gateway, request, result, postFix: "OnError_" + onError);
    }

    [Theory]
    [InlineData(ErrorHandlingMode.Propagate)]
    [InlineData(ErrorHandlingMode.Null)]
    public async Task No_Data_And_Error_Without_Path_For_Root_Field(ErrorHandlingMode onError)
    {
        // arrange
        using var server1 = CreateSourceSchema(
            "A",
            b => b.AddQueryType<SourceSchema5.Query>()
                .UseRequest(
                    (_, _) =>
                    {
                        return context =>
                        {
                            context.Result = OperationResult.FromError(
                                ErrorBuilder.New()
                                    .SetMessage("A global error")
                                    .Build());

                            return ValueTask.CompletedTask;
                        };
                    },
                    key: "error",
                    before: WellKnownRequestMiddleware.OperationExecutionMiddleware));

        using var gateway = await CreateCompositeSchemaAsync(
        [
            ("A", server1)
        ]);

        // act
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        var request = new OperationRequest(
            """
            {
              productById(id: 1) {
                name
              }
            }
            """,
            onError: onError);

        using var result = await client.PostAsync(
            request,
            new Uri("http://localhost:5000/graphql"),
            TestContext.Current.CancellationToken);

        // assert
        await MatchSnapshotAsync(gateway, request, result, postFix: "OnError_" + onError);
    }

    [Theory]
    [InlineData(ErrorHandlingMode.Propagate)]
    [InlineData(ErrorHandlingMode.Null)]
    public async Task No_Data_And_Error_Without_Path_For_Root_Field_NonNull(ErrorHandlingMode onError)
    {
        // arrange
        using var server1 = CreateSourceSchema(
            "A",
            b => b.AddQueryType<SourceSchema6.Query>()
                .UseRequest(
                    (_, _) =>
                    {
                        return context =>
                        {
                            context.Result = OperationResult.FromError(
                                ErrorBuilder.New()
                                    .SetMessage("A global error")
                                    .Build());

                            return ValueTask.CompletedTask;
                        };
                    },
                    key: "error",
                    before: WellKnownRequestMiddleware.OperationExecutionMiddleware));

        using var gateway = await CreateCompositeSchemaAsync(
        [
            ("A", server1)
        ]);

        // act
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        var request = new OperationRequest(
            """
            {
              productById(id: 1) {
                name
              }
            }
            """,
            onError: onError);

        using var result = await client.PostAsync(
            request,
            new Uri("http://localhost:5000/graphql"),
            TestContext.Current.CancellationToken);

        // assert
        await MatchSnapshotAsync(gateway, request, result, postFix: "OnError_" + onError);
    }

    [Theory]
    [InlineData(ErrorHandlingMode.Propagate)]
    [InlineData(ErrorHandlingMode.Null)]
    public async Task SourceSchema_Request_Fails_For_Root_Field(ErrorHandlingMode onError)
    {
        // arrange
        using var server1 = CreateSourceSchema(
            "A",
            b => b.AddQueryType<SourceSchema1.Query>(),
            isOffline: true);

        using var gateway = await CreateCompositeSchemaAsync(
        [
            ("A", server1)
        ]);

        // act
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        var request = new OperationRequest(
            """
            {
              nullableTopProduct {
                price
              }
            }
            """,
            onError: onError);

        using var result = await client.PostAsync(
            request,
            new Uri("http://localhost:5000/graphql"),
            TestContext.Current.CancellationToken);

        // assert
        await MatchSnapshotAsync(gateway, request, result, postFix: "OnError_" + onError);
    }

    [Theory]
    [InlineData(ErrorHandlingMode.Propagate)]
    [InlineData(ErrorHandlingMode.Null)]
    public async Task SourceSchema_Request_Fails_For_Root_Field_NonNull(ErrorHandlingMode onError)
    {
        // arrange
        using var server1 = CreateSourceSchema(
            "A",
            b => b.AddQueryType<SourceSchema1.Query>(),
            isOffline: true);

        using var gateway = await CreateCompositeSchemaAsync(
        [
            ("A", server1)
        ]);

        // act
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        var request = new OperationRequest(
            """
            {
              topProduct {
                price
              }
            }
            """,
            onError: onError);

        using var result = await client.PostAsync(
            request,
            new Uri("http://localhost:5000/graphql"),
            TestContext.Current.CancellationToken);

        // assert
        await MatchSnapshotAsync(gateway, request, result, postFix: "OnError_" + onError);
    }

    #endregion

    #region Lookup

    [Theory]
    [InlineData(ErrorHandlingMode.Propagate)]
    [InlineData(ErrorHandlingMode.Null)]
    public async Task Error_On_Lookup_Leaf(ErrorHandlingMode onError)
    {
        // arrange
        using var server1 = CreateSourceSchema(
            "A",
            b => b.AddQueryType<SourceSchema1.Query>());

        using var server2 = CreateSourceSchema(
            "B",
            b => b.AddQueryType<SourceSchema2.Query>());

        using var gateway = await CreateCompositeSchemaAsync(
        [
            ("A", server1),
            ("B", server2)
        ]);

        // act
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        var request = new OperationRequest(
            """
            {
              topProduct {
                price
                name
              }
            }
            """,
            onError: onError);

        using var result = await client.PostAsync(
            request,
            new Uri("http://localhost:5000/graphql"),
            TestContext.Current.CancellationToken);

        // assert
        await MatchSnapshotAsync(gateway, request, result, postFix: "OnError_" + onError);
    }

    [Theory]
    [InlineData(ErrorHandlingMode.Propagate)]
    [InlineData(ErrorHandlingMode.Null)]
    public async Task Error_On_Lookup_Field(ErrorHandlingMode onError)
    {
        // arrange
        using var server1 = CreateSourceSchema(
            "A",
            b => b.AddQueryType<SourceSchema1.Query>());

        using var server2 = CreateSourceSchema(
            "B",
            b => b.AddQueryType<SourceSchema7.Query>());

        using var gateway = await CreateCompositeSchemaAsync(
        [
            ("A", server1),
            ("B", server2)
        ]);

        // act
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        var request = new OperationRequest(
            """
            {
              topProduct {
                price
                name
              }
            }
            """,
            onError: onError);

        using var result = await client.PostAsync(
            request,
            new Uri("http://localhost:5000/graphql"),
            TestContext.Current.CancellationToken);

        // assert
        await MatchSnapshotAsync(gateway, request, result, postFix: "OnError_" + onError);
    }

    [Theory]
    [InlineData(ErrorHandlingMode.Propagate)]
    [InlineData(ErrorHandlingMode.Null)]
    public async Task Error_On_Lookup_Leaf_NonNull(ErrorHandlingMode onError)
    {
        // arrange
        using var server1 = CreateSourceSchema(
            "A",
            b => b.AddQueryType<SourceSchema1.Query>());

        using var server2 = CreateSourceSchema(
            "B",
            b => b.AddQueryType<SourceSchema3.Query>());

        using var gateway = await CreateCompositeSchemaAsync(
        [
            ("A", server1),
            ("B", server2)
        ]);

        // act
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        var request = new OperationRequest(
            """
            {
              topProduct {
                price
                name
              }
            }
            """,
            onError: onError);

        using var result = await client.PostAsync(
            request,
            new Uri("http://localhost:5000/graphql"),
            TestContext.Current.CancellationToken);

        // assert
        await MatchSnapshotAsync(gateway, request, result, postFix: "OnError_" + onError);
    }

    [Theory]
    [InlineData(ErrorHandlingMode.Propagate)]
    [InlineData(ErrorHandlingMode.Null)]
    public async Task Error_On_Lookup_Field_In_List(ErrorHandlingMode onError)
    {
        // arrange
        using var server1 = CreateSourceSchema(
            "A",
            b => b.AddQueryType<SourceSchema1.Query>());

        using var server2 = CreateSourceSchema(
            "B",
            b => b.AddQueryType<SourceSchema7.Query>());

        using var gateway = await CreateCompositeSchemaAsync(
        [
            ("A", server1),
            ("B", server2)
        ]);

        // act
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        var request = new OperationRequest(
            """
            {
              topProducts {
                price
                name
              }
            }
            """,
            onError: onError);

        using var result = await client.PostAsync(
            request,
            new Uri("http://localhost:5000/graphql"),
            TestContext.Current.CancellationToken);

        // assert
        await MatchSnapshotAsync(gateway, request, result, postFix: "OnError_" + onError);
    }

    [Theory]
    [InlineData(ErrorHandlingMode.Propagate)]
    [InlineData(ErrorHandlingMode.Null)]
    public async Task Error_On_Lookup_Leaf_In_List(ErrorHandlingMode onError)
    {
        // arrange
        using var server1 = CreateSourceSchema(
            "A",
            b => b.AddQueryType<SourceSchema1.Query>());

        using var server2 = CreateSourceSchema(
            "B",
            b => b.AddQueryType<SourceSchema2.Query>());

        using var gateway = await CreateCompositeSchemaAsync(
        [
            ("A", server1),
            ("B", server2)
        ]);

        // act
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        var request = new OperationRequest(
            """
            {
              topProducts {
                price
                name
              }
            }
            """,
            onError: onError);

        using var result = await client.PostAsync(
            request,
            new Uri("http://localhost:5000/graphql"),
            TestContext.Current.CancellationToken);

        // assert
        await MatchSnapshotAsync(gateway, request, result, postFix: "OnError_" + onError);
    }

    [Theory]
    [InlineData(ErrorHandlingMode.Propagate)]
    [InlineData(ErrorHandlingMode.Null)]
    public async Task Error_On_Lookup_Leaf_In_List_NonNull(ErrorHandlingMode onError)
    {
        // arrange
        using var server1 = CreateSourceSchema(
            "A",
            b => b.AddQueryType<SourceSchema1.Query>());

        using var server2 = CreateSourceSchema(
            "B",
            b => b.AddQueryType<SourceSchema3.Query>());

        using var gateway = await CreateCompositeSchemaAsync(
        [
            ("A", server1),
            ("B", server2)
        ]);

        // act
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        var request = new OperationRequest(
            """
            {
              topProducts {
                price
                name
              }
            }
            """,
            onError: onError);

        using var result = await client.PostAsync(
            request,
            new Uri("http://localhost:5000/graphql"),
            TestContext.Current.CancellationToken);

        // assert
        await MatchSnapshotAsync(gateway, request, result, postFix: "OnError_" + onError);
    }

    [Theory]
    [InlineData(ErrorHandlingMode.Propagate)]
    [InlineData(ErrorHandlingMode.Null)]
    public async Task No_Data_And_Error_With_Path_For_Lookup_Leaf_NonNull(ErrorHandlingMode onError)
    {
        // arrange
        using var server1 = CreateSourceSchema(
            "A",
            b => b.AddQueryType<SourceSchema1.Query>());

        using var server2 = CreateSourceSchema(
            "B",
            b => b.AddQueryType<SourceSchema6.Query>());

        using var gateway = await CreateCompositeSchemaAsync(
        [
            ("A", server1),
            ("B", server2)
        ]);

        // act
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        var request = new OperationRequest(
            """
            {
              topProduct {
                price
                name
              }
            }
            """,
            onError: onError);

        using var result = await client.PostAsync(
            request,
            new Uri("http://localhost:5000/graphql"),
            TestContext.Current.CancellationToken);

        // assert
        await MatchSnapshotAsync(gateway, request, result, postFix: "OnError_" + onError);
    }

    [Theory]
    [InlineData(ErrorHandlingMode.Propagate)]
    [InlineData(ErrorHandlingMode.Null)]
    public async Task No_Data_And_Error_Without_Path_For_Lookup_Field_NonNull(ErrorHandlingMode onError)
    {
        // arrange
        using var server1 = CreateSourceSchema(
            "A",
            b => b.AddQueryType<SourceSchema1.Query>());

        using var server2 = CreateSourceSchema(
            "B",
            b => b.AddQueryType<SourceSchema5.Query>()
                .UseRequest(
                    (_, _) =>
                    {
                        return context =>
                        {
                            context.Result = OperationResult.FromError(
                                ErrorBuilder.New()
                                    .SetMessage("A global error")
                                    .Build());

                            return ValueTask.CompletedTask;
                        };
                    },
                    key: "error",
                    before: WellKnownRequestMiddleware.OperationExecutionMiddleware));

        using var gateway = await CreateCompositeSchemaAsync(
        [
            ("A", server1),
            ("B", server2)
        ]);

        // act
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        var request = new OperationRequest(
            """
            {
              topProduct {
                price
                name
              }
            }
            """,
            onError: onError);

        using var result = await client.PostAsync(
            request,
            new Uri("http://localhost:5000/graphql"),
            TestContext.Current.CancellationToken);

        // assert
        await MatchSnapshotAsync(gateway, request, result, postFix: "OnError_" + onError);
    }

    [Theory]
    [InlineData(ErrorHandlingMode.Propagate)]
    [InlineData(ErrorHandlingMode.Null)]
    public async Task SourceSchema_Request_Fails_For_Lookup(ErrorHandlingMode onError)
    {
        // arrange
        using var server1 = CreateSourceSchema(
            "A",
            b => b.AddQueryType<SourceSchema1.Query>());

        using var server2 = CreateSourceSchema(
            "B",
            b => b.AddQueryType<SourceSchema2.Query>(),
            isOffline: true);

        using var gateway = await CreateCompositeSchemaAsync(
        [
            ("A", server1),
            ("B", server2)
        ]);

        // act
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        var request = new OperationRequest(
            """
            {
              nullableTopProduct {
                price
                name
              }
            }
            """,
            onError: onError);

        using var result = await client.PostAsync(
            request,
            new Uri("http://localhost:5000/graphql"),
            TestContext.Current.CancellationToken);

        // assert
        await MatchSnapshotAsync(gateway, request, result, postFix: "OnError_" + onError);
    }

    [Theory]
    [InlineData(ErrorHandlingMode.Propagate)]
    [InlineData(ErrorHandlingMode.Null)]
    public async Task SourceSchema_Request_Fails_For_Lookup_NonNull(ErrorHandlingMode onError)
    {
        // arrange
        using var server1 = CreateSourceSchema(
            "A",
            b => b.AddQueryType<SourceSchema1.Query>());

        using var server2 = CreateSourceSchema(
            "B",
            b => b.AddQueryType<SourceSchema3.Query>(),
            isOffline: true);

        using var gateway = await CreateCompositeSchemaAsync(
        [
            ("A", server1),
            ("B", server2)
        ]);

        // act
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        var request = new OperationRequest(
            """
            {
              topProduct {
                price
                name
              }
            }
            """,
            onError: onError);

        using var result = await client.PostAsync(
            request,
            new Uri("http://localhost:5000/graphql"),
            TestContext.Current.CancellationToken);

        // assert
        await MatchSnapshotAsync(gateway, request, result, postFix: "OnError_" + onError);
    }

    [Theory]
    [InlineData(ErrorHandlingMode.Propagate)]
    [InlineData(ErrorHandlingMode.Null)]
    public async Task SourceSchema_Request_Fails_For_Lookup_On_List(ErrorHandlingMode onError)
    {
        // arrange
        using var server1 = CreateSourceSchema(
            "A",
            b => b.AddQueryType<SourceSchema1.Query>());

        using var server2 = CreateSourceSchema(
            "B",
            b => b.AddQueryType<SourceSchema2.Query>(),
            isOffline: true);

        using var gateway = await CreateCompositeSchemaAsync(
        [
            ("A", server1),
            ("B", server2)
        ]);

        // act
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        var request = new OperationRequest(
            """
            {
              topProducts {
                price
                name
              }
            }
            """,
            onError: onError);

        using var result = await client.PostAsync(
            request,
            new Uri("http://localhost:5000/graphql"),
            TestContext.Current.CancellationToken);

        // assert
        await MatchSnapshotAsync(gateway, request, result, postFix: "OnError_" + onError);
    }

    [Theory]
    [InlineData(ErrorHandlingMode.Propagate)]
    [InlineData(ErrorHandlingMode.Null)]
    public async Task SourceSchema_Request_Fails_For_Lookup_On_List_NonNull(ErrorHandlingMode onError)
    {
        // arrange
        using var server1 = CreateSourceSchema(
            "A",
            b => b.AddQueryType<SourceSchema1.Query>());

        using var server2 = CreateSourceSchema(
            "B",
            b => b.AddQueryType<SourceSchema3.Query>(),
            isOffline: true);

        using var gateway = await CreateCompositeSchemaAsync(
        [
            ("A", server1),
            ("B", server2)
        ]);

        // act
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        var request = new OperationRequest(
            """
            {
              topProducts {
                price
                name
              }
            }
            """,
            onError: onError);

        using var result = await client.PostAsync(
            request,
            new Uri("http://localhost:5000/graphql"),
            TestContext.Current.CancellationToken);

        // assert
        await MatchSnapshotAsync(gateway, request, result, postFix: "OnError_" + onError);
    }

    #endregion

    [Fact]
    public async Task Error_Extensions_From_Source_Schema_Are_Properly_Forwarded()
    {
        // arrange
        using var server1 = CreateSourceSchema(
            "A",
            b => b
                .AddQueryType<SourceSchema8.Query>()
                .ModifyRequestOptions(o => o.IncludeExceptionDetails = true));

        using var gateway = await CreateCompositeSchemaAsync(
        [
            ("A", server1)
        ]);

        // act
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        var request = new OperationRequest(
            """
            {
              someField
            }
            """);

        using var result = await client.PostAsync(
            request,
            new Uri("http://localhost:5000/graphql"),
            TestContext.Current.CancellationToken);

        // assert
        await MatchSnapshotAsync(gateway, request, result);
    }

    [Theory]
    [InlineData(ErrorHandlingMode.Propagate)]
    [InlineData(ErrorHandlingMode.Null)]
    public async Task Error_On_List_Field_With_Null_Element(ErrorHandlingMode onError)
    {
        // arrange
        using var server1 = CreateSourceSchema(
            "A",
            b => b.AddQueryType<SourceSchema9.Query>());

        using var gateway = await CreateCompositeSchemaAsync(
        [
            ("A", server1)
        ]);

        // act
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        var request = new OperationRequest(
            """
            {
              job {
                items {
                  name
                }
              }
            }
            """,
            onError: onError);

        using var result = await client.PostAsync(
            request,
            new Uri("http://localhost:5000/graphql"),
            TestContext.Current.CancellationToken);

        // assert
        await MatchSnapshotAsync(gateway, request, result, postFix: "OnError_" + onError);
    }

    [Theory]
    [InlineData(ErrorHandlingMode.Propagate)]
    [InlineData(ErrorHandlingMode.Null)]
    public async Task Error_Below_Null_List_Element(ErrorHandlingMode onError)
    {
        // arrange
        using var server1 = CreateSourceSchema(
            "A",
            b => b.AddQueryType<SourceSchema10.Query>());

        using var gateway = await CreateCompositeSchemaAsync(
        [
            ("A", server1)
        ]);

        // act
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        var request = new OperationRequest(
            """
            {
              job {
                items {
                  name
                }
              }
            }
            """,
            onError: onError);

        using var result = await client.PostAsync(
            request,
            new Uri("http://localhost:5000/graphql"),
            TestContext.Current.CancellationToken);

        // assert
        await MatchSnapshotAsync(gateway, request, result, postFix: "OnError_" + onError);
    }

    [Theory]
    [InlineData(ErrorHandlingMode.Propagate)]
    [InlineData(ErrorHandlingMode.Null)]
    public async Task Error_Below_Null_List_Element_With_Alias(ErrorHandlingMode onError)
    {
        // arrange
        using var server1 = CreateSourceSchema(
            "A",
            b => b.AddQueryType<SourceSchema10.Query>());

        using var gateway = await CreateCompositeSchemaAsync(
        [
            ("A", server1)
        ]);

        // act
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        var request = new OperationRequest(
            """
            {
              job {
                items {
                  title: name
                }
              }
            }
            """,
            onError: onError);

        using var result = await client.PostAsync(
            request,
            new Uri("http://localhost:5000/graphql"),
            TestContext.Current.CancellationToken);

        // assert
        await MatchSnapshotAsync(gateway, request, result, postFix: "OnError_" + onError);
    }

    [Theory]
    [InlineData(ErrorHandlingMode.Propagate)]
    [InlineData(ErrorHandlingMode.Null)]
    public async Task Error_Below_Null_List_Element_With_Deep_Path(ErrorHandlingMode onError)
    {
        // arrange
        // the error path continues 100000 segments below the selected leaf field
        var deepPath = string.Join(",", Enumerable.Repeat("\"child\"", 100_000));
        var sourceSchemaResponse =
            $$"""
            {
              "errors": [
                {
                  "message": "Could not resolve Item.name",
                  "path": ["job", "items", 0, "name", {{deepPath}}]
                }
              ],
              "data": { "job": { "items": [null] } }
            }
            """;

        using var server1 = CreateSourceSchema(
            "A",
            """
            type Query {
              job: Job!
            }

            type Job {
              items: [Item]!
            }

            type Item {
              name: String!
            }
            """,
            mockHttpResponse: _ => Task.FromResult(
                new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(
                        sourceSchemaResponse,
                        Encoding.UTF8,
                        new MediaTypeHeaderValue("application/json"))
                }));

        using var gateway = await CreateCompositeSchemaAsync(
            [("A", server1)],
            includeOperationPlan: false);

        // act
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        var request = new OperationRequest(
            """
            {
              job {
                items {
                  name
                }
              }
            }
            """,
            onError: onError);

        using var result = await client.PostAsync(
            request,
            new Uri("http://localhost:5000/graphql"),
            TestContext.Current.CancellationToken);

        // assert
        using var response = await result.ReadAsResultAsync(TestContext.Current.CancellationToken);
        response.MatchInlineSnapshot(
            """
            {
              "data": {
                "job": {
                  "items": [
                    null
                  ]
                }
              },
              "errors": [
                {
                  "message": "Could not resolve Item.name",
                  "path": [
                    "job",
                    "items",
                    0,
                    "name"
                  ]
                }
              ]
            }
            """);
    }

    public static class SourceSchema1
    {
        public class Query
        {
            public Product GetTopProduct() => new(1, 13.99);

            public Product? GetNullableTopProduct() => new(1, 13.99);

            public List<Product> GetTopProducts()
                => [new(1, 13.99), new(2, 13.99), new(3, 13.99)];

            [Lookup]
            [Internal]
            public Product? GetProductById(int id) => new(id, 13.99);
        }

        public record Product(int Id, double Price);
    }

    public static class SourceSchema2
    {
        public class Query
        {
            [Lookup]
            public Product? GetProductById(int id) => new(id);
        }

        public record Product(int Id)
        {
            public string? GetName(IResolverContext context)
            {
                throw new GraphQLException(ErrorBuilder.New().SetMessage("Could not resolve Product.name")
                    .SetPath(context.Path).Build());
            }
        }
    }

    public static class SourceSchema3
    {
        public class Query
        {
            [Lookup]
            public Product? GetProductById(int id, IResolverContext context)
                => throw new GraphQLException(ErrorBuilder.New().SetMessage("Could not resolve Product")
                    .SetPath(context.Path).Build());
        }

        public record Product(int Id)
        {
            public string GetName() => "Product " + Id;
        }
    }

    public static class SourceSchema4
    {
        public class Query
        {
            [Lookup]
            public Product? GetProductById(int id, IResolverContext context)
                => throw new GraphQLException(ErrorBuilder.New().SetMessage("Could not resolve Product")
                    .SetPath(context.Path).Build());
        }

        public record Product(int Id)
        {
            public string GetName() => "Product " + Id;
        }
    }

    public static class SourceSchema5
    {
        public class Query
        {
            [Lookup]
            public Product? GetProductById(int id) => null;
        }

        public record Product(int Id)
        {
            public string GetName() => "Product " + Id;
        }
    }

    public static class SourceSchema6
    {
        public class Query
        {
            [Lookup]
            public Product? GetProductById(int id) => new(id);
        }

        public record Product(int Id)
        {
            public string GetName(IResolverContext context)
            {
                throw new GraphQLException(ErrorBuilder.New().SetMessage("Could not resolve Product.name")
                    .SetPath(context.Path).Build());
            }
        }
    }

    public static class SourceSchema7
    {
        public class Query
        {
            [Lookup]
            public Product? GetProductById(int id, IResolverContext context)
                => throw new GraphQLException(ErrorBuilder.New().SetMessage("Could not resolve Product")
                    .SetPath(context.Path).Build());
        }

        public record Product(int Id)
        {
            public string? GetName() => "Product " + Id;
        }
    }

    public static class SourceSchema8
    {
        public class Query
        {
            public string SomeField(IResolverContext context)
            {
                throw new GraphQLException(
                    ErrorBuilder.New()
                        .SetMessage("Something went wrong")
                        .SetCode("SOME_ERROR")
                        .SetExtension("stringValue", "a-string")
                        .SetExtension("booleanValue", true)
                        .SetExtension("numberValue", 123)
                        .SetExtension("arrayValue", new[] { 1, 2, 3 })
                        .SetExtension("emptyArrayValue", Array.Empty<string>())
                        .SetPath(context.Path)
                        .SetException(new Exception("Some exception"))
                        .Build());
            }
        }
    }

    public static class SourceSchema9
    {
        public class Query
        {
            public Job GetJob() => new();
        }

        public class Job
        {
            public List<Item?> GetItems(IResolverContext context)
            {
                context.ReportError(
                    ErrorBuilder.New()
                        .SetMessage("Could not resolve Job.items")
                        .SetCode("NOT_FOUND")
                        .SetPath(context.Path)
                        .Build());
                return [null];
            }
        }

        public record Item(string Name);
    }

    public static class SourceSchema10
    {
        public class Query
        {
            public Job GetJob() => new();
        }

        public class Job
        {
            public List<Item?> GetItems() => [new Item()];
        }

        public class Item
        {
            public string GetName(IResolverContext context)
                => throw new GraphQLException(
                    ErrorBuilder.New()
                        .SetMessage("Could not resolve Item.name")
                        .SetCode("NOT_FOUND")
                        .SetPath(context.Path)
                        .Build());
        }
    }
}
