using Aspire.Hosting.ApplicationModel;

var builder = DistributedApplication.CreateBuilder(args);

// Infrastructure - Azure Service Bus emulator running in Docker.
var serviceBus = builder
    .AddAzureServiceBus("messaging")
    .RunAsEmulator();

var administrationEndpoint = serviceBus.GetEndpoint("emulatorhealth");

// Services
builder
    .AddProject<Projects.AzureServiceBusTransport_OrderService>("order-service")
    .WithReference(serviceBus)
    .WithEnvironment(
        "MESSAGING_ADMINISTRATIONENDPOINT",
        $"sb://{administrationEndpoint.Property(EndpointProperty.HostAndPort)}")
    .WaitFor(serviceBus);

builder
    .AddProject<Projects.AzureServiceBusTransport_ShippingService>("shipping-service")
    .WithReference(serviceBus)
    .WithEnvironment(
        "MESSAGING_ADMINISTRATIONENDPOINT",
        $"sb://{administrationEndpoint.Property(EndpointProperty.HostAndPort)}")
    .WaitFor(serviceBus);

builder
    .AddProject<Projects.AzureServiceBusTransport_NotificationService>("notification-service")
    .WithReference(serviceBus)
    .WithEnvironment(
        "MESSAGING_ADMINISTRATIONENDPOINT",
        $"sb://{administrationEndpoint.Property(EndpointProperty.HostAndPort)}")
    .WaitFor(serviceBus);

builder.Build().Run();
