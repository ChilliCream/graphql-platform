// To run without a project file:
// #:package Mocha@1.0.0-preview.*
// #:package Mocha.Transport.InMemory@1.0.0-preview.*
// $ dotnet run SendFireAndForget.cs

using Mocha;
using Mocha.Transport.InMemory;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddMessageBus()
    .AddRequestHandler<ReserveInventoryCommandHandler>()
    .AddInMemory();

var app = builder.Build();

app.MapGet("/reserve", async (IMessageBus bus) =>
{
    await bus.SendAsync(new ReserveInventoryCommand
    {
        OrderId = Guid.NewGuid(),
        ProductId = Guid.NewGuid(),
        Quantity = 3
    }, CancellationToken.None);

    return Results.Ok();
});

Console.WriteLine("Listening on http://localhost:5000/reserve");

app.Run();

public sealed record ReserveInventoryCommand
{
    public required Guid OrderId { get; init; }
    public required Guid ProductId { get; init; }
    public required int Quantity { get; init; }
}

public class ReserveInventoryCommandHandler(ILogger<ReserveInventoryCommandHandler> logger)
    : IEventRequestHandler<ReserveInventoryCommand>
{
    public async ValueTask HandleAsync(
        ReserveInventoryCommand request,
        CancellationToken cancellationToken)
    {
        logger.LogInformation(
            "Reserving {Quantity} units of product {ProductId} for order {OrderId}",
            request.Quantity,
            request.ProductId,
            request.OrderId);

        // Reserve inventory logic here
        await Task.CompletedTask;

        logger.LogInformation(
            "Reserved {Quantity} units for order {OrderId}",
            request.Quantity,
            request.OrderId);
    }
}
