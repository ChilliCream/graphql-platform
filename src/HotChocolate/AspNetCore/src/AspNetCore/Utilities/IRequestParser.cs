using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate.Language;

namespace HotChocolate.AspNetCore.Utilities
{
    public interface IRequestParser
    {
        ValueTask<IReadOnlyList<GraphQLRequest>> ReadGraphQLQueryAsync(
            Stream stream, 
            CancellationToken cancellationToken);

        ValueTask<IReadOnlyList<GraphQLRequest>> ReadJsonRequestAsync(
            Stream stream, 
            CancellationToken cancellationToken);
    }
}
