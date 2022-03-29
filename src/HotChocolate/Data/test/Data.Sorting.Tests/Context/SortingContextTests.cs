using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using HotChocolate.Data.Filters.Expressions;
using HotChocolate.Data.Sorting.Expressions;
using HotChocolate.Execution;
using HotChocolate.Resolvers;
using HotChocolate.Types;
using Microsoft.Extensions.DependencyInjection;
using Snapshooter.Xunit;
using Xunit;

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
        ISortingFieldInfo field = Assert.Single(Assert.Single(context!.GetFields()));
        object? operation = Assert.IsType<SortingValue>(field.Value).Value;
        Assert.Equal("title", field.Field.Name);
        Assert.Equal("DESC", operation);
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
        ISortingFieldInfo field = Assert.Single(context!.GetFields()[0]);
        object? operation = Assert.IsType<SortingValue>(field.Value).Value;
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
        ISortingFieldInfo field = Assert.Single(Assert.Single(context!.GetFields()));
        ISortingFieldInfo name =
            Assert.Single(Assert.IsType<SortingInfo>(field.Value).GetFields());
        object? operation = Assert.IsType<SortingValue>(name.Value).Value;
        Assert.Equal("author", field.Field.Name);
        Assert.Equal("name", name.Field.Name);
        Assert.Equal("DESC", operation);
    }

    [Fact]
    public async Task EnableSortingExecution_Should_EnableSortingExecution()
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
                    x.GetSortingContext()?.EnableSortingExecution();
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
    public async Task EnableSortingExecution_Should_DisableSortingExecutionByDefault()
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
