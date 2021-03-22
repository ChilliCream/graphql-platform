using System.Linq;
using HotChocolate.Types.Scalars;
using System.Text.RegularExpressions;
using HotChocolate.Language;

namespace HotChocolate.Types
{
    /// <summary>
    /// The PostalCode scalar type represents a valid postal code.
    /// </summary>
    public class PostalCodeType : StringType
    {
        /// <summary>
        /// Different validation patterns for postal codes.
        /// </summary>
        private static readonly Regex[] _validationPatterns =
            new[]
                {
                    PostalCodePatterns.US,
                    PostalCodePatterns.UK,
                    PostalCodePatterns.DE,
                    PostalCodePatterns.CA,
                    PostalCodePatterns.FR,
                    PostalCodePatterns.IT,
                    PostalCodePatterns.AU,
                    PostalCodePatterns.NL,
                    PostalCodePatterns.ES,
                    PostalCodePatterns.DK,
                    PostalCodePatterns.SE,
                    PostalCodePatterns.BE,
                    PostalCodePatterns.IN,
                    PostalCodePatterns.AT,
                    PostalCodePatterns.PT,
                    PostalCodePatterns.CH,
                    PostalCodePatterns.LU
                }
                .Select(x => new Regex(x, RegexOptions.Compiled | RegexOptions.IgnoreCase))
                .ToArray();

        /// <summary>
        /// Initializes a new instance of the <see cref="PostalCodeType"/> class.
        /// </summary>
        public PostalCodeType()
            : this(
                WellKnownScalarTypes.PostalCode,
                ScalarResources.PostalCodeType_Description)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PostalCodeType"/> class.
        /// </summary>
        public PostalCodeType(
            NameString name,
            string? description = null,
            BindingBehavior bind = BindingBehavior.Explicit)
            : base(name, description, bind)
        {
            Description = description;
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

            if (runtimeValue is string s &&
                ValidatePostCode(s))
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

            if (resultValue is string s &&
                ValidatePostCode(s))
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
            for (var i = 0; i < _validationPatterns.Length; i++)
            {
                if (_validationPatterns[i].IsMatch(postCode))
                {
                    return true;
                }
            }

            return false;
        }

        private static class PostalCodePatterns
        {
            public const string US =
                "(^\\d{5}([-]?\\d{4})?$)";
            public const string UK =
                "(^(GIR|[A-Z]\\d[A-Z\\d]??|[A-Z]{2}\\d[A-Z\\d]??)[ ]??(\\d[A-Z]{2})$)";
            public const string DE =
                "(\\b((?:0[1-46-9]\\d{3})|(?:[1-357-9]\\d{4})|(?:[4][0-24-9]" +
                "\\d{3})|(?:[6][013-9]\\d{3}))\\b)";
            public const string CA =
                "(^([ABCEGHJKLMNPRSTVXY]\\d[ABCEGHJKLMNPRSTVWXYZ]) {0,1}" +
                "(\\d[ABCEGHJKLMNPRSTVWXYZ]\\d)$)";
            public const string FR =
                "(^(F-)?((2[A|B])|[0-9]{2})[0-9]{3}$)";
            public const string IT =
                "(^(V-|I-)?[0-9]{5}$)";
            public const string AU =
                "(^(0[289][0-9]{2})|([1345689][0-9]{3})|(2[0-8][0-9]{2})|(290[0-9])|" +
                "(291[0-4])|(7[0-4][0-9]{2})|(7[8-9][0-9]{2})$)";
            public const string NL =
                "(^[1-9][0-9]{3}\\s?([a-zA-Z]{2})?$)";
            public const string ES =
                "(^([1-9]{2}|[0-9][1-9]|[1-9][0-9])[0-9]{3}$)";
            public const string DK =
                "(^([D|d][K|k]( |-))?[1-9]{1}[0-9]{3}$)";
            public const string SE =
                "(^(s-|S-){0,1}[0-9]{3}\\s?[0-9]{2}$)";
            public const string BE =
                "(^[1-9]{1}[0-9]{3}$)";
            public const string IN =
                "(^\\d{6}$)";
            public const string AT =
                "(^\\d{4}$)";
            public const string PT =
                "(^\\d{4}([\\-]\\d{3})?$)";
            public const string CH =
                "(^\\d{4}$)";
            public const string LU =
                "(^\\d{4}$)";
        }
    }
}
