namespace StrawberryShake.CodeGeneration.CSharp
{
    public class CSharpGeneratorTransportProfile
    {
        public CSharpGeneratorTransportProfile(
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
    }
}
