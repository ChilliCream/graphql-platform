using System.Collections;
using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using HotChocolate.Execution;
using HotChocolate.Tests;
using Microsoft.Extensions.DependencyInjection;

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

        (await executor
            .ExecuteAsync(@"
                {
                    letters {
                        items
                        pageInfo {
                            hasNextPage
                            hasPreviousPage
                        }
                    }
                }"))
            .MatchSnapshot();
    }

    [Fact]
    public async Task No_Paging_Boundaries()
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
                        items
                        pageInfo {
                            hasNextPage
                            hasPreviousPage
                        }
                    }
                }")
            .MatchSnapshotAsync();
    }

    [Fact]
    public async Task MaxPageSizeReached()
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
            .ExecuteAsync($@"
                {{
                    letters(take: {51}) {{
                        items
                        pageInfo {{
                            hasNextPage
                            hasPreviousPage
                        }}
                    }}
                }}")
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

        (await executor
            .ExecuteAsync(@"
                {
                    letters {
                        items
                        pageInfo {
                            hasNextPage
                            hasPreviousPage
                        }
                    }
                }"))
            .MatchSnapshot();
    }

    [Fact]
    public async Task Simple_StringList_Take_2()
    {
        var executor =
            await new ServiceCollection()
                .AddGraphQL()
                .AddQueryType<QueryType>()
                .Services
                .BuildServiceProvider()
                .GetRequestExecutorAsync();

        (await executor
            .ExecuteAsync(@"
                {
                    letters(take: 2) {
                        items
                        pageInfo {
                            hasNextPage
                            hasPreviousPage
                        }
                    }
                }"))
            .MatchSnapshot();
    }

    [Fact]
    public async Task Attribute_Simple_StringList_Take_2()
    {
        var executor =
            await new ServiceCollection()
                .AddGraphQL()
                .AddQueryType<QueryAttr>()
                .Services
                .BuildServiceProvider()
                .GetRequestExecutorAsync();

        (await executor
            .ExecuteAsync(@"
                {
                    letters(take: 2) {
                        items
                        pageInfo {
                            hasNextPage
                            hasPreviousPage
                        }
                    }
                }"))
            .MatchSnapshot();
    }

    [Fact]
    public async Task Simple_StringList_Take_2_Skip_2()
    {
        var executor =
            await new ServiceCollection()
                .AddGraphQL()
                .AddQueryType<QueryType>()
                .Services
                .BuildServiceProvider()
                .GetRequestExecutorAsync();

        (await executor
            .ExecuteAsync(@"
                {
                    letters(take: 2 skip: 2) {
                        items
                        pageInfo {
                            hasNextPage
                            hasPreviousPage
                        }
                    }
                }"))
            .MatchSnapshot();
    }

    [Fact]
    public async Task Attribute_Simple_StringList_Take_2_Skip_2()
    {
        var executor =
            await new ServiceCollection()
                .AddGraphQL()
                .AddQueryType<QueryAttr>()
                .Services
                .BuildServiceProvider()
                .GetRequestExecutorAsync();

        (await executor
            .ExecuteAsync(@"
                {
                    letters(take: 2 skip: 2) {
                        items
                        pageInfo {
                            hasNextPage
                            hasPreviousPage
                        }
                    }
                }"))
            .MatchSnapshot();
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

        (await executor
            .ExecuteAsync(@"
                {
                    letters {
                        items
                        pageInfo {
                            hasNextPage
                            hasPreviousPage
                        }
                    }
                }"))
            .MatchSnapshot();
    }

    [Fact]
    public async Task Simple_StringList_Global_DefaultItem_50_Page_Larger_Than_Data_List()
    {
        var executor =
            await new ServiceCollection()
                .AddGraphQL()
                .AddQueryType<QueryType>()
                .ModifyPagingOptions(o => o.DefaultPageSize = 50)
                .Services
                .BuildServiceProvider()
                .GetRequestExecutorAsync();

        (await executor
            .ExecuteAsync(@"
                {
                    letters {
                        items
                        pageInfo {
                            hasNextPage
                            hasPreviousPage
                        }
                    }
                }"))
            .MatchSnapshot();
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

        (await executor
            .ExecuteAsync(@"
                {
                    letters(take: 2) {
                        items
                        pageInfo {
                            hasNextPage
                            hasPreviousPage
                        }
                    }
                }"))
            .MatchSnapshot();
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

        (await executor
            .ExecuteAsync(@"
                {
                    explicitType(take: 2) {
                        items
                        pageInfo {
                            hasNextPage
                            hasPreviousPage
                        }
                    }
                }"))
            .MatchSnapshot();
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

        (await executor
            .ExecuteAsync(@"
                {
                    explicitType(take: 2) {
                        items
                        pageInfo {
                            hasNextPage
                            hasPreviousPage
                        }
                    }
                }"))
            .MatchSnapshot();
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

        (await executor
            .ExecuteAsync(@"
                {
                    nestedObjectList {
                        items {
                            bar
                        }
                        pageInfo {
                            hasNextPage
                            hasPreviousPage
                        }
                        totalCount
                    }
                }"))
            .MatchSnapshot();
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

        (await executor
            .ExecuteAsync(@"
                {
                    fooExecutable {
                        items {
                            bar
                        }
                        pageInfo {
                            hasNextPage
                            hasPreviousPage
                        }
                        totalCount
                    }
                }"))
            .MatchSnapshot();
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

        (await executor
            .ExecuteAsync(@"
                {
                    nestedObjectList {
                        items {
                            bar
                        }
                        pageInfo {
                            hasNextPage
                            hasPreviousPage
                        }
                        totalCount
                    }
                }")).MatchSnapshot();
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

        (await executor
            .ExecuteAsync(@"
                {
                    nestedObjectList(skip: 2) {
                        items {
                            bar
                        }
                        pageInfo {
                            hasNextPage
                            hasPreviousPage
                        }
                        totalCount
                    }
                }"))
            .MatchSnapshot();
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

        (await executor
            .ExecuteAsync(@"
                {
                    nestedObjectList(skip: 2) {
                        items {
                            bar
                        }
                        pageInfo {
                            hasNextPage
                            hasPreviousPage
                        }
                        totalCount
                    }
                }"))
            .MatchSnapshot();
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

        (await executor
            .ExecuteAsync(@"
                {
                    extendedTypeRef {
                        items
                    }
                }"))
            .MatchSnapshot();
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

        (await executor
            .ExecuteAsync(@"
                {
                    extendedTypeRefNested {
                        items
                    }
                }"))
            .MatchSnapshot();
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
                    .UseOffsetPaging(
                        options: new PagingOptions { InferCollectionSegmentNameFromField = false, }))
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
    public async Task FluentPagingTests()
    {
        var snapshot = new Snapshot();

        var executor =
            await new ServiceCollection()
                .AddGraphQL()
                .AddQueryType<FluentPaging>()
                .Services
                .BuildServiceProvider()
                .GetRequestExecutorAsync();

        snapshot.Add(
            await executor.ExecuteAsync(
                """
                {
                    items {
                        items
                    }
                }
                """));

        await snapshot.MatchAsync();
    }

    [Fact]
    public async Task TotalCountWithCustomCollectionSegment()
    {
        // arrange
        var executor = await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType<CustomCollectionSegmentQuery>()
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
    public async Task Simple_EnumerableValueType_ReturnsError()
    {
        // arrange
        var executor = await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType<QueryEnumerableValueType>()
            .BuildRequestExecutorAsync();

        // act
        const string query = "{ test { items } }";

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
                .UseOffsetPaging();

            descriptor
                .Field("explicitType")
                .ResolveWith<Query>(t => t.Letters)
                .UseOffsetPaging<NonNullType<StringType>>();

            descriptor
                .Field(t => t.Foos())
                .Name("nestedObjectList")
                .UseOffsetPaging(
                    options: new PagingOptions { MaxPageSize = 2, IncludeTotalCount = true, });

            descriptor
                .Field("extendedTypeRef")
                .Resolve(_ => new List<string>(["one", "two"]))
                .UseOffsetPaging();

            descriptor
                .Field("extendedTypeRefNested")
                .Resolve(_ => new List<List<string>>([["one", "two"]]))
                .UseOffsetPaging();
        }
    }

    public class ExecutableQueryType : ObjectType<ExecutableQuery>
    {
        protected override void Configure(IObjectTypeDescriptor<ExecutableQuery> descriptor)
        {
            descriptor
                .Field(t => t.FoosExecutable())
                .Name("fooExecutable")
                .UseOffsetPaging(
                    options: new PagingOptions { MaxPageSize = 2, IncludeTotalCount = true, });
        }
    }

    public class Query
    {
        public string[] Letters =>
        [
            "a", "b", "c", "d", "e", "f", "g", "h", "i", "j", "k", "l",
        ];

        public List<List<Foo>> Foos() =>
        [
            [new Foo { Bar = "a", },],
            [new Foo { Bar = "b", }, new Foo { Bar = "c", },],
            [new Foo { Bar = "d", },],
            [new Foo { Bar = "e", },],
            [new Foo { Bar = "f", },],
        ];
    }

    public class ExecutableQuery
    {
        public IExecutable<Foo> FoosExecutable() => new MockExecutable<Foo>(
            new List<Foo>
            {
                new Foo { Bar = "a", },
                new Foo { Bar = "b", },
                new Foo { Bar = "c", },
                new Foo { Bar = "d", },
                new Foo { Bar = "e", },
                new Foo { Bar = "f", },
            }.AsQueryable());
    }

    public class Foo
    {
        public string Bar { get; set; } = default!;
    }

    public class FluentPaging
    {
        [UseOffsetPaging(ProviderName = "Items")]
        public async Task<CollectionSegment<string>> GetItems(
            int? skip,
            int? take,
            CancellationToken cancellationToken)
            => await new[] { "a", "b", "c", "d", }
                .AsQueryable()
                .ApplyOffsetPaginationAsync(skip, take, cancellationToken);
    }

    public class QueryAttr
    {
        [UseOffsetPaging]
        public string[] Letters =>
        [
            "a", "b", "c", "d", "e", "f", "g", "h", "i", "j", "k", "l",
        ];

        [UseOffsetPaging(typeof(NonNullType<StringType>))]
        public string[] ExplicitType => Letters;

        [GraphQLName("nestedObjectList")]
        [UseOffsetPaging(
            MaxPageSize = 2,
            IncludeTotalCount = true)]
        public List<List<Foo>> Foos() =>
        [
            [new Foo { Bar = "a", },],
            [new Foo { Bar = "b", }, new Foo { Bar = "c", },],
            [new Foo { Bar = "d", },],
            [new Foo { Bar = "e", },],
            [new Foo { Bar = "f", },],
        ];
    }

    public interface ISome
    {
        public string[] ExplicitType();
    }

    public interface ISome2
    {
        [UseOffsetPaging(typeof(NonNullType<StringType>))]
        public string[] ExplicitType();
    }

    public class QueryEnumerableValueType
    {
        [UseOffsetPaging]
        public ImmutableArray<int> Test()
        {
            return ImmutableArray<int>.Empty;
        }
    }
}

public class MockExecutable<T>(IQueryable<T> source) : IExecutable<T>
{
    public object Source => source;

    ValueTask<IList> IExecutable.ToListAsync(CancellationToken cancellationToken)
        => new(source.ToList());

    public ValueTask<List<T>> ToListAsync(CancellationToken cancellationToken)
        => new(source.ToList());

    public async IAsyncEnumerable<T> ToAsyncEnumerable(
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var queryable = await new ValueTask<IQueryable<T>>(source);

        foreach (var item in queryable)
        {
            yield return item;
        }
    }

    async IAsyncEnumerable<object?> IExecutable.ToAsyncEnumerable(
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var queryable = await new ValueTask<IQueryable<T>>(source);

        foreach (var item in queryable)
        {
            yield return item;
        }
    }

    ValueTask<object?> IExecutable.FirstOrDefaultAsync(CancellationToken cancellationToken)
        => new(source.FirstOrDefault());

    public ValueTask<T?> FirstOrDefaultAsync(CancellationToken cancellationToken)
        => new(source.FirstOrDefault());

    ValueTask<object?> IExecutable.SingleOrDefaultAsync(CancellationToken cancellationToken)
        => new(source.SingleOrDefault());

    public ValueTask<int> CountAsync(CancellationToken cancellationToken = default)
        => new(source.Count());

    public ValueTask<T?> SingleOrDefaultAsync(CancellationToken cancellationToken)
        => new(source.SingleOrDefault());

    public string Print()
        => source.ToString()!;
}

public class CustomCollectionSegmentQuery
{
    [UseOffsetPaging(IncludeTotalCount = true)]
    public CollectionSegment<string> GetFoos(int? first, string? after)
        => new(new[] { "asd", "asd2", }, new CollectionSegmentInfo(false, false), 2);
}
