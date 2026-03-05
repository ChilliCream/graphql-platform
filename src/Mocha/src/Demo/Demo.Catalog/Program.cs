using Demo.Catalog.Data;
using Demo.Catalog.Entities;
using Demo.Catalog.Handlers;
using Demo.Catalog.Sagas;
using Demo.Contracts.Commands;
using Demo.Contracts.Events;
using Demo.Contracts.Saga;
using Microsoft.EntityFrameworkCore;
using Mocha;
using Mocha.EntityFrameworkCore;
using Mocha.Hosting;
using Mocha.Outbox;
using Mocha.Sagas;
using Mocha.Transport.RabbitMQ;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

// Database
builder.AddNpgsqlDbContext<CatalogDbContext>("catalog-db");

// RabbitMQ
builder.AddRabbitMQClient("rabbitmq", x => x.DisableTracing = true);

// MessageBus
builder
    .Services.AddMessageBus()
    .AddInstrumentation()
    // Event handlers
    .AddEventHandler<PaymentCompletedEventHandler>()
    .AddEventHandler<ShipmentCreatedEventHandler>()
    // Request handlers
    .AddRequestHandler<GetProductRequestHandler>()
    .AddRequestHandler<ReserveInventoryCommandHandler>()
    .AddRequestHandler<InspectReturnCommandHandler>()
    .AddRequestHandler<RestockInventoryCommandHandler>()
    // Sagas
    .AddSaga<QuickRefundSaga>()
    .AddSaga<ReturnProcessingSaga>()
    .AddEntityFramework<CatalogDbContext>(p =>
    {
        p.AddPostgresOutbox();
        p.AddPostgresSagas();
        p.UseResilience();
        p.UseTransaction();
    })
    .AddRabbitMQ();

var app = builder.Build();

// Ensure database is created and seeded
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<CatalogDbContext>();
    await db.Database.EnsureCreatedAsync();
}

// REST API Endpoints
app.MapGet("/", () => "Catalog Service");

// Products
app.MapGet("/api/products", async (CatalogDbContext db) => await db.Products.Include(p => p.Category).ToListAsync());

app.MapGet(
    "/api/products/{id:guid}",
    async (Guid id, CatalogDbContext db) =>
        await db.Products.Include(p => p.Category).FirstOrDefaultAsync(p => p.Id == id) is { } product
            ? Results.Ok(product)
            : Results.NotFound());

// Categories
app.MapGet("/api/categories", async (CatalogDbContext db) => await db.Categories.ToListAsync());

// Orders - placing an order triggers OrderPlacedEvent
app.MapPost(
    "/api/orders",
    async (PlaceOrderRequest request, CatalogDbContext db, IMessageBus messageBus) =>
    {
        var executionStrategy = db.Database.CreateExecutionStrategy();

        return await executionStrategy.ExecuteAsync(async () =>
        {
            await using var transaction = await db.Database.BeginTransactionAsync();

            var product = await db.Products.FindAsync(request.ProductId);
            if (product is null)
            {
                return Results.NotFound("Product not found");
            }

            if (product.StockQuantity < request.Quantity)
            {
                return Results.BadRequest("Insufficient stock");
            }

            var order = new OrderRecord
            {
                Id = Guid.NewGuid(),
                ProductId = product.Id,
                Quantity = request.Quantity,
                CustomerId = request.CustomerId,
                ShippingAddress = request.ShippingAddress,
                TotalAmount = product.Price * request.Quantity,
                Status = OrderStatus.Pending,
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            };

            db.Orders.Add(order);
            await db.SaveChangesAsync();

            // Publish OrderPlacedEvent
            await messageBus.PublishAsync(
                new OrderPlacedEvent
                {
                    OrderId = order.Id,
                    ProductId = product.Id,
                    ProductName = product.Name,
                    Quantity = order.Quantity,
                    UnitPrice = product.Price,
                    TotalAmount = order.TotalAmount,
                    CustomerId = order.CustomerId,
                    ShippingAddress = order.ShippingAddress,
                    CreatedAt = order.CreatedAt
                },
                CancellationToken.None);

            await transaction.CommitAsync();

            return Results.Created($"/api/orders/{order.Id}", order);
        });
    });

// Bulk order dispatch — fires thousands of BulkOrderEvents for batch processing demo
app.MapPost(
    "/api/orders/bulk",
    async (BulkOrderRequest request, IMessageBus messageBus, ILogger<Program> logger) =>
    {
        var count = request.Count is > 0 ? request.Count : 2000;

        var products = new[]
        {
            ("Wireless Headphones", 299.99m),
            ("Mechanical Keyboard", 149.99m),
            ("Clean Code", 39.99m)
        };

        logger.LogInformation("Dispatching {Count} BulkOrderEvents", count);
        var sw = System.Diagnostics.Stopwatch.StartNew();

        for (var i = 0; i < count; i++)
        {
            var (name, price) = products[i % products.Length];
            var qty = (i % 5) + 1;

            await messageBus.PublishAsync(
                new BulkOrderEvent
                {
                    OrderId = Guid.NewGuid(),
                    ProductName = name,
                    Quantity = qty,
                    UnitPrice = price,
                    TotalAmount = price * qty,
                    CustomerId = $"bulk-customer-{i:D5}",
                    CreatedAt = DateTimeOffset.UtcNow
                },
                CancellationToken.None);
        }

        sw.Stop();
        logger.LogInformation("Dispatched {Count} BulkOrderEvents in {Elapsed}ms", count, sw.ElapsedMilliseconds);

        return Results.Ok(new { dispatched = count, elapsedMs = sw.ElapsedMilliseconds });
    });

app.MapGet("/api/orders", async (CatalogDbContext db) => await db.Orders.Include(o => o.Product).ToListAsync());

app.MapGet(
    "/api/orders/{id:guid}",
    async (Guid id, CatalogDbContext db) =>
        await db.Orders.Include(o => o.Product).FirstOrDefaultAsync(o => o.Id == id) is { } order
            ? Results.Ok(order)
            : Results.NotFound());

// ============================================
// Saga Endpoints
// ============================================

// Quick Refund Saga - for digital goods or goodwill refunds
app.MapPost(
    "/api/refunds/quick",
    async (QuickRefundRequest request, CatalogDbContext db, IMessageBus messageBus, ILogger<Program> logger) =>
    {
        // Verify order exists
        var order = await db.Orders.FindAsync(request.OrderId);
        if (order is null)
        {
            return Results.NotFound("Order not found");
        }

        logger.LogInformation("Initiating quick refund saga for order {OrderId}", request.OrderId);

        try
        {
            var response = await messageBus.RequestAsync(
                new RequestQuickRefundRequest
                {
                    OrderId = request.OrderId,
                    Amount = request.Amount ?? order.TotalAmount,
                    CustomerId = order.CustomerId,
                    Reason = request.Reason
                },
                CancellationToken.None);

            if (response.Success)
            {
                // Update order status
                order.Status = OrderStatus.Cancelled;
                order.UpdatedAt = DateTimeOffset.UtcNow;
                await db.SaveChangesAsync();
            }

            return Results.Ok(response);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Quick refund saga failed for order {OrderId}", request.OrderId);
            return Results.Problem("Refund processing failed: " + ex.Message);
        }
    });

// Return Processing - creates return label, saga handles the rest async when package arrives
app.MapPost(
    "/api/returns/initiate",
    async (InitiateReturnRequestDto request, CatalogDbContext db, IMessageBus messageBus, ILogger<Program> logger) =>
    {
        // Verify order exists
        var order = await db.Orders.Include(o => o.Product).FirstOrDefaultAsync(o => o.Id == request.OrderId);
        if (order is null)
        {
            return Results.NotFound("Order not found");
        }

        if (order.Status != OrderStatus.Delivered && order.Status != OrderStatus.Shipping)
        {
            return Results.BadRequest($"Order cannot be returned in status: {order.Status}");
        }

        logger.LogInformation("Creating return label for order {OrderId}", request.OrderId);

        try
        {
            // Step 1: Create return label synchronously via Shipping service
            var labelResponse = await messageBus.RequestAsync(
                new CreateReturnLabelCommand
                {
                    OrderId = request.OrderId,
                    OriginalShipmentId = request.ShipmentId,
                    CustomerAddress = order.ShippingAddress,
                    CustomerId = order.CustomerId,
                    // Include order details for saga when package arrives
                    ProductId = order.ProductId,
                    Quantity = order.Quantity,
                    Amount = order.TotalAmount,
                    Reason = request.Reason
                },
                CancellationToken.None);

            if (!labelResponse.Success)
            {
                return Results.Problem($"Failed to create return label: {labelResponse.FailureReason}");
            }

            // Update order status to indicate return in progress
            order.Status = OrderStatus.ReturnInitiated;
            order.UpdatedAt = DateTimeOffset.UtcNow;
            await db.SaveChangesAsync();

            logger.LogInformation(
                "Return label created for order {OrderId}: {ReturnId}, tracking: {Tracking}",
                request.OrderId,
                labelResponse.ReturnId,
                labelResponse.ReturnTrackingNumber);

            // Return immediately - saga will continue when package arrives (ReturnPackageReceivedEvent)
            return Results.Ok(
                new
                {
                    orderId = request.OrderId,
                    returnId = labelResponse.ReturnId,
                    returnTrackingNumber = labelResponse.ReturnTrackingNumber,
                    returnLabelUrl = labelResponse.ReturnLabelUrl,
                    message = "Return label created. Ship the package and we'll process the refund when it arrives."
                });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to create return label for order {OrderId}", request.OrderId);
            return Results.Problem("Failed to create return label: " + ex.Message);
        }
    });

app.MapMessageBus();

app.Run();

public record PlaceOrderRequest(Guid ProductId, int Quantity, string CustomerId, string ShippingAddress);

public record QuickRefundRequest(Guid OrderId, decimal? Amount, string Reason);

public record InitiateReturnRequestDto(Guid OrderId, Guid ShipmentId, string Reason);

public record BulkOrderRequest(int Count);
