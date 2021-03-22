namespace StrawberryShake.CodeGeneration.CSharp.Analyzers
{
    public class StrawberryShakeTransportSettings
    {
        public string Name { get; set; } = default!;

        public TransportType Default { get; set; } = TransportType.Http;

        public TransportType? Query { get; set; }

        public TransportType? Mutation { get; set; }

        public TransportType? Subscription { get; set; }
    }
}
