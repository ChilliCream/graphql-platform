using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using System.Threading;

namespace StrawberryShake.Http
{
    public abstract class JsonResultParserBase<T>
        : IResultParser<T>
        where T : class
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

        private static readonly byte[] _extensions = new byte[]
        {
            (byte)'e',
            (byte)'x',
            (byte)'t',
            (byte)'e',
            (byte)'n',
            (byte)'s',
            (byte)'i',
            (byte)'o',
            (byte)'n',
            (byte)'s',
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

        public Type ResultType => typeof(T);

        public Task<IOperationResult<T>> ParseAsync(
            Stream stream,
            CancellationToken cancellationToken)
        {
            if (stream is null)
            {
                throw new ArgumentNullException(nameof(stream));
            }

            return ParseInternalAsync(stream, cancellationToken);
        }

        async Task<IOperationResult> IResultParser.ParseAsync(
            Stream stream,
            CancellationToken cancellationToken) =>
            await ParseAsync(stream, cancellationToken).ConfigureAwait(false);

        private async Task<IOperationResult<T>> ParseInternalAsync(
            Stream stream,
            CancellationToken cancellationToken)
        {
            using (JsonDocument document = await JsonDocument.ParseAsync(stream)
                .ConfigureAwait(false))
            {
                if (document.RootElement.TryGetProperty(
                    _data, out JsonElement data))
                {
                    ParserData(data);
                }

                if (document.RootElement.TryGetProperty(
                    _error, out JsonElement errors))
                {
                }

                if (document.RootElement.TryGetProperty(
                    _extensions, out JsonElement extensions))
                {
                }
            }

            throw new Exception();
        }

        protected abstract T ParserData(JsonElement parent);
    }
}
