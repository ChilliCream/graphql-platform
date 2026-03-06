// To run without a project file:
// #:package Mocha@1.0.0-preview.*
// #:package Mocha.Transport.RabbitMQ@1.0.0-preview.*
// #:package Mocha.EntityFrameworkCore.Postgres@1.0.0-preview.*
// #:package Mocha.Outbox@1.0.0-preview.*
// $ dotnet run OutboxInbox.cs

using Microsoft.EntityFrameworkCore;
using Mocha;
using Mocha.EntityFrameworkCore;
using Mocha.Outbox;
using Mocha.Transport.RabbitMQ;
using RabbitMQ.Client;
using Mocha.Hosting;

var builder = WebApplication.CreateBuilder(args);

// Register the DbContext with a Postgres connection string from configuration.
// In production, use an Aspire component or environment variable for the connection string.
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("postgres")
        ?? "Host=localhost;Database=orders;Username=postgres;Password=postgres"));

builder.Services.AddSingleton<IConnectionFactory>(_ =>
    new RabbitMQ.Client.ConnectionFactory
    {
        HostName = "localhost",
        Port = 5672,
        UserName = "guest",
        Password = "guest"
    });

builder.Services
    .AddMessageBus()
    .AddEventHandler<OrderPlacedHandler>()
    .AddEntityFramework<AppDbContext>(p =>
    {
        // Persist outbound messages to the outbox table in the same transaction
        // as your business data. A background processor dispatches them after commit.
        p.AddPostgresOutbox();

        // Wrap each consumer invocation in a database transaction.
        // The transaction commits on success and rolls back on failure.
        p.UseTransaction();
    })
    .AddRabbitMQ();

var app = builder.Build();

app.MapGet("/orders", async (IMessageBus bus) =>
{
    var orderId = Guid.NewGuid();

    await bus.PublishAsync(
        new OrderPlaced(orderId, "Mechanical Keyboard", 149.99m),
        CancellationToken.None);

    return Results.Ok(new { OrderId = orderId, Status = "Published" });
});

if (app.Environment.IsDevelopment())
{
    app.MapMessageBusDeveloperTopology();
}

app.Run();

// --- Domain ---

public sealed record OrderPlaced(Guid OrderId, string ProductName, decimal Amount);

public sealed record InvoiceCreated(Guid InvoiceId, Guid OrderId, decimal Amount);

// --- DbContext ---

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    // Outbox table — required for the transactional outbox
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    // Your business data
    public DbSet<Order> Orders => Set<Order>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configures the OutboxMessages table with Postgres-optimized column types and indexes
        modelBuilder.AddPostgresOutbox();
    }
}

public class Order
{
    public Guid Id { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public decimal Amount { get; set; }
}

// --- Handlers ---

// With UseTransaction() active, this handler runs inside a database transaction.
// With AddPostgresOutbox() active, calls to bus.PublishAsync() write to the outbox
// table rather than directly to RabbitMQ — within the same transaction.
// After db.SaveChangesAsync() commits the transaction, the outbox processor
// picks up the pending messages and dispatches them to RabbitMQ.
public class OrderPlacedHandler(AppDbContext db, IMessageBus bus)
    : IEventHandler<OrderPlaced>
{
    public async ValueTask HandleAsync(
        OrderPlaced message,
        CancellationToken cancellationToken)
    {
        var order = new Order
        {
            Id = message.OrderId,
            ProductName = message.ProductName,
            Amount = message.Amount
        };

        db.Orders.Add(order);

        // This write goes to the outbox table (not directly to RabbitMQ) because
        // AddPostgresOutbox() intercepts IMessageBus calls inside a transaction.
        await bus.PublishAsync(
            new InvoiceCreated(Guid.NewGuid(), order.Id, order.Amount),
            cancellationToken);

        // Both the Order row and the OutboxMessage row are committed atomically.
        await db.SaveChangesAsync(cancellationToken);
    }
}

public class InvoiceCreatedHandler(ILogger<InvoiceCreatedHandler> logger)
    : IEventHandler<InvoiceCreated>
{
    public ValueTask HandleAsync(
        InvoiceCreated message,
        CancellationToken cancellationToken)
    {
        logger.LogInformation(
            "Invoice {InvoiceId} created for order {OrderId} — amount {Amount:C}",
            message.InvoiceId,
            message.OrderId,
            message.Amount);

        return ValueTask.CompletedTask;
    }
}
