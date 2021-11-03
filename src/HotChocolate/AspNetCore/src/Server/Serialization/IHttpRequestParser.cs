using HotChocolate.Language;
using Microsoft.Extensions.Primitives;

namespace HotChocolate.Server.Serialization;

public interface IHttpRequestParser
{
    ValueTask<IReadOnlyList<GraphQLRequest>> ReadJsonRequestAsync(
        Stream stream,
        CancellationToken cancellationToken);

    GraphQLRequest ReadParamsRequest(
        Func<string, StringValues> getParameter);

    IReadOnlyList<GraphQLRequest> ReadOperationsRequest(
        string operations);
}
