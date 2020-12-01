using System.Text.Json;

namespace StrawberryShake.Transport
{
    public sealed class SocketMessageWriter
        : MessageWriter
    {
        private static readonly JsonWriterOptions _options =
            new JsonWriterOptions { SkipValidation = true };
        private readonly Utf8JsonWriter _writer;

        public SocketMessageWriter()
        {
            _writer = new Utf8JsonWriter(this, _options);
        }

        public void WriteStartObject()
        {
            _writer.WriteStartObject();
            _writer.Flush();
        }

        public void WriteEndObject()
        {
            _writer.WriteEndObject();
            _writer.Flush();
        }

        public void WriteId(string id)
        {
            _writer.WritePropertyName("id");
            _writer.WriteStringValue(id);
            _writer.Flush();
        }

        public void WriteType(string type)
        {
            _writer.WritePropertyName("type");
            _writer.WriteStringValue(type);
            _writer.Flush();
        }

        public void WriteStartPayload()
        {
            _writer.WritePropertyName("payload");
            _writer.Flush();
        }
    }
}
