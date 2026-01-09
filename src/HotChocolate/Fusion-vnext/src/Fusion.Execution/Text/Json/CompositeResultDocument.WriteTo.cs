using System.Buffers;
using System.Diagnostics;
using System.Text.Json;
using HotChocolate.Execution;
using HotChocolate.Text.Json;

namespace HotChocolate.Fusion.Text.Json;

public sealed partial class CompositeResultDocument : IRawJsonFormatter
{
    public void WriteTo(OperationResult result, IBufferWriter<byte> writer, JsonWriterOptions options)
    {
        options = options with { SkipValidation = true };
        using var jsonWriter = new Utf8JsonWriter(writer, options);
        var formatter = new RawJsonFormatter(this, jsonWriter);
        formatter.Write();
        jsonWriter.Flush();
    }

    internal ref struct RawJsonFormatter(CompositeResultDocument document, JsonWriter writer)
    {
        public void Write()
        {
            writer.WriteStartObject();

            if (document._errors?.Count > 0)
            {
                writer.WritePropertyName(JsonConstants.Errors);
                JsonValueFormatter.WriteErrors(
                    writer,
                    document._errors,
                    new JsonSerializerOptions(JsonSerializerDefaults.Web),
                    default);
            }

            var root = Cursor.Zero;
            var row = document._metaDb.Get(root);

            writer.WritePropertyName(JsonConstants.Data);

            if (row.TokenType is ElementTokenType.Null
                || (ElementFlags.Invalidated & row.Flags) == ElementFlags.Invalidated)
            {
                writer.WriteNullValue();
            }
            else
            {
                WriteObject(root, row);
            }

            if (document._extensions?.Count > 0)
            {
                writer.WritePropertyName(JsonConstants.Extensions);
                JsonValueFormatter.WriteDictionary(
                    writer,
                    document._extensions,
                    new JsonSerializerOptions(JsonSerializerDefaults.Web),
                    default);
            }

            writer.WriteEndObject();
        }

        public void WriteValue(Cursor cursor, DbRow row)
        {
            var tokenType = row.TokenType;

            // Inline reference resolution
            if (tokenType is ElementTokenType.Reference)
            {
                cursor = document._metaDb.GetLocationCursor(cursor);
                row = document._metaDb.Get(cursor);
                tokenType = row.TokenType;
            }

            Debug.Assert(tokenType is not ElementTokenType.Reference);
            Debug.Assert(tokenType is not ElementTokenType.EndObject);
            Debug.Assert(tokenType is not ElementTokenType.EndArray);

            switch (tokenType)
            {
                case ElementTokenType.StartObject
                    when (ElementFlags.SourceResult & row.Flags) != ElementFlags.SourceResult:
                    WriteObject(cursor, row);
                    break;

                case ElementTokenType.StartArray
                    when (ElementFlags.SourceResult & row.Flags) != ElementFlags.SourceResult:
                    WriteArray(cursor, row);
                    break;

                case ElementTokenType.None:
                case ElementTokenType.Null:
                    writer.WriteNullValue();
                    break;

                case ElementTokenType.True:
                    writer.WriteBooleanValue(true);
                    break;

                case ElementTokenType.False:
                    writer.WriteBooleanValue(false);
                    break;

                default:
                    document.WriteRawValueTo(writer, row);
                    break;
            }
        }

        private void WriteObject(Cursor start, DbRow startRow)
        {
            Debug.Assert(startRow.TokenType is ElementTokenType.StartObject);

            var current = start + 1;
            var end = start + startRow.NumberOfRows;

            writer.WriteStartObject();

            while (current < end)
            {
                var row = document._metaDb.Get(current);
                Debug.Assert(row.TokenType is ElementTokenType.PropertyName);

                if ((ElementFlags.IsInternal & row.Flags) == ElementFlags.IsInternal
                    || (ElementFlags.IsExcluded & row.Flags) == ElementFlags.IsExcluded)
                {
                    // skip name+value
                    current += 2;
                    continue;
                }

                // property name
                writer.WritePropertyName(document.ReadRawValue(row));

                // property value
                current++;
                row = document._metaDb.Get(current);
                WriteValue(current, row);

                // next property (move past value)
                current++;
            }

            writer.WriteEndObject();
        }

        private void WriteArray(Cursor start, DbRow startRow)
        {
            Debug.Assert(startRow.TokenType is ElementTokenType.StartArray);

            var current = start + 1;
            var end = start + startRow.NumberOfRows;

            writer.WriteStartArray();

            while (current < end)
            {
                var row = document._metaDb.Get(current);
                WriteValue(current, row);
                current++;
            }

            writer.WriteEndArray();
        }
    }
}
