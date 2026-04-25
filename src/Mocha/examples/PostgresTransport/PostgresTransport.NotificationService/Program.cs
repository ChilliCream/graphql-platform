using Mocha;
using Mocha.Resources.AspNetCore;
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

// Resource source diagnostics — exposes the message bus topology as Mocha resources.
builder.Services.AddMochaMessageBusResources();

var app = builder.Build();

app.MapDefaultEndpoints();

app.MapGet("/", () => "Notification Service (Postgres Transport)");

app.MapMochaResourceEndpoint();

app.Run();
