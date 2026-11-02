using Aspire.Hosting.ApplicationModel;

var builder = DistributedApplication.CreateBuilder(args);

// Infrastructure - Azure Service Bus emulator running in Docker.
var serviceBus = builder
    .AddAzureServiceBus("messaging")
    .RunAsEmulator();

var administrationEndpoint = serviceBus.GetEndpoint("emulatorhealth");
var administrationConnectionString = ReferenceExpression.Create(
    $"Endpoint=sb://{administrationEndpoint.Property(EndpointProperty.HostAndPort)};SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=SAS_KEY_VALUE;UseDevelopmentEmulator=true;");

// Services
builder
    .AddProject<Projects.AzureServiceBusTransport_OrderService>("order-service")
    .WithReference(serviceBus)
    .WithEnvironment(
        "ConnectionStrings__messaging-administration",
        administrationConnectionString)
    .WaitFor(serviceBus);

builder
    .AddProject<Projects.AzureServiceBusTransport_ShippingService>("shipping-service")
    .WithReference(serviceBus)
    .WithEnvironment(
        "ConnectionStrings__messaging-administration",
        administrationConnectionString)
    .WaitFor(serviceBus);

builder
    .AddProject<Projects.AzureServiceBusTransport_NotificationService>("notification-service")
    .WithReference(serviceBus)
    .WithEnvironment(
        "ConnectionStrings__messaging-administration",
        administrationConnectionString)
    .WaitFor(serviceBus);

builder.Build().Run();
