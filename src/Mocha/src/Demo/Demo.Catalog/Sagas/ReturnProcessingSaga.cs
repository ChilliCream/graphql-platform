using Demo.Contracts.Commands;
using Demo.Contracts.Events;
using Mocha.Sagas;

namespace Demo.Catalog.Sagas;

/// <summary>
/// Saga for processing returned packages.
/// Starts when ReturnPackageReceivedEvent arrives (package at warehouse).
/// Handles: Inspection → Parallel (Refund + Restock) → Complete
/// </summary>
public sealed class ReturnProcessingSaga : Saga<RefundSagaState>
{
    // States
    private const string AwaitingInspection = nameof(AwaitingInspection);
    private const string AwaitingBothReplies = nameof(AwaitingBothReplies);
    private const string RestockDoneAwaitingRefund = nameof(RestockDoneAwaitingRefund);
    private const string RefundDoneAwaitingRestock = nameof(RefundDoneAwaitingRestock);
    private const string Completed = nameof(Completed);

    protected override void Configure(ISagaDescriptor<RefundSagaState> descriptor)
    {
        // Start: Package received → Inspect
        descriptor
            .Initially()
            .OnEvent<ReturnPackageReceivedEvent>()
            .StateFactory(RefundSagaState.FromReturnPackageReceived)
            .Send((_, state) => state.ToInspectReturn())
            .TransitionTo(AwaitingInspection);

        // After inspection → Send both refund and restock in parallel
        descriptor
            .During(AwaitingInspection)
            .OnReply<InspectReturnResponse>()
            .Then((state, response) => state.InspectionResult = response.Result)
            .Send((_, state) => state.ToRestockInventory())
            .Send((_, state) => state.ToProcessRefund())
            .TransitionTo(AwaitingBothReplies);

        // Parallel handling: Restock arrives first
        descriptor
            .During(AwaitingBothReplies)
            .OnReply<RestockInventoryResponse>()
            .Then(
                (state, response) =>
                {
                    state.InventoryRestocked = response.Success;
                    state.QuantityRestocked = response.QuantityRestocked;
                })
            .TransitionTo(RestockDoneAwaitingRefund);

        // Parallel handling: Refund arrives first
        descriptor
            .During(AwaitingBothReplies)
            .OnReply<ProcessRefundResponse>()
            .Then((state, response) => { if (response.Success)
                {
                    state.RefundId = response.RefundId;
                    state.RefundedAmount = response.Amount;
                    state.RefundPercentage = 100;
                }
                else
                {
                    state.FailureReason = response.FailureReason;
                } })
            .TransitionTo(RefundDoneAwaitingRestock);

        // Waiting for refund after restock done
        descriptor
            .During(RestockDoneAwaitingRefund)
            .OnReply<ProcessRefundResponse>()
            .Then((state, response) => { if (response.Success)
                {
                    state.RefundId = response.RefundId;
                    state.RefundedAmount = response.Amount;
                    state.RefundPercentage = 100;
                }
                else
                {
                    state.FailureReason = response.FailureReason;
                } })
            .TransitionTo(Completed);

        // Waiting for restock after refund done
        descriptor
            .During(RefundDoneAwaitingRestock)
            .OnReply<RestockInventoryResponse>()
            .Then(
                (state, response) =>
                {
                    state.InventoryRestocked = response.Success;
                    state.QuantityRestocked = response.QuantityRestocked;
                })
            .TransitionTo(Completed);

        // Final state - saga completes (no response needed, this was event-driven)
        // Could publish a ReturnCompletedEvent here if needed
        descriptor.Finally(Completed);
    }
}
