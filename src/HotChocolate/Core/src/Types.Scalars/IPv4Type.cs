using System.Text.RegularExpressions;
using HotChocolate.Language;

namespace HotChocolate.Types;

/// <summary>
/// The `IPv4` scalar type represents a valid a IPv4 address as defined in
/// <a href="https://tools.ietf.org/html/rfc791">RFC791</a>
/// </summary>
public partial class IPv4Type : RegexType
{
    private const string _validationPattern =
        "(^(?:(?:(?:0?0?[0-9]|0?[1-9][0-9]|1[0-9][0-9]|2[0-4][0-9]|25[0-5])\\.){3}(?:0?0?" +
        "[0-9]|0?[1-9][0-9]|1[0-9][0-9]|2[0-4][0-9]|25[0-5])(?:\\/(?:[0-9]|[1-2][0-9]|3[0" +
        "-2]))?)$)";

    [GeneratedRegex(_validationPattern, RegexOptions.IgnoreCase, DefaultRegexTimeoutInMs)]
    private static partial Regex CreateRegex();

    /// <summary>
    /// Initializes a new instance of the <see cref="IPv4Type"/> class.
    /// </summary>
    public IPv4Type(
        string name,
        string? description = null,
        BindingBehavior bind = BindingBehavior.Explicit)
        : base(
            name,
            CreateRegex(),
            description,
            bind)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="IPv4Type"/> class.
    /// </summary>
    [ActivatorUtilitiesConstructor]
    public IPv4Type()
        : this(
            WellKnownScalarTypes.IPv4,
            ScalarResources.IPv4Type_Description)
    {
    }

    /// <inheritdoc />
    protected override SerializationException CreateParseLiteralError(IValueNode valueSyntax)
    {
        return ThrowHelper.IPv4Type_ParseLiteral_IsInvalid(this);
    }

    /// <inheritdoc />
    protected override SerializationException CreateParseValueError(object runtimeValue)
    {
        return ThrowHelper.IPv4Type_ParseValue_IsInvalid(this);
    }
}
