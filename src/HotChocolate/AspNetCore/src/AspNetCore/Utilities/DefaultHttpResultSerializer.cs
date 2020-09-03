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
            new JsonQueryResultSerializer();
        private readonly JsonArrayResponseStreamSerializer _responseStreamSerializer =
            new JsonArrayResponseStreamSerializer();

        public string GetContentType(IExecutionResult result) =>
            "application/json; charset=utf-8";

        public HttpStatusCode GetStatusCode(IExecutionResult result)
        {
            if (result is IQueryResult q)
            {
                return q.Data is null
                    ? q.ContextData is not null &&
                      q.ContextData.ContainsKey(ContextDataKeys.ValidationErrors)
                        ? HttpStatusCode.BadRequest
                        : HttpStatusCode.InternalServerError
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
                    ;
            }

            if (result is IResponseStream r)
            {
                await _responseStreamSerializer.SerializeAsync(
                    r, stream, cancellationToken)
                    ;
            }
        }
    }
}
