using System.Buffers;
using System.Buffers.Text;
using System.Text;

namespace HotChocolate.Subscriptions.Postgres;

internal readonly struct PostgresMessageEnvelope
{
    private static readonly Random _random = Random.Shared;
    private const byte separator = (byte)':';
    private const byte _messageIdLength = 24;

    public PostgresMessageEnvelope(string topic, string payload)
    {
        Topic = topic;
        Payload = payload;
    }

    public string Topic { get; }

    public string Payload { get; }

    public string Format()
    {
        var topicMaxBytesCount = Encoding.UTF8.GetMaxByteCount(Topic.Length);
        var payloadMaxBytesCount = Encoding.UTF8.GetMaxByteCount(Payload.Length);
        // we encode the topic to base64 to ensure that we do not have the separator in the topic
        var topicMaxLength = Base64.GetMaxEncodedToUtf8Length(topicMaxBytesCount);
        var maxSize = topicMaxLength + 2 + payloadMaxBytesCount + _messageIdLength;

        byte[]? bufferArray = null;
        var buffer = maxSize < 1024
            ? stackalloc byte[maxSize]
            : bufferArray = ArrayPool<byte>.Shared.Rent(maxSize);

        var slicedBuffer = buffer;

        // prefix with id
        var id = buffer[.._messageIdLength];
        _random.NextBytes(id);

        const int numberOfLetter = 26;
        const int asciiCodeOfLowerCaseA = 97;
        for (var i = 0; i < _messageIdLength; i++)
        {
            slicedBuffer[i] = (byte)(id[i] % numberOfLetter + asciiCodeOfLowerCaseA);
        }

        slicedBuffer = slicedBuffer[_messageIdLength..];

        // write separator
        slicedBuffer[0] = separator;
        slicedBuffer = slicedBuffer[1..];

        // write topic as base64
        var topicLengthUtf8 = Encoding.UTF8.GetBytes(Topic, slicedBuffer);
        Base64.EncodeToUtf8InPlace(slicedBuffer, topicLengthUtf8, out var topicLengthBase64);
        slicedBuffer = slicedBuffer[topicLengthBase64..];

        // write separator
        slicedBuffer[0] = separator;
        slicedBuffer = slicedBuffer[1..];

        // write payload
        var payloadLengthUtf8 = Encoding.UTF8.GetBytes(Payload, slicedBuffer);

        // create string
        var endOfEncodedString = topicLengthBase64 + 2 + payloadLengthUtf8 + _messageIdLength;
        var result = Encoding.UTF8.GetString(buffer[..endOfEncodedString]);

        if (bufferArray is not null)
        {
            ArrayPool<byte>.Shared.Return(bufferArray);
        }

        return result;
    }

    public static PostgresMessageEnvelope? Parse(string message)
    {
        var maxSize = Encoding.UTF8.GetMaxByteCount(message.Length);

        byte[]? bufferArray = null;
        var buffer = maxSize < 1024
            ? stackalloc byte[maxSize]
            : bufferArray = ArrayPool<byte>.Shared.Rent(maxSize);

        // get the bytes of the message
        var utf8ByteLength = Encoding.UTF8.GetBytes(message, buffer);

        // slice the buffer to the actual length
        buffer = buffer[..utf8ByteLength];

        // remove message id and separator
        buffer = buffer[(_messageIdLength + 1)..];

        // find the separator
        var indexOfColon = buffer.IndexOf(separator);
        if (indexOfColon == -1)
        {
            return null;
        }

        var topicLengthBase64 = indexOfColon;
        Base64.DecodeFromUtf8InPlace(buffer[..topicLengthBase64], out var topicLengthUtf8);
        var topic = Encoding.UTF8.GetString(buffer[..topicLengthUtf8]);
        var payload = Encoding.UTF8.GetString(buffer[(indexOfColon + 1)..]);

        if (bufferArray is not null)
        {
            ArrayPool<byte>.Shared.Return(bufferArray);
        }

        return new PostgresMessageEnvelope(topic, payload);
    }
}
