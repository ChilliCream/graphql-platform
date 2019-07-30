using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace HotChocolate.Execution
{
    public sealed class JsonQueryResultSerializer
        : IQueryResultSerializer
    {
        private readonly UTF8Encoding _encoding = new UTF8Encoding();

        public Task SerializeAsync(
            IReadOnlyQueryResult result,
            Stream stream)
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
            string json = JsonConvert.SerializeObject(dict);
            byte[] buffer = _encoding.GetBytes(json);
            return stream.WriteAsync(buffer, 0, buffer.Length);
        }

        public Task SerializeAsync(IReadOnlyQueryResult result, Stream stream, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }
}
