using System;
using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate.Execution;
using HotChocolate.Execution.Serialization;
using static HotChocolate.AspNetCore.ErrorHelper;

namespace HotChocolate.AspNetCore.Serialization
{
    public class DefaultHttpResultSerializer : IHttpResultSerializer
    {
        private readonly JsonQueryResultSerializer _jsonSerializer = new();
        private readonly JsonArrayResponseStreamSerializer _jsonArraySerializer = new();
        private readonly MultiPartResponseStreamSerializer _multiPartSerializer = new();

        private readonly HttpResultSerialization _batchSerialization;
        private readonly HttpResultSerialization _deferSerialization;

        public DefaultHttpResultSerializer(
            HttpResultSerialization batchSerialization = HttpResultSerialization.MultiPartChunked,
            HttpResultSerialization deferSerialization = HttpResultSerialization.MultiPartChunked)
        {
            _batchSerialization = batchSerialization;
            _deferSerialization = deferSerialization;
        }

        public virtual string GetContentType(IExecutionResult result)
        {
            if (result == null)
            {
                throw new ArgumentNullException(nameof(result));
            }

            switch (result)
            {
                case QueryResult:
                    return ContentType.Json;

                case DeferredQueryResult:
                    return _deferSerialization == HttpResultSerialization.JsonArray
                        ? ContentType.Json
                        : ContentType.MultiPart;

                case BatchQueryResult:
                    return _batchSerialization == HttpResultSerialization.JsonArray
                        ? ContentType.Json
                        : ContentType.MultiPart;

                default:
                    return ContentType.Json;
            }
        }

        public virtual HttpStatusCode GetStatusCode(IExecutionResult result)
        {
            return result switch
            {
                QueryResult queryResult => GetStatusCode(queryResult),
                DeferredQueryResult => HttpStatusCode.OK,
                BatchQueryResult => HttpStatusCode.OK,
                _ => HttpStatusCode.InternalServerError
            };
        }

        private HttpStatusCode GetStatusCode(IQueryResult result)
        {
            if (result.Data is not null)
            {
                return HttpStatusCode.OK;
            }

            if (result.ContextData is not null)
            {
                if (result.ContextData.ContainsKey(WellKnownContextData.ValidationErrors))
                {
                    return HttpStatusCode.BadRequest;
                }

                if (result.ContextData.ContainsKey(WellKnownContextData.OperationNotAllowed))
                {
                    return HttpStatusCode.MethodNotAllowed;
                }
            }

            return HttpStatusCode.InternalServerError;
        }

        public virtual async ValueTask SerializeAsync(
            IExecutionResult result,
            Stream stream,
            CancellationToken cancellationToken)
        {
            switch (result)
            {
                case IQueryResult queryResult:
                    await _jsonSerializer
                        .SerializeAsync(queryResult, stream, cancellationToken)
                        .ConfigureAwait(false);
                    break;

                case DeferredQueryResult deferredResult
                    when _deferSerialization == HttpResultSerialization.JsonArray:
                    await _jsonArraySerializer
                        .SerializeAsync(deferredResult, stream, cancellationToken)
                        .ConfigureAwait(false);
                    break;

                case DeferredQueryResult deferredResult:
                    await _multiPartSerializer
                        .SerializeAsync(deferredResult, stream, cancellationToken)
                        .ConfigureAwait(false);
                    break;

                case BatchQueryResult batchResult
                    when _batchSerialization == HttpResultSerialization.JsonArray:
                    await _jsonArraySerializer
                        .SerializeAsync(batchResult, stream, cancellationToken)
                        .ConfigureAwait(false);
                    break;

                case BatchQueryResult batchResult:
                    await _multiPartSerializer
                        .SerializeAsync(batchResult, stream, cancellationToken)
                        .ConfigureAwait(false);
                    break;

                default:
                    await _jsonSerializer
                        .SerializeAsync(ResponseTypeNotSupported(), stream, cancellationToken)
                        .ConfigureAwait(false);
                    break;
            }
        }
    }
}
