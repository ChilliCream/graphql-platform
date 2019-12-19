using System.Threading;
using System;
using System.Linq;
using System.Threading.Tasks;
using HotChocolate;
using MarshmallowPie.GraphQL.DataLoader;
using MarshmallowPie.Repositories;
using HotChocolate.Types.Relay;
using HotChocolate.Types;

namespace MarshmallowPie.GraphQL.Resolvers
{
    public class Query
    {
        [UsePaging]
        [UseFiltering]
        [UseSorting]
        public IQueryable<Environment> GetEnvironments(
            [Service]IEnvironmentRepository repository) =>
            repository.GetEnvironments();
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
