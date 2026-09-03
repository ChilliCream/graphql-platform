using Testcontainers.Nats;

namespace CookieCrumble.Resources;

public class NatsResource : ContainerResource<NatsContainer>
{
    public string NatsConnectionString => Container.GetConnectionString();

    protected override NatsContainer Build()
        => Configure(new NatsBuilder("nats:2.10-alpine")).Build();

    protected virtual NatsBuilder Configure(NatsBuilder builder) => builder;
}
