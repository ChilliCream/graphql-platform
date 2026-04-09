var builder = DistributedApplication.CreateBuilder(args);

// Infrastructure - single Kafka instance
var kafka = builder.AddKafka("kafka").WithKafkaUI();

// Services
builder
    .AddProject<Projects.KafkaTransport_OrderService>("order-service")
    .WithReference(kafka)
    .WaitFor(kafka);

builder
    .AddProject<Projects.KafkaTransport_ShippingService>("shipping-service")
    .WithReference(kafka)
    .WaitFor(kafka);

builder
    .AddProject<Projects.KafkaTransport_NotificationService>("notification-service")
    .WithReference(kafka)
    .WaitFor(kafka);

builder.Build().Run();
