using Testcontainers.Kafka;

namespace CookieCrumble.Resources;

public class KafkaResource : ContainerResource<KafkaContainer>
{
    public string BootstrapServers => Container.GetBootstrapAddress();

    protected override KafkaContainer Build()
        => Configure(new KafkaBuilder("confluentinc/cp-kafka:7.7.0")).Build();

    protected virtual KafkaBuilder Configure(KafkaBuilder builder) => builder;
}
