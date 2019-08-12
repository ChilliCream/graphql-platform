using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace HotChocolate.Execution.Batching
{
    public sealed class JsonArrayResponseStreamSerializer
        : IResponseStreamSerializer
    {
        private const string _contentType = "application/json";
        private const byte _leftBracket = (byte)'[';
        private const byte _rightBracket = (byte)']';
        private const byte _comma = (byte)',';
        private readonly UTF8Encoding _encoding = new UTF8Encoding();
        private readonly JsonQueryResultSerializer _serializer =
            new JsonQueryResultSerializer();

        public string ContentType => _contentType;

        public Task SerializeAsync(
            IResponseStream responseStream,
            Stream outputStream) =>
            SerializeAsync(
                responseStream,
                outputStream,
                CancellationToken.None);

        public async Task SerializeAsync(
            IResponseStream responseStream,
            Stream outputStream,
            CancellationToken cancellationToken)
        {
            outputStream.WriteByte(_leftBracket);

            await WriteNextResultAsync(
                responseStream, outputStream, false, cancellationToken)
                .ConfigureAwait(false);

            while (!responseStream.IsCompleted)
            {
                await WriteNextResultAsync(
                    responseStream, outputStream, true, cancellationToken)
                    .ConfigureAwait(false);
            }

            outputStream.WriteByte(_rightBracket);
        }

        private async Task WriteNextResultAsync(
            IResponseStream responseStream,
            Stream outputStream,
            bool delimiter,
            CancellationToken cancellationToken)
        {
            IReadOnlyQueryResult result =
                await responseStream.ReadAsync(cancellationToken)
                    .ConfigureAwait(false);

            if (result == null)
            {
                return;
            }

            if (delimiter)
            {
                outputStream.WriteByte(_comma);
            }

            await _serializer.SerializeAsync(
                result, outputStream, cancellationToken)
                .ConfigureAwait(false);

            await outputStream.FlushAsync(cancellationToken)
                .ConfigureAwait(false);
        }
    }
}
