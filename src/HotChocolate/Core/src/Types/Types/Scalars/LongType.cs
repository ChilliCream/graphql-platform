using HotChocolate.Language;
using HotChocolate.Properties;

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
        SerializationType = ScalarSerializationType.Int;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="LongType"/> class.
    /// </summary>
    [ActivatorUtilitiesConstructor]
    public LongType()
        : this(long.MinValue, long.MaxValue)
    {
    }

    protected override long ParseLiteral(IntValueNode valueSyntax) =>
        valueSyntax.ToInt64();

    protected override IntValueNode ParseValue(long runtimeValue) =>
        new(runtimeValue);
}
