using StrawberryShake.Tools.Configuration;

namespace StrawberryShake.CodeGeneration;

public class TransportProfile
{
    public const string DefaultProfileName = "Default";

    public TransportProfile(
        string name,
        TransportType defaultTransport,
        TransportType? query = null,
        TransportType? mutation = null,
        TransportType? subscription = null)
    {
        Name = name;
        Query = query ?? defaultTransport;
        Mutation = mutation ?? defaultTransport;
        Subscription = subscription ?? defaultTransport;
    }

    public string Name { get; }

    public TransportType Query { get; }

    public TransportType Mutation { get; }

    public TransportType Subscription { get; }

    public static TransportProfile Default { get; } =
        new(DefaultProfileName, TransportType.Http, subscription: TransportType.WebSocket);
}
