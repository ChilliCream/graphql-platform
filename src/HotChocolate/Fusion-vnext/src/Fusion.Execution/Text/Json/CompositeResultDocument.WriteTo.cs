using System.Buffers;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Xml.Linq;
using HotChocolate.Execution;

namespace HotChocolate.Fusion.Text.Json;

public sealed partial class CompositeResultDocument : IRawJsonFormatter
{
    public void WriteTo(IBufferWriter<byte> writer, bool indented = false)
    {
        var formatter = new RawJsonFormatter(this, writer, indented);
        formatter.Write();
    }

    private ref struct RawJsonFormatter(CompositeResultDocument document, IBufferWriter<byte> writer, bool indented)
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

        private ReadOnlySpan<byte> Data => "data"u8;
        private ReadOnlySpan<byte> Errors => "errors"u8;
        private ReadOnlySpan<byte> Extensions => "extensions"u8;
        private ReadOnlySpan<byte> True => "true"u8;
        private ReadOnlySpan<byte> False => "false"u8;
        private ReadOnlySpan<byte> Null => "null"u8;

        private int _indentLevel = 0;

        public void Write()
        {
            WriteByte(StartObject);

            if (indented)
            {
                _indentLevel++;
            }

            if (document._errors?.Count > 0)
            {
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

            var row = document._metaDb.Get(0);

            // Write data property name with quotes
            WriteByte(Quote);
            writer.Write(Data);
            WriteByte(Quote);
            WriteByte(Colon);

            if (indented)
            {
                WriteByte(Space);
            }

            if ((ElementFlags.Invalidated & row.Flags) == ElementFlags.Invalidated)
            {
                writer.Write(Null);
            }
            else
            {
                WriteObject(0, row);
            }

            if (document._extensions?.Count > 0)
            {
                WriteByte(Comma);

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

        private void WriteValue(int index, DbRow row)
        {
            var tokenType = row.TokenType;

            // if the row is a reference we resolve the reference in place
            if (tokenType is ElementTokenType.Reference)
            {
                index = row.Location;
                row = document._metaDb.Get(index);
                tokenType = row.TokenType;
            }

            Debug.Assert(tokenType is not ElementTokenType.Reference);
            Debug.Assert(tokenType is not ElementTokenType.EndObject);
            Debug.Assert(tokenType is not ElementTokenType.EndArray);

            switch (tokenType)
            {
                case ElementTokenType.StartObject:
                    WriteObject(index, row);
                    break;

                case ElementTokenType.StartArray:
                    WriteArray(index, row);
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

        private void WriteObject(int index, DbRow row)
        {
            Debug.Assert(row.TokenType is ElementTokenType.StartObject);

            var currentIndex = index + 1;
            var endIndex = index + row.NumberOfRows;

            WriteByte(StartObject);

            if (indented && currentIndex < endIndex)
            {
                _indentLevel++;
            }

            var first = true;
            while (currentIndex < endIndex)
            {
                row = document._metaDb.Get(currentIndex);
                Debug.Assert(row.TokenType is ElementTokenType.PropertyName);

                if ((ElementFlags.IsInternal & row.Flags) == ElementFlags.IsInternal)
                {
                    currentIndex += 2;
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

                // Write property name with quotes
                WriteByte(Quote);
                writer.Write(document.ReadRawValue(row));
                WriteByte(Quote);
                WriteByte(Colon);

                if (indented)
                {
                    WriteByte(Space);
                }

                // Write property value
                currentIndex++;
                row = document._metaDb.Get(currentIndex);
                WriteValue(currentIndex, row);

                // Skip to next property
                currentIndex++;
            }

            if (indented && !first)
            {
                _indentLevel--;
                WriteNewLine();
                WriteIndent();
            }

            WriteByte(EndObject);
        }

        private void WriteArray(int index, DbRow row)
        {
            Debug.Assert(row.TokenType is ElementTokenType.StartArray);

            var currentIndex = index + 1;
            var endIndex = index + row.NumberOfRows;

            WriteByte(StartArray);

            if (indented && currentIndex < endIndex)
            {
                _indentLevel++;
            }

            var first = true;
            while (currentIndex < endIndex)
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

                row = document._metaDb.Get(currentIndex);
                WriteValue(currentIndex, row);

                currentIndex++;
            }

            if (indented && endIndex > index + 1)
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
