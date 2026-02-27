using HotChocolate.Execution;
using HotChocolate.Types;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Squadron;

namespace HotChocolate.Data;

[Collection(PostgresCacheCollectionFixture.DefinitionName)]
public sealed class ComputedExpressionProjectionTests(PostgreSqlResource resource)
{
    [Fact]
    public async Task Projection_ComputedExpression_Field_Dependencies_Should_Work_Against_PostgreSql()
    {
        // arrange
        var db = "db_" + Guid.NewGuid().ToString("N");
        var connectionString = resource.GetConnectionString(db);

        await using var services = new ServiceCollection()
            .AddDbContext<ExpressionPersonContext>(c => c.UseNpgsql(connectionString))
            .AddGraphQLServer()
            .AddQueryType<Query>()
            .AddType<ExpressionPersonType>()
            .AddProjections()
            .Services
            .BuildServiceProvider();

        await using var scope = services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<ExpressionPersonContext>();
        await context.Database.EnsureCreatedAsync();
        context.People.AddRange(
            new ExpressionPerson { Id = 1, FirstName = "Jane", LastName = "Doe" },
            new ExpressionPerson { Id = 2, FirstName = "John", LastName = "Smith" });
        await context.SaveChangesAsync();

        var executor = await services
            .GetRequiredService<IRequestExecutorProvider>()
            .GetExecutorAsync();

        // act
        var result = await executor.ExecuteAsync(
            """
            {
              people {
                firstName
                fullName
              }
            }
            """);

        // assert
        result.MatchInlineSnapshot(
            """
            {
              "data": {
                "people": [
                  {
                    "firstName": "Jane",
                    "fullName": "Jane Doe"
                  },
                  {
                    "firstName": "John",
                    "fullName": "John Smith"
                  }
                ]
              }
            }
            """);
    }

    public sealed class Query
    {
        [UseProjection]
        public IQueryable<ExpressionPerson> GetPeople([Service] ExpressionPersonContext context)
            => context.People.OrderBy(x => x.Id);
    }

    public sealed class ExpressionPersonType : ObjectType<ExpressionPerson>
    {
        protected override void Configure(IObjectTypeDescriptor<ExpressionPerson> descriptor)
        {
            descriptor.Field(x => x.FirstName + " " + x.LastName).Name("fullName");
        }
    }

    public sealed class ExpressionPerson
    {
        public int Id { get; set; }

        public string FirstName { get; set; } = null!;

        public string LastName { get; set; } = null!;
    }

    public sealed class ExpressionPersonContext(DbContextOptions<ExpressionPersonContext> options)
        : DbContext(options)
    {
        public DbSet<ExpressionPerson> People => Set<ExpressionPerson>();
    }
}
