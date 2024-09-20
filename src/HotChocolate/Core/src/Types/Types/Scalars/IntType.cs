using HotChocolate.Language;
using HotChocolate.Properties;

#nullable enable

namespace HotChocolate.Types;

/// <summary>
/// The Int scalar type represents a signed 32‐bit numeric non‐fractional
/// value. Response formats that support a 32‐bit integer or a number type
/// should use that type to represent this scalar.
///
/// http://facebook.github.io/graphql/June2018/#sec-Int
/// </summary>
[SpecScalar]
public class IntType : IntegerTypeBase<int>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="IntType"/> class.
    /// </summary>
    public IntType(int min, int max)
        : this(
            ScalarNames.Int,
            TypeResources.IntType_Description,
            min,
            max,
            BindingBehavior.Implicit)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="IntType"/> class.
    /// </summary>
    public IntType(
        string name,
        string? description = null,
        int min = int.MinValue,
        int max = int.MaxValue,
        BindingBehavior bind = BindingBehavior.Explicit)
        : base(name, min, max, bind)
    {
        Description = description;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="IntType"/> class.
    /// </summary>
    [ActivatorUtilitiesConstructor]
    public IntType()
        : this(int.MinValue, int.MaxValue)
    {
    }

    protected override int ParseLiteral(IntValueNode valueSyntax)
        => valueSyntax.ToInt32();

    protected override IntValueNode ParseValue(int runtimeValue)
        => new(runtimeValue);
}
