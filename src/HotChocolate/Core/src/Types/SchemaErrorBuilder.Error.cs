using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using HotChocolate.Language;
using HotChocolate.Types;
using HotChocolate.Utilities;

#nullable enable

namespace HotChocolate
{
    public partial class SchemaErrorBuilder
    {
        private class Error : ISchemaError
        {
            private static readonly JsonWriterOptions _serializationOptions =
                new JsonWriterOptions
                {
                    Indented = true,
                    Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
                };

            public string Message { get; set; } = default!;

            public string? Code { get; set; }

            public ITypeSystemObject? TypeSystemObject { get; set; }

            public IReadOnlyCollection<object>? Path { get; set; }

            public ImmutableList<ISyntaxNode> SyntaxNodes { get; set; } =
                ImmutableList<ISyntaxNode>.Empty;

            IReadOnlyCollection<ISyntaxNode> ISchemaError.SyntaxNodes => SyntaxNodes;

            public ImmutableDictionary<string, object> Extensions { get; set; }
                = ImmutableDictionary<string, object>.Empty;

            IReadOnlyDictionary<string, object> ISchemaError.Extensions => Extensions;

            public Exception? Exception { get; set; }

            public unsafe override string ToString()
            {
                using var buffer = new ArrayWriter();

                using var writer = new Utf8JsonWriter(buffer, _serializationOptions);

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

                if (TypeSystemObject is INamedType namedType)
                {
                    writer.WriteString("type", namedType.Name.Value);
                }

                if (Path is { })
                {
                    writer.WritePropertyName("path");
                    writer.WriteStartArray();

                    foreach (string segment in Path.Select(t => t.ToString()!))
                    {
                        writer.WriteStringValue(segment);
                    }

                    writer.WriteEndArray();
                }

                if (Extensions is { })
                {
                    writer.WritePropertyName("extensions");
                    writer.WriteStartObject();

                    foreach (KeyValuePair<string, object> item in Extensions.OrderBy(t => t.Key))
                    {
                        writer.WritePropertyName(item.Key);

                        if (item.Value is null)
                        {
                            writer.WriteNullValue();
                        }
                        else if (item.Value is IField f)
                        {
                            writer.WriteStringValue(f.Name.Value);
                        }
                        else if (item.Value is INamedType n)
                        {
                            writer.WriteStringValue(n.Name.HasValue
                                ? n.Name.Value
                                : n.GetType().FullName);
                        }
                        else
                        {
                            writer.WriteStringValue(item.Value.ToString());
                        }
                    }

                    writer.WriteEndObject();
                }

                if (Exception is { })
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
}
