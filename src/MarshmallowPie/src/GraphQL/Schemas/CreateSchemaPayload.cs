namespace MarshmallowPie.GraphQL.Schemas
{
    public class CreateSchemaPayload
    {
        public CreateSchemaPayload(Schema schema, string? clientMutationId)
        {
            Schema = schema;
            ClientMutationId = clientMutationId;
        }

        public Schema Schema { get; }

        public string? ClientMutationId { get; }
    }
}
