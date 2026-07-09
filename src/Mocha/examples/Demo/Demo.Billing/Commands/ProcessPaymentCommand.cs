using Demo.Billing.Data;
using Demo.Billing.Entities;
using Demo.Contracts.Events;
using Microsoft.EntityFrameworkCore;
using Mocha;
using Mocha.Mediator;

namespace Demo.Billing.Commands;

public record ProcessPaymentCommand(Guid InvoiceId, string PaymentMethod) : ICommand<ProcessPaymentResult>;

public record ProcessPaymentResult(bool Success, Payment? Payment = null, string? Error = null);

public class ProcessPaymentCommandHandler(BillingDbContext db, IMessageBus messageBus)
    : ICommandHandler<ProcessPaymentCommand, ProcessPaymentResult>
{
    public async ValueTask<ProcessPaymentResult> HandleAsync(
        ProcessPaymentCommand command, CancellationToken cancellationToken)
    {
        var invoice = await db.Invoices.FirstOrDefaultAsync(
            i => i.Id == command.InvoiceId, cancellationToken);

        if (invoice is null)
        {
            return new ProcessPaymentResult(false, Error: "Invoice not found");
        }

        if (invoice.Status == InvoiceStatus.Paid)
        {
            return new ProcessPaymentResult(false, Error: "Invoice already paid");
        }

        var payment = new Payment
        {
            Id = Guid.NewGuid(),
            InvoiceId = invoice.Id,
            Amount = invoice.Amount,
            Method = command.PaymentMethod,
            Status = PaymentStatus.Completed,
            ProcessedAt = DateTimeOffset.UtcNow
        };

        db.Payments.Add(payment);
        invoice.Status = InvoiceStatus.Paid;
        invoice.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(cancellationToken);

        await messageBus.PublishAsync(
            new PaymentCompletedEvent
            {
                PaymentId = payment.Id,
                InvoiceId = invoice.Id,
                OrderId = invoice.OrderId,
                Amount = payment.Amount,
                PaymentMethod = payment.Method,
                ProcessedAt = payment.ProcessedAt
            },
            cancellationToken);

        return new ProcessPaymentResult(true, payment);
    }
}
