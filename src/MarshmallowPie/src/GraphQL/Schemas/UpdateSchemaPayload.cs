namespace MarshmallowPie.GraphQL.Schemas
{
    public class UpdateSchemaPayload
    {
        public UpdateSchemaPayload(
            Schema environment,
            string? clientMutationId)
        {
            Schema = environment;
            ClientMutationId = clientMutationId;
        }

        public Schema Schema { get; }

        public string? ClientMutationId { get; }
    }
}
