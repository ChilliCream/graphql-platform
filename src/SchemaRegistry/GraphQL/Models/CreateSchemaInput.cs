namespace MarshmallowPie.GraphQL.Models
{
    public class CreateSchemaInput
    {
        public CreateSchemaInput(
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
