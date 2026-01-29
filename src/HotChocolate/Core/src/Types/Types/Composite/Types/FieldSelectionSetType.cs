using System.Text.Json;
using HotChocolate.Features;
using HotChocolate.Language;
using HotChocolate.Text.Json;
using static HotChocolate.Utilities.ThrowHelper;

namespace HotChocolate.Types.Composite;

/// <summary>
/// A scalar type that represents a GraphQL selection set.
/// This type parses and serializes selection set syntax as strings.
/// </summary>
public sealed class FieldSelectionSetType : ScalarType<SelectionSetNode, StringValueNode>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="FieldSelectionSetType"/> class
    /// with the default name "FieldSelectionSet".
    /// </summary>
    public FieldSelectionSetType() : this("FieldSelectionSet")
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="FieldSelectionSetType"/> class.
    /// </summary>
    /// <param name="name">
    /// The name that this scalar shall have.
    /// </param>
    /// <param name="bind">
    /// Defines the binding behavior of this scalar type.
    /// </param>
    public FieldSelectionSetType(string name, BindingBehavior bind = BindingBehavior.Explicit)
        : base(name, bind)
    {
    }

    /// <inheritdoc />
    protected override bool ApplySerializeAsToScalars => false;

    /// <inheritdoc />
    protected override SelectionSetNode OnCoerceInputLiteral(StringValueNode valueLiteral)
    {
        try
        {
            return ParseSelectionSet(valueLiteral.Value);
        }
        catch (SyntaxException)
        {
            throw new SchemaException(
                SchemaErrorBuilder.New()
                    .SetMessage("The field selection set syntax is invalid.")
                    .Build());
        }
    }

    /// <inheritdoc />
    protected override SelectionSetNode OnCoerceInputValue(JsonElement inputValue, IFeatureProvider context)
    {
        try
        {
            return ParseSelectionSet(inputValue.GetString()!);
        }
        catch (SyntaxException)
        {
            throw Scalar_Cannot_CoerceInputValue(this, inputValue);
        }
    }

    /// <inheritdoc />
    protected override void OnCoerceOutputValue(SelectionSetNode runtimeValue, ResultElement resultValue)
        => resultValue.SetStringValue(SerializeSelectionSet(runtimeValue));

    /// <inheritdoc />
    protected override StringValueNode OnValueToLiteral(SelectionSetNode runtimeValue)
        => new StringValueNode(SerializeSelectionSet(runtimeValue));

    /// <summary>
    /// Parses a GraphQL selection set from a string representation.
    /// </summary>
    /// <param name="s">
    /// The source text containing the selection set syntax.
    /// Braces are optional and will be added if not present.
    /// </param>
    /// <returns>
    /// A <see cref="SelectionSetNode"/> representing the parsed selection set.
    /// </returns>
    /// <exception cref="SyntaxException">
    /// Thrown when the source text contains invalid selection set syntax.
    /// </exception>
    internal static SelectionSetNode ParseSelectionSet(string s)
    {
        s = $"{{ {s.Trim('{', '}')} }}";
        return Utf8GraphQLParser.Syntax.ParseSelectionSet(s);
    }

    /// <summary>
    /// Serializes a selection set node into its string representation.
    /// </summary>
    /// <param name="selectionSet">
    /// The selection set node to serialize.
    /// </param>
    /// <returns>
    /// A string representation of the selection set without the outer braces.
    /// </returns>
    private static string SerializeSelectionSet(SelectionSetNode selectionSet)
    {
        var s = selectionSet.ToString(false);
        return s.AsSpan()[1..^1].Trim().ToString();
    }
}
