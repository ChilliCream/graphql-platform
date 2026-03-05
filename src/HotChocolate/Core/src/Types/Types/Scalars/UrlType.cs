using System.Text.Json;
using HotChocolate.Features;
using HotChocolate.Language;
using HotChocolate.Properties;
using HotChocolate.Text.Json;
using static HotChocolate.Utilities.ThrowHelper;

namespace HotChocolate.Types;

/// <summary>
/// The <c>URL</c> scalar type represents a Uniform Resource Locator (URL) as defined by RFC 3986.
/// It is intended for scenarios where a field must contain a valid URL, such as links to external
/// resources, API endpoints, image sources, or any web-accessible resource.
/// </summary>
/// <seealso href="https://scalars.graphql.org/chillicream/url.html">Specification</seealso>
public class UrlType : ScalarType<Uri, StringValueNode>
{
    private const string SpecifiedByUri = "https://scalars.graphql.org/chillicream/url.html";
    // TODO: This is for backwards compatibility. The UriType should be used for relative URIs.
    private readonly bool _allowRelativeUris;

    /// <summary>
    /// Initializes a new instance of the <see cref="UrlType"/> class.
    /// </summary>
    public UrlType(
        string name,
        string? description = null,
        BindingBehavior bind = BindingBehavior.Explicit,
        bool allowRelativeUris = false)
        : base(name, bind)
    {
        Description = description;
        SpecifiedBy = new Uri(SpecifiedByUri);
        _allowRelativeUris = allowRelativeUris;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="UrlType"/> class.
    /// </summary>
    public UrlType(bool allowRelativeUris = false) : this(ScalarNames.URL, TypeResources.UrlType_Description)
    {
        _allowRelativeUris = allowRelativeUris;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="UrlType"/> class.
    /// </summary>
    [ActivatorUtilitiesConstructor]
    public UrlType() : this(ScalarNames.URL, TypeResources.UrlType_Description)
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

    private bool TryParseUri(string value, out Uri uri)
    {
        var uriKind = _allowRelativeUris ? UriKind.RelativeOrAbsolute : UriKind.Absolute;

        if (!Uri.TryCreate(value, uriKind, out var parsedUri))
        {
            uri = null!;
            return false;
        }

        // Don't accept a relative URI that does not start with '/'
        if (!parsedUri.IsAbsoluteUri && !parsedUri.OriginalString.StartsWith('/'))
        {
            uri = null!;
            return false;
        }

        uri = parsedUri;
        return true;
    }
}
