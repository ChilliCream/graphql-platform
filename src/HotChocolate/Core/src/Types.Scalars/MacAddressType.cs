using System.Text.RegularExpressions;
using HotChocolate.Language;

namespace HotChocolate.Types;

/// <summary>
/// The `MacAddress` scalar type represents IEEE 802 48-bit (MAC-48/EUI-48)
/// and 64-bit (EUI-64) Mac addresses, represented as UTF-8
/// character sequences. The scalar follows the specification defined in
/// <a href="https://tools.ietf.org/html/rfc7042#page-19">RFC7042</a> and
/// <a href="https://tools.ietf.org/html/rfc7043">RFC 7043</a> respectively.
/// </summary>
#if BACKREFERENCE_NOT_SUPPORTED
public partial class MacAddressType : RegexType
#else
public class MacAddressType : RegexType
#endif
{
    private const string ValidationPattern =
        @"^(?:[0-9A-Fa-f]{2}([:-]?)[0-9A-Fa-f]{2})(?:(?:\1|\.)(?:[0-9A-Fa-f]{2}([:-]?)"
        + "[0-9A-Fa-f]{2})){2,3}$";

#if BACKREFERENCE_NOT_SUPPORTED
    [GeneratedRegex(_validationPattern, RegexOptions.None, DefaultRegexTimeoutInMs)]
    private static partial Regex CreateRegex();
#else
    private static Regex CreateRegex()
        => new Regex(
            ValidationPattern,
            RegexOptions.Compiled,
            TimeSpan.FromMilliseconds(DefaultRegexTimeoutInMs));
#endif

    /// <summary>
    /// Initializes a new instance of the <see cref="MacAddressType"/> class.
    /// </summary>
    public MacAddressType(
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
    /// Initializes a new instance of the <see cref="MacAddressType"/> class.
    /// </summary>
    [ActivatorUtilitiesConstructor]
    public MacAddressType()
        : this(
            WellKnownScalarTypes.MacAddress,
            ScalarResources.MacAddressType_Description)
    {
    }

    protected override LeafCoercionException FormatException(string runtimeValue)
        => ThrowHelper.MacAddressType_InvalidFormat(this);
}
