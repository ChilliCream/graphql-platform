using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate;
using HotChocolate.Types;
using HotChocolate.Types.Relay;
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


        public Task<QueryDocument?> GetQueryDocumentByHash(
            string hash,
            [Service]IClientRepository repository,
            CancellationToken cancellationToken) =>
            repository.GetQueryDocumentAsync(hash, cancellationToken);
    }
}
