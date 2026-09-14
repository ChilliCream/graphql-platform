#pragma warning disable RCS1102 // Make class static
using System.Collections.Immutable;
using System.Reflection;
using System.Text.Json;
using HotChocolate.Authorization;
using HotChocolate.Execution;
using HotChocolate.Execution.Processing;
using HotChocolate.Language;
using HotChocolate.Resolvers;
using HotChocolate.Tests;
using HotChocolate.Types.Composite;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Configurations;
using HotChocolate.Types.Relay;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Types;

public class NodeResolverTests
{
    [Fact]
    public async Task ResolveNodeBatch_Should_Preserve_Null_Positions_When_Typed_Delegate_Returns_Missing_Nodes()
    {
        // arrange
        var collector = new BatchNodeCollector();
        var executor = await new ServiceCollection()
            .AddGraphQL()
            .AddGlobalObjectIdentification()
            .AddQueryType(d => d.Field("ready").Resolve(true))
            .AddObjectType<BatchEntity>(d => d.ImplementsNode().IdField(n => n.Id).ResolveNodeBatch((_, ids) =>
            {
                collector.Record(ids);
                return Task.FromResult<IReadOnlyList<BatchEntity?>>(
                    ids.Select(id => id == "y" ? null : new BatchEntity { Name = id }).ToArray());
            }))
            .BuildRequestExecutorAsync(cancellationToken: TestContext.Current.CancellationToken);

        // act
        var result = await executor.ExecuteAsync(
            """
            {
                nodes(ids: ["QmF0Y2hFbnRpdHk6eA==", "QmF0Y2hFbnRpdHk6eQ==", "QmF0Y2hFbnRpdHk6eA=="]) {
                    ... on BatchEntity { name }
                }
            }
            """,
            cancellationToken: TestContext.Current.CancellationToken);

        // assert
        new Snapshot()
            .Add(result, "Result")
            .Add(new { collector.InvocationCount, collector.ReceivedIds }, "Dispatch")
            .MatchMarkdownSnapshot();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public async Task ResolveNodeBatch_Should_Reject_Wrong_Result_Count_When_Registered(int registration)
    {
        // arrange
        var executor = await new ServiceCollection()
            .AddGraphQL()
            .AddGlobalObjectIdentification()
            .AddQueryType(d => d.Field("ready").Resolve(true))
            .AddErrorFilter(error => error.Exception is { } exception ? error.WithMessage(exception.Message) : error)
            .AddObjectType<BatchEntity>(d =>
            {
                var node = d.ImplementsNode();
                _ = registration switch
                {
                    0 => node.ResolveNodeBatch<string>((_, _) => Task.FromResult<IReadOnlyList<BatchEntity?>>([])),
                    1 => node.ResolveNodeBatch(_ => new ValueTask<IReadOnlyList<ResolverResult>>([])),
                    _ => node.ResolveNodeBatchWith(typeof(FluentBatchNodeResolver).GetMethod(nameof(FluentBatchNodeResolver.WrongCount))!)
                };
            })
            .BuildRequestExecutorAsync(cancellationToken: TestContext.Current.CancellationToken);

        // act
        var result = await executor.ExecuteAsync(
            """
            {
                node(id: "QmF0Y2hFbnRpdHk6eA==") { ... on BatchEntity { name } }
            }
            """,
            cancellationToken: TestContext.Current.CancellationToken);

        // assert
        result.MatchSnapshot();
    }

    [Fact]
    public async Task ResolveNodeBatchWith_Should_Deny_Method_Policy_When_Not_Allowed()
    {
        // arrange
        var collector = new BatchNodeCollector();
        var handler = new NodePolicyHandler(false);
        var executor = await new ServiceCollection()
            .AddSingleton(collector)
            .AddGraphQL()
            .AddAuthorizationHandler(_ => handler)
            .AddGlobalObjectIdentification()
            .AddQueryType(d => d.Field("ready").Resolve(true))
            .AddObjectType<BatchEntity>(d => d.ImplementsNode().IdField(n => n.Id)
                .ResolveNodeWith<FluentBatchNodeResolver>(r => r.Protected(default!, default!)))
            .BuildRequestExecutorAsync(cancellationToken: TestContext.Current.CancellationToken);

        // act
        var result = await executor.ExecuteAsync(
            """
            {
                node(id: "QmF0Y2hFbnRpdHk6eA==") { ... on BatchEntity { name } }
            }
            """,
            cancellationToken: TestContext.Current.CancellationToken);

        // assert
        new Snapshot()
            .Add(result, "Result")
            .Add(new { collector.InvocationCount, handler.Policies }, "Authorization")
            .MatchMarkdownSnapshot();
    }

    [Fact]
    public async Task ResolveNodeBatchWith_Should_Preserve_Partitioner_When_Ids_Share_A_Selection()
    {
        // arrange
        var collector = new BatchNodeCollector();
        var executor = await new ServiceCollection()
            .AddSingleton(collector)
            .AddGraphQL()
            .AddGlobalObjectIdentification()
            .AddQueryType(d => d.Field("ready").Resolve(true))
            .AddObjectType<BatchEntity>(d => d.ImplementsNode()
                .ResolveNodeBatchWith<FluentBatchNodeResolver>(r => r.Partitioned(default!, default!)))
            .BuildRequestExecutorAsync(cancellationToken: TestContext.Current.CancellationToken);

        // act
        var result = await executor.ExecuteAsync(
            """
            {
                nodes(ids: ["QmF0Y2hFbnRpdHk6eA==", "QmF0Y2hFbnRpdHk6eQ==", "QmF0Y2hFbnRpdHk6eA=="]) {
                    ... on BatchEntity { name }
                }
            }
            """,
            cancellationToken: TestContext.Current.CancellationToken);

        // assert
        new Snapshot()
            .Add(result, "Result")
            .Add(new { collector.InvocationCount, collector.BatchSizes, collector.ReceivedIds }, "Dispatch")
            .MatchMarkdownSnapshot();
    }

    [Fact]
    public async Task ResolveNodeBatch_Should_Use_Innermost_Formatters_When_Method_Declares_Middleware()
    {
        // arrange
        var collector = new BatchNodeCollector();
        var executor = await new ServiceCollection()
            .AddSingleton(collector)
            .AddGraphQL()
            .AddGlobalObjectIdentification()
            .AddQueryType(d => d.Field("ready").Resolve(true))
            .AddDirectiveType<NodeBatchDirectiveType>()
            .AddObjectType<BatchEntity>(d => d.ImplementsNode()
                .ResolveNodeBatchWith(typeof(FluentBatchNodeResolver).GetMethod(nameof(FluentBatchNodeResolver.Formatted))!))
            .BuildRequestExecutorAsync(cancellationToken: TestContext.Current.CancellationToken);

        // act
        var result = await executor.ExecuteAsync(
            """
            {
                nodes(ids: ["QmF0Y2hFbnRpdHk6eA==", "QmF0Y2hFbnRpdHk6eQ==", "QmF0Y2hFbnRpdHk6eA=="]) {
                    ... on BatchEntity { name }
                }
            }
            """,
            cancellationToken: TestContext.Current.CancellationToken);

        // assert
        new Snapshot()
            .Add(result, "Result")
            .Add(new { collector.InvocationCount, collector.ReceivedIds }, "Dispatch")
            .MatchMarkdownSnapshot();
    }

    [Fact]
    public async Task ResolveNodeWith_Should_Batch_Ids_When_Node_Attribute_Infers_Resolver()
    {
        // arrange
        var collector = new BatchNodeCollector();
        var executor = await new ServiceCollection()
            .AddSingleton(collector)
            .AddGraphQL()
            .AddGlobalObjectIdentification()
            .AddQueryType(d => d.Field("ready").Resolve(true))
            .AddType<InferredBatchEntity>()
            .BuildRequestExecutorAsync(cancellationToken: TestContext.Current.CancellationToken);

        // act
        var result = await executor.ExecuteAsync(
            """
            {
                nodes(ids: ["SW5mZXJyZWRCYXRjaEVudGl0eTp4", "SW5mZXJyZWRCYXRjaEVudGl0eTp5"]) {
                    ... on InferredBatchEntity { id }
                }
            }
            """,
            cancellationToken: TestContext.Current.CancellationToken);

        // assert
        new Snapshot()
            .Add(result, "Result")
            .Add(new { collector.InvocationCount, collector.ReceivedIds }, "Dispatch")
            .MatchMarkdownSnapshot();
    }

    [Theory]
    [InlineData(ApplyPolicy.BeforeResolver, false)]
    [InlineData(ApplyPolicy.BeforeResolver, true)]
    [InlineData(ApplyPolicy.AfterResolver, false)]
    [InlineData(ApplyPolicy.AfterResolver, true)]
    [InlineData(ApplyPolicy.Validation, false)]
    [InlineData(ApplyPolicy.Validation, true)]
    public async Task ResolveNodeBatch_Should_Enforce_Type_Policy_When_Registered(
        ApplyPolicy apply,
        bool allowed)
    {
        // arrange
        var collector = new BatchNodeCollector();
        var handler = new NodePolicyHandler(allowed);
        var executor = await new ServiceCollection()
            .AddGraphQL()
            .AddAuthorizationHandler(_ => handler)
            .AddGlobalObjectIdentification()
            .AddQueryType(d => d.Field("ready").Resolve(true))
            .AddObjectType<BatchEntity>(d =>
            {
                d.Authorize("read-node", apply);
                d.ImplementsNode().IdField(n => n.Id).ResolveNodeBatch((_, ids) =>
                {
                    collector.Record(ids);
                    return Task.FromResult<IReadOnlyList<BatchEntity?>>(
                        ids.Select(id => new BatchEntity { Name = id }).ToArray());
                });
            })
            .BuildRequestExecutorAsync(cancellationToken: TestContext.Current.CancellationToken);

        // act
        var result = await executor.ExecuteAsync(
            """
            {
                nodes(ids: ["QmF0Y2hFbnRpdHk6eA==", "QmF0Y2hFbnRpdHk6eQ=="]) {
                    ... on BatchEntity { name }
                }
            }
            """,
            cancellationToken: TestContext.Current.CancellationToken);

        // assert
        new Snapshot(postFix: $"{apply}-{allowed}")
            .Add(result, "Result")
            .Add(new { collector.InvocationCount, Policies = handler.Policies.Order().ToArray() }, "Authorization")
            .MatchMarkdownSnapshot();
    }

    [Fact]
    public async Task ResolveNodeBatch_Should_Isolate_Error_And_Null_Entries_When_Delegate_Returns_Results()
    {
        // arrange
        var calls = 0;
        var executor = await new ServiceCollection()
            .AddGraphQL()
            .AddGlobalObjectIdentification()
            .AddQueryType(d => d.Field("ready").Resolve(true))
            .AddObjectType<BatchEntity>(d => d.ImplementsNode().ResolveNodeBatch(contexts =>
            {
                calls++;
                return new ValueTask<IReadOnlyList<ResolverResult>>(
                [
                    ResolverResult.Ok(new BatchEntity { Name = "x" }),
                    ResolverResult.Fail(ErrorHelper.NodeMissing()),
                    ResolverResult.Ok(null),
                    ResolverResult.Ok(new BatchEntity { Name = "x" })
                ]);
            }))
            .BuildRequestExecutorAsync(cancellationToken: TestContext.Current.CancellationToken);

        // act
        var result = await executor.ExecuteAsync(
            """
            {
                nodes(ids: ["QmF0Y2hFbnRpdHk6eA==", "QmF0Y2hFbnRpdHk6eQ==", "QmF0Y2hFbnRpdHk6eQ==", "QmF0Y2hFbnRpdHk6eA=="]) {
                    ... on BatchEntity { name }
                }
            }
            """,
            cancellationToken: TestContext.Current.CancellationToken);

        // assert
        new Snapshot().Add(result, "Result").Add(calls, "Calls").MatchMarkdownSnapshot();
    }

    [Fact]
    public async Task ResolveNodeBatch_Should_Report_Error_At_Indexed_Path_When_Fluent_Delegate_Fails_One_Entry()
    {
        // arrange
        var calls = 0;
        var executor = await new ServiceCollection()
            .AddGraphQL()
            .AddGlobalObjectIdentification()
            .AddQueryType(d => d.Field("ready").Resolve(true))
            .AddObjectType<BatchEntity>(d => d.ImplementsNode().ResolveNodeBatch(contexts =>
            {
                calls++;
                return new ValueTask<IReadOnlyList<ResolverResult>>(
                [
                    ResolverResult.Ok(new BatchEntity { Name = "x" }),
                    ResolverResult.Fail(ErrorHelper.NodeMissing())
                ]);
            }))
            .BuildRequestExecutorAsync(cancellationToken: TestContext.Current.CancellationToken);

        // act
        var result = await executor.ExecuteAsync(
            """
            {
                nodes(ids: ["QmF0Y2hFbnRpdHk6eA==", "QmF0Y2hFbnRpdHk6eQ=="]) {
                    ... on BatchEntity { name }
                }
            }
            """,
            cancellationToken: TestContext.Current.CancellationToken);

        // assert
        new Snapshot().Add(result, "Result").Add(calls, "Calls").MatchMarkdownSnapshot();
    }

    [Fact]
    public async Task ResolveNodeBatch_Should_Index_Error_Path_When_Typed_Delegate_Reports_Error_For_One_Entry()
    {
        // arrange
        var calls = 0;
        var executor = await new ServiceCollection()
            .AddGraphQL()
            .AddGlobalObjectIdentification()
            .AddQueryType(d => d.Field("ready").Resolve(true))
            .AddObjectType<BatchEntity>(d => d.ImplementsNode().IdField(n => n.Id)
                .ResolveNodeBatch((contexts, ids) =>
                {
                    calls++;
                    var results = new BatchEntity?[ids.Count];

                    for (var i = 0; i < ids.Count; i++)
                    {
                        if (ids[i] == "y")
                        {
                            contexts[i].ReportError(ErrorHelper.NodeMissing());
                            continue;
                        }

                        results[i] = new BatchEntity { Name = ids[i] };
                    }

                    return Task.FromResult<IReadOnlyList<BatchEntity?>>(results);
                }))
            .BuildRequestExecutorAsync(cancellationToken: TestContext.Current.CancellationToken);

        // act
        var result = await executor.ExecuteAsync(
            """
            {
                nodes(ids: ["QmF0Y2hFbnRpdHk6eA==", "QmF0Y2hFbnRpdHk6eQ=="]) {
                    ... on BatchEntity { name }
                }
            }
            """,
            cancellationToken: TestContext.Current.CancellationToken);

        // assert
        new Snapshot().Add(result, "Result").Add(calls, "Calls").MatchMarkdownSnapshot();
    }

    [Fact]
    public async Task ResolveNodeBatch_Should_Report_Error_At_Indexed_Path_When_Classic_Resolver_Throws_For_One_Entry()
    {
        // arrange
        var calls = 0;
        var executor = await new ServiceCollection()
            .AddGraphQL()
            .AddGlobalObjectIdentification()
            .AddQueryType(d => d.Field("ready").Resolve(true))
            .AddObjectType<BatchEntity>(d => d.ImplementsNode().IdField(n => n.Id)
                .ResolveNode((_, id) =>
                {
                    calls++;

                    if (id == "y")
                    {
                        throw new InvalidOperationException("boom");
                    }

                    return Task.FromResult<BatchEntity?>(new BatchEntity { Name = id });
                }))
            .BuildRequestExecutorAsync(cancellationToken: TestContext.Current.CancellationToken);

        // act
        var result = await executor.ExecuteAsync(
            """
            {
                nodes(ids: ["QmF0Y2hFbnRpdHk6eA==", "QmF0Y2hFbnRpdHk6eQ=="]) {
                    ... on BatchEntity { name }
                }
            }
            """,
            cancellationToken: TestContext.Current.CancellationToken);

        // assert
        new Snapshot().Add(result, "Result").Add(calls, "Calls").MatchMarkdownSnapshot();
    }

    [Fact]
    public async Task ResolveNodeBatch_Should_Index_Error_Path_When_One_Id_Has_No_Node_Resolver()
    {
        // arrange
        var calls = 0;
        var executor = await new ServiceCollection()
            .AddGraphQL()
            .AddGlobalObjectIdentification()
            .AddQueryType(d => d.Field("ready").Resolve(true))
            .AddObjectType<BatchEntity>(d => d.ImplementsNode().IdField(n => n.Id)
                .ResolveNodeBatch((_, ids) =>
                {
                    calls++;
                    return Task.FromResult<IReadOnlyList<BatchEntity?>>(
                        ids.Select(id => new BatchEntity { Name = id }).ToArray());
                }))
            .BuildRequestExecutorAsync(cancellationToken: TestContext.Current.CancellationToken);

        // act
        var result = await executor.ExecuteAsync(
            """
            {
                nodes(ids: ["QmF0Y2hFbnRpdHk6eA==", "UXVlcnk6MQ=="]) {
                    ... on BatchEntity { name }
                }
            }
            """,
            cancellationToken: TestContext.Current.CancellationToken);

        // assert
        new Snapshot().Add(result, "Result").Add(calls, "Calls").MatchMarkdownSnapshot();
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(0, 1)]
    [InlineData(0, 2)]
    [InlineData(0, 3)]
    [InlineData(0, 4)]
    [InlineData(0, 5)]
    [InlineData(1, 0)]
    [InlineData(1, 1)]
    [InlineData(1, 2)]
    [InlineData(1, 3)]
    [InlineData(1, 4)]
    [InlineData(1, 5)]
    [InlineData(2, 0)]
    [InlineData(2, 1)]
    [InlineData(2, 2)]
    [InlineData(2, 3)]
    [InlineData(2, 4)]
    [InlineData(2, 5)]
    public async Task ResolveNodeBatch_Should_Preserve_Positions_And_Separate_Aliases_When_Configured(
        int style,
        int registration)
    {
        // arrange
        var collector = new BatchNodeCollector();
        var method = typeof(FluentBatchNodeResolver).GetMethod(nameof(FluentBatchNodeResolver.Resolve))!;
        var builder = new ServiceCollection()
            .AddSingleton(collector)
            .AddGraphQL()
            .AddGlobalObjectIdentification()
            .AddQueryType(d => d.Field("ready").Resolve(true));

        if (style == 0)
        {
            builder.AddObjectType(d =>
            {
                d.Name(nameof(BatchEntity));
                d.Field("name").Type<StringType>().Resolve(c => c.Parent<BatchEntity>().Name);
                var node = d.ImplementsNode();
                var field = registration switch
                {
                    0 => node.ResolveNodeBatch<string>(ResolveObjects),
                    1 => node.ResolveNodeBatch(ResolveResults),
                    2 => node.ResolveNodeBatchWith<FluentBatchNodeResolver>(r => r.Resolve(default!, default!)),
                    3 => node.ResolveNodeBatchWith(method),
                    4 => node.ResolveNodeWith<FluentBatchNodeResolver>(r => r.Resolve(default!, default!)),
                    _ => node.ResolveNodeWith(method)
                };
                field.Resolve(c => c.Parent<BatchEntity>().Id);
            });
        }
        else
        {
            builder.AddObjectType<BatchEntity>(d =>
            {
                var node = d.ImplementsNode();
                if (style == 1)
                {
                    _ = registration switch
                    {
                        0 => node.ResolveNodeBatch<string>(ResolveNodes),
                        1 => node.ResolveNodeBatch(ResolveResults),
                        2 => node.ResolveNodeBatchWith<FluentBatchNodeResolver>(r => r.Resolve(default!, default!)),
                        3 => node.ResolveNodeBatchWith(method),
                        4 => node.ResolveNodeWith<FluentBatchNodeResolver>(r => r.Resolve(default!, default!)),
                        _ => node.ResolveNodeWith(method)
                    };
                }
                else
                {
                    var withId = node.IdField(n => n.Id);
                    _ = registration switch
                    {
                        0 => withId.ResolveNodeBatch(ResolveNodes),
                        1 => withId.ResolveNodeBatch(ResolveResults),
                        2 => withId.ResolveNodeBatchWith<FluentBatchNodeResolver>(r => r.Resolve(default!, default!)),
                        3 => withId.ResolveNodeBatchWith(method),
                        4 => withId.ResolveNodeWith<FluentBatchNodeResolver>(r => r.Resolve(default!, default!)),
                        _ => withId.ResolveNodeWith(method)
                    };
                }
            });
        }

        var executor = await builder.BuildRequestExecutorAsync(
            cancellationToken: TestContext.Current.CancellationToken);

        // act
        var result = await executor.ExecuteAsync(
            """
            {
                nodes(ids: ["QmF0Y2hFbnRpdHk6eA==", "QmF0Y2hFbnRpdHk6eQ==", "QmF0Y2hFbnRpdHk6eA=="]) {
                    ... on BatchEntity { id name }
                }
                alias: nodes(ids: ["QmF0Y2hFbnRpdHk6eQ=="]) { ... on BatchEntity { name } }
                single: node(id: "QmF0Y2hFbnRpdHk6eA==") { ... on BatchEntity { name } }
                malformed: node(id: "garbage") { ... on BatchEntity { name } }
            }
            """,
            cancellationToken: TestContext.Current.CancellationToken);

        // assert
        var type = executor.Schema.Types.GetType<ObjectType>(nameof(BatchEntity));
        type.TryGetNodeResolver(out var resolver);
        var snapshot = new Snapshot(postFix: style.ToString());
        snapshot.Add(result, "Result");
        snapshot.Add(new
        {
            collector.InvocationCount,
            BatchSizes = collector.BatchSizes.Order().ToArray(),
            Ids = collector.ReceivedIds.Order().ToArray(),
            RegularPipeline = resolver?.Pipeline is not null,
            BatchPipeline = resolver?.BatchPipeline is not null
        }, "Dispatch");
        snapshot.Add(executor.Schema, "Schema");
        snapshot.MatchMarkdownSnapshot();

        Task<IReadOnlyList<BatchEntity?>> ResolveNodes(IReadOnlyList<IResolverContext> contexts, IReadOnlyList<string> ids)
        {
            collector.Record(ids);
            return Task.FromResult<IReadOnlyList<BatchEntity?>>(ids.Select(id => new BatchEntity { Name = id }).ToArray());
        }

        async Task<IReadOnlyList<object?>> ResolveObjects(IReadOnlyList<IResolverContext> contexts, IReadOnlyList<string> ids)
            => await ResolveNodes(contexts, ids);

        async ValueTask<IReadOnlyList<ResolverResult>> ResolveResults(IReadOnlyList<IResolverContext> contexts)
        {
            var ids = contexts.Select(c => c.GetLocalState<string>(WellKnownContextData.InternalId)).ToArray();
            var nodes = await ResolveNodes(contexts, ids);
            return nodes.Select(ResolverResult.Ok).ToArray();
        }
    }

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
    public async Task Node_Should_Isolate_Type_Group_When_Batch_Resolver_Throws_For_One_Variable_Batch_Set()
    {
        // arrange
        // A variable batch shares one node(id:) selection across two virtual root contexts, so
        // both sets dispatch through a single ResolveNodeBatchAsync call. The second set's id
        // resolves to a type whose batch resolver throws; the first set's healthy type group
        // must still resolve instead of being poisoned by the type group failure.
        var okCollector = new BatchNodeCollector();
        var failingCollector = new BatchNodeCollector();
        var executor = await new ServiceCollection()
            .AddGraphQL()
            .AddGlobalObjectIdentification()
            .AddQueryType(d => d.Field("ready").Resolve(true))
            .AddObjectType<BatchEntity>(d => d.ImplementsNode().IdField(n => n.Id)
                .ResolveNodeBatch((_, ids) =>
                {
                    okCollector.Record(ids);
                    return Task.FromResult<IReadOnlyList<BatchEntity?>>(
                        ids.Select(id => new BatchEntity { Name = id }).ToArray());
                }))
            .AddObjectType<FailingBatchEntity>(d => d.ImplementsNode().IdField(n => n.Id)
                .ResolveNodeBatch((_, ids) =>
                {
                    failingCollector.Record(ids);
                    throw new InvalidOperationException("The batch resolver failed.");
                }))
            .BuildRequestExecutorAsync(cancellationToken: TestContext.Current.CancellationToken);

        var sets = new List<IReadOnlyDictionary<string, object?>>
        {
            new Dictionary<string, object?> { ["id"] = "QmF0Y2hFbnRpdHk6eA==" },
            new Dictionary<string, object?> { ["id"] = "RmFpbGluZ0JhdGNoRW50aXR5OjE=" }
        };

        // act
        var result = await executor.ExecuteAsync(
            OperationRequestBuilder.New()
                .SetDocument(
                    """
                    query($id: ID!) {
                        node(id: $id) {
                            ... on BatchEntity { name }
                            ... on FailingBatchEntity { name }
                        }
                    }
                    """)
                .SetVariableValues(sets)
                .Build(),
            cancellationToken: TestContext.Current.CancellationToken);

        // assert
        var batch = Assert.IsType<OperationResultBatch>(result);
        new Snapshot()
            .Add(batch.Results[0], "Set 0")
            .Add(batch.Results[1], "Set 1")
            .Add(
                new
                {
                    OkInvocationCount = okCollector.InvocationCount,
                    FailingInvocationCount = failingCollector.InvocationCount
                },
                "Dispatch")
            .MatchMarkdownSnapshot();
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
    public async Task Nodes_Should_Isolate_Type_Group_When_Batch_Resolver_Throws_For_One_Type()
    {
        // arrange
        // Two node types share one nodes() call. The failing type's batch resolver throws for
        // its whole slice while the healthy type's batch resolver still dispatches and resolves.
        var okCollector = new BatchNodeCollector();
        var failingCollector = new BatchNodeCollector();
        var executor = await new ServiceCollection()
            .AddGraphQL()
            .AddGlobalObjectIdentification()
            .AddQueryType(d => d.Field("ready").Resolve(true))
            .AddObjectType<BatchEntity>(d => d.ImplementsNode().IdField(n => n.Id)
                .ResolveNodeBatch((_, ids) =>
                {
                    okCollector.Record(ids);
                    return Task.FromResult<IReadOnlyList<BatchEntity?>>(
                        ids.Select(id => new BatchEntity { Name = id }).ToArray());
                }))
            .AddObjectType<FailingBatchEntity>(d => d.ImplementsNode().IdField(n => n.Id)
                .ResolveNodeBatch((_, ids) =>
                {
                    failingCollector.Record(ids);
                    throw new InvalidOperationException("The batch resolver failed.");
                }))
            .BuildRequestExecutorAsync(cancellationToken: TestContext.Current.CancellationToken);

        // act
        var result = await executor.ExecuteAsync(
            """
            {
                nodes(ids: [
                    "RmFpbGluZ0JhdGNoRW50aXR5OjE=",
                    "QmF0Y2hFbnRpdHk6eA==",
                    "RmFpbGluZ0JhdGNoRW50aXR5OjI=",
                    "QmF0Y2hFbnRpdHk6eQ=="
                ]) {
                    ... on BatchEntity { name }
                    ... on FailingBatchEntity { name }
                }
            }
            """,
            cancellationToken: TestContext.Current.CancellationToken);

        // assert
        new Snapshot()
            .Add(result, "Result")
            .Add(
                new
                {
                    OkInvocationCount = okCollector.InvocationCount,
                    FailingInvocationCount = failingCollector.InvocationCount
                },
                "Dispatch")
            .MatchMarkdownSnapshot();
    }

    [Fact]
    public async Task Nodes_Should_Leave_Sibling_Alias_Untouched_When_Other_Alias_Batch_Resolver_Throws()
    {
        // arrange
        // Aliased nodes fields dispatch independently. A failing type's batch resolver in one
        // alias must not affect a sibling alias that never references that type.
        var okCollector = new BatchNodeCollector();
        var failingCollector = new BatchNodeCollector();
        var executor = await new ServiceCollection()
            .AddGraphQL()
            .AddGlobalObjectIdentification()
            .AddQueryType(d => d.Field("ready").Resolve(true))
            .AddObjectType<BatchEntity>(d => d.ImplementsNode().IdField(n => n.Id)
                .ResolveNodeBatch((_, ids) =>
                {
                    okCollector.Record(ids);
                    return Task.FromResult<IReadOnlyList<BatchEntity?>>(
                        ids.Select(id => new BatchEntity { Name = id }).ToArray());
                }))
            .AddObjectType<FailingBatchEntity>(d => d.ImplementsNode().IdField(n => n.Id)
                .ResolveNodeBatch((_, ids) =>
                {
                    failingCollector.Record(ids);
                    throw new InvalidOperationException("The batch resolver failed.");
                }))
            .BuildRequestExecutorAsync(cancellationToken: TestContext.Current.CancellationToken);

        // act
        var result = await executor.ExecuteAsync(
            """
            {
                a: nodes(ids: ["RmFpbGluZ0JhdGNoRW50aXR5OjE=", "QmF0Y2hFbnRpdHk6eA=="]) {
                    ... on BatchEntity { name }
                    ... on FailingBatchEntity { name }
                }
                b: nodes(ids: ["QmF0Y2hFbnRpdHk6eQ=="]) {
                    ... on BatchEntity { name }
                }
            }
            """,
            cancellationToken: TestContext.Current.CancellationToken);

        // assert
        new Snapshot()
            .Add(result, "Result")
            .Add(
                new
                {
                    OkInvocationCount = okCollector.InvocationCount,
                    FailingInvocationCount = failingCollector.InvocationCount
                },
                "Dispatch")
            .MatchMarkdownSnapshot();
    }

    [Fact]
    public async Task Nodes_Should_Isolate_Partition_When_Inner_Partition_Batch_Resolver_Throws_For_One_Key()
    {
        // arrange
        // The resolver partitions its slice by internal id. The partition holding "x" throws
        // while the sibling partition holding "y" still resolves.
        var collector = new BatchNodeCollector();
        var executor = await new ServiceCollection()
            .AddSingleton(collector)
            .AddGraphQL()
            .AddGlobalObjectIdentification()
            .AddQueryType(d => d.Field("ready").Resolve(true))
            .AddObjectType<BatchEntity>(d => d.ImplementsNode()
                .ResolveNodeBatchWith<FluentBatchNodeResolver>(r => r.PartitionedThrowing(default!, default!)))
            .BuildRequestExecutorAsync(cancellationToken: TestContext.Current.CancellationToken);

        // act
        var result = await executor.ExecuteAsync(
            """
            {
                nodes(ids: ["QmF0Y2hFbnRpdHk6eA==", "QmF0Y2hFbnRpdHk6eA==", "QmF0Y2hFbnRpdHk6eQ=="]) {
                    ... on BatchEntity { name }
                }
            }
            """,
            cancellationToken: TestContext.Current.CancellationToken);

        // assert
        new Snapshot()
            .Add(result, "Result")
            .Add(new { collector.InvocationCount, collector.BatchSizes, collector.ReceivedIds }, "Dispatch")
            .MatchMarkdownSnapshot();
    }

    [Fact]
    public async Task Nodes_Should_Isolate_Type_Group_When_Inner_Partition_Key_Resolver_Throws_For_One_Entry()
    {
        // arrange
        // The inner partition key delegate itself throws while computing the key for the second
        // FailingBatchEntity id, before any slice of that type group is dispatched. The whole
        // type group is isolated through the outer DispatchTypeGroupAsync boundary while the
        // sibling BatchEntity group still resolves.
        var collector = new BatchNodeCollector();
        var executor = await new ServiceCollection()
            .AddSingleton(collector)
            .AddGraphQL()
            .AddGlobalObjectIdentification()
            .AddQueryType(d => d.Field("ready").Resolve(true))
            .AddObjectType<BatchEntity>(d => d.ImplementsNode()
                .ResolveNodeBatchWith<FluentBatchNodeResolver>(r => r.Resolve(default!, default!)))
            .AddObjectType<FailingBatchEntity>(d => d.ImplementsNode()
                .ResolveNodeBatchWith<FluentBatchNodeResolver>(r => r.PartitionKeyThrowing(default!, default!)))
            .BuildRequestExecutorAsync(cancellationToken: TestContext.Current.CancellationToken);

        // act
        var result = await executor.ExecuteAsync(
            """
            {
                nodes(ids: [
                    "RmFpbGluZ0JhdGNoRW50aXR5OjE=",
                    "QmF0Y2hFbnRpdHk6eA==",
                    "RmFpbGluZ0JhdGNoRW50aXR5OjI=",
                    "QmF0Y2hFbnRpdHk6eQ=="
                ]) {
                    ... on BatchEntity { name }
                    ... on FailingBatchEntity { name }
                }
            }
            """,
            cancellationToken: TestContext.Current.CancellationToken);

        // assert
        new Snapshot()
            .Add(result, "Result")
            .Add(new { collector.InvocationCount, collector.BatchSizes, collector.ReceivedIds }, "Dispatch")
            .MatchMarkdownSnapshot();
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

    public class FailingBatchEntity
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
        private readonly List<int> _batchSizes = [];

        public int InvocationCount { get; private set; }

        public IReadOnlyList<string> ReceivedIds => _receivedIds;

        public IReadOnlyList<int> BatchSizes => _batchSizes;

        public void Record(IEnumerable<string> ids)
        {
            InvocationCount++;
            var values = ids.ToArray();
            _receivedIds.AddRange(values);
            _batchSizes.Add(values.Length);
        }
    }

    public class FluentBatchNodeResolver
    {
        public List<BatchEntity> WrongCount(List<string> id) => [];

        [BatchResolver]
        [Authorize("read-node")]
        public List<BatchEntity> Protected(List<string> id, [Service] BatchNodeCollector collector)
            => Resolve(id, collector);

        [NodeBatchPartition]
        public BatchEntity[] Partitioned(List<string> id, [Service] BatchNodeCollector collector)
            => Resolve(id, collector).ToArray();

        [NodeBatchPartition]
        public BatchEntity[] PartitionedThrowing(List<string> id, [Service] BatchNodeCollector collector)
        {
            collector.Record(id);

            if (id.Contains("x"))
            {
                throw new InvalidOperationException("The partition resolver failed.");
            }

            return id.Select(value => new BatchEntity { Name = value }).ToArray();
        }

        [NodeBatchPartitionKeyThrowing]
        public List<FailingBatchEntity> PartitionKeyThrowing(List<string> id, [Service] BatchNodeCollector collector)
        {
            collector.Record(id);
            return id.Select(value => new FailingBatchEntity { Name = value }).ToList();
        }

        [BatchResolver]
        public List<BatchEntity> Resolve(IReadOnlyList<string> id, [Service] BatchNodeCollector collector)
        {
            collector.Record(id);
            return id.Select(value => new BatchEntity { Name = value }).ToList();
        }

        [NodeBatchPipeline]
        public ValueTask<ImmutableArray<BatchEntity>> Formatted(
            string[] id,
            [Service] BatchNodeCollector collector)
        {
            collector.Record(id);
            return new(id.Select(value => new BatchEntity { Name = value }).ToImmutableArray());
        }
    }

    [Node(NodeResolver = nameof(GetAsync))]
    public class InferredBatchEntity(string id)
    {
        public string Id => id;

        [NodeResolver]
        [BatchResolver]
        public static Task<List<InferredBatchEntity>> GetAsync(
            ImmutableArray<string> keys,
            [Service] BatchNodeCollector collector)
        {
            collector.Record(keys);
            return Task.FromResult(keys.Select(key => new InferredBatchEntity(key)).ToList());
        }
    }

    private sealed class NodeBatchPipelineAttribute : ObjectFieldDescriptorAttribute
    {
        protected override void OnConfigure(
            IDescriptorContext context,
            IObjectFieldDescriptor descriptor,
            MemberInfo? member)
        {
            descriptor.Directive("nodeBatch");
            descriptor.UseBatch(next => async contexts =>
            {
                contexts[0].Result = new BatchEntity { Name = "cached" };
                await next(contexts);
                foreach (var entry in contexts)
                {
                    entry.Result = Append(entry.Result, ":middleware");
                }
            });
            var configuration = descriptor.Extend().Configuration;
            configuration.FormatterConfigurations.Add(new ResultFormatterConfiguration((_, value) => Append(value, ":first")));
            configuration.FormatterConfigurations.Add(new ResultFormatterConfiguration((_, value) => Append(value, ":second")));
        }
    }

    private sealed class NodeBatchPartitionAttribute : ObjectFieldDescriptorAttribute
    {
        protected override void OnConfigure(
            IDescriptorContext context,
            IObjectFieldDescriptor descriptor,
            MemberInfo? member)
        {
            descriptor.Extend().Configuration.BatchPartitionKeyResolver =
                c => c.GetLocalState<string>(WellKnownContextData.InternalId) == "x" ? 0UL : 1UL;
        }
    }

    private sealed class NodeBatchPartitionKeyThrowingAttribute : ObjectFieldDescriptorAttribute
    {
        protected override void OnConfigure(
            IDescriptorContext context,
            IObjectFieldDescriptor descriptor,
            MemberInfo? member)
        {
            // The key delegate itself throws for the second entry, before any slice of the
            // group is dispatched, so the failure escapes the partition-building loop entirely.
            descriptor.Extend().Configuration.BatchPartitionKeyResolver =
                c => c.GetLocalState<string>(WellKnownContextData.InternalId) == "2"
                    ? throw new InvalidOperationException("The partition key resolver failed.")
                    : 0UL;
        }
    }

    private sealed class NodeBatchDirectiveType : DirectiveType
    {
        protected override void Configure(IDirectiveTypeDescriptor descriptor)
        {
            descriptor.Name("nodeBatch").Location(DirectiveLocation.FieldDefinition);
            descriptor.UseBatch((next, _) => async contexts =>
            {
                await next(contexts);
                foreach (var entry in contexts)
                {
                    entry.Result = Append(entry.Result, ":directive");
                }
            });
        }
    }

    private static BatchEntity? Append(object? value, string suffix)
        => value is BatchEntity node ? new BatchEntity { Name = node.Name + suffix } : null;

    private sealed class NodePolicyHandler(bool allowed) : IAuthorizationHandler
    {
        public List<string?> Policies { get; } = [];

        public ValueTask<AuthorizeResult> AuthorizeAsync(
            IMiddlewareContext context,
            AuthorizeDirective directive,
            CancellationToken cancellationToken)
        {
            Policies.Add(directive.Policy);
            return new(allowed ? AuthorizeResult.Allowed : AuthorizeResult.NotAllowed);
        }

        public ValueTask<AuthorizeResult> AuthorizeAsync(
            AuthorizationContext context,
            IReadOnlyList<AuthorizeDirective> directives,
            CancellationToken cancellationToken)
        {
            Policies.AddRange(directives.Select(d => d.Policy));
            return new(allowed ? AuthorizeResult.Allowed : AuthorizeResult.NotAllowed);
        }
    }

    private static class ErrorHelper
    {
        public static IError NodeMissing()
            => ErrorBuilder.New().SetMessage("missing node").Build();
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
