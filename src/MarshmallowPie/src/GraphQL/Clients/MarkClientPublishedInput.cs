namespace MarshmallowPie.GraphQL.Clients
{
    public class MarkClientPublishedInput
    {
        public MarkClientPublishedInput(
            string environmentName,
            string schemaName,
            string externalId,
            string? clientMutationId)
        {
            EnvironmentName = environmentName;
            SchemaName = schemaName;
            ExternalId = externalId;
            ClientMutationId = clientMutationId;
        }

        public string EnvironmentName { get; }

        public string SchemaName { get; }

        public string ExternalId { get; }

        public string? ClientMutationId { get; }
    }
}
