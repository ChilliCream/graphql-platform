using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate;
using HotChocolate.Types;
using HotChocolate.Types.Relay;
using MarshmallowPie.GraphQL.Environments;
using MarshmallowPie.GraphQL.Schemas;
using MarshmallowPie.Repositories;

namespace MarshmallowPie.GraphQL.Clients
{
    [ExtendObjectType(Name = "Query")]
    public class ClientQueries
    {
        [UsePaging(SchemaType = typeof(NonNullType<ClientType>))]
        [UseFiltering]
        [UseSorting]
        public IQueryable<Client> GetClients(
            [Service]IClientRepository repository) =>
            repository.GetClients();


        public async Task<QueryDocument?> GetQueryDocumentByHash(
            string environmentName,
            string schemaName,
            string hash,
            [Service]IClientRepository repository,
            [DataLoader]EnvironmentByNameDataLoader environmentByName,
            [DataLoader]SchemaByNameDataLoader schemaByName,
            CancellationToken cancellationToken)
        {
            Environment environment = await environmentByName.LoadAsync(
                environmentName, cancellationToken)
                .ConfigureAwait(false);

            Schema schema = await schemaByName.LoadAsync(
                schemaName, cancellationToken)
                .ConfigureAwait(false);

            return await repository.GetQueryDocumentAsync(
                environment.Id, schema.Id, hash, cancellationToken)
                .ConfigureAwait(false);
        }
    }
}
