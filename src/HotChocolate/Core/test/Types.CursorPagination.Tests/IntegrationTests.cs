using System.Collections.Immutable;
using Microsoft.Extensions.DependencyInjection;
using HotChocolate.Execution;
using HotChocolate.Internal;
using HotChocolate.Resolvers;
using HotChocolate.Tests;

namespace HotChocolate.Types.Pagination;

public class IntegrationTests
{
    [Fact]
    public async Task Simple_StringList_Schema()
    {
        var executor =
            await new ServiceCollection()
                .AddGraphQL()
                .AddQueryType<QueryType>()
                .Services
                .BuildServiceProvider()
                .GetRequestExecutorAsync();

        executor.Schema.Print().MatchSnapshot();
    }

    [Fact]
    public async Task IncludeNodesField_False()
    {
        var executor =
            await new ServiceCollection()
                .AddGraphQL()
                .AddQueryType<QueryType>()
                .ModifyPagingOptions(o => o.IncludeNodesField = false)
                .Services
                .BuildServiceProvider()
                .GetRequestExecutorAsync();

        executor.Schema.Print().MatchSnapshot();
    }

    [Fact]
    public async Task SetPagingOptionsIsStillApplied()
    {
        var executor =
#pragma warning disable CS0618 // Type or member is obsolete
            await new ServiceCollection()
                .AddGraphQL()
                .AddQueryType<QueryType>()
                .SetPagingOptions(new PagingOptions
                {
                    IncludeTotalCount = true
                })
#pragma warning restore CS0618 // Type or member is obsolete
                .Services
                .BuildServiceProvider()
                .GetRequestExecutorAsync();

        executor.Schema.Print().MatchSnapshot();
    }

    [Fact]
    public async Task Attribute_Simple_StringList_Schema()
    {
        var executor =
            await new ServiceCollection()
                .AddGraphQL()
                .AddQueryType<QueryAttr>()
                .Services
                .BuildServiceProvider()
                .GetRequestExecutorAsync();

        executor.Schema.Print().MatchSnapshot();
    }

    [Fact]
    public async Task Simple_StringList_Default_Items()
    {
        var executor =
            await new ServiceCollection()
                .AddGraphQL()
                .AddQueryType<QueryType>()
                .Services
                .BuildServiceProvider()
                .GetRequestExecutorAsync();

        await executor
            .ExecuteAsync(@"
                {
                    letters {
                        edges {
                            node
                            cursor
                        }
                        nodes
                        pageInfo {
                            hasNextPage
                            hasPreviousPage
                            startCursor
                            endCursor
                        }
                    }
                }")
            .MatchSnapshotAsync();
    }

    [Fact]
    public async Task No_Boundaries_Set()
    {
        var executor =
            await new ServiceCollection()
                .AddGraphQL()
                .AddQueryType<QueryType>()
                .ModifyPagingOptions(o => o.RequirePagingBoundaries = true)
                .Services
                .BuildServiceProvider()
                .GetRequestExecutorAsync();

        await executor
            .ExecuteAsync(@"
                {
                    letters {
                        edges {
                            node
                            cursor
                        }
                        nodes
                        pageInfo {
                            hasNextPage
                            hasPreviousPage
                            startCursor
                            endCursor
                        }
                    }
                }")
            .MatchSnapshotAsync();
    }

    [Fact]
    public async Task Attribute_Simple_StringList_Default_Items()
    {
        var executor =
            await new ServiceCollection()
                .AddGraphQL()
                .AddQueryType<QueryAttr>()
                .Services
                .BuildServiceProvider()
                .GetRequestExecutorAsync();

        await executor
            .ExecuteAsync(@"
                {
                    letters {
                        edges {
                            node
                            cursor
                        }
                        nodes
                        pageInfo {
                            hasNextPage
                            hasPreviousPage
                            startCursor
                            endCursor
                        }
                    }
                }")
            .MatchSnapshotAsync();
    }

    [Fact]
    public async Task Simple_StringList_First_2()
    {
        var executor =
            await new ServiceCollection()
                .AddGraphQL()
                .AddQueryType<QueryType>()
                .Services
                .BuildServiceProvider()
                .GetRequestExecutorAsync();

        await executor
            .ExecuteAsync(@"
                {
                    letters(first: 2) {
                        edges {
                            node
                            cursor
                        }
                        nodes
                        pageInfo {
                            hasNextPage
                            hasPreviousPage
                            startCursor
                            endCursor
                        }
                    }
                }")
            .MatchSnapshotAsync();
    }

    [Fact]
    public async Task MinPageSizeReached_First()
    {
        var executor =
            await new ServiceCollection()
                .AddGraphQL()
                .AddQueryType<QueryType>()
                .Services
                .BuildServiceProvider()
                .GetRequestExecutorAsync();

        await executor
            .ExecuteAsync(@"
                {
                    letters(first: -1) {
                        edges {
                            node
                            cursor
                        }
                        nodes
                        pageInfo {
                            hasNextPage
                            hasPreviousPage
                            startCursor
                            endCursor
                        }
                    }
                }")
            .MatchSnapshotAsync();
    }

    [Fact]
    public async Task MaxPageSizeReached_First()
    {
        var executor =
            await new ServiceCollection()
                .AddGraphQL()
                .AddQueryType<QueryType>()
                .ModifyPagingOptions(o => o.MaxPageSize = 2)
                .Services
                .BuildServiceProvider()
                .GetRequestExecutorAsync();

        await executor
            .ExecuteAsync(@"
                {
                    letters(first: 3) {
                        edges {
                            node
                            cursor
                        }
                        nodes
                        pageInfo {
                            hasNextPage
                            hasPreviousPage
                            startCursor
                            endCursor
                        }
                    }
                }")
            .MatchSnapshotAsync();
    }

    [Fact]
    public async Task MinPageSizeReached_Last()
    {
        var executor =
            await new ServiceCollection()
                .AddGraphQL()
                .AddQueryType<QueryType>()
                .Services
                .BuildServiceProvider()
                .GetRequestExecutorAsync();

        await executor
            .ExecuteAsync(@"
                {
                    letters(last: -1) {
                        edges {
                            node
                            cursor
                        }
                        nodes
                        pageInfo {
                            hasNextPage
                            hasPreviousPage
                            startCursor
                            endCursor
                        }
                    }
                }")
            .MatchSnapshotAsync();
    }

    [Fact]
    public async Task MaxPageSizeReached_Last()
    {
        var executor =
            await new ServiceCollection()
                .AddGraphQL()
                .AddQueryType<QueryType>()
                .ModifyPagingOptions(o => o.MaxPageSize = 2)
                .Services
                .BuildServiceProvider()
                .GetRequestExecutorAsync();

        await executor
            .ExecuteAsync(@"
                {
                    letters(last: 3) {
                        edges {
                            node
                            cursor
                        }
                        nodes
                        pageInfo {
                            hasNextPage
                            hasPreviousPage
                            startCursor
                            endCursor
                        }
                    }
                }")
            .MatchSnapshotAsync();
    }

    [Fact]
    public async Task Attribute_Simple_StringList_First_2()
    {
        var executor =
            await new ServiceCollection()
                .AddGraphQL()
                .AddQueryType<QueryAttr>()
                .Services
                .BuildServiceProvider()
                .GetRequestExecutorAsync();

        await executor
            .ExecuteAsync(@"
                {
                    letters(first: 2) {
                        edges {
                            node
                            cursor
                        }
                        nodes
                        pageInfo {
                            hasNextPage
                            hasPreviousPage
                            startCursor
                            endCursor
                        }
                    }
                }")
            .MatchSnapshotAsync();
    }

    [Fact]
    public async Task Simple_StringList_First_2_After()
    {
        var executor =
            await new ServiceCollection()
                .AddGraphQL()
                .AddQueryType<QueryType>()
                .Services
                .BuildServiceProvider()
                .GetRequestExecutorAsync();

        await executor
            .ExecuteAsync(@"
                {
                    letters(first: 2 after: ""MQ=="") {
                        edges {
                            node
                            cursor
                        }
                        nodes
                        pageInfo {
                            hasNextPage
                            hasPreviousPage
                            startCursor
                            endCursor
                        }
                    }
                }")
            .MatchSnapshotAsync();
    }

    [Fact]
    public async Task Attribute_Simple_StringList_First_2_After()
    {
        var executor =
            await new ServiceCollection()
                .AddGraphQL()
                .AddQueryType<QueryAttr>()
                .Services
                .BuildServiceProvider()
                .GetRequestExecutorAsync();

        await executor
            .ExecuteAsync(@"
                {
                    letters(first: 2 after: ""MQ=="") {
                        edges {
                            node
                            cursor
                        }
                        nodes
                        pageInfo {
                            hasNextPage
                            hasPreviousPage
                            startCursor
                            endCursor
                        }
                    }
                }")
            .MatchSnapshotAsync();
    }

    [Fact]
    public async Task Simple_StringList_Global_DefaultItem_2()
    {
        var executor =
            await new ServiceCollection()
                .AddGraphQL()
                .AddQueryType<QueryType>()
                .ModifyPagingOptions(o => o.DefaultPageSize = 2)
                .Services
                .BuildServiceProvider()
                .GetRequestExecutorAsync();

        await executor
            .ExecuteAsync(@"
                {
                    letters {
                        edges {
                            node
                            cursor
                        }
                        nodes
                        pageInfo {
                            hasNextPage
                            hasPreviousPage
                            startCursor
                            endCursor
                        }
                    }
                }")
            .MatchSnapshotAsync();
    }

    [Fact]
    public async Task Attribute_Simple_StringList_Global_DefaultItem_2()
    {
        var executor =
            await new ServiceCollection()
                .AddGraphQL()
                .AddQueryType<QueryAttr>()
                .ModifyPagingOptions(o => o.DefaultPageSize = 2)
                .Services
                .BuildServiceProvider()
                .GetRequestExecutorAsync();

        await executor
            .ExecuteAsync(@"
                {
                    letters {
                        edges {
                            node
                            cursor
                        }
                        nodes
                        pageInfo {
                            hasNextPage
                            hasPreviousPage
                            startCursor
                            endCursor
                        }
                    }
                }")
            .MatchSnapshotAsync();
    }

    [Fact]
    public async Task Schema_Type_Is_Explicitly_Specified()
    {
        var executor =
            await new ServiceCollection()
                .AddGraphQL()
                .AddQueryType<QueryType>()
                .Services
                .BuildServiceProvider()
                .GetRequestExecutorAsync();

        await executor
            .ExecuteAsync(@"
                {
                    explicitType(first: 2) {
                        edges {
                            node
                            cursor
                        }
                        nodes
                        pageInfo {
                            hasNextPage
                            hasPreviousPage
                            startCursor
                            endCursor
                        }
                    }
                }")
            .MatchSnapshotAsync();
    }

    [Fact]
    public async Task Attribute_Schema_Type_Is_Explicitly_Specified()
    {
        var executor =
            await new ServiceCollection()
                .AddGraphQL()
                .AddQueryType<QueryAttr>()
                .Services
                .BuildServiceProvider()
                .GetRequestExecutorAsync();

        await executor
            .ExecuteAsync(@"
                {
                    explicitType(first: 2) {
                        edges {
                            node
                            cursor
                        }
                        nodes
                        pageInfo {
                            hasNextPage
                            hasPreviousPage
                            startCursor
                            endCursor
                        }
                    }
                }")
            .MatchSnapshotAsync();
    }

    [Fact]
    public async Task Nested_List_With_Field_Settings()
    {
        var executor =
            await new ServiceCollection()
                .AddGraphQL()
                .AddQueryType<QueryType>()
                .Services
                .BuildServiceProvider()
                .GetRequestExecutorAsync();

        await executor
            .ExecuteAsync(@"
                {
                    nestedObjectList {
                        edges {
                            node {
                                bar
                            }
                            cursor
                        }
                        nodes {
                            bar
                        }
                        pageInfo {
                            hasNextPage
                            hasPreviousPage
                            startCursor
                            endCursor
                        }
                        totalCount
                    }
                }")
            .MatchSnapshotAsync();
    }

    [Fact]
    public async Task Executable_With_Field_Settings()
    {
        var executor =
            await new ServiceCollection()
                .AddGraphQL()
                .AddQueryType<ExecutableQueryType>()
                .Services
                .BuildServiceProvider()
                .GetRequestExecutorAsync();

        await executor
            .ExecuteAsync(
                """
                {
                  fooExecutable {
                      edges {
                          node {
                              bar
                          }
                          cursor
                      }
                      nodes {
                          bar
                      }
                      pageInfo {
                          hasNextPage
                          hasPreviousPage
                          startCursor
                          endCursor
                      }
                      totalCount
                  }
                }
                """)
            .MatchSnapshotAsync();
    }

    [Fact]
    public async Task Attribute_Nested_List_With_Field_Settings()
    {
        var executor =
            await new ServiceCollection()
                .AddGraphQL()
                .AddQueryType<QueryAttr>()
                .Services
                .BuildServiceProvider()
                .GetRequestExecutorAsync();

        await executor
            .ExecuteAsync(@"
                {
                    nestedObjectList {
                        edges {
                            node {
                                bar
                            }
                            cursor
                        }
                        nodes {
                            bar
                        }
                        pageInfo {
                            hasNextPage
                            hasPreviousPage
                            startCursor
                            endCursor
                        }
                        totalCount
                    }
                }")
            .MatchSnapshotAsync();
    }

    [Fact]
    public async Task Nested_List_With_Field_Settings_Skip_2()
    {
        var executor =
            await new ServiceCollection()
                .AddGraphQL()
                .AddQueryType<QueryType>()
                .Services
                .BuildServiceProvider()
                .GetRequestExecutorAsync();

        await executor
            .ExecuteAsync(@"
                {
                    nestedObjectList(first: 2) {
                        edges {
                            node {
                                bar
                            }
                            cursor
                        }
                        nodes {
                            bar
                        }
                        pageInfo {
                            hasNextPage
                            hasPreviousPage
                            startCursor
                            endCursor
                        }
                        totalCount
                    }
                }")
            .MatchSnapshotAsync();
    }

    [Fact]
    public async Task Attribute_Nested_List_With_Field_Settings_Skip_2()
    {
        var executor =
            await new ServiceCollection()
                .AddGraphQL()
                .AddQueryType<QueryAttr>()
                .Services
                .BuildServiceProvider()
                .GetRequestExecutorAsync();

        await executor
            .ExecuteAsync(@"
                {
                    nestedObjectList(first: 2) {
                        edges {
                            node {
                                bar
                            }
                            cursor
                        }
                        nodes {
                            bar
                        }
                        pageInfo {
                            hasNextPage
                            hasPreviousPage
                            startCursor
                            endCursor
                        }
                        totalCount
                    }
                }")
            .MatchSnapshotAsync();
    }

    [Fact]
    public async Task ExtendedTypeRef_Default_Items()
    {
        var executor =
            await new ServiceCollection()
                .AddGraphQL()
                .AddQueryType<QueryType>()
                .Services
                .BuildServiceProvider()
                .GetRequestExecutorAsync();

        await executor
            .ExecuteAsync(@"
                {
                    extendedTypeRef {
                        nodes
                    }
                }")
            .MatchSnapshotAsync();
    }

    [Fact]
    public async Task ExtendedTypeRefNested_Default_Items()
    {
        var executor =
            await new ServiceCollection()
                .AddGraphQL()
                .AddQueryType<QueryType>()
                .Services
                .BuildServiceProvider()
                .GetRequestExecutorAsync();

        await executor
            .ExecuteAsync(@"
                {
                    extendedTypeRefNested {
                        nodes
                    }
                }")
            .MatchSnapshotAsync();
    }

    [Fact]
    public async Task Interface_With_Paging_Field()
    {
        var schema =
            await new ServiceCollection()
                .AddGraphQL()
                .AddQueryType<QueryAttr>()
                .AddInterfaceType<ISome>(d => d
                    .Field(t => t.ExplicitType())
                    .UsePaging())
                .ModifyOptions(o =>
                {
                    o.RemoveUnreachableTypes = false;
                    o.StrictValidation = false;
                })
                .Services
                .BuildServiceProvider()
                .GetSchemaAsync();

        schema.Print().MatchSnapshot();
    }

    [Fact]
    public async Task Attribute_Interface_With_Paging_Field()
    {
        var schema =
            await new ServiceCollection()
                .AddGraphQL()
                .AddQueryType<QueryAttr>()
                .AddInterfaceType<ISome2>()
                .ModifyOptions(o =>
                {
                    o.RemoveUnreachableTypes = false;
                    o.StrictValidation = false;
                })
                .Services
                .BuildServiceProvider()
                .GetSchemaAsync();

        schema.Print().MatchSnapshot();
    }

    [Fact]
    public async Task Deactivate_BackwardPagination()
    {
        var executor =
            await new ServiceCollection()
                .AddGraphQL()
                .AddQueryType<QueryType>()
                .ModifyPagingOptions(o => o.AllowBackwardPagination = false)
                .Services
                .BuildServiceProvider()
                .GetRequestExecutorAsync();

        executor.Schema.Print().MatchSnapshot();
    }

    [Fact]
    public async Task Deactivate_BackwardPagination_Interface()
    {
        var schema =
            await new ServiceCollection()
                .AddGraphQL()
                .AddQueryType<QueryAttr>()
                .ModifyPagingOptions(o => o.AllowBackwardPagination = false)
                .AddInterfaceType<ISome>(d => d.Field(t => t.ExplicitType()).UsePaging())
                .Services
                .BuildServiceProvider()
                .GetSchemaAsync();

        schema.Print().MatchSnapshot();
    }

    [Fact]
    public async Task Infer_ConnectionName_From_Field()
    {
        var schema =
            await new ServiceCollection()
                .AddGraphQL()
                .AddQueryType<InferConnectionNameFromFieldType>()
                .Services
                .BuildServiceProvider()
                .GetSchemaAsync();

        schema.Print().MatchSnapshot();
    }

    [Fact]
    public async Task Explicit_ConnectionName()
    {
        var schema =
            await new ServiceCollection()
                .AddGraphQL()
                .AddQueryType<ExplicitConnectionName>()
                .Services
                .BuildServiceProvider()
                .GetSchemaAsync();

        schema.Print().MatchSnapshot();
    }

    [Fact]
    public async Task SelectProviderByName()
    {
        var executor =
            await new ServiceCollection()
                .AddGraphQL()
                .AddQueryType<ProviderByName>()
                .AddCursorPagingProvider<DummyProvider>(providerName: "Abc")
                .ModifyPagingOptions(o => o.InferConnectionNameFromField = false)
                .Services
                .BuildServiceProvider()
                .GetRequestExecutorAsync();

        await executor
            .ExecuteAsync(@"
                {
                    abc {
                        nodes
                    }
                }")
            .MatchSnapshotAsync();
    }

    [Fact]
    public async Task SelectDefaultProvider()
    {
        var executor =
            await new ServiceCollection()
                .AddGraphQL()
                .AddQueryType<ProviderByName>()
                .AddCursorPagingProvider<DummyProvider>()
                .AddCursorPagingProvider<Dummy2Provider>(defaultProvider: true)
                .Services
                .BuildServiceProvider()
                .GetRequestExecutorAsync();

        await executor
            .ExecuteAsync(@"
                {
                    abc {
                        nodes
                    }
                }")
            .MatchSnapshotAsync();
    }

    [Fact]
    public async Task Ensure_That_Explicit_Backward_Paging_Fields_Work()
    {
        await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType<BackwardQuery>()
            .BuildSchemaAsync()
            .MatchSnapshotAsync();
    }

    [Fact]
    public async Task Ensure_That_Explicit_Backward_Paging_Fields_Work_Execute()
    {
        await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType<BackwardQuery>()
            .ExecuteRequestAsync("{ foos { nodes } }")
            .MatchSnapshotAsync();
    }

    [Fact]
    public async Task TotalCountWithCustomConnection()
    {
        // arrange
        var executor = await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType<CustomConnectionQuery>()
            .BuildRequestExecutorAsync();

        // act
        const string query = @"
            {
                foos {
                    totalCount
                }
            }
            ";

        var result = await executor.ExecuteAsync(query);

        // assert
        result.ToJson().MatchSnapshot();
    }

    [Fact]
    public async Task Invalid_After_Index_Cursor()
    {
        var executor =
            await new ServiceCollection()
                .AddGraphQL()
                .AddQueryType<QueryType>()
                .Services
                .BuildServiceProvider()
                .GetRequestExecutorAsync();

        var result = await executor.ExecuteAsync(
            """
            {
              letters(first: 2 after: "INVALID") {
                  edges {
                      cursor
                  }
              }
            }
            """);

        result.MatchSnapshot();
    }

    [Fact]
    public async Task Invalid_Before_Index_Cursor()
    {
        var executor =
            await new ServiceCollection()
                .AddGraphQL()
                .AddQueryType<QueryType>()
                .Services
                .BuildServiceProvider()
                .GetRequestExecutorAsync();

        var result = await executor.ExecuteAsync(
            """
            {
              letters(first: 2 before: "INVALID") {
                  edges {
                      cursor
                  }
              }
            }
            """);

        result.MatchSnapshot();
    }

    [Fact]
    public async Task Simple_EnumerableValueType_ReturnsError()
    {
        // arrange
        var executor = await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType<QueryEnumerableValueType>()
            .BuildRequestExecutorAsync();

        // act
        const string query = "{ test { nodes } }";

        var result = await executor.ExecuteAsync(query);
        var errors = result.ExpectOperationResult().Errors;

        // assert
        Assert.NotNull(errors);
        var error = Assert.Single(errors);
        Assert.Equal("Cannot handle the specified data source.", error.Message);
    }

    public class QueryType : ObjectType<Query>
    {
        protected override void Configure(IObjectTypeDescriptor<Query> descriptor)
        {
            descriptor
                .Field(t => t.Letters)
                .UsePaging();

            descriptor
                .Field("explicitType")
                .ResolveWith<Query>(t => t.Letters)
                .UsePaging<NonNullType<StringType>>();

            descriptor
                .Field(t => t.Foos())
                .Name("nestedObjectList")
                .UsePaging(
                    options: new PagingOptions
                    {
                        MaxPageSize = 2,
                        IncludeTotalCount = true,
                    });

            descriptor
                .Field("extendedTypeRef")
                .Resolve(_ => new List<string>(["one", "two"]))
                .UsePaging();

            descriptor
                .Field("extendedTypeRefNested")
                .Resolve(_ => new List<List<string>>([["one", "two"]]))
                .UsePaging();
        }
    }

    public class ExecutableQueryType : ObjectType<ExecutableQuery>
    {
        protected override void Configure(IObjectTypeDescriptor<ExecutableQuery> descriptor)
        {
            descriptor
                .Field(t => t.FoosExecutable())
                .Name("fooExecutable")
                .UsePaging(
                    options: new PagingOptions
                    {
                        MaxPageSize = 2,
                        IncludeTotalCount = true,
                    });
        }
    }

    public class Query
    {
        public string[] Letters =>
        [
            "a",
            "b",
            "c",
            "d",
            "e",
            "f",
            "g",
            "h",
            "i",
            "j",
            "k",
            "l",
        ];

        public List<List<Foo>> Foos() =>
        [
            [new() { Bar = "a", },],
            [new() { Bar = "b", }, new() { Bar = "c", },],
            [new() { Bar = "d", },],
            [new() { Bar = "e", },],
            [new() { Bar = "f", },],
        ];
    }

    public class ExecutableQuery
    {
        public IExecutable<Foo> FoosExecutable()
            => Executable.From(
                new List<Foo>
                {
                    new() { Bar = "a", },
                    new() { Bar = "b", },
                    new() { Bar = "c", } ,
                    new() { Bar = "d", },
                    new() { Bar = "e", },
                    new() { Bar = "f", },
                }.AsQueryable());
    }

    public class Foo
    {
        public string Bar { get; set; } = default!;
    }

    public class QueryAttr
    {
        [UsePaging]
        public string[] Letters =>
        [
            "a",
            "b",
            "c",
            "d",
            "e",
            "f",
            "g",
            "h",
            "i",
            "j",
            "k",
            "l",
        ];

        [UsePaging(typeof(NonNullType<StringType>))]
        public string[] ExplicitType => Letters;

        [GraphQLName("nestedObjectList")]
        [UsePaging(
            MaxPageSize = 2,
            IncludeTotalCount = true)]
        public List<List<Foo>> Foos() =>
        [
            [new() { Bar = "a", },],
            [new() { Bar = "b", }, new() { Bar = "c", },],
            [new() { Bar = "d", },],
            [new() { Bar = "e", },],
            [new() { Bar = "f", },],
        ];
    }

    public interface ISome
    {
        public string[] ExplicitType();
    }

    public interface ISome2
    {
        [UsePaging(typeof(NonNullType<StringType>))]
        public string[] ExplicitType();
    }

    public class InferConnectionNameFromFieldType : ObjectType<InferConnectionNameFromField>
    {
        protected override void Configure(
            IObjectTypeDescriptor<InferConnectionNameFromField> descriptor)
        {
            descriptor
                .Field(t => t.Names())
                .UsePaging(options: new() { InferConnectionNameFromField = true, });
        }
    }

    public class InferConnectionNameFromField
    {
        public string[] Names() => ["a", "b",];
    }

    public class ExplicitConnectionName
    {
        [UsePaging(ConnectionName = "Connection1")]
        public string[] Abc => throw new NotImplementedException();

        [UsePaging(ConnectionName = "Connection2")]
        public string[] Def => throw new NotImplementedException();

        [UsePaging]
        public string[] Ghi => throw new NotImplementedException();
    }

    public class ProviderByName
    {
        [UsePaging(ProviderName = "Abc")]
        public string[] Abc => [];
    }

    public class DummyProvider : CursorPagingProvider
    {
        public override bool CanHandle(IExtendedType source) => false;

        protected override CursorPagingHandler CreateHandler(
            IExtendedType source,
            PagingOptions options)
            => new DummyHandler(options);
    }

    public class DummyHandler : CursorPagingHandler
    {
        public DummyHandler(PagingOptions options) : base(options)
        {
        }

        protected override ValueTask<Connection> SliceAsync(
            IResolverContext context,
            object source,
            CursorPagingArguments arguments)
            => new(new Connection(
                new[] { new Edge<string>("a", "b"), },
                new ConnectionPageInfo(false, false, null, null), 1));
    }

    public class Dummy2Provider : CursorPagingProvider
    {
        public override bool CanHandle(IExtendedType source) => false;

        protected override CursorPagingHandler CreateHandler(
            IExtendedType source,
            PagingOptions options)
            => new Dummy2Handler(options);
    }

    public class Dummy2Handler(PagingOptions options) : CursorPagingHandler(options)
    {
        protected override ValueTask<Connection> SliceAsync(
            IResolverContext context,
            object source,
            CursorPagingArguments arguments)
            => new(new Connection(
                new[] { new Edge<string>("d", "e"), },
                new ConnectionPageInfo(false, false, null, null), 1));
    }

    public class BackwardQuery
    {
        [UsePaging(AllowBackwardPagination = false)]
        public Connection<string> GetFoos(int? first, string? after)
            => new Connection<string>(
                new[] { new Edge<string>("abc", "def"), },
                new ConnectionPageInfo(false, false, null, null), 1);
    }

    public class CustomConnectionQuery
    {
        [UsePaging(IncludeTotalCount = true)]
        public Connection<string> GetFoos(int? first, string? after)
            => new Connection<string>(
                new[] {new Edge<string>("abc", "def"), new Edge<string>("abc", "def"), },
                new ConnectionPageInfo(false, false, null, null), 2);
    }

    public class QueryEnumerableValueType
    {
        [UsePaging]
        public ImmutableArray<int> Test()
        {
            return ImmutableArray<int>.Empty;
        }
    }
}
