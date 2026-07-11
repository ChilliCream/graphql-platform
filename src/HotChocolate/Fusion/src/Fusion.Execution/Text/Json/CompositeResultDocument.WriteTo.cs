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

            var remainingRows = startRow.NumberOfRows - 1;
            var operation = document._operation;

            writer.WriteStartObject();

            if (remainingRows == 0)
            {
                writer.WriteEndObject();
                return;
            }

            var reader = document._metaDb.CreateSequentialReader(start + 1);

            while (remainingRows > 0)
            {
                var property = reader.ReadProperty();

                if ((ElementFlags.IsInternal & property.Flags) == ElementFlags.IsInternal
                    || (ElementFlags.IsExcluded & property.Flags) == ElementFlags.IsExcluded)
                {
                    remainingRows -= 2;

                    if (remainingRows > 0)
                    {
                        reader.Advance(1);
                    }

                    continue;
                }

                writer.WritePropertyName(
                    operation
                        .GetSelectionById(property.SelectionId)
                        .Utf8ResponseName);

                WriteValue(reader.Cursor, reader.PeekRow());
                remainingRows -= 2;

                if (remainingRows > 0)
                {
                    reader.Advance(1);
                }
            }

            writer.WriteEndObject();
        }

        private void WriteArray(Cursor start, DbRow startRow)
        {
            Debug.Assert(startRow.TokenType is ElementTokenType.StartArray);

            var remainingRows = startRow.NumberOfRows - 1;

            writer.WriteStartArray();

            if (remainingRows == 0)
            {
                writer.WriteEndArray();
                return;
            }

            var reader = document._metaDb.CreateSequentialReader(start + 1);

            while (remainingRows > 0)
            {
                WriteValue(reader.Cursor, reader.PeekRow());
                remainingRows--;

                if (remainingRows > 0)
                {
                    reader.Advance(1);
                }
            }

            writer.WriteEndArray();
        }
    }
}
