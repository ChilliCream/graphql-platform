using System.Runtime.CompilerServices;
using System.Security.Cryptography;

namespace HotChocolate.Language;

public sealed class MD5DocumentHashProvider : DocumentHashProviderBase
{
    private readonly ThreadLocal<MD5> _md5 = new(MD5.Create);

    public MD5DocumentHashProvider()
        : this(HashFormat.Base64) { }

    public MD5DocumentHashProvider(HashFormat format)
        : base(format) { }

    public override string Name => "md5Hash";

#if NETSTANDARD2_0
    protected override byte[] ComputeHash(byte[] document, int length)
    {
        return _md5.Value!.ComputeHash(document, 0, length);
    }
#else
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected override string ComputeHash(ReadOnlySpan<byte> document, HashFormat format)
    {
        var hashBytes = new byte[16];
        var hashSpan = hashBytes.AsSpan();

        _md5.Value!.TryComputeHash(document, hashBytes, out var written);

        if (written < 16)
        {
            hashSpan = hashSpan.Slice(0, written);
        }

        return FormatHash(hashSpan, format);
    }
#endif
}
