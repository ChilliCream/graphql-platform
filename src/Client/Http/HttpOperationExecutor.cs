using System;
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

        public Task<IOperationResult> ExecuteAsync(IOperation operation, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task<IOperationResult<T>> ExecuteAsync<T>(
            IOperation<T> operation,
            CancellationToken cancellationToken)
        {
            /*
            var request = new HttpRequestMessage(
                HttpMethod.Post, _client.BaseAddress);

            request.Content = new ByteArrayContent()
            using (var stream = new MemoryStream())
            {
                _serializer.SerializeAsync(operation, null, true, stream);
            }
 */
            throw new NotImplementedException();
        }
    }
}
