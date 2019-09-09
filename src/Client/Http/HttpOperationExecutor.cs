using System.Linq;
using System.Collections.Generic;
using System;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace StrawberryShake.Http
{
    public class HttpOperationExecutor
        : IOperationExecutor
    {
        private readonly HttpClient _client;
        private readonly IOperationSerializer _serializer;
        private readonly IReadOnlyDictionary<Type, IResultParser> _resultParsers;

        public HttpOperationExecutor(
            HttpClient client,
            IOperationSerializer serializer,
            IEnumerable<IResultParser> resultParsers)
        {
            _client = client ?? throw new ArgumentNullException(nameof(client));
            _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
            _resultParsers = resultParsers.ToDictionary();
        }

        public Task<IOperationResult> ExecuteAsync(
            IOperation operation,
            CancellationToken cancellationToken)
        {
            if (operation is null)
            {
                throw new ArgumentNullException(nameof(operation));
            }

            return ExecuteOperationAsync(operation, cancellationToken);
        }

        public Task<IOperationResult<T>> ExecuteAsync<T>(
            IOperation<T> operation,
            CancellationToken cancellationToken)
        {
            if (operation is null)
            {
                throw new ArgumentNullException(nameof(operation));
            }

            return ExecuteAndCastAsync<T>(operation, cancellationToken);
        }

        public async Task<IOperationResult<T>> ExecuteAndCastAsync<T>(
           IOperation<T> operation,
           CancellationToken cancellationToken)
        {
            IOperationResult result =
                await ExecuteOperationAsync(operation, cancellationToken)
                    .ConfigureAwait(false);
            return (IOperationResult<T>)result;
        }

        private async Task<IOperationResult> ExecuteOperationAsync(
            IOperation operation,
            CancellationToken cancellationToken)
        {
            using (var request = new HttpRequestMessage(
                HttpMethod.Post,
                _client.BaseAddress))
            {
                using (var stream = new MemoryStream())
                {
                    await _serializer.SerializeAsync(operation, null, true, stream)
                        .ConfigureAwait(false);
                    request.Content = new ByteArrayContent(stream.ToArray());
                }

                request.Content.Headers.Add("Content-Type", "application/json");

                HttpResponseMessage response = await _client.SendAsync(request)
                    .ConfigureAwait(false);

                return await HandleResult(operation.ResultType, response, cancellationToken)
                    .ConfigureAwait(false);
            }
        }

        private async Task<IOperationResult> HandleResult(
            Type resultType,
            HttpResponseMessage response,
            CancellationToken cancellationToken)
        {
            response.EnsureSuccessStatusCode();

            // TOOD : throw error if not exists
            IResultParser resultParser = _resultParsers[resultType];

            using (var stream = await response.Content.ReadAsStreamAsync()
                .ConfigureAwait(false))
            {
                return await resultParser.ParseAsync(stream, cancellationToken)
                    .ConfigureAwait(false);
            }
        }
    }
}
