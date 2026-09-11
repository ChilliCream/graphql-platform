#pragma warning disable RCS1102 // Make class static
using System.Text.Json;
using HotChocolate.Execution;
using HotChocolate.Execution.Processing;
using HotChocolate.Language;
using HotChocolate.Tests;
using HotChocolate.Types.Composite;
using HotChocolate.Types.Relay;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Types;

public class NodeResolverTests
{
    [Fact]
    public async Task NodeResolver_ResolveNode()
    {
        // arrange
        var executor =
            await new ServiceCollection()
                .AddGraphQLServer()
                .AddGlobalObjectIdentification()
                .AddType<EntityType>()
                .AddQueryType<Query>()
                .BuildRequestExecutorAsync(cancellationToken: TestContext.Current.CancellationToken);

        // act
        var result = await executor.ExecuteAsync(
            "{ node(id: \"RW50aXR5OmZvbw==\")  "
            + "{ ... on Entity { id name } } }",
            TestContext.Current.CancellationToken);

        // assert
        result.ToJson().MatchSnapshot();
    }

    [Fact]
    public async Task Node_Should_Error_Only_Offending_Field_When_Sibling_Id_Is_Malformed()
    {
        // arrange
        var executor =
            await new ServiceCollection()
                .AddGraphQLServer()
                .AddGlobalObjectIdentification()
                .AddType<EntityType>()
                .AddQueryType<Query>()
                .BuildRequestExecutorAsync(cancellationToken: TestContext.Current.CancellationToken);

        // act
        var result = await executor.ExecuteAsync(
            """
            {
                a: node(id: "garbage") { ... on Entity { name } }
                b: node(id: "RW50aXR5OmZvbw==") { ... on Entity { name } }
            }
            """,
            cancellationToken: TestContext.Current.CancellationToken);

        // assert
        result.ToJson().MatchInlineSnapshot(
            """
            {
              "errors": [
                {
                  "message": "The node ID string has an invalid format.",
                  "path": [
                    "a"
                  ],
                  "extensions": {
                    "originalValue": "garbage"
                  }
                }
              ],
              "data": {
                "a": null,
                "b": {
                  "name": "foo"
                }
              }
            }
            """);
    }

    [Fact]
    public async Task Nodes_Should_Error_Only_Offending_Field_When_Sibling_Id_Is_Malformed()
    {
        // arrange
        var executor =
            await new ServiceCollection()
                .AddGraphQLServer()
                .AddGlobalObjectIdentification()
                .AddType<EntityType>()
                .AddQueryType<Query>()
                .BuildRequestExecutorAsync(cancellationToken: TestContext.Current.CancellationToken);

        // act
        var result = await executor.ExecuteAsync(
            """
            {
                a: nodes(ids: ["garbage"]) { ... on Entity { name } }
                b: nodes(ids: ["RW50aXR5OmZvbw=="]) { ... on Entity { name } }
            }
            """,
            cancellationToken: TestContext.Current.CancellationToken);

        // assert
        // The malformed id in `a` produces exactly one error scoped to `a`. The valid sibling
        // `b` is never bogusly errored. Since `nodes` is `[Node]!`, nulling `a` propagates to
        // the non-null root and so `data` is null, matching the per-field behavior.
        result.ToJson().MatchInlineSnapshot(
            """
            {
              "errors": [
                {
                  "message": "The node ID string has an invalid format.",
                  "path": [
                    "a"
                  ],
                  "extensions": {
                    "originalValue": "garbage"
                  }
                }
              ],
              "data": null
            }
            """);
    }

    [Fact]
    public async Task Node_Should_Error_Only_Offending_Field_When_Sibling_Id_Literal_Is_Not_A_String()
    {
        // arrange
        var executor =
            await new ServiceCollection()
                .AddGraphQLServer()
                .AddGlobalObjectIdentification()
                .AddType<EntityType>()
                .AddQueryType<Query>()
                .BuildRequestExecutorAsync(cancellationToken: TestContext.Current.CancellationToken);

        // act
        // `id: 123` passes validation because IdType accepts int literals, but the resolver
        // requires a string literal. The incompatible literal must error only `a`.
        var result = await executor.ExecuteAsync(
            """
            {
                a: node(id: 123) { ... on Entity { name } }
                b: node(id: "RW50aXR5OmZvbw==") { ... on Entity { name } }
            }
            """,
            cancellationToken: TestContext.Current.CancellationToken);

        // assert
        result.ToJson().MatchInlineSnapshot(
            """
            {
              "errors": [
                {
                  "message": "The argument literal representation is `HotChocolate.Language.IntValueNode` which is not compatible with the request literal type `HotChocolate.Language.StringValueNode`.",
                  "path": [
                    "a"
                  ],
                  "extensions": {
                    "fieldName": "node",
                    "argumentName": "id",
                    "requestedType": "HotChocolate.Language.StringValueNode",
                    "actualType": "HotChocolate.Language.IntValueNode"
                  }
                }
              ],
              "data": {
                "a": null,
                "b": {
                  "name": "foo"
                }
              }
            }
            """);
    }

    [Fact]
    public async Task Nodes_Should_Error_Only_Offending_Field_When_Sibling_Id_Literal_Is_Not_A_String()
    {
        // arrange
        var executor =
            await new ServiceCollection()
                .AddGraphQLServer()
                .AddGlobalObjectIdentification()
                .AddType<EntityType>()
                .AddQueryType<Query>()
                .BuildRequestExecutorAsync(cancellationToken: TestContext.Current.CancellationToken);

        // act
        // `ids: 123` passes validation because IdType accepts int literals, but the resolver
        // requires a string literal. The incompatible literal must error only `a`.
        var result = await executor.ExecuteAsync(
            """
            {
                a: nodes(ids: 123) { ... on Entity { name } }
                b: nodes(ids: ["RW50aXR5OmZvbw=="]) { ... on Entity { name } }
            }
            """,
            cancellationToken: TestContext.Current.CancellationToken);

        // assert
        // The incompatible literal in `a` produces exactly one error scoped to `a`. The valid
        // sibling `b` is never bogusly errored. Since `nodes` is `[Node]!`, nulling `a`
        // propagates to the non-null root and so `data` is null, matching the per-field behavior.
        result.ToJson().MatchInlineSnapshot(
            """
            {
              "errors": [
                {
                  "message": "The argument literal representation is `HotChocolate.Language.IntValueNode` which is not compatible with the request literal type `HotChocolate.Language.StringValueNode`.",
                  "path": [
                    "a"
                  ],
                  "extensions": {
                    "fieldName": "nodes",
                    "argumentName": "ids",
                    "requestedType": "HotChocolate.Language.StringValueNode",
                    "actualType": "HotChocolate.Language.IntValueNode"
                  }
                }
              ],
              "data": null
            }
            """);
    }

    [Fact]
    public async Task Nodes_Should_Error_Whole_Field_When_Second_Id_Is_Malformed()
    {
        // arrange
        var executor =
            await new ServiceCollection()
                .AddGraphQLServer()
                .AddGlobalObjectIdentification()
                .AddType<EntityType>()
                .AddQueryType<Query>()
                .BuildRequestExecutorAsync(cancellationToken: TestContext.Current.CancellationToken);

        // act
        // The first id is valid and stages a child, the second id is malformed. The whole field
        // errors and the staged child for the valid id is abandoned without cleanup failures.
        var result = await executor.ExecuteAsync(
            """
            {
                nodes(ids: ["RW50aXR5OmZvbw==", "garbage"]) { ... on Entity { name } }
            }
            """,
            cancellationToken: TestContext.Current.CancellationToken);

        // assert
        result.ToJson().MatchInlineSnapshot(
            """
            {
              "errors": [
                {
                  "message": "The node ID string has an invalid format.",
                  "path": [
                    "nodes"
                  ],
                  "extensions": {
                    "originalValue": "garbage"
                  }
                }
              ],
              "data": null
            }
            """);
    }

    [Fact]
    public async Task NodeResolver_ResolveNode_DynamicField()
    {
        // arrange
        var executor =
            await new ServiceCollection()
                .AddGraphQLServer()
                .AddGlobalObjectIdentification()
                .AddObjectType<Entity>(d =>
                {
                    d.ImplementsNode()
                        .ResolveNode<string>(
                            (_, id) => Task.FromResult<Entity?>(new Entity { Name = id }))
                        .Resolve(ctx => ctx.Parent<Entity>().Id);
                })
                .AddQueryType<Query>()
                .BuildRequestExecutorAsync(cancellationToken: TestContext.Current.CancellationToken);

        // act
        var result = await executor.ExecuteAsync(
            "{ node(id: \"RW50aXR5OmZvbw==\")  "
            + "{ ... on Entity { id name } } }",
            TestContext.Current.CancellationToken);

        // assert
        result.ToJson().MatchSnapshot();
    }

    [Fact]
    public async Task NodeResolver_ResolveNode_DynamicFieldObject()
    {
        // arrange
        var executor =
            await new ServiceCollection()
                .AddGraphQLServer()
                .AddGlobalObjectIdentification()
                .AddObjectType<Entity>(d =>
                {
                    d.ImplementsNode()
                        .ResolveNode<string>((_, id) =>
                            Task.FromResult<Entity?>(new Entity { Name = id }))
                        .Resolve(ctx => ctx.Parent<Entity>().Id);
                })
                .AddQueryType<Query>()
                .BuildRequestExecutorAsync(cancellationToken: TestContext.Current.CancellationToken);

        // act
        var result = await executor.ExecuteAsync(
            "{ node(id: \"RW50aXR5OmZvbw==\")  "
            + "{ ... on Entity { id name } } }",
            TestContext.Current.CancellationToken);

        // assert
        result.ToJson().MatchSnapshot();
    }

    [Fact]
    public async Task NodeResolverObject_ResolveNode_DynamicField()
    {
        // arrange
        var executor =
            await new ServiceCollection()
                .AddGraphQLServer()
                .AddGlobalObjectIdentification()
                .AddObjectType(d =>
                {
                    d.Name("Entity");
                    d.ImplementsNode()
                        .ResolveNode<string>(
                            (_, id) => Task.FromResult<object?>(new Entity { Name = id }))
                        .Resolve(ctx => ctx.Parent<Entity>().Id);
                    d.Field("name")
                        .Type<StringType>()
                        .Resolve(t => t.Parent<Entity>().Name);
                })
                .AddQueryType(d =>
                {
                    d.Name("Query")
                        .Field("entity")
                        .Type(new NamedTypeNode("Entity"))
                        .Resolve(new Entity { Name = "foo" });
                })
                .BuildRequestExecutorAsync(cancellationToken: TestContext.Current.CancellationToken);

        // act
        var result = await executor.ExecuteAsync(
            "{ node(id: \"RW50aXR5OmZvbw==\")  "
            + "{ ... on Entity { id name } } }",
            TestContext.Current.CancellationToken);

        // assert
        result.ToJson().MatchSnapshot();
    }

    [Fact]
    public async Task NodeResolverObject_ResolveNode_DynamicFieldObject()
    {
        // arrange
        var executor =
            await new ServiceCollection()
                .AddGraphQLServer()
                .AddGlobalObjectIdentification()
                .AddObjectType(d =>
                {
                    d.Name("Entity");
                    d.ImplementsNode()
                        .ResolveNode<string>(
                            (_, id) => Task.FromResult<object?>(new Entity { Name = id }))
                        .Resolve(ctx => ctx.Parent<Entity>().Id);
                    d.Field("name")
                        .Type<StringType>()
                        .Resolve(t => t.Parent<Entity>().Name);
                })
                .AddQueryType(d =>
                {
                    d.Name("Query")
                        .Field("entity")
                        .Type(new NamedTypeNode("Entity"))
                        .Resolve(new Entity { Name = "foo" });
                })
                .BuildRequestExecutorAsync(cancellationToken: TestContext.Current.CancellationToken);

        // act
        var result = await executor.ExecuteAsync(
            "{ node(id: \"RW50aXR5OmZvbw==\")  "
            + "{ ... on Entity { id name } } }",
            TestContext.Current.CancellationToken);

        // assert
        result.ToJson().MatchSnapshot();
    }

    [Fact]
    public async Task NodeResolver_ResolveNode_WithInterface()
    {
        // arrange
        var schema = SchemaBuilder.New()
            .AddGlobalObjectIdentification()
            .AddQueryType<Query>()
            .AddType<Entity3>()
            .Create();

        var executor = schema.MakeExecutable();

        // act
        var result = await executor.ExecuteAsync(
            """
            {
                node(id: "RW50aXR5Mzox") {
                    ... on Entity3 {
                        id
                    }
                }
            }
            """,
            TestContext.Current.CancellationToken);

        // assert
        result.ToJson().MatchSnapshot();
    }

    [Fact]
    public async Task NodeAttribute_On_Extension()
    {
        await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType<Query>()
            .AddTypeExtension<EntityExtension>()
            .BuildSchemaAsync(cancellationToken: TestContext.Current.CancellationToken)
            .MatchSnapshotAsync();
    }

    [Fact]
    public async Task NodeAttribute_On_Extension2()
    {
        await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType<Query>()
            .AddTypeExtension<EntityExtension2>()
            .BuildSchemaAsync(cancellationToken: TestContext.Current.CancellationToken)
            .MatchSnapshotAsync();
    }

    [Fact]
    public async Task NodeAttribute_On_Extension3()
    {
        await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType<Query>()
            .AddTypeExtension<EntityExtension3>()
            .BuildSchemaAsync(cancellationToken: TestContext.Current.CancellationToken)
            .MatchSnapshotAsync();
    }

    [Fact]
    public async Task NodeAttribute_On_Extension4()
    {
        await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType<Query>()
            .AddTypeExtension<EntityExtension4>()
            .BuildSchemaAsync(cancellationToken: TestContext.Current.CancellationToken)
            .MatchSnapshotAsync();
    }

    [Fact]
    public async Task NodeAttribute_On_Extension_Fetch_Through_Node_Field()
    {
        await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType<Query>()
            .AddTypeExtension<EntityExtension>()
            .AddGlobalObjectIdentification()
            .ExecuteRequestAsync(
                """
                {
                    node(id: "RW50aXR5OmFiYw==") {
                        ... on Entity {
                            name
                        }
                    }
                }
                """,
                cancellationToken: TestContext.Current.CancellationToken)
            .MatchSnapshotAsync();
    }

    [Fact]
    public async Task NodeResolver_On_Query_Field_With_BatchResolver_Schema()
    {
        await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType<QueryWithBatchNodeResolver>()
            .AddType<BatchEntity>()
            .AddGlobalObjectIdentification()
            .BuildSchemaAsync(cancellationToken: TestContext.Current.CancellationToken)
            .MatchSnapshotAsync();
    }

    [Fact]
    public async Task NodeResolver_On_Query_Field_With_BatchResolver_Fetch_Through_Node_Field()
    {
        await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType<QueryWithBatchNodeResolver>()
            .AddType<BatchEntity>()
            .AddGlobalObjectIdentification()
            .ExecuteRequestAsync(
                """
                {
                    node(id: "QmF0Y2hFbnRpdHk6YWJj") {
                        ... on BatchEntity {
                            id
                            name
                        }
                    }
                }
                """,
                cancellationToken: TestContext.Current.CancellationToken)
            .MatchSnapshotAsync();
    }

    [Fact]
    public async Task NodeAttribute_On_Extension_Fetch_Through_Node_Field_With_NonId_Argument_Name()
    {
        var result = await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType<Query>()
            .AddTypeExtension<EntityExtensionWithNonIdArgument>()
            .AddGlobalObjectIdentification()
            .ExecuteRequestAsync(
                """
                {
                    node(id: "RW50aXR5OmFiYw==") {
                        ... on Entity {
                            name
                        }
                    }
                }
                """,
                cancellationToken: TestContext.Current.CancellationToken);

        var operationResult = result.ExpectOperationResult();

        Assert.True(
            operationResult.Errors.Count == 0,
            $"Expected no errors but got: {operationResult.ToJson()}");

        using var document = JsonDocument.Parse(operationResult.ToJson());
        var node = document.RootElement.GetProperty("data").GetProperty("node");

        Assert.Equal(JsonValueKind.Object, node.ValueKind);
        Assert.Equal(JsonValueKind.String, node.GetProperty("name").ValueKind);
    }

    // Ensure Issue 7829 is fixed.
    [Fact]
    public async Task NodeAttribute_On_Extension_With_Renamed_Id()
    {
        await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType<QueryEntityRenamed>()
            .AddTypeExtension<EntityExtensionRenamingId>()
            .ExecuteRequestAsync(
                """
                {
                  entity(id: 5) {
                    id
                    data
                  }
                }
                """,
                cancellationToken: TestContext.Current.CancellationToken)
            .MatchSnapshotAsync();
    }

    [Fact]
    public async Task Node_Should_Separate_Batch_Invocations_When_Same_Type_Fields_Are_Aliased()
    {
        // arrange
        // Each alias invokes the batch node resolver separately, including duplicate IDs.
        var collector = new BatchNodeCollector();
        var executor =
            await new ServiceCollection()
                .AddSingleton(collector)
                .AddGraphQLServer()
                .AddQueryType<QueryWithCollectingBatchNodeResolver>()
                .AddType<BatchEntity>()
                .AddType<EntityType>()
                .AddGlobalObjectIdentification()
                .BuildRequestExecutorAsync(cancellationToken: TestContext.Current.CancellationToken);

        // act
        var result = await executor.ExecuteAsync(
            """
            {
                a: node(id: "QmF0Y2hFbnRpdHk6eA==") { ... on BatchEntity { id name } }
                b: node(id: "QmF0Y2hFbnRpdHk6eQ==") { ... on BatchEntity { id name } }
                dup: node(id: "QmF0Y2hFbnRpdHk6eA==") { ... on BatchEntity { id name } }
            }
            """,
            cancellationToken: TestContext.Current.CancellationToken);

        // assert
        Assert.Equal(3, collector.InvocationCount);
        Assert.Equal(["x", "x", "y"], [.. collector.ReceivedIds.OrderBy(x => x)]);
        result.ToJson().MatchInlineSnapshot(
            """
            {
              "data": {
                "a": {
                  "id": "QmF0Y2hFbnRpdHk6eA==",
                  "name": "x"
                },
                "b": {
                  "id": "QmF0Y2hFbnRpdHk6eQ==",
                  "name": "y"
                },
                "dup": {
                  "id": "QmF0Y2hFbnRpdHk6eA==",
                  "name": "x"
                }
              }
            }
            """);
    }

    [Fact]
    public async Task Node_Should_Separate_Aliases_When_Fields_Mix_Batch_And_Classic_Resolvers()
    {
        // arrange
        // Two aliases invoke the batch node resolver separately, one invokes the classic resolver.
        var collector = new BatchNodeCollector();
        var executor =
            await new ServiceCollection()
                .AddSingleton(collector)
                .AddGraphQLServer()
                .AddQueryType<QueryWithCollectingBatchNodeResolver>()
                .AddType<BatchEntity>()
                .AddType<EntityType>()
                .AddGlobalObjectIdentification()
                .BuildRequestExecutorAsync(cancellationToken: TestContext.Current.CancellationToken);

        // act
        var result = await executor.ExecuteAsync(
            """
            {
                a: node(id: "QmF0Y2hFbnRpdHk6eA==") { ... on BatchEntity { id name } }
                b: node(id: "QmF0Y2hFbnRpdHk6eQ==") { ... on BatchEntity { id name } }
                c: node(id: "RW50aXR5OmZvbw==") { ... on Entity { id name } }
            }
            """,
            cancellationToken: TestContext.Current.CancellationToken);

        // assert
        Assert.Equal(2, collector.InvocationCount);
        Assert.Equal(["x", "y"], [.. collector.ReceivedIds.OrderBy(x => x)]);
        result.ToJson().MatchInlineSnapshot(
            """
            {
              "data": {
                "a": {
                  "id": "QmF0Y2hFbnRpdHk6eA==",
                  "name": "x"
                },
                "b": {
                  "id": "QmF0Y2hFbnRpdHk6eQ==",
                  "name": "y"
                },
                "c": {
                  "id": "RW50aXR5OmZvbw==",
                  "name": "foo"
                }
              }
            }
            """);
    }

    [Fact]
    public async Task Nodes_Should_Dispatch_Through_Batch_Node_Resolver_When_Ids_Contain_Duplicates()
    {
        // arrange
        // A single nodes field with two distinct ids plus a duplicate must reach the batch node
        // resolver once with all three internal ids and map the results back positionally.
        var collector = new BatchNodeCollector();
        var executor =
            await new ServiceCollection()
                .AddSingleton(collector)
                .AddGraphQLServer()
                .AddQueryType<QueryWithCollectingBatchNodeResolver>()
                .AddType<BatchEntity>()
                .AddGlobalObjectIdentification()
                .BuildRequestExecutorAsync(cancellationToken: TestContext.Current.CancellationToken);

        // act
        var result = await executor.ExecuteAsync(
            """
            {
                nodes(ids: ["QmF0Y2hFbnRpdHk6eA==", "QmF0Y2hFbnRpdHk6eQ==", "QmF0Y2hFbnRpdHk6eA=="]) {
                    ... on BatchEntity { id name }
                }
            }
            """,
            cancellationToken: TestContext.Current.CancellationToken);

        // assert
        Assert.Equal(1, collector.InvocationCount);
        Assert.Equal(3, collector.ReceivedIds.Count);
        result.ToJson().MatchInlineSnapshot(
            """
            {
              "data": {
                "nodes": [
                  {
                    "id": "QmF0Y2hFbnRpdHk6eA==",
                    "name": "x"
                  },
                  {
                    "id": "QmF0Y2hFbnRpdHk6eQ==",
                    "name": "y"
                  },
                  {
                    "id": "QmF0Y2hFbnRpdHk6eA==",
                    "name": "x"
                  }
                ]
              }
            }
            """);
    }

    [Fact]
    public async Task Nodes_Should_Separate_Batch_Invocations_When_Fields_Are_Aliased()
    {
        // arrange
        // Each nodes alias invokes the batch node resolver separately.
        var collector = new BatchNodeCollector();
        var executor =
            await new ServiceCollection()
                .AddSingleton(collector)
                .AddGraphQLServer()
                .AddQueryType<QueryWithCollectingBatchNodeResolver>()
                .AddType<BatchEntity>()
                .AddGlobalObjectIdentification()
                .BuildRequestExecutorAsync(cancellationToken: TestContext.Current.CancellationToken);

        // act
        var result = await executor.ExecuteAsync(
            """
            {
                a: nodes(ids: ["QmF0Y2hFbnRpdHk6eA=="]) { ... on BatchEntity { name } }
                b: nodes(ids: ["QmF0Y2hFbnRpdHk6eQ=="]) { ... on BatchEntity { name } }
            }
            """,
            cancellationToken: TestContext.Current.CancellationToken);

        // assert
        Assert.Equal(2, collector.InvocationCount);
        Assert.Equal(["x", "y"], [.. collector.ReceivedIds.OrderBy(x => x)]);
        result.ToJson().MatchInlineSnapshot(
            """
            {
              "data": {
                "a": [
                  {
                    "name": "x"
                  }
                ],
                "b": [
                  {
                    "name": "y"
                  }
                ]
              }
            }
            """);
    }

    [Fact]
    public async Task Nodes_Should_Error_Whole_Field_When_List_Contains_Int_Literal()
    {
        // arrange
        var executor =
            await new ServiceCollection()
                .AddGraphQLServer()
                .AddGlobalObjectIdentification()
                .AddType<EntityType>()
                .AddQueryType<Query>()
                .BuildRequestExecutorAsync(cancellationToken: TestContext.Current.CancellationToken);

        // act
        // `123` inside the ids list passes validation (IdType accepts int literals) but the
        // resolver requires string literals. The non-string element must surface the same
        // literal-not-compatible error scoped to the nodes field, not a masked cast exception.
        var result = await executor.ExecuteAsync(
            """
            {
                nodes(ids: ["RW50aXR5OmZvbw==", 123]) { ... on Entity { name } }
            }
            """,
            cancellationToken: TestContext.Current.CancellationToken);

        // assert
        result.ToJson().MatchInlineSnapshot(
            """
            {
              "errors": [
                {
                  "message": "The argument literal representation is `HotChocolate.Language.IntValueNode` which is not compatible with the request literal type `HotChocolate.Language.StringValueNode`.",
                  "path": [
                    "nodes"
                  ],
                  "extensions": {
                    "fieldName": "nodes",
                    "argumentName": "ids",
                    "requestedType": "HotChocolate.Language.StringValueNode",
                    "actualType": "HotChocolate.Language.IntValueNode"
                  }
                }
              ],
              "data": null
            }
            """);
    }

    [Fact]
    public async Task Nodes_Should_Recover_On_Second_Query_After_Malformed_Id_Failure()
    {
        // arrange
        // The first query fails on a malformed second id (abandoning the staged child for the
        // valid first id). A second valid query on the same executor must still resolve, proving
        // pooled-context cleanup did not corrupt state.
        var executor =
            await new ServiceCollection()
                .AddGraphQLServer()
                .AddGlobalObjectIdentification()
                .AddType<EntityType>()
                .AddQueryType<Query>()
                .BuildRequestExecutorAsync(cancellationToken: TestContext.Current.CancellationToken);

        // act
        var failed = await executor.ExecuteAsync(
            """
            {
                nodes(ids: ["RW50aXR5OmZvbw==", "garbage"]) { ... on Entity { name } }
            }
            """,
            cancellationToken: TestContext.Current.CancellationToken);

        var recovered = await executor.ExecuteAsync(
            """
            {
                nodes(ids: ["RW50aXR5OmZvbw=="]) { ... on Entity { name } }
            }
            """,
            cancellationToken: TestContext.Current.CancellationToken);

        // assert
        Assert.Single(failed.ExpectOperationResult().Errors!);
        recovered.ToJson().MatchInlineSnapshot(
            """
            {
              "data": {
                "nodes": [
                  {
                    "name": "foo"
                  }
                ]
              }
            }
            """);
    }

    [Fact]
    public async Task Node_Should_Round_Trip_Through_Batch_Resolver_With_Custom_Id_Serializer()
    {
        // arrange
        // A custom value serializer encodes the batch type's int key. The batch node path must
        // parse the global id once and re-encode the resolved entity's id with the same serializer.
        var executor =
            await new ServiceCollection()
                .AddGraphQLServer()
                .AddQueryType<QueryWithCustomKeyBatchNodeResolver>()
                .AddType<CustomKeyEntity>()
                .AddGlobalObjectIdentification()
                .AddNodeIdValueSerializer<CustomKeyNodeIdValueSerializer>()
                .BuildRequestExecutorAsync(cancellationToken: TestContext.Current.CancellationToken);

        // act
        // "Q3VzdG9tS2V5RW50aXR5OmtleS00Mg==" decodes to "CustomKeyEntity:key-42".
        var result = await executor.ExecuteAsync(
            """
            {
                a: node(id: "Q3VzdG9tS2V5RW50aXR5OmtleS00Mg==") { ... on CustomKeyEntity { id value } }
                b: node(id: "garbage") { ... on CustomKeyEntity { id value } }
            }
            """,
            cancellationToken: TestContext.Current.CancellationToken);

        // assert
        // `a` round trips through the custom serializer; `b` errors alone, proving the parse-error
        // cache path works with a non-default serializer.
        result.ToJson().MatchInlineSnapshot(
            """
            {
              "errors": [
                {
                  "message": "The node ID string has an invalid format.",
                  "path": [
                    "b"
                  ],
                  "extensions": {
                    "originalValue": "garbage"
                  }
                }
              ],
              "data": {
                "a": {
                  "id": "Q3VzdG9tS2V5RW50aXR5OmtleS00Mg==",
                  "value": 42
                },
                "b": null
              }
            }
            """);
    }

    [Fact]
    public async Task NodeResolver_And_AsSelector()
    {
        // arrange
        var executor =
            await new ServiceCollection()
                .AddGraphQLServer()
                .AddGlobalObjectIdentification()
                .AddTypeExtension<EntityExtension5>()
                .AddTypeExtension<Entity2Extension1>()
                .AddQueryType<Query>()
                .BuildRequestExecutorAsync(cancellationToken: TestContext.Current.CancellationToken);

        // act
        var result = await executor.ExecuteAsync(
            """
            {
                nodes(ids: ["RW50aXR5OmZvbw=="]) {
                    id
                }
            }
            """,
            TestContext.Current.CancellationToken);

        // assert
        Assert.Empty(result.ExpectOperationResult().Errors);
    }

    public class Query
    {
        public Entity GetEntity(string name) => new Entity { Name = name };

        public Entity2 GetEntity2(string name) => new Entity2 { Name = name };
    }

    public class QueryWithBatchNodeResolver
    {
        [Lookup]
        [NodeResolver]
        [BatchResolver]
        public List<BatchEntity> GetBatchEntity(List<string> id)
        {
            var result = new List<BatchEntity>();

            foreach (var value in id)
            {
                result.Add(new BatchEntity { Name = value });
            }

            return result;
        }
    }

    public class BatchEntity
    {
        public string Id
        {
            get => Name;
            set => Name = value;
        }

        public required string Name { get; set; }
    }

    public sealed class BatchNodeCollector
    {
        private readonly List<string> _receivedIds = [];

        public int InvocationCount { get; private set; }

        public IReadOnlyList<string> ReceivedIds => _receivedIds;

        public void Record(IEnumerable<string> ids)
        {
            InvocationCount++;
            _receivedIds.AddRange(ids);
        }
    }

    public class QueryWithCollectingBatchNodeResolver
    {
        [NodeResolver]
        [BatchResolver]
        public List<BatchEntity> GetBatchEntity(
            List<string> id,
            [Service] BatchNodeCollector collector)
        {
            collector.Record(id);

            var result = new List<BatchEntity>();

            foreach (var value in id)
            {
                result.Add(new BatchEntity { Name = value });
            }

            return result;
        }

        public Entity GetEntity(string name) => new() { Name = name };
    }

    public class QueryWithCustomKeyBatchNodeResolver
    {
        [NodeResolver]
        [BatchResolver]
        public List<CustomKeyEntity> GetCustomKeyEntity(List<CustomKey> id)
            => id.Select(key => new CustomKeyEntity { Id = key }).ToList();
    }

    public class CustomKeyEntity
    {
        public required CustomKey Id { get; set; }

        public int Value => Id.Number;
    }

    public readonly record struct CustomKey(int Number)
    {
        public override string ToString() => $"key-{Number}";

        public static CustomKey Parse(string value)
            => new(int.Parse(value["key-".Length..]));
    }

    public sealed class CustomKeyNodeIdValueSerializer : INodeIdValueSerializer
    {
        public bool IsSupported(Type type) => type == typeof(CustomKey);

        public NodeIdFormatterResult Format(Span<byte> buffer, object value, out int written)
        {
            if (value is CustomKey key)
            {
                written = System.Text.Encoding.UTF8.GetBytes(key.ToString(), buffer);
                return NodeIdFormatterResult.Success;
            }

            written = 0;
            return NodeIdFormatterResult.InvalidValue;
        }

        public bool TryParse(
            ReadOnlySpan<byte> buffer,
            [System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out object? value)
        {
            value = CustomKey.Parse(System.Text.Encoding.UTF8.GetString(buffer));
            return true;
        }
    }

    public class EntityType : ObjectType<Entity>
    {
        protected override void Configure(
            IObjectTypeDescriptor<Entity> descriptor)
        {
            descriptor
                .ImplementsNode()
                .IdField(t => t.Id)
                .ResolveNode((_, id) => Task.FromResult<Entity?>(new Entity { Name = id }));
        }
    }

    public class Entity
    {
        public string Id
        {
            get => Name;
            set => Name = value;
        }

        public required string Name { get; set; }
    }

    public class Entity2
    {
        public string Id => Name;
        public required string Name { get; set; }

        public static Entity2 Get(string id) => new() { Name = id };
    }

    [Node]
    public class Entity3 : EntityBase, IResolvable<Entity3>
    {
        public string? Message { get; set; }
    }

    public class EntityBase
    {
        public int Id { get; set; }
    }

    public interface IResolvable<T> where T : EntityBase, new()
    {
        static Task<T> GetAsync(int id) => Task.FromResult(new T { Id = id });
    }

    [Node]
    [ExtendObjectType(typeof(Entity))]

    public class EntityExtension
    {
        public static Entity GetEntity(string id) => new() { Name = id };
    }

    [Node]
    [ExtendObjectType(typeof(Entity))]
    public class EntityExtension2
    {
        [NodeResolver]
        public static Entity Foo(string id) => new() { Name = id };
    }

    [Node]
    [ExtendObjectType(typeof(Entity))]
    public class EntityExtensionWithNonIdArgument
    {
        [NodeResolver]
        public static Entity Foo(string userId) => new() { Name = userId };
    }

    [Node]
    [ExtendObjectType(typeof(Entity))]
    public class EntityExtension3
    {
        [NodeResolver]
        public static Entity Foo(string id) => new() { Name = id };
    }

    [Node]
    [ExtendObjectType(typeof(Entity))]
    public class EntityExtension4
    {
        public static Entity GetEntity(string id) => new() { Name = id };
    }

    [Node]
    [ExtendObjectType(typeof(Entity))]
    public class EntityExtension5
    {
        [NodeResolver]
        public static Entity GetEntity(string id, ISelection selection)
        {
            selection.AsSelector<Entity>();

            return new Entity { Name = id };
        }
    }

    [Node]
    [ExtendObjectType(typeof(Entity2))]
    public class Entity2Extension1
    {
        [NodeResolver]
        public static Entity2 GetEntity2(string id, ISelection selection)
        {
            selection.AsSelector<Entity2>();

            return new Entity2 { Name = id };
        }
    }

    public class QueryEntityRenamed
    {
        public EntityNoId GetEntity(int id)
            => new EntityNoId { Data = id };
    }

    public class EntityNoId
    {
        public int Data { get; set; }
    }

    [Node]
    [ExtendObjectType(typeof(EntityNoId))]
    public class EntityExtensionRenamingId
    {
        public int GetId([Parent] EntityNoId entity)
            => entity.Data;

        [NodeResolver]
        public EntityNoId GetEntity(int id)
            => new() { Data = id };
    }
}
#pragma warning restore RCS1102 // Make class static
