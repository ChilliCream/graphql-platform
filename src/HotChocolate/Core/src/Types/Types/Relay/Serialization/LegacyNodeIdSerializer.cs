#nullable enable

using System;
using System.Buffers;
using System.Buffers.Text;
using System.Text;
using HotChocolate.Properties;
using static System.Buffers.Text.Utf8Parser;

namespace HotChocolate.Types.Relay;

internal sealed class LegacyNodeIdSerializer : INodeIdSerializer
{
    private const int _stackallocThreshold = 256;
    private const byte _separator = (byte)'\n';
    private const byte _guid = (byte)'g';
    private const byte _short = (byte)'s';
    private const byte _int = (byte)'i';
    private const byte _long = (byte)'l';
    private const byte _default = (byte)'d';

    private static readonly Encoding _utf8 = Encoding.UTF8;

    public string Format(string typeName, object internalId)
    {
        if (string.IsNullOrEmpty(typeName))
        {
            throw new ArgumentNullException(nameof(typeName));
        }

        if (internalId is null)
        {
            throw new ArgumentNullException(nameof(internalId));
        }


        string? idString = null;

        switch (internalId)
        {
            case Guid:
            case short:
            case int:
            case long:
                break;

            case string s:
                idString = s;
                break;

            default:
                idString = internalId.ToString()!;
                break;
        }

        var nameSize = GetAllocationSize(typeName);

        var idSize = idString is null
            ? GetAllocationSize(in internalId)
            : GetAllocationSize(in idString);

        var serializedSize = ((nameSize + idSize + 16) / 3) * 4;

        byte[]? serializedArray = null;

        var serialized = serializedSize <= _stackallocThreshold
            ? stackalloc byte[serializedSize]
            : serializedArray = ArrayPool<byte>.Shared.Rent(serializedSize);

        try
        {
            var position = 0;

            position += CopyString(typeName, serialized.Slice(position, nameSize));
            serialized[position++] = _separator;

            var value = serialized.Slice(position + 1);

            int bytesWritten;
            switch (internalId)
            {
                case Guid g:
                    serialized[position++] = _guid;
                    Utf8Formatter.TryFormat(g, value, out bytesWritten, 'N');
                    position += idSize;
                    break;

                case short s:
                    serialized[position++] = _short;
                    Utf8Formatter.TryFormat(s, value, out bytesWritten);
                    position += bytesWritten;
                    break;

                case int i:
                    serialized[position++] = _int;
                    Utf8Formatter.TryFormat(i, value, out bytesWritten);
                    position += bytesWritten;
                    break;

                case long l:
                    serialized[position++] = _long;
                    Utf8Formatter.TryFormat(l, value, out bytesWritten);
                    position += bytesWritten;
                    break;

                default:
                    serialized[position++] = _default;
                    position += CopyString(idString!, value);
                    break;
            }

            var operationStatus = Base64.EncodeToUtf8InPlace(serialized, position, out bytesWritten);

            if (operationStatus != OperationStatus.Done)
            {
                throw new IdSerializationException(
                    TypeResources.IdSerializer_UnableToEncode,
                    operationStatus,
                    idString);
            }

            serialized = serialized.Slice(0, bytesWritten);

            return CreateString(serialized);
        }
        finally
        {
            if (serializedArray != null)
            {
                serialized.Clear();
                ArrayPool<byte>.Shared.Return(serializedArray);
            }
        }
    }

    public NodeId Parse(string formattedId)
    {
        if (formattedId is null)
        {
            throw new ArgumentNullException(nameof(formattedId));
        }

        var serializedSize = GetAllocationSize(formattedId);

        byte[]? serializedArray = null;

        var serialized = serializedSize <= _stackallocThreshold
            ? stackalloc byte[serializedSize]
            : serializedArray = ArrayPool<byte>.Shared.Rent(serializedSize);

        try
        {
            var bytesWritten = CopyString(formattedId, serialized);
            serialized = serialized.Slice(0, bytesWritten);

            var operationStatus = Base64.DecodeFromUtf8InPlace(serialized, out bytesWritten);

            if (operationStatus != OperationStatus.Done)
            {
                throw new IdSerializationException(
                    TypeResources.IdSerializer_UnableToDecode,
                    operationStatus,
                    formattedId);
            }

            var decoded = serialized.Slice(0, bytesWritten);
            var nextSeparator = NextSeparator(decoded);
            var typeName = CreateString(decoded.Slice(0, nextSeparator));
            decoded = decoded.Slice(nextSeparator + 1);

            object value;

            switch (decoded[0])
            {
                case _guid:
                    TryParse(decoded.Slice(1), out Guid g, out _, 'N');
                    value = g;
                    break;
                case _short:
                    TryParse(decoded.Slice(1), out short s, out _);
                    value = s;
                    break;
                case _int:
                    TryParse(decoded.Slice(1), out int i, out _);
                    value = i;
                    break;
                case _long:
                    TryParse(decoded.Slice(1), out long l, out _);
                    value = l;
                    break;
                default:
                    value = CreateString(decoded.Slice(1));
                    break;
            }

            return new NodeId(typeName, value);
        }
        finally
        {
            if (serializedArray != null)
            {
                serialized.Clear();
                ArrayPool<byte>.Shared.Return(serializedArray);
            }
        }
    }

    public NodeId Parse(string formattedId, Type runtimeType)
    {
       // the older implementation had no way to convert ...
       // so we just call the standard parse.
       return Parse(formattedId);
    }

    private static unsafe int CopyString(string value, Span<byte> serialized)
    {
        fixed (byte* bytePtr = serialized)
        {
            fixed (char* charPtr = value)
            {
                return _utf8.GetBytes(
                    charPtr, value.Length,
                    bytePtr, serialized.Length);
            }
        }
    }

    private static unsafe string CreateString(Span<byte> serialized)
    {
        fixed (byte* bytePtr = serialized)
        {
            return _utf8.GetString(bytePtr, serialized.Length);
        }
    }

    private static int GetAllocationSize<T>(in T value)
    {
        return value switch
        {
            Guid => 32,
            short => 6,
            int => 11,
            long => 20,
            string s => _utf8.GetByteCount(s),
            _ => throw new NotSupportedException(),
        };
    }

    private static int NextSeparator(ReadOnlySpan<byte> serializedId)
    {
        for (var i = 0; i < serializedId.Length; i++)
        {
            if (serializedId[i] == _separator)
            {
                return i;
            }
        }

        throw new InvalidOperationException("Invalid string sequence.");
    }
}
