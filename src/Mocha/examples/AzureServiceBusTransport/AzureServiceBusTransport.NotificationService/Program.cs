using System.Data.Common;
using Azure.Identity;
using ChilliCream.Nitro;
using Mocha;
using Mocha.Transport.AzureServiceBus;
using AzureServiceBusTransport.NotificationService.Handlers;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.Services.AddNitro().AddMocha();

var connectionString = builder.Configuration["MESSAGING_CONNECTIONSTRING"];
var administrationEndpoint = builder.Configuration["MESSAGING_ADMINISTRATIONENDPOINT"];
var fullyQualifiedNamespace = builder.Configuration["MESSAGING_FULLYQUALIFIEDNAMESPACE"];

builder
    .Services.AddMessageBus()
    .AddInstrumentation()
    .Host(h => h.InstanceId(Guid.Parse("00000000-0000-0000-0000-000000000003")))
    .AddEventHandler<OrderPlacedNotificationHandler>()
    .AddEventHandler<OrderShippedNotificationHandler>()
    .AddAzureServiceBus(t =>
    {
        if (connectionString is not null)
        {
            t.ConnectionString(connectionString);

            if (administrationEndpoint is not null)
            {
                var administrationConnectionString = new DbConnectionStringBuilder { ConnectionString = connectionString };
                administrationConnectionString["Endpoint"] = administrationEndpoint;
                t.AdministrationConnectionString(administrationConnectionString.ConnectionString);
                t.AutoProvision();
            }
            else
            {
                t.AutoProvision(false);
            }
        }
        else
        {
            t.Namespace(
                fullyQualifiedNamespace
                    ?? throw new InvalidOperationException("Service Bus namespace is not configured."),
                new DefaultAzureCredential());
            t.AutoProvision(false);
        }
    });

var app = builder.Build();

app.MapDefaultEndpoints();

app.MapGet("/", () => "Notification Service (Azure Service Bus Transport)");

app.Run();
