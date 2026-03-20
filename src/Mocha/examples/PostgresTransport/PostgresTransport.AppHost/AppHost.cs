var builder = DistributedApplication.CreateBuilder(args);

// Infrastructure - single Postgres instance with a shared messaging database
var postgres = builder.AddPostgres("postgres").WithPgAdmin();
var messagingDb = postgres.AddDatabase("messaging-db");

// Services
builder
    .AddProject<Projects.PostgresTransport_OrderService>("order-service")
    .WithReference(messagingDb)
    .WaitFor(messagingDb);

builder
    .AddProject<Projects.PostgresTransport_ShippingService>("shipping-service")
    .WithReference(messagingDb)
    .WaitFor(messagingDb);

builder
    .AddProject<Projects.PostgresTransport_NotificationService>("notification-service")
    .WithReference(messagingDb)
    .WaitFor(messagingDb);

builder.Build().Run();
