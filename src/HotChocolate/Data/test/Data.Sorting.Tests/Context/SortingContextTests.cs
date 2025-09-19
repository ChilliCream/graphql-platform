using System.Collections.Immutable;
using HotChocolate.Data.Sorting.Expressions;
using HotChocolate.Execution;
using HotChocolate.Types;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Data.Sorting;

public class SortingContextTests
{
    [Fact]
    public async Task GetFields_Should_ReturnScalarField()
    {
        // arrange
        ISortingContext? context = null;
        var executor = await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType(x => x
                .Name("Query")
                .Field("test")
                .Type<ListType<ObjectType<Book>>>()
                .UseSorting()
                .Resolve(x =>
                {
                    context = x.GetSortingContext();
                    return Array.Empty<Book>();
                }))
            .AddSorting()
            .BuildRequestExecutorAsync();

        // act
        const string query = @"
            {
                test(order: { title: DESC }) {
                    title
                }
            }
        ";

        await executor.ExecuteAsync(query);

        // assert
        Assert.NotNull(context);
        var field = Assert.Single(Assert.Single(context!.GetFields()));
        var operation = Assert.IsType<SortingValue>(field.Value).Value;
        Assert.Equal("title", field.Field.Name);
        Assert.Equal("DESC", operation);
    }

    [Fact]
    public async Task When_Sorting_Is_Empty_IsDefined_Should_Be_False()
    {
        // arrange
        ISortingContext? context = null;
        var executor = await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType(x => x
                .Name("Query")
                .Field("test")
                .Type<ListType<ObjectType<Book>>>()
                .UseSorting()
                .Resolve(x =>
                {
                    context = x.GetSortingContext();
                    return Array.Empty<Book>();
                }))
            .AddSorting()
            .BuildRequestExecutorAsync();

        // act
        const string query =
            """
            {
              test {
                title
              }
            }
            """;

        await executor.ExecuteAsync(query);

        // assert
        Assert.NotNull(context);
        Assert.False(context!.IsDefined);
    }

    [Fact]
    public async Task When_Sorting_Is_Set_IsDefined_Should_Be_True()
    {
        // arrange
        ISortingContext? context = null;
        var executor = await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType(x => x
                .Name("Query")
                .Field("test")
                .Type<ListType<ObjectType<Book>>>()
                .UseSorting()
                .Resolve(x =>
                {
                    context = x.GetSortingContext();
                    return Array.Empty<Book>();
                }))
            .AddSorting()
            .BuildRequestExecutorAsync();

        // act
        const string query =
            """
            {
              test(order: { title: DESC }) {
                title
              }
            }
            """;

        await executor.ExecuteAsync(query);

        // assert
        Assert.NotNull(context);
        Assert.True(context!.IsDefined);
    }

    [Fact]
    public async Task GetFields_Should_ReturnMultipleScalarField()
    {
        // arrange
        ISortingContext? context = null;
        var executor = await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType(x => x
                .Name("Query")
                .Field("test")
                .Type<ListType<ObjectType<Book>>>()
                .UseSorting()
                .Resolve(x =>
                {
                    context = x.GetSortingContext();
                    return Array.Empty<Book>();
                }))
            .AddSorting()
            .BuildRequestExecutorAsync();

        // act
        const string query = @"
            {
                test(order: [{ title: DESC }, { pages: DESC }]) {
                    title
                }
            }
        ";

        await executor.ExecuteAsync(query);

        // assert
        Assert.NotNull(context);
        Assert.Equal(2, context!.GetFields().Count);
        var field = Assert.Single(context!.GetFields()[0]);
        var operation = Assert.IsType<SortingValue>(field.Value).Value;
        Assert.Equal("title", field.Field.Name);
        Assert.Equal("DESC", operation);
    }

    [Fact]
    public async Task GetFields_Should_ReturnDeepScalarField()
    {
        // arrange
        ISortingContext? context = null;
        var executor = await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType(x => x
                .Name("Query")
                .Field("test")
                .Type<ListType<ObjectType<Book>>>()
                .UseSorting()
                .Resolve(x =>
                {
                    context = x.GetSortingContext();
                    return Array.Empty<Book>();
                }))
            .AddSorting()
            .BuildRequestExecutorAsync();

        // act
        const string query = @"
            {
                test(order: { author: { name: DESC }}) {
                    title
                }
            }
        ";

        await executor.ExecuteAsync(query);

        // assert
        Assert.NotNull(context);
        var field = Assert.Single(Assert.Single(context!.GetFields()));
        var name =
            Assert.Single(Assert.IsType<SortingInfo>(field.Value).GetFields());
        var operation = Assert.IsType<SortingValue>(name.Value).Value;
        Assert.Equal("author", field.Field.Name);
        Assert.Equal("name", name.Field.Name);
        Assert.Equal("DESC", operation);
    }

    [Fact]
    public async Task Handled_Should_EnableSortingExecution()
    {
        IImmutableDictionary<string, object?>? localContextData = null;
        // arrange
        var executor = await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType(x => x
                .Name("Query")
                .Field("test")
                .Type<ListType<ObjectType<Book>>>()
                .UseSorting()
                .Resolve(x =>
                {
                    x.GetSortingContext()?.Handled(false);
                    localContextData = x.LocalContextData.Add("foo", true);

                    return Array.Empty<Book>();
                }))
            .AddSorting()
            .BuildRequestExecutorAsync();

        // act
        const string query = @"
            {
                test(order: { title: DESC }) {
                    title
                }
            }
        ";

        await executor.ExecuteAsync(query);

        // assert
        Assert.NotNull(localContextData);
        Assert.False(localContextData!.ContainsKey(QueryableSortProvider.SkipSortingKey));
    }

    [Fact]
    public async Task Handled_Should_DisableSortingExecutionByDefault()
    {
        IImmutableDictionary<string, object?>? localContextData = null;
        // arrange
        var executor = await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType(x => x
                .Name("Query")
                .Field("test")
                .Type<ListType<ObjectType<Book>>>()
                .UseSorting()
                .Resolve(x =>
                {
                    x.GetSortingContext();
                    localContextData = x.LocalContextData.Add("foo", true);

                    return Array.Empty<Book>();
                }))
            .AddSorting()
            .BuildRequestExecutorAsync();

        // act
        const string query = @"
            {
                test(order: { title: DESC }) {
                    title
                }
            }
        ";

        await executor.ExecuteAsync(query);

        // assert
        Assert.NotNull(localContextData);
        Assert.True(localContextData!.ContainsKey(QueryableSortProvider.SkipSortingKey));
    }

    [Fact]
    public async Task GetSortingContext_ReturnNullWhenNoSorting()
    {
        var obj = new object();

        // arrange
        var executor = await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType(x => x
                .Name("Query")
                .Field("test")
                .Type<ListType<ObjectType<Book>>>()
                .Resolve(x =>
                {
                    obj = x.GetSortingContext();
                    return Array.Empty<Book>();
                }))
            .AddSorting()
            .BuildRequestExecutorAsync();

        // act
        const string query = @"
            {
                test {
                    title
                }
            }
        ";

        await executor.ExecuteAsync(query);

        // assert
        Assert.Null(obj);
    }

    [Fact]
    public async Task SortingContext_Should_SerializeAllOperations()
    {
        // arrange
        ISortingContext? context = null;
        var executor = await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType(x => x
                .Name("Query")
                .Field("test")
                .Type<ListType<ObjectType<Book>>>()
                .UseSorting()
                .Resolve(x =>
                {
                    context = x.GetSortingContext();
                    return Array.Empty<Book>();
                }))
            .AddSorting()
            .BuildRequestExecutorAsync();

        // act
        const string query = @"
            {
                test(order: [{ title: DESC }, { author: { name: DESC } }]) {
                    title
                }
            }
        ";

        await executor.ExecuteAsync(query);

        // assert
        Assert.NotNull(context);
        context!.ToList().MatchSnapshot();
    }

    [Fact]
    public async Task SortingContext_Should_NotFail_When_SortingArgumentIsNotProvided()
    {
        // arrange
        ISortingContext? context = null;
        var executor = await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType(x => x
                .Name("Query")
                .Field("test")
                .Type<ListType<ObjectType<Book>>>()
                .UseSorting()
                .Resolve(x =>
                {
                    context = x.GetSortingContext();
                    return Array.Empty<Book>();
                }))
            .AddSorting()
            .BuildRequestExecutorAsync();

        // act
        const string query = @"
            {
                test {
                    title
                }
            }
        ";

        await executor.ExecuteAsync(query);

        // assert
        Assert.NotNull(context);
        context!.ToList().MatchSnapshot();
    }

    [Fact]
    public async Task SortingContext_Should_NotFail_When_SortingArgumentHasAListValue()
    {
        // arrange
        ISortingContext? context = null;
        var executor = await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType(x => x
                .Name("Query")
                .Field("test")
                .Type<ListType<ObjectType<Book>>>()
                .UseSorting<TestSortType>()
                .Resolve(x =>
                {
                    context = x.GetSortingContext();
                    return Array.Empty<Book>();
                }))
            .AddSorting()
            .BuildRequestExecutorAsync();

        // act
        const string query = @"
            {
                test(order: {title: 1, id: [1,2,3], author: [{name:DESC}]}) {
                    title
                }
            }
        ";

        await executor.ExecuteAsync(query);

        // assert
        Assert.NotNull(context);
        context!.ToList().MatchSnapshot();
    }

    public class TestSortType : SortInputType<Book>
    {
        protected override void Configure(ISortInputTypeDescriptor<Book> descriptor)
        {
            descriptor.Field(x => x.Id).Type<ListType<IntType>>();
            descriptor
                .Field(x => x.Author)
                .Type<NonNullType<ListType<NonNullType<SortInputType<Author>>>>>();
            descriptor.Field(x => x.Title).Type<NonNullType<IntType>>();
        }
    }

    public class Book
    {
        public int Id { get; set; }

        public string? Title { get; set; }

        public int Pages { get; set; }

        public int Chapters { get; set; }

        public bool IsActive { get; set; }

        public Author? Author { get; set; }

        public Author[]? CoAuthor { get; set; }
    }

    public class Author
    {
        public int Id { get; set; }

        public string? Name { get; set; }
    }
}
