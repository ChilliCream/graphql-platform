using HotChocolate.Execution;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Types;

public class MissingQueryTypeTests
{
    [Fact]
    public async Task Schema_Should_ExecuteMutationAndIntrospection_When_QueryTypeIsMissing()
    {
        // arrange
        var executor = await new ServiceCollection()
            .AddGraphQL()
            .AddMutationType(
                descriptor => descriptor
                    .Name("Mutation")
                    .Field("doThing")
                    .Resolve("done"))
            .ModifyOptions(options => options.StrictValidation = false)
            .BuildRequestExecutorAsync(cancellationToken: TestContext.Current.CancellationToken);

        // act
        await using var introspectionResult = await executor.ExecuteAsync(
            "{ __typename __schema { queryType { name } } }",
            TestContext.Current.CancellationToken);
        await using var mutationResult = await executor.ExecuteAsync(
            "mutation { doThing }",
            TestContext.Current.CancellationToken);

        // assert
        introspectionResult.MatchInlineSnapshot(
            """
            {
              "data": {
                "__typename": "Query",
                "__schema": {
                  "queryType": {
                    "name": "Query"
                  }
                }
              }
            }
            """);
        mutationResult.MatchInlineSnapshot(
            """
            {
              "data": {
                "doThing": "done"
              }
            }
            """);
        executor.Schema.QueryType.Fields.Select(field => field.Name).MatchInlineSnapshot(
            """
            [
              "__schema",
              "__type",
              "__typename",
              "__search",
              "__definitions"
            ]
            """);
    }

    [Fact]
    public async Task Schema_Should_ExecuteSubscriptionAndIntrospection_When_QueryTypeIsMissing()
    {
        // arrange
        var executor = await new ServiceCollection()
            .AddGraphQL()
            .AddSubscriptionType(
                descriptor => descriptor
                    .Name("Subscription")
                    .Field("events")
                    .Type<StringType>()
                    .Resolve(context => context.GetEventMessage<string>())
                    .Subscribe(_ => new[] { "event" }))
            .ModifyOptions(options => options.StrictValidation = false)
            .BuildRequestExecutorAsync(cancellationToken: TestContext.Current.CancellationToken);

        // act
        await using var introspectionResult = await executor.ExecuteAsync(
            "{ __typename __schema { queryType { name } } }",
            TestContext.Current.CancellationToken);
        await using var subscriptionResult = await executor.ExecuteAsync(
            "subscription { events }",
            TestContext.Current.CancellationToken);
        var events = new List<OperationResult>();

        await foreach (var result in subscriptionResult.ExpectResponseStream()
            .ReadResultsAsync()
            .WithCancellation(TestContext.Current.CancellationToken))
        {
            events.Add(result);
        }

        // assert
        introspectionResult.MatchInlineSnapshot(
            """
            {
              "data": {
                "__typename": "Query",
                "__schema": {
                  "queryType": {
                    "name": "Query"
                  }
                }
              }
            }
            """);
        events.MatchInlineSnapshots(
            [
                """
                {
                  "data": {
                    "events": "event"
                  }
                }
                """
            ]);

        foreach (var result in events)
        {
            await result.DisposeAsync();
        }
    }

    [Fact]
    public async Task Schema_Should_UseConfiguredQueryName_When_QueryTypeIsMissing()
    {
        // arrange
        var executor = await new ServiceCollection()
            .AddGraphQL()
            .AddMutationType(
                descriptor => descriptor
                    .Name("Mutation")
                    .Field("doThing")
                    .Resolve("done"))
            .ModifyOptions(options =>
            {
                options.QueryTypeName = "RootQuery";
                options.StrictValidation = false;
            })
            .BuildRequestExecutorAsync(cancellationToken: TestContext.Current.CancellationToken);

        // act
        await using var result = await executor.ExecuteAsync(
            "{ __typename __schema { queryType { name } } }",
            TestContext.Current.CancellationToken);

        // assert
        result.MatchInlineSnapshot(
            """
            {
              "data": {
                "__typename": "RootQuery",
                "__schema": {
                  "queryType": {
                    "name": "RootQuery"
                  }
                }
              }
            }
            """);
        executor.Schema.ToString().MatchInlineSnapshot(
            """
            schema {
              mutation: Mutation
            }

            type Mutation {
              doThing: String
            }
            """);
    }

    [Fact]
    public void Schema_Should_OmitSynthesizedQuery_When_Printed()
    {
        // arrange
        var schema = SchemaBuilder.New()
            .AddMutationType(
                descriptor => descriptor
                    .Name("Mutation")
                    .Field("doThing")
                    .Resolve("done"))
            .ModifyOptions(options => options.StrictValidation = false)
            .Create();

        // act
        var schemaText = schema.ToString();

        // assert
        schemaText.MatchInlineSnapshot(
            """
            schema {
              mutation: Mutation
            }

            type Mutation {
              doThing: String
            }
            """);
    }

    [Fact]
    public void Schema_Should_OmitDeclaredIntrospectionOnlyQuery_When_Printed()
    {
        // arrange
        var schema = SchemaBuilder.New()
            .AddQueryType(descriptor => descriptor.Name("Query"))
            .AddMutationType(
                descriptor => descriptor
                    .Name("Mutation")
                    .Field("doThing")
                    .Resolve("done"))
            .ModifyOptions(options => options.StrictValidation = false)
            .Create();

        // act
        var schemaText = schema.ToString();

        // assert
        schemaText.MatchInlineSnapshot(
            """
            schema {
              mutation: Mutation
            }

            type Mutation {
              doThing: String
            }
            """);
    }

    [Fact]
    public void Schema_Should_PrintDeclaredQuery_When_QueryHasRealField()
    {
        // arrange
        var schema = SchemaBuilder.New()
            .AddQueryType(
                descriptor => descriptor
                    .Name("Query")
                    .Field("hello")
                    .Resolve("world"))
            .ModifyOptions(options => options.StrictValidation = false)
            .Create();

        // act
        var schemaText = schema.ToString();

        // assert
        schemaText.MatchInlineSnapshot(
            """
            schema {
              query: Query
            }

            type Query {
              hello: String
            }
            """);
    }

    [Fact]
    public void Schema_Should_PreserveDeclaredQuery_When_QueryHasRealField()
    {
        // arrange
        var queryType = new ObjectType(
            descriptor => descriptor
                .Name("Query")
                .Field("hello")
                .Resolve("world"));

        // act
        var schema = SchemaBuilder.New()
            .AddQueryType(queryType)
            .ModifyOptions(options => options.StrictValidation = false)
            .Create();

        // assert
        Assert.Same(queryType, schema.QueryType);
        schema.QueryType.Fields.Select(field => field.Name).MatchInlineSnapshot(
            """
            [
              "__schema",
              "__type",
              "__typename",
              "hello",
              "__search",
              "__definitions"
            ]
            """);
    }

    [Fact]
    public void Schema_Should_RejectConfiguredQueryName_When_NameBelongsToScalar()
    {
        // arrange
        var builder = SchemaBuilder.New()
            .AddType(new AnyType("RootQuery", "Arbitrary value.", BindingBehavior.Explicit))
            .AddMutationType(
                descriptor => descriptor
                    .Name("Mutation")
                    .Field("doThing")
                    .Type<BooleanType>()
                    .Resolve(true))
            .ModifyOptions(options =>
            {
                options.QueryTypeName = "RootQuery";
                options.StrictValidation = false;
            });

        // act
        void CreateSchema() => builder.Create();

        // assert
        Assert.Throws<SchemaException>(CreateSchema).Errors.MatchInlineSnapshot(
            """
            Cannot register `RootQuery` as Query type as it is not an object type. `RootQuery` is of type `HotChocolate.Types.AnyType`.
            """);
    }
}
