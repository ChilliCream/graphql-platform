using System.Linq;
using HotChocolate;
using HotChocolate.Types;
using MarshmallowPie.Repositories;

namespace MarshmallowPie.GraphQL.Clients
{
    [ExtendObjectType(Name = "Client")]
    public class ClientExtension
    {
        public IQueryable<ClientVersion> GetVersions(
            [Parent]Client client,
            [Service]IClientRepository repository) =>
            repository.GetClientVersions().Where(t => t.ClientId == client.Id);
    }
}
