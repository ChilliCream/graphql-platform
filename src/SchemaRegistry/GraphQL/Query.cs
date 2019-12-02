using System.Threading;
using System;
using System.Linq;
using System.Threading.Tasks;
using HotChocolate;
using MarshmallowPie.GraphQL.DataLoader;
using MarshmallowPie.Repositories;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types;
using System.Reflection;

namespace MarshmallowPie.GraphQL.Resolvers
{
    public class Query
    {
        public IQueryable<Schema> GetSchemas(
            [Service]ISchemaRepository repository)
        {
            return repository.Schemas;
        }

        public Task<Schema?> GetSchemaAsync(
            Guid id,
            [DataLoader]SchemaDataLoader dataLoader,
            CancellationToken cancellationToken)
        {
            return dataLoader.LoadAsync(id, cancellationToken);
        }
    }

    public class Mutation
    {
        public async Task<AddSchemaPayload> AddSchemaAsync(
            AddSchemaInput input,
            [Service]ISchemaRepository repository,
            CancellationToken cancellationToken)
        {
            var schema = new Schema(input.Name, input.Description);
            await repository.AddSchemaAsync(schema, cancellationToken);
            return new AddSchemaPayload(schema);
        }
    }

    public class AddSchemaInput
    {
        public string Name { get; set; }

        public string? Description { get; set; }
    }

    public class AddSchemaPayload
    {
        public AddSchemaPayload(Schema schema)
        {
            Schema = schema;
        }

        public Schema Schema { get; }
    }
}
