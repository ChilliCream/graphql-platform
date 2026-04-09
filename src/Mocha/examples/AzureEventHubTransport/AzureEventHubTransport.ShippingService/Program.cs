using Mocha;
using Mocha.Hosting;
using Mocha.Transport.AzureEventHub;
using AzureEventHubTransport.ShippingService.Handlers;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

var eventHubConnectionString = builder.Configuration.GetConnectionString("eventhubs")
    ?? "Endpoint=sb://localhost;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=SAS_KEY_VALUE;UseDevelopmentEmulator=true";

// MessageBus with Azure Event Hub transport
builder
    .Services.AddMessageBus()
    .AddInstrumentation()
    .AddEventHandler<ShipOrderCommandHandler>()
    .AddEventHub(t => t.ConnectionString(eventHubConnectionString));

var app = builder.Build();

app.MapDefaultEndpoints();

app.MapGet("/", () => "Shipping Service (Azure Event Hub Transport)");

app.MapMessageBusDeveloperTopology();

app.Run();
