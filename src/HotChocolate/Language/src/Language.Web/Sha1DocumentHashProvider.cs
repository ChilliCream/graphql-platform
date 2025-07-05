using System.Runtime.CompilerServices;
using System.Security.Cryptography;

namespace HotChocolate.Language;

public sealed class Sha1DocumentHashProvider : DocumentHashProviderBase
{
    private readonly ThreadLocal<SHA1> _sha = new(SHA1.Create);

    public Sha1DocumentHashProvider()
        : this(HashFormat.Base64)
    {
    }

    public Sha1DocumentHashProvider(HashFormat format)
        : base(format)
    {
    }

    public override string Name => "sha1Hash";

#if NETSTANDARD2_0
    protected override byte[] ComputeHash(byte[] document, int length)
        => _sha.Value!.ComputeHash(document, 0, length);
#else
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected override string ComputeHash(ReadOnlySpan<byte> document, HashFormat format)
    {
        var hashBytes = new byte[20];
        var hashSpan = hashBytes.AsSpan();

        _sha.Value!.TryComputeHash(document, hashBytes, out var written);

        if (written < 20)
        {
            hashSpan = hashSpan[..written];
        }

        return FormatHash(hashSpan, format);
    }
#endif
}
