using System.Data.Common;
using Azure.Identity;
using ChilliCream.Nitro;
using Mocha;
using Mocha.Transport.AzureServiceBus;
using AzureServiceBusTransport.Contracts.Commands;
using AzureServiceBusTransport.Contracts.Events;
using AzureServiceBusTransport.ShippingService.Handlers;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.Services.AddNitro().AddMocha();

var connectionString = builder.Configuration["MESSAGING_CONNECTIONSTRING"];
var administrationEndpoint = builder.Configuration["MESSAGING_ADMINISTRATIONENDPOINT"];
var fullyQualifiedNamespace = builder.Configuration["MESSAGING_FULLYQUALIFIEDNAMESPACE"];

builder
    .Services.AddMessageBus()
    .AddInstrumentation()
    .Host(h => h.InstanceId(Guid.Parse("00000000-0000-0000-0000-000000000002")))
    .AddEventHandler<OrderPlacedEventHandler>()
    .AddRequestHandler<PrepareShipmentRequestHandler>()
    .AddAzureServiceBus(t =>
    {
        if (connectionString is not null)
        {
            t.ConnectionString(connectionString);

            if (administrationEndpoint is not null)
            {
                var administrationConnectionString = new DbConnectionStringBuilder { ConnectionString = connectionString };
                administrationConnectionString["Endpoint"] = administrationEndpoint;
                t.AdministrationConnectionString(administrationConnectionString.ConnectionString);
                t.AutoProvision();
            }
            else
            {
                t.AutoProvision(false);
            }
        }
        else
        {
            t.Namespace(
                fullyQualifiedNamespace
                    ?? throw new InvalidOperationException("Service Bus namespace is not configured."),
                new DefaultAzureCredential());
            t.AutoProvision(false);
        }

        // Receive PrepareShipmentCommand from the order fulfillment saga
        t.DeclareQueue("prepare-shipment");
        t.Endpoint("prepare-shipment-ep")
            .Queue("prepare-shipment")
            .Handler<PrepareShipmentRequestHandler>();
    });

var app = builder.Build();

app.MapDefaultEndpoints();

app.MapGet("/", () => "Shipping Service (Azure Service Bus Transport)");

// Ship an order - publishes OrderShippedEvent
app.MapPost(
    "/api/shipments",
    async (CreateShipmentRequest request, IMessageBus messageBus) =>
    {
        var trackingNumber = $"TRK-{Guid.NewGuid().ToString()[..8].ToUpperInvariant()}";

        await messageBus.PublishAsync(
            new OrderShippedEvent
            {
                OrderId = request.OrderId,
                TrackingNumber = trackingNumber,
                Carrier = request.Carrier,
                ShippedAt = DateTimeOffset.UtcNow
            },
            CancellationToken.None);

        return Results.Ok(new
        {
            request.OrderId,
            trackingNumber,
            request.Carrier,
            status = "Shipped"
        });
    });

app.Run();

public record CreateShipmentRequest(Guid OrderId, string Carrier);
