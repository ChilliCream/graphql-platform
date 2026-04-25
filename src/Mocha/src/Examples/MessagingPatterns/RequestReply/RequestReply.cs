// To run without a project file:
// #:package Mocha@1.0.0-preview.*
// #:package Mocha.Resources.AspNetCore@1.0.0-preview.*
// #:package Mocha.Transport.InMemory@1.0.0-preview.*
// $ dotnet run RequestReply.cs

using Mocha;
using Mocha.Events;
using Mocha.Resources;
using Mocha.Resources.AspNetCore;
using Mocha.Transport.InMemory;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddMessageBus()
    .AddRequestHandler<ProcessRefundCommandHandler>()
    .AddInMemory();

builder.Services.AddMochaMessageBusResources();

var app = builder.Build();

app.MapGet("/refund", async (IMessageBus bus, ILogger<Program> logger) =>
{
    var orderId = Guid.NewGuid();

    try
    {
        var response = await bus.RequestAsync(
            new ProcessRefundCommand
            {
                OrderId = orderId,
                Amount = 49.99m,
                Reason = "Defective product",
                CustomerId = "customer-42"
            },
            CancellationToken.None);

        Console.WriteLine($"Refund {response.RefundId}: {response.Amount:C}, success={response.Success}");

        return Results.Ok(response);
    }
    catch (ResponseTimeoutException ex)
    {
        logger.LogWarning(
            "Refund request timed out for order {OrderId}: {Message}",
            orderId,
            ex.Message);

        return Results.StatusCode(504);
    }
});

Console.WriteLine("Listening on http://localhost:5000/refund");

if (app.Environment.IsDevelopment())
{
    app.MapMochaResourceEndpoint();
}

app.Run();

public sealed record ProcessRefundCommand : IEventRequest<ProcessRefundResponse>
{
    public required Guid OrderId { get; init; }
    public required decimal Amount { get; init; }
    public required string Reason { get; init; }
    public required string CustomerId { get; init; }
}

public sealed record ProcessRefundResponse
{
    public required Guid RefundId { get; init; }
    public required Guid OrderId { get; init; }
    public required decimal Amount { get; init; }
    public required bool Success { get; init; }
    public string? FailureReason { get; init; }
    public required DateTimeOffset ProcessedAt { get; init; }
}

public class ProcessRefundCommandHandler(ILogger<ProcessRefundCommandHandler> logger)
    : IEventRequestHandler<ProcessRefundCommand, ProcessRefundResponse>
{
    public async ValueTask<ProcessRefundResponse> HandleAsync(
        ProcessRefundCommand request,
        CancellationToken cancellationToken)
    {
        logger.LogInformation(
            "Processing refund of {Amount} for order {OrderId}",
            request.Amount,
            request.OrderId);

        // Process refund logic here
        await Task.CompletedTask;

        return new ProcessRefundResponse
        {
            RefundId = Guid.NewGuid(),
            OrderId = request.OrderId,
            Amount = request.Amount,
            Success = true,
            ProcessedAt = DateTimeOffset.UtcNow
        };
    }
}
