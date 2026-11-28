using System.Buffers;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text.Encodings.Web;
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
        /// <summary>
        /// Indicates whether the encoder of the writer is known to leave ASCII letters, digits and
        /// underscores unescaped.
        /// </summary>
        private readonly bool _keepsPlainNames =
            writer.Options.Encoder is not { } encoder
                || ReferenceEquals(encoder, JavaScriptEncoder.UnsafeRelaxedJsonEscaping)
                || ReferenceEquals(encoder, JavaScriptEncoder.Default);

        public void Write()
        {
            var root = Cursor.CreateZero();
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
                    if (document.TryGetLocalData(row.Location, row.SizeOrLength, out var value))
                    {
                        WriteStringValue(value, row.Flags);
                    }
                    else
                    {
                        WriteValueAcrossChunks(row);
                    }

                    break;
                }

                case ElementTokenType.Number:
                {
                    if (document.TryGetLocalData(row.Location, row.SizeOrLength, out var value))
                    {
                        writer.WriteNumberValue(value);
                    }
                    else
                    {
                        WriteValueAcrossChunks(row);
                    }

                    break;
                }

                default:
                    throw new NotSupportedException();
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void WriteStringValue(ReadOnlySpan<byte> value, ElementFlags flags)
        {
            if ((ElementFlags.IsEncoded & flags) == ElementFlags.IsEncoded)
            {
                writer.WriteStringValue(value, skipEscaping: true);
            }
            else
            {
                writer.WriteStringValue(value[1..^1], skipEscaping: false);
            }
        }

        /// <summary>
        /// Writes a string or a number whose data spans chunk boundaries.
        /// </summary>
        [MethodImpl(MethodImplOptions.NoInlining)]
        private void WriteValueAcrossChunks(DbRow row)
        {
            var size = row.SizeOrLength;
            var buffer = ArrayPool<byte>.Shared.Rent(size);

            try
            {
                var value = buffer.AsSpan(0, size);
                document.CopyLocalData(row.Location, value);

                if (row.TokenType is ElementTokenType.String)
                {
                    WriteStringValue(value, row.Flags);
                }
                else
                {
                    writer.WriteNumberValue(value);
                }
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(buffer);
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
                if (row.OperationReferenceType is OperationReferenceType.Selection)
                {
                    var selection = document._operation.GetSelectionById(row.OperationReferenceId);

                    if (_keepsPlainNames && selection.HasPlainResponseName)
                    {
                        writer.WritePropertyNameUnescaped(selection.Utf8ResponseName);
                    }
                    else
                    {
                        writer.WritePropertyName(selection.Utf8ResponseName);
                    }
                }
                else
                {
                    writer.WritePropertyName(document.ReadLocalData(row.Location, row.SizeOrLength));
                }

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
