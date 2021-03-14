using System;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace HotChocolate.Execution.Serialization
{
    public sealed class JsonArrayResponseStreamSerializer : IResponseStreamSerializer
    {
        private const byte _leftBracket = (byte)'[';
        private const byte _rightBracket = (byte)']';
        private const byte _comma = (byte)',';
        private readonly JsonQueryResultSerializer _serializer;

        /// <summary>
        /// Creates a new instance of <see cref="JsonArrayResponseStreamSerializer" />.
        /// </summary>
        /// <param name="indented">
        /// Defines whether the underlying <see cref="Utf8JsonWriter"/>
        /// should pretty print the JSON which includes:
        /// indenting nested JSON tokens, adding new lines, and adding
        /// white space between property names and values.
        /// By default, the JSON is written without any extra white space.
        /// </param>
        public JsonArrayResponseStreamSerializer(bool indented = false)
        {
            _serializer = new(indented);
        }

        public Task SerializeAsync(
            IResponseStream responseStream,
            Stream outputStream,
            CancellationToken cancellationToken = default)
        {
            if (responseStream == null)
            {
                throw new ArgumentNullException(nameof(responseStream));
            }

            if (outputStream == null)
            {
                throw new ArgumentNullException(nameof(outputStream));
            }

            return WriteStreamAsync(responseStream, outputStream, cancellationToken);
        }

        private async Task WriteStreamAsync(
            IResponseStream responseStream,
            Stream outputStream,
            CancellationToken cancellationToken = default)
        {
            var delimiter = false;

            await outputStream
                .WriteAsync(new[] { _leftBracket }, 0, 1, cancellationToken)
                .ConfigureAwait(false);

            await foreach (IQueryResult result in responseStream.ReadResultsAsync()
                .WithCancellation(cancellationToken)
                .ConfigureAwait(false))
            {
                await WriteNextResultAsync(
                        result, outputStream, delimiter, cancellationToken)
                    .ConfigureAwait(false);
                delimiter = true;
            }

            await outputStream
                .WriteAsync(new[] { _rightBracket }, 0, 1, cancellationToken)
                .ConfigureAwait(false);
        }

        private async Task WriteNextResultAsync(
            IQueryResult result,
            Stream outputStream,
            bool delimiter,
            CancellationToken cancellationToken)
        {
            if (delimiter)
            {
                await outputStream.WriteAsync(
                    new[] { _comma }, 0, 1, cancellationToken)
                    .ConfigureAwait(false);
            }

            await _serializer.SerializeAsync(
                result, outputStream, cancellationToken)
                .ConfigureAwait(false);

            await outputStream.FlushAsync(cancellationToken)
                .ConfigureAwait(false);
        }
    }
}
