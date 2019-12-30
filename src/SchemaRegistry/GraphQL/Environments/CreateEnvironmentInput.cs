namespace MarshmallowPie.GraphQL.Environments
{
    public class CreateEnvironmentInput
    {
        public CreateEnvironmentInput(
            string name,
            string? description,
            string? clientMutationId)
        {
            Name = name;
            Description = description;
            ClientMutationId = clientMutationId;
        }

        public string Name { get; }

        public string? Description { get; }

        public string? ClientMutationId { get; }
    }
}
