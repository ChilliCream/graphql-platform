using System.Text.Json;
using HotChocolate.Language;
using HotChocolate.Properties;
using HotChocolate.Text.Json;

namespace HotChocolate.Types;

/// <summary>
/// The <c>UnsignedLong</c> scalar type represents an unsigned 64-bit integer. It is intended for
/// scenarios where values exceed the range of unsigned 32-bit integers, such as representing very
/// large counts, file sizes, memory addresses, or any non-negative integer values requiring more
/// than 32 bits.
/// </summary>
/// <seealso href="https://scalars.graphql.org/chillicream/unsigned-long.html">Specification</seealso>
public class UnsignedLongType : IntegerTypeBase<ulong>
{
    private const string SpecifiedByUri = "https://scalars.graphql.org/chillicream/unsigned-long.html";

    /// <summary>
    /// Initializes a new instance of the <see cref="UnsignedLongType"/> class.
    /// </summary>
    public UnsignedLongType(
        string name,
        string? description = null,
        BindingBehavior bind = BindingBehavior.Implicit)
        : base(name, ulong.MinValue, ulong.MaxValue, bind)
    {
        Description = description;
        SpecifiedBy = new Uri(SpecifiedByUri);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="UnsignedLongType"/> class.
    /// </summary>
    [ActivatorUtilitiesConstructor]
    public UnsignedLongType()
        : this(
            ScalarNames.UnsignedLong,
            TypeResources.UnsignedLongType_Description)
    {
    }

    /// <inheritdoc />
    protected override ulong OnCoerceInputLiteral(IntValueNode valueLiteral)
        => valueLiteral.ToUInt64();

    /// <inheritdoc />
    protected override ulong OnCoerceInputValue(JsonElement inputValue)
        => inputValue.GetUInt64();

    /// <inheritdoc />
    protected override void OnCoerceOutputValue(ulong runtimeValue, ResultElement resultValue)
        => resultValue.SetNumberValue(runtimeValue);

    /// <inheritdoc />
    protected override IValueNode OnValueToLiteral(ulong runtimeValue)
        => new IntValueNode(runtimeValue);
}
