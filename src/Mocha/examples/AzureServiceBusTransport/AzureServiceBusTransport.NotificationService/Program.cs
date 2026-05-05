using Mocha;
using Mocha.Hosting;
using Mocha.Transport.AzureServiceBus;
using AzureServiceBusTransport.NotificationService.Handlers;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

var connectionString =
    builder.Configuration.GetConnectionString("messaging")
    ?? throw new InvalidOperationException("Connection string 'messaging' not found.");

builder
    .Services.AddMessageBus()
    .AddInstrumentation()
    .Host(h => h.InstanceId(Guid.Parse("00000000-0000-0000-0000-000000000003")))
    .AddEventHandler<OrderPlacedNotificationHandler>()
    .AddEventHandler<OrderShippedNotificationHandler>()
    .AddAzureServiceBus(t =>
    {
        t.ConnectionString(connectionString);
        t.AutoProvision(false);
    });

var app = builder.Build();

app.MapDefaultEndpoints();

app.MapGet("/", () => "Notification Service (Azure Service Bus Transport)");

app.MapMessageBusDeveloperTopology();

app.Run();
