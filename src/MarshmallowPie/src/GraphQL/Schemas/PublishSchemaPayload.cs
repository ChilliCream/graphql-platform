namespace MarshmallowPie.GraphQL.Schemas
{
    public class PublishSchemaPayload
    {
        public PublishSchemaPayload(
            string sessionId,
            string? clientMutationId)
        {
            SessionId = sessionId;
            ClientMutationId = clientMutationId;
        }

        public string SessionId { get; }

        public string? ClientMutationId { get; }
    }
}
