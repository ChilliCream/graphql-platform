namespace MarshmallowPie.GraphQL.Clients
{
    public class MarkClientPublishedPayload
    {
        public MarkClientPublishedPayload(
            Environment environment,
            Schema schema,
            ClientVersion clientVersion,
            string? clientMutationId)
        {
            Environment = environment;
            Schema = schema;
            ClientVersion = clientVersion;
            ClientMutationId = clientMutationId;
        }

        public Environment Environment { get; }

        public Schema Schema { get; }

        public ClientVersion ClientVersion { get; }

        public string? ClientMutationId { get; }
    }
}
