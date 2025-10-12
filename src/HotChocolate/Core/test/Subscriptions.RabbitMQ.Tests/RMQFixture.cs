using Testcontainers.RabbitMq;

namespace HotChocolate.Subscriptions.RabbitMQ;

public class RMQFixture : IAsyncLifetime
{
    public RabbitMqContainer? Container { get; private set; }

    public async Task InitializeAsync()
    {
        Container = new RabbitMqBuilder()
            .WithImage("rabbitmq:4")
            .Build();
        await Container.StartAsync();
    }

    public async Task DisposeAsync()
    {
        if (Container != null)
        {
            await Container.StopAsync();
            await Container.DisposeAsync();
        }
    }
}
