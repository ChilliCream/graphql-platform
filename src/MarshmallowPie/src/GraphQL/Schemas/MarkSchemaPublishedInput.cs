namespace MarshmallowPie.GraphQL.Schemas
{
    public class MarkSchemaPublishedInput
    {
        public MarkSchemaPublishedInput(
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
