using ChilliCream.Nitro;
using Mocha;
using Mocha.Transport.AzureServiceBus;
using AzureServiceBusTransport.NotificationService.Handlers;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.AddAzureServiceBusClient("messaging");
builder.Services.AddNitro().AddMocha();

var administrationConnectionString = builder.Configuration.GetConnectionString("messaging-administration");

builder
    .Services.AddMessageBus()
    .AddInstrumentation()
    .Host(h => h.InstanceId(Guid.Parse("00000000-0000-0000-0000-000000000003")))
    .AddEventHandler<OrderPlacedNotificationHandler>()
    .AddEventHandler<OrderShippedNotificationHandler>()
    .AddAzureServiceBus(t =>
    {
        // Aspire does not register the emulator's separate administration client.
        if (administrationConnectionString is not null)
        {
            t.AdministrationConnectionString(administrationConnectionString);
        }
    });

var app = builder.Build();

app.MapDefaultEndpoints();

app.MapGet("/", () => "Notification Service (Azure Service Bus Transport)");

app.Run();
