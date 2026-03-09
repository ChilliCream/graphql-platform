using System.Text.Json;
using HotChocolate.Features;
using HotChocolate.Fusion.Language;
using HotChocolate.Language;
using HotChocolate.Text.Json;
using static HotChocolate.Utilities.ThrowHelper;

namespace HotChocolate.Types.Composite;

/// <summary>
/// A scalar type that represents a field selection map used in GraphQL Fusion.
/// This type parses and serializes field selection syntax as strings.
/// </summary>
public sealed class FieldSelectionMapType : ScalarType<IValueSelectionNode, StringValueNode>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="FieldSelectionMapType"/> class
    /// with the default name "FieldSelectionMap".
    /// </summary>
    public FieldSelectionMapType() : this("FieldSelectionMap")
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="FieldSelectionMapType"/> class.
    /// </summary>
    /// <param name="name">
    /// The name that this scalar shall have.
    /// </param>
    /// <param name="bind">
    /// Defines the binding behavior of this scalar type.
    /// </param>
    public FieldSelectionMapType(string name, BindingBehavior bind = BindingBehavior.Explicit)
        : base(name, bind)
    {
    }

    /// <inheritdoc />
    protected override bool ApplySerializeAsToScalars => false;

    /// <inheritdoc />
    protected override IValueSelectionNode OnCoerceInputLiteral(StringValueNode valueLiteral)
    {
        try
        {
            return ParseValueSelection(valueLiteral.Value);
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
    protected override IValueSelectionNode OnCoerceInputValue(JsonElement inputValue, IFeatureProvider context)
    {
        try
        {
            return ParseValueSelection(inputValue.GetString()!);
        }
        catch (SyntaxException)
        {
            throw Scalar_Cannot_CoerceInputValue(this, inputValue);
        }
    }

    /// <inheritdoc />
    protected override void OnCoerceOutputValue(IValueSelectionNode runtimeValue, ResultElement resultValue)
        => resultValue.SetStringValue(FormatValueSelection(runtimeValue));

    /// <inheritdoc />
    protected override StringValueNode OnValueToLiteral(IValueSelectionNode runtimeValue)
        => new StringValueNode(FormatValueSelection(runtimeValue));

    /// <summary>
    /// Parses a field selection map from a string representation.
    /// </summary>
    /// <param name="sourceText">
    /// The source text containing the field selection syntax.
    /// </param>
    /// <returns>
    /// An <see cref="IValueSelectionNode"/> representing the parsed field selection.
    /// </returns>
    /// <exception cref="SyntaxException">
    /// Thrown when the source text contains invalid field selection syntax.
    /// </exception>
    internal static IValueSelectionNode ParseValueSelection(string sourceText)
        => FieldSelectionMapParser.Parse(sourceText);

    /// <summary>
    /// Formats a field selection node into its string representation.
    /// </summary>
    /// <param name="valueSelection">
    /// The value selection node to format.
    /// </param>
    /// <returns>
    /// A string representation of the field selection.
    /// </returns>
    private static string FormatValueSelection(IValueSelectionNode valueSelection)
        => valueSelection.ToString();
}
