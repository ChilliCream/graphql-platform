using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HotChocolate.Execution;
using HotChocolate.Execution.Processing;
using HotChocolate.Types;
using Microsoft.Extensions.DependencyInjection;
using Snapshooter;
using Snapshooter.Xunit;
using Xunit;

namespace HotChocolate.Data
{
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
            IRequestExecutor executor = await new ServiceCollection()
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
            IExecutionResult result = await executor.ExecuteAsync(
                @"
                {
                    executable {
                        name
                    }
                }
                ");

            // assert
            result.ToJson().MatchSnapshot();
        }

        [Fact]
        public async Task ExecuteAsync_Should_OnlyOneItem_When_SingleOrDefault()
        {
            // arrange
            IRequestExecutor executor = await new ServiceCollection()
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
            IExecutionResult result = await executor.ExecuteAsync(
                @"
                {
                    executable {
                        name
                    }
                }
                ");

            // assert
            result.ToJson().MatchSnapshot();
        }

        [Fact]
        public async Task ExecuteAsync_Should_Fail_When_SingleOrDefaultMoreThanOne()
        {
            // arrange
            IRequestExecutor executor = await new ServiceCollection()
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
            IExecutionResult result = await executor.ExecuteAsync(
                @"
                {
                    executable {
                        name
                    }
                }
                ");

            // assert
            result.ToJson().MatchSnapshot();
        }

        [Fact]
        public async Task ExecuteAsync_Should_Fail_When_SingleOrDefaultZero()
        {
            // arrange
            IRequestExecutor executor = await new ServiceCollection()
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
            IExecutionResult result = await executor.ExecuteAsync(
                @"
                {
                    executable {
                        name
                    }
                }
                ");

            // assert
            result.ToJson().MatchSnapshot();
        }

        [Fact]
        public async Task ExecuteAsync_Should_OnlyOneItem_When_FirstOrDefault()
        {
            // arrange
            IRequestExecutor executor = await new ServiceCollection()
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
            IExecutionResult result = await executor.ExecuteAsync(
                @"
                {
                    executable {
                        name
                    }
                }
                ");

            // assert
            result.ToJson().MatchSnapshot();
        }

        [Fact]
        public async Task ExecuteAsync_Should_OnlyOneItem_When_FirstOrDefaultZero()
        {
            // arrange
            IRequestExecutor executor = await new ServiceCollection()
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
            IExecutionResult result = await executor.ExecuteAsync(
                @"
                {
                    executable {
                        name
                    }
                }
                ");

            // assert
            result.ToJson().MatchSnapshot();
        }

        [Fact]
        public async Task ExecuteAsync_Should_ProjectAndPage_When_BothMiddlewaresAreApplied()
        {
            // arrange
            IRequestExecutor executor = await new ServiceCollection()
                .AddGraphQL()
                .AddFiltering()
                .EnableRelaySupport()
                .AddSorting()
                .AddProjections()
                .AddQueryType<PagingAndProjection>()
                .AddObjectType<Book>(x =>
                    x.ImplementsNode().IdField(x => x.Id).ResolveNode(x => default!))
                .BuildRequestExecutorAsync();

            // act
            IExecutionResult result = await executor.ExecuteAsync(
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
            result.ToJson().MatchSnapshot();
        }

        [Fact]
        public async Task ExecuteAsync_Should_ProjectAndPage_When_BothAreAppliedAndProvided()
        {
            // arrange
            IRequestExecutor executor = await new ServiceCollection()
                .AddGraphQL()
                .AddFiltering()
                .EnableRelaySupport()
                .AddSorting()
                .AddProjections()
                .AddQueryType<PagingAndProjection>()
                .AddObjectType<Book>(x =>
                    x.ImplementsNode().IdField(x => x.Id).ResolveNode(x => default!))
                .BuildRequestExecutorAsync();

            // act
            IExecutionResult result = await executor.ExecuteAsync(
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
            result.ToJson().MatchSnapshot();
        }

        [Fact]
        public async Task ExecuteAsync_Should_ProjectAndPage_When_EdgesFragment()
        {
            // arrange
            IRequestExecutor executor = await new ServiceCollection()
                .AddGraphQL()
                .AddFiltering()
                .EnableRelaySupport()
                .AddSorting()
                .AddProjections()
                .AddQueryType<PagingAndProjection>()
                .AddObjectType<Book>(x =>
                    x.ImplementsNode().IdField(x => x.Id).ResolveNode(x => default!))
                .BuildRequestExecutorAsync();

            // act
            IExecutionResult result = await executor.ExecuteAsync(
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
            result.ToJson().MatchSnapshot();
        }

        [Fact]
        public async Task ExecuteAsync_Should_ProjectAndPage_When_NodesFragment()
        {
            // arrange
            IRequestExecutor executor = await new ServiceCollection()
                .AddGraphQL()
                .AddFiltering()
                .EnableRelaySupport()
                .AddSorting()
                .AddProjections()
                .AddQueryType<PagingAndProjection>()
                .AddObjectType<Book>(x =>
                    x.ImplementsNode().IdField(x => x.Id).ResolveNode(x => default!))
                .BuildRequestExecutorAsync();

            // act
            IExecutionResult result = await executor.ExecuteAsync(
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
            result.ToJson().MatchSnapshot();
        }

        [Fact]
        public async Task ExecuteAsync_Should_ProjectAndPage_When_EdgesFragmentNested()
        {
            // arrange
            IRequestExecutor executor = await new ServiceCollection()
                .AddGraphQL()
                .AddFiltering()
                .EnableRelaySupport()
                .AddSorting()
                .AddProjections()
                .AddQueryType<PagingAndProjection>()
                .AddObjectType<Book>(x =>
                    x.ImplementsNode().IdField(x => x.Id).ResolveNode(x => default!))
                .BuildRequestExecutorAsync();

            // act
            IExecutionResult result = await executor.ExecuteAsync(
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
            result.ToJson().MatchSnapshot();
        }

        [Fact]
        public async Task ExecuteAsync_Should_ProjectAndPage_When_NodesFragmentNested()
        {
            // arrange
            IRequestExecutor executor = await new ServiceCollection()
                .AddGraphQL()
                .AddFiltering()
                .EnableRelaySupport()
                .AddSorting()
                .AddProjections()
                .AddQueryType<PagingAndProjection>()
                .AddObjectType<Book>(x =>
                    x.ImplementsNode().IdField(x => x.Id).ResolveNode(x => default!))
                .BuildRequestExecutorAsync();

            // act
            IExecutionResult result = await executor.ExecuteAsync(
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
            result.ToJson().MatchSnapshot();
        }

        [Fact]
        public async Task
            ExecuteAsync_Should_ProjectAndPage_When_NodesFragmentContainsProjectedField()
        {
            // arrange
            IRequestExecutor executor = await new ServiceCollection()
                .AddGraphQL()
                .AddFiltering()
                .EnableRelaySupport()
                .AddSorting()
                .AddProjections()
                .AddQueryType<PagingAndProjection>()
                .AddObjectType<Book>(x =>
                    x.ImplementsNode().IdField(x => x.Id).ResolveNode(x => default!))
                .BuildRequestExecutorAsync();

            // act
            IExecutionResult result = await executor.ExecuteAsync(
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
            result.ToJson().MatchSnapshot();
        }

        [Fact]
        public async Task
            ExecuteAsync_Should_ProjectAndPage_When_NodesFragmentContainsProjectedField_With_Extensions()
        {
            // arrange
            IRequestExecutor executor = await new ServiceCollection()
                .AddGraphQL()
                .AddFiltering()
                .EnableRelaySupport()
                .AddSorting()
                .AddProjections()
                .AddQueryType(c => c.Name("Query"))
                .AddTypeExtension<PagingAndProjectionExtension>()
                .AddObjectType<Book>(x =>
                    x.ImplementsNode().IdField(x => x.Id).ResolveNode(x => default!))
                .BuildRequestExecutorAsync();

            // act
            IExecutionResult result = await executor.ExecuteAsync(
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
            executor.Schema.Print().MatchSnapshot(new SnapshotNameExtension("Schema"));
            result.ToJson().MatchSnapshot(new SnapshotNameExtension("Result"));
        }

        [Fact]
        public async Task
            ExecuteAsync_Should_ProjectAndPage_When_AliasIsSameAsAlwaysProjectedField()
        {
            // arrange
            IRequestExecutor executor = await new ServiceCollection()
                .AddGraphQL()
                .AddFiltering()
                .EnableRelaySupport()
                .AddSorting()
                .AddProjections()
                .AddQueryType(c => c.Name("Query"))
                .AddTypeExtension<PagingAndProjectionExtension>()
                .AddObjectType<Book>(x =>
                    x.ImplementsNode().IdField(x => x.Id).ResolveNode(x => default!))
                .BuildRequestExecutorAsync();

            // act
            IExecutionResult result = await executor.ExecuteAsync(
                @"
                {
                    books {
                        nodes {
                            authorId: title
                        }
                    }
                }
                ");

            // assert
            executor.Schema.Print().MatchSnapshot(new SnapshotNameExtension("Schema"));
            result.ToJson().MatchSnapshot(new SnapshotNameExtension("Result"));
        }

        [Fact]
        public async Task CreateSchema_CodeFirst_AsyncQueryable()
        {
            // arrange
            IRequestExecutor executor = await new ServiceCollection()
                .AddGraphQL()
                .AddFiltering()
                .AddQueryType<FooType>()
                .BuildRequestExecutorAsync();

            // act
            IExecutionResult result = await executor.ExecuteAsync(
                @"
                {
                    foos(where: { qux: {eq: ""a""}}) {
                        qux
                    }
                }
                ");

            // assert
            executor.Schema.Print().MatchSnapshot(new SnapshotNameExtension("Schema"));
            result.ToJson().MatchSnapshot(new SnapshotNameExtension("Result"));
        }

        [Fact]
        public async Task CreateSchema_OnDifferentScope()
        {
            // arrange
            IRequestExecutor executor = await new ServiceCollection()
                .AddGraphQL()
                .AddFiltering("Foo")
                .AddSorting("Foo")
                .AddProjections("Foo")
                .AddQueryType<DifferentScope>()
                .BuildRequestExecutorAsync();

            // act
            IExecutionResult result = await executor.ExecuteAsync(
                @"
                {
                    books(where: { title: {eq: ""BookTitle""}}) {
                        nodes { title }
                    }
                }
                ");

            // assert
            executor.Schema.Print().MatchSnapshot(new SnapshotNameExtension("Schema"));
            result.ToJson().MatchSnapshot(new SnapshotNameExtension("Result"));
        }

        [Fact]
        public async Task Execute_And_OnRoot()
        {
            // arrange
            IRequestExecutor executor = await new ServiceCollection()
                .AddGraphQL()
                .AddFiltering("Foo")
                .AddSorting("Foo")
                .AddProjections("Foo")
                .AddQueryType<DifferentScope>()
                .BuildRequestExecutorAsync();

            // act
            IExecutionResult result = await executor.ExecuteAsync(
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
            executor.Schema.Print().MatchSnapshot(new SnapshotNameExtension("Schema"));
            result.ToJson().MatchSnapshot(new SnapshotNameExtension("Result"));
        }

        [Fact]
        public async Task Execute_And_OnRoot_Reverse()
        {
            // arrange
            IRequestExecutor executor = await new ServiceCollection()
                .AddGraphQL()
                .AddFiltering("Foo")
                .AddSorting("Foo")
                .AddProjections("Foo")
                .AddQueryType<DifferentScope>()
                .BuildRequestExecutorAsync();

            // act
            IExecutionResult result = await executor.ExecuteAsync(
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
            executor.Schema.Print().MatchSnapshot(new SnapshotNameExtension("Schema"));
            result.ToJson().MatchSnapshot(new SnapshotNameExtension("Result"));
        }

        public class FooType : ObjectType
        {
            protected override void Configure(IObjectTypeDescriptor descriptor)
            {
                descriptor
                    .Field("foos")
                    .Type<ListType<ObjectType<Bar>>>()
                    .Resolver(_ =>
                    {
                        IQueryable<Bar> data = new Bar[]
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
            public string Qux { get; set; }

            public static Bar Create(string qux) => new() { Qux = qux };
        }

        public class PagingAndProjection
        {
            [UsePaging]
            [UseProjection]
            public IQueryable<Book> GetBooks() => new[]
            {
                new Book { Id = 1, Title = "BookTitle", Author = new Author { Name = "Author" } }
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
                new Book { Id = 1, Title = "BookTitle", Author = new Author { Name = "Author" } }
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
    }
}
