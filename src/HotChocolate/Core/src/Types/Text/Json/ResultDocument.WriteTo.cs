using System.Buffers;
using System.Diagnostics;
using System.Text.Json;
using HotChocolate.Execution;

namespace HotChocolate.Text.Json;

public sealed partial class ResultDocument : IRawJsonFormatter
{
    public void WriteTo(OperationResult result, IBufferWriter<byte> writer, JsonWriterOptions options)
    {
        options = options with { SkipValidation = true };
        using var jsonWriter = new Utf8JsonWriter(writer, options);
        var formatter = new RawJsonFormatter(this, jsonWriter);
        formatter.Write(result);
        jsonWriter.Flush();
    }

    internal ref struct RawJsonFormatter(ResultDocument document, Utf8JsonWriter writer)
    {
        public void Write(OperationResult result)
        {
            writer.WriteStartObject();

            if (!result.Errors.IsEmpty)
            {
                writer.WritePropertyName(JsonConstants.Errors);
                JsonValueFormatter.WriteErrors(
                    writer,
                    result.Errors,
                    new JsonSerializerOptions(JsonSerializerDefaults.Web),
                    default);
            }

            var root = Cursor.Zero;
            var row = document._metaDb.Get(root);

            writer.WritePropertyName(JsonConstants.Data);

            if (row.TokenType is ElementTokenType.Null
                || (ElementFlags.IsInvalidated & row.Flags) == ElementFlags.IsInvalidated)
            {
                writer.WriteNullValue();
            }
            else
            {
                WriteObject(root, row);
            }

            if (result.Extensions?.Count > 0)
            {
                writer.WritePropertyName(JsonConstants.Extensions);
                JsonValueFormatter.WriteDictionary(
                    writer,
                    result.Extensions,
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
                case ElementTokenType.StartObject:
                    WriteObject(cursor, row);
                    break;

                case ElementTokenType.StartArray:
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
                    var rawValue = document.ReadRawValue(row);
                    writer.WriteRawValue(rawValue, skipInputValidation: true);
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
                var propertyName = document.ReadRawValue(row);
                writer.WritePropertyName(propertyName);

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
