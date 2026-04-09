var builder = DistributedApplication.CreateBuilder(args);

// Infrastructure - Azure Event Hubs emulator running in Docker.
// Each hub must be pre-declared so the emulator creates them on startup.
var eventHubs = builder
    .AddAzureEventHubs("eventhubs")
    .RunAsEmulator();

// Shared event hubs — one per event type, named by the publish naming convention
// ({namespace}.{event-name}). Multiple services consume from the same hub using
// different consumer groups for independent read positions.
var orderPlacedHub = eventHubs.AddHub("order-placed-hub",
    hubName: "azure-event-hub-transport.contracts.events.order-placed");
orderPlacedHub.AddConsumerGroup("order-placed-orders", groupName: "order-service");
orderPlacedHub.AddConsumerGroup("order-placed-notifications", groupName: "notification-service");

var paymentProcessedHub = eventHubs.AddHub("payment-processed-hub",
    hubName: "azure-event-hub-transport.contracts.events.payment-processed");
paymentProcessedHub.AddConsumerGroup("payment-processed-orders", groupName: "order-service");
paymentProcessedHub.AddConsumerGroup("payment-processed-notifications", groupName: "notification-service");

var orderShippedHub = eventHubs.AddHub("order-shipped-hub",
    hubName: "azure-event-hub-transport.contracts.events.order-shipped");
orderShippedHub.AddConsumerGroup("order-shipped-orders", groupName: "order-service");
orderShippedHub.AddConsumerGroup("order-shipped-notifications", groupName: "notification-service");

var orderFulfilledHub = eventHubs.AddHub("order-fulfilled-hub",
    hubName: "azure-event-hub-transport.contracts.events.order-fulfilled");
orderFulfilledHub.AddConsumerGroup("order-fulfilled-orders", groupName: "order-service");
orderFulfilledHub.AddConsumerGroup("order-fulfilled-notifications", groupName: "notification-service");

// Command hubs — one per command type, named by the publish naming convention
// ({namespace}.{command-name}). Handlers registered via AddEventHandler use
// Subscribe routing, so both dispatch and receive use the publish endpoint name.
var processPaymentHub = eventHubs.AddHub("process-payment-hub",
    hubName: "azure-event-hub-transport.contracts.commands.process-payment");
processPaymentHub.AddConsumerGroup("process-payment-orders", groupName: "order-service");

var shipOrderHub = eventHubs.AddHub("ship-order-hub",
    hubName: "azure-event-hub-transport.contracts.commands.ship-order");
shipOrderHub.AddConsumerGroup("ship-order-shipping", groupName: "shipping-service");

// Reply hub (used by the transport for request/reply patterns)
eventHubs.AddHub("replies");

// Error/skipped hubs (used by the transport for failed/unhandled messages)
eventHubs.AddHub("error");
eventHubs.AddHub("skipped");

// Services
builder
    .AddProject<Projects.AzureEventHubTransport_OrderService>("order-service")
    .WithReference(eventHubs)
    .WaitFor(eventHubs);

builder
    .AddProject<Projects.AzureEventHubTransport_ShippingService>("shipping-service")
    .WithReference(eventHubs)
    .WaitFor(eventHubs);

builder
    .AddProject<Projects.AzureEventHubTransport_NotificationService>("notification-service")
    .WithReference(eventHubs)
    .WaitFor(eventHubs);

builder.Build().Run();
