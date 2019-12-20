namespace MarshmallowPie.GraphQL.Models
{
    public class CreateSchemaPayload
    {
        public CreateSchemaPayload(Schema schema)
        {
            Schema = schema;
        }

        public Schema Schema { get; }

        public string? ClientMutationId { get; }
    }
}
