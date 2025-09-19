using HotChocolate.Language;

namespace HotChocolate.Types;

/// <summary>
/// The NonEmptyString scalar type represents non empty textual data, represented as
/// UTF‐8 character sequences with at least one character
/// </summary>
public class NonEmptyStringType : StringType
{
    /// <summary>
    /// Initializes a new instance of the <see cref="NonEmptyStringType"/> class.
    /// </summary>
    public NonEmptyStringType(
        string name,
        string? description = null,
        BindingBehavior bind = BindingBehavior.Explicit)
        : base(name, description, bind)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="NonEmptyStringType"/> class.
    /// </summary>
    [ActivatorUtilitiesConstructor]
    public NonEmptyStringType()
        : this(
            WellKnownScalarTypes.NonEmptyString,
            ScalarResources.NonEmptyStringType_Description)
    {
    }

    /// <inheritdoc />
    protected override bool IsInstanceOfType(string runtimeValue)
    {
        return runtimeValue != string.Empty;
    }

    /// <inheritdoc />
    protected override bool IsInstanceOfType(StringValueNode valueSyntax)
    {
        return valueSyntax.Value != string.Empty;
    }

    /// <inheritdoc />
    public override bool TrySerialize(object? runtimeValue, out object? resultValue)
    {
        if (runtimeValue is string s && s == string.Empty)
        {
            resultValue = null;
            return false;
        }

        return base.TrySerialize(runtimeValue, out resultValue);
    }

    /// <inheritdoc />
    public override bool TryDeserialize(object? resultValue, out object? runtimeValue)
    {
        if (!base.TryDeserialize(resultValue, out runtimeValue))
        {
            return false;
        }

        if (runtimeValue is string s && s == string.Empty)
        {
            return false;
        }

        return true;
    }

    /// <inheritdoc />
    protected override SerializationException CreateParseLiteralError(IValueNode valueSyntax)
    {
        throw ThrowHelper.NonEmptyStringType_ParseLiteral_IsEmpty(this);
    }

    /// <inheritdoc />
    protected override SerializationException CreateParseValueError(object runtimeValue)
    {
        throw ThrowHelper.NonEmptyStringType_ParseValue_IsEmpty(this);
    }
}
