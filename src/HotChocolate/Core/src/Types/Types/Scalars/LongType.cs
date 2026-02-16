using System.Text.Json;
using HotChocolate.Language;
using HotChocolate.Properties;
using HotChocolate.Text.Json;

namespace HotChocolate.Types;

/// <summary>
/// Represents a scalar type for 64-bit signed integers (long) in GraphQL.
/// This type serializes as an integer and supports values from -9,223,372,036,854,775,808
/// to 9,223,372,036,854,775,807.
/// </summary>
public class LongType : IntegerTypeBase<long>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="LongType"/> class.
    /// </summary>
    public LongType(long min, long max)
        : this(
            ScalarNames.Long,
            TypeResources.LongType_Description,
            min,
            max,
            BindingBehavior.Implicit)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="LongType"/> class.
    /// </summary>
    public LongType(
        string name,
        string? description = null,
        long min = long.MinValue,
        long max = long.MaxValue,
        BindingBehavior bind = BindingBehavior.Explicit)
        : base(name, min, max, bind)
    {
        Description = description;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="LongType"/> class.
    /// </summary>
    [ActivatorUtilitiesConstructor]
    public LongType()
        : this(long.MinValue, long.MaxValue)
    {
    }

    /// <inheritdoc />
    protected override long OnCoerceInputLiteral(IntValueNode valueSyntax)
        => valueSyntax.ToInt64();

    /// <inheritdoc />
    protected override long OnCoerceInputValue(JsonElement inputValue)
        => inputValue.GetInt64();

    /// <inheritdoc />
    protected override void OnCoerceOutputValue(long runtimeValue, ResultElement resultValue)
        => resultValue.SetNumberValue(runtimeValue);

    /// <inheritdoc />
    protected override IValueNode OnValueToLiteral(long runtimeValue)
        => new IntValueNode(runtimeValue);
}
