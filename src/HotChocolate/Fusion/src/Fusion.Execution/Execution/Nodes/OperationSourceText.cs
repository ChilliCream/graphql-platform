using System.IO.Hashing;
using System.Security.Cryptography;
using System.Text;
using HotChocolate.Buffers;
using HotChocolate.Fusion.Transport.Serialization;
using HotChocolate.Language;
using HotChocolate.Text.Json;

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
/// Gets the UTF-8 encoded GraphQL operation document source text sent to the source schema.
/// </param>
/// <param name="Hash">
/// Gets the hash of the operation source text.
/// </param>
/// <param name="JsonLiteral">
/// Gets <see cref="Value"/> encoded as a JSON string value, including quotes, with the default transport options.
/// </param>
public readonly record struct OperationSourceText(
    string Name,
    OperationType Type,
    ReadOnlyMemory<byte> Value,
    OperationSourceTextHash Hash,
    ReadOnlyMemory<byte> JsonLiteral)
{
    /// <summary>
    /// Creates compact source text and its hash and JSON string literal from a syntax node.
    /// </summary>
    internal static OperationSourceText Create(string name, OperationType type, ISyntaxNode node)
    {
        var compact = node.ToString(indented: false);
        using var textBuffer = new PooledArrayWriter();
        var length = Encoding.UTF8.GetBytes(compact.AsSpan(), textBuffer.GetSpan(Encoding.UTF8.GetByteCount(compact)));
        textBuffer.Advance(length);

        using var literalBuffer = new PooledArrayWriter();
        var writer = new JsonWriter(literalBuffer, JsonOptionDefaults.WriterOptions);
        writer.WriteStringValue(textBuffer.WrittenSpan);
        var literal = literalBuffer.WrittenSpan.ToArray().AsMemory();
        ReadOnlyMemory<byte> value = literal.Length == length + 2
            ? literal.Slice(1, length)
            : textBuffer.WrittenSpan.ToArray();

        return new OperationSourceText(name, type, value, OperationSourceTextHash.Compute(value.Span), literal);
    }

    /// <summary>
    /// Creates source text with the supplied bytes and hash and encodes its JSON string literal.
    /// </summary>
    internal static OperationSourceText Create(
        string name,
        OperationType type,
        ReadOnlyMemory<byte> value,
        OperationSourceTextHash hash)
    {
        using var buffer = new PooledArrayWriter();
        var writer = new JsonWriter(buffer, JsonOptionDefaults.WriterOptions);
        writer.WriteStringValue(value.Span);

        var literal = buffer.WrittenSpan.ToArray().AsMemory();

        if (literal.Length == value.Length + 2)
        {
            value = literal.Slice(1, value.Length);
        }

        return new OperationSourceText(name, type, value, hash, literal);
    }
}

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
