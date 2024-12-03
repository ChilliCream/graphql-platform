using System.Buffers;
using System.Buffers.Text;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using static HotChocolate.Language.Properties.LangWebResources;

namespace HotChocolate.Language;

public abstract class DocumentHashProviderBase : IDocumentHashProvider
{
    internal DocumentHashProviderBase(HashFormat format)
    {
        Format = format;
    }

    public abstract string Name { get; }

    public HashFormat Format { get; }

    public string ComputeHash(ReadOnlySpan<byte> document)
    {
#if NETSTANDARD2_0
        var rented = ArrayPool<byte>.Shared.Rent(document.Length);
        document.CopyTo(rented);

        try
        {
            var hash = ComputeHash(rented, document.Length);
            return FormatHash(hash, Format);
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(rented);
        }
#else
        return ComputeHash(document, Format);
#endif
    }

#if NETSTANDARD2_0
    protected abstract byte[] ComputeHash(byte[] document, int length);
#else
    protected abstract string ComputeHash(ReadOnlySpan<byte> document, HashFormat format);
#endif

    protected static string FormatHash(ReadOnlySpan<byte> hash, HashFormat format)
        => format switch
        {
            HashFormat.Base64 => ToBase64UrlSafeString(hash),
            HashFormat.Hex => ToHexString(hash),
            _ => throw new NotSupportedException(ComputeHash_FormatNotSupported),
        };

    protected static string ToHexString(ReadOnlySpan<byte> hash)
    {
        if (hash.Length == 0)
        {
            return string.Empty;
        }

        var hashString = new string('-', hash.Length * 2);
        ref var first = ref MemoryMarshal.GetReference(hashString.AsSpan());

        var i = 0;
        var j = 0;

        while (i < hash.Length)
        {
            var b = hash[i++];

            ref var element = ref Unsafe.Add(ref first, j++);
            element = ToCharLower(b >> 4);

            element = ref Unsafe.Add(ref first, j++);
            element = ToCharLower(b);
        }

        return hashString;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static char ToCharLower(int value)
    {
        value &= 0xF;
        value += '0';

        if (value > '9')
        {
            value += 'A' - ('9' + 1);
        }

        if (value is > 64 and < 91)
        {
            value |= 0x20;
        }

        return (char)value;
    }

#if NETSTANDARD2_0
    private static unsafe string ToBase64UrlSafeString(
#else
    private static string ToBase64UrlSafeString(
#endif
        ReadOnlySpan<byte> hash)
    {
        byte[]? rented = null;
        var initialSize = hash.Length * 3;
        var buffer = initialSize <= GraphQLConstants.StackallocThreshold
            ? stackalloc byte[initialSize]
            : rented = ArrayPool<byte>.Shared.Rent(initialSize);
        int written;

        while (Base64.EncodeToUtf8(hash, buffer, out _, out written) == OperationStatus.DestinationTooSmall)
        {
            var newMemory = ArrayPool<byte>.Shared.Rent(buffer.Length * 2);

            if (rented is not null)
            {
                ArrayPool<byte>.Shared.Return(rented);
            }
            rented = newMemory;
            buffer = rented;
        }

        for (var i = written - 1; i >= 0; i--)
        {
            switch (buffer[i])
            {
                case GraphQLConstants.Plus:
                    buffer[i] = GraphQLConstants.Minus;
                    break;

                case GraphQLConstants.ForwardSlash:
                    buffer[i] = GraphQLConstants.Underscore;
                    break;

                case GraphQLConstants.Equal:
                    written--;
                    break;
            }
        }

#if NETSTANDARD2_0
        string result;

        fixed (byte* bytePtr = buffer)
        {
            result = StringHelper.UTF8Encoding.GetString(bytePtr, written);
        }
#else
        var result = StringHelper.UTF8Encoding.GetString(buffer[..written]);
#endif

        if (rented is not null)
        {
            ArrayPool<byte>.Shared.Return(rented);
        }

        return result;
    }
}
