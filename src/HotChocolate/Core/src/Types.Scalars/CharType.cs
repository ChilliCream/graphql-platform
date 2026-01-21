using System;
using System.Diagnostics.CodeAnalysis;
using HotChocolate.Language;

namespace HotChocolate.Types;

/// <summary>
/// The CharType scalar type represents a single character value.
/// </summary>
public class CharType : ScalarType<char, StringValueNode>
{
    public CharType(
        string name,
        string? description = null,
        BindingBehavior bind = BindingBehavior.Explicit)
        : base(name, bind)
    {
        Description = description;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="CharType"/> class.
    /// </summary>
    [ActivatorUtilitiesConstructor]
    public CharType()
        : this(
            WellKnownScalarTypes.Char,
            ScalarResources.CharType_Description)
    {
    }

    /// <inheritdoc />
    protected override bool IsInstanceOfType(StringValueNode valueSyntax)
    {
        return TryParseChar(valueSyntax.Value, out _);
    }

    /// <inheritdoc />
    protected override char ParseLiteral(StringValueNode valueSyntax)
    {
        if (TryParseChar(valueSyntax.Value, out var character))
        {
            return character.Value;
        }

        throw ThrowHelper.CharType_ParseLiteral_IsInvalid(this);
    }

    /// <inheritdoc />
    protected override StringValueNode ParseValue(char runtimeValue)
    {
        if (TryParseChar(runtimeValue, out var character))
        {
            return new StringValueNode(character.Value.ToString());
        }

        throw ThrowHelper.CharType_ParseValue_IsInvalid(this);
    }

    /// <inheritdoc />
    public override IValueNode ParseResult(object? resultValue)
    {
        if (resultValue is null)
        {
            return NullValueNode.Default;
        }

        if (TryParseChar(resultValue, out var character))
        {
            return new StringValueNode(character.Value.ToString());
        }

        throw ThrowHelper.CharType_ParseValue_IsInvalid(this);
    }

    /// <inheritdoc />
    public override bool TrySerialize(object? runtimeValue, out object? resultValue)
    {
        if (runtimeValue is null)
        {
            resultValue = null;
            return true;
        }

        if (runtimeValue is char character)
        {
            resultValue = character;
            return true;
        }

        if (TryParseChar(runtimeValue, out var c))
        {
            resultValue = c;
            return true;
        }

        resultValue = null;
        return false;
    }

    /// <inheritdoc />
    public override bool TryDeserialize(object? resultValue, out object? runtimeValue)
    {
        if (resultValue is null)
        {
            runtimeValue = null;
            return true;
        }

        if (resultValue is char character)
        {
            runtimeValue = character;
            return true;
        }

        if (TryParseChar(resultValue, out var c))
        {
            runtimeValue = c;
            return true;
        }

        runtimeValue = null;
        return false;
    }

    /// <summary>
    /// Attempts to convert the specified object to a char.
    /// </summary>
    /// <param name="value">The object to convert.</param>
    /// <param name="character">The char value equivalent to the value parameter if the conversion succeeded, or null if the conversion failed.</param>
    /// <returns>True if the conversion succeeded, otherwise false.</returns>
    private static bool TryParseChar(object value, [NotNullWhen(true)] out char? character)
    {
        character = null;

        try
        {
            character = Convert.ToChar(value);
            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }
}
