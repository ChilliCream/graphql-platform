using System.Text.Json.Nodes;

var builder = DistributedApplication.CreateBuilder(args);

// Infrastructure - Azure Service Bus emulator running in Docker.
// The emulator does not support creating entities via the management REST API;
// all queues, topics, and subscriptions must be pre-defined in its Config.json.
var serviceBus = builder
    .AddAzureServiceBus("messaging")
    .RunAsEmulator(configure => configure.WithConfiguration(document =>
    {
        var ns = document["UserConfig"]!["Namespaces"]![0]!;

        // Queues — point-to-point and request-reply targets
        ns["Queues"] = new JsonArray(
            Queue("process-order"),
            Queue("prepare-shipment"),
            Queue("get-order-status-request"),
            Queue("fulfill-order-request"),
            Queue("order-service.order-shipped-event"),
            Queue("order-service.order-shipped-event_error"),
            Queue("order-service.order-shipped-event_skipped"),
            Queue("order-service.order-analytics-batch"),
            Queue("order-service.order-analytics-batch_error"),
            Queue("order-service.order-analytics-batch_skipped"),
            Queue("order-service.order-fulfillment-saga"),
            Queue("order-service.order-fulfillment-saga_error"),
            Queue("order-service.order-fulfillment-saga_skipped"),
            Queue("shipping-service.order-placed-event"),
            Queue("shipping-service.order-placed-event_error"),
            Queue("shipping-service.order-placed-event_skipped"),
            Queue("notification-service.order-placed-notification"),
            Queue("notification-service.order-placed-notification_error"),
            Queue("notification-service.order-placed-notification_skipped"),
            Queue("notification-service.order-shipped-notification"),
            Queue("notification-service.order-shipped-notification_error"),
            Queue("notification-service.order-shipped-notification_skipped"),
            Queue("response-00000000000000000000000000000001"),
            Queue("response-00000000000000000000000000000002"),
            Queue("response-00000000000000000000000000000003"));

        // Topics with forwarding subscriptions
        ns["Topics"] = new JsonArray(
            Topic("azure-service-bus-transport.contracts.events.order-placed",
                "order-service.order-analytics-batch",
                "shipping-service.order-placed-event",
                "notification-service.order-placed-notification"),
            Topic("azure-service-bus-transport.contracts.events.order-shipped",
                "order-service.order-shipped-event",
                "notification-service.order-shipped-notification"),
            Topic("azure-service-bus-transport.contracts.commands.process-order",
                "process-order"));

        static JsonObject Queue(string name) => new() { ["Name"] = name };

        static JsonObject Topic(string name, params string[] subscriptions)
        {
            var subs = new JsonArray();
            foreach (var sub in subscriptions)
            {
                subs.Add(new JsonObject
                {
                    ["Name"] = "fwd-" + sub,
                    ["Properties"] = new JsonObject { ["ForwardTo"] = sub }
                });
            }

            return new JsonObject { ["Name"] = name, ["Subscriptions"] = subs };
        }
    }));

// Services
builder
    .AddProject<Projects.AzureServiceBusTransport_OrderService>("order-service")
    .WithReference(serviceBus)
    .WaitFor(serviceBus);

builder
    .AddProject<Projects.AzureServiceBusTransport_ShippingService>("shipping-service")
    .WithReference(serviceBus)
    .WaitFor(serviceBus);

builder
    .AddProject<Projects.AzureServiceBusTransport_NotificationService>("notification-service")
    .WithReference(serviceBus)
    .WaitFor(serviceBus);

builder.Build().Run();
