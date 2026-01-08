using System.Buffers;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text.Json;
using HotChocolate.Execution;

namespace HotChocolate.Text.Json;

public sealed partial class ResultDocument : IRawJsonFormatter
{
    public void WriteTo(OperationResult result, IBufferWriter<byte> writer, bool indented = false)
    {
        var formatter = new RawJsonFormatter(this, writer, indented);
        formatter.Write(result);
    }

    internal ref struct RawJsonFormatter(
        ResultDocument document,
        IBufferWriter<byte> writer,
        bool indented)
    {
        private int _indentation = 0;

        public void Write(OperationResult result)
        {
            WriteByte(JsonConstants.OpenBrace);

            if (indented)
            {
                WriteNewLine();
                _indentation++;
            }

            if (!result.Errors.IsEmpty)
            {
                if (indented)
                {
                    WriteIndent();
                }

                WriteByte(JsonConstants.Quote);
                writer.Write(JsonConstants.Errors);
                WriteByte(JsonConstants.Quote);
                WriteByte(JsonConstants.Colon);

                if (indented)
                {
                    WriteByte(JsonConstants.Space);
                }

                var options = new JsonWriterOptions { Indented = indented };
                using var jsonWriter = new Utf8JsonWriter(writer, options);
                JsonValueFormatter.WriteErrors(
                    jsonWriter,
                    result.Errors,
                    new JsonSerializerOptions(JsonSerializerDefaults.Web),
                    default);
                jsonWriter.Flush();

                WriteByte(JsonConstants.Comma);
            }

            // Write "data":
            var root = Cursor.Zero;
            var row = document._metaDb.Get(root);

            if (indented)
            {
                if (!result.Errors.IsEmpty)
                {
                    WriteNewLine();
                }

                WriteIndent();
            }

            WriteByte(JsonConstants.Quote);
            writer.Write(JsonConstants.Data);
            WriteByte(JsonConstants.Quote);
            WriteByte(JsonConstants.Colon);

            if (indented)
            {
                WriteByte(JsonConstants.Space);
            }

            if (row.TokenType is ElementTokenType.Null
                || (ElementFlags.IsInvalidated & row.Flags) == ElementFlags.IsInvalidated)
            {
                writer.Write(JsonConstants.NullValue);
            }
            else
            {
                WriteObject(root, row);
            }

            if (result.Extensions?.Count > 0)
            {
                WriteByte(JsonConstants.Comma);

                if (indented)
                {
                    WriteNewLine();
                    WriteIndent();
                }

                WriteByte(JsonConstants.Quote);
                writer.Write(JsonConstants.Extensions);
                WriteByte(JsonConstants.Quote);
                WriteByte(JsonConstants.Colon);

                if (indented)
                {
                    WriteByte(JsonConstants.Space);
                }

                var options = new JsonWriterOptions { Indented = indented };
                using var jsonWriter = new Utf8JsonWriter(writer, options);
                JsonValueFormatter.WriteDictionary(
                    jsonWriter,
                    result.Extensions,
                    new JsonSerializerOptions(JsonSerializerDefaults.Web),
                    default);
                jsonWriter.Flush();
            }

            if (indented)
            {
                _indentation--;
                WriteNewLine();
                WriteIndent();
            }

            WriteByte(JsonConstants.CloseBrace);
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
                    when (ElementFlags.IsObject & row.Flags) != ElementFlags.IsObject:
                    WriteObject(cursor, row);
                    break;

                case ElementTokenType.StartArray
                    when (ElementFlags.IsList & row.Flags) != ElementFlags.IsList:
                    WriteArray(cursor, row);
                    break;

                case ElementTokenType.None:
                case ElementTokenType.Null:
                    writer.Write(JsonConstants.NullValue);
                    break;

                case ElementTokenType.True:
                    writer.Write(JsonConstants.TrueValue);
                    break;

                case ElementTokenType.False:
                    writer.Write(JsonConstants.FalseValue);
                    break;

                default:
                    document.WriteRawValueTo(writer, row, _indentation, indented);
                    break;
            }
        }

        private void WriteObject(Cursor start, DbRow startRow)
        {
            Debug.Assert(startRow.TokenType is ElementTokenType.StartObject);

            var current = start + 1;
            var end = start + startRow.NumberOfRows;

            WriteByte(JsonConstants.OpenBrace);

            if (indented && current < end)
            {
                _indentation++;
            }

            var first = true;
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

                if (!first)
                {
                    WriteByte(JsonConstants.Comma);
                }
                first = false;

                if (indented)
                {
                    WriteNewLine();
                    WriteIndent();
                }

                // property name (quoted)
                WriteByte(JsonConstants.Quote);
                writer.Write(document.ReadRawValue(row));
                WriteByte(JsonConstants.Quote);
                WriteByte(JsonConstants.Colon);

                if (indented)
                {
                    WriteByte(JsonConstants.Space);
                }

                // property value
                current++;
                row = document._metaDb.Get(current);
                WriteValue(current, row);

                // next property (move past value)
                current++;
            }

            if (indented && !first)
            {
                _indentation--;
                WriteNewLine();
                WriteIndent();
            }

            WriteByte(JsonConstants.CloseBrace);
        }

        private void WriteArray(Cursor start, DbRow startRow)
        {
            Debug.Assert(startRow.TokenType is ElementTokenType.StartArray);

            var current = start + 1;
            var end = start + startRow.NumberOfRows;

            WriteByte(JsonConstants.OpenBracket);

            if (indented && current < end)
            {
                _indentation++;
            }

            var first = true;
            while (current < end)
            {
                if (!first)
                {
                    WriteByte(JsonConstants.Comma);
                }
                first = false;

                if (indented)
                {
                    WriteNewLine();
                    WriteIndent();
                }

                var row = document._metaDb.Get(current);
                WriteValue(current, row);

                current++;
            }

            if (indented && end > start + 1)
            {
                _indentation--;
                WriteNewLine();
                WriteIndent();
            }

            WriteByte(JsonConstants.CloseBracket);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private readonly void WriteNewLine() => WriteByte(JsonConstants.NewLineLineFeed);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private readonly void WriteIndent()
        {
            var indentSize = _indentation * 2;
            if (indentSize > 0)
            {
                var span = writer.GetSpan(indentSize);
                span[..indentSize].Fill(JsonConstants.Space);
                writer.Advance(indentSize);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private readonly void WriteByte(byte value)
        {
            var span = writer.GetSpan(1);
            span[0] = value;
            writer.Advance(1);
        }
    }
}
