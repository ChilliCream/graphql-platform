using System.Linq.Expressions;
using System.Text.Json;
using System.Text.RegularExpressions;
using GreenDonut.Data;
using HotChocolate.Execution;
using HotChocolate.Types;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Squadron;

namespace HotChocolate.Data;

[Collection(PostgresCacheCollectionFixture.DefinitionName)]
public sealed partial class AsSelectorEncapsulatedProjectionTests(PostgreSqlResource resource)
{
    [Fact]
    public async Task AsSelector_Should_Prune_Columns_When_Entity_Has_NonPublic_Ctor_And_Private_Setters()
    {
        // arrange
        var db = "db_" + Guid.NewGuid().ToString("N");
        var connectionString = resource.GetConnectionString(db);

        await using var services = CreateServer(connectionString);
        await SeedAsync(services);

        var executor = await services
            .GetRequiredService<IRequestExecutorProvider>()
            .GetExecutorAsync(cancellationToken: TestContext.Current.CancellationToken);

        // act
        var result = await executor.ExecuteAsync(
            """
            {
              encapsulatedStores {
                name
              }
            }
            """,
            TestContext.Current.CancellationToken);

        // assert
        var operationResult = result.ExpectOperationResult();
        Assert.Empty(operationResult.Errors ?? []);

        Assert.Equal(["Basel", "Zurich"], ReadNames(result, "encapsulatedStores"));

        var capture = services.GetRequiredService<EncapsulatedSelectorCapture>();

        // the selector is a genuine member-init projection (not an identity x => x reuse)
        // that binds exactly the selected member and nothing else.
        AssertProjectsExactly(
            capture.EncapsulatedSelector,
            nameof(EncapsulatedStore.Name));

        // the generated SQL fetches exactly the selected column, not Region or CountryCode.
        Assert.Equal(
            [nameof(EncapsulatedStore.Name)],
            ExtractSelectedColumns(capture.EncapsulatedSql));
    }

    [Fact]
    public async Task AsSelector_Should_Prune_Columns_When_Entity_Has_Private_Parameterless_And_Public_Business_Ctor()
    {
        // arrange
        var db = "db_" + Guid.NewGuid().ToString("N");
        var connectionString = resource.GetConnectionString(db);

        await using var services = CreateServer(connectionString);
        await SeedAsync(services);

        var executor = await services
            .GetRequiredService<IRequestExecutorProvider>()
            .GetExecutorAsync(cancellationToken: TestContext.Current.CancellationToken);

        // act
        var result = await executor.ExecuteAsync(
            """
            {
              mixedCtorStores {
                name
              }
            }
            """,
            TestContext.Current.CancellationToken);

        // assert
        var operationResult = result.ExpectOperationResult();
        Assert.Empty(operationResult.Errors ?? []);

        Assert.Equal(["Bern", "Geneva"], ReadNames(result, "mixedCtorStores"));

        var capture = services.GetRequiredService<EncapsulatedSelectorCapture>();

        // the public business ctor does not interfere: member-init via the private
        // parameterless ctor is still used, binding exactly the selected member.
        AssertProjectsExactly(
            capture.MixedCtorSelector,
            nameof(MixedCtorStore.Name));

        // the generated SQL fetches exactly the selected column, not Region or CountryCode.
        Assert.Equal(
            [nameof(MixedCtorStore.Name)],
            ExtractSelectedColumns(capture.MixedCtorSql));
    }

    private static ServiceProvider CreateServer(string connectionString)
        => new ServiceCollection()
            .AddDbContext<EncapsulatedStoreContext>(c => c.UseNpgsql(connectionString))
            .AddScoped<EncapsulatedStoreService>()
            .AddSingleton<EncapsulatedSelectorCapture>()
            .AddGraphQLServer()
            .AddQueryContext()
            .AddQueryType(descriptor =>
            {
                descriptor.Name(OperationTypeNames.Query);

                descriptor
                    .Field("encapsulatedStores")
                    .ResolveWith<EncapsulatedStoreQueryResolver>(
                        t => t.GetEncapsulatedStoresAsync(default!, default!, default))
                    .Type<ListType<NonNullType<ObjectType<EncapsulatedStore>>>>();

                descriptor
                    .Field("mixedCtorStores")
                    .ResolveWith<EncapsulatedStoreQueryResolver>(
                        t => t.GetMixedCtorStoresAsync(default!, default!, default))
                    .Type<ListType<NonNullType<ObjectType<MixedCtorStore>>>>();
            })
            .ModifyRequestOptions(o => o.IncludeExceptionDetails = true)
            .Services
            .BuildServiceProvider();

    private static async Task SeedAsync(IServiceProvider services)
    {
        await using var scope = services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<EncapsulatedStoreContext>();

        await context.Database.EnsureCreatedAsync();

        context.EncapsulatedStores.AddRange(
            EncapsulatedStore.Create("Zurich", "ZH", "CH"),
            EncapsulatedStore.Create("Basel", "BS", "CH"));

        context.MixedCtorStores.AddRange(
            MixedCtorStore.Create("Geneva", "GE", "CH"),
            MixedCtorStore.Create("Bern", "BE", "CH"));

        await context.SaveChangesAsync();
    }

    private static string[] ReadNames(IExecutionResult result, string field)
    {
        using var document = JsonDocument.Parse(result.ToJson());

        return document.RootElement
            .GetProperty("data")
            .GetProperty(field)
            .EnumerateArray()
            .Select(t => t.GetProperty("name").GetString()!)
            .OrderBy(t => t, StringComparer.Ordinal)
            .ToArray();
    }

    private static void AssertProjectsExactly<T>(
        Expression<Func<T, T>>? selector,
        params string[] expectedMembers)
    {
        Assert.NotNull(selector);
        var body = UnwrapConvert(selector.Body);

        // a real projection materializes a new instance via member-init. The reuse path
        // (x => x) returns the parameter itself, so this also proves it is not identity reuse.
        var memberInit = Assert.IsType<MemberInitExpression>(body);

        var visitor = new RootMemberAccessVisitor(selector.Parameters[0]);
        visitor.Visit(memberInit);

        Assert.Equal(
            expectedMembers.OrderBy(m => m, StringComparer.Ordinal).ToArray(),
            visitor.Members.OrderBy(m => m, StringComparer.Ordinal).ToArray());
    }

    private static string[] ExtractSelectedColumns(string? sql)
    {
        Assert.NotNull(sql);

        var selectIndex = sql.IndexOf("SELECT", StringComparison.Ordinal);
        var fromIndex = sql.IndexOf("FROM", selectIndex, StringComparison.Ordinal);
        var selectClause = sql[selectIndex..fromIndex];

        return ColumnReferenceRegex()
            .Matches(selectClause)
            .Select(m => m.Groups["column"].Value)
            .Distinct()
            .OrderBy(c => c, StringComparer.Ordinal)
            .ToArray();
    }

    [GeneratedRegex("\"(?<column>[^\"]+)\"")]
    private static partial Regex ColumnReferenceRegex();

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

    private sealed class EncapsulatedStore
    {
        private EncapsulatedStore()
        {
        }

        private EncapsulatedStore(string name, string region, string countryCode)
        {
            Name = name;
            Region = region;
            CountryCode = countryCode;
        }

        public int Id { get; private set; }

        public string Name { get; private set; } = default!;

        public string Region { get; private set; } = default!;

        public string CountryCode { get; private set; } = default!;

        public static EncapsulatedStore Create(string name, string region, string countryCode)
            => new(name, region, countryCode);
    }

    private sealed class MixedCtorStore
    {
        private MixedCtorStore()
        {
        }

        public MixedCtorStore(string name, string region, string countryCode)
        {
            Name = name;
            Region = region;
            CountryCode = countryCode;
        }

        public int Id { get; private set; }

        public string Name { get; private set; } = default!;

        public string Region { get; private set; } = default!;

        public string CountryCode { get; private set; } = default!;

        public static MixedCtorStore Create(string name, string region, string countryCode)
            => new(name, region, countryCode);
    }

    private sealed class EncapsulatedStoreContext(DbContextOptions<EncapsulatedStoreContext> options)
        : DbContext(options)
    {
        public DbSet<EncapsulatedStore> EncapsulatedStores => Set<EncapsulatedStore>();

        public DbSet<MixedCtorStore> MixedCtorStores => Set<MixedCtorStore>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<EncapsulatedStore>().HasKey(t => t.Id);
            modelBuilder.Entity<MixedCtorStore>().HasKey(t => t.Id);
        }
    }

    private sealed class EncapsulatedSelectorCapture
    {
        public Expression<Func<EncapsulatedStore, EncapsulatedStore>>? EncapsulatedSelector { get; set; }

        public Expression<Func<MixedCtorStore, MixedCtorStore>>? MixedCtorSelector { get; set; }

        public string? EncapsulatedSql { get; set; }

        public string? MixedCtorSql { get; set; }
    }

    private sealed class EncapsulatedStoreService(
        EncapsulatedStoreContext context,
        EncapsulatedSelectorCapture capture)
    {
        public async Task<IReadOnlyList<EncapsulatedStore>> GetEncapsulatedStoresAsync(
            QueryContext<EncapsulatedStore> query,
            CancellationToken cancellationToken)
        {
            capture.EncapsulatedSelector = query.Selector;

            var projectedQuery = context.EncapsulatedStores.AsNoTracking().With(query);
            capture.EncapsulatedSql = projectedQuery.ToQueryString();

            return await projectedQuery.ToListAsync(cancellationToken);
        }

        public async Task<IReadOnlyList<MixedCtorStore>> GetMixedCtorStoresAsync(
            QueryContext<MixedCtorStore> query,
            CancellationToken cancellationToken)
        {
            capture.MixedCtorSelector = query.Selector;

            var projectedQuery = context.MixedCtorStores.AsNoTracking().With(query);
            capture.MixedCtorSql = projectedQuery.ToQueryString();

            return await projectedQuery.ToListAsync(cancellationToken);
        }
    }

    private sealed class EncapsulatedStoreQueryResolver
    {
        public Task<IReadOnlyList<EncapsulatedStore>> GetEncapsulatedStoresAsync(
            QueryContext<EncapsulatedStore> query,
            [Service] EncapsulatedStoreService service,
            CancellationToken cancellationToken)
            => service.GetEncapsulatedStoresAsync(query, cancellationToken);

        public Task<IReadOnlyList<MixedCtorStore>> GetMixedCtorStoresAsync(
            QueryContext<MixedCtorStore> query,
            [Service] EncapsulatedStoreService service,
            CancellationToken cancellationToken)
            => service.GetMixedCtorStoresAsync(query, cancellationToken);
    }
}
