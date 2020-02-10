namespace MarshmallowPie.GraphQL.Schemas
{
    public class MarkSchemaPublishedPayload
    {
        public MarkSchemaPublishedPayload(
            Environment environment,
            Schema schema,
            SchemaVersion schemaVersion,
            string? clientMutationId)
        {
            Environment = environment;
            Schema = schema;
            SchemaVersion = schemaVersion;
            ClientMutationId = clientMutationId;
        }

        public Environment Environment { get; }

        public Schema Schema { get; }

        public SchemaVersion SchemaVersion { get; }

        public string? ClientMutationId { get; }
    }
}
