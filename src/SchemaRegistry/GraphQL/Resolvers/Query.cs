using System.Threading;
using System;
using System.Linq;
using System.Threading.Tasks;
using HotChocolate;
using MarshmallowPie.GraphQL.DataLoader;
using MarshmallowPie.Repositories;

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


}
