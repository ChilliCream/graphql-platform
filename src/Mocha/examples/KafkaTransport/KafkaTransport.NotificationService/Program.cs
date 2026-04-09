using Mocha;
using Mocha.Hosting;
using Mocha.Transport.Kafka;
using KafkaTransport.NotificationService.Handlers;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

// Get the Kafka bootstrap servers from Aspire-injected configuration
var bootstrapServers = (
    builder.Configuration.GetConnectionString("kafka")
    ?? "localhost:9092")
    .Replace("localhost", "127.0.0.1");

// MessageBus with Kafka transport
builder
    .Services.AddMessageBus()
    .AddInstrumentation()
    .AddEventHandler<OrderPlacedNotificationHandler>()
    .AddEventHandler<OrderShippedNotificationHandler>()
    .AddEventHandler<OrderFulfilledNotificationHandler>()
    .AddKafka(t =>
    {
        t.BootstrapServers(bootstrapServers);
        t.AutoProvision(true);
    });

var app = builder.Build();

app.MapDefaultEndpoints();

app.MapGet("/", () => "Notification Service (Kafka Transport)");

app.MapMessageBusDeveloperTopology();

app.Run();
