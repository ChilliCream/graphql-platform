using Demo.Billing.Data;
using Demo.Billing.Entities;
using Demo.Billing.Handlers;
using Demo.Contracts.Events;
using Microsoft.EntityFrameworkCore;
using Mocha;
using Mocha.EntityFrameworkCore;
using Mocha.Inbox;
using Mocha.Outbox;
using Mocha.Transport.RabbitMQ;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

// Database
builder.AddNpgsqlDbContext<BillingDbContext>("billing-db");

// RabbitMQ
builder.AddRabbitMQClient("rabbitmq", x => x.DisableTracing = true);

// MessageBus
builder
    .Services.AddMessageBus()
    .AddInstrumentation()
    // Event handlers
    .AddEventHandler<OrderPlacedEventHandler>()
    .AddEventHandler<ShipmentShippedEventHandler>()
    // Batch event handlers
    .AddBatchHandler<OrderPlacedBatchHandler>(opts =>
    {
        opts.MaxBatchSize = 5;
        opts.BatchTimeout = TimeSpan.FromSeconds(10);
    })
    .AddBatchHandler<BulkOrderBatchHandler>(opts =>
    {
        opts.MaxBatchSize = 500;
        opts.BatchTimeout = TimeSpan.FromSeconds(5);
    })
    // Request handlers for saga commands
    .AddRequestHandler<ProcessRefundCommandHandler>()
    .AddRequestHandler<ProcessPartialRefundCommandHandler>()
    .AddEntityFramework<BillingDbContext>(p =>
    {
        p.UsePostgresOutbox();

        p.UseResilience();
        p.UseTransaction();
        p.UsePostgresInbox();
    })
    .AddRabbitMQ();

var app = builder.Build();

// Ensure database is created
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<BillingDbContext>();
    await db.Database.EnsureCreatedAsync();
}

// REST API Endpoints
app.MapGet("/", () => "Billing Service");

// Invoices
app.MapGet("/api/invoices", async (BillingDbContext db) => await db.Invoices.Include(i => i.Payments).ToListAsync());

app.MapGet(
    "/api/invoices/{id:guid}",
    async (Guid id, BillingDbContext db) =>
        await db.Invoices.Include(i => i.Payments).FirstOrDefaultAsync(i => i.Id == id) is { } invoice
            ? Results.Ok(invoice)
            : Results.NotFound());

app.MapGet(
    "/api/invoices/order/{orderId:guid}",
    async (Guid orderId, BillingDbContext db) =>
        await db.Invoices.Include(i => i.Payments).FirstOrDefaultAsync(i => i.OrderId == orderId) is { } invoice
            ? Results.Ok(invoice)
            : Results.NotFound());

// Payments - manually trigger payment processing
app.MapPost(
    "/api/payments/{invoiceId:guid}",
    async (Guid invoiceId, ProcessPaymentRequest request, BillingDbContext db, IMessageBus messageBus) =>
    {
        var invoice = await db.Invoices.FirstOrDefaultAsync(i => i.Id == invoiceId);
        if (invoice is null)
        {
            return Results.NotFound("Invoice not found");
        }

        if (invoice.Status == InvoiceStatus.Paid)
        {
            return Results.BadRequest("Invoice already paid");
        }

        var payment = new Payment
        {
            Id = Guid.NewGuid(),
            InvoiceId = invoice.Id,
            Amount = invoice.Amount,
            Method = request.PaymentMethod,
            Status = PaymentStatus.Completed,
            ProcessedAt = DateTimeOffset.UtcNow
        };

        db.Payments.Add(payment);
        invoice.Status = InvoiceStatus.Paid;
        invoice.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync();

        // Publish PaymentCompletedEvent
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
            CancellationToken.None);

        return Results.Ok(payment);
    });

app.MapGet("/api/payments", async (BillingDbContext db) => await db.Payments.Include(p => p.Invoice).ToListAsync());

// Refunds
app.MapGet("/api/refunds", async (BillingDbContext db) => await db.Refunds.ToListAsync());

app.MapGet(
    "/api/refunds/order/{orderId:guid}",
    async (Guid orderId, BillingDbContext db) => await db.Refunds.Where(r => r.OrderId == orderId).ToListAsync());

// Revenue Summaries (batch analytics)
app.MapGet(
    "/api/revenue-summaries",
    async (BillingDbContext db) => await db.RevenueSummaries.OrderByDescending(r => r.CreatedAt).ToListAsync());

app.MapGet(
    "/api/revenue-summaries/latest",
    async (BillingDbContext db) =>
        await db.RevenueSummaries.OrderByDescending(r => r.CreatedAt).FirstOrDefaultAsync() is { } summary
            ? Results.Ok(summary)
            : Results.NotFound());

app.Run();

public record ProcessPaymentRequest(string PaymentMethod);
