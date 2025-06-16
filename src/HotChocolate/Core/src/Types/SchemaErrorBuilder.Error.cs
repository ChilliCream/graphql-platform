using System.Collections.Immutable;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using HotChocolate.Buffers;
using HotChocolate.Language;
using HotChocolate.Types;
using HotChocolate.Utilities;

#nullable enable

namespace HotChocolate;

public partial class SchemaErrorBuilder
{
    private sealed class Error : ISchemaError
    {
        private static readonly JsonWriterOptions s_serializationOptions = new()
        {
            Indented = true,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };

        public string Message { get; set; } = null!;

        public string? Code { get; set; }

        public TypeSystemObject? TypeSystemObject { get; set; }

        public IReadOnlyCollection<object>? Path { get; set; }

        public ImmutableList<ISyntaxNode> SyntaxNodes { get; set; } = [];

        IReadOnlyCollection<ISyntaxNode> ISchemaError.SyntaxNodes => SyntaxNodes;

        public ImmutableDictionary<string, object> Extensions { get; set; }
            = ImmutableDictionary<string, object>.Empty;

        IReadOnlyDictionary<string, object> ISchemaError.Extensions => Extensions;

        public Exception? Exception { get; set; }

        public override unsafe string ToString()
        {
            using var buffer = new PooledArrayWriter();

            using var writer = new Utf8JsonWriter(buffer, s_serializationOptions);

            writer.WriteStartObject();
            Serialize(writer);
            writer.WriteEndObject();

            writer.Flush();

            fixed (byte* b = buffer.GetInternalBuffer())
            {
                return Encoding.UTF8.GetString(b, buffer.Length);
            }
        }

        private void Serialize(Utf8JsonWriter writer)
        {
            writer.WriteString("message", Message);

            if (Code is { })
            {
                writer.WriteString("code", Code);
            }

            if (TypeSystemObject is ITypeDefinition typeDefinition)
            {
                writer.WriteString("type", typeDefinition.Name);
            }

            if (Path is { })
            {
                writer.WritePropertyName("path");
                writer.WriteStartArray();

                foreach (var segment in Path.Select(t => t.ToString()!))
                {
                    writer.WriteStringValue(segment);
                }

                writer.WriteEndArray();
            }

            writer.WritePropertyName("extensions");
            writer.WriteStartObject();

            foreach (var item in Extensions.OrderBy(t => t.Key))
            {
                writer.WritePropertyName(item.Key);

                if (item.Value is null)
                {
                    writer.WriteNullValue();
                }
                else if (item.Value is IFieldDefinition f)
                {
                    writer.WriteStringValue(f.Name);
                }
                else if (item.Value is ITypeDefinition n)
                {
                    writer.WriteStringValue(n.Name ?? n.GetType().FullName);
                }
                else
                {
                    writer.WriteStringValue(item.Value.ToString());
                }
            }

            writer.WriteEndObject();

            if (Exception is not null)
            {
                writer.WritePropertyName("exception");
                writer.WriteStringValue(Exception.Message);
            }
        }

        public Error Clone()
        {
            return (Error)MemberwiseClone();
        }
    }
}
