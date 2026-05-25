using System.Text.Json;
using HotChocolate.Features;
using HotChocolate.Language;
using HotChocolate.Properties;
using HotChocolate.Text.Json;
using static HotChocolate.Utilities.ThrowHelper;

namespace HotChocolate.Types;

/// <summary>
/// The <c>URI</c> scalar type represents a Uniform Resource Identifier (URI) as defined by RFC
/// 3986. It is intended for scenarios where a field must contain a valid URI, which includes both
/// URLs (locators) and URNs (names), such as resource identifiers, namespace identifiers, or any
/// standardized resource reference.
/// </summary>
/// <seealso href="https://scalars.graphql.org/chillicream/uri.html">Specification</seealso>
public class UriType : ScalarType<Uri, StringValueNode>
{
    private const string SpecifiedByUri = "https://scalars.graphql.org/chillicream/uri.html";

    /// <summary>
    /// Initializes a new instance of the <see cref="UriType"/> class.
    /// </summary>
    public UriType(
        string name,
        string? description = null,
        BindingBehavior bind = BindingBehavior.Explicit)
        : base(name, bind)
    {
        Description = description;
        SpecifiedBy = new Uri(SpecifiedByUri);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="UriType"/> class.
    /// </summary>
    [ActivatorUtilitiesConstructor]
    public UriType()
        : this(
            ScalarNames.URI,
            TypeResources.UriType_Description,
            BindingBehavior.Implicit)
    {
    }

    /// <inheritdoc />
    protected override Uri OnCoerceInputLiteral(StringValueNode valueLiteral)
    {
        if (TryParseUri(valueLiteral.Value, out var value))
        {
            return value;
        }

        throw Scalar_Cannot_CoerceInputLiteral(this, valueLiteral);
    }

    /// <inheritdoc />
    protected override Uri OnCoerceInputValue(JsonElement inputValue, IFeatureProvider context)
    {
        if (TryParseUri(inputValue.GetString()!, out var value))
        {
            return value;
        }

        throw Scalar_Cannot_CoerceInputValue(this, inputValue);
    }

    /// <inheritdoc />
    protected override void OnCoerceOutputValue(Uri runtimeValue, ResultElement resultValue)
    {
        var serialized = runtimeValue.IsAbsoluteUri
            ? runtimeValue.AbsoluteUri
            : runtimeValue.ToString();
        resultValue.SetStringValue(serialized);
    }

    /// <inheritdoc />
    protected override StringValueNode OnValueToLiteral(Uri runtimeValue)
    {
        var value = runtimeValue.IsAbsoluteUri
            ? runtimeValue.AbsoluteUri
            : runtimeValue.ToString();
        return new StringValueNode(value);
    }

    private static bool TryParseUri(string value, out Uri uri)
    {
        if (!Uri.TryCreate(value, UriKind.RelativeOrAbsolute, out var parsedUri))
        {
            uri = null!;
            return false;
        }

        uri = parsedUri;
        return true;
    }
}
