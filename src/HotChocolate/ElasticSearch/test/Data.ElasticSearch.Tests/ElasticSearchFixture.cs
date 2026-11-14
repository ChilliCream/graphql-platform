using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;

namespace HotChocolate.Data.ElasticSearch;

[CollectionDefinition("Elastic Tests")]
public class ElasticSearchFixture : ICollectionFixture<ElasticsearchResource>;

public sealed class ElasticsearchResource : IAsyncLifetime
{
    private const int Port = 9200;

    private readonly IContainer _container = new ContainerBuilder("docker.elastic.co/elasticsearch/elasticsearch:9.5.6")
        .WithEnvironment("discovery.type", "single-node")
        .WithEnvironment("xpack.security.enabled", "false")
        .WithEnvironment("ES_JAVA_OPTS", "-Xms1g -Xmx1g")
        .WithPortBinding(Port, true)
        .WithWaitStrategy(Wait.ForUnixContainer().UntilHttpRequestIsSucceeded(r => r.ForPort(Port)))
        .Build();

    public Uri Uri => new($"http://{_container.Hostname}:{_container.GetMappedPublicPort(Port)}");

    public async ValueTask InitializeAsync() => await _container.StartAsync();

    public async ValueTask DisposeAsync() => await _container.DisposeAsync();
}
