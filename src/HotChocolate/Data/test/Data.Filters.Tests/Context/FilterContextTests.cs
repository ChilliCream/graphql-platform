using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HotChocolate.Data.Filters.Expressions;
using HotChocolate.Execution;
using HotChocolate.Types;
using Microsoft.Extensions.DependencyInjection;
using Snapshooter.Xunit;
using Xunit;

namespace HotChocolate.Data.Filters;

public class FilterContextTests
{
    [Fact]
    public async Task GetFields_Should_ReturnScalarField()
    {
        // arrange
        IFilterContext? context = null;
        var executor = await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType(x => x
                .Name("Query")
                .Field("test")
                .Type<ListType<ObjectType<Book>>>()
                .UseFiltering()
                .Resolve(x =>
                {
                    context = x.GetFilterContext();
                    return Array.Empty<Book>();
                }))
            .AddFiltering()
            .BuildRequestExecutorAsync();

        // act
        const string query = @"
            {
                test(where: { title: { eq: ""test"" } }) {
                    title
                }
            }
        ";

        await executor.ExecuteAsync(query);

        // assert
        Assert.NotNull(context);
        IFilterFieldInfo field = Assert.Single(context!.GetFields());
        Assert.Empty(context.GetOperations());
        IFilterOperationInfo operation = Assert.Single(field.GetOperations());
        Assert.Empty(field.GetFields());
        Assert.Equal("title", field.Field.Name);
        Assert.Equal("eq", operation.Field.Name);
        Assert.Equal("test", operation.Value);
    }

    [Fact]
    public async Task GetFields_Should_ReturnScalarFieldWithListOperation()
    {
        // arrange
        IFilterContext? context = null;
        var executor = await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType(x => x
                .Name("Query")
                .Field("test")
                .Type<ListType<ObjectType<Book>>>()
                .UseFiltering()
                .Resolve(x =>
                {
                    context = x.GetFilterContext();
                    return Array.Empty<Book>();
                }))
            .AddFiltering()
            .BuildRequestExecutorAsync();

        // act
        const string query = @"
            {
                test(where: { title: { in: [""a"", ""b""] } }) {
                    title
                }
            }
        ";

        await executor.ExecuteAsync(query);

        // assert
        Assert.NotNull(context);
        IFilterFieldInfo field = Assert.Single(context!.GetFields());
        Assert.Empty(context.GetOperations());
        IFilterOperationInfo operation = Assert.Single(field.GetOperations());
        Assert.Empty(field.GetFields());
        Assert.Equal("title", field.Field.Name);
        Assert.Equal("in", operation.Field.Name);
        Assert.Equal("a", ((IEnumerable<string>)operation.Value!).FirstOrDefault());
        Assert.Equal("b", ((IEnumerable<string>)operation.Value!).LastOrDefault());
    }

    [Fact]
    public async Task GetFields_Should_ReturnDeepObject()
    {
        // arrange
        IFilterContext? context = null;
        var executor = await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType(x => x
                .Name("Query")
                .Field("test")
                .Type<ListType<ObjectType<Book>>>()
                .UseFiltering()
                .Resolve(x =>
                {
                    context = x.GetFilterContext();
                    return Array.Empty<Book>();
                }))
            .AddFiltering()
            .BuildRequestExecutorAsync();

        // act
        const string query = @"
            {
                test(where: { author: { name: { eq: ""test"" } } }) {
                    title
                }
            }
        ";

        await executor.ExecuteAsync(query);

        // assert
        Assert.NotNull(context);
        IFilterFieldInfo author = Assert.Single(context!.GetFields());
        Assert.Empty(context.GetOperations());
        IFilterFieldInfo name = Assert.Single(author!.GetFields());
        Assert.Empty(author.GetOperations());
        IFilterOperationInfo operation = Assert.Single(name.GetOperations());
        Assert.Empty(name.GetFields());
        Assert.Equal("author", author.Field.Name);
        Assert.Equal(context, author.Parent);
        Assert.Equal("name", name.Field.Name);
        Assert.Equal(author, name.Parent);
        Assert.Equal("eq", operation.Field.Name);
        Assert.Equal(name, operation.Parent);
        Assert.Equal("test", operation.Value);
    }

    [Fact]
    public async Task GetFields_Should_ReturnOrOperations()
    {
        // arrange
        IFilterContext? context = null;
        var executor = await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType(x => x
                .Name("Query")
                .Field("test")
                .Type<ListType<ObjectType<Book>>>()
                .UseFiltering()
                .Resolve(x =>
                {
                    context = x.GetFilterContext();
                    return Array.Empty<Book>();
                }))
            .AddFiltering()
            .BuildRequestExecutorAsync();

        // act
        const string query = @"
            {
                test(where: {
                    or: [
                        { title: { eq: ""a"" } }
                        { title: { eq: ""b"" } }
                    ]
                }) {
                    title
                }
            }
        ";

        await executor.ExecuteAsync(query);

        // assert
        Assert.NotNull(context);
        context!.ToDictionary().MatchSnapshot();
    }

    public class Book
    {
        public int Id { get; set; }

        public string? Title { get; set; }

        public int Pages { get; set; }
        public int Chapters { get; set; }

        public Author? Author { get; set; }

        public Author[]? CoAuthor { get; set; }
    }

    public class Author
    {
        public int Id { get; set; }

        public string? Name { get; set; }
    }
}

