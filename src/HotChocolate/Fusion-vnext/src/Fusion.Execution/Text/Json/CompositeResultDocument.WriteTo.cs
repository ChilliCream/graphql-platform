using System.Buffers;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text.Json;
using HotChocolate.Execution;

namespace HotChocolate.Fusion.Text.Json;

public sealed partial class CompositeResultDocument : IRawJsonFormatter
{
    public void WriteTo(IBufferWriter<byte> writer, bool indented = false)
    {
        var formatter = new RawJsonFormatter(this, writer, indented);
        formatter.Write();
    }

    internal ref struct RawJsonFormatter(CompositeResultDocument document, IBufferWriter<byte> writer, bool indented)
    {
        private const byte StartObject = (byte)'{';
        private const byte EndObject = (byte)'}';
        private const byte StartArray = (byte)'[';
        private const byte EndArray = (byte)']';
        private const byte Quote = (byte)'"';
        private const byte Colon = (byte)':';
        private const byte Comma = (byte)',';
        private const byte Space = (byte)' ';
        private const byte NewLine = (byte)'\n';

        private static ReadOnlySpan<byte> Data => "data"u8;
        private static ReadOnlySpan<byte> Errors => "errors"u8;
        private static ReadOnlySpan<byte> Extensions => "extensions"u8;
        private static ReadOnlySpan<byte> True => "true"u8;
        private static ReadOnlySpan<byte> False => "false"u8;
        private static ReadOnlySpan<byte> Null => "null"u8;

        private int _indentLevel = 0;

        public void Write()
        {
            WriteByte(StartObject);

            if (indented)
            {
                WriteNewLine();
                _indentLevel++;
            }

            if (document._errors?.Count > 0)
            {
                if (indented)
                {
                    WriteIndent();
                }

                WriteByte(Quote);
                writer.Write(Errors);
                WriteByte(Quote);
                WriteByte(Colon);

                if (indented)
                {
                    WriteByte(Space);
                }

                var options = new JsonWriterOptions { Indented = indented };
                using var jsonWriter = new Utf8JsonWriter(writer, options);
                JsonValueFormatter.WriteErrors(jsonWriter, document._errors, new JsonSerializerOptions(JsonSerializerDefaults.Web), default);
                jsonWriter.Flush();

                WriteByte(Comma);
            }

            // Write "data":
            var root = Cursor.Zero;
            var row = document._metaDb.Get(root);

            if (indented)
            {
                WriteIndent();
            }

            WriteByte(Quote);
            writer.Write(Data);
            WriteByte(Quote);
            WriteByte(Colon);

            if (indented)
            {
                WriteByte(Space);
            }

            if (row.TokenType is ElementTokenType.Null
                || (ElementFlags.Invalidated & row.Flags) == ElementFlags.Invalidated)
            {
                writer.Write(Null);
            }
            else
            {
                WriteObject(root, row);
            }

            if (document._extensions?.Count > 0)
            {
                WriteByte(Comma);

                if (indented)
                {
                    WriteIndent();
                }

                WriteByte(Quote);
                writer.Write(Extensions);
                WriteByte(Quote);
                WriteByte(Colon);

                if (indented)
                {
                    WriteByte(Space);
                }

                var options = new JsonWriterOptions { Indented = indented };
                using var jsonWriter = new Utf8JsonWriter(writer, options);
                JsonValueFormatter.WriteDictionary(jsonWriter, document._extensions, new JsonSerializerOptions(JsonSerializerDefaults.Web), default);
                jsonWriter.Flush();
            }

            if (indented)
            {
                _indentLevel--;
                WriteNewLine();
                WriteIndent();
            }

            WriteByte(EndObject);
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
                    writer.Write(Null);
                    break;

                case ElementTokenType.True:
                    writer.Write(True);
                    break;

                case ElementTokenType.False:
                    writer.Write(False);
                    break;

                default:
                    writer.Write(document.ReadRawValue(row));
                    break;
            }
        }

        private void WriteObject(Cursor start, DbRow startRow)
        {
            Debug.Assert(startRow.TokenType is ElementTokenType.StartObject);

            var current = start + 1;
            var end = start + startRow.NumberOfRows;

            WriteByte(StartObject);

            if (indented && current < end)
            {
                _indentLevel++;
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
                    WriteByte(Comma);
                }
                first = false;

                if (indented)
                {
                    WriteNewLine();
                    WriteIndent();
                }

                // property name (quoted)
                WriteByte(Quote);
                writer.Write(document.ReadRawValue(row));
                WriteByte(Quote);
                WriteByte(Colon);

                if (indented)
                {
                    WriteByte(Space);
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
                _indentLevel--;
                WriteNewLine();
                WriteIndent();
            }

            WriteByte(EndObject);
        }

        private void WriteArray(Cursor start, DbRow startRow)
        {
            Debug.Assert(startRow.TokenType is ElementTokenType.StartArray);

            var current = start + 1;
            var end = start + startRow.NumberOfRows;

            WriteByte(StartArray);

            if (indented && current < end)
            {
                _indentLevel++;
            }

            var first = true;
            while (current < end)
            {
                if (!first)
                {
                    WriteByte(Comma);
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
                _indentLevel--;
                WriteNewLine();
                WriteIndent();
            }

            WriteByte(EndArray);
        }

        private void WriteNewLine() => WriteByte(NewLine);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void WriteIndent()
        {
            var indentSize = _indentLevel * 2;
            if (indentSize > 0)
            {
                var span = writer.GetSpan(indentSize);
                span[..indentSize].Fill(Space);
                writer.Advance(indentSize);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void WriteByte(byte value)
        {
            var span = writer.GetSpan(1);
            span[0] = value;
            writer.Advance(1);
        }
    }
}
