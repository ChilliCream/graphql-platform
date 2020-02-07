namespace MarshmallowPie.GraphQL.Clients
{
    public class CreateClientPayload
    {
        public CreateClientPayload(Schema schema, Client client, string? clientMutationId)
        {
            Schema = schema;
            Client = client;
            ClientMutationId = clientMutationId;
        }

        public Schema Schema { get; }

        public Client Client { get; }

        public string? ClientMutationId { get; }
    }
}
