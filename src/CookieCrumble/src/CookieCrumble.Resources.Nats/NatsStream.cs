using NATS.Client.JetStream;

namespace CookieCrumble.Resources;

/// <summary>
/// A JetStream stream created by <see cref="NatsResource.CreateStreamAsync"/>.
/// Disposing it deletes the stream.
/// </summary>
public sealed class NatsStream : IAsyncDisposable
{
    private readonly NatsJSContext _jetStream;

    internal NatsStream(NatsJSContext jetStream, string name)
    {
        _jetStream = jetStream;
        Name = name;
    }

    public string Name { get; }

    public async ValueTask DisposeAsync() => await _jetStream.DeleteStreamAsync(Name);
}
