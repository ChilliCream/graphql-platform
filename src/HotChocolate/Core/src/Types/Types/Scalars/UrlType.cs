using System.Diagnostics.CodeAnalysis;
using HotChocolate.Language;
using HotChocolate.Properties;

#nullable enable

namespace HotChocolate.Types;

public class UrlType : ScalarType<Uri, StringValueNode>
{
    private const string _specifiedBy = "https://tools.ietf.org/html/rfc3986";

    /// <summary>
    /// Initializes a new instance of the <see cref="UrlType"/> class.
    /// </summary>
    public UrlType(
        string name,
        string? description = null,
        BindingBehavior bind = BindingBehavior.Explicit)
        : base(name, bind)
    {
        Description = description;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="UrlType"/> class.
    /// </summary>
    [ActivatorUtilitiesConstructor]
    public UrlType()
        : this(ScalarNames.URL, bind: BindingBehavior.Implicit)
    {
        SpecifiedBy = new Uri(_specifiedBy);
    }

    protected override bool IsInstanceOfType(StringValueNode valueSyntax)
    {
        return TryParseUri(valueSyntax.Value, out _);
    }

    protected override Uri ParseLiteral(StringValueNode valueSyntax)
    {
        if (TryParseUri(valueSyntax.Value, out var uri))
        {
            return uri;
        }

        throw new SerializationException(
            TypeResourceHelper.Scalar_Cannot_ParseLiteral(Name, valueSyntax.GetType()),
            this);
    }

    protected override StringValueNode ParseValue(Uri runtimeValue)
    {
        return new(runtimeValue.AbsoluteUri);
    }

    public override IValueNode ParseResult(object? resultValue)
    {
        if (resultValue is null)
        {
            return NullValueNode.Default;
        }

        if (resultValue is string s)
        {
            return new StringValueNode(s);
        }

        if (resultValue is Uri uri)
        {
            return ParseValue(uri);
        }

        throw new SerializationException(
            TypeResourceHelper.Scalar_Cannot_ParseResult(Name, resultValue.GetType()),
            this);
    }

    public override bool TrySerialize(object? runtimeValue, out object? resultValue)
    {
        if (runtimeValue is null)
        {
            resultValue = null;
            return true;
        }

        if (runtimeValue is Uri uri)
        {
            resultValue = uri.IsAbsoluteUri ? uri.AbsoluteUri : uri.ToString();
            return true;
        }

        resultValue = null;
        return false;
    }

    public override bool TryDeserialize(object? resultValue, out object? runtimeValue)
    {
        if (resultValue is null)
        {
            runtimeValue = null;
            return true;
        }

        if (resultValue is string s && TryParseUri(s, out var uri))
        {
            runtimeValue = uri;
            return true;
        }

        if (resultValue is Uri u)
        {
            runtimeValue = u;
            return true;
        }

        runtimeValue = null;
        return false;
    }

    private bool TryParseUri(string value, [NotNullWhen(true)] out Uri? uri)
    {
        if (!Uri.TryCreate(value, UriKind.RelativeOrAbsolute, out uri))
        {
            return false;
        }

        // Don't accept a relative URI that does not start with '/'
        if (!uri.IsAbsoluteUri && !uri.OriginalString.StartsWith("/"))
        {
            uri = null;
            return false;
        }

        return true;
    }
}
