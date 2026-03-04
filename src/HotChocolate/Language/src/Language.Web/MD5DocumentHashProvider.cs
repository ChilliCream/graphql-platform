#if NET8_0_OR_GREATER
using System.Buffers;
using System.Runtime.CompilerServices;
#endif
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
            hashSpan = hashSpan[..written];
        }

        return FormatHash(hashSpan, format);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected override string ComputeHash(ReadOnlySequence<byte> document, HashFormat format)
    {
        using var incrementalHash = IncrementalHash.CreateHash(HashAlgorithmName.MD5);

        foreach (var segment in document)
        {
            incrementalHash.AppendData(segment.Span);
        }

        Span<byte> hashBytes = stackalloc byte[16];
        incrementalHash.TryGetHashAndReset(hashBytes, out var written);

        if (written < 16)
        {
            hashBytes = hashBytes[..written];
        }

        return FormatHash(hashBytes, format);
    }
#endif
}
