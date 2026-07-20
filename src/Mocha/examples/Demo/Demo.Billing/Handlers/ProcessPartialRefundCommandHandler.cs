using Demo.Billing.Data;
using Demo.Billing.Entities;
using Demo.Contracts.Commands;
using Microsoft.EntityFrameworkCore;
using Mocha;

namespace Demo.Billing.Handlers;

public class ProcessPartialRefundCommandHandler(BillingDbContext db, ILogger<ProcessPartialRefundCommandHandler> logger)
    : IEventRequestHandler<ProcessPartialRefundCommand, ProcessPartialRefundResponse>
{
    public async ValueTask<ProcessPartialRefundResponse> HandleAsync(
        ProcessPartialRefundCommand request,
        CancellationToken cancellationToken)
    {
        var refundedAmount = request.OriginalAmount * (request.RefundPercentage / 100);

        logger.LogInformation(
            "Processing partial refund of {RefundedAmount} ({Percentage}%) for order {OrderId}",
            refundedAmount,
            request.RefundPercentage,
            request.OrderId);

        // Find the invoice for this order
        var invoice = await db
            .Invoices.Include(i => i.Payments)
            .FirstOrDefaultAsync(i => i.OrderId == request.OrderId, cancellationToken);

        // Create refund record
        var refund = new Refund
        {
            Id = Guid.NewGuid(),
            OrderId = request.OrderId,
            InvoiceId = invoice?.Id,
            OriginalAmount = request.OriginalAmount,
            RefundedAmount = refundedAmount,
            RefundPercentage = request.RefundPercentage,
            CustomerId = request.CustomerId,
            Reason = request.Reason,
            Status = RefundStatus.Pending,
            Type = RefundType.Partial,
            CreatedAt = DateTimeOffset.UtcNow
        };

        db.Refunds.Add(refund);

        // Simulate refund processing
        await Task.Delay(100, cancellationToken);

        // Mark refund as completed
        refund.Status = RefundStatus.Completed;
        refund.ProcessedAt = DateTimeOffset.UtcNow;

        // Update invoice status if exists - partial refund doesn't fully refund the invoice
        // Keep invoice as Paid but we've recorded the partial refund
        invoice?.UpdatedAt = DateTimeOffset.UtcNow;

        await db.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Partial refund {RefundId} completed for order {OrderId}, {RefundedAmount} of {OriginalAmount} ({Percentage}%)",
            refund.Id,
            request.OrderId,
            refundedAmount,
            request.OriginalAmount,
            request.RefundPercentage);

        return new ProcessPartialRefundResponse
        {
            RefundId = refund.Id,
            OrderId = request.OrderId,
            OriginalAmount = request.OriginalAmount,
            RefundedAmount = refundedAmount,
            RefundPercentage = request.RefundPercentage,
            Success = true,
            FailureReason = null,
            ProcessedAt = refund.ProcessedAt!.Value
        };
    }
}
