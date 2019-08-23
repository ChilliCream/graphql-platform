using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

namespace StrawberryShake
{
    public abstract class GeneratedResultParserBase<T>
        : IResultParser<T>
    {
        private static readonly byte[] _data = new byte[]
        {
            (byte)'d',
            (byte)'a',
            (byte)'t',
            (byte)'a'
        };

        private static readonly byte[] _error = new byte[]
        {
            (byte)'e',
            (byte)'r',
            (byte)'r',
            (byte)'o',
            (byte)'r'
        };

        private static readonly byte[] _typename = new byte[]
        {
            (byte)'t',
            (byte)'y',
            (byte)'p',
            (byte)'e',
            (byte)'n',
            (byte)'a',
            (byte)'m',
            (byte)'e',
        };

        protected ReadOnlySpan<byte> TypeName => _typename;

        public Task<T> ParseAsync(Stream stream)
        {
            if (stream is null)
            {
                throw new ArgumentNullException(nameof(stream));
            }

            return ParseInternalAsync(stream);
        }

        private async Task<T> ParseInternalAsync(Stream stream)
        {
            using (JsonDocument document = await JsonDocument.ParseAsync(stream))
            {
                if (document.RootElement.TryGetProperty(
                    _data, out JsonElement data))
                {
                    return ParserData(data);
                }
            }

            throw new Exception();
        }

        protected abstract T ParserData(JsonElement parent);
    }
}
