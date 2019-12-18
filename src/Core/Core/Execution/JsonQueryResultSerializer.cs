using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace HotChocolate.Execution
{
    public sealed class JsonQueryResultSerializer
        : IQueryResultSerializer
    {
        private const string _contentType = "application/json";
        private static readonly JsonSerializerOptions _options =
            new JsonSerializerOptions
            {
                IgnoreNullValues = false,
                WriteIndented = false
            };

        public string ContentType => _contentType;

        public Task SerializeAsync(
            IReadOnlyQueryResult result,
            Stream stream) =>
            SerializeAsync(result, stream, CancellationToken.None);

        public Task SerializeAsync(
            IReadOnlyQueryResult result,
            Stream stream,
            CancellationToken cancellationToken)
        {
            if (result is null)
            {
                throw new ArgumentNullException(nameof(result));
            }

            if (stream is null)
            {
                throw new ArgumentNullException(nameof(stream));
            }

            IReadOnlyDictionary<string, object> dict = result.ToDictionary();
            return JsonSerializer.SerializeAsync(stream, dict, _options, cancellationToken);
        }
    }
}
