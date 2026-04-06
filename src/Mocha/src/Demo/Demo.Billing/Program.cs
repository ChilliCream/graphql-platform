using Demo.Billing.Commands;
using Demo.Billing.Data;
using Demo.Billing.Handlers;
using Demo.Billing.Queries;
using Microsoft.EntityFrameworkCore;
using Mocha;
using Mocha.EntityFrameworkCore;
using Mocha.Inbox;
using Mocha.Mediator;
using Mocha.Outbox;
using Mocha.Scheduling;
using Mocha.Transport.RabbitMQ;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

// Database
builder.AddNpgsqlDbContext<BillingDbContext>("billing-db", x => x.DisableTracing = true);

// RabbitMQ
builder.AddRabbitMQClient("rabbitmq", x => x.DisableTracing = true);

// Mocha.Mediator
builder.Services.AddMediator()
    .AddBilling()
    .AddInstrumentation()
    .UseEntityFrameworkTransactions<BillingDbContext>();

// MessageBus
builder
    .Services.AddMessageBus()
    .AddInstrumentation()
    .AddExceptionPolicy()
    .AddBilling()
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
    .AddEntityFramework<BillingDbContext>(p =>
    {
        p.UsePostgresOutbox();

        p.UseResilience();
        p.UseTransaction();
        p.UsePostgresInbox();
        p.UsePostgresScheduling();
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
app.MapGet("/api/invoices", async (ISender sender) => await sender.QueryAsync(new GetInvoicesQuery()));

app.MapGet(
    "/api/invoices/{id:guid}",
    async (Guid id, ISender sender) =>
        await sender.QueryAsync(new GetInvoiceByIdQuery(id)) is { } invoice ? Results.Ok(invoice) : Results.NotFound());

app.MapGet(
    "/api/invoices/order/{orderId:guid}",
    async (Guid orderId, ISender sender) =>
        await sender.QueryAsync(new GetInvoiceByOrderIdQuery(orderId)) is { } invoice
            ? Results.Ok(invoice)
            : Results.NotFound());

// Payments
app.MapPost(
    "/api/payments/{invoiceId:guid}",
    async (Guid invoiceId, ProcessPaymentRequest request, ISender sender) =>
    {
        var result = await sender.SendAsync(new ProcessPaymentCommand(invoiceId, request.PaymentMethod));

        if (!result.Success)
        {
            return result.Error == "Invoice not found"
                ? Results.NotFound(result.Error)
                : Results.BadRequest(result.Error);
        }

        return Results.Ok(result.Payment);
    });

app.MapGet("/api/payments", async (ISender sender) => await sender.QueryAsync(new GetPaymentsQuery()));

// Refunds
app.MapGet("/api/refunds", async (ISender sender) => await sender.QueryAsync(new GetRefundsQuery()));

app.MapGet(
    "/api/refunds/order/{orderId:guid}",
    async (Guid orderId, ISender sender) => await sender.QueryAsync(new GetRefundsByOrderIdQuery(orderId)));

// Revenue Summaries
app.MapGet("/api/revenue-summaries", async (ISender sender) => await sender.QueryAsync(new GetRevenueSummariesQuery()));

app.MapGet(
    "/api/revenue-summaries/latest",
    async (ISender sender) =>
        await sender.QueryAsync(new GetLatestRevenueSummaryQuery()) is { } summary
            ? Results.Ok(summary)
            : Results.NotFound());

app.Run();

public record ProcessPaymentRequest(string PaymentMethod);
