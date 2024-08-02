using HotChocolate.Language;
using HotChocolate.Properties;

#nullable enable

namespace HotChocolate.Types;

public class LongType
    : IntegerTypeBase<long>
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

    protected override long ParseLiteral(IntValueNode valueSyntax) =>
        valueSyntax.ToInt64();

    protected override IntValueNode ParseValue(long runtimeValue) =>
        new(runtimeValue);
}
