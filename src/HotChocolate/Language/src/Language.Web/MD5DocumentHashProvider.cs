#if NETSTANDARD2_0
using System.Security.Cryptography;
#else
using System.Buffers;
using System.Runtime.CompilerServices;
#endif

namespace HotChocolate.Language;

public sealed class MD5DocumentHashProvider : DocumentHashProviderBase
{
#if NETSTANDARD2_0
    private readonly ThreadLocal<MD5> _md5 = new(MD5.Create);

#endif
    public MD5DocumentHashProvider()
        : this(HashFormat.Base64) { }

    public MD5DocumentHashProvider(HashFormat format)
        : base(format) { }

    public override string Name => "md5Hash";

    public override string AlgorithmName => "md5";

#if NETSTANDARD2_0
    protected override byte[] ComputeHash(byte[] document, int length)
    {
        return _md5.Value!.ComputeHash(document, 0, length);
    }
#else
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected override string ComputeHash(ReadOnlySpan<byte> document, HashFormat format)
    {
        Span<byte> hashBytes = stackalloc byte[Md5Digest.SizeInBytes];
        Md5Digest.Compute(document, hashBytes);
        return FormatHash(hashBytes, format);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected override string ComputeHash(ReadOnlySequence<byte> document, HashFormat format)
    {
        Span<byte> hashBytes = stackalloc byte[Md5Digest.SizeInBytes];
        Md5Digest.Compute(document, hashBytes);
        return FormatHash(hashBytes, format);
    }
#endif
}
