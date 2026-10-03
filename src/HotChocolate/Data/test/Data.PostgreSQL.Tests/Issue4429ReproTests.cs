using HotChocolate.Execution;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Squadron;

namespace HotChocolate.Data;

[Collection(PostgresCacheCollectionFixture.DefinitionName)]
public sealed class Issue4429ReproTests(PostgreSqlResource resource)
{
    [Fact]
    public async Task Projection_On_Jsonb_Array_And_Object_Does_Not_Throw()
    {
        var db = "db_" + Guid.NewGuid().ToString("N");
        var connectionString = resource.GetConnectionString(db);

        await using var services = CreateServer(connectionString);
        await using var scope = services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<Issue4429Context>();
        await dbContext.Database.EnsureCreatedAsync();

        dbContext.Companies.Add(
            new Issue4429Company
            {
                Id = 1,
                Name = "Company 1",
                Details = new Issue4429CompanyDetails
                {
                    Detail1 = "Content1",
                    Detail2 = "Content2",
                    Detail3 = "Content3"
                },
                Tags =
                [
                    new Issue4429Tag
                    {
                        Name = "Tag 1"
                    }
                ]
            });

        await dbContext.SaveChangesAsync();

        var executor = await services.GetRequiredService<IRequestExecutorProvider>().GetExecutorAsync();
        var result = await executor.ExecuteAsync(
            """
            {
              companies {
                id
                name
                details {
                  detail1
                  detail2
                  detail3
                }
                tags {
                  name
                }
              }
            }
            """);

        var operationResult = result.ExpectOperationResult();
        Assert.Empty(operationResult.Errors ?? []);
    }

    private static ServiceProvider CreateServer(string connectionString)
    {
        var dataSourceBuilder = new NpgsqlDataSourceBuilder(connectionString);
        dataSourceBuilder.EnableDynamicJson();
        var dataSource = dataSourceBuilder.Build();

        return new ServiceCollection()
            .AddSingleton(dataSource)
            .AddDbContext<Issue4429Context>(c => c.UseNpgsql(dataSource))
            .AddGraphQLServer()
            .AddProjections()
            .AddQueryType<Issue4429Query>()
            .ModifyRequestOptions(o => o.IncludeExceptionDetails = true)
            .Services
            .BuildServiceProvider();
    }

    public sealed class Issue4429Context(DbContextOptions<Issue4429Context> options)
        : DbContext(options)
    {
        public DbSet<Issue4429Company> Companies => Set<Issue4429Company>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Issue4429Company>(entity =>
            {
                entity.HasKey(t => t.Id);
                entity.Property(t => t.Details).HasColumnType("jsonb");
                entity.Property(t => t.Tags).HasColumnType("jsonb");
            });
        }
    }

    public sealed class Issue4429Query
    {
        [UseProjection]
        public IQueryable<Issue4429Company> GetCompanies(Issue4429Context dbContext)
            => dbContext.Companies;
    }

    public sealed class Issue4429Company
    {
        public int Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public List<Issue4429Tag> Tags { get; set; } = [];

        public Issue4429CompanyDetails Details { get; set; } = new();
    }

    public sealed class Issue4429Tag
    {
        public string Name { get; set; } = string.Empty;
    }

    public sealed class Issue4429CompanyDetails
    {
        public string Detail1 { get; set; } = string.Empty;

        public string Detail2 { get; set; } = string.Empty;

        public string Detail3 { get; set; } = string.Empty;
    }
}
