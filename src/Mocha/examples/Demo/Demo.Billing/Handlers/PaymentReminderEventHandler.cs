using Demo.Billing.Data;
using Demo.Billing.Entities;
using Demo.Contracts.Events;
using Microsoft.EntityFrameworkCore;
using Mocha;

namespace Demo.Billing.Handlers;

public class PaymentReminderEventHandler(
    BillingDbContext db,
    ILogger<PaymentReminderEventHandler> logger) : IEventHandler<PaymentReminderEvent>
{
    public async ValueTask HandleAsync(PaymentReminderEvent message, CancellationToken cancellationToken)
    {
        var invoice = await db.Invoices.FirstOrDefaultAsync(i => i.Id == message.InvoiceId, cancellationToken);

        if (invoice is null)
        {
            logger.LogWarning("Payment reminder for invoice {InvoiceId} - invoice not found", message.InvoiceId);
            return;
        }

        if (invoice.Status == InvoiceStatus.Pending)
        {
            logger.LogWarning(
                "Payment reminder: Invoice {InvoiceId} for order {OrderId} is still pending. Amount: {Amount}",
                message.InvoiceId,
                message.OrderId,
                message.Amount);
        }
        else
        {
            logger.LogInformation(
                "Payment reminder: Invoice {InvoiceId} for order {OrderId} is already {Status}",
                message.InvoiceId,
                message.OrderId,
                invoice.Status);
        }
    }
}
