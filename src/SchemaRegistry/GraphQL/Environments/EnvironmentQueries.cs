using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate;
using HotChocolate.Types;
using HotChocolate.Types.Relay;
using MarshmallowPie.Repositories;

namespace MarshmallowPie.GraphQL.Environments
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

        public Task<Environment> GetEnvironmentByIdAsync(
            [GraphQLType(typeof(NonNullType<IdType>))]string id,
            [Service]IIdSerializer idSerializer,
            [DataLoader]EnvironmentByIdDataLoader dataLoader,
            CancellationToken cancellationToken)
        {
            IdValue deserializedId = idSerializer.Deserialize(id);

            if (!deserializedId.TypeName.Equals(nameof(Environment), StringComparison.Ordinal))
            {
                throw new GraphQLException("The specified id type is invalid.");
            }

            return dataLoader.LoadAsync((Guid)deserializedId.Value, cancellationToken);
        }

        public Task<Environment> GetEnvironmentByNameAsync(
            string name,
            [DataLoader]EnvironmentByNameDataLoader dataLoader,
            CancellationToken cancellationToken) =>
            dataLoader.LoadAsync(name, cancellationToken);

        [UseSorting]
        public Task<IReadOnlyList<Environment>> GetEnvironmentsByIdAsync(
            [GraphQLType(typeof(NonNullType<ListType<NonNullType<IdType>>>))]string[] ids,
            [Service]IIdSerializer idSerializer,
            [DataLoader]EnvironmentByIdDataLoader dataLoader,
            CancellationToken cancellationToken)
        {
            if (ids.Length == 0)
            {
                throw new GraphQLException("No ids where provided.");
            }

            var deserializedIds = new Guid[ids.Length];

            for (int i = 0; i < ids.Length; i++)
            {
                IdValue deserializedId = idSerializer.Deserialize(ids[i]);

                if (!deserializedId.TypeName.Equals(nameof(Environment), StringComparison.Ordinal))
                {
                    throw new GraphQLException("The specified id type is invalid.");
                }

                deserializedIds[i] = (Guid)deserializedId.Value;
            }

            return dataLoader.LoadAsync(deserializedIds, cancellationToken);
        }

        [UseSorting]
        public Task<IReadOnlyList<Environment>> GetEnvironmentsByNameAsync(
            string[] names,
            [DataLoader]EnvironmentByNameDataLoader dataLoader,
            CancellationToken cancellationToken)
        {
            if (names.Length == 0)
            {
                throw new GraphQLException("No names where provided.");
            }

            return dataLoader.LoadAsync(names, cancellationToken);
        }
    }
}
