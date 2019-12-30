using System;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate;
using HotChocolate.Types;
using HotChocolate.Types.Relay;
using MarshmallowPie.Repositories;

namespace MarshmallowPie.GraphQL.Environments
{
    [ExtendObjectType(Name = "Mutation")]
    public class EnvironmentMutations
    {
        public async Task<CreateEnvironmentPayload?> CreateEnvironmentAsync(
            CreateEnvironmentInput input,
            [Service]IEnvironmentRepository repository,
            CancellationToken cancellationToken)
        {
            var environment = new Environment(input.Name, input.Description);
            await repository.AddEnvironmentAsync(environment, cancellationToken);
            return new CreateEnvironmentPayload(environment, input.ClientMutationId);
        }

        public async Task<UpdateEnvironmentPayload?> UpdateEnvironmentAsync(
            UpdateEnvironmentInput input,
            [Service]IIdSerializer idSerializer,
            [Service]IEnvironmentRepository repository,
            CancellationToken cancellationToken)
        {
            IdValue deserializedId = idSerializer.Deserialize(input.Id);

            if (!deserializedId.TypeName.Equals(nameof(Environment), StringComparison.Ordinal))
            {
                throw new GraphQLException("The specified id type is invalid.");
            }

            var environment = new Environment(
                (Guid)deserializedId.Value,
                input.Name,
                input.Description);

            await repository.UpdateEnvironmentAsync(environment, cancellationToken);
            return new UpdateEnvironmentPayload(environment, input.ClientMutationId);
        }
    }
}
