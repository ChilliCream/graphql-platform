using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate.Language;
using Microsoft.AspNetCore.Http;

namespace HotChocolate.AspNetCore.Serialization;

public interface IHttpRequestParser
{
    ValueTask<IReadOnlyList<GraphQLRequest>> ReadJsonRequestAsync(
        Stream stream,
        CancellationToken cancellationToken);

    GraphQLRequest ReadParamsRequest(
        IQueryCollection parameters);

    IReadOnlyList<GraphQLRequest> ReadOperationsRequest(
        string operations);
}
