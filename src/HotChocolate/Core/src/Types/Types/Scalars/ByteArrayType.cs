using System.Buffers;
using System.Buffers.Text;
using System.Text.Json;
using HotChocolate.Buffers;
using HotChocolate.Features;
using HotChocolate.Language;
using HotChocolate.Text.Json;

namespace HotChocolate.Types;

/// <summary>
/// Represents a scalar type for byte arrays that are serialized as Base64-encoded strings in GraphQL.
/// This type handles the conversion between byte arrays in .NET and string representations in GraphQL schemas.
/// </summary>
[Obsolete("Use Base64StringType instead.")]
public class ByteArrayType : ScalarType<byte[], StringValueNode>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ByteArrayType"/> class.
    /// </summary>
    public ByteArrayType(
        string name,
        string? description = null,
        BindingBehavior bind = BindingBehavior.Explicit)
        : base(name, bind)
    {
        Description = description;
        Pattern = @"^(?:[A-Za-z0-9+\/]{4})*(?:[A-Za-z0-9+\/]{2}==|[A-Za-z0-9+\/]{3}=)?$";
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ByteArrayType"/> class.
    /// </summary>
    [ActivatorUtilitiesConstructor]
    public ByteArrayType()
        : this(ScalarNames.ByteArray, bind: BindingBehavior.Implicit)
    {
    }

    protected override byte[] OnCoerceInputLiteral(StringValueNode valueLiteral)
    {
        byte[]? rented = null;
        var valueSpan = valueLiteral.AsSpan();
        var length = Base64.GetMaxDecodedFromUtf8Length(valueSpan.Length);
        var buffer = length <= 256 ? stackalloc byte[length] : rented = ArrayPool<byte>.Shared.Rent(length);

        try
        {
            Base64.DecodeFromUtf8(valueSpan, buffer, out _, out var bytesWritten);
            return buffer[..bytesWritten].ToArray();
        }
        finally
        {
            if (rented is not null)
            {
                ArrayPool<byte>.Shared.Return(rented);
            }
        }
    }

    protected override byte[] OnCoerceInputValue(JsonElement inputValue, IFeatureProvider context)
        => inputValue.GetBytesFromBase64();

    protected override void OnCoerceOutputValue(byte[] runtimeValue, ResultElement resultValue)
    {
        byte[]? rented = null;
        var length = Base64.GetMaxEncodedToUtf8Length(runtimeValue.Length);
        var buffer = length <= 256 ? stackalloc byte[length] : rented = ArrayPool<byte>.Shared.Rent(length);

        try
        {
            Base64.EncodeToUtf8(
                runtimeValue,
                buffer,
                out _,
                out var bytesWritten);
            resultValue.SetStringValue(buffer[..bytesWritten]);
        }
        finally
        {
            if (rented is not null)
            {
                ArrayPool<byte>.Shared.Return(rented);
            }
        }
    }

    protected override StringValueNode OnValueToLiteral(byte[] runtimeValue)
    {
        byte[]? rented = null;
        var length = Base64.GetMaxEncodedToUtf8Length(runtimeValue.Length);
        var buffer = length <= 256 ? stackalloc byte[length] : rented = ArrayPool<byte>.Shared.Rent(length);

        try
        {
            Base64.EncodeToUtf8(
                runtimeValue,
                buffer,
                out _,
                out var bytesWritten);
            var encodedBuffer = buffer[..bytesWritten].ToArray();
            var segment = new ReadOnlyMemorySegment(encodedBuffer);
            return new StringValueNode(null, segment, false);
        }
        finally
        {
            if (rented is not null)
            {
                ArrayPool<byte>.Shared.Return(rented);
            }
        }
    }
}
