using HotChocolate;
using HotChocolate.Types;

namespace MarshmallowPie.GraphQL
{
    public class UpdateEnvironmentInput
    {
        public UpdateEnvironmentInput(
            string id,
            string name,
            string? description,
            string? clientMutationId)
        {
            Id = id;
            Name = name;
            Description = description;
            ClientMutationId = clientMutationId;
        }

        [GraphQLType(typeof(NonNullType<IdType>))]
        public string Id { get; }

        public string Name { get; }

        public string? Description { get; }

        public string? ClientMutationId { get; }
    }
}
