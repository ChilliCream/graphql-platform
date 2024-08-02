namespace StrawberryShake.Tools.Configuration;

public class StrawberryShakeSettingsTransportProfile
{
    public string Name { get; set; } = default!;

    public TransportType Default { get; set; } = TransportType.Http;

    public TransportType? Query { get; set; }

    public TransportType? Mutation { get; set; }

    public TransportType? Subscription { get; set; }

    public IEnumerable<TransportType> GetUsedTransports()
    {
        var set = new HashSet<TransportType> { Default, };

        if (Query is not null)
        {
            set.Add(Query.Value);
        }

        if (Mutation is not null)
        {
            set.Add(Mutation.Value);
        }

        if (Subscription is not null)
        {
            set.Add(Subscription.Value);
        }

        return set;
    }
}
