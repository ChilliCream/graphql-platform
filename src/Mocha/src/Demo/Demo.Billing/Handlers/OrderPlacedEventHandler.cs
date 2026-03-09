using Demo.Billing.Data;
using Demo.Billing.Entities;
using Demo.Contracts.Events;
using Mocha;

namespace Demo.Billing.Handlers;

public class OrderPlacedEventHandler(
    BillingDbContext db,
    IMessageBus messageBus,
    ILogger<OrderPlacedEventHandler> logger) : IEventHandler<OrderPlacedEvent>
{
    public async ValueTask HandleAsync(OrderPlacedEvent message, CancellationToken cancellationToken)
    {
        logger.LogInformation(
            "Order placed: {OrderId} for customer {CustomerId}, creating invoice",
            message.OrderId,
            message.CustomerId);

        // Create invoice for the order
        var invoice = new Invoice
        {
            Id = Guid.NewGuid(),
            OrderId = message.OrderId,
            Amount = message.TotalAmount,
            Status = InvoiceStatus.Pending,
            CustomerId = message.CustomerId,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        db.Invoices.Add(invoice);
        await db.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Invoice {InvoiceId} created for order {OrderId} with amount {Amount}",
            invoice.Id,
            message.OrderId,
            message.TotalAmount);

        // Auto-process payment (simulate immediate payment for demo)
        var payment = new Payment
        {
            Id = Guid.NewGuid(),
            InvoiceId = invoice.Id,
            Amount = invoice.Amount,
            Method = "CreditCard",
            Status = PaymentStatus.Completed,
            ProcessedAt = DateTimeOffset.UtcNow
        };

        db.Payments.Add(payment);
        invoice.Status = InvoiceStatus.Paid;
        invoice.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Payment {PaymentId} processed for invoice {InvoiceId}", payment.Id, invoice.Id);

        // Publish PaymentCompletedEvent
        await messageBus.PublishAsync(
            new PaymentCompletedEvent
            {
                PaymentId = payment.Id,
                InvoiceId = invoice.Id,
                OrderId = message.OrderId,
                Amount = payment.Amount,
                PaymentMethod = payment.Method,
                ProcessedAt = payment.ProcessedAt
            },
            cancellationToken);

        logger.LogInformation("PaymentCompletedEvent published for order {OrderId}", message.OrderId);
    }
}
