using System;
using System.IO;
using System.Net;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate.Execution;
using HotChocolate.Execution.Serialization;
using static HotChocolate.AspNetCore.ErrorHelper;

namespace HotChocolate.AspNetCore.Serialization
{
    public class DefaultHttpResultSerializer : IHttpResultSerializer
    {
        private readonly JsonQueryResultSerializer _jsonSerializer;
        private readonly JsonArrayResponseStreamSerializer _jsonArraySerializer;
        private readonly MultiPartResponseStreamSerializer _multiPartSerializer;

        private readonly HttpResultSerialization _batchSerialization;
        private readonly HttpResultSerialization _deferSerialization;

        /// <summary>
        /// Creates a new instance of <see cref="DefaultHttpResultSerializer" />.
        /// </summary>
        /// <param name="batchSerialization">
        /// Specifies the output-format for batched queries.
        /// </param>
        /// <param name="deferSerialization">
        /// Specifies the output-format for deferred queries.
        /// </param>
        /// <param name="indented">
        /// Defines whether the underlying <see cref="Utf8JsonWriter"/>
        /// should pretty print the JSON which includes:
        /// indenting nested JSON tokens, adding new lines, and adding
        /// white space between property names and values.
        /// By default, the JSON is written without any extra white space.
        /// </param>
        public DefaultHttpResultSerializer(
            HttpResultSerialization batchSerialization = HttpResultSerialization.MultiPartChunked,
            HttpResultSerialization deferSerialization = HttpResultSerialization.MultiPartChunked,
            bool indented = true)
        {
            _batchSerialization = batchSerialization;
            _deferSerialization = deferSerialization;

            _jsonSerializer = new(indented);
            _jsonArraySerializer = new(indented);
            _multiPartSerializer = new(indented);
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

            // if a persisted query is not found when using the active persisted query pipeline
            // is used we will return a success status code.
            if (result.Errors is { Count: 1 } &&
                result.Errors[0] is { Code: ErrorCodes.Execution.PersistedQueryNotFound })
            {
                return HttpStatusCode.OK;
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
