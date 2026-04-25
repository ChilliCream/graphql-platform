// To run without a project file:
// #:package Mocha@1.0.0-preview.*
// #:package Mocha.Resources.AspNetCore@1.0.0-preview.*
// #:package Mocha.Transport.InMemory@1.0.0-preview.*
// $ dotnet run BasicSaga.cs

using Mocha;
using Mocha.Resources;
using Mocha.Resources.AspNetCore;
using Mocha.Sagas;
using Mocha.Transport.InMemory;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddMessageBus()
    .AddSaga<QuickRefundSaga>()
    .AddRequestHandler<ProcessRefundCommandHandler>()
    .AddInMemory();

builder.Services.AddMochaMessageBusResources();

var app = builder.Build();

app.MapGet("/refund", async (IMessageBus bus) =>
{
    var response = await bus.RequestAsync(
        new RequestQuickRefundRequest
        {
            OrderId = Guid.NewGuid(),
            Amount = 49.99m,
            CustomerId = "customer-42",
            Reason = "Defective product"
        },
        CancellationToken.None);

    return Results.Ok(new
    {
        response.OrderId,
        response.Success,
        response.RefundId,
        response.RefundedAmount,
        response.CompletedAt
    });
});

Console.WriteLine("Listening on http://localhost:5000/refund");

if (app.Environment.IsDevelopment())
{
    app.MapMochaResourceEndpoint();
}

app.Run();

// ── Message contracts ────────────────────────────────────────────────────────

public sealed class RequestQuickRefundRequest : IEventRequest<QuickRefundResponse>
{
    public required Guid OrderId { get; init; }
    public required decimal Amount { get; init; }
    public required string CustomerId { get; init; }
    public required string Reason { get; init; }
}

public sealed class QuickRefundResponse
{
    public required Guid OrderId { get; init; }
    public required bool Success { get; init; }
    public Guid? RefundId { get; init; }
    public decimal? RefundedAmount { get; init; }
    public string? FailureReason { get; init; }
    public required DateTimeOffset CompletedAt { get; init; }
}

public sealed class ProcessRefundCommand : IEventRequest<ProcessRefundResponse>
{
    public required Guid OrderId { get; init; }
    public required decimal Amount { get; init; }
    public required string Reason { get; init; }
    public required string CustomerId { get; init; }
}

public sealed class ProcessRefundResponse
{
    public required Guid RefundId { get; init; }
    public required Guid OrderId { get; init; }
    public required decimal Amount { get; init; }
    public required bool Success { get; init; }
    public string? FailureReason { get; init; }
    public required DateTimeOffset ProcessedAt { get; init; }
}

// ── Saga state ───────────────────────────────────────────────────────────────

public class RefundSagaState : SagaStateBase
{
    public required Guid OrderId { get; init; }
    public required decimal Amount { get; init; }
    public required string CustomerId { get; init; }
    public required string Reason { get; init; }

    public Guid? RefundId { get; set; }
    public decimal? RefundedAmount { get; set; }
    public string? FailureReason { get; set; }

    public static RefundSagaState FromQuickRefund(RequestQuickRefundRequest request)
        => new()
        {
            OrderId = request.OrderId,
            Amount = request.Amount,
            CustomerId = request.CustomerId,
            Reason = request.Reason
        };

    public ProcessRefundCommand ToProcessRefund()
        => new()
        {
            OrderId = OrderId,
            Amount = Amount,
            Reason = Reason,
            CustomerId = CustomerId
        };
}

// ── Saga definition ──────────────────────────────────────────────────────────

public sealed class QuickRefundSaga : Saga<RefundSagaState>
{
    private const string AwaitingRefund = nameof(AwaitingRefund);
    private const string Completed = nameof(Completed);

    protected override void Configure(ISagaDescriptor<RefundSagaState> descriptor)
    {
        // Initial state: receive the refund request, create state, send command to billing
        descriptor
            .Initially()
            .OnRequest<RequestQuickRefundRequest>()
            .StateFactory(RefundSagaState.FromQuickRefund)
            .Send((_, state) => state.ToProcessRefund())
            .TransitionTo(AwaitingRefund);

        // Awaiting refund: handle the billing service's reply
        descriptor
            .During(AwaitingRefund)
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
                    state.FailureReason = response.FailureReason ?? "Refund processing failed";
                }
            })
            .TransitionTo(Completed);

        // Final state: build and send the response back to the original requester
        descriptor
            .Finally(Completed)
            .Respond(state => new QuickRefundResponse
            {
                OrderId = state.OrderId,
                Success = state.RefundId.HasValue,
                RefundId = state.RefundId,
                RefundedAmount = state.RefundedAmount,
                FailureReason = state.FailureReason,
                CompletedAt = DateTimeOffset.UtcNow
            });
    }
}

// ── Billing service handler (simulates a remote service) ────────────────────

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
