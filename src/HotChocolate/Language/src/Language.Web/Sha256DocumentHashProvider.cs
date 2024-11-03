using System.Runtime.CompilerServices;
using System.Security.Cryptography;

namespace HotChocolate.Language;

public sealed class Sha256DocumentHashProvider : DocumentHashProviderBase
{
    private readonly ThreadLocal<SHA256> _sha = new(SHA256.Create);

    public Sha256DocumentHashProvider()
       : this(HashFormat.Base64)
    {
    }

    public Sha256DocumentHashProvider(HashFormat format)
        : base(format)
    {
    }

    public override string Name => "sha256Hash";

#if NETSTANDARD2_0
    protected override byte[] ComputeHash(byte[] document, int length)
    {
        return _sha.Value!.ComputeHash(document, 0, length);
    }
#else
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected override string ComputeHash(ReadOnlySpan<byte> document, HashFormat format)
    {
        var hashBytes = new byte[32];
        var hashSpan = hashBytes.AsSpan();

        _sha.Value!.TryComputeHash(document, hashBytes, out var written);

        if (written < 32)
        {
            hashSpan = hashSpan[..written];
        }

        return FormatHash(hashSpan, format);
    }
#endif
}
