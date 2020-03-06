using System;
using HotChocolate;
using HotChocolate.Types;

namespace MarshmallowPie.GraphQL.Clients
{
    public class CreateClientInput
    {
        public CreateClientInput(
            string schemaId,
            string name,
            string? description,
            string? clientMutationId)
        {
            SchemaId = schemaId;
            Name = name;
            Description = description;
            ClientMutationId = clientMutationId;
        }

        [GraphQLType(typeof(NonNullType<IdType>))]
        public string SchemaId { get; }

        public string Name { get; }

        public string? Description { get; }

        public string? ClientMutationId { get; }
    }
}
