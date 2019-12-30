using System;

namespace MarshmallowPie.GraphQL
{
    public class UpdateEnvironmentInput
    {
        public UpdateEnvironmentInput(
            Guid id,
            string name,
            string? description,
            string? clientMutationId)
        {
            Id = id;
            Name = name;
            Description = description;
            ClientMutationId = clientMutationId;
        }

        public Guid Id { get; }

        public string Name { get; }

        public string? Description { get; }

        public string? ClientMutationId { get; }
    }
}
