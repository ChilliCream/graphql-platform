using System;
using System.Text.RegularExpressions;
using HotChocolate.Language;

namespace HotChocolate.Types;

/// <summary>
/// The `IPv4` scalar type represents a valid a IPv4 address as defined in
/// <a href="https://tools.ietf.org/html/rfc791">RFC791</a>
/// </summary>
#if NET7_0_OR_GREATER
public partial class IPv4Type : RegexType
#else
public class IPv4Type : RegexType
#endif
{
    private const string _validationPattern =
        "(^(?:(?:(?:0?0?[0-9]|0?[1-9][0-9]|1[0-9][0-9]|2[0-4][0-9]|25[0-5])\\.){3}(?:0?0?" +
        "[0-9]|0?[1-9][0-9]|1[0-9][0-9]|2[0-4][0-9]|25[0-5])(?:\\/(?:[0-9]|[1-2][0-9]|3[0" +
        "-2]))?)$)";

#if NET7_0_OR_GREATER
    [GeneratedRegex(_validationPattern, RegexOptions.IgnoreCase, DefaultRegexTimeoutInMs)]
    private static partial Regex CreateRegex();
#else
    private static Regex CreateRegex()
        => new Regex(
            _validationPattern,
            RegexOptions.Compiled | RegexOptions.IgnoreCase,
            TimeSpan.FromMilliseconds(DefaultRegexTimeoutInMs));
#endif


    /// <summary>
    /// Initializes a new instance of the <see cref="IPv4Type"/> class.
    /// </summary>
    public IPv4Type()
        : this(
            WellKnownScalarTypes.IPv4,
            ScalarResources.IPv4Type_Description)
    {
    }

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
