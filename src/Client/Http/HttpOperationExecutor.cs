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
        private readonly Dictionary<Type, IResultParser> _resultParsers;


        public Task<IOperationResult> ExecuteAsync(
            IOperation operation,
            CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public async Task<IOperationResult<T>> ExecuteAsync<T>(
            IOperation<T> operation,
            CancellationToken cancellationToken)
        {
            var request = new HttpRequestMessage(
                HttpMethod.Post,
                _client.BaseAddress);

            using (var stream = new MemoryStream())
            {
                await _serializer.SerializeAsync(operation, null, true, stream)
                    .ConfigureAwait(false);
                request.Content = new ByteArrayContent(stream.ToArray());
            }

            HttpResponseMessage response =
                await _client.SendAsync(request).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();

            var resultParser = (IResultParser<T>)_resultParsers[typeof(T)];

            using (var stream = await response.Content.ReadAsStreamAsync()
                .ConfigureAwait(false))
            {
                T result = await resultParser.ParseAsync(stream)
                    .ConfigureAwait(false);
            }


            throw new NotImplementedException();
        }
    }
}
