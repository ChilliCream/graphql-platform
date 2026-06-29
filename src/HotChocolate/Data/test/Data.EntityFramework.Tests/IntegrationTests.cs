using System.Data.Common;
using System.Linq.Expressions;
using System.Text.Json;
using CookieCrumble;
using HotChocolate.Execution;
using HotChocolate.Execution.Processing;
using HotChocolate.Resolvers;
using HotChocolate.Types;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;

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
    public async Task Projection_Should_ProjectRequiredNavigation_When_ParentRequiresObject()
    {
        // arrange
        var fileName = Guid.NewGuid().ToString("N") + ".db";
        var connectionString = "Data Source=" + fileName;
        var sql = new List<string>();

        try
        {
            await using (var seed = new BookContext(
                new DbContextOptionsBuilder<BookContext>().UseSqlite(connectionString).Options))
            {
                await seed.Database.EnsureCreatedAsync(Xunit.TestContext.Current.CancellationToken);
                seed.Authors.Add(
                    new Author { Id = 1, Name = "Foo", Books = { new Book { Id = 1, Title = "Foo1" } } });
                await seed.SaveChangesAsync(Xunit.TestContext.Current.CancellationToken);
            }

            var result = await new ServiceCollection()
                .AddDbContext<BookContext>(
                    b => b
                        .UseSqlite(connectionString)
                        .AddInterceptors(new SqlCapturingInterceptor(sql)))
                .AddGraphQL()
                .AddProjections()
                .AddQueryType(
                    d => d
                        .Name("Query")
                        .Field("books")
                        .Resolve(ctx => ctx.Service<BookContext>().Books)
                        .UseProjection())
                .AddObjectType<Book>(
                    d =>
                    {
                        d.Field(b => b.Title);
                        d.Field("authorInfo")
                            .Type<ObjectType<Author>>()
                            .Resolve(ctx => ctx.Parent<Book>().Author)
                            .ParentRequires<Book>(b => b.Author!);
                    })
                .ModifyRequestOptions(o => o.IncludeExceptionDetails = true)
                .ExecuteRequestAsync(
                    "{ books { title authorInfo { name } } }",
                    cancellationToken: Xunit.TestContext.Current.CancellationToken);

            // assert
            // The SQL must join and select the Authors columns, proving the required navigation
            // is projected from the database rather than relying on an in-memory object graph.
            Snapshot
                .Create(
                    postFix: TestEnvironment.TargetFramework == "NET10_0"
                        ? TestEnvironment.TargetFramework
                        : null)
                .Add(string.Join("\n", sql), "SQL")
                .Add(result, "Result")
                .MatchMarkdownSnapshot();
        }
        finally
        {
            Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
            File.Delete(fileName);
        }
    }

    private sealed class SqlCapturingInterceptor(List<string> queries) : DbCommandInterceptor
    {
        public override InterceptionResult<DbDataReader> ReaderExecuting(
            DbCommand command,
            CommandEventData eventData,
            InterceptionResult<DbDataReader> result)
        {
            queries.Add(command.CommandText);
            return base.ReaderExecuting(command, eventData, result);
        }

        public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
            DbCommand command,
            CommandEventData eventData,
            InterceptionResult<DbDataReader> result,
            CancellationToken cancellationToken = default)
        {
            queries.Add(command.CommandText);
            return base.ReaderExecutingAsync(command, eventData, result, cancellationToken);
        }
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
            .BuildRequestExecutorAsync(cancellationToken: Xunit.TestContext.Current.CancellationToken);

        // act
        var result = await executor.ExecuteAsync(
            """
            {
              executable {
                name
              }
            }
            """,
            Xunit.TestContext.Current.CancellationToken);

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
            .BuildRequestExecutorAsync(cancellationToken: Xunit.TestContext.Current.CancellationToken);

        // act
        var result = await executor.ExecuteAsync(
            """
            {
                executable {
                    name
                }
            }
            """,
            Xunit.TestContext.Current.CancellationToken);

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
            .BuildRequestExecutorAsync(cancellationToken: Xunit.TestContext.Current.CancellationToken);

        // act
        var result = await executor.ExecuteAsync(
            """
            {
                executable {
                    name
                }
            }
            """,
            Xunit.TestContext.Current.CancellationToken);

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
            .BuildRequestExecutorAsync(cancellationToken: Xunit.TestContext.Current.CancellationToken);

        // act
        var result = await executor.ExecuteAsync(
            """
            {
                executable {
                    name
                }
            }
            """,
            Xunit.TestContext.Current.CancellationToken);

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
            .BuildRequestExecutorAsync(cancellationToken: Xunit.TestContext.Current.CancellationToken);

        // act
        var result = await executor.ExecuteAsync(
            """
            {
                executable {
                    name
                }
            }
            """,
            Xunit.TestContext.Current.CancellationToken);

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
            .BuildRequestExecutorAsync(cancellationToken: Xunit.TestContext.Current.CancellationToken);

        // act
        var result = await executor.ExecuteAsync(
            """
            {
                executable {
                    name
                }
            }
            """,
            Xunit.TestContext.Current.CancellationToken);

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
            .BuildRequestExecutorAsync(cancellationToken: Xunit.TestContext.Current.CancellationToken);

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
            """,
            Xunit.TestContext.Current.CancellationToken);

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
            .BuildRequestExecutorAsync(cancellationToken: Xunit.TestContext.Current.CancellationToken);

        // act
        var result = await executor.ExecuteAsync(
            """
            {
                executable {
                    name
                }
            }
            """,
            Xunit.TestContext.Current.CancellationToken);

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
            .BuildRequestExecutorAsync(cancellationToken: Xunit.TestContext.Current.CancellationToken);

        // act
        var result = await executor.ExecuteAsync(
            """
            {
                executable {
                    name
                }
            }
            """,
            Xunit.TestContext.Current.CancellationToken);

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
            .BuildRequestExecutorAsync(cancellationToken: Xunit.TestContext.Current.CancellationToken);

        // act
        var result = await executor.ExecuteAsync(
            """
            {
                executable {
                    name
                }
            }
            """,
            Xunit.TestContext.Current.CancellationToken);

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
            .BuildRequestExecutorAsync(cancellationToken: Xunit.TestContext.Current.CancellationToken);

        // act
        var result = await executor.ExecuteAsync(
            """
            {
                executable {
                    name
                }
            }
            """,
            Xunit.TestContext.Current.CancellationToken);

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
            .BuildRequestExecutorAsync(cancellationToken: Xunit.TestContext.Current.CancellationToken);

        // act
        var result = await executor.ExecuteAsync(
            """
            {
                executable {
                    name
                }
            }
            """,
            Xunit.TestContext.Current.CancellationToken);

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
            .BuildRequestExecutorAsync(cancellationToken: Xunit.TestContext.Current.CancellationToken);

        // act
        var result = await executor.ExecuteAsync(
            """
            {
                executable {
                    name
                }
            }
            """,
            Xunit.TestContext.Current.CancellationToken);

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
            .BuildRequestExecutorAsync(cancellationToken: Xunit.TestContext.Current.CancellationToken);

        // act
        var result = await executor.ExecuteAsync(
            """
            {
                user {
                    name
                    isActive
                }
            }
            """,
            Xunit.TestContext.Current.CancellationToken);

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
            await seedContext.Database.EnsureCreatedAsync(Xunit.TestContext.Current.CancellationToken);

            var blog1 = new ConstructorInjectionBlog { Name = "Blog1" };
            var blog2 = new ConstructorInjectionBlog { Name = "Blog2" };

            await seedContext.Blogs.AddRangeAsync(blog1, blog2);
            await seedContext.SaveChangesAsync(Xunit.TestContext.Current.CancellationToken);

            await seedContext.Posts.AddRangeAsync(
                new ConstructorInjectionPost { BlogId = blog1.Id },
                new ConstructorInjectionPost { BlogId = blog1.Id },
                new ConstructorInjectionPost { BlogId = blog2.Id });
            await seedContext.SaveChangesAsync(Xunit.TestContext.Current.CancellationToken);
        }

        var executor = await new ServiceCollection()
            .AddDbContext<ConstructorInjectionDbContext>(
                b => b.UseInMemoryDatabase(databaseName))
            .AddGraphQL()
            .AddProjections()
            .AddQueryType<ConstructorInjectionQuery>()
            .BuildRequestExecutorAsync(cancellationToken: Xunit.TestContext.Current.CancellationToken);

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
            """,
            Xunit.TestContext.Current.CancellationToken);

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
            await seedContext.Database.EnsureCreatedAsync(Xunit.TestContext.Current.CancellationToken);

            var blog1 = new ConstructorInjectionBlog { Name = "Blog1" };
            var blog2 = new ConstructorInjectionBlog { Name = "Blog2" };

            await seedContext.Blogs.AddRangeAsync(blog1, blog2);
            await seedContext.SaveChangesAsync(Xunit.TestContext.Current.CancellationToken);

            await seedContext.Posts.AddRangeAsync(
                new ConstructorInjectionPost { BlogId = blog1.Id },
                new ConstructorInjectionPost { BlogId = blog1.Id },
                new ConstructorInjectionPost { BlogId = blog2.Id });
            await seedContext.SaveChangesAsync(Xunit.TestContext.Current.CancellationToken);
        }

        var executor = await new ServiceCollection()
            .AddDbContext<ConstructorInjectionDbContext>(
                b => b.UseInMemoryDatabase(databaseName))
            .AddGraphQL()
            .AddProjections()
            .AddQueryType<ConstructorInjectionQuery>()
            .BuildRequestExecutorAsync(cancellationToken: Xunit.TestContext.Current.CancellationToken);

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
            """,
            Xunit.TestContext.Current.CancellationToken);

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

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task AsSelector_Should_Project_Conditional_Child_When_Include_Flag_Is_Set(bool include)
    {
        // arrange
        var dbFile = System.IO.Path.Combine(
            System.IO.Path.GetTempPath(),
            $"conditional-projection-{Guid.NewGuid():N}.db");
        var connectionString = $"Data Source={dbFile}";

        try
        {
            var sqlCapture = new ConditionalSqlCapture();

            await using var services = new ServiceCollection()
                .AddSingleton(sqlCapture)
                .AddDbContext<ConditionalDbContext>(b => b.UseSqlite(connectionString))
                .AddGraphQL()
                .AddProjectionSelectorCache()
                .AddQueryType<ConditionalQuery>()
                .AddType<ConditionalTenantType>()
                .ModifyRequestOptions(o => o.IncludeExceptionDetails = true)
                .Services
                .BuildServiceProvider();

            await using (var scope = services.CreateAsyncScope())
            {
                await using var context = scope.ServiceProvider.GetRequiredService<ConditionalDbContext>();
                await context.Database.EnsureCreatedAsync(Xunit.TestContext.Current.CancellationToken);

                context.Tenants.Add(
                    new ConditionalTenant
                    {
                        Id = 1,
                        Name = "Acme",
                        Workspaces =
                        [
                            new ConditionalWorkspace { Id = 1, Name = "Alpha" },
                            new ConditionalWorkspace { Id = 2, Name = "Beta" }
                        ]
                    });

                await context.SaveChangesAsync(Xunit.TestContext.Current.CancellationToken);
            }

            var executor = await services
                .GetRequiredService<IRequestExecutorProvider>()
                .GetExecutorAsync(cancellationToken: Xunit.TestContext.Current.CancellationToken);

            // act
            // the workspaces relation carries a variable based @include directive, which makes
            // the child selection conditional. when included, the relation must be projected and
            // present in the SQL. when excluded, the relation must not be projected (no over-fetch).
            var result = await executor.ExecuteAsync(
                OperationRequestBuilder.New()
                    .SetDocument(
                        """
                        query($if: Boolean!) {
                          tenants {
                            id
                            workspaces @include(if: $if) {
                              id
                            }
                          }
                        }
                        """)
                    .SetVariableValues(new Dictionary<string, object?> { ["if"] = include })
                    .Build(),
                Xunit.TestContext.Current.CancellationToken);

            // assert
            await new Snapshot(postFix: include ? "include_true" : "include_false")
                .Add(result, "Result")
                .Add(sqlCapture.Sql ?? "<none>", "SQL")
                .MatchMarkdownAsync(Xunit.TestContext.Current.CancellationToken);
        }
        finally
        {
            if (File.Exists(dbFile))
            {
                File.Delete(dbFile);
            }
        }
    }

    [Fact]
    public async Task AsSelector_Should_Project_Child_When_Parent_Include_Is_True()
    {
        // arrange
        var dbFile = System.IO.Path.Combine(
            System.IO.Path.GetTempPath(),
            $"conditional-projection-{Guid.NewGuid():N}.db");
        var connectionString = $"Data Source={dbFile}";

        try
        {
            var sqlCapture = new ConditionalSqlCapture();

            await using var services = new ServiceCollection()
                .AddSingleton(sqlCapture)
                .AddDbContext<ConditionalDbContext>(b => b.UseSqlite(connectionString))
                .AddGraphQL()
                .AddProjectionSelectorCache()
                .AddQueryType<ConditionalQuery>()
                .AddType<ConditionalTenantType>()
                .ModifyRequestOptions(o => o.IncludeExceptionDetails = true)
                .Services
                .BuildServiceProvider();

            await using (var scope = services.CreateAsyncScope())
            {
                await using var context = scope.ServiceProvider.GetRequiredService<ConditionalDbContext>();
                await context.Database.EnsureCreatedAsync(Xunit.TestContext.Current.CancellationToken);

                context.Tenants.Add(
                    new ConditionalTenant
                    {
                        Id = 1,
                        Name = "Acme",
                        Workspaces =
                        [
                            new ConditionalWorkspace { Id = 1, Name = "Alpha" },
                            new ConditionalWorkspace { Id = 2, Name = "Beta" }
                        ]
                    });

                await context.SaveChangesAsync(Xunit.TestContext.Current.CancellationToken);
            }

            var executor = await services
                .GetRequiredService<IRequestExecutorProvider>()
                .GetExecutorAsync(cancellationToken: Xunit.TestContext.Current.CancellationToken);

            // act
            // the parent field carries a variable based @include directive, which makes the
            // selection conditional. the unconditional workspaces child inherits the parent's
            // path include-flag and must still be projected (relation present in the SQL).
            var result = await executor.ExecuteAsync(
                OperationRequestBuilder.New()
                    .SetDocument(
                        """
                        query($if: Boolean!) {
                          tenants @include(if: $if) {
                            id
                            workspaces {
                              id
                            }
                          }
                        }
                        """)
                    .SetVariableValues(new Dictionary<string, object?> { ["if"] = true })
                    .Build(),
                Xunit.TestContext.Current.CancellationToken);

            // assert
            await new Snapshot()
                .Add(result, "Result")
                .Add(sqlCapture.Sql ?? "<none>", "SQL")
                .MatchMarkdownAsync(Xunit.TestContext.Current.CancellationToken);
        }
        finally
        {
            if (File.Exists(dbFile))
            {
                File.Delete(dbFile);
            }
        }
    }

    [Fact]
    public async Task AsSelector_Should_Not_Invoke_Resolver_When_Parent_Include_Is_False()
    {
        // arrange
        var dbFile = System.IO.Path.Combine(
            System.IO.Path.GetTempPath(),
            $"conditional-projection-{Guid.NewGuid():N}.db");
        var connectionString = $"Data Source={dbFile}";

        try
        {
            var sqlCapture = new ConditionalSqlCapture();

            await using var services = new ServiceCollection()
                .AddSingleton(sqlCapture)
                .AddDbContext<ConditionalDbContext>(b => b.UseSqlite(connectionString))
                .AddGraphQL()
                .AddProjectionSelectorCache()
                .AddQueryType<ConditionalQuery>()
                .AddType<ConditionalTenantType>()
                .ModifyRequestOptions(o => o.IncludeExceptionDetails = true)
                .Services
                .BuildServiceProvider();

            await using (var scope = services.CreateAsyncScope())
            {
                await using var context = scope.ServiceProvider.GetRequiredService<ConditionalDbContext>();
                await context.Database.EnsureCreatedAsync(Xunit.TestContext.Current.CancellationToken);

                context.Tenants.Add(
                    new ConditionalTenant
                    {
                        Id = 1,
                        Name = "Acme",
                        Workspaces =
                        [
                            new ConditionalWorkspace { Id = 1, Name = "Alpha" },
                            new ConditionalWorkspace { Id = 2, Name = "Beta" }
                        ]
                    });

                await context.SaveChangesAsync(Xunit.TestContext.Current.CancellationToken);
            }

            var executor = await services
                .GetRequiredService<IRequestExecutorProvider>()
                .GetExecutorAsync(cancellationToken: Xunit.TestContext.Current.CancellationToken);

            // act
            // the parent field is excluded at the top level, so the resolver is never invoked
            // and the tenants field is absent from the result.
            var result = await executor.ExecuteAsync(
                OperationRequestBuilder.New()
                    .SetDocument(
                        """
                        query($if: Boolean!) {
                          tenants @include(if: $if) {
                            id
                            workspaces {
                              id
                            }
                          }
                        }
                        """)
                    .SetVariableValues(new Dictionary<string, object?> { ["if"] = false })
                    .Build(),
                Xunit.TestContext.Current.CancellationToken);

            // assert
            var operationResult = result.ExpectOperationResult();
            using var document = JsonDocument.Parse(operationResult.ToJson());
            Assert.Empty(operationResult.Errors ?? []);
            Assert.Null(sqlCapture.Sql);
            Assert.False(
                document.RootElement.GetProperty("data").TryGetProperty("tenants", out _),
                operationResult.ToJson());
        }
        finally
        {
            if (File.Exists(dbFile))
            {
                File.Delete(dbFile);
            }
        }
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task AsSelector_Should_Project_Conditional_Child_When_No_Flags_Are_Passed(bool include)
    {
        // arrange
        var dbFile = System.IO.Path.Combine(
            System.IO.Path.GetTempPath(),
            $"conditional-projection-{Guid.NewGuid():N}.db");
        var connectionString = $"Data Source={dbFile}";

        try
        {
            var sqlCapture = new ConditionalSqlCapture();

            await using var services = new ServiceCollection()
                .AddSingleton(sqlCapture)
                .AddDbContext<ConditionalDbContext>(b => b.UseSqlite(connectionString))
                .AddGraphQL()
                .AddProjectionSelectorCache()
                .AddQueryType<ConditionalQuery>()
                .AddType<ConditionalTenantType>()
                .ModifyRequestOptions(o => o.IncludeExceptionDetails = true)
                .Services
                .BuildServiceProvider();

            await using (var scope = services.CreateAsyncScope())
            {
                await using var context = scope.ServiceProvider.GetRequiredService<ConditionalDbContext>();
                await context.Database.EnsureCreatedAsync(Xunit.TestContext.Current.CancellationToken);

                context.Tenants.Add(
                    new ConditionalTenant
                    {
                        Id = 1,
                        Name = "Acme",
                        Workspaces =
                        [
                            new ConditionalWorkspace { Id = 1, Name = "Alpha" },
                            new ConditionalWorkspace { Id = 2, Name = "Beta" }
                        ]
                    });

                await context.SaveChangesAsync(Xunit.TestContext.Current.CancellationToken);
            }

            var executor = await services
                .GetRequiredService<IRequestExecutorProvider>()
                .GetExecutorAsync(cancellationToken: Xunit.TestContext.Current.CancellationToken);

            // act
            // the resolver builds the selector without runtime include flags, so the runtime
            // inclusion of the conditional workspaces relation is unknown. the relation must
            // always be projected (relation present in the SQL) so that no data is missing
            // when the relation is included. the result must only contain the relation when
            // it is included.
            var result = await executor.ExecuteAsync(
                OperationRequestBuilder.New()
                    .SetDocument(
                        """
                        query($if: Boolean!) {
                          tenantsWithDefaultSelector {
                            id
                            workspaces @include(if: $if) {
                              id
                            }
                          }
                        }
                        """)
                    .SetVariableValues(new Dictionary<string, object?> { ["if"] = include })
                    .Build(),
                Xunit.TestContext.Current.CancellationToken);

            // assert
            await new Snapshot(postFix: include ? "include_true" : "include_false")
                .Add(result, "Result")
                .Add(sqlCapture.Sql ?? "<none>", "SQL")
                .MatchMarkdownAsync(Xunit.TestContext.Current.CancellationToken);
        }
        finally
        {
            if (File.Exists(dbFile))
            {
                File.Delete(dbFile);
            }
        }
    }

    [Fact]
    public async Task AsSelector_Should_Project_Child_When_Parent_Is_Conditional_And_No_Flags_Are_Passed()
    {
        // arrange
        var dbFile = System.IO.Path.Combine(
            System.IO.Path.GetTempPath(),
            $"conditional-projection-{Guid.NewGuid():N}.db");
        var connectionString = $"Data Source={dbFile}";

        try
        {
            var sqlCapture = new ConditionalSqlCapture();

            await using var services = new ServiceCollection()
                .AddSingleton(sqlCapture)
                .AddDbContext<ConditionalDbContext>(b => b.UseSqlite(connectionString))
                .AddGraphQL()
                .AddProjectionSelectorCache()
                .AddQueryType<ConditionalQuery>()
                .AddType<ConditionalTenantType>()
                .ModifyRequestOptions(o => o.IncludeExceptionDetails = true)
                .Services
                .BuildServiceProvider();

            await using (var scope = services.CreateAsyncScope())
            {
                await using var context = scope.ServiceProvider.GetRequiredService<ConditionalDbContext>();
                await context.Database.EnsureCreatedAsync(Xunit.TestContext.Current.CancellationToken);

                context.Tenants.Add(
                    new ConditionalTenant
                    {
                        Id = 1,
                        Name = "Acme",
                        Workspaces =
                        [
                            new ConditionalWorkspace { Id = 1, Name = "Alpha" },
                            new ConditionalWorkspace { Id = 2, Name = "Beta" }
                        ]
                    });

                await context.SaveChangesAsync(Xunit.TestContext.Current.CancellationToken);
            }

            var executor = await services
                .GetRequiredService<IRequestExecutorProvider>()
                .GetExecutorAsync(cancellationToken: Xunit.TestContext.Current.CancellationToken);

            // act
            // the parent field carries a variable based @include directive and the resolver
            // builds the selector without runtime include flags. the children inherit the
            // parent's include condition and must still be projected, since the resolver
            // only runs when the parent is included.
            var result = await executor.ExecuteAsync(
                OperationRequestBuilder.New()
                    .SetDocument(
                        """
                        query($if: Boolean!) {
                          tenantsWithDefaultSelector @include(if: $if) {
                            id
                            workspaces {
                              id
                            }
                          }
                        }
                        """)
                    .SetVariableValues(new Dictionary<string, object?> { ["if"] = true })
                    .Build(),
                Xunit.TestContext.Current.CancellationToken);

            // assert
            await new Snapshot()
                .Add(result, "Result")
                .Add(sqlCapture.Sql ?? "<none>", "SQL")
                .MatchMarkdownAsync(Xunit.TestContext.Current.CancellationToken);
        }
        finally
        {
            if (File.Exists(dbFile))
            {
                File.Delete(dbFile);
            }
        }
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task AsSelector_Should_Project_Conditional_Child_When_Paging_Is_Used(bool include)
    {
        // arrange
        var dbFile = System.IO.Path.Combine(
            System.IO.Path.GetTempPath(),
            $"conditional-projection-{Guid.NewGuid():N}.db");
        var connectionString = $"Data Source={dbFile}";

        try
        {
            var sqlCapture = new ConditionalSqlCapture();

            await using var services = new ServiceCollection()
                .AddSingleton(sqlCapture)
                .AddDbContext<ConditionalDbContext>(b => b.UseSqlite(connectionString))
                .AddGraphQL()
                .AddProjectionSelectorCache()
                .AddQueryType<ConditionalQuery>()
                .AddType<ConditionalTenantType>()
                .ModifyRequestOptions(o => o.IncludeExceptionDetails = true)
                .Services
                .BuildServiceProvider();

            await using (var scope = services.CreateAsyncScope())
            {
                await using var context = scope.ServiceProvider.GetRequiredService<ConditionalDbContext>();
                await context.Database.EnsureCreatedAsync(Xunit.TestContext.Current.CancellationToken);

                context.Tenants.Add(
                    new ConditionalTenant
                    {
                        Id = 1,
                        Name = "Acme",
                        Workspaces =
                        [
                            new ConditionalWorkspace { Id = 1, Name = "Alpha" },
                            new ConditionalWorkspace { Id = 2, Name = "Beta" }
                        ]
                    });

                await context.SaveChangesAsync(Xunit.TestContext.Current.CancellationToken);
            }

            var executor = await services
                .GetRequiredService<IRequestExecutorProvider>()
                .GetExecutorAsync(cancellationToken: Xunit.TestContext.Current.CancellationToken);

            // act
            // the projection is created for a connection field, so the selector is built
            // from the nodes selection of the connection. the conditional workspaces
            // relation below the nodes selection must be projected exactly as included.
            var result = await executor.ExecuteAsync(
                OperationRequestBuilder.New()
                    .SetDocument(
                        """
                        query($if: Boolean!) {
                          tenantsPaged {
                            nodes {
                              id
                              workspaces @include(if: $if) {
                                id
                              }
                            }
                          }
                        }
                        """)
                    .SetVariableValues(new Dictionary<string, object?> { ["if"] = include })
                    .Build(),
                Xunit.TestContext.Current.CancellationToken);

            // assert
            await new Snapshot(postFix: include ? "include_true" : "include_false")
                .Add(result, "Result")
                .Add(sqlCapture.Sql ?? "<none>", "SQL")
                .MatchMarkdownAsync(Xunit.TestContext.Current.CancellationToken);
        }
        finally
        {
            if (File.Exists(dbFile))
            {
                File.Delete(dbFile);
            }
        }
    }

    [Fact]
    public async Task AsSelector_Should_Project_Conditional_Child_When_Executor_Is_Reused()
    {
        // arrange
        var dbFile = System.IO.Path.Combine(
            System.IO.Path.GetTempPath(),
            $"conditional-projection-{Guid.NewGuid():N}.db");
        var connectionString = $"Data Source={dbFile}";

        try
        {
            var sqlCapture = new ConditionalSqlCapture();

            await using var services = new ServiceCollection()
                .AddSingleton(sqlCapture)
                .AddDbContext<ConditionalDbContext>(b => b.UseSqlite(connectionString))
                .AddGraphQL()
                .AddProjectionSelectorCache()
                .AddQueryType<ConditionalQuery>()
                .AddType<ConditionalTenantType>()
                .ModifyRequestOptions(o => o.IncludeExceptionDetails = true)
                .Services
                .BuildServiceProvider();

            await using (var scope = services.CreateAsyncScope())
            {
                await using var context = scope.ServiceProvider.GetRequiredService<ConditionalDbContext>();
                await context.Database.EnsureCreatedAsync(Xunit.TestContext.Current.CancellationToken);

                context.Tenants.Add(
                    new ConditionalTenant
                    {
                        Id = 1,
                        Name = "Acme",
                        Workspaces =
                        [
                            new ConditionalWorkspace { Id = 1, Name = "Alpha" },
                            new ConditionalWorkspace { Id = 2, Name = "Beta" }
                        ]
                    });

                await context.SaveChangesAsync(Xunit.TestContext.Current.CancellationToken);
            }

            var executor = await services
                .GetRequiredService<IRequestExecutorProvider>()
                .GetExecutorAsync(cancellationToken: Xunit.TestContext.Current.CancellationToken);

            const string document =
                """
                query($if: Boolean!) {
                  tenants {
                    id
                    workspaces @include(if: $if) {
                      id
                    }
                  }
                }
                """;

            // act
            // the same executor processes the same document with different variable values,
            // so the second request reuses the cached operation. the projection of the second
            // request must not be affected by any state the first request left behind.
            var excludedResult = await executor.ExecuteAsync(
                OperationRequestBuilder.New()
                    .SetDocument(document)
                    .SetVariableValues(new Dictionary<string, object?> { ["if"] = false })
                    .Build(),
                Xunit.TestContext.Current.CancellationToken);
            var excludedSql = sqlCapture.Sql;

            sqlCapture.Sql = null;

            var includedResult = await executor.ExecuteAsync(
                OperationRequestBuilder.New()
                    .SetDocument(document)
                    .SetVariableValues(new Dictionary<string, object?> { ["if"] = true })
                    .Build(),
                Xunit.TestContext.Current.CancellationToken);
            var includedSql = sqlCapture.Sql;

            // assert
            await new Snapshot()
                .Add(excludedResult, "Result include=false")
                .Add(excludedSql ?? "<none>", "SQL include=false")
                .Add(includedResult, "Result include=true")
                .Add(includedSql ?? "<none>", "SQL include=true")
                .MatchMarkdownAsync(Xunit.TestContext.Current.CancellationToken);
        }
        finally
        {
            if (File.Exists(dbFile))
            {
                File.Delete(dbFile);
            }
        }
    }

    [Fact]
    public async Task AsSelector_Should_Reuse_Cached_Selector_When_False_Flags_Repeat()
    {
        // arrange
        await using var context = await CreateConditionalTestContextAsync();

        // act
        await ExecuteConditionalSelectorCaptureRequestAsync(context, false);
        await ExecuteConditionalSelectorCaptureRequestAsync(context, true);
        await ExecuteConditionalSelectorCaptureRequestAsync(context, false);

        // assert
        Assert.Equal(3, context.SelectorCapture.Selectors.Count);
        Assert.Same(context.SelectorCapture.Selectors[0], context.SelectorCapture.Selectors[2]);
        Assert.NotSame(context.SelectorCapture.Selectors[0], context.SelectorCapture.Selectors[1]);
    }

    [Fact]
    public async Task AsSelector_Should_Reuse_Cached_Selector_When_True_Flags_Repeat()
    {
        // arrange
        await using var context = await CreateConditionalTestContextAsync();

        // act
        await ExecuteConditionalSelectorCaptureRequestAsync(context, true);
        await ExecuteConditionalSelectorCaptureRequestAsync(context, false);
        await ExecuteConditionalSelectorCaptureRequestAsync(context, true);

        // assert
        Assert.Equal(3, context.SelectorCapture.Selectors.Count);
        Assert.Same(context.SelectorCapture.Selectors[0], context.SelectorCapture.Selectors[2]);
        Assert.NotSame(context.SelectorCapture.Selectors[0], context.SelectorCapture.Selectors[1]);
    }

    [Fact]
    public async Task AsSelector_Should_Reuse_Selection_Cached_Selector_When_Subtree_Is_Unconditional_In_Conditional_Operation()
    {
        // arrange
        await using var context = await CreateConditionalTestContextAsync();

        const string document =
            """
            query($if: Boolean!) {
              tenantsCapturedUnconditional {
                id
              }
              tenantNames @include(if: $if)
            }
            """;

        // act
        await ExecuteConditionalRequestAsync(context, document, false);
        await ExecuteConditionalRequestAsync(context, document, true);

        // assert
        Assert.Equal(4, context.SelectorCapture.Selectors.Count);
        Assert.Same(context.SelectorCapture.Selectors[0], context.SelectorCapture.Selectors[1]);
        Assert.Same(context.SelectorCapture.Selectors[0], context.SelectorCapture.Selectors[2]);
        Assert.Same(context.SelectorCapture.Selectors[0], context.SelectorCapture.Selectors[3]);
    }

    private static async Task ExecuteConditionalSelectorCaptureRequestAsync(
        ConditionalTestContext context,
        bool include)
        => await ExecuteConditionalRequestAsync(
            context,
            """
            query($if: Boolean!) {
              tenantsCaptured {
                id
                workspaces @include(if: $if) {
                  id
                }
              }
            }
            """,
            include);

    private static async Task ExecuteConditionalRequestAsync(
        ConditionalTestContext context,
        string document,
        bool include)
    {
        var result = await context.Executor.ExecuteAsync(
            OperationRequestBuilder.New()
                .SetDocument(document)
                .SetVariableValues(new Dictionary<string, object?> { ["if"] = include })
                .Build(),
            Xunit.TestContext.Current.CancellationToken);

        var operationResult = result.ExpectOperationResult();
        if (operationResult.Errors is { Count: > 0 })
        {
            throw new InvalidOperationException(operationResult.ToJson());
        }
    }

    private static async Task<ConditionalTestContext> CreateConditionalTestContextAsync()
    {
        var dbFile = System.IO.Path.Combine(
            System.IO.Path.GetTempPath(),
            $"conditional-projection-{Guid.NewGuid():N}.db");
        var connectionString = $"Data Source={dbFile}";
        var sqlCapture = new ConditionalSqlCapture();
        var selectorCapture = new ConditionalSelectorCapture();

        var services = new ServiceCollection()
            .AddSingleton(sqlCapture)
            .AddSingleton(selectorCapture)
            .AddDbContext<ConditionalDbContext>(b => b.UseSqlite(connectionString))
            .AddGraphQL()
            .AddProjectionSelectorCache()
            .AddQueryType<ConditionalQuery>()
            .AddType<ConditionalTenantType>()
            .ModifyRequestOptions(o => o.IncludeExceptionDetails = true)
            .Services
            .BuildServiceProvider();

        try
        {
            await using (var scope = services.CreateAsyncScope())
            {
                await using var context = scope.ServiceProvider.GetRequiredService<ConditionalDbContext>();
                await context.Database.EnsureCreatedAsync(Xunit.TestContext.Current.CancellationToken);

                context.Tenants.Add(
                    new ConditionalTenant
                    {
                        Id = 1,
                        Name = "Acme",
                        Workspaces =
                        [
                            new ConditionalWorkspace { Id = 1, Name = "Alpha" },
                            new ConditionalWorkspace { Id = 2, Name = "Beta" }
                        ]
                    });

                await context.SaveChangesAsync(Xunit.TestContext.Current.CancellationToken);
            }

            var executor = await services
                .GetRequiredService<IRequestExecutorProvider>()
                .GetExecutorAsync(cancellationToken: Xunit.TestContext.Current.CancellationToken);

            return new ConditionalTestContext(
                dbFile,
                services,
                executor,
                sqlCapture,
                selectorCapture);
        }
        catch
        {
            await services.DisposeAsync();
            if (File.Exists(dbFile))
            {
                File.Delete(dbFile);
            }

            throw;
        }
    }

    [Fact]
    public async Task UseProjection_Should_Project_Only_Selected_Columns_When_Entity_Is_Record()
    {
        // arrange
        var fileName = Guid.NewGuid().ToString("N") + ".db";
        var connectionString = "Data Source=" + fileName;
        var sql = new List<string>();

        try
        {
            await using (var seed = new RecordProjectionDbContext(
                new DbContextOptionsBuilder<RecordProjectionDbContext>()
                    .UseSqlite(connectionString)
                    .Options))
            {
                await seed.Database.EnsureCreatedAsync(Xunit.TestContext.Current.CancellationToken);
                seed.Products.Add(
                    new RecordProjectionProduct { Id = 1, Name = "Product", Description = "Description" });
                await seed.SaveChangesAsync(Xunit.TestContext.Current.CancellationToken);
            }

            var executor = await new ServiceCollection()
                .AddDbContext<RecordProjectionDbContext>(
                    b => b
                        .UseSqlite(connectionString)
                        .AddInterceptors(new SqlCapturingInterceptor(sql)))
                .AddGraphQL()
                .AddProjections()
                .AddQueryType<RecordProjectionQuery>()
                .BuildRequestExecutorAsync(cancellationToken: Xunit.TestContext.Current.CancellationToken);

            // act
            var result = await executor.ExecuteAsync(
                "{ products { id } }",
                Xunit.TestContext.Current.CancellationToken);

            // assert
            var operationResult = result.ExpectOperationResult();
            Assert.Empty(operationResult.Errors);
            string.Join("\n", sql).MatchInlineSnapshot(
                """
                SELECT "p"."Id"
                FROM "Products" AS "p"
                """);
        }
        finally
        {
            Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
            File.Delete(fileName);
        }
    }

    [Fact]
    public async Task AsSelector_Should_ExecuteSuccessfully_When_TypeHasOnlyPrivateConstructors()
    {
        // arrange
        var databaseName = $"db-{Guid.NewGuid():N}";

        await using (var seedContext = new FactoryOnlyBlogDbContext(
            new DbContextOptionsBuilder<FactoryOnlyBlogDbContext>()
                .UseInMemoryDatabase(databaseName)
                .Options))
        {
            await seedContext.Database.EnsureCreatedAsync();
            await seedContext.Blogs.AddAsync(FactoryOnlyBlog.Create("Blog1"));
            await seedContext.SaveChangesAsync();
        }

        var executor = await new ServiceCollection()
            .AddDbContext<FactoryOnlyBlogDbContext>(b => b.UseInMemoryDatabase(databaseName))
            .AddGraphQL()
            .AddProjections()
            .AddQueryType<FactoryOnlyBlogQuery>()
            .BuildRequestExecutorAsync();

        // act
        var result = await executor.ExecuteAsync(
            """
            {
                blogs {
                    id
                    name
                }
            }
            """);

        // assert
        var operationResult = result.ExpectOperationResult();
        Assert.True(operationResult.Errors is null || operationResult.Errors.Count == 0);
        Assert.True(operationResult.Data.HasValue);
    }

    [Fact]
    public async Task AsSelector_Should_UseMemberInitExpression_When_PocoHasPublicParameterlessConstructor()
    {
        // arrange
        var captured = new List<Expression<Func<SimpleBlog, SimpleBlog>>>();

        var executor = await new ServiceCollection()
            .AddSingleton(captured)
            .AddGraphQL()
            .AddQueryType<SimpleBlogQuery>()
            .BuildRequestExecutorAsync();

        // act
        var result = await executor.ExecuteAsync("{ blogs { id name } }");

        // assert
        var operationResult = result.ExpectOperationResult();
        Assert.True(operationResult.Errors is null || operationResult.Errors.Count == 0);
        var expr = Assert.Single(captured);
        Assert.IsType<MemberInitExpression>(expr.Body);
    }

    [Fact]
    public async Task AsSelector_Should_ExecuteSuccessfully_When_TypeHasMixedAccessibilityConstructors()
    {
        // arrange
        var databaseName = $"db-{Guid.NewGuid():N}";

        await using (var seedContext = new MixedCtorBlogDbContext(
            new DbContextOptionsBuilder<MixedCtorBlogDbContext>()
                .UseInMemoryDatabase(databaseName)
                .Options))
        {
            await seedContext.Database.EnsureCreatedAsync();
            await seedContext.Blogs.AddAsync(MixedCtorBlog.Create("Blog1"));
            await seedContext.SaveChangesAsync();
        }

        var executor = await new ServiceCollection()
            .AddDbContext<MixedCtorBlogDbContext>(b => b.UseInMemoryDatabase(databaseName))
            .AddGraphQL()
            .AddProjections()
            .AddQueryType<MixedCtorBlogQuery>()
            .BuildRequestExecutorAsync();

        // act
        var result = await executor.ExecuteAsync(
            """
            {
                blogs {
                    id
                    name
                }
            }
            """);

        // assert
        var operationResult = result.ExpectOperationResult();
        Assert.True(operationResult.Errors is null || operationResult.Errors.Count == 0);
        Assert.True(operationResult.Data.HasValue);
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

#pragma warning disable IDE0051 // Remove unused private members
        private ConstructorInjectionBlog(ConstructorInjectionDbContext context)
#pragma warning restore IDE0051 // Remove unused private members
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

    public class RecordProjectionQuery
    {
        [UseProjection]
        public IQueryable<RecordProjectionProduct> GetProducts(RecordProjectionDbContext context)
            => context.Products;
    }

    public class RecordProjectionDbContext(
        DbContextOptions<RecordProjectionDbContext> options)
        : DbContext(options)
    {
        public DbSet<RecordProjectionProduct> Products => Set<RecordProjectionProduct>();
    }

    public record RecordProjectionProduct
    {
        public required int Id { get; init; }

        public required string Name { get; init; }

        public required string Description { get; init; }
    }

    public class SingleOrDefaultUser
    {
        public string Name { get; set; } = string.Empty;
    }

    public class SingleOrDefaultActiveUser : SingleOrDefaultUser
    {
        public bool IsActive { get; set; }
    }

    public sealed class ConditionalSqlCapture
    {
        public string? Sql { get; set; }
    }

    public sealed class ConditionalSelectorCapture
    {
        public List<LambdaExpression> Selectors { get; } = [];
    }

    private sealed class ConditionalTestContext(
        string dbFile,
        ServiceProvider services,
        IRequestExecutor executor,
        ConditionalSqlCapture sqlCapture,
        ConditionalSelectorCapture selectorCapture)
        : IAsyncDisposable
    {
        public IRequestExecutor Executor { get; } = executor;

        public ConditionalSqlCapture SqlCapture { get; } = sqlCapture;

        public ConditionalSelectorCapture SelectorCapture { get; } = selectorCapture;

        public async ValueTask DisposeAsync()
        {
            await services.DisposeAsync();

            if (File.Exists(dbFile))
            {
                File.Delete(dbFile);
            }
        }
    }

    public sealed class ConditionalQuery
    {
        public IQueryable<ConditionalTenant> GetTenants(
            ConditionalDbContext database,
            IResolverContext context,
            [Service] ConditionalSqlCapture sqlCapture)
        {
            var selection = context.Selection;
            var query = database.Tenants.Select(selection.AsSelector<ConditionalTenant>(context.IncludeFlags));
            sqlCapture.Sql = query.ToQueryString();
            return query;
        }

        public IQueryable<ConditionalTenant> GetTenantsCaptured(
            ConditionalDbContext database,
            IResolverContext context,
            [Service] ConditionalSqlCapture sqlCapture,
            [Service] ConditionalSelectorCapture selectorCapture)
        {
            var selection = context.Selection;
            var selector = selection.AsSelector<ConditionalTenant>(context.IncludeFlags);
            selectorCapture.Selectors.Add(selector);
            var query = database.Tenants.Select(selector);
            sqlCapture.Sql = query.ToQueryString();
            return query;
        }

        public IQueryable<ConditionalTenant> GetTenantsCapturedUnconditional(
            ConditionalDbContext database,
            IResolverContext context,
            [Service] ConditionalSqlCapture sqlCapture,
            [Service] ConditionalSelectorCapture selectorCapture)
        {
            var selection = context.Selection;
            var selector = selection.AsSelector<ConditionalTenant>(context.IncludeFlags);
            var defaultSelector = selection.AsSelector<ConditionalTenant>();
            selectorCapture.Selectors.Add(selector);
            selectorCapture.Selectors.Add(defaultSelector);
            var query = database.Tenants.Select(selector);
            sqlCapture.Sql = query.ToQueryString();
            return query;
        }

        public IQueryable<ConditionalTenant> GetTenantsWithDefaultSelector(
            ConditionalDbContext database,
            IResolverContext context,
            [Service] ConditionalSqlCapture sqlCapture)
        {
            var selection = context.Selection;
            var query = database.Tenants.Select(selection.AsSelector<ConditionalTenant>());
            sqlCapture.Sql = query.ToQueryString();
            return query;
        }

        [UsePaging]
        public IQueryable<ConditionalTenant> GetTenantsPaged(
            ConditionalDbContext database,
            IResolverContext context,
            [Service] ConditionalSqlCapture sqlCapture)
        {
            var selection = context.Selection;
            var query = database.Tenants
                .OrderBy(t => t.Id)
                .Select(selection.AsSelector<ConditionalTenant>(context.IncludeFlags));
            sqlCapture.Sql = query.ToQueryString();
            return query;
        }

        public IQueryable<string> GetTenantNames(ConditionalDbContext database)
            => database.Tenants.Select(t => t.Name);
    }

    public sealed class ConditionalTenantType : ObjectType<ConditionalTenant>
    {
        protected override void Configure(IObjectTypeDescriptor<ConditionalTenant> descriptor)
        {
            descriptor.Field(t => t.Name).Ignore();
        }
    }

    public sealed class ConditionalDbContext(DbContextOptions<ConditionalDbContext> options) : DbContext(options)
    {
        public DbSet<ConditionalTenant> Tenants => Set<ConditionalTenant>();

        public DbSet<ConditionalWorkspace> Workspaces => Set<ConditionalWorkspace>();
    }

    public sealed class ConditionalTenant
    {
        public int Id { get; set; }

        public required string Name { get; set; }

        public List<ConditionalWorkspace> Workspaces { get; set; } = [];
    }

    public sealed class ConditionalWorkspace
    {
        public int Id { get; set; }

        public required string Name { get; set; }
    }

    public class FactoryOnlyBlogQuery
    {
        public IQueryable<FactoryOnlyBlog> GetBlogs(
            FactoryOnlyBlogDbContext context,
            ISelection selection)
            => context.Blogs.Select(selection.AsSelector<FactoryOnlyBlog>());
    }

    public sealed class FactoryOnlyBlogDbContext(DbContextOptions<FactoryOnlyBlogDbContext> options)
        : DbContext(options)
    {
        public DbSet<FactoryOnlyBlog> Blogs => Set<FactoryOnlyBlog>();
    }

    public sealed class FactoryOnlyBlog
    {
        private FactoryOnlyBlog() { }

        private FactoryOnlyBlog(string name) { Name = name; }

        public int Id { get; private set; }

        public string Name { get; private set; } = string.Empty;

        public static FactoryOnlyBlog Create(string name) => new(name);
    }

    public class MixedCtorBlogQuery
    {
        public IQueryable<MixedCtorBlog> GetBlogs(
            MixedCtorBlogDbContext context,
            ISelection selection)
            => context.Blogs.Select(selection.AsSelector<MixedCtorBlog>());
    }

    public sealed class MixedCtorBlogDbContext(DbContextOptions<MixedCtorBlogDbContext> options)
        : DbContext(options)
    {
        public DbSet<MixedCtorBlog> Blogs => Set<MixedCtorBlog>();
    }

    public sealed class MixedCtorBlog
    {
        private MixedCtorBlog() { }

        public MixedCtorBlog(string name) { Name = name; }

        public int Id { get; private set; }

        public string Name { get; private set; } = string.Empty;

        public static MixedCtorBlog Create(string name) => new(name);
    }

    public class SimpleBlogQuery
    {
        public IReadOnlyList<SimpleBlog> GetBlogs(
            ISelection selection,
            [Service] List<Expression<Func<SimpleBlog, SimpleBlog>>> captured)
        {
            captured.Add(selection.AsSelector<SimpleBlog>());
            return [new SimpleBlog { Id = 1, Name = "test" }];
        }
    }

    public sealed class SimpleBlog
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }
}
