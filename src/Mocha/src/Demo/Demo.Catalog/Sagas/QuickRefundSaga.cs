using Demo.Contracts.Commands;
using Demo.Contracts.Saga;
using Mocha.Sagas;

namespace Demo.Catalog.Sagas;

/// <summary>
/// Simple saga for quick refunds without physical returns.
/// Used for digital goods, low-value items, or goodwill refunds.
/// </summary>
public sealed class QuickRefundSaga : Saga<RefundSagaState>
{
    private const string AwaitingRefund = nameof(AwaitingRefund);
    private const string Completed = nameof(Completed);
    private const string Failed = nameof(Failed);

    protected override void Configure(ISagaDescriptor<RefundSagaState> descriptor)
    {
        // Initial: Receive refund request, send to billing
        descriptor
            .Initially()
            .OnRequest<RequestQuickRefundRequest>()
            .StateFactory(RefundSagaState.FromQuickRefund)
            .Send((_, state) => state.ToProcessRefund())
            .TransitionTo(AwaitingRefund);

        // AwaitingRefund: Handle refund response
        descriptor
            .During(AwaitingRefund)
            .OnReply<ProcessRefundResponse>()
            .Then((state, response) => { if (response.Success)
                {
                    state.RefundId = response.RefundId;
                    state.RefundedAmount = response.Amount;
                }
                else
                {
                    state.FailureReason = response.FailureReason ?? "Refund processing failed";
                } })
            .TransitionTo(Completed);

        // Note: We handle both success and failure in the same transition,
        // checking the response.Success flag to determine the outcome.
        // For a stricter separation, you could use different response types.

        // Completed: Return success response
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

        // Note: In this simple saga, we use a single Completed state
        // and check RefundId to determine success/failure in the response.
    }
}
