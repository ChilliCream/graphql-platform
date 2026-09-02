using Testcontainers.RabbitMq;

namespace CookieCrumble.Resources;

public class RabbitMQResource : ContainerResource<RabbitMqContainer>
{
    public string ConnectionString => Container.GetConnectionString();

    public string Hostname => Container.Hostname;

    public ushort Port => Container.GetMappedPublicPort(RabbitMqBuilder.RabbitMqPort);

    public async Task<string> InvokeCommandAsync(IEnumerable<string> command)
    {
        var result = await Container.ExecAsync([.. command]);

        return result.Stdout;
    }

    protected override RabbitMqContainer Build()
        => Configure(
            new RabbitMqBuilder("rabbitmq:3.11")
                .WithUsername("guest")
                .WithPassword("guest"))
            .Build();

    protected virtual RabbitMqBuilder Configure(RabbitMqBuilder builder) => builder;
}
