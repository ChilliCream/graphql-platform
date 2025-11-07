using HotChocolate.Language;
using HotChocolate.Properties;

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
        SerializationType = ScalarSerializationType.Int;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ShortType"/> class.
    /// </summary>
    [ActivatorUtilitiesConstructor]
    public ShortType() : this(short.MinValue, short.MaxValue)
    {
    }

    protected override short ParseLiteral(IntValueNode valueSyntax)
    {
        return valueSyntax.ToInt16();
    }

    protected override IntValueNode ParseValue(short runtimeValue)
    {
        return new IntValueNode(runtimeValue);
    }
}
