using System.Buffers;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text.Json;

namespace HotChocolate.Fusion.Text.Json;

public sealed partial class SourceResultDocument
{
    internal ref struct RawJsonFormatter(
        SourceResultDocument document,
        IBufferWriter<byte> writer,
        int indentLevel,
        bool indented)
    {
        private int _indentLevel = indentLevel;

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
                    writer.Write(JsonConstants.NullValue);
                    break;

                case JsonTokenType.True:
                    writer.Write(JsonConstants.TrueValue);
                    break;

                case JsonTokenType.False:
                    writer.Write(JsonConstants.FalseValue);
                    break;

                default:
                    document.WriteRawValueTo(writer, row);
                    break;
            }
        }

        private void WriteObject(Cursor start, DbRow startRow)
        {
            Debug.Assert(startRow.TokenType is JsonTokenType.StartObject);

            var current = start + 1;
            var end = start + startRow.NumberOfRows - 1;

            WriteByte(JsonConstants.OpenBrace);

            if (indented && current < end)
            {
                _indentLevel++;
            }

            var first = true;
            while (current < end)
            {
                var row = document._parsedData.Get(current);
                Debug.Assert(row.TokenType is JsonTokenType.PropertyName);

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

            if (indented && !first)
            {
                _indentLevel--;
                WriteNewLine();
                WriteIndent();
            }

            WriteByte(JsonConstants.CloseBrace);
        }

        private void WriteArray(Cursor start, DbRow startRow)
        {
            Debug.Assert(startRow.TokenType is JsonTokenType.StartArray);

            var current = start + 1;
            var end = start + startRow.NumberOfRows - 1;

            WriteByte(JsonConstants.OpenBracket);

            if (indented && current < end)
            {
                _indentLevel++;
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

            if (indented && end > start + 1)
            {
                _indentLevel--;
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
            var indentSize = _indentLevel * 2;
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
