using Demo.Catalog.Commands;
using Demo.Catalog.Data;
using Demo.Catalog.Queries;
using Microsoft.EntityFrameworkCore;
using Mocha;
using Mocha.EntityFrameworkCore;
using Mocha.Hosting;
using Mocha.Mediator;
using Mocha.Inbox;
using Mocha.Outbox;
using Mocha.Sagas;
using Mocha.Transport.RabbitMQ;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

// Database
builder.AddNpgsqlDbContext<CatalogDbContext>("catalog-db", x => x.DisableTracing = true);

// RabbitMQ
builder.AddRabbitMQClient("rabbitmq", x => x.DisableTracing = true);

// Mocha.Mediator
builder.Services.AddMediator()
    .AddCatalog()
    .AddInstrumentation()
    .UseEntityFrameworkTransactions<CatalogDbContext>();

// MessageBus
builder
    .Services.AddMessageBus()
    .AddInstrumentation()
    .AddResilience()
    .AddCatalog()
    .AddEntityFramework<CatalogDbContext>(p =>
    {
        p.AddPostgresSagas();

        // dispatch
        p.UsePostgresOutbox();

        // receive
        p.UseResilience();
        p.UseTransaction();
        p.UsePostgresInbox();
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
app.MapGet("/api/products", async (ISender sender) =>
    await sender.QueryAsync(new GetProductsQuery()));

app.MapGet("/api/products/{id:guid}", async (Guid id, ISender sender) =>
    await sender.QueryAsync(new GetProductByIdQuery(id)) is { } product
        ? Results.Ok(product)
        : Results.NotFound());

// Categories
app.MapGet("/api/categories", async (ISender sender) =>
    await sender.QueryAsync(new GetCategoriesQuery()));

// Orders - placing an order triggers OrderPlacedEvent
app.MapPost("/api/orders", async (PlaceOrderRequest request, ISender sender) =>
{
    var result = await sender.SendAsync(
        new PlaceOrderCommand(request.ProductId, request.Quantity, request.CustomerId, request.ShippingAddress));

    return result.Success
        ? Results.Created($"/api/orders/{result.Order!.Id}", result.Order)
        : Results.BadRequest(result.Error);
});

// Bulk order dispatch
app.MapPost("/api/orders/bulk", async (BulkOrderRequest request, ISender sender) =>
{
    var result = await sender.SendAsync(new PlaceBulkOrderCommand(request.Count));
    return Results.Ok(new { dispatched = result.Dispatched, elapsedMs = result.ElapsedMs });
});

app.MapGet("/api/orders", async (ISender sender) =>
    await sender.QueryAsync(new GetOrdersQuery()));

app.MapGet("/api/orders/{id:guid}", async (Guid id, ISender sender) =>
    await sender.QueryAsync(new GetOrderByIdQuery(id)) is { } order
        ? Results.Ok(order)
        : Results.NotFound());

// Quick Refund Saga
app.MapPost("/api/refunds/quick", async (QuickRefundRequest request, ISender sender) =>
{
    var result = await sender.SendAsync(
        new RequestQuickRefundCommand(request.OrderId, request.Amount, request.Reason));

    if (!result.Success)
    {
        return result.Error == "Order not found" ? Results.NotFound(result.Error) : Results.Problem(result.Error);
    }

    return Results.Ok(result.Response);
});

// Return Processing
app.MapPost("/api/returns/initiate", async (InitiateReturnRequestDto request, ISender sender) =>
{
    var result = await sender.SendAsync(
        new InitiateReturnCommand(request.OrderId, request.ShipmentId, request.Reason));

    if (!result.Success)
    {
        if (result.Error!.Contains("not found"))
        {
            return Results.NotFound(result.Error);
        }

        if (result.Error.Contains("cannot be returned"))
        {
            return Results.BadRequest(result.Error);
        }

        return Results.Problem(result.Error);
    }

    return Results.Ok(new
    {
        orderId = request.OrderId,
        returnId = result.ReturnId,
        returnTrackingNumber = result.ReturnTrackingNumber,
        returnLabelUrl = result.ReturnLabelUrl,
        message = "Return label created. Ship the package and we'll process the refund when it arrives."
    });
});

app.MapMessageBusDeveloperTopology();

app.Run();

public record PlaceOrderRequest(Guid ProductId, int Quantity, string CustomerId, string ShippingAddress);

public record QuickRefundRequest(Guid OrderId, decimal? Amount, string Reason);

public record InitiateReturnRequestDto(Guid OrderId, Guid ShipmentId, string Reason);

public record BulkOrderRequest(int Count);
