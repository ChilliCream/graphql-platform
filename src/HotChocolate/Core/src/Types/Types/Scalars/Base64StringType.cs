using System.Buffers;
using System.Buffers.Text;
using System.Text.Json;
using HotChocolate.Buffers;
using HotChocolate.Features;
using HotChocolate.Language;
using HotChocolate.Properties;
using HotChocolate.Text.Json;

namespace HotChocolate.Types;

/// <summary>
/// The <c>Base64String</c> scalar type represents an array of bytes encoded as a Base64 string. It
/// is intended for scenarios where binary data needs to be transmitted, such as file contents,
/// cryptographic keys, image data, or any arbitrary binary data.
/// </summary>
/// <seealso href="https://scalars.graphql.org/chillicream/base64-string.html">Specification</seealso>
public class Base64StringType : ScalarType<byte[], StringValueNode>
{
    private const string SpecifiedByUri = "https://scalars.graphql.org/chillicream/base64-string.html";

    /// <summary>
    /// Initializes a new instance of the <see cref="Base64StringType"/> class.
    /// </summary>
    public Base64StringType(
        string name,
        string? description = null,
        BindingBehavior bind = BindingBehavior.Explicit)
        : base(name, bind)
    {
        Description = description;
        Pattern = @"^(?:[A-Za-z0-9+\/]{4})*(?:[A-Za-z0-9+\/]{2}==|[A-Za-z0-9+\/]{3}=)?$";
        SpecifiedBy = new Uri(SpecifiedByUri);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Base64StringType"/> class.
    /// </summary>
    [ActivatorUtilitiesConstructor]
    public Base64StringType()
        : this(
            ScalarNames.Base64String,
            TypeResources.Base64StringType_Description,
            BindingBehavior.Implicit)
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
