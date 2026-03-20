using Mocha;
using Mocha.Hosting;
using Mocha.Transport.Postgres;
using PostgresTransport.NotificationService.Handlers;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

// Get the Postgres connection string from Aspire-injected configuration
var messagingConnectionString =
    builder.Configuration.GetConnectionString("messaging-db")
    ?? "Host=localhost;Database=mocha_messaging;Username=postgres;Password=postgres";

// MessageBus with PostgreSQL transport
builder
    .Services.AddMessageBus()
    .AddInstrumentation()
    .AddEventHandler<OrderPlacedNotificationHandler>()
    .AddEventHandler<OrderShippedNotificationHandler>()
    .AddPostgres(t => t.ConnectionString(messagingConnectionString));

var app = builder.Build();

app.MapDefaultEndpoints();

app.MapGet("/", () => "Notification Service (Postgres Transport)");

app.MapMessageBusDeveloperTopology();

app.Run();
