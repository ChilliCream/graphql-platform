using System.Diagnostics;
using HotChocolate.Execution;

namespace HotChocolate.Text.Json;

public sealed partial class ResultDocument : IRawJsonFormatter
{
    public void WriteDataTo(JsonWriter jsonWriter)
    {
        var formatter = new RawJsonFormatter(this, jsonWriter);
        formatter.Write();
    }

    internal readonly ref struct RawJsonFormatter(ResultDocument document, JsonWriter writer)
    {
        public void Write()
        {
            var root = Cursor.Zero;
            var row = document._metaDb.Get(root);

            if (row.TokenType is ElementTokenType.Null
                || (ElementFlags.IsInvalidated & row.Flags) == ElementFlags.IsInvalidated)
            {
                writer.WriteNullValue();
            }
            else
            {
                WriteObject(root, row);
            }
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

                case ElementTokenType.String:
                {
                    var isEncoded = false;
                    var value = document.ReadRawValue(row);

                    if ((ElementFlags.IsEncoded & row.Flags) == ElementFlags.IsEncoded)
                    {
                        isEncoded = true;
                    }
                    else
                    {
                        value = value[1..^1];
                    }

                    writer.WriteStringValue(value, skipEscaping: isEncoded);
                    break;
                }

                case ElementTokenType.Number:
                {
                    var value = document.ReadRawValue(row);
                    writer.WriteNumberValue(value);
                    break;
                }

                default:
                    throw new NotSupportedException();
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

                var flags = row.Flags;

                if ((flags & (ElementFlags.IsInternal | ElementFlags.IsExcluded | ElementFlags.IsDeferred)) != 0)
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
