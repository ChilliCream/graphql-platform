using System;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate;
using HotChocolate.Types;
using MarshmallowPie.GraphQL.Models;
using MarshmallowPie.Repositories;

namespace MarshmallowPie.GraphQL.Resolvers
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
            [Service]IEnvironmentRepository repository,
            CancellationToken cancellationToken)
        {
            var environment = new Environment(input.Id, input.Name, input.Description);
            await repository.UpdateEnvironmentAsync(environment, cancellationToken);
            return new UpdateEnvironmentPayload(environment, input.ClientMutationId);
        }
    }
}
