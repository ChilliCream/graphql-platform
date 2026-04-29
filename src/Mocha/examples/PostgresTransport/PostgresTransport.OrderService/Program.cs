using System.Diagnostics;
using Mocha;
using Mocha.Transport.Postgres;
using PostgresTransport.Contracts.Commands;
using PostgresTransport.Contracts.Events;
using PostgresTransport.Contracts.Requests;
using PostgresTransport.OrderService;
using PostgresTransport.OrderService.Handlers;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

var messagingConnectionString =
    builder.Configuration.GetConnectionString("messaging-db")
    ?? "Host=localhost;Database=mocha_messaging;Username=postgres;Password=postgres";

builder
    .Services.AddMessageBus()
    .AddInstrumentation()
    .AddEventHandler<OrderShippedEventHandler>()
    .AddRequestHandler<GetOrderStatusRequestHandler>()
    .AddBatchHandler<OrderAnalyticsBatchHandler>(o =>
    {
        o.MaxBatchSize = 100;
        o.BatchTimeout = TimeSpan.FromSeconds(2);
    })
    .AddEventHandler<ProcessOrderCommandHandler>()
    .AddPostgres(t =>
    {
        t.ConnectionString(messagingConnectionString);

        // Explicit topology for Send pattern demo
        t.DeclareQueue("process-order");
        t.Endpoint("process-order-ep").Queue("process-order").Handler<ProcessOrderCommandHandler>();
        t.DispatchEndpoint("send-demo").ToQueue("process-order").Send<ProcessOrderCommand>();
    });

builder.Services.AddHostedService<OrderSimulatorWorker>();

var app = builder.Build();

app.MapDefaultEndpoints();
app.UseDefaultFiles();
app.UseStaticFiles();

// ---------------------------------------------------------------------------
//  Demo API
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

// ---------------------------------------------------------------------------
//  Original API
// ---------------------------------------------------------------------------

app.MapPost(
    "/api/orders",
    async (PlaceOrderRequest request, IMessageBus messageBus) =>
    {
        var orderId = Guid.NewGuid();

        await messageBus.PublishAsync(
            new OrderPlacedEvent
            {
                OrderId = orderId,
                ProductName = request.ProductName,
                Quantity = request.Quantity,
                TotalAmount = request.UnitPrice * request.Quantity,
                CustomerEmail = request.CustomerEmail,
                PlacedAt = DateTimeOffset.UtcNow
            },
            CancellationToken.None);

        return Results.Created($"/api/orders/{orderId}", new { orderId, status = "Placed" });
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

app.Run();

public record PlaceOrderRequest(string ProductName, int Quantity, decimal UnitPrice, string CustomerEmail);
