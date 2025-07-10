#nullable enable

using System.Buffers;
using System.Buffers.Text;
using System.Text;
using HotChocolate.Properties;
using static System.Buffers.Text.Utf8Parser;

namespace HotChocolate.Types.Relay;

internal sealed class LegacyNodeIdSerializer : INodeIdSerializer
{
    private const int StackallocThreshold = 256;
    private const byte Separator = (byte)'\n';
    internal const byte Guid = (byte)'g';
    internal const byte Short = (byte)'s';
    internal const byte Int = (byte)'i';
    internal const byte Long = (byte)'l';
    internal const byte Default = (byte)'d';

    private static readonly Encoding s_utf8 = Encoding.UTF8;

    public string Format(string typeName, object internalId)
        => FormatInternal(typeName, internalId);

    internal static string FormatInternal(string typeName, object internalId)
    {
        ArgumentException.ThrowIfNullOrEmpty(typeName);
        ArgumentNullException.ThrowIfNull(internalId);

        string? idString = null;

        switch (internalId)
        {
            case System.Guid:
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

        var serialized = serializedSize <= StackallocThreshold
            ? stackalloc byte[serializedSize]
            : serializedArray = ArrayPool<byte>.Shared.Rent(serializedSize);

        try
        {
            var position = 0;

            position += CopyString(typeName, serialized.Slice(position, nameSize));
            serialized[position++] = Separator;

            var value = serialized[(position + 1)..];

            int bytesWritten;
            switch (internalId)
            {
                case Guid g:
                    serialized[position++] = Guid;
                    Utf8Formatter.TryFormat(g, value, out bytesWritten, 'N');
                    position += idSize;
                    break;

                case short s:
                    serialized[position++] = Short;
                    Utf8Formatter.TryFormat(s, value, out bytesWritten);
                    position += bytesWritten;
                    break;

                case int i:
                    serialized[position++] = Int;
                    Utf8Formatter.TryFormat(i, value, out bytesWritten);
                    position += bytesWritten;
                    break;

                case long l:
                    serialized[position++] = Long;
                    Utf8Formatter.TryFormat(l, value, out bytesWritten);
                    position += bytesWritten;
                    break;

                default:
                    serialized[position++] = Default;
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

            serialized = serialized[..bytesWritten];

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

    public NodeId Parse(string formattedId, INodeIdRuntimeTypeLookup runtimeTypeLookup)
    {
        // the older implementation had no way to convert ...
        // so we just call the standard parse.
        return Parse(formattedId);
    }

    public static byte GetLegacyValueCode(object value)
    {
        switch (value)
        {
            case System.Guid:
                return Guid;

            case short:
                return Short;

            case int:
                return Int;

            case long:
                return Long;

            default:
                return Default;
        }
    }

    private static NodeId Parse(string formattedId)
    {
        ArgumentNullException.ThrowIfNull(formattedId);

        var serializedSize = GetAllocationSize(formattedId);

        byte[]? serializedArray = null;

        var serialized = serializedSize <= StackallocThreshold
            ? stackalloc byte[serializedSize]
            : serializedArray = ArrayPool<byte>.Shared.Rent(serializedSize);

        try
        {
            var bytesWritten = CopyString(formattedId, serialized);
            serialized = serialized[..bytesWritten];

            var operationStatus = Base64.DecodeFromUtf8InPlace(serialized, out bytesWritten);

            if (operationStatus != OperationStatus.Done)
            {
                throw new IdSerializationException(
                    TypeResources.IdSerializer_UnableToDecode,
                    operationStatus,
                    formattedId);
            }

            var decoded = serialized[..bytesWritten];
            var nextSeparator = NextSeparator(decoded);
            var typeName = CreateString(decoded[..nextSeparator]);
            decoded = decoded[(nextSeparator + 1)..];

            var value = ParseValueInternal(decoded);
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

    private static object ParseValueInternal(ReadOnlySpan<byte> formattedId)
    {
        object value;

        switch (formattedId[0])
        {
            case Guid:
                TryParse(formattedId[1..], out Guid g, out _, 'N');
                value = g;
                break;
            case Short:
                TryParse(formattedId[1..], out short s, out _);
                value = s;
                break;
            case Int:
                TryParse(formattedId[1..], out int i, out _);
                value = i;
                break;
            case Long:
                TryParse(formattedId[1..], out long l, out _);
                value = l;
                break;
            default:
                value = CreateString(formattedId[1..]);
                break;
        }

        return value;
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
                return s_utf8.GetBytes(
                    charPtr, value.Length,
                    bytePtr, serialized.Length);
            }
        }
    }

    private static unsafe string CreateString(ReadOnlySpan<byte> serialized)
    {
        if (serialized.Length == 0)
        {
            return "";
        }

        fixed (byte* bytePtr = serialized)
        {
            return s_utf8.GetString(bytePtr, serialized.Length);
        }
    }

    private static int GetAllocationSize<T>(in T value)
    {
        return value switch
        {
            System.Guid => 32,
            short => 6,
            int => 11,
            long => 20,
            string s => s_utf8.GetByteCount(s),
            _ => throw new NotSupportedException()
        };
    }

    private static int NextSeparator(ReadOnlySpan<byte> serializedId)
    {
        for (var i = 0; i < serializedId.Length; i++)
        {
            if (serializedId[i] == Separator)
            {
                return i;
            }
        }

        throw new InvalidOperationException("Invalid string sequence.");
    }
}
