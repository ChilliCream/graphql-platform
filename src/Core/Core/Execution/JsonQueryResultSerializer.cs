using System.IO;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using HotChocolate.Utilities;

namespace HotChocolate.Execution
{
    public sealed class JsonQueryResultSerializer
        : IQueryResultSerializer
    {
        private const string _data = "data";
        private const string _errors = "errors";
        private const string _extensions = "extensions";

        public async Task SerializeAsync(
            IReadOnlyQueryResult result,
            Stream stream)
        {
            var formatted = new OrderedDictionary();

            if (result.Errors.Count > 0)
            {
                formatted[_errors] = result.Errors;
            }

            if (result.Data.Count > 0)
            {
                formatted[_data] = result.Data;
            }

            if (result.Extensions.Count > 0)
            {
                formatted[_extensions] = result.Extensions;
            }

            using (var memStream = new MemoryStream())
            {
                ObjectToJsonBytes.WriteObjectToStream(formatted, memStream);

                await stream.WriteAsync(memStream.ToArray(), 0, memStream.ToArray().Length)
                    .ConfigureAwait(false);
            }
        }
    }
}
