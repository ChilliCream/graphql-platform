using System.IO.Hashing;
using System.Security.Cryptography;
using HotChocolate.Language;

namespace HotChocolate.Fusion.Execution.Nodes;

/// <summary>
/// Represents a GraphQL operation definition source text that needs parsing before it can be executed.
/// </summary>
/// <param name="Name">
/// Gets the operation name.
/// </param>
/// <param name="Type">
/// Gets the type of the operation.
/// </param>
/// <param name="Value">
/// Gets the UTF-8 encoded GraphQL operation document source text.
/// </param>
/// <param name="Hash">
/// Gets the hash of the operation source text.
/// </param>
public readonly record struct OperationSourceText(
    string Name,
    OperationType Type,
    ReadOnlyMemory<byte> Value,
    OperationSourceTextHash Hash);

/// <summary>
/// Represents the hash of a GraphQL operation source text.
/// </summary>
/// <param name="Sha256">
/// Gets the SHA-256 hash of the operation source text as a lowercase hex string.
/// </param>
/// <param name="Sha256Short">
/// Gets the first eight characters of <see cref="Sha256"/>.
/// </param>
/// <param name="Xxx">
/// Gets the XxHash64 of the operation source text.
/// </param>
public readonly record struct OperationSourceTextHash(
    string Sha256,
    string Sha256Short,
    ulong Xxx)
{
    /// <summary>
    /// Computes the hash of an operation source text.
    /// </summary>
    /// <param name="value">
    /// The UTF-8 encoded GraphQL operation document source text.
    /// </param>
    public static OperationSourceTextHash Compute(ReadOnlySpan<byte> value)
    {
        Span<byte> sha256Bytes = stackalloc byte[SHA256.HashSizeInBytes];
        SHA256.HashData(value, sha256Bytes);
#if NET9_0_OR_GREATER
        var sha256 = Convert.ToHexStringLower(sha256Bytes);
#else
        var sha256 = Convert.ToHexString(sha256Bytes).ToLowerInvariant();
#endif

        return new OperationSourceTextHash(sha256, sha256[..8], XxHash64.HashToUInt64(value));
    }

    /// <summary>
    /// Creates the hash from renderings that were computed before.
    /// </summary>
    /// <param name="sha256">
    /// The SHA-256 hash of the operation source text as a lowercase hex string.
    /// </param>
    /// <param name="xxx">
    /// The XxHash64 of the same operation source text.
    /// </param>
    public static OperationSourceTextHash From(string sha256, ulong xxx)
        => new(sha256, sha256[..8], xxx);
}
