using Demo.Billing.Data;
using Demo.Billing.Entities;
using Demo.Contracts.Commands;
using Microsoft.EntityFrameworkCore;
using Mocha;

namespace Demo.Billing.Handlers;

public class ProcessRefundCommandHandler(BillingDbContext db, ILogger<ProcessRefundCommandHandler> logger)
    : IEventRequestHandler<ProcessRefundCommand, ProcessRefundResponse>
{
    public async ValueTask<ProcessRefundResponse> HandleAsync(
        ProcessRefundCommand request,
        CancellationToken cancellationToken)
    {
        logger.LogInformation(
            "Processing full refund of {Amount} for order {OrderId}",
            request.Amount,
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
            OriginalAmount = request.Amount,
            RefundedAmount = request.Amount,
            RefundPercentage = 100,
            CustomerId = request.CustomerId,
            Reason = request.Reason,
            Status = RefundStatus.Pending,
            Type = RefundType.Full,
            CreatedAt = DateTimeOffset.UtcNow
        };

        db.Refunds.Add(refund);

        // Simulate refund processing - in real world, this would call payment gateway
        await Task.Delay(100, cancellationToken); // Simulate processing time

        // Mark refund as completed
        refund.Status = RefundStatus.Completed;
        refund.ProcessedAt = DateTimeOffset.UtcNow;

        // Update invoice status if exists
        if (invoice is not null)
        {
            invoice.Status = InvoiceStatus.Refunded;
            invoice.UpdatedAt = DateTimeOffset.UtcNow;

            // Mark payments as refunded
            foreach (var payment in invoice.Payments)
            {
                payment.Status = PaymentStatus.Refunded;
            }
        }

        await db.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Refund {RefundId} completed for order {OrderId}, amount {Amount}",
            refund.Id,
            request.OrderId,
            request.Amount);

        return new ProcessRefundResponse
        {
            RefundId = refund.Id,
            OrderId = request.OrderId,
            Amount = refund.RefundedAmount,
            Success = true,
            FailureReason = null,
            ProcessedAt = refund.ProcessedAt!.Value
        };
    }
}
