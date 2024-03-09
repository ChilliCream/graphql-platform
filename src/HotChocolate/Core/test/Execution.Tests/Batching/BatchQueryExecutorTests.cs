using CookieCrumble;
using Microsoft.Extensions.DependencyInjection;
using HotChocolate.StarWars;
using HotChocolate.Tests;
using HotChocolate.Types;
using static HotChocolate.Tests.TestHelper;
using Snapshot = Snapshooter.Xunit.Snapshot;

namespace HotChocolate.Execution.Batching;

public class BatchQueryExecutorTests
{
    [Fact]
    public async Task ExecuteExportScalar()
    {
        // arrange
        Snapshot.FullName();

        var executor = await CreateExecutorAsync(c => c
            .AddStarWarsTypes()
            .AddExportDirectiveType()
            .Services
            .AddStarWarsRepositories());

        // act
        var batch = new List<IOperationRequest>
        {
            OperationRequestBuilder.Create()
                .SetDocument(
                    @"
                        query getHero {
                            hero(episode: EMPIRE) {
                                id @export
                            }
                        }")
                .Build(),
            OperationRequestBuilder.Create()
                .SetDocument(
                    @"
                        query getHuman {
                            human(id: $id) {
                                name
                            }
                        }")
                .Build(),
        };

        var batchResult = await executor.ExecuteBatchAsync(batch);

        // assert
        await batchResult.MatchSnapshotAsync();
    }

    [LocalFact]
    public async Task ExecuteExportScalarList()
    {
        // arrange
        Snapshot.FullName();

        var executor = await CreateExecutorAsync(c => c
            .AddStarWarsTypes()
            .AddExportDirectiveType()
            .Services
            .AddStarWarsRepositories());

        // act
        var batch = new List<IOperationRequest>
        {
            OperationRequestBuilder.Create()
                .SetDocument(
                    @"
                        query getHero {
                            hero(episode: EMPIRE) {
                                friends {
                                    nodes {
                                        id @export(as: ""abc"")
                                    }
                                }
                            }
                        }")
                .Build(),
            OperationRequestBuilder.Create()
                .SetDocument(
                    @"
                        query getCharacter {
                            character(characterIds: $abc) {
                                name
                            }
                        }")
                .Build(),
        };

        var batchResult = await executor.ExecuteBatchAsync(batch);

        // assert
        await batchResult.MatchSnapshotAsync();
    }

    [Fact]
    public async Task ExecuteExportScalarList_ExplicitVariable()
    {
        // arrange
        Snapshot.FullName();

        var executor = await CreateExecutorAsync(c => c
            .AddStarWarsTypes()
            .AddExportDirectiveType()
            .Services
            .AddStarWarsRepositories());

        // act
        var batch = new List<IOperationRequest>
        {
            OperationRequestBuilder.Create()
                .SetDocument(
                    @"
                        query getHero {
                            hero(episode: EMPIRE) {
                                friends {
                                    nodes {
                                        id @export(as: ""abc"")
                                    }
                                }
                            }
                        }")
                .Build(),
            OperationRequestBuilder.Create()
                .SetDocument(
                    @"
                        query getCharacter($abc: [String!]!) {
                            character(characterIds: $abc) {
                                name
                            }
                        }")
                .Build(),
        };

        var batchResult = await executor.ExecuteBatchAsync(batch);

        // assert
        await batchResult.MatchSnapshotAsync();
    }

    [Fact]
    public async Task ExecuteExportObject()
    {
        // arrange
        Snapshot.FullName();

        var executor = await CreateExecutorAsync(c => c
            .AddStarWarsTypes()
            .AddExportDirectiveType()
            .AddInMemorySubscriptions()
            .Services
            .AddStarWarsRepositories());

        // act
        var batch = new List<IOperationRequest>
        {
            OperationRequestBuilder.Create()
                .SetDocument(
                    @"mutation firstReview {
                            createReview(
                                episode: NEW_HOPE
                                review: { commentary: ""foo"", stars: 4 })
                                    @export(as: ""r"") {
                                commentary
                                stars
                            }
                        }")
                .Build(),
            OperationRequestBuilder.Create()
                .SetDocument(
                    @"
                        mutation secondReview {
                            createReview(
                                episode: EMPIRE
                                review: $r) {
                                commentary
                                stars
                            }
                        }")
                .Build(),
        };

        var batchResult = await executor.ExecuteBatchAsync(batch);

        // assert
        await batchResult.MatchSnapshotAsync();
    }

    [Fact]
    public async Task ExecuteExportLeafList()
    {
        // arrange
        Snapshot.FullName();

        var executor = await CreateExecutorAsync(c => c
            .AddQueryType(d => d.Name("Query")
                .Field("foo")
                .Argument("bar", a => a.Type<ListType<StringType>>())
                .Type<ListType<StringType>>()
                .Resolve(ctx =>
                {
                    var list = ctx.ArgumentValue<List<string>>("bar");

                    if (list is null)
                    {
                        return
                        [
                            "123",
                            "456",
                        ];
                    }

                    list.Add("789");
                    return list;
                }))
            .AddExportDirectiveType());

        // act
        var batch = new List<IOperationRequest>
        {
            OperationRequestBuilder.Create()
                .SetDocument(
                    @"{
                            foo @export(as: ""b"")
                        }")
                .Build(),
            OperationRequestBuilder.Create()
                .SetDocument(
                    @"{
                            foo(bar: $b)
                        }")
                .Build(),
        };

        var batchResult = await executor.ExecuteBatchAsync(batch);

        // assert
        await batchResult.MatchSnapshotAsync();
    }

    [Fact]
    public async Task ExecuteExportObjectList()
    {
        // arrange
        Snapshot.FullName();

        var executor = await CreateExecutorAsync(c => c
            .AddDocumentFromString(
                @"
                    type Query {
                        foo(f: [FooInput]) : [Foo]
                    }

                    type Foo {
                        bar: String!
                    }

                    input FooInput {
                        bar: String!
                    }")
            .AddResolver("Query", "foo", ctx =>
            {
                var list = ctx.ArgumentValue<List<object>>("f");

                if (list is null)
                {
                    return
                    [
                        new Dictionary<string, object>
                        {
                            { "bar", "123" },
                        },

                    ];
                }

                list.Add(new Dictionary<string, object>
                {
                    { "bar" , "456" },
                });
                return list;
            })
            .UseField(next => context =>
            {
                var o = context.Parent<object>();
                if (o is Dictionary<string, object> d
                    && d.TryGetValue(context.ResponseName, out var v))
                {
                    context.Result = v;
                }
                return next(context);
            })
            .AddExportDirectiveType());

        // act
        var batch = new List<IOperationRequest>
        {
            OperationRequestBuilder.Create()
                .SetDocument(
                    @"{
                            foo @export(as: ""b"")
                            {
                                bar
                            }
                        }")
                .Build(),
            OperationRequestBuilder.Create()
                .SetDocument(
                    @"{
                            foo(f: $b)
                            {
                                bar
                            }
                        }")
                .Build(),
        };

        var batchResult = await executor.ExecuteBatchAsync(batch);

        // assert
        await batchResult.MatchSnapshotAsync();
    }

    [Fact]
    public async Task Add_Value_To_Variable_List()
    {
        // arrange
        Snapshot.FullName();

        var executor = await CreateExecutorAsync(c => c
            .AddQueryType(d => d.Name("Query")
                .Field("foo")
                .Argument("bar", a => a.Type<ListType<StringType>>())
                .Type<ListType<StringType>>()
                .Resolve(ctx =>
                {
                    var list = ctx.ArgumentValue<List<string>>("bar");
                    list.Add("789");
                    return list;
                }))
            .AddExportDirectiveType());

        // act
        var batch = new List<IOperationRequest>
        {
            OperationRequestBuilder.Create()
                .SetDocument(
                    @"query foo1($b: [String]) {
                            foo(bar: $b) @export(as: ""b"")
                        }")
                .AddVariableValue("b", new[] { "123", })
                .Create(),
            OperationRequestBuilder.Create()
                .SetDocument(
                    @"query foo2($b: [String]) {
                            foo(bar: $b)
                        }")
                .Build(),
        };

        var batchResult = await executor.ExecuteBatchAsync(batch);

        // assert
        await batchResult.MatchSnapshotAsync();
    }

    [Fact]
    public async Task Convert_List_To_Single_Value_With_Converters()
    {
        // arrange
        Snapshot.FullName();

        var executor = await CreateExecutorAsync(c => c
            .AddQueryType(d =>
            {
                d.Name("Query");

                d.Field("foo")
                    .Argument("bar", a => a.Type<ListType<StringType>>())
                    .Type<ListType<StringType>>()
                    .Resolve(ctx =>
                    {
                        var list = ctx.ArgumentValue<List<string>>("bar");
                        list.Add("789");
                        return list;
                    });

                d.Field("baz")
                    .Argument("bar", a => a.Type<StringType>())
                    .Resolve(ctx => ctx.ArgumentValue<string>("bar"));
            })
            .AddExportDirectiveType());

        // act
        var batch = new List<IOperationRequest>
        {
            OperationRequestBuilder.Create()
                .SetDocument(
                    @"query foo1($b1: [String]) {
                            foo(bar: $b1) @export(as: ""b2"")
                        }")
                .AddVariableValue("b1", new[] { "123", })
                .Create(),
            OperationRequestBuilder.Create()
                .SetDocument(
                    @"query foo2($b2: String) {
                            baz(bar: $b2)
                        }")
                .Build(),
        };

        var batchResult = await executor.ExecuteBatchAsync(batch);

        // assert
        await batchResult.MatchSnapshotAsync();
    }

    [Fact]
    public async Task Batch_Is_Null()
    {
        // arrange
        Snapshot.FullName();

        var executor = await CreateExecutorAsync(c => c
            .AddStarWarsTypes()
            .AddExportDirectiveType()
            .Services
            .AddStarWarsRepositories());

        // act
        Task Action() => executor.ExecuteBatchAsync(null!);

        // assert
        await Assert.ThrowsAsync<ArgumentNullException>(Action);
    }
}
