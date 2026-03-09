using System.Diagnostics;
using HotChocolate.Execution;
using Microsoft.Extensions.DependencyInjection;
using static HotChocolate.Fusion.Diagnostics.ActivityTestHelper;

namespace HotChocolate.Fusion.Diagnostics;

[Collection("Instrumentation")]
public class QueryInstrumentationTests : FusionTestBase
{
    [Fact]
    public async Task Track_Events_Of_A_Simple_Query_Default()
    {
        using (CaptureActivities(out var activities))
        {
            // arrange
            using var server1 = CreateSourceSchema(
                "a",
                b => b.AddQueryType<Query>());

            using var gateway = await CreateCompositeSchemaAsync(
            [
                ("a", server1)
            ],
            configureGatewayBuilder: b => b.AddInstrumentation());

            // act
            var executor = await gateway.Services.GetRequestExecutorAsync();

            var request = OperationRequestBuilder.New()
                .SetDocument("{ sayHello }")
                .Build();

            // act
            await executor.ExecuteAsync(request);

            // assert
            activities.MatchSnapshot();
        }
    }

    [Fact]
    public async Task Track_Events_Of_A_Simple_Query_Default_Rename_Root()
    {
        using (CaptureActivities(out _))
        {
            // arrange
            using var server1 = CreateSourceSchema(
                "a",
                b => b.AddQueryType<Query>());

            using var gateway = await CreateCompositeSchemaAsync(
            [
                ("a", server1)
            ],
            configureGatewayBuilder: b => b.AddInstrumentation(o =>
            {
                o.RenameRootActivity = true;
                o.Scopes = FusionActivityScopes.All;
            }));

            var executor = await gateway.Services.GetRequestExecutorAsync();

            var request = OperationRequestBuilder.New()
                .SetDocument("{ sayHello }")
                .Build();

            // act
            await executor.ExecuteAsync(request);

            // assert
            Assert.Equal("CaptureActivities: query { sayHello }", Activity.Current!.DisplayName);
        }
    }

    [Fact]
    public async Task Parsing_Error_When_Rename_Root_Is_Activated()
    {
        using (CaptureActivities(out _))
        {
            // arrange
            using var server1 = CreateSourceSchema(
                "a",
                b => b.AddQueryType<Query>());

            using var gateway = await CreateCompositeSchemaAsync(
            [
                ("a", server1)
            ],
            configureGatewayBuilder: b => b.AddInstrumentation(o =>
            {
                o.RenameRootActivity = true;
                o.Scopes = FusionActivityScopes.All;
            }));

            var executor = await gateway.Services.GetRequestExecutorAsync();

            var request = OperationRequestBuilder.New()
                .SetDocument("{ sayHello")
                .Build();

            // act
            await executor.ExecuteAsync(request);

            // assert
            Assert.Equal("CaptureActivities: Begin Parse Document", Activity.Current!.DisplayName);
        }
    }

    [Fact]
    public async Task Validation_Error_When_Rename_Root_Is_Activated()
    {
        using (CaptureActivities(out _))
        {
            // arrange
            using var server1 = CreateSourceSchema(
                "a",
                b => b.AddQueryType<Query>());

            using var gateway = await CreateCompositeSchemaAsync(
            [
                ("a", server1)
            ],
            configureGatewayBuilder: b => b.AddInstrumentation(o =>
            {
                o.RenameRootActivity = true;
                o.Scopes = FusionActivityScopes.All;
            }));

            var executor = await gateway.Services.GetRequestExecutorAsync();

            var request = OperationRequestBuilder.New()
                .SetDocument("{ abc123 }")
                .Build();

            // act
            await executor.ExecuteAsync(request);

            // assert
            Assert.Equal("CaptureActivities: Begin Validate Document",
                Activity.Current!.DisplayName);
        }
    }

    [Fact]
    public async Task Create_Operation_Display_Name_With_1_Field()
    {
        using (CaptureActivities(out var activities))
        {
            // arrange
            using var server1 = CreateSourceSchema(
                "a",
                b => b.AddQueryType<Query>());

            using var gateway = await CreateCompositeSchemaAsync(
            [
                ("a", server1)
            ],
            configureGatewayBuilder: b => b.AddInstrumentation(o =>
            {
                o.RenameRootActivity = true;
                o.Scopes = FusionActivityScopes.All;
            }));

            var executor = await gateway.Services.GetRequestExecutorAsync();

            var request = OperationRequestBuilder.New()
                .SetDocument("{ a: sayHello }")
                .Build();

            // act
            await executor.ExecuteAsync(request);

            // assert
            activities.MatchSnapshot();
        }
    }

    [Fact]
    public async Task Create_Operation_Display_Name_With_1_Field_And_Op()
    {
        using (CaptureActivities(out var activities))
        {
            // arrange
            using var server1 = CreateSourceSchema(
                "a",
                b => b.AddQueryType<Query>());

            using var gateway = await CreateCompositeSchemaAsync(
            [
                ("a", server1)
            ],
            configureGatewayBuilder: b => b.AddInstrumentation(o =>
            {
                o.RenameRootActivity = true;
                o.Scopes = FusionActivityScopes.All;
            }));

            var executor = await gateway.Services.GetRequestExecutorAsync();

            var request = OperationRequestBuilder.New()
                .SetDocument("query GetA { a: sayHello }")
                .Build();

            // act
            await executor.ExecuteAsync(request);

            // assert
            activities.MatchSnapshot();
        }
    }

    [Fact]
    public async Task Create_Operation_Display_Name_With_3_Field()
    {
        using (CaptureActivities(out var activities))
        {
            // arrange
            using var server1 = CreateSourceSchema(
                "a",
                b => b.AddQueryType<Query>());

            using var gateway = await CreateCompositeSchemaAsync(
            [
                ("a", server1)
            ],
            configureGatewayBuilder: b => b.AddInstrumentation(o =>
            {
                o.RenameRootActivity = true;
                o.Scopes = FusionActivityScopes.All;
            }));

            var executor = await gateway.Services.GetRequestExecutorAsync();

            var request = OperationRequestBuilder.New()
                .SetDocument("{ a: sayHello b: sayHello c: sayHello }")
                .Build();

            // act
            await executor.ExecuteAsync(request);

            // assert
            activities.MatchSnapshot();
        }
    }

    [Fact]
    public async Task Create_Operation_Display_Name_With_4_Field()
    {
        using (CaptureActivities(out var activities))
        {
            // arrange
            using var server1 = CreateSourceSchema(
                "a",
                b => b.AddQueryType<Query>());

            using var gateway = await CreateCompositeSchemaAsync(
            [
                ("a", server1)
            ],
            configureGatewayBuilder: b => b.AddInstrumentation(o =>
            {
                o.RenameRootActivity = true;
                o.Scopes = FusionActivityScopes.All;
            }));

            var executor = await gateway.Services.GetRequestExecutorAsync();

            var request = OperationRequestBuilder.New()
                .SetDocument("{ a: sayHello b: sayHello c: sayHello d: sayHello }")
                .Build();

            // act
            await executor.ExecuteAsync(request);

            // assert
            activities.MatchSnapshot();
        }
    }

    [Fact]
    public async Task Track_Events_Of_A_Simple_Query_Detailed()
    {
        using (CaptureActivities(out var activities))
        {
            // arrange
            using var server1 = CreateSourceSchema(
                "a",
                b => b.AddQueryType<Query>());

            using var gateway = await CreateCompositeSchemaAsync(
            [
                ("a", server1)
            ],
            configureGatewayBuilder: b => b.AddInstrumentation(o =>
                o.Scopes = FusionActivityScopes.All));

            var executor = await gateway.Services.GetRequestExecutorAsync();

            var request = OperationRequestBuilder.New()
                .SetDocument("{ sayHello }")
                .Build();

            // act
            await executor.ExecuteAsync(request);

            // assert
            activities.MatchSnapshot();
        }
    }

    [Fact]
    public async Task Ensure_Operation_Name_Is_Used_As_Request_Name()
    {
        using (CaptureActivities(out var activities))
        {
            // arrange
            using var server1 = CreateSourceSchema(
                "a",
                b => b.AddQueryType<Query>());

            using var gateway = await CreateCompositeSchemaAsync(
            [
                ("a", server1)
            ],
            configureGatewayBuilder: b => b.AddInstrumentation(o =>
                o.Scopes = FusionActivityScopes.All));

            var executor = await gateway.Services.GetRequestExecutorAsync();

            var request = OperationRequestBuilder.New()
                .SetDocument("query SayHelloOperation { sayHello }")
                .Build();

            // act
            await executor.ExecuteAsync(request);

            // assert
            activities.MatchSnapshot();
        }
    }

    [Fact]
    public async Task Allow_Document_To_Be_Captured()
    {
        using (CaptureActivities(out var activities))
        {
            // arrange
            using var server1 = CreateSourceSchema(
                "a",
                b => b.AddQueryType<Query>());

            using var gateway = await CreateCompositeSchemaAsync(
            [
                ("a", server1)
            ],
            configureGatewayBuilder: b => b.AddInstrumentation(o =>
            {
                o.Scopes = FusionActivityScopes.All;
                o.IncludeDocument = true;
            }));

            var executor = await gateway.Services.GetRequestExecutorAsync();

            var request = OperationRequestBuilder.New()
                .SetDocument("query SayHelloOperation { sayHello }")
                .Build();

            // act
            await executor.ExecuteAsync(request);

            // assert
            activities.MatchSnapshot();
        }
    }

    [Fact]
    public async Task Ensure_That_The_Validation_Activity_Has_An_Error_Status()
    {
        using (CaptureActivities(out var activities))
        {
            // arrange
            using var server1 = CreateSourceSchema(
                "a",
                b => b.AddQueryType<Query>());

            using var gateway = await CreateCompositeSchemaAsync(
            [
                ("a", server1)
            ],
            configureGatewayBuilder: b => b.AddInstrumentation(o =>
            {
                o.Scopes = FusionActivityScopes.All;
                o.IncludeDocument = true;
            }));

            var executor = await gateway.Services.GetRequestExecutorAsync();

            var request = OperationRequestBuilder.New()
                .SetDocument("query SayHelloOperation { sayHello_ }")
                .Build();

            // act
            await executor.ExecuteAsync(request);

            // assert
            activities.MatchSnapshot();
        }
    }

    [Fact]
    public async Task Cause_A_Resolver_Error_That_Deletes_The_Whole_Result()
    {
        using (CaptureActivities(out var activities))
        {
            // arrange
            using var server1 = CreateSourceSchema(
                "a",
                b => b.AddQueryType<Query>());

            using var gateway = await CreateCompositeSchemaAsync(
            [
                ("a", server1)
            ],
            configureGatewayBuilder: b => b.AddInstrumentation(o =>
            {
                o.Scopes = FusionActivityScopes.All;
                o.IncludeDocument = true;
            }));

            var executor = await gateway.Services.GetRequestExecutorAsync();

            var request = OperationRequestBuilder.New()
                .SetDocument("query SayHelloOperation { causeFatalError }")
                .Build();

            // act
            await executor.ExecuteAsync(request);

            // assert
            activities.MatchSnapshot();
        }
    }

    [Fact]
    public async Task Cause_A_Resolver_Error_That_Deletes_The_Whole_Result_Deep()
    {
        using (CaptureActivities(out var activities))
        {
            // arrange
            using var server1 = CreateSourceSchema(
                "a",
                b => b.AddQueryType<Query>());

            using var gateway = await CreateCompositeSchemaAsync(
            [
                ("a", server1)
            ],
            configureGatewayBuilder: b => b.AddInstrumentation(o =>
            {
                o.Scopes = FusionActivityScopes.All;
                o.IncludeDocument = true;
            }));

            var executor = await gateway.Services.GetRequestExecutorAsync();

            var request = OperationRequestBuilder.New()
                .SetDocument(
                    """
                    query SayHelloOperation {
                        deep {
                            deeper {
                                deeps {
                                    deeper {
                                        causeFatalError
                                    }
                                }
                            }
                        }
                    }
                    """)
                .Build();

            // act
            await executor.ExecuteAsync(request);

            // assert
            activities.MatchSnapshot();
        }
    }

    [Fact]
    public async Task Track_Events_Of_A_Simple_Query_With_Node_Scopes()
    {
        using (CaptureActivities(out var activities))
        {
            // arrange
            using var server1 = CreateSourceSchema(
                "a",
                b => b.AddQueryType<Query>());

            using var gateway = await CreateCompositeSchemaAsync(
            [
                ("a", server1)
            ],
            configureGatewayBuilder: b => b.AddInstrumentation(o =>
                o.Scopes = FusionActivityScopes.All));

            var executor = await gateway.Services.GetRequestExecutorAsync();

            var request = OperationRequestBuilder.New()
                .SetDocument("{ sayHello }")
                .Build();

            // act
            await executor.ExecuteAsync(request);

            // assert
            activities.MatchSnapshot();
        }
    }

    [Fact]
    public async Task Source_Schema_Transport_Error()
    {
        using (CaptureActivities(out var activities))
        {
            // arrange
            using var server1 = CreateSourceSchema(
                "a",
                b => b.AddQueryType<Query>(),
                isOffline: true);

            using var gateway = await CreateCompositeSchemaAsync(
            [
                ("a", server1)
            ],
            configureGatewayBuilder: b => b.AddInstrumentation(o =>
            {
                o.Scopes = FusionActivityScopes.All;
                o.IncludeDocument = true;
            }));

            var executor = await gateway.Services.GetRequestExecutorAsync();

            var request = OperationRequestBuilder.New()
                .SetDocument("{ sayHello }")
                .Build();

            // act
            await executor.ExecuteAsync(request);

            // assert
            activities.MatchSnapshot();
        }
    }

    [Fact]
    public async Task Track_Events_Of_A_Query_With_Multiple_Sources()
    {
        using (CaptureActivities(out var activities))
        {
            // arrange
            using var server1 = CreateSourceSchema(
                "a",
                b => b.AddQueryType<QueryA>());

            using var server2 = CreateSourceSchema(
                "b",
                b => b.AddQueryType<QueryB>());

            using var gateway = await CreateCompositeSchemaAsync(
            [
                ("a", server1),
                ("b", server2)
            ],
            configureGatewayBuilder: b => b.AddInstrumentation(o =>
                o.Scopes = FusionActivityScopes.All));

            var executor = await gateway.Services.GetRequestExecutorAsync();

            var request = OperationRequestBuilder.New()
                .SetDocument("{ sayHello sayGoodbye }")
                .Build();

            // act
            await executor.ExecuteAsync(request);

            // assert
            activities.MatchSnapshot();
        }
    }

    public class Query
    {
        public string SayHello() => "hello";

        public string CauseFatalError() => throw new GraphQLException("fail");

        public Deep Deep() => new();
    }

    [GraphQLName("Query")]
    public class QueryA
    {
        public string SayHello() => "hello";
    }

    [GraphQLName("Query")]
    public class QueryB
    {
        public string SayGoodbye() => "goodbye";
    }

    public class Deep
    {
        public Deeper Deeper() => new();

        public string CauseFatalError() => throw new GraphQLException("fail");
    }

    public class Deeper
    {
        public Deep[] Deeps() => [new Deep()];
    }
}
