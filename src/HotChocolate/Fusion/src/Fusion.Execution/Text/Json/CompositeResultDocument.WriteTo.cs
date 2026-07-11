using System.Diagnostics;
using HotChocolate.Execution;
using HotChocolate.Text.Json;

namespace HotChocolate.Fusion.Text.Json;

public sealed partial class CompositeResultDocument : IRawJsonFormatter
{
    public void WriteDataTo(JsonWriter jsonWriter)
    {
        if (HasNullMarkers)
        {
            var formatter = new NullMarkerRawJsonFormatter(this, jsonWriter);
            formatter.Write();
        }
        else
        {
            var formatter = new RawJsonFormatter(this, jsonWriter);
            formatter.Write();
        }
    }

    internal readonly ref struct RawJsonFormatter(CompositeResultDocument document, JsonWriter writer)
    {
        public void Write()
        {
            var root = Cursor.CreateZero();
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
                cursor = new Cursor(row.Location);
                row = document._metaDb.Get(cursor);
                tokenType = row.TokenType;
            }

            Debug.Assert(tokenType is not ElementTokenType.Reference);
            Debug.Assert(tokenType is not ElementTokenType.EndObject);
            Debug.Assert(tokenType is not ElementTokenType.EndArray);
            var isSourceResult = (ElementFlags.SourceResult & row.Flags) == ElementFlags.SourceResult;

            switch (tokenType)
            {
                case ElementTokenType.StartObject:
                    if (isSourceResult)
                    {
                        WriteSourceValue(row);
                    }
                    else
                    {
                        WriteObject(cursor, row);
                    }
                    break;

                case ElementTokenType.StartArray:
                    if (isSourceResult)
                    {
                        WriteSourceValue(row);
                    }
                    else
                    {
                        WriteArray(cursor, row);
                    }
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
                    if (isSourceResult)
                    {
                        document._sources[row.SourceDocumentId]
                            .WriteRawStringValueTo(writer, row.Location, row.SizeOrLength);
                    }
                    else
                    {
                        writer.WriteStringValue(document.ReadRawValue(row), skipEscaping: true);
                    }
                    break;

                case ElementTokenType.Number:
                    if (isSourceResult)
                    {
                        document._sources[row.SourceDocumentId]
                            .WriteRawNumberValueTo(writer, row.Location, row.SizeOrLength);
                    }
                    else
                    {
                        writer.WriteNumberValue(document.ReadRawValue(row));
                    }
                    break;

                default:
                    throw new NotSupportedException();
            }
        }

        private void WriteSourceValue(DbRow row)
        {
            var sourceDocument = document._sources[row.SourceDocumentId];
            // Reconstruct the source cursor from stored Location (Chunk) and SizeOrLength (Row)
            var sourceCursor = SourceResultDocument.Cursor.From(row.Location, row.SizeOrLength);
            var formatter = new SourceResultDocument.RawJsonFormatter(sourceDocument, writer);
            formatter.WriteValue(sourceCursor);
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

    internal readonly ref struct NullMarkerRawJsonFormatter(
        CompositeResultDocument document,
        JsonWriter writer)
    {
        public void Write()
        {
            var root = Cursor.CreateZero();
            var row = document._metaDb.Get(root);

            if (row.IsNullMarker
                || row.TokenType is ElementTokenType.Null
                || (ElementFlags.Invalidated & row.Flags) is ElementFlags.Invalidated)
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
            if (row.IsNullMarker)
            {
                writer.WriteNullValue();
                return;
            }

            var tokenType = row.TokenType;

            // The null marker belongs to this logical value/reference. Resolve only after
            // checking it so another path that references the same object remains unaffected.
            if (tokenType is ElementTokenType.Reference)
            {
                cursor = new Cursor(row.Location);
                row = document._metaDb.Get(cursor);
                tokenType = row.TokenType;
            }

            Debug.Assert(tokenType is not ElementTokenType.Reference);
            Debug.Assert(tokenType is not ElementTokenType.EndObject);
            Debug.Assert(tokenType is not ElementTokenType.EndArray);
            var isSourceResult = (ElementFlags.SourceResult & row.Flags) is ElementFlags.SourceResult;

            switch (tokenType)
            {
                case ElementTokenType.StartObject:
                    if (isSourceResult)
                    {
                        WriteSourceValue(row);
                    }
                    else
                    {
                        WriteObject(cursor, row);
                    }
                    break;

                case ElementTokenType.StartArray:
                    if (isSourceResult)
                    {
                        WriteSourceValue(row);
                    }
                    else
                    {
                        WriteArray(cursor, row);
                    }
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
                    if (isSourceResult)
                    {
                        document._sources[row.SourceDocumentId]
                            .WriteRawStringValueTo(writer, row.Location, row.SizeOrLength);
                    }
                    else
                    {
                        writer.WriteStringValue(document.ReadRawValue(row), skipEscaping: true);
                    }
                    break;

                case ElementTokenType.Number:
                    if (isSourceResult)
                    {
                        document._sources[row.SourceDocumentId]
                            .WriteRawNumberValueTo(writer, row.Location, row.SizeOrLength);
                    }
                    else
                    {
                        writer.WriteNumberValue(document.ReadRawValue(row));
                    }
                    break;

                default:
                    throw new NotSupportedException();
            }
        }

        private void WriteSourceValue(DbRow row)
        {
            var sourceDocument = document._sources[row.SourceDocumentId];
            var sourceCursor = SourceResultDocument.Cursor.From(row.Location, row.SizeOrLength);
            var formatter = new SourceResultDocument.RawJsonFormatter(sourceDocument, writer);
            formatter.WriteValue(sourceCursor);
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

                if ((ElementFlags.IsInternal & row.Flags) is ElementFlags.IsInternal
                    || (ElementFlags.IsExcluded & row.Flags) is ElementFlags.IsExcluded)
                {
                    current += 2;
                    continue;
                }

                writer.WritePropertyName(document.ReadRawValue(row));

                current++;
                row = document._metaDb.Get(current);
                WriteValue(current, row);

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
