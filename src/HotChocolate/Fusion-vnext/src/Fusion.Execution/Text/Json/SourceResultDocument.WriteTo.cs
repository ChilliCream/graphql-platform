using System.Diagnostics;
using System.Text.Json;
using HotChocolate.Text.Json;

namespace HotChocolate.Fusion.Text.Json;

public sealed partial class SourceResultDocument
{
    internal ref struct RawJsonFormatter(SourceResultDocument document, JsonWriter writer)
    {
        public void WriteValue(Cursor cursor)
        {
            var row = document._parsedData.Get(cursor);
            WriteValue(cursor, row);
        }

        private void WriteValue(Cursor cursor, DbRow row)
        {
            var tokenType = row.TokenType;

            Debug.Assert(tokenType is not JsonTokenType.EndObject);
            Debug.Assert(tokenType is not JsonTokenType.EndArray);

            switch (tokenType)
            {
                case JsonTokenType.StartObject:
                    WriteObject(cursor, row);
                    break;

                case JsonTokenType.StartArray:
                    WriteArray(cursor, row);
                    break;

                case JsonTokenType.None:
                case JsonTokenType.Null:
                    writer.WriteNullValue();
                    break;

                case JsonTokenType.True:
                    writer.WriteBooleanValue(true);
                    break;

                case JsonTokenType.False:
                    writer.WriteBooleanValue(false);
                    break;

                case JsonTokenType.String:
                {
                    var value = document.ReadRawValue(row, includeQuotes: true);
                    writer.WriteStringValue(value, skipEscaping: true);
                    break;
                }

                case JsonTokenType.Number:
                {
                    var value = document.ReadRawValue(row, includeQuotes: false);
                    writer.WriteNumberValue(value);
                    break;
                }

                default:
                    throw new NotSupportedException();
            }
        }

        private void WriteObject(Cursor start, DbRow startRow)
        {
            Debug.Assert(startRow.TokenType is JsonTokenType.StartObject);

            var current = start + 1;
            var end = start + startRow.NumberOfRows - 1;

            writer.WriteStartObject();

            while (current < end)
            {
                var row = document._parsedData.Get(current);
                Debug.Assert(row.TokenType is JsonTokenType.PropertyName);

                // property name
                writer.WritePropertyName(document.ReadRawValue(row, includeQuotes: false));

                // property value
                current++;
                row = document._parsedData.Get(current);
                WriteValue(current, row);

                // next property (move past value)
                if (row.IsSimpleValue)
                {
                    current++;
                }
                else
                {
                    current += row.NumberOfRows;
                }
            }

            writer.WriteEndObject();
        }

        private void WriteArray(Cursor start, DbRow startRow)
        {
            Debug.Assert(startRow.TokenType is JsonTokenType.StartArray);

            var current = start + 1;
            var end = start + startRow.NumberOfRows - 1;

            writer.WriteStartArray();

            while (current < end)
            {
                var row = document._parsedData.Get(current);
                WriteValue(current, row);

                if (row.IsSimpleValue)
                {
                    current++;
                }
                else
                {
                    current += row.NumberOfRows;
                }
            }

            writer.WriteEndArray();
        }
    }
}
