using AotExample.OrderService;
using Mocha;
using Mocha.Mediator;
using Mocha.Transport.RabbitMQ;
using RabbitMQ.Client;

[assembly: MessagingModule("OrderService")]
[assembly: MediatorModule("OrderService")]

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<IConnectionFactory>(
    new ConnectionFactory { HostName = "localhost", Port = 5673 });

builder.Services.AddHostedService<OrderSimulatorWorker>();

builder.Services.AddMessageBus().AddAotExampleContracts().AddOrderService().AddRabbitMQ();
builder.Services.AddMediator().AddOrderService();

var app = builder.Build();

app.MapGet("/", () => "Order Service (AOT Example)");

app.Run();
