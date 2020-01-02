using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate;
using HotChocolate.Types;
using HotChocolate.Types.Relay;
using MarshmallowPie.Repositories;

namespace MarshmallowPie.GraphQL.Schemas
{
    [ExtendObjectType(Name = "Query")]
    public class SchemaQueries
    {
        [UsePaging(SchemaType = typeof(NonNullType<SchemaType>))]
        [UseFiltering]
        [UseSorting]
        public IQueryable<Schema> GetSchemas(
            [Service]ISchemaRepository repository) =>
            repository.GetSchemas();

        public Task<Schema> GetSchemaByIdAsync(
            [GraphQLType(typeof(NonNullType<IdType>))]string id,
            [Service]IIdSerializer idSerializer,
            [DataLoader]SchemaByIdDataLoader dataLoader,
            CancellationToken cancellationToken)
        {
            IdValue deserializedId = idSerializer.Deserialize(id);

            if (!deserializedId.TypeName.Equals(nameof(Schema), StringComparison.Ordinal))
            {
                throw new GraphQLException("The specified id type is invalid.");
            }

            return dataLoader.LoadAsync((Guid)deserializedId.Value, cancellationToken);
        }

        public Task<Schema> GetSchemaByNameAsync(
            string name,
            [Service]IIdSerializer idSerializer,
            [DataLoader]SchemaByNameDataLoader dataLoader,
            CancellationToken cancellationToken)
        {
            return dataLoader.LoadAsync(name, cancellationToken);
        }

        [UseSorting]
        public Task<IReadOnlyList<Schema>> GetSchemasByIdAsync(
            [GraphQLType(typeof(NonNullType<ListType<NonNullType<IdType>>>))]string[] ids,
            [Service]IIdSerializer idSerializer,
            [DataLoader]SchemaByIdDataLoader dataLoader,
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

                if (!deserializedId.TypeName.Equals(nameof(Schema), StringComparison.Ordinal))
                {
                    throw new GraphQLException("The specified id type is invalid.");
                }

                deserializedIds[i] = (Guid)deserializedId.Value;
            }

            return dataLoader.LoadAsync(deserializedIds, cancellationToken);
        }

        [UseSorting]
        public Task<IReadOnlyList<Schema>> GetSchemasByNameAsync(
            string[] names,
            [Service]IIdSerializer idSerializer,
            [DataLoader]SchemaByNameDataLoader dataLoader,
            CancellationToken cancellationToken)
        {
            if (names.Length == 0)
            {
                throw new GraphQLException("No ids where provided.");
            }

            return dataLoader.LoadAsync(names, cancellationToken);
        }

        [UsePaging(SchemaType = typeof(NonNullType<SchemaVersionType>))]
        [UseFiltering]
        [UseSorting]
        public IQueryable<SchemaVersion> GetSchemaVersions(
            [Service]ISchemaRepository repository) =>
            repository.GetSchemaVersions();

        public Task<SchemaVersion> GetSchemaVersionsByIdAsync(
            [GraphQLType(typeof(NonNullType<IdType>))]string id,
            [Service]IIdSerializer idSerializer,
            [DataLoader]SchemaVersionDataLoader dataLoader,
            CancellationToken cancellationToken)
        {
            IdValue deserializedId = idSerializer.Deserialize(id);

            if (!deserializedId.TypeName.Equals(nameof(SchemaVersion), StringComparison.Ordinal))
            {
                throw new GraphQLException("The specified id type is invalid.");
            }

            return dataLoader.LoadAsync((Guid)deserializedId.Value, cancellationToken);
        }

        [UseSorting]
        public Task<IReadOnlyList<SchemaVersion>> GetSchemaVersionsByIdAsync(
            [GraphQLType(typeof(NonNullType<ListType<NonNullType<IdType>>>))]string[] ids,
            [Service]IIdSerializer idSerializer,
            [DataLoader]SchemaVersionDataLoader dataLoader,
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

                if (!deserializedId.TypeName.Equals(nameof(Schema), StringComparison.Ordinal))
                {
                    throw new GraphQLException("The specified id type is invalid.");
                }

                deserializedIds[i] = (Guid)deserializedId.Value;
            }

            return dataLoader.LoadAsync(deserializedIds, cancellationToken);
        }
    }
}
