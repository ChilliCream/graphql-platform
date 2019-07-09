using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace HotChocolate.Execution
{
    public sealed class JsonQueryResultSerializer
        : IQueryResultSerializer
    {
        public async Task SerializeAsync(
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

            byte[] buffer = Encoding.UTF8.GetBytes(
                JsonConvert.SerializeObject(result.ToDictionary()));

            await stream.WriteAsync(buffer, 0, buffer.Length)
                .ConfigureAwait(false);
        }
    }
}
