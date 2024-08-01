using HotChocolate.Language;
using HotChocolate.Properties;

#nullable enable

namespace HotChocolate.Types;

/// <summary>
/// The Boolean scalar type represents true or false.
///
/// http://facebook.github.io/graphql/June2018/#sec-Boolean
/// </summary>
[SpecScalar]
public class BooleanType : ScalarType<bool, BooleanValueNode>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="BooleanType"/> class.
    /// </summary>
    public BooleanType(
        string name,
        string? description = null,
        BindingBehavior bind = BindingBehavior.Explicit)
        : base(name, bind)
    {
        Description = description;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="BooleanType"/> class.
    /// </summary>
    [ActivatorUtilitiesConstructor]
    public BooleanType()
        : this(
            ScalarNames.Boolean,
            TypeResources.BooleanType_Description,
            BindingBehavior.Implicit)
    {
    }

    protected override bool ParseLiteral(BooleanValueNode valueSyntax)
    {
        return valueSyntax.Value;
    }

    protected override BooleanValueNode ParseValue(bool runtimeValue)
    {
        return runtimeValue ? BooleanValueNode.True : BooleanValueNode.False;
    }

    public override IValueNode ParseResult(object? resultValue)
    {
        if (resultValue is null)
        {
            return NullValueNode.Default;
        }

        if (resultValue is bool b)
        {
            return b ? BooleanValueNode.True : BooleanValueNode.False;
        }

        throw new SerializationException(
            TypeResourceHelper.Scalar_Cannot_ParseResult(Name, resultValue.GetType()),
            this);
    }
}
