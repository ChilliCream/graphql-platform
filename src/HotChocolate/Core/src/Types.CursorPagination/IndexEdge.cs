using System;
using System.Buffers;
using System.Buffers.Text;
using System.Text;
using HotChocolate.Execution;

#nullable enable

namespace HotChocolate.Types.Pagination
{
    public sealed class IndexEdge<T> : Edge<T>
    {
        private static readonly Encoding _utf8 = Encoding.UTF8;

        private IndexEdge(T node, string cursor, int index)
            : base(node, cursor)
        {
            Index = index;
        }

        public int Index { get; }

        public static IndexEdge<T> Create(T node, int index)
        {
            Span<byte> buffer = stackalloc byte[27 / 3 * 4];
            Utf8Formatter.TryFormat(index, buffer, out var written);
            Base64.EncodeToUtf8InPlace(buffer, written, out written);
            string cursor = CreateString(buffer.Slice(0, written));
            return new IndexEdge<T>(node, cursor, index);
        }

        private static unsafe string CreateString(Span<byte> buffer)
        {
            fixed (byte* bytePtr = buffer)
            {
                return _utf8.GetString(bytePtr, buffer.Length);
            }
        }

        public static unsafe int DeserializeCursor(string cursor)
        {
            fixed (char* cPtr = cursor)
            {
                var count = _utf8.GetByteCount(cPtr, cursor.Length);
                byte[]? rented = null;

                Span<byte> buffer = count <= 128
                    ? stackalloc byte[count]
                    : rented = ArrayPool<byte>.Shared.Rent(count);

                try
                {
                    fixed (byte* bytePtr = buffer)
                    {
                        _utf8.GetBytes(cPtr, cursor.Length, bytePtr, buffer.Length);
                    }

                    Base64.DecodeFromUtf8InPlace(buffer, out var written);
                    if (Utf8Parser.TryParse(buffer.Slice(0, written), out int index, out _))
                    {
                        return index;
                    }

                    throw new QueryException("The cursor has an invalid format.");
                }
                finally
                {
                    if (rented is { })
                    {
                        ArrayPool<byte>.Shared.Return(rented);
                    }
                }
            }
        }
    }
}
