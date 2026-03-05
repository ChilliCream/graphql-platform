var builder = DistributedApplication.CreateBuilder(args);

// Infrastructure
var rabbitmq = builder.AddRabbitMQ("rabbitmq").WithManagementPlugin();

var db = builder.AddPostgres("postgres").WithPgWeb();

var catalogDb = db.AddDatabase("catalog-db");

var billingDb = db.AddDatabase("billing-db");

var shippingDb = db.AddDatabase("shipping-db");

// Services
builder
    .AddProject<Projects.Demo_Catalog>("catalog")
    .WithReference(rabbitmq)
    .WithReference(catalogDb)
    .WaitFor(rabbitmq)
    .WaitFor(catalogDb);

builder
    .AddProject<Projects.Demo_Billing>("billing")
    .WithReference(rabbitmq)
    .WithReference(billingDb)
    .WaitFor(rabbitmq)
    .WaitFor(billingDb);

builder
    .AddProject<Projects.Demo_Shipping>("shipping")
    .WithReference(rabbitmq)
    .WithReference(shippingDb)
    .WaitFor(rabbitmq)
    .WaitFor(shippingDb);

builder.Build().Run();
