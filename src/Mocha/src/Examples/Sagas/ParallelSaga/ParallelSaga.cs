// To run without a project file:
// #:package Mocha@1.0.0-preview.*
// #:package Mocha.Resources.AspNetCore@1.0.0-preview.*
// #:package Mocha.Transport.InMemory@1.0.0-preview.*
// $ dotnet run ParallelSaga.cs

using Mocha;
using Mocha.Events;
using Mocha.Resources;
using Mocha.Resources.AspNetCore;
using Mocha.Sagas;
using Mocha.Transport.InMemory;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddMessageBus()
    .AddSaga<ReturnProcessingSaga>()
    .AddRequestHandler<InspectReturnCommandHandler>()
    .AddRequestHandler<RestockInventoryCommandHandler>()
    .AddRequestHandler<ProcessRefundCommandHandler>()
    .AddInMemory();

builder.Services.AddMochaMessageBusResources();

var app = builder.Build();

app.MapGet("/return", async (IMessageBus bus) =>
{
    // Publish the event that initiates the saga
    await bus.PublishAsync(
        new ReturnPackageReceivedEvent
        {
            ReturnId = Guid.NewGuid(),
            OrderId = Guid.NewGuid(),
            CustomerId = "customer-42",
            Quantity = 2,
            Amount = 99.98m,
            ReceivedAt = DateTimeOffset.UtcNow
        },
        CancellationToken.None);

    return Results.Ok(new { Status = "Return processing started" });
});

Console.WriteLine("Listening on http://localhost:5000/return");

if (app.Environment.IsDevelopment())
{
    app.MapMochaResourceEndpoint();
}

app.Run();

// ── Message contracts ────────────────────────────────────────────────────────

// Initiating event: triggers the saga when a package is received
public sealed class ReturnPackageReceivedEvent
{
    public required Guid ReturnId { get; init; }
    public required Guid OrderId { get; init; }
    public required string CustomerId { get; init; }
    public required int Quantity { get; init; }
    public required decimal Amount { get; init; }
    public required DateTimeOffset ReceivedAt { get; init; }
}

// Inspection command and reply
public sealed class InspectReturnCommand : IEventRequest<InspectReturnResponse>
{
    public required Guid ReturnId { get; init; }
    public required Guid OrderId { get; init; }
}

public sealed class InspectReturnResponse
{
    public required Guid ReturnId { get; init; }
    public required string Result { get; init; }
}

// Restock command and reply
public sealed class RestockInventoryCommand : IEventRequest<RestockInventoryResponse>
{
    public required Guid ReturnId { get; init; }
    public required Guid OrderId { get; init; }
    public required int Quantity { get; init; }
}

public sealed class RestockInventoryResponse
{
    public required Guid ReturnId { get; init; }
    public required bool Success { get; init; }
    public required int QuantityRestocked { get; init; }
}

// Refund command and reply
public sealed class ProcessRefundCommand : IEventRequest<ProcessRefundResponse>
{
    public required Guid ReturnId { get; init; }
    public required Guid OrderId { get; init; }
    public required decimal Amount { get; init; }
    public required string CustomerId { get; init; }
}

public sealed class ProcessRefundResponse
{
    public required Guid RefundId { get; init; }
    public required Guid ReturnId { get; init; }
    public required decimal Amount { get; init; }
    public required bool Success { get; init; }
    public string? FailureReason { get; init; }
}

// ── Saga state ───────────────────────────────────────────────────────────────

public class ReturnSagaState : SagaStateBase
{
    public required Guid ReturnId { get; init; }
    public required Guid OrderId { get; init; }
    public required string CustomerId { get; init; }
    public required int Quantity { get; init; }
    public required decimal Amount { get; init; }

    public string? InspectionResult { get; set; }
    public bool InventoryRestocked { get; set; }
    public int QuantityRestocked { get; set; }
    public Guid? RefundId { get; set; }
    public decimal? RefundedAmount { get; set; }
    public string? FailureReason { get; set; }

    public static ReturnSagaState FromReturnPackageReceived(ReturnPackageReceivedEvent e)
        => new()
        {
            ReturnId = e.ReturnId,
            OrderId = e.OrderId,
            CustomerId = e.CustomerId,
            Quantity = e.Quantity,
            Amount = e.Amount
        };

    public InspectReturnCommand ToInspectReturn()
        => new() { ReturnId = ReturnId, OrderId = OrderId };

    public RestockInventoryCommand ToRestockInventory()
        => new() { ReturnId = ReturnId, OrderId = OrderId, Quantity = Quantity };

    public ProcessRefundCommand ToProcessRefund()
        => new() { ReturnId = ReturnId, OrderId = OrderId, Amount = Amount, CustomerId = CustomerId };
}

// ── Saga definition ──────────────────────────────────────────────────────────

public sealed class ReturnProcessingSaga : Saga<ReturnSagaState>
{
    private const string AwaitingInspection = nameof(AwaitingInspection);
    private const string AwaitingBothReplies = nameof(AwaitingBothReplies);
    private const string RestockDoneAwaitingRefund = nameof(RestockDoneAwaitingRefund);
    private const string RefundDoneAwaitingRestock = nameof(RefundDoneAwaitingRestock);
    private const string Completed = nameof(Completed);
    private const string Failed = nameof(Failed);

    protected override void Configure(ISagaDescriptor<ReturnSagaState> descriptor)
    {
        // Start: package received, send inspection command
        descriptor
            .Initially()
            .OnEvent<ReturnPackageReceivedEvent>()
            .StateFactory(ReturnSagaState.FromReturnPackageReceived)
            .Send((_, state) => state.ToInspectReturn())
            .TransitionTo(AwaitingInspection);

        // After inspection: send refund AND restock in parallel
        descriptor
            .During(AwaitingInspection)
            .OnReply<InspectReturnResponse>()
            .Then((state, response) => state.InspectionResult = response.Result)
            .Send((_, state) => state.ToRestockInventory())
            .Send((_, state) => state.ToProcessRefund())
            .TransitionTo(AwaitingBothReplies);

        // Fault during inspection: compensate by marking failed
        descriptor
            .During(AwaitingInspection)
            .OnFault()
            .Then((state, fault) => state.FailureReason = $"Inspection failed: {fault.ErrorMessage}")
            .TransitionTo(Failed);

        // Restock arrives first
        descriptor
            .During(AwaitingBothReplies)
            .OnReply<RestockInventoryResponse>()
            .Then((state, response) =>
            {
                state.InventoryRestocked = response.Success;
                state.QuantityRestocked = response.QuantityRestocked;
            })
            .TransitionTo(RestockDoneAwaitingRefund);

        // Refund arrives first
        descriptor
            .During(AwaitingBothReplies)
            .OnReply<ProcessRefundResponse>()
            .Then((state, response) =>
            {
                if (response.Success)
                {
                    state.RefundId = response.RefundId;
                    state.RefundedAmount = response.Amount;
                }
                else
                {
                    state.FailureReason = response.FailureReason;
                }
            })
            .TransitionTo(RefundDoneAwaitingRestock);

        // Fault while waiting for either reply: compensation
        descriptor
            .During(AwaitingBothReplies)
            .OnFault()
            .Then((state, fault) => state.FailureReason = $"Parallel step failed: {fault.ErrorMessage}")
            .TransitionTo(Failed);

        // Second reply arrives: restock after refund
        descriptor
            .During(RefundDoneAwaitingRestock)
            .OnReply<RestockInventoryResponse>()
            .Then((state, response) =>
            {
                state.InventoryRestocked = response.Success;
                state.QuantityRestocked = response.QuantityRestocked;
            })
            .TransitionTo(Completed);

        // Second reply arrives: refund after restock
        descriptor
            .During(RestockDoneAwaitingRefund)
            .OnReply<ProcessRefundResponse>()
            .Then((state, response) =>
            {
                if (response.Success)
                {
                    state.RefundId = response.RefundId;
                    state.RefundedAmount = response.Amount;
                }
                else
                {
                    state.FailureReason = response.FailureReason;
                }
            })
            .TransitionTo(Completed);

        // Done - no response since the saga was initiated by a published event (OnEvent)
        descriptor.Finally(Completed);

        // Failed final state
        descriptor.Finally(Failed);
    }
}

// ── Service handlers (simulate remote services) ──────────────────────────────

public class InspectReturnCommandHandler(ILogger<InspectReturnCommandHandler> logger)
    : IEventRequestHandler<InspectReturnCommand, InspectReturnResponse>
{
    public async ValueTask<InspectReturnResponse> HandleAsync(
        InspectReturnCommand request,
        CancellationToken cancellationToken)
    {
        logger.LogInformation("Inspecting return {ReturnId} for order {OrderId}", request.ReturnId, request.OrderId);

        await Task.CompletedTask;

        return new InspectReturnResponse
        {
            ReturnId = request.ReturnId,
            Result = "Approved"
        };
    }
}

public class RestockInventoryCommandHandler(ILogger<RestockInventoryCommandHandler> logger)
    : IEventRequestHandler<RestockInventoryCommand, RestockInventoryResponse>
{
    public async ValueTask<RestockInventoryResponse> HandleAsync(
        RestockInventoryCommand request,
        CancellationToken cancellationToken)
    {
        logger.LogInformation("Restocking {Quantity} items for return {ReturnId}", request.Quantity, request.ReturnId);

        await Task.CompletedTask;

        return new RestockInventoryResponse
        {
            ReturnId = request.ReturnId,
            Success = true,
            QuantityRestocked = request.Quantity
        };
    }
}

public class ProcessRefundCommandHandler(ILogger<ProcessRefundCommandHandler> logger)
    : IEventRequestHandler<ProcessRefundCommand, ProcessRefundResponse>
{
    public async ValueTask<ProcessRefundResponse> HandleAsync(
        ProcessRefundCommand request,
        CancellationToken cancellationToken)
    {
        logger.LogInformation(
            "Processing refund of {Amount} for return {ReturnId}",
            request.Amount,
            request.ReturnId);

        await Task.CompletedTask;

        return new ProcessRefundResponse
        {
            RefundId = Guid.NewGuid(),
            ReturnId = request.ReturnId,
            Amount = request.Amount,
            Success = true
        };
    }
}
