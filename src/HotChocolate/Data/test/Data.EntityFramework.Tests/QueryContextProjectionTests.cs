using System.Reflection;
using GreenDonut.Data;
using HotChocolate.Execution;
using HotChocolate.Types;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using IOPath = System.IO.Path;

namespace HotChocolate.Data;

public class QueryContextProjectionTests : IDisposable
{
    private readonly string _fileName = IOPath.Combine(
        IOPath.GetTempPath(),
        $"query-context-projection-{Guid.NewGuid():N}.db");

    [Fact]
    public async Task QueryContext_Should_PreserveNull_When_OptionalNavigationNullabilityIsUnknown()
    {
        // arrange
        // Order.Parent is declared in a nullable-oblivious context, so the runtime
        // reports its nullability as Unknown rather than Nullable.
        var parentProperty = typeof(Order).GetProperty(nameof(Order.Parent))!;
        var nullabilityInfo = new NullabilityInfoContext().Create(parentProperty);
        Assert.Equal(NullabilityState.Unknown, nullabilityInfo.WriteState);
        Assert.Equal(NullabilityState.Unknown, nullabilityInfo.ReadState);

        await SeedAsync();

        var executor = await new ServiceCollection()
            .AddDbContext<OrderDbContext>(
                b => b.UseSqlite("Data Source=" + _fileName))
            .AddGraphQL()
            .AddQueryContext()
            .AddQueryType<QueryContextQueryType>()
            .AddType<OrderType>()
            .AddType<OrderRelationType>()
            .ModifyRequestOptions(o => o.IncludeExceptionDetails = true)
            .BuildRequestExecutorAsync(cancellationToken: Xunit.TestContext.Current.CancellationToken);

        // act
        var result = await executor.ExecuteAsync(
            """
            {
              orders {
                orderId
                parent {
                  parentOrderId
                  childOrderId
                }
              }
            }
            """,
            Xunit.TestContext.Current.CancellationToken);

        // assert
        result.MatchInlineSnapshot(
            """
            {
              "data": {
                "orders": [
                  {
                    "orderId": "11111111-1111-1111-1111-111111111111",
                    "parent": null
                  },
                  {
                    "orderId": "22222222-2222-2222-2222-222222222222",
                    "parent": null
                  }
                ]
              }
            }
            """);
    }

    private async Task SeedAsync()
    {
        var options = new DbContextOptionsBuilder<OrderDbContext>()
            .UseSqlite("Data Source=" + _fileName)
            .Options;

        await using var context = new OrderDbContext(options);
        await context.Database.EnsureCreatedAsync(Xunit.TestContext.Current.CancellationToken);

        context.Orders.Add(new Order { OrderId = Guid.Parse("11111111-1111-1111-1111-111111111111") });
        context.Orders.Add(new Order { OrderId = Guid.Parse("22222222-2222-2222-2222-222222222222") });

        await context.SaveChangesAsync(Xunit.TestContext.Current.CancellationToken);
    }

    public void Dispose()
    {
        SqliteConnection.ClearAllPools();

        if (File.Exists(_fileName))
        {
            File.Delete(_fileName);
        }
    }

#nullable disable

    public class Order
    {
        public Guid OrderId { get; set; }

        public OrderRelation Parent { get; set; }

        public ICollection<OrderRelation> Children { get; set; }
    }

    public class OrderRelation
    {
        public Guid ParentOrderId { get; set; }

        public Guid ChildOrderId { get; set; }

        public Order ParentOrder { get; set; }

        public Order ChildOrder { get; set; }
    }

#nullable restore

    public class OrderDbContext(DbContextOptions<OrderDbContext> options) : DbContext(options)
    {
        public DbSet<Order> Orders => Set<Order>();

        public DbSet<OrderRelation> OrderRelations => Set<OrderRelation>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Order>(b => b.HasKey(x => x.OrderId));

            modelBuilder.Entity<OrderRelation>(b =>
            {
                b.HasKey(x => new { x.ParentOrderId, x.ChildOrderId });

                b.HasOne(x => x.ParentOrder)
                    .WithMany(x => x.Children)
                    .HasForeignKey(x => x.ParentOrderId)
                    .OnDelete(DeleteBehavior.NoAction)
                    .IsRequired();

                b.HasOne(x => x.ChildOrder)
                    .WithOne(x => x.Parent)
                    .HasForeignKey<OrderRelation>(x => x.ChildOrderId)
                    .IsRequired();
            });
        }
    }

    public class OrderType : ObjectType<Order>
    {
        protected override void Configure(IObjectTypeDescriptor<Order> descriptor)
        {
            descriptor.BindFieldsExplicitly();
            descriptor.Field(x => x.OrderId);
            descriptor.Field(x => x.Parent).Type<OrderRelationType>();
        }
    }

    public class OrderRelationType : ObjectType<OrderRelation>
    {
        protected override void Configure(IObjectTypeDescriptor<OrderRelation> descriptor)
        {
            descriptor.BindFieldsExplicitly();
            descriptor.Field(x => x.ParentOrderId);
            descriptor.Field(x => x.ChildOrderId);
        }
    }

    public class QueryContextQuery
    {
        public IQueryable<Order> GetOrders(
            OrderDbContext context,
            QueryContext<Order> query)
            => context.Orders.With(query);
    }

    public class QueryContextQueryType : ObjectType<QueryContextQuery>
    {
        protected override void Configure(IObjectTypeDescriptor<QueryContextQuery> descriptor)
        {
            descriptor.Field(x => x.GetOrders(null!, null!));
        }
    }
}
