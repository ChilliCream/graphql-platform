using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CookieCrumble;
using HotChocolate.Execution;
using HotChocolate.Execution.Processing;
using HotChocolate.Types;
using Microsoft.Extensions.DependencyInjection;
using Snapshooter;

using Xunit;

namespace HotChocolate.Data;

public class IntegrationTests : IClassFixture<AuthorFixture>
{
    private readonly Author[] _authors;

    public IntegrationTests(AuthorFixture authorFixture)
    {
        _authors = authorFixture.Authors;
    }

    [Fact]
    public async Task ExecuteAsync_Should_ReturnAllItems_When_ToListAsync()
    {
        // arrange
        var executor = await new ServiceCollection()
            .AddGraphQL()
            .AddFiltering()
            .AddSorting()
            .AddProjections()
            .AddQueryType(
                x => x
                    .Name("Query")
                    .Field("executable")
                    .Resolve(_authors.AsExecutable())
                    .UseProjection()
                    .UseFiltering()
                    .UseSorting())
            .BuildRequestExecutorAsync();

        // act
        var result = await executor.ExecuteAsync(
            @"
                {
                    executable {
                        name
                    }
                }
                ");

        // assert
        result.MatchSnapshot();
    }

    [Fact]
    public async Task ExecuteAsync_Should_OnlyOneItem_When_SingleOrDefault()
    {
        // arrange
        var executor = await new ServiceCollection()
            .AddGraphQL()
            .AddFiltering()
            .AddSorting()
            .AddProjections()
            .AddQueryType(
                x => x
                    .Name("Query")
                    .Field("executable")
                    .Type<ObjectType<Author>>()
                    .Resolve(_authors.Take(1).AsExecutable())
                    .UseSingleOrDefault()
                    .UseProjection()
                    .UseFiltering()
                    .UseSorting())
            .BuildRequestExecutorAsync();

        // act
        var result = await executor.ExecuteAsync(
            @"
                {
                    executable {
                        name
                    }
                }
                ");

        // assert
        result.MatchSnapshot();
    }

    [Fact]
    public async Task ExecuteAsync_Should_Fail_When_SingleOrDefaultMoreThanOne()
    {
        // arrange
        var executor = await new ServiceCollection()
            .AddGraphQL()
            .AddFiltering()
            .AddSorting()
            .AddProjections()
            .AddQueryType(
                x => x
                    .Name("Query")
                    .Field("executable")
                    .Type<ObjectType<Author>>()
                    .Resolve(_authors.AsExecutable())
                    .UseSingleOrDefault()
                    .UseProjection()
                    .UseFiltering()
                    .UseSorting())
            .BuildRequestExecutorAsync();

        // act
        var result = await executor.ExecuteAsync(
            @"
                {
                    executable {
                        name
                    }
                }
                ");

        // assert
        result.MatchSnapshot();
    }

    [Fact]
    public async Task ExecuteAsync_Should_Fail_When_SingleOrDefaultZero()
    {
        // arrange
        var executor = await new ServiceCollection()
            .AddGraphQL()
            .AddFiltering()
            .AddSorting()
            .AddProjections()
            .AddQueryType(
                x => x
                    .Name("Query")
                    .Field("executable")
                    .Type<ObjectType<Author>>()
                    .Resolve(_authors.Take(0).AsExecutable())
                    .UseSingleOrDefault()
                    .UseProjection()
                    .UseFiltering()
                    .UseSorting())
            .BuildRequestExecutorAsync();

        // act
        var result = await executor.ExecuteAsync(
            @"
                {
                    executable {
                        name
                    }
                }
                ");

        // assert
        result.MatchSnapshot();
    }

    [Fact]
    public async Task ExecuteAsync_Should_OnlyOneItem_When_FirstOrDefault()
    {
        // arrange
        var executor = await new ServiceCollection()
            .AddGraphQL()
            .AddFiltering()
            .AddSorting()
            .AddProjections()
            .AddQueryType(
                x => x
                    .Name("Query")
                    .Field("executable")
                    .Type<ObjectType<Author>>()
                    .Resolve(_authors.AsExecutable())
                    .UseFirstOrDefault()
                    .UseProjection()
                    .UseFiltering()
                    .UseSorting())
            .BuildRequestExecutorAsync();

        // act
        var result = await executor.ExecuteAsync(
            @"
                {
                    executable {
                        name
                    }
                }
                ");

        // assert
        result.MatchSnapshot();
    }

    [Fact]
    public async Task ExecuteAsync_Should_OnlyOneItem_When_FirstOrDefaultZero()
    {
        // arrange
        var executor = await new ServiceCollection()
            .AddGraphQL()
            .AddFiltering()
            .AddSorting()
            .AddProjections()
            .AddQueryType(
                x => x
                    .Name("Query")
                    .Field("executable")
                    .Type<ObjectType<Author>>()
                    .Resolve(_authors.Take(0).AsExecutable())
                    .UseFirstOrDefault()
                    .UseProjection()
                    .UseFiltering()
                    .UseSorting())
            .BuildRequestExecutorAsync();

        // act
        var result = await executor.ExecuteAsync(
            @"
                {
                    executable {
                        name
                    }
                }
                ");

        // assert
        result.MatchSnapshot();
    }

    [Fact]
    public async Task ExecuteAsync_Should_ProjectAndPage_When_BothMiddlewaresAreApplied()
    {
        // arrange
        var executor = await new ServiceCollection()
            .AddGraphQL()
            .AddFiltering()
            .AddGlobalObjectIdentification()
            .AddSorting()
            .AddProjections()
            .AddQueryType<PagingAndProjection>()
            .AddObjectType<Book>(x =>
                x.ImplementsNode().IdField(x => x.Id).ResolveNode(x => default!))
            .BuildRequestExecutorAsync();

        // act
        var result = await executor.ExecuteAsync(
            @"
                {
                    books {
                        edges {
                            node {
                                id
                                author {
                                    name
                                }
                            }
                        }
                    }
                }
                ");

        // assert
        result.MatchSnapshot();
    }

    [Fact]
    public async Task ExecuteAsync_Should_ProjectAndPage_When_BothAreAppliedAndProvided()
    {
        // arrange
        var executor = await new ServiceCollection()
            .AddGraphQL()
            .AddFiltering()
            .AddGlobalObjectIdentification()
            .AddSorting()
            .AddProjections()
            .AddQueryType<PagingAndProjection>()
            .AddObjectType<Book>(x =>
                x.ImplementsNode().IdField(x => x.Id).ResolveNode(x => default!))
            .BuildRequestExecutorAsync();

        // act
        var result = await executor.ExecuteAsync(
            @"
                {
                    books {
                        nodes {
                            id
                        }
                        edges {
                            node {
                                title
                            }
                        }
                    }
                }
                ");

        // assert
        result.MatchSnapshot();
    }

    [Fact]
    public async Task ExecuteAsync_Should_ProjectAndPage_When_EdgesFragment()
    {
        // arrange
        var executor = await new ServiceCollection()
            .AddGraphQL()
            .AddFiltering()
            .AddGlobalObjectIdentification()
            .AddSorting()
            .AddProjections()
            .AddQueryType<PagingAndProjection>()
            .AddObjectType<Book>(x =>
                x.ImplementsNode().IdField(x => x.Id).ResolveNode(x => default!))
            .BuildRequestExecutorAsync();

        // act
        var result = await executor.ExecuteAsync(
            @"
                {
                    books {
                        edges {
                            node {
                                ... Test
                            }
                        }
                    }
                }
                fragment Test on Book {
                    title
                    id
                }
                ");

        // assert
        result.MatchSnapshot();
    }

    [Fact]
    public async Task ExecuteAsync_Should_ProjectAndPage_When_NodesFragment()
    {
        // arrange
        var executor = await new ServiceCollection()
            .AddGraphQL()
            .AddFiltering()
            .AddGlobalObjectIdentification()
            .AddSorting()
            .AddProjections()
            .AddQueryType<PagingAndProjection>()
            .AddObjectType<Book>(x =>
                x.ImplementsNode().IdField(x => x.Id).ResolveNode(x => default!))
            .BuildRequestExecutorAsync();

        // act
        var result = await executor.ExecuteAsync(
            @"
                {
                    books {
                        nodes {
                            ... Test
                        }
                    }
                }
                fragment Test on Book {
                    title
                    id
                }
                ");

        // assert
        result.MatchSnapshot();
    }

    [Fact]
    public async Task ExecuteAsync_Should_ProjectAndPage_When_EdgesFragmentNested()
    {
        // arrange
        var executor = await new ServiceCollection()
            .AddGraphQL()
            .AddFiltering()
            .AddGlobalObjectIdentification()
            .AddSorting()
            .AddProjections()
            .AddQueryType<PagingAndProjection>()
            .AddObjectType<Book>(x =>
                x.ImplementsNode().IdField(x => x.Id).ResolveNode(x => default!))
            .BuildRequestExecutorAsync();

        // act
        var result = await executor.ExecuteAsync(
            @"
                {
                    books {
                        edges {
                            node {
                                author {
                                   ... Test
                                }
                            }
                        }
                    }
                }
                fragment Test on Author {
                    name
                }
                ");

        // assert
        result.MatchSnapshot();
    }

    [Fact]
    public async Task ExecuteAsync_Should_ProjectAndPage_When_NodesFragmentNested()
    {
        // arrange
        var executor = await new ServiceCollection()
            .AddGraphQL()
            .AddFiltering()
            .AddGlobalObjectIdentification()
            .AddSorting()
            .AddProjections()
            .AddQueryType<PagingAndProjection>()
            .AddObjectType<Book>(x =>
                x.ImplementsNode().IdField(x => x.Id).ResolveNode(x => default!))
            .BuildRequestExecutorAsync();

        // act
        var result = await executor.ExecuteAsync(
            @"
                {
                    books {
                        nodes {
                            author {
                               ... Test
                            }
                        }
                    }
                }
                fragment Test on Author {
                    name
                }
                ");

        // assert
        result.MatchSnapshot();
    }

    [Fact]
    public async Task
        ExecuteAsync_Should_ProjectAndPage_When_NodesFragmentContainsProjectedField()
    {
        // arrange
        var executor = await new ServiceCollection()
            .AddGraphQL()
            .AddFiltering()
            .AddGlobalObjectIdentification()
            .AddSorting()
            .AddProjections()
            .AddQueryType<PagingAndProjection>()
            .AddObjectType<Book>(x =>
                x.ImplementsNode().IdField(x => x.Id).ResolveNode(x => default!))
            .BuildRequestExecutorAsync();

        // act
        var result = await executor.ExecuteAsync(
            @"
                {
                    books {
                        nodes {
                            ... Test
                        }
                    }
                }
                fragment Test on Book {
                    title
                    author {
                       name
                    }
                }
                ");

        // assert
        result.MatchSnapshot();
    }

    [Fact]
    public async Task
        ExecuteAsync_Should_ProjectAndPage_When_NodesFragmentContainsProjectedField_With_Extensions()
    {
        // arrange
        var executor = await new ServiceCollection()
            .AddGraphQL()
            .AddFiltering()
            .AddGlobalObjectIdentification()
            .AddSorting()
            .AddProjections()
            .AddQueryType(c => c.Name("Query"))
            .AddTypeExtension<PagingAndProjectionExtension>()
            .AddObjectType<Book>(x =>
                x.ImplementsNode().IdField(x => x.Id).ResolveNode(x => default!))
            .BuildRequestExecutorAsync();

        // act
        var result = await executor.ExecuteAsync(
            @"
                {
                    books {
                        nodes {
                            ... Test
                        }
                    }
                }
                fragment Test on Book {
                    title
                    author {
                       name
                    }
                }
                ");

        // assert
        await Snapshot
            .Create()
            .Add(result, "Result:")
            .Add(executor.Schema, "Schema:")
            .MatchAsync();
    }

    [Fact]
    public async Task ExecuteAsync_Should_ProjectAndPage_When_AliasIsSameAsAlwaysProjectedField()
    {
        // arrange
        var executor = await new ServiceCollection()
            .AddGraphQL()
            .AddFiltering()
            .AddGlobalObjectIdentification()
            .AddSorting()
            .AddProjections()
            .AddQueryType(c => c.Name("Query"))
            .AddTypeExtension<PagingAndProjectionExtension>()
            .AddObjectType<Book>(x =>
                x.ImplementsNode()
                    .IdField(book => book.Id)
                    .ResolveNode(_ => default!))
            .BuildRequestExecutorAsync();

        // act
        var result = await executor.ExecuteAsync(
            @"{
                books {
                    nodes {
                        authorId: title
                    }
                }
            }");

        // assert
        await Snapshot
            .Create()
            .Add(result, "Result:")
            .Add(executor.Schema, "Schema:")
            .MatchAsync();
    }

    [Fact]
    public async Task CreateSchema_CodeFirst_AsyncQueryable()
    {
        // arrange
        var executor = await new ServiceCollection()
            .AddGraphQL()
            .AddFiltering()
            .AddQueryType<FooType>()
            .BuildRequestExecutorAsync();

        // act
        var result = await executor.ExecuteAsync(
            @"
                {
                    foos(where: { qux: {eq: ""a""}}) {
                        qux
                    }
                }
                ");

        // assert
        await Snapshot
            .Create()
            .Add(result, "Result:")
            .Add(executor.Schema, "Schema:")
            .MatchAsync();
    }

    [Fact]
    public async Task CreateSchema_OnDifferentScope()
    {
        // arrange
        var executor = await new ServiceCollection()
            .AddGraphQL()
            .AddFiltering("Foo")
            .AddSorting("Foo")
            .AddProjections("Foo")
            .AddQueryType<DifferentScope>()
            .BuildRequestExecutorAsync();

        // act
        var result = await executor.ExecuteAsync(
            @"
                {
                    books(where: { title: {eq: ""BookTitle""}}) {
                        nodes { title }
                    }
                }
                ");

        // assert
        await Snapshot
            .Create()
            .Add(result, "Result:")
            .Add(executor.Schema, "Schema:")
            .MatchAsync();
    }

    [Fact]
    public async Task Execute_And_OnRoot()
    {
        // arrange
        var executor = await new ServiceCollection()
            .AddGraphQL()
            .AddFiltering("Foo")
            .AddSorting("Foo")
            .AddProjections("Foo")
            .AddQueryType<DifferentScope>()
            .BuildRequestExecutorAsync();

        // act
        var result = await executor.ExecuteAsync(
            @"
                query GetBooks($title: String) {
                    books(where: {
                            and: [
                                { title: { startsWith: $title } },
                                { title: { eq: ""BookTitle"" } },
                            ]
                    }) {
                        nodes { title }
                    }
                }",
            new Dictionary<string, object?> { ["title"] = "BookTitle" });

        // assert
        await Snapshot
            .Create()
            .Add(result, "Result:")
            .Add(executor.Schema, "Schema:")
            .MatchAsync();
    }

    [Fact]
    public async Task Execute_And_OnRoot_Reverse()
    {
        // arrange
        var executor = await new ServiceCollection()
            .AddGraphQL()
            .AddFiltering("Foo")
            .AddSorting("Foo")
            .AddProjections("Foo")
            .AddQueryType<DifferentScope>()
            .BuildRequestExecutorAsync();

        // act
        var result = await executor.ExecuteAsync(
            @"
                query GetBooks($title: String) {
                    books(where: {
                            and: [
                                { title: { eq: ""BookTitle"" } },
                                { title: { startsWith: $title } },
                            ]
                    }) {
                        nodes { title }
                    }
                }",
            new Dictionary<string, object?> { ["title"] = "BookTitle" });

        // assert
        await Snapshot
            .Create()
            .Add(result, "Result:")
            .Add(executor.Schema, "Schema:")
            .MatchAsync();
    }

    [Fact]
    public async Task ExecuteAsync_Should_ArgumentAndFirstOrDefault_When_Executed()
    {
        // arrange
        var executor = await new ServiceCollection()
            .AddGraphQL()
            .AddFiltering()
            .AddSorting()
            .AddProjections()
            .AddQueryType<FirstOrDefaulQuery>()
            .BuildRequestExecutorAsync();

        // act
        var result = await executor.ExecuteAsync(
            @"
                {
                    books(book: {id: 1, authorId: 0}) {
                        title
                    }
                }
                ");

        // assert
        result.MatchSnapshot();
    }

    [Fact]
    public async Task
        Schema_Should_Generate_WhenMutationInputHasManyToManyRelationshipWithOutputType()
    {
        // arrange
        var executor = await new ServiceCollection()
            .AddGraphQL()
            .AddFiltering()
            .AddSorting()
            .AddProjections()
            .AddQueryType<FirstOrDefaulQuery>()
            .AddMutationType<FirstOrDefaultMutation_ManyToMany>()
            .BuildRequestExecutorAsync();

        // act
        var result = executor.Schema.Print();

        // assert
        result.MatchSnapshot();
    }

    [Fact]
    public async Task
        Schema_Should_Generate_WhenMutationInputHasManyToOneRelationshipWithOutputType()
    {
        // arrange
        var executor = await new ServiceCollection()
            .AddGraphQL()
            .AddFiltering()
            .AddSorting()
            .AddProjections()
            .AddQueryType<FirstOrDefaulQuery>()
            .AddMutationType<FirstOrDefaultMutation_ManyToOne>()
            .BuildRequestExecutorAsync();

        // act
        var result = executor.Schema.Print();

        // assert
        result.MatchSnapshot();
    }

    [Fact]
    public async Task
    Schema_Should_Generate_WhenStaticTypeExtensionWithOffsetPagingOnStaticResolver()
    {
        // arrange
        var executor = await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType()
            .AddTypeExtension(typeof(StaticQuery))
            .BuildRequestExecutorAsync();

        // act
        var result = executor.Schema.Print();

        // assert
        result.MatchSnapshot();
    }

    [QueryType]
    public static class StaticQuery
    {
        [UseOffsetPaging]
        public static IEnumerable<Bar> GetBars() => new[] { Bar.Create("tox") };
    }

    public class FooType : ObjectType
    {
        protected override void Configure(IObjectTypeDescriptor descriptor)
        {
            descriptor
                .Field("foos")
                .Type<ListType<ObjectType<Bar>>>()
                .Resolve(_ =>
                {
                    var data = new[]
                    {
                        Bar.Create("a"),
                        Bar.Create("b")
                    }.AsQueryable();
                    return Task.FromResult(data);
                })
                .UseFiltering();
        }
    }

    public class Bar
    {
        public string Qux { get; set; } = default!;

        public static Bar Create(string qux) => new() { Qux = qux };
    }

    public class PagingAndProjection
    {
        [UsePaging]
        [UseProjection]
        public IQueryable<Book> GetBooks() => new[]
        {
            new Book
            {
                Id = 1,
                Title = "BookTitle",
                Author = new Author { Name = "Author" }
            }
        }.AsQueryable();
    }

    [ExtendObjectType("Query")]
    public class PagingAndProjectionExtension
    {
        [UsePaging]
        [UseProjection]
        [UseFiltering]
        [UseSorting]
        public IQueryable<Book> GetBooks() => new[]
        {
            new Book
            {
                Id = 1,
                Title = "BookTitle",
                Author = new Author { Name = "Author" }
            }
        }.AsQueryable();
    }

    public class DifferentScope
    {
        [UsePaging]
        [UseProjection(Scope = "Foo")]
        [UseFiltering(Scope = "Foo")]
        [UseSorting(Scope = "Foo")]
        public IQueryable<Book> GetBooks() => new[]
        {
                new Book { Id = 1, Title = "BookTitle", Author = new Author { Name = "Author" } }
            }.AsQueryable();
    }

    public class BookInput
    {
        public int Id { get; set; }

    }

    public class FirstOrDefaulQuery
    {
        [UseFirstOrDefault]
        [UseProjection]
        public IQueryable<Book> GetBooks(Book book) => new[]
        {
            new Book
            {
                Id = 1, Title = "BookTitle", Author = new Author { Name = "Author" }
            },
            new Book
            {
                Id = 2, Title = "BookTitle2", Author = new Author { Name = "Author2" }
            }
        }.AsQueryable().Where(x => x.Id == book.Id);
    }

    public class FirstOrDefaultMutation_ManyToMany
    {
        [UseFirstOrDefault]
        [UseProjection]
        public IQueryable<Author> AddPublisher(Publisher publisher) => new[]
        {
            new Author { Name = "Author", Publishers = new List<Publisher> { publisher } }
        }.AsQueryable();
    }

    public class FirstOrDefaultMutation_ManyToOne
    {
        [UseFirstOrDefault]
        [UseProjection]
        public IQueryable<Author> AddBook(Book book) => new[]
        {
            new Author { Name = "Author", Books = new List<Book> { book } }
        }.AsQueryable();
    }
}
