using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;
using HotChocolate.Language;
using static HotChocolate.Types.RegexType;

namespace HotChocolate.Types;

/// <summary>
/// The PostalCode scalar type represents a valid postal code.
/// </summary>
#if DISABLED_DUE_TO_COMPILER_ISSUE
public partial class PostalCodeType : StringType
#else
public class PostalCodeType : StringType
#endif
{
    /// <summary>
    /// Different validation patterns for postal codes.
    /// </summary>
    private static readonly Regex[] s_validationPatterns =
    [
        CreateRegexUs(),
        CreateRegexUk(),
        CreateRegexDe(),
        CreateRegexCa(),
        CreateRegexFr(),
        CreateRegexIt(),
        CreateRegexAu(),
        CreateRegexNl(),
        CreateRegexEs(),
        CreateRegexDk(),
        CreateRegexSe(),
        CreateRegexBe(),
        CreateRegexIn(),
        CreateRegexAt(),
        CreateRegexPt(),
        CreateRegexCh(),
        CreateRegexLu()
    ];

#if DISABLED_DUE_TO_COMPILER_ISSUE
    [GeneratedRegex(PostalCodePatterns.US, RegexOptions.None, DefaultRegexTimeoutInMs)]
    private static partial Regex CreateRegexUs();

    [GeneratedRegex(PostalCodePatterns.UK, RegexOptions.None, DefaultRegexTimeoutInMs)]
    private static partial Regex CreateRegexUk();

    [GeneratedRegex(PostalCodePatterns.DE, RegexOptions.None, DefaultRegexTimeoutInMs)]
    private static partial Regex CreateRegexDe();

    [GeneratedRegex(PostalCodePatterns.CA, RegexOptions.None, DefaultRegexTimeoutInMs)]
    private static partial Regex CreateRegexCa();

    [GeneratedRegex(PostalCodePatterns.FR, RegexOptions.None, DefaultRegexTimeoutInMs)]
    private static partial Regex CreateRegexFr();

    [GeneratedRegex(PostalCodePatterns.IT, RegexOptions.None, DefaultRegexTimeoutInMs)]
    private static partial Regex CreateRegexIt();

    [GeneratedRegex(PostalCodePatterns.AU, RegexOptions.None, DefaultRegexTimeoutInMs)]
    private static partial Regex CreateRegexAu();

    [GeneratedRegex(PostalCodePatterns.NL, RegexOptions.None, DefaultRegexTimeoutInMs)]
    private static partial Regex CreateRegexNl();

    [GeneratedRegex(PostalCodePatterns.ES, RegexOptions.None, DefaultRegexTimeoutInMs)]
    private static partial Regex CreateRegexEs();

    [GeneratedRegex(PostalCodePatterns.DK, RegexOptions.None, DefaultRegexTimeoutInMs)]
    private static partial Regex CreateRegexDk();

    [GeneratedRegex(PostalCodePatterns.SE, RegexOptions.None, DefaultRegexTimeoutInMs)]
    private static partial Regex CreateRegexSe();

    [GeneratedRegex(PostalCodePatterns.BE, RegexOptions.None, DefaultRegexTimeoutInMs)]
    private static partial Regex CreateRegexBe();

    [GeneratedRegex(PostalCodePatterns.IN, RegexOptions.None, DefaultRegexTimeoutInMs)]
    private static partial Regex CreateRegexIn();

    [GeneratedRegex(PostalCodePatterns.AT, RegexOptions.None, DefaultRegexTimeoutInMs)]
    private static partial Regex CreateRegexAt();

    [GeneratedRegex(PostalCodePatterns.PT, RegexOptions.None, DefaultRegexTimeoutInMs)]
    private static partial Regex CreateRegexPt();

    [GeneratedRegex(PostalCodePatterns.CH, RegexOptions.None, DefaultRegexTimeoutInMs)]
    private static partial Regex CreateRegexCh();

    [GeneratedRegex(PostalCodePatterns.LU, RegexOptions.None, DefaultRegexTimeoutInMs)]
    private static partial Regex CreateRegexLu();
#else
    private static Regex CreateRegexUs()
        => CreateRegex(PostalCodePatterns.US);

    private static Regex CreateRegexUk()
        => CreateRegex(PostalCodePatterns.UK);

    private static Regex CreateRegexDe()
        => CreateRegex(PostalCodePatterns.DE);

    private static Regex CreateRegexCa()
        => CreateRegex(PostalCodePatterns.CA);

    private static Regex CreateRegexFr()
        => CreateRegex(PostalCodePatterns.FR);

    private static Regex CreateRegexIt()
        => CreateRegex(PostalCodePatterns.IT);

    private static Regex CreateRegexAu()
        => CreateRegex(PostalCodePatterns.AU);

    private static Regex CreateRegexNl()
        => CreateRegex(PostalCodePatterns.NL);

    private static Regex CreateRegexEs()
        => CreateRegex(PostalCodePatterns.ES);

    private static Regex CreateRegexDk()
        => CreateRegex(PostalCodePatterns.DK);

    private static Regex CreateRegexSe()
        => CreateRegex(PostalCodePatterns.SE);

    private static Regex CreateRegexBe()
        => CreateRegex(PostalCodePatterns.BE);

    private static Regex CreateRegexIn()
        => CreateRegex(PostalCodePatterns.IN);

    private static Regex CreateRegexAt()
        => CreateRegex(PostalCodePatterns.AT);

    private static Regex CreateRegexPt()
        => CreateRegex(PostalCodePatterns.PT);

    private static Regex CreateRegexCh()
        => CreateRegex(PostalCodePatterns.CH);

    private static Regex CreateRegexLu()
        => CreateRegex(PostalCodePatterns.LU);

    private static Regex CreateRegex(string pattern)
        => new Regex(
            pattern,
            RegexOptions.Compiled,
            TimeSpan.FromMilliseconds(DefaultRegexTimeoutInMs));
#endif

    /// <summary>
    /// Initializes a new instance of the <see cref="PostalCodeType"/> class.
    /// </summary>
    public PostalCodeType(
        string name,
        string? description = null,
        BindingBehavior bind = BindingBehavior.Explicit)
        : base(name, description, bind)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="PostalCodeType"/> class.
    /// </summary>
    [ActivatorUtilitiesConstructor]
    public PostalCodeType()
        : this(
            WellKnownScalarTypes.PostalCode,
            ScalarResources.PostalCodeType_Description)
    {
    }

    /// <inheritdoc />
    protected override bool IsInstanceOfType(string runtimeValue)
    {
        return ValidatePostCode(runtimeValue);
    }

    /// <inheritdoc />
    protected override bool IsInstanceOfType(StringValueNode valueSyntax)
    {
        return ValidatePostCode(valueSyntax.Value);
    }

    /// <inheritdoc />
    public override bool TrySerialize(object? runtimeValue, out object? resultValue)
    {
        if (runtimeValue is null)
        {
            resultValue = null;
            return true;
        }

        if (runtimeValue is string s
            && ValidatePostCode(s))
        {
            resultValue = s;
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

        if (resultValue is string s
            && ValidatePostCode(s))
        {
            runtimeValue = s;
            return true;
        }

        runtimeValue = null;
        return false;
    }

    /// <inheritdoc />
    protected override SerializationException CreateParseLiteralError(IValueNode valueSyntax)
    {
        return ThrowHelper.PostalCodeType_ParseLiteral_IsInvalid(this);
    }

    /// <inheritdoc />
    protected override SerializationException CreateParseValueError(object runtimeValue)
    {
        return ThrowHelper.PostalCodeType_ParseValue_IsInvalid(this);
    }

    private static bool ValidatePostCode(string postCode)
    {
        for (var i = 0; i < s_validationPatterns.Length; i++)
        {
            if (s_validationPatterns[i].IsMatch(postCode))
            {
                return true;
            }
        }

        return false;
    }

    [SuppressMessage("ReSharper", "InconsistentNaming")]
    private static class PostalCodePatterns
    {
        public const string US =
            "(^[0-9]{5}([-]?[0-9]{4})?$)";
        public const string UK =
            "(^(GIR|[A-Z][0-9][A-Z0-9]??|[A-Z]{2}[0-9][A-Z0-9]??)[ ]??([0-9][A-Z]{2})$)";
        public const string DE =
            "(\\b((?:0[1-46-9][0-9]{3})|(?:[1-357-9][0-9]{4})|(?:[4][0-24-9]"
            + "[0-9]{3})|(?:[6][013-9][0-9]{3}))\\b)";
        public const string CA =
            "(^([ABCEGHJKLMNPRSTVXY][0-9][ABCEGHJKLMNPRSTVWXYZ]) {0,1}"
            + "([0-9][ABCEGHJKLMNPRSTVWXYZ][0-9])$)";
        public const string FR =
            "(^([Ff]-)?((2[ABab])|[0-9]{2})[0-9]{3}$)";
        public const string IT =
            "(^([VIvi]-)?[0-9]{5}$)";
        public const string AU =
            "(^(0[289][0-9]{2})|([1345689][0-9]{3})|(2[0-8][0-9]{2})|(290[0-9])|"
            + "(291[0-4])|(7[0-4][0-9]{2})|(7[8-9][0-9]{2})$)";
        public const string NL =
            "(^[1-9][0-9]{3}\\s?([a-zA-Z]{2})?$)";
        public const string ES =
            "(^([1-9]{2}|[0-9][1-9]|[1-9][0-9])[0-9]{3}$)";
        public const string DK =
            "(^([Dd][Kk]( |-))?[1-9]{1}[0-9]{3}$)";
        public const string SE =
            "(^(s-|S-){0,1}[0-9]{3}\\s?[0-9]{2}$)";
        public const string BE =
            "(^[1-9]{1}[0-9]{3}$)";
        public const string IN =
            "(^[0-9]{6}$)";
        public const string AT =
            "(^[0-9]{4}$)";
        public const string PT =
            "(^[0-9]{4}([\\-][0-9]{3})?$)";
        public const string CH =
            "(^[0-9]{4}$)";
        public const string LU =
            "(^[0-9]{4}$)";
    }
}
