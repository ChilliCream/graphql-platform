using Mocha;
using Mocha.Transport.RabbitMQ;
using RabbitMQ.Client;

[assembly: MessagingModule("FulfillmentService")]

var builder = WebApplication.CreateBuilder(args);

builder.WebHost.UseUrls("http://localhost:52310");

builder.Services.AddSingleton<IConnectionFactory>(
    new ConnectionFactory { HostName = "localhost", Port = 5673 });

builder.Services.AddMessageBus().AddAotExampleContracts().AddFulfillmentService().AddRabbitMQ();

var app = builder.Build();

app.MapGet("/", () => "Fulfillment Service (AOT Example)");

app.Run();
