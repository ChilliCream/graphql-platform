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
/// Gets the UTF-8 encoded GraphQL operation document source text.
/// </param>
/// <param name="Hash">
/// Gets the hash of the operation source text.
/// </param>
public readonly record struct OperationSourceText(
    string Name,
    OperationType Type,
    ReadOnlyMemory<byte> Value,
    OperationSourceTextHash Hash)
{
    /// <summary>
    /// Gets <see cref="Value"/> encoded as the JSON string literal of a source schema request body,
    /// quotes included. It is empty when <see cref="Value"/> is empty.
    /// </summary>
    public ReadOnlyMemory<byte> JsonLiteral { get; } = EncodeJsonLiteral(Value.Span);

    /// <summary>
    /// Compares <see cref="Name"/>, <see cref="Type"/>, <see cref="Value"/> and <see cref="Hash"/>.
    /// </summary>
    public bool Equals(OperationSourceText other)
        => Name == other.Name
            && Type == other.Type
            && Value.Equals(other.Value)
            && Hash.Equals(other.Hash);

    /// <inheritdoc />
    public override int GetHashCode()
        => HashCode.Combine(Name, Type, Value, Hash);

    private bool PrintMembers(StringBuilder builder)
    {
        builder.Append("Name = ").Append(Name);
        builder.Append(", Type = ").Append(Type);
        builder.Append(", Value = ").Append(Value);
        builder.Append(", Hash = ").Append(Hash);
        return true;
    }

    private static ReadOnlyMemory<byte> EncodeJsonLiteral(ReadOnlySpan<byte> value)
    {
        if (value.IsEmpty)
        {
            return ReadOnlyMemory<byte>.Empty;
        }

        using var buffer = new PooledArrayWriter();
        var writer = new JsonWriter(buffer, JsonOptionDefaults.WriterOptions);
        writer.WriteStringValue(value);
        return buffer.WrittenSpan.ToArray();
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
