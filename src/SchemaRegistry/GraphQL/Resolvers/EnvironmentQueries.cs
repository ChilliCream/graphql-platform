using System;
using System.Linq;
using System.Runtime.Serialization;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate;
using HotChocolate.Types;
using HotChocolate.Types.Relay;
using MarshmallowPie.GraphQL.Types;
using MarshmallowPie.Repositories;

namespace MarshmallowPie.GraphQL.Resolvers
{
    [ExtendObjectType(Name = "Query")]
    public class EnvironmentQueries
    {
        [UsePaging(SchemaType = typeof(NonNullType<EnvironmentType>))]
        [UseFiltering]
        [UseSorting]
        public IQueryable<Environment> GetEnvironments(
            [Service]IEnvironmentRepository repository) =>
            repository.GetEnvironments();

        public Task<Environment> GetEnvironmentAsync(
            Guid id,
            [Service]IEnvironmentRepository repository,
            CancellationToken cancellationToken) =>
            repository.GetEnvironmentAsync(id, cancellationToken);
    }
}
