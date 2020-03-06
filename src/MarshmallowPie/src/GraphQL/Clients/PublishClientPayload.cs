namespace MarshmallowPie.GraphQL.Clients
{
    public class PublishClientPayload
    {
        public PublishClientPayload(
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
