using HotChocolate.Data.Filters;
using HotChocolate.Execution;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Squadron;

namespace HotChocolate.Data;

[Collection(PostgresCacheCollectionFixture.DefinitionName)]
public sealed class ArrayLengthFilterTests(PostgreSqlResource resource)
{
    [Fact]
    public async Task Filter_ArrayLengthExpression_Should_Work_Against_PostgreSql()
    {
        // arrange
        var db = "db_" + Guid.NewGuid().ToString("N");
        var connectionString = resource.GetConnectionString(db);

        await using var services = new ServiceCollection()
            .AddDbContext<CardReaderContext>(c => c.UseNpgsql(connectionString))
            .AddGraphQLServer()
            .AddQueryType<Query>()
            .AddType<CardReaderFilterInputType>()
            .AddFiltering()
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
              cardReaders(where: { cardReaderUidLength: { eq: 3 } }) {
                id
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
                    "id": 1
                  }
                ]
              }
            }
            """);
    }

    public sealed class Query
    {
        [UseFiltering(typeof(CardReaderFilterInputType))]
        public IQueryable<CardReader> GetCardReaders([Service] CardReaderContext context)
            => context.CardReaders;
    }

    public sealed class CardReader
    {
        public int Id { get; set; }

        public byte[] CardReaderUid { get; set; } = [];
    }

    public sealed class CardReaderFilterInputType : FilterInputType<CardReader>
    {
        protected override void Configure(IFilterInputTypeDescriptor<CardReader> descriptor)
        {
            descriptor.BindFieldsExplicitly();
            descriptor.Field(x => x.CardReaderUid.Length).Name("cardReaderUidLength");
        }
    }

    public sealed class CardReaderContext(DbContextOptions<CardReaderContext> options)
        : DbContext(options)
    {
        public DbSet<CardReader> CardReaders => Set<CardReader>();
    }
}
