using Mocha;
using Mocha.Hosting;
using Mocha.Mediator;
using Mocha.Sagas;
using Mocha.Transport.AzureEventHub;
using AzureEventHubTransport.Contracts.Events;
using AzureEventHubTransport.Contracts.Mediator;
using AzureEventHubTransport.Contracts.Sagas;
using AzureEventHubTransport.OrderService;
using AzureEventHubTransport.OrderService.Handlers;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

var eventHubConnectionString =
    builder.Configuration.GetConnectionString("eventhubs")
    ?? "Endpoint=sb://localhost;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=SAS_KEY_VALUE;UseDevelopmentEmulator=true";

// ---------------------------------------------------------------------------
//  Mediator — local in-process CQRS for order creation and queries
// ---------------------------------------------------------------------------
builder
    .Services.AddMediator()
    .AddInstrumentation()
    .AddHandler<CreateOrderCommandHandler>()
    .AddHandler<GetOrderStatusQueryHandler>();

// ---------------------------------------------------------------------------
//  Message Bus — distributed events via Azure Event Hub + saga orchestration
// ---------------------------------------------------------------------------
builder.Services.AddInMemorySagas();

builder
    .Services.AddMessageBus()
    .AddInstrumentation()
    .AddEventHandler<ProcessPaymentCommandHandler>()
    .AddEventHandler<OrderFulfilledEventHandler>()
    .AddSaga<OrderFulfillmentSaga>()
    .AddEventHub(t => t.ConnectionString(eventHubConnectionString));

builder.Services.AddHostedService<OrderSimulatorWorker>();

var app = builder.Build();

app.MapDefaultEndpoints();

// ---------------------------------------------------------------------------
//  API — uses mediator for local commands/queries, bus publishes events
// ---------------------------------------------------------------------------

app.MapPost(
    "/api/orders",
    async (PlaceOrderRequest request, ISender sender, IMessageBus messageBus, CancellationToken ct) =>
    {
        // 1. Local command via mediator: validate, persist, return result
        var result = await sender.SendAsync(
            new CreateOrderCommand
            {
                ProductName = request.ProductName,
                Quantity = request.Quantity,
                UnitPrice = request.UnitPrice,
                CustomerEmail = request.CustomerEmail
            },
            ct);

        // 2. Publish event to the bus — this kicks off the fulfillment saga
        await messageBus.PublishAsync(
            new OrderPlacedEvent
            {
                OrderId = result.OrderId,
                ProductName = request.ProductName,
                Quantity = request.Quantity,
                TotalAmount = result.TotalAmount,
                CustomerEmail = request.CustomerEmail,
                PlacedAt = DateTimeOffset.UtcNow
            },
            ct);

        return Results.Created($"/api/orders/{result.OrderId}", result);
    });

app.MapGet(
    "/api/orders/{orderId:guid}/status",
    async (Guid orderId, ISender sender) =>
    {
        var status = await sender.QueryAsync(
            new GetOrderStatusQuery { OrderId = orderId });

        return Results.Ok(status);
    });

app.MapMessageBusDeveloperTopology();

app.Run();

public record PlaceOrderRequest(string ProductName, int Quantity, decimal UnitPrice, string CustomerEmail);
