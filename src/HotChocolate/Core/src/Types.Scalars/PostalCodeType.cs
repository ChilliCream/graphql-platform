using System;
using System.Linq;
using System.Text.RegularExpressions;
using HotChocolate.Language;

namespace HotChocolate.Types.Scalars
{
    /// <summary>
    /// The PostalCode scalar type represents a valid postal code.
    /// </summary>
    public class PostalCodeType : StringType
    {
        /// <summary>
        /// US - United States
        /// UK - United Kingdom
        /// DE - Germany
        /// CA - Canada
        /// FR - France
        /// IT - Italy
        /// AU - Australia
        /// NL - Netherlands
        /// ES - Spain
        /// DK - Denmark
        /// SE - Sweden
        /// BE - Belgium
        /// IN - India
        /// AT - Austria
        /// PT - Portugal
        /// CH - Switzerland
        /// LU - Luxembourg
        /// </summary>
        private static readonly string[] _validationPatterns =
        {
            ScalarResources.PostalCode_ValidationPattern_US,
            ScalarResources.PostalCode_ValidationPattern_DE,
            ScalarResources.PostalCode_ValidationPattern_UK,
            ScalarResources.PostalCode_ValidationPattern_CA,
            ScalarResources.PostalCode_ValidationPattern_FR,
            ScalarResources.PostalCode_ValidationPattern_IT,
            ScalarResources.PostalCode_ValidationPattern_AU,
            ScalarResources.PostalCode_ValidationPattern_NL,
            ScalarResources.PostalCode_ValidationPattern_ES,
            ScalarResources.PostalCode_ValidationPattern_DK,
            ScalarResources.PostalCode_ValidationPattern_SE,
            ScalarResources.PostalCode_ValidationPattern_BE,
            ScalarResources.PostalCode_ValidationPattern_IN,
            ScalarResources.PostalCode_ValidationPattern_AT,
            ScalarResources.PostalCode_ValidationPattern_PT,
            ScalarResources.PostalCode_ValidationPattern_CH,
            ScalarResources.PostalCode_ValidationPattern_LU
        };

        /// <summary>
        /// Start with a limited set as suggested here:
        /// http://www.pixelenvision.com/1708/zip-postal-code-validation-regex-php-code-for-12-countries/
        /// and here:
        /// https://stackoverflow.com/questions/578406/what-is-the-ultimate-postal-code-and-zip-regex
        /// </summary>
        private static readonly Func<string, bool> _validPostalCode = input =>
            _validationPatterns.Select(pattern => Regex.Match(
                input, pattern, RegexOptions.Compiled | RegexOptions.IgnoreCase)).Any(match => match.Success);

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
            return _validPostalCode(runtimeValue);
        }

        /// <inheritdoc />
        protected override bool IsInstanceOfType(StringValueNode valueSyntax)
        {
            return _validPostalCode(valueSyntax.Value);
        }

        /// <inheritdoc />
        protected override string ParseLiteral(StringValueNode valueSyntax)
        {
            if(!_validPostalCode(valueSyntax.Value))
            {
                throw ThrowHelper.PostalCodeType_ParseLiteral_IsInvalid(this);
            }

            return base.ParseLiteral(valueSyntax);
        }

        /// <inheritdoc />
        protected override StringValueNode ParseValue(string runtimeValue)
        {
            if(!_validPostalCode(runtimeValue))
            {
                throw ThrowHelper.PostalCodeType_ParseValue_IsInvalid(this);
            }

            return base.ParseValue(runtimeValue);
        }

        /// <inheritdoc />
        public override bool TrySerialize(object? runtimeValue, out object? resultValue)
        {
            if (runtimeValue is null)
            {
                resultValue = null;
                return true;
            }

            if(runtimeValue is string s &&
               _validPostalCode(s))
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

            if(resultValue is string s &&
               _validPostalCode(s))
            {
                runtimeValue = s;
                return true;
            }

            runtimeValue = null;
            return false;
        }
    }
}
