using System.Diagnostics;
using Mocha;
using Mocha.Hosting;
using Mocha.Mediator;
using Mocha.Sagas;
using Mocha.Transport.AzureServiceBus;
using AzureServiceBusTransport.Contracts.Commands;
using AzureServiceBusTransport.Contracts.Events;
using AzureServiceBusTransport.Contracts.Requests;
using AzureServiceBusTransport.OrderService;
using AzureServiceBusTransport.OrderService.Handlers;
using AzureServiceBusTransport.OrderService.Mediator.Commands;
using AzureServiceBusTransport.OrderService.Mediator.Notifications;
using AzureServiceBusTransport.OrderService.Mediator.Queries;
using AzureServiceBusTransport.OrderService.Sagas;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

var connectionString =
    builder.Configuration.GetConnectionString("messaging")
    ?? throw new InvalidOperationException("Connection string 'messaging' not found.");

// ---------------------------------------------------------------------------
//  Mediator — in-process CQRS for API-to-domain dispatch
// ---------------------------------------------------------------------------

builder
    .Services.AddMediator()
    .AddInstrumentation()
    .AddHandler<PlaceOrderCommandHandler>()
    .AddHandler<GetOrderDetailsQueryHandler>()
    .AddHandler<OrderActivityNotificationHandler>();

// ---------------------------------------------------------------------------
//  Message Bus — distributed messaging with saga orchestration
// ---------------------------------------------------------------------------

builder
    .Services.AddMessageBus()
    .AddInstrumentation()
    .Host(h => h.InstanceId(Guid.Parse("00000000-0000-0000-0000-000000000001")))
    .AddEventHandler<OrderShippedEventHandler>()
    .AddRequestHandler<GetOrderStatusRequestHandler>()
    .AddBatchHandler<OrderAnalyticsBatchHandler>(o =>
    {
        o.MaxBatchSize = 100;
        o.BatchTimeout = TimeSpan.FromSeconds(2);
    })
    .AddEventHandler<ProcessOrderCommandHandler>()
    .AddSaga<OrderFulfillmentSaga>()
    .AddAzureServiceBus(t =>
    {
        t.ConnectionString(connectionString);
        t.AutoProvision(false);

        // Explicit topology for Send pattern demo
        t.DeclareQueue("process-order");
        t.Endpoint("process-order-ep").Queue("process-order").Handler<ProcessOrderCommandHandler>();
        t.DispatchEndpoint("send-demo").ToQueue("process-order").Send<ProcessOrderCommand>();

        // Saga topology — sends PrepareShipmentCommand to the shipping service
        t.DeclareQueue("prepare-shipment");
        t.DispatchEndpoint("saga-shipment").ToQueue("prepare-shipment").Send<PrepareShipmentCommand>();
    });

builder.Services.AddInMemorySagas();

builder.Services.AddHostedService<OrderSimulatorWorker>();

var app = builder.Build();

app.MapDefaultEndpoints();
app.UseDefaultFiles();
app.UseStaticFiles();

// ---------------------------------------------------------------------------
//  Mediator API — dispatched in-process via IMediator
// ---------------------------------------------------------------------------

app.MapPost(
    "/api/orders",
    async (PlaceOrderRequest request, IMediator mediator) =>
    {
        var result = await mediator.SendAsync(
            new PlaceOrderCommand
            {
                ProductName = request.ProductName,
                Quantity = request.Quantity,
                UnitPrice = request.UnitPrice,
                CustomerEmail = request.CustomerEmail
            });

        await mediator.PublishAsync(
            new OrderActivityNotification
            {
                OrderId = result.OrderId,
                Activity = "OrderPlaced"
            });

        return Results.Created($"/api/orders/{result.OrderId}", result);
    });

app.MapGet(
    "/api/orders/{orderId:guid}",
    async (Guid orderId, IMediator mediator) =>
    {
        var details = await mediator.QueryAsync(
            new GetOrderDetailsQuery { OrderId = orderId });

        return Results.Ok(details);
    });

// ---------------------------------------------------------------------------
//  Saga API — orchestrates fulfillment across services
// ---------------------------------------------------------------------------

app.MapPost(
    "/api/demo/fulfill",
    async (IMessageBus messageBus, CancellationToken ct) =>
    {
        var sw = Stopwatch.StartNew();
        var orderId = Guid.NewGuid();

        var response = await messageBus.RequestAsync(
            new FulfillOrderRequest
            {
                OrderId = orderId,
                ProductName = "Demo Widget",
                Quantity = 2,
                TotalAmount = 59.98m,
                CustomerEmail = "demo@example.com"
            },
            ct);

        return Results.Ok(
            new
            {
                response.OrderId,
                response.Status,
                response.TrackingNumber,
                response.Carrier,
                response.FulfilledAt,
                elapsedMs = sw.ElapsedMilliseconds
            });
    });

// ---------------------------------------------------------------------------
//  Demo API — direct bus operations
// ---------------------------------------------------------------------------

app.MapPost(
    "/api/demo/publish",
    async (IMessageBus messageBus, CancellationToken ct) =>
    {
        var sw = Stopwatch.StartNew();
        var orderId = Guid.NewGuid();

        await messageBus.PublishAsync(
            new OrderPlacedEvent
            {
                OrderId = orderId,
                ProductName = "Demo Widget",
                Quantity = 1,
                TotalAmount = 29.99m,
                CustomerEmail = "demo@example.com",
                PlacedAt = DateTimeOffset.UtcNow
            },
            ct);

        return Results.Ok(new { orderId, elapsedMs = sw.ElapsedMilliseconds });
    });

app.MapPost(
    "/api/demo/send",
    async (IMessageBus messageBus, CancellationToken ct) =>
    {
        var sw = Stopwatch.StartNew();
        var orderId = Guid.NewGuid();

        await messageBus.SendAsync(
            new ProcessOrderCommand
            {
                OrderId = orderId,
                Action = "validate",
                RequestedAt = DateTimeOffset.UtcNow
            },
            ct);

        return Results.Ok(
            new
            {
                orderId,
                action = "validate",
                elapsedMs = sw.ElapsedMilliseconds
            });
    });

app.MapPost(
    "/api/demo/request-reply",
    async (IMessageBus messageBus, CancellationToken ct) =>
    {
        var sw = Stopwatch.StartNew();
        var orderId = Guid.NewGuid();

        var response = await messageBus.RequestAsync(new GetOrderStatusRequest { OrderId = orderId }, ct);

        return Results.Ok(
            new
            {
                request = new { orderId },
                response = new
                {
                    response.OrderId,
                    response.Status,
                    response.UpdatedAt
                },
                elapsedMs = sw.ElapsedMilliseconds
            });
    });

app.MapPost(
    "/api/demo/mediator",
    async (IMediator mediator, CancellationToken ct) =>
    {
        var sw = Stopwatch.StartNew();

        var result = await mediator.SendAsync(
            new PlaceOrderCommand
            {
                ProductName = "Mediator Widget",
                Quantity = 1,
                UnitPrice = 19.99m,
                CustomerEmail = "mediator@example.com"
            },
            ct);

        await mediator.PublishAsync(
            new OrderActivityNotification
            {
                OrderId = result.OrderId,
                Activity = "OrderPlaced"
            },
            ct);

        return Results.Ok(
            new
            {
                result.OrderId,
                result.Status,
                elapsedMs = sw.ElapsedMilliseconds
            });
    });

app.MapPost(
    "/api/demo/batch",
    async (IServiceScopeFactory scopeFactory, CancellationToken ct) =>
    {
        var sw = Stopwatch.StartNew();
        const int count = 500;
        const int workers = 10;
        const int perWorker = count / workers;

        var tasks = Enumerable
            .Range(0, workers)
            .Select(async _ =>
            {
                await using var scope = scopeFactory.CreateAsyncScope();
                var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

                for (var i = 0; i < perWorker; i++)
                {
                    await bus.PublishAsync(
                        new OrderPlacedEvent
                        {
                            OrderId = Guid.NewGuid(),
                            ProductName = "Batch Item",
                            Quantity = Random.Shared.Next(1, 10),
                            TotalAmount = Math.Round(Random.Shared.Next(999, 9999) / 100m, 2),
                            CustomerEmail = $"batch-{Random.Shared.Next(1, 50)}@example.com",
                            PlacedAt = DateTimeOffset.UtcNow
                        },
                        ct);
                }
            });

        await Task.WhenAll(tasks);

        return Results.Ok(
            new
            {
                count,
                elapsedMs = sw.ElapsedMilliseconds,
                messagesPerSecond = count * 1000.0 / Math.Max(1, sw.ElapsedMilliseconds)
            });
    });

app.MapPost(
    "/api/demo/bulk-publish",
    async (HttpContext context, IServiceScopeFactory scopeFactory, CancellationToken ct) =>
    {
        var countStr = context.Request.Query["count"].FirstOrDefault();
        var total = int.TryParse(countStr, out var c) ? Math.Clamp(c, 1, 100_000) : 50_000;
        var sw = Stopwatch.StartNew();
        const int workers = 10;
        var perWorker = total / workers;
        var remainder = total % workers;

        var tasks = Enumerable
            .Range(0, workers)
            .Select(async worker =>
            {
                var workerCount = perWorker + (worker < remainder ? 1 : 0);
                await using var scope = scopeFactory.CreateAsyncScope();
                var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

                for (var i = 0; i < workerCount; i++)
                {
                    await bus.PublishAsync(
                        new OrderPlacedEvent
                        {
                            OrderId = Guid.NewGuid(),
                            ProductName = "Bulk Item",
                            Quantity = 1,
                            TotalAmount = 9.99m,
                            CustomerEmail = "bulk@example.com",
                            PlacedAt = DateTimeOffset.UtcNow
                        },
                        ct);
                }
            });

        await Task.WhenAll(tasks);

        return Results.Ok(
            new
            {
                count = total,
                elapsedMs = sw.ElapsedMilliseconds,
                messagesPerSecond = total * 1000.0 / Math.Max(1, sw.ElapsedMilliseconds)
            });
    });

app.MapGet(
    "/api/orders/{orderId:guid}/status",
    async (Guid orderId, IMessageBus messageBus) =>
    {
        var response = await messageBus.RequestAsync(
            new GetOrderStatusRequest { OrderId = orderId },
            CancellationToken.None);

        return Results.Ok(response);
    });

app.MapMessageBusDeveloperTopology();

app.Run();

public record PlaceOrderRequest(string ProductName, int Quantity, decimal UnitPrice, string CustomerEmail);
