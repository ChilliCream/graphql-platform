using HotChocolate.Execution;
using HotChocolate.Execution.Processing;
using HotChocolate.Types;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Text.Json;

namespace HotChocolate.Data;

public class IntegrationTests : IClassFixture<AuthorFixture>
{
    private readonly DbSet<Author> _authors;
    private readonly DbSet<SingleOrDefaultAuthor> _singleOrDefaultAuthors;
    private readonly DbSet<ZeroAuthor> _zeroAuthors;

    public IntegrationTests(AuthorFixture authorFixture)
    {
        _authors = authorFixture.Context.Authors;
        _zeroAuthors = authorFixture.Context.ZeroAuthors;
        _singleOrDefaultAuthors = authorFixture.Context.SingleOrDefaultAuthors;
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
                    .Resolve(_authors))
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
                    .UseSingleOrDefault())
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
    public async Task ExecuteAsync_Should_ReturnNull_When_SingleOrDefaultZero()
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
                    .UseFirstOrDefault())
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
    public async Task ExecuteAsync_Should_ReturnNull_When_FirstOrDefaultZero()
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
                    .Type<ObjectType<ZeroAuthor>>()
                    .Resolve(_zeroAuthors.Take(0))
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
    public async Task OffsetPagingExecutable()
    {
        // arrange
        // act
        var executor = await new ServiceCollection()
            .AddDbContextPool<BookContext>(
                b => b.UseInMemoryDatabase("Data Source=EF.OffsetPagingExecutable.db"))
            .AddGraphQL()
            .AddFiltering()
            .AddSorting()
            .AddProjections()
            .AddQueryType<Query>()
            .BuildRequestExecutorAsync();

        // assert
        var result = await executor.ExecuteAsync(
            """
            query Test {
                authorOffsetPagingExecutable {
                    items {
                        name
                    }
                    pageInfo {
                        hasNextPage
                        hasPreviousPage
                    }
                }
            }
            """);

        // assert
        result.MatchSnapshot();
    }

    [Fact]
    public async Task ExecuteAsync_Should_ReturnAllItems_When_ToListAsync_AsyncEnumerable()
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
                    .Resolve(Executable.From(_authors))
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
    public async Task ExecuteAsync_Should_OnlyOneItem_When_SingleOrDefault_AsyncEnumerable()
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
                    .Type<ObjectType<SingleOrDefaultAuthor>>()
                    .Resolve(Executable.From(_singleOrDefaultAuthors))
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
    public async Task ExecuteAsync_Should_Fail_When_SingleOrDefaultMoreThanOne_AsyncEnumerable()
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
                    .Resolve(Executable.From(_authors))
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
    public async Task ExecuteAsync_Should_ReturnNull_When_SingleOrDefaultZero_AsyncEnumerable()
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
                    .Type<ObjectType<ZeroAuthor>>()
                    .Resolve(Executable.From(_zeroAuthors))
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
    public async Task ExecuteAsync_Should_OnlyOneItem_When_FirstOrDefault_AsyncEnumerable()
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
                    .Resolve(Executable.From(_authors))
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
    public async Task ExecuteAsync_Should_ReturnNull_When_FirstOrDefaultZero_AsyncEnumerable()
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
                    .Type<ObjectType<ZeroAuthor>>()
                    .Resolve(Executable.From(_zeroAuthors))
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
    public async Task UseSingleOrDefault_Should_Respect_Explicit_Field_Type()
    {
        // arrange
        var users = new SingleOrDefaultUser[]
        {
            new SingleOrDefaultActiveUser
            {
                Name = "Alice",
                IsActive = true
            }
        }.AsQueryable();

        var executor = await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType(
                x => x
                    .Name("Query")
                    .Field("user")
                    .Type<ObjectType<SingleOrDefaultActiveUser>>()
                    .Resolve(users)
                    .UseSingleOrDefault())
            .BuildRequestExecutorAsync();

        // act
        var result = await executor.ExecuteAsync(
            """
            {
                user {
                    name
                    isActive
                }
            }
            """);

        // assert
        result.MatchSnapshot();
    }

    [Fact]
    public async Task UseProjection_Should_Preserve_Entity_Constructor_DbContext_Injection()
    {
        var databaseName = $"db-{Guid.NewGuid():N}";

        await using (var seedContext = new ConstructorInjectionDbContext(
            new DbContextOptionsBuilder<ConstructorInjectionDbContext>()
                .UseInMemoryDatabase(databaseName)
                .Options))
        {
            await seedContext.Database.EnsureCreatedAsync();

            var blog1 = new ConstructorInjectionBlog { Name = "Blog1" };
            var blog2 = new ConstructorInjectionBlog { Name = "Blog2" };

            await seedContext.Blogs.AddRangeAsync(blog1, blog2);
            await seedContext.SaveChangesAsync();

            await seedContext.Posts.AddRangeAsync(
                new ConstructorInjectionPost { BlogId = blog1.Id },
                new ConstructorInjectionPost { BlogId = blog1.Id },
                new ConstructorInjectionPost { BlogId = blog2.Id });
            await seedContext.SaveChangesAsync();
        }

        var executor = await new ServiceCollection()
            .AddDbContext<ConstructorInjectionDbContext>(
                b => b.UseInMemoryDatabase(databaseName))
            .AddGraphQL()
            .AddProjections()
            .AddQueryType<ConstructorInjectionQuery>()
            .BuildRequestExecutorAsync();

        var result = await executor.ExecuteAsync(
            """
            {
                blogs {
                    name
                    postCount
                }
                blogsNoProjection {
                    name
                    postCount
                }
            }
            """);

        var operationResult = result.ExpectOperationResult();
        Assert.True(operationResult.Errors is null || operationResult.Errors.Count == 0);
        Assert.True(operationResult.Data.HasValue);

        using var document = JsonDocument.Parse(result.ToJson());
        var data = document.RootElement.GetProperty("data");
        var projectedCounts = ReadCounts(data.GetProperty("blogs"));
        var unprojectedCounts = ReadCounts(data.GetProperty("blogsNoProjection"));

        Assert.Equal(unprojectedCounts, projectedCounts);
        Assert.Equal(3, projectedCounts["Blog1"]);
        Assert.Equal(3, projectedCounts["Blog2"]);

        static Dictionary<string, int> ReadCounts(JsonElement value)
        {
            var result = new Dictionary<string, int>();

            foreach (var item in value.EnumerateArray())
            {
                result.Add(item.GetProperty("name").GetString()!, item.GetProperty("postCount").GetInt32());
            }

            return result;
        }
    }

    [Fact]
    public async Task AsSelector_Should_Preserve_Entity_Constructor_DbContext_Injection()
    {
        var databaseName = $"db-{Guid.NewGuid():N}";

        await using (var seedContext = new ConstructorInjectionDbContext(
            new DbContextOptionsBuilder<ConstructorInjectionDbContext>()
                .UseInMemoryDatabase(databaseName)
                .Options))
        {
            await seedContext.Database.EnsureCreatedAsync();

            var blog1 = new ConstructorInjectionBlog { Name = "Blog1" };
            var blog2 = new ConstructorInjectionBlog { Name = "Blog2" };

            await seedContext.Blogs.AddRangeAsync(blog1, blog2);
            await seedContext.SaveChangesAsync();

            await seedContext.Posts.AddRangeAsync(
                new ConstructorInjectionPost { BlogId = blog1.Id },
                new ConstructorInjectionPost { BlogId = blog1.Id },
                new ConstructorInjectionPost { BlogId = blog2.Id });
            await seedContext.SaveChangesAsync();
        }

        var executor = await new ServiceCollection()
            .AddDbContext<ConstructorInjectionDbContext>(
                b => b.UseInMemoryDatabase(databaseName))
            .AddGraphQL()
            .AddProjections()
            .AddQueryType<ConstructorInjectionQuery>()
            .BuildRequestExecutorAsync();

        var result = await executor.ExecuteAsync(
            """
            {
                blogsAsSelector {
                    name
                    postCount
                }
                blogsNoProjection {
                    name
                    postCount
                }
            }
            """);

        var operationResult = result.ExpectOperationResult();
        Assert.True(operationResult.Errors is null || operationResult.Errors.Count == 0);
        Assert.True(operationResult.Data.HasValue);

        using var document = JsonDocument.Parse(result.ToJson());
        var data = document.RootElement.GetProperty("data");
        var projectedCounts = ReadCounts(data.GetProperty("blogsAsSelector"));
        var unprojectedCounts = ReadCounts(data.GetProperty("blogsNoProjection"));

        Assert.Equal(unprojectedCounts, projectedCounts);
        Assert.Equal(3, projectedCounts["Blog1"]);
        Assert.Equal(3, projectedCounts["Blog2"]);

        static Dictionary<string, int> ReadCounts(JsonElement value)
        {
            var result = new Dictionary<string, int>();

            foreach (var item in value.EnumerateArray())
            {
                result.Add(item.GetProperty("name").GetString()!, item.GetProperty("postCount").GetInt32());
            }

            return result;
        }
    }

    public class ConstructorInjectionQuery
    {
        [UseProjection]
        public IQueryable<ConstructorInjectionBlog> GetBlogs(ConstructorInjectionDbContext context)
            => context.Blogs;

        public IQueryable<ConstructorInjectionBlog> GetBlogsAsSelector(
            ConstructorInjectionDbContext context,
            ISelection selection)
            => context.Blogs.Select(selection.AsSelector<ConstructorInjectionBlog>());

        public IQueryable<ConstructorInjectionBlog> GetBlogsNoProjection(
            ConstructorInjectionDbContext context)
            => context.Blogs;
    }

    public class ConstructorInjectionDbContext(
        DbContextOptions<ConstructorInjectionDbContext> options)
        : DbContext(options)
    {
        public DbSet<ConstructorInjectionBlog> Blogs => Set<ConstructorInjectionBlog>();

        public DbSet<ConstructorInjectionPost> Posts => Set<ConstructorInjectionPost>();
    }

    public class ConstructorInjectionBlog
    {
        public ConstructorInjectionBlog()
        {
        }

        private ConstructorInjectionBlog(ConstructorInjectionDbContext context)
        {
            Context = context;
        }

        private ConstructorInjectionDbContext? Context { get; }

        public int Id { get; set; }

        public string Name { get; set; } = default!;

        public int PostCount => Context?.Posts.Count() ?? 0;
    }

    public class ConstructorInjectionPost
    {
        public int Id { get; set; }

        public int BlogId { get; set; }
    }

    public class SingleOrDefaultUser
    {
        public string Name { get; set; } = string.Empty;
    }

    public class SingleOrDefaultActiveUser : SingleOrDefaultUser
    {
        public bool IsActive { get; set; }
    }
}
