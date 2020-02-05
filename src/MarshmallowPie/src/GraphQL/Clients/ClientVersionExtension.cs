using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GreenDonut;
using HotChocolate;
using HotChocolate.Types;

namespace MarshmallowPie.GraphQL.Clients
{
    [ExtendObjectType(Name = "ClientVersion")]
    public class ClientVersionExtension
    {
        public Task<IReadOnlyList<QueryDocument>> GetQueryDocuments(
            [Parent]ClientVersion clientVersion,
            [DataLoader]QueryDocumentByIdDataLoader dataLoader) =>
            dataLoader.LoadAsync(clientVersion.QueryIds.ToArray());
    }
}
