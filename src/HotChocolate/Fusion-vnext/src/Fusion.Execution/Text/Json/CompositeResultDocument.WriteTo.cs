using System.Buffers;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using HotChocolate.Execution;

namespace HotChocolate.Fusion.Text.Json;

public sealed partial class CompositeResultDocument : IRawJsonFormatter
{
    public void WriteTo(IBufferWriter<byte> writer, bool indented = false)
    {
        var formatter = new RawJsonFormatter(this, writer, indented);
        formatter.WriteValue(0);
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

        private int _indentLevel = 0;

        public void WriteValue(int index)
        {
            var row = document._metaDb.Get(index);
            WriteValue(index, row);
        }

        private void WriteValue(int index, DbRow row)
        {
            switch (row.TokenType)
            {
                case ElementTokenType.StartObject:
                    WriteObject(index, row);
                    break;

                case ElementTokenType.StartArray:
                    WriteArray(index, row);
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
