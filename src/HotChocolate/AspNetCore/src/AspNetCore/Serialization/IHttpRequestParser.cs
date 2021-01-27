using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using HotChocolate.Language;

namespace HotChocolate.AspNetCore.Serialization
{
    public interface IHttpRequestParser
    {
        ValueTask<IReadOnlyList<GraphQLRequest>> ReadJsonRequestAsync(
            Stream stream,
            CancellationToken cancellationToken);

        ValueTask<IReadOnlyList<GraphQLRequest>> ReadFormAsync(
            IFormCollection form);

        GraphQLRequest ReadParamsRequest(
            IQueryCollection parameters);
    }
}
