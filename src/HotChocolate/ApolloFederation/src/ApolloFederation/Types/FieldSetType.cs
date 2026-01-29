using System.Text.Json;
using HotChocolate.ApolloFederation.Properties;
using HotChocolate.Features;
using HotChocolate.Language;
using HotChocolate.Text.Json;
using static HotChocolate.Utilities.ThrowHelper;

namespace HotChocolate.ApolloFederation.Types;

/// <summary>
/// A scalar called _FieldSet is a custom scalar type that is used to
/// represent a set of fields.
///
/// Grammatically, a field set is a selection set minus the braces.
///
/// This means it can represent a single field "upc", multiple fields "id countryCode",
/// and even nested selection sets "id organization { id }".
/// </summary>
[Package(FederationVersionUrls.Federation20)]
[FieldSetTypeLegacySupport]
public sealed class FieldSetType : ScalarType<SelectionSetNode, StringValueNode>
{
    /// <summary>
    /// Initializes a new instance of <see cref="FieldSetType"/>.
    /// </summary>
    public FieldSetType() : this(FederationTypeNames.FieldSetType_Name)
    {
    }

    /// <summary>
    /// Initializes a new instance of <see cref="FieldSetType"/>.
    /// </summary>
    /// <param name="name">
    /// The name the scalar shall have.
    /// </param>
    /// <param name="bind">
    /// Defines if this scalar shall bind implicitly to <see cref="SelectionSetNode"/>.
    /// </param>
    public FieldSetType(string name, BindingBehavior bind = BindingBehavior.Explicit)
        : base(name, bind)
    {
        Description = FederationResources.FieldsetType_Description;
    }

    protected override SelectionSetNode OnCoerceInputLiteral(StringValueNode valueLiteral)
    {
        try
        {
            return ParseSelectionSet(valueLiteral.Value);
        }
        catch (SyntaxException)
        {
            throw ThrowHelper.FieldSet_InvalidFormat(this);
        }
    }

    protected override SelectionSetNode OnCoerceInputValue(JsonElement inputValue, IFeatureProvider context)
    {
        if (inputValue.ValueKind is JsonValueKind.String)
        {
            try
            {
                return ParseSelectionSet(inputValue.GetString()!);
            }
            catch (SyntaxException)
            {
                throw ThrowHelper.FieldSet_InvalidFormat(this);
            }
        }

        throw Scalar_Cannot_CoerceInputValue(this, inputValue);
    }

    protected override void OnCoerceOutputValue(SelectionSetNode runtimeValue, ResultElement resultValue)
        => resultValue.SetStringValue(SerializeSelectionSet(runtimeValue));

    protected override StringValueNode OnValueToLiteral(SelectionSetNode runtimeValue)
        => new(SerializeSelectionSet(runtimeValue));

    internal static SelectionSetNode ParseSelectionSet(string s)
        => Utf8GraphQLParser.Syntax.ParseSelectionSet($"{{{s}}}");

    private static string SerializeSelectionSet(SelectionSetNode selectionSet)
    {
        var s = selectionSet.ToString(false);
        return s.AsSpan()[1..^1].Trim().ToString();
    }
}
