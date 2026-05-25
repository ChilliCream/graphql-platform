using HotChocolate.Execution;
using HotChocolate.Types;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Squadron;

namespace HotChocolate.Data;

[Collection(PostgresCacheCollectionFixture.DefinitionName)]
public sealed class ArrayLengthProjectionTests(PostgreSqlResource resource)
{
    [Fact]
    public async Task Projection_ArrayLengthExpression_Should_Work_Against_PostgreSql()
    {
        // arrange
        var db = "db_" + Guid.NewGuid().ToString("N");
        var connectionString = resource.GetConnectionString(db);

        await using var services = new ServiceCollection()
            .AddDbContext<CardReaderContext>(c => c.UseNpgsql(connectionString))
            .AddGraphQLServer()
            .AddQueryType<Query>()
            .AddType<CardReaderType>()
            .AddProjections()
            .Services
            .BuildServiceProvider();

        await using var scope = services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<CardReaderContext>();
        await context.Database.EnsureCreatedAsync();
        context.CardReaders.AddRange(
            new CardReader { Id = 1, CardReaderUid = [1, 2, 3] },
            new CardReader { Id = 2, CardReaderUid = [7] });
        await context.SaveChangesAsync();

        var executor = await services
            .GetRequiredService<IRequestExecutorProvider>()
            .GetExecutorAsync();

        // act
        var result = await executor.ExecuteAsync(
            """
            {
              cardReaders {
                cardReaderUidLength
              }
            }
            """);

        // assert
        result.MatchInlineSnapshot(
            """
            {
              "data": {
                "cardReaders": [
                  {
                    "cardReaderUidLength": 3
                  },
                  {
                    "cardReaderUidLength": 1
                  }
                ]
              }
            }
            """);
    }

    public sealed class Query
    {
        [UseProjection]
        public IQueryable<CardReader> GetCardReaders([Service] CardReaderContext context)
            => context.CardReaders.OrderBy(x => x.Id);
    }

    public sealed class CardReaderType : ObjectType<CardReader>
    {
        protected override void Configure(IObjectTypeDescriptor<CardReader> descriptor)
        {
            descriptor.Field(x => x.CardReaderUid.Length).Name("cardReaderUidLength");
        }
    }

    public sealed class CardReader
    {
        public int Id { get; set; }

        public byte[] CardReaderUid { get; set; } = [];
    }

    public sealed class CardReaderContext(DbContextOptions<CardReaderContext> options)
        : DbContext(options)
    {
        public DbSet<CardReader> CardReaders => Set<CardReader>();
    }
}
