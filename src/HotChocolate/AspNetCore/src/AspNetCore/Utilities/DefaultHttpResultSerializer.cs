using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate.Execution;
using HotChocolate.Execution.Serialization;

namespace HotChocolate.AspNetCore.Utilities
{
    public class DefaultHttpResultSerializer : IHttpResultSerializer
    {
        private readonly JsonQueryResultSerializer _queryResultSerializer =
            new JsonQueryResultSerializer(false);

        public string GetContentType(IExecutionResult result) =>
            "application/json; charset=utf-8";

        public HttpStatusCode GetStatusCode(IExecutionResult result)
        {
            if (result is IQueryResult q)
            {
                return q.Data is null
                    ? HttpStatusCode.InternalServerError
                    : HttpStatusCode.OK;
            }
            return HttpStatusCode.OK;
        }

        public async ValueTask SerializeAsync(
            IExecutionResult result,
            Stream stream,
            CancellationToken cancellationToken)
        {
            if (result is IReadOnlyQueryResult q)
            {
                await _queryResultSerializer.SerializeAsync(
                    q, stream, cancellationToken)
                    .ConfigureAwait(false);
            }
        }
    }
}
