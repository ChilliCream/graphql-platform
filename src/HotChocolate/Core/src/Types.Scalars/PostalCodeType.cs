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
        /// Different validation patterns for postal codes.
        /// </summary>
        private static readonly Regex[] _validationPatterns =
            new[]
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
        protected override string ParseLiteral(StringValueNode valueSyntax)
        {
            if (!ValidatePostCode(valueSyntax.Value))
            {
                throw ThrowHelper.PostalCodeType_ParseLiteral_IsInvalid(this);
            }

            return base.ParseLiteral(valueSyntax);
        }

        /// <inheritdoc />
        protected override StringValueNode ParseValue(string runtimeValue)
        {
            if (!ValidatePostCode(runtimeValue))
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
    }
}
