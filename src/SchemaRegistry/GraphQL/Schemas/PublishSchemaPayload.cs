namespace MarshmallowPie.GraphQL.Schemas
{
    public class PublishSchemaPayload
    {
        public PublishSchemaPayload(
            SchemaVersion version,
            SchemaPublishReport report,
            string? clientMutationId)
        {
            Version = version;
            Report = report;
            ClientMutationId = clientMutationId;
        }

        public SchemaVersion Version { get; }

        public SchemaPublishReport Report { get; }

        public string? ClientMutationId { get; }
    }
}
