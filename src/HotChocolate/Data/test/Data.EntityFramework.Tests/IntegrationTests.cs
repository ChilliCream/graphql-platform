using System.Data.Common;
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
                await seed.Database.EnsureCreatedAsync();
                seed.Authors.Add(
                    new Author { Id = 1, Name = "Foo", Books = { new Book { Id = 1, Title = "Foo1" } } });
                await seed.SaveChangesAsync();
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
                .ExecuteRequestAsync("{ books { title authorInfo { name } } }");

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
                .AddQueryType<ConditionalQuery>()
                .AddType<ConditionalTenantType>()
                .ModifyRequestOptions(o => o.IncludeExceptionDetails = true)
                .Services
                .BuildServiceProvider();

            await using (var scope = services.CreateAsyncScope())
            {
                await using var context = scope.ServiceProvider.GetRequiredService<ConditionalDbContext>();
                await context.Database.EnsureCreatedAsync();

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

                await context.SaveChangesAsync();
            }

            var executor = await services
                .GetRequiredService<IRequestExecutorProvider>()
                .GetExecutorAsync();

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
                    .Build());

            // assert
            await new Snapshot(postFix: include ? "include_true" : "include_false")
                .Add(result, "Result")
                .Add(sqlCapture.Sql ?? "<none>", "SQL")
                .MatchMarkdownAsync();
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
                .AddQueryType<ConditionalQuery>()
                .AddType<ConditionalTenantType>()
                .ModifyRequestOptions(o => o.IncludeExceptionDetails = true)
                .Services
                .BuildServiceProvider();

            await using (var scope = services.CreateAsyncScope())
            {
                await using var context = scope.ServiceProvider.GetRequiredService<ConditionalDbContext>();
                await context.Database.EnsureCreatedAsync();

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

                await context.SaveChangesAsync();
            }

            var executor = await services
                .GetRequiredService<IRequestExecutorProvider>()
                .GetExecutorAsync();

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
                    .Build());

            // assert
            await new Snapshot()
                .Add(result, "Result")
                .Add(sqlCapture.Sql ?? "<none>", "SQL")
                .MatchMarkdownAsync();
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
                .AddQueryType<ConditionalQuery>()
                .AddType<ConditionalTenantType>()
                .ModifyRequestOptions(o => o.IncludeExceptionDetails = true)
                .Services
                .BuildServiceProvider();

            await using (var scope = services.CreateAsyncScope())
            {
                await using var context = scope.ServiceProvider.GetRequiredService<ConditionalDbContext>();
                await context.Database.EnsureCreatedAsync();

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

                await context.SaveChangesAsync();
            }

            var executor = await services
                .GetRequiredService<IRequestExecutorProvider>()
                .GetExecutorAsync();

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
                    .Build());

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
}
