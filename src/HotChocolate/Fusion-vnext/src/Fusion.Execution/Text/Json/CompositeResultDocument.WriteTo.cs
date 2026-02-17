using System.Diagnostics;
using HotChocolate.Execution;
using HotChocolate.Text.Json;

namespace HotChocolate.Fusion.Text.Json;

public sealed partial class CompositeResultDocument : IRawJsonFormatter
{
    public void WriteDataTo(JsonWriter jsonWriter)
    {
        var formatter = new RawJsonFormatter(this, jsonWriter);
        formatter.Write();
    }

    internal readonly ref struct RawJsonFormatter(CompositeResultDocument document, JsonWriter writer)
    {
        public void Write()
        {
            var root = Cursor.Zero;
            var row = document._metaDb.Get(root);

            if (row.TokenType is ElementTokenType.Null
                || (ElementFlags.Invalidated & row.Flags) == ElementFlags.Invalidated)
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
                case ElementTokenType.StartObject
                    when (ElementFlags.SourceResult & row.Flags) != ElementFlags.SourceResult:
                    WriteObject(cursor, row);
                    break;

                case ElementTokenType.StartObject:
                {
                    var sourceDocument = document._sources[row.SourceDocumentId];
                    // Reconstruct the source cursor from stored Location (Chunk) and SizeOrLength (Row)
                    var sourceCursor = SourceResultDocument.Cursor.From(row.Location, row.SizeOrLength);
                    var formatter = new SourceResultDocument.RawJsonFormatter(sourceDocument, writer);
                    formatter.WriteValue(sourceCursor);
                    break;
                }

                case ElementTokenType.StartArray
                    when (ElementFlags.SourceResult & row.Flags) != ElementFlags.SourceResult:
                    WriteArray(cursor, row);
                    break;

                case ElementTokenType.StartArray:
                {
                    var sourceDocument = document._sources[row.SourceDocumentId];
                    // Reconstruct the source cursor from stored Location (Chunk) and SizeOrLength (Row)
                    var sourceCursor = SourceResultDocument.Cursor.From(row.Location, row.SizeOrLength);
                    var formatter = new SourceResultDocument.RawJsonFormatter(sourceDocument, writer);
                    formatter.WriteValue(sourceCursor);
                    break;
                }

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
                    var value = document.ReadRawValue(row);
                    writer.WriteStringValue(value, skipEscaping: true);
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
