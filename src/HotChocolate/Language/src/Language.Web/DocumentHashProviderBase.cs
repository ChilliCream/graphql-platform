#if NET6_0_OR_GREATER
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
#else
using System.Buffers;
using static HotChocolate.Language.Properties.LangWebResources;
#endif

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
#if NET6_0_OR_GREATER
        return ComputeHash(document, Format);
#else
        var rented = ArrayPool<byte>.Shared.Rent(document.Length);
        document.CopyTo(rented);

        try
        {
            var hash = ComputeHash(rented, document.Length);

            switch (Format)
            {
                case HashFormat.Base64:
                    return Convert.ToBase64String(hash);
                case HashFormat.Hex:
                    return BitConverter.ToString(hash)
                        .ToLowerInvariant()
                        .Replace("-", string.Empty);
                default:
                    throw new NotSupportedException(ComputeHash_FormatNotSupported);
            }
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(rented);
        }
#endif
    }

#if NET6_0_OR_GREATER
    protected abstract string ComputeHash(ReadOnlySpan<byte> document, HashFormat format);

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
#else
    protected abstract byte[] ComputeHash(byte[] document, int length);
#endif
}
