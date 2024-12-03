// ReSharper disable ClassNeverInstantiated.Local
// ReSharper disable UnusedMember.Local
// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable MoveLocalFunctionAfterJumpStatement

using HotChocolate.Data.Filters;
using HotChocolate.Execution;
using HotChocolate.Types;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Data;

public class IntegrationTests(AuthorFixture authorFixture) : IClassFixture<AuthorFixture>
{
    private readonly Author[] _authors = authorFixture.Authors;

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
                    .Resolve(_authors)
                    .UseProjection()
                    .UseFiltering()
                    .UseSorting())
            .BuildRequestExecutorAsync();

        // act
        var result = await executor.ExecuteAsync(
            """
            {
                executable {
                    name
                }
            }
            """);

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
                    .Resolve(_authors.Take(1))
                    .UseSingleOrDefault()
                    .UseProjection()
                    .UseFiltering()
                    .UseSorting())
            .BuildRequestExecutorAsync();

        // act
        var result = await executor.ExecuteAsync(
            """
            {
                executable {
                    name
                }
            }
            """);

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
                    .Resolve(_authors)
                    .UseSingleOrDefault()
                    .UseProjection()
                    .UseFiltering()
                    .UseSorting())
            .BuildRequestExecutorAsync();

        // act
        var result = await executor.ExecuteAsync(
            """
            {
                executable {
                    name
                }
            }
            """);

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
                    .Resolve(_authors.Take(0))
                    .UseSingleOrDefault()
                    .UseProjection()
                    .UseFiltering()
                    .UseSorting())
            .BuildRequestExecutorAsync();

        // act
        var result = await executor.ExecuteAsync(
            """
            {
                executable {
                    name
                }
            }
            """);

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
                    .Resolve(_authors)
                    .UseFirstOrDefault()
                    .UseProjection()
                    .UseFiltering()
                    .UseSorting())
            .BuildRequestExecutorAsync();

        // act
        var result = await executor.ExecuteAsync(
            """
            {
                executable {
                    name
                }
            }
            """);

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
                    .Resolve(_authors.Take(0))
                    .UseFirstOrDefault()
                    .UseProjection()
                    .UseFiltering()
                    .UseSorting())
            .BuildRequestExecutorAsync();

        // act
        var result = await executor.ExecuteAsync(
            """
            {
                executable {
                    name
                }
            }
            """);

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
            .AddObjectType<Book>(
                o =>
                    o.ImplementsNode()
                        .IdField(f => f.Id)
                        .ResolveNode(_ => default!))
            .BuildRequestExecutorAsync();

        // act
        var result = await executor.ExecuteAsync(
            """
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
            """);

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
            .AddObjectType<Book>(
                o =>
                    o.ImplementsNode()
                        .IdField(f => f.Id)
                        .ResolveNode(_ => default!))
            .BuildRequestExecutorAsync();

        // act
        var result = await executor.ExecuteAsync(
            """
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
            """);

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
            .AddObjectType<Book>(
                o =>
                    o.ImplementsNode()
                        .IdField(f => f.Id)
                        .ResolveNode(_ => default!))
            .BuildRequestExecutorAsync();

        // act
        var result = await executor.ExecuteAsync(
            """
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
            """);

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
            .AddObjectType<Book>(
                o =>
                    o.ImplementsNode()
                        .IdField(f => f.Id)
                        .ResolveNode(_ => default!))
            .BuildRequestExecutorAsync();

        // act
        var result = await executor.ExecuteAsync(
            """
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
            """);

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
            .AddObjectType<Book>(
                o =>
                    o.ImplementsNode()
                        .IdField(f => f.Id)
                        .ResolveNode(_ => default!))
            .BuildRequestExecutorAsync();

        // act
        var result = await executor.ExecuteAsync(
            """
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
            """);

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
            .AddObjectType<Book>(
                o =>
                    o.ImplementsNode()
                        .IdField(f => f.Id)
                        .ResolveNode(_ => default!))
            .BuildRequestExecutorAsync();

        // act
        var result = await executor.ExecuteAsync(
            """
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
            """);

        // assert
        result.MatchSnapshot();
    }

    [Fact]
    public async Task ExecuteAsync_Should_ProjectAndPage_When_NodesFragmentContainsProjectedField()
    {
        // arrange
        var executor = await new ServiceCollection()
            .AddGraphQL()
            .AddFiltering()
            .AddGlobalObjectIdentification()
            .AddSorting()
            .AddProjections()
            .AddQueryType<PagingAndProjection>()
            .AddObjectType<Book>(
                o =>
                    o.ImplementsNode()
                        .IdField(f => f.Id)
                        .ResolveNode(_ => default!))
            .BuildRequestExecutorAsync();

        // act
        var result = await executor.ExecuteAsync(
            """
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
            """);

        // assert
        result.MatchSnapshot();
    }

    [Fact]
    public async Task ExecuteAsync_Should_ProjectAndPage_When_NodesFragmentContainsProjectedField_With_Extensions()
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
            .AddObjectType<Book>(
                o =>
                    o.ImplementsNode()
                        .IdField(f => f.Id)
                        .ResolveNode(_ => default!))
            .BuildRequestExecutorAsync();

        // act
        var result = await executor.ExecuteAsync(
            """
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
            """);

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
            .AddObjectType<Book>(
                o =>
                    o.ImplementsNode()
                        .IdField(f => f.Id)
                        .ResolveNode(_ => default!))
            .BuildRequestExecutorAsync();

        // act
        var result = await executor.ExecuteAsync(
            """
            {
                books {
                    nodes {
                        authorId: title
                    }
                }
            }
            """);

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
            """
            {
                foos(where: { qux: {eq: "a"}}) {
                    qux
                }
            }
            """);

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
            """
            {
                books(where: { title: { eq: "BookTitle" }}) {
                    nodes { title }
                }
            }
            """);

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
            """
            query GetBooks($title: String) {
                books(where: {
                        and: [
                            { title: { startsWith: $title } },
                            { title: { eq: "BookTitle" } },
                        ]
                }) {
                    nodes { title }
                }
            }
            """,
            new Dictionary<string, object?>
            {
                ["title"] = "BookTitle",
            });

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
            """
            query GetBooks($title: String) {
                books(where: {
                        and: [
                            { title: { eq: "BookTitle" } },
                            { title: { startsWith: $title } },
                        ]
                }) {
                    nodes { title }
                }
            }
            """,
            new Dictionary<string, object?>
            {
                ["title"] = "BookTitle",
            });

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
            .AddQueryType<FirstOrDefaultQuery>()
            .BuildRequestExecutorAsync();

        // act
        var result = await executor.ExecuteAsync(
            """
            {
                books(book: {id: 1, authorId: 0}) {
                    title
                }
            }
            """);

        // assert
        result.MatchSnapshot();
    }

    [Fact]
    public async Task Schema_Should_Generate_WhenMutationInputHasManyToManyRelationshipWithOutputType()
    {
        // arrange
        var executor = await new ServiceCollection()
            .AddGraphQL()
            .AddFiltering()
            .AddSorting()
            .AddProjections()
            .AddQueryType<FirstOrDefaultQuery>()
            .AddMutationType<FirstOrDefaultMutationManyToMany>()
            .BuildRequestExecutorAsync();

        // act
        var result = executor.Schema.Print();

        // assert
        result.MatchSnapshot();
    }

    [Fact]
    public async Task Schema_Should_Generate_WhenMutationInputHasManyToOneRelationshipWithOutputType()
    {
        // arrange
        var executor = await new ServiceCollection()
            .AddGraphQL()
            .AddFiltering()
            .AddSorting()
            .AddProjections()
            .AddQueryType<FirstOrDefaultQuery>()
            .AddMutationType<FirstOrDefaultMutationManyToOne>()
            .BuildRequestExecutorAsync();

        // act
        var result = executor.Schema.Print();

        // assert
        result.MatchSnapshot();
    }

    [Fact]
    public async Task Schema_Should_Generate_WhenStaticTypeExtensionWithOffsetPagingOnStaticResolver()
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

    [Fact]
    public async Task Duplicate_Filter_Attribute_Throws()
    {
        // arrange
        async Task Error()
            => await new ServiceCollection()
                .AddGraphQL()
                .AddQueryType<DuplicateAttribute>()
                .AddFiltering()
                .BuildRequestExecutorAsync();

        // act & assert
        var error = await Assert.ThrowsAsync<SchemaException>(Error);
        error.Errors[0].Message.MatchInlineSnapshot(
            """
            The field `DuplicateAttribute.addBook` declares the data middleware `UseFiltering` more than once.
            """);
    }

    [Fact]
    public async Task AsPredicate_No_Filter_Returns_All_Data()
    {
        // arrange
        var executor = await new ServiceCollection()
            .AddGraphQL()
            .AddFiltering()
            .AddSorting()
            .AddProjections()
            .AddQueryType<AsPredicateQuery>()
            .BuildRequestExecutorAsync();

        // act
        var result = await executor.ExecuteAsync(
            """
            {
                authors {
                    name
                }
            }
            """);

        // assert
        result.MatchSnapshot();
    }

    [Fact]
    public async Task AsPredicate_With_Filter_Returns_Author_1()
    {
        // arrange
        var executor = await new ServiceCollection()
            .AddGraphQL()
            .AddFiltering()
            .AddSorting()
            .AddProjections()
            .AddQueryType<AsPredicateQuery>()
            .BuildRequestExecutorAsync();

        // act
        var result = await executor.ExecuteAsync(
            """
            {
                authors(where: { name: { eq: "Author1" } }) {
                    name
                }
            }
            """);

        // assert
        result.MatchSnapshot();
    }

    [QueryType]
    public static class StaticQuery
    {
        [UseOffsetPaging]
        public static IEnumerable<Bar> GetBars()
            => new[]
            {
                Bar.Create("tox"),
            };
    }

    public class FooType : ObjectType
    {
        protected override void Configure(IObjectTypeDescriptor descriptor)
        {
            descriptor
                .Field("foos")
                .Type<ListType<ObjectType<Bar>>>()
                .Resolve(
                    _ =>
                    {
                        var data = new[]
                        {
                            Bar.Create("a"),
                            Bar.Create("b"),
                        }.AsQueryable();
                        return Task.FromResult(data);
                    })
                .UseFiltering();
        }
    }

    public class Bar
    {
        public string Qux { get; set; } = default!;

        public static Bar Create(string qux) => new()
        {
            Qux = qux,
        };
    }

    public class PagingAndProjection
    {
        [UsePaging]
        [UseProjection]
        public IQueryable<Book> GetBooks()
            => new[]
            {
                new Book
                {
                    Id = 1,
                    Title = "BookTitle",
                    Author = new Author
                    {
                        Name = "Author",
                    },
                },
            }.AsQueryable();
    }

    [ExtendObjectType("Query")]
    public class PagingAndProjectionExtension
    {
        [UsePaging]
        [UseProjection]
        [UseFiltering]
        [UseSorting]
        public IQueryable<Book> GetBooks()
            => new[]
            {
                new Book
                {
                    Id = 1,
                    Title = "BookTitle",
                    Author = new Author
                    {
                        Name = "Author",
                    },
                },
            }.AsQueryable();
    }

    public class DifferentScope
    {
        [UsePaging]
        [UseProjection(Scope = "Foo")]
        [UseFiltering(Scope = "Foo")]
        [UseSorting(Scope = "Foo")]
        public IQueryable<Book> GetBooks()
            => new[]
            {
                new Book
                {
                    Id = 1,
                    Title = "BookTitle",
                    Author = new Author
                    {
                        Name = "Author",
                    },
                },
            }.AsQueryable();
    }

    public class FirstOrDefaultQuery
    {
        [UseFirstOrDefault]
        [UseProjection]
        public IQueryable<Book> GetBooks(Book book) => new[]
        {
            new Book
            {
                Id = 1,
                Title = "BookTitle",
                Author = new Author
                {
                    Name = "Author",
                },
            },
            new Book
            {
                Id = 2,
                Title = "BookTitle2",
                Author = new Author
                {
                    Name = "Author2",
                },
            },
        }.AsQueryable().Where(x => x.Id == book.Id);
    }

    public class FirstOrDefaultMutationManyToMany
    {
        [UseFirstOrDefault]
        [UseProjection]
        public IQueryable<Author> AddPublisher(Publisher publisher)
            => new[]
            {
                new Author
                {
                    Name = "Author",
                    Publishers = new List<Publisher>
                    {
                        publisher,
                    },
                },
            }.AsQueryable();
    }

    public class FirstOrDefaultMutationManyToOne
    {
        [UseFirstOrDefault]
        [UseProjection]
        public IQueryable<Author> AddBook(Book book)
            => new[]
            {
                new Author
                {
                    Name = "Author",
                    Books = new List<Book>
                    {
                        book,
                    },
                },
            }.AsQueryable();
    }

    public class DuplicateAttribute
    {
        [UseFiltering]
        [UseFiltering]
        public IQueryable<Author> AddBook(Book book)
            => new[]
            {
                new Author
                {
                    Name = "Author",
                    Books = new List<Book>
                    {
                        book,
                    },
                },
            }.AsQueryable();
    }

    public class AsPredicateQuery
    {
        [UseFiltering]
        public IQueryable<Author> GetAuthors(IFilterContext filter)
            => new[]
                {
                    new Author
                    {
                        Name = "Author1",
                        Books = new List<Book>(),
                    },
                    new Author
                    {
                        Name = "Author2",
                        Books = new List<Book>()
                    },
                }.AsQueryable()
                .Where(filter);
    }
}
