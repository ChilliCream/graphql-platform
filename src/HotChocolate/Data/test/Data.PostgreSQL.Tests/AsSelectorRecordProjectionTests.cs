using System.Linq.Expressions;
using System.Text.Json;
using GreenDonut.Data;
using HotChocolate.Execution;
using HotChocolate.Types;
using HotChocolate.Types.Relay;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Squadron;

namespace HotChocolate.Data;

[Collection(PostgresCacheCollectionFixture.DefinitionName)]
public sealed class AsSelectorRecordProjectionTests(PostgreSqlResource resource)
{
    [Fact]
    public async Task AsSelector_Should_Project_Record_On_Standard_Field()
    {
        // arrange
        var db = "db_" + Guid.NewGuid().ToString("N");
        var connectionString = resource.GetConnectionString(db);

        await using var services = CreateServer(connectionString);
        await SeedAsync(
            services,
            new StoreRecord(1, "Zurich", "ZH", "CH"),
            new StoreRecord(2, "Basel", "BS", "CH"));

        var executor = await services
            .GetRequiredService<IRequestExecutorProvider>()
            .GetExecutorAsync();

        // act
        var result = await executor.ExecuteAsync(
            """
            {
              stores {
                name
              }
            }
            """);

        // assert
        var operationResult = result.ExpectOperationResult();
        Assert.Empty(operationResult.Errors ?? []);

        using var document = JsonDocument.Parse(result.ToJson());
        var names = document.RootElement
            .GetProperty("data")
            .GetProperty("stores")
            .EnumerateArray()
            .Select(t => t.GetProperty("name").GetString()!)
            .OrderBy(t => t)
            .ToArray();

        Assert.Equal(["Basel", "Zurich"], names);

        var capture = services.GetRequiredService<RecordSelectorCapture>();

        AssertSelectorProjects(capture.StandardFieldSelector, nameof(StoreRecord.Name));
        Assert.NotNull(capture.StandardFieldSql);
    }

    [Fact]
    public async Task AsSelector_Should_Project_Record_On_Node_Field()
    {
        // arrange
        var db = "db_" + Guid.NewGuid().ToString("N");
        var connectionString = resource.GetConnectionString(db);

        await using var services = CreateServer(connectionString);
        await SeedAsync(services, new StoreRecord(1, "Zurich", "ZH", "CH"));

        var executor = await services
            .GetRequiredService<IRequestExecutorProvider>()
            .GetExecutorAsync();

        var serializer = services.GetRequiredService<INodeIdSerializer>();
        var storeId = serializer.Format("Store", 1);

        var nodeQuery = $$"""
            {
              node(id: "{{storeId}}") {
                id
                ... on Store {
                  name
                }
              }
            }
            """;

        // act
        var result = await executor.ExecuteAsync(nodeQuery);

        // assert
        var operationResult = result.ExpectOperationResult();
        Assert.Empty(operationResult.Errors ?? []);

        using var document = JsonDocument.Parse(result.ToJson());
        var node = document.RootElement
            .GetProperty("data")
            .GetProperty("node");

        Assert.Equal("Zurich", node.GetProperty("name").GetString());

        var capture = services.GetRequiredService<RecordSelectorCapture>();

        AssertSelectorProjects(
            capture.NodeFieldSelector,
            nameof(StoreRecord.Id),
            nameof(StoreRecord.Name));

        Assert.NotNull(capture.NodeFieldSql);
    }

    private static ServiceProvider CreateServer(string connectionString)
        => new ServiceCollection()
            .AddDbContext<RecordStoreContext>(c => c.UseNpgsql(connectionString))
            .AddScoped<RecordStoreService>()
            .AddSingleton<RecordSelectorCapture>()
            .AddGraphQLServer()
            .AddQueryContext()
            .AddGlobalObjectIdentification()
            .AddQueryType<RecordStoreQuery>()
            .AddType<StoreRecordType>()
            .AddTypeExtension(typeof(RecordStoreNode))
            .ModifyRequestOptions(o => o.IncludeExceptionDetails = true)
            .Services
            .BuildServiceProvider();

    private static async Task SeedAsync(IServiceProvider services, params StoreRecord[] stores)
    {
        await using var scope = services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<RecordStoreContext>();

        await context.Database.EnsureCreatedAsync();
        context.Stores.AddRange(stores);
        await context.SaveChangesAsync();
    }

    private static void AssertSelectorProjects(
        Expression<Func<StoreRecord, StoreRecord>>? selector,
        params string[] projectedMembers)
    {
        Assert.NotNull(selector);
        var body = UnwrapConvert(selector!.Body);

        Assert.IsType<NewExpression>(body);

        var visitor = new RootMemberAccessVisitor(selector.Parameters[0]);
        visitor.Visit(body);

        foreach (var member in projectedMembers)
        {
            Assert.Contains(member, visitor.Members);
        }
    }

    private static Expression UnwrapConvert(Expression expression)
    {
        while (expression is UnaryExpression
            {
                NodeType: ExpressionType.Convert or ExpressionType.ConvertChecked,
                Operand: { } operand
            })
        {
            expression = operand;
        }

        return expression;
    }

    private sealed class RootMemberAccessVisitor(ParameterExpression root) : ExpressionVisitor
    {
        public HashSet<string> Members { get; } = [];

        protected override Expression VisitMember(MemberExpression node)
        {
            if (IsRootMember(node.Expression))
            {
                Members.Add(node.Member.Name);
            }

            return base.VisitMember(node);
        }

        private bool IsRootMember(Expression? expression)
        {
            while (expression is UnaryExpression
                {
                    NodeType: ExpressionType.Convert or ExpressionType.ConvertChecked,
                    Operand: { } operand
                })
            {
                expression = operand;
            }

            return expression == root;
        }
    }

    public sealed record StoreRecord(
        int Id,
        string Name,
        string Region,
        string CountryCode);

    public sealed class StoreRecordType : ObjectType<StoreRecord>
    {
        protected override void Configure(IObjectTypeDescriptor<StoreRecord> descriptor)
            => descriptor.Name("Store");
    }

    public sealed class RecordStoreContext(DbContextOptions<RecordStoreContext> options)
        : DbContext(options)
    {
        public DbSet<StoreRecord> Stores => Set<StoreRecord>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
            => modelBuilder.Entity<StoreRecord>().HasKey(t => t.Id);
    }

    public sealed class RecordSelectorCapture
    {
        public Expression<Func<StoreRecord, StoreRecord>>? StandardFieldSelector { get; set; }

        public Expression<Func<StoreRecord, StoreRecord>>? NodeFieldSelector { get; set; }

        public string? StandardFieldSql { get; set; }

        public string? NodeFieldSql { get; set; }
    }

    public sealed class RecordStoreService(RecordStoreContext context, RecordSelectorCapture capture)
    {
        public async Task<IReadOnlyList<StoreRecord>> GetStoresAsync(
            QueryContext<StoreRecord> query,
            CancellationToken cancellationToken)
        {
            capture.StandardFieldSelector = query.Selector;

            var projectedQuery = context.Stores.AsNoTracking().With(query);
            capture.StandardFieldSql = projectedQuery.ToQueryString();

            return await projectedQuery.ToListAsync(cancellationToken);
        }

        public async Task<StoreRecord?> GetStoreByIdAsync(
            int id,
            QueryContext<StoreRecord> query,
            CancellationToken cancellationToken)
        {
            capture.NodeFieldSelector = query.Selector;

            var projectedQuery = context.Stores
                .AsNoTracking()
                .Where(t => t.Id == id)
                .With(query);

            capture.NodeFieldSql = projectedQuery.ToQueryString();

            return await projectedQuery.SingleOrDefaultAsync(cancellationToken);
        }
    }

    public sealed class RecordStoreQuery
    {
        public Task<IReadOnlyList<StoreRecord>> GetStoresAsync(
            QueryContext<StoreRecord> query,
            RecordStoreService service,
            CancellationToken cancellationToken)
            => service.GetStoresAsync(query, cancellationToken);
    }

    [Node]
    [ExtendObjectType(typeof(StoreRecord))]
    public static class RecordStoreNode
    {
        [NodeResolver]
        public static Task<StoreRecord?> GetStoreByIdAsync(
            int id,
            QueryContext<StoreRecord> query,
            RecordStoreService service,
            CancellationToken cancellationToken)
            => service.GetStoreByIdAsync(id, query, cancellationToken);
    }
}
