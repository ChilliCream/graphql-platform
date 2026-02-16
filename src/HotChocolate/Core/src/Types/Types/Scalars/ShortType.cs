using System.Text.Json;
using HotChocolate.Language;
using HotChocolate.Properties;
using HotChocolate.Text.Json;

namespace HotChocolate.Types;

/// <summary>
/// Represents a scalar type for 16-bit signed integers (short) in GraphQL.
/// This type serializes as an integer and supports values from -32,768 to 32,767.
/// </summary>
public class ShortType : IntegerTypeBase<short>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ShortType"/> class.
    /// </summary>
    public ShortType(short min, short max)
        : this(
            ScalarNames.Short,
            TypeResources.ShortType_Description,
            min,
            max,
            BindingBehavior.Implicit)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ShortType"/> class.
    /// </summary>
    public ShortType(
        string name,
        string? description = null,
        short min = short.MinValue,
        short max = short.MaxValue,
        BindingBehavior bind = BindingBehavior.Explicit)
        : base(name, min, max, bind)
    {
        Description = description;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ShortType"/> class.
    /// </summary>
    [ActivatorUtilitiesConstructor]
    public ShortType() : this(short.MinValue, short.MaxValue)
    {
    }

    /// <inheritdoc />
    protected override short OnCoerceInputLiteral(IntValueNode valueSyntax)
        => valueSyntax.ToInt16();

    /// <inheritdoc />
    protected override short OnCoerceInputValue(JsonElement inputValue)
        => inputValue.GetInt16();

    /// <inheritdoc />
    public override void OnCoerceOutputValue(short runtimeValue, ResultElement resultValue)
        => resultValue.SetNumberValue(runtimeValue);

    /// <inheritdoc />
    public override IValueNode OnValueToLiteral(short runtimeValue)
        => new IntValueNode(runtimeValue);
}
