namespace MarshmallowPie.GraphQL.Environments
{
    public class UpdateEnvironmentPayload
    {
        public UpdateEnvironmentPayload(
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
