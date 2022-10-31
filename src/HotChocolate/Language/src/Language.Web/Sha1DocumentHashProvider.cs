using System;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Threading;

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

#if NETCOREAPP3_1_OR_GREATER
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected override string ComputeHash(ReadOnlySpan<byte> document, HashFormat format)
    {
        var hashBytes = new byte[20];
        var hashSpan = hashBytes.AsSpan();

        _sha.Value!.TryComputeHash(document, hashBytes, out var written);

        if (written < 20)
        {
            hashSpan = hashSpan.Slice(0, written);
        }

        return format switch
        {
            HashFormat.Base64 => Convert.ToBase64String(hashSpan),
            HashFormat.Hex => ToHexString(hashSpan),
            _ => throw new NotSupportedException("The specified hash format is not supported.")
        };
    }
#else
    protected override byte[] ComputeHash(byte[] document, int length)
    {
        return _sha.Value!.ComputeHash(document, 0, length);
    }
#endif
}
