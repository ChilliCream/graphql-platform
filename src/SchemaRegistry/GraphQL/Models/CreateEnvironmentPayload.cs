namespace MarshmallowPie.GraphQL.Models
{
    public class CreateEnvironmentPayload
    {
        public CreateEnvironmentPayload(
            Environment environment,
            string? clientMutationId)
        {
            Environment = environment;
            ClientMutationId = clientMutationId;
        }

        public Environment Environment { get; }

        public string? ClientMutationId { get; }
    }
}
