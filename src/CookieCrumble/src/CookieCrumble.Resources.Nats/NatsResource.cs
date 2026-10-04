using NATS.Client.Core;
using NATS.Client.JetStream;
using NATS.Client.JetStream.Models;
using Testcontainers.Nats;

namespace CookieCrumble.Resources;

public class NatsResource : ContainerResource<NatsContainer>
{
    private readonly Lazy<NatsConnection> _connection;

    public NatsResource()
    {
        _connection = new(() => new NatsConnection(new NatsOpts { Url = NatsConnectionString }));
    }

    public string NatsConnectionString => Container.GetConnectionString();

    /// <summary>
    /// Creates a uniquely named JetStream stream over <paramref name="subjects"/>.
    /// Disposing the returned stream deletes it.
    /// </summary>
    public async Task<NatsStream> CreateStreamAsync(
        IReadOnlyList<string> subjects,
        CancellationToken cancellationToken)
    {
        var name = $"S{Guid.NewGuid():N}";
        var jetStream = new NatsJSContext(_connection.Value);
        await jetStream.CreateStreamAsync(
            new StreamConfig
            {
                Name = name,
                Subjects = [.. subjects]
            },
            cancellationToken);

        return new NatsStream(jetStream, name);
    }

    protected override NatsContainer Build()
        => Configure(new NatsBuilder("nats:2.10-alpine")).Build();

    protected virtual NatsBuilder Configure(NatsBuilder builder) => builder;

    protected override async ValueTask DisposeAsyncCore()
    {
        if (_connection.IsValueCreated)
        {
            await _connection.Value.DisposeAsync();
        }
    }
}
