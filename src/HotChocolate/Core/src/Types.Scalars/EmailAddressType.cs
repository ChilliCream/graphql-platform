using System.Text.RegularExpressions;
using HotChocolate.Language;

namespace HotChocolate.Types.Scalars
{
    /// <summary>
    /// The `EmailAddress` scalar type represents a email address, represented as UTF-8 character
    /// sequences. The scalar follows the specification defined in RFC 5322.
    /// </summary>
    public class EmailAddressType : StringType
    {
        /// <summary>
        /// Well established regex for email validation
        /// Source : https://emailregex.com/
        /// </summary>
        private static readonly string _validationPattern =
            ScalarResources.EmailAddressType_ValidationPattern;

        private static readonly Regex _validationRegex =
            new(_validationPattern, RegexOptions.Compiled | RegexOptions.IgnoreCase);

        public EmailAddressType()
            : this(
                WellKnownScalarTypes.EmailAddress,
                ScalarResources.EmailAddressType_Description)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="EmailAddressType"/> class.
        /// </summary>
        public EmailAddressType(
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
            return _validationRegex.IsMatch(runtimeValue);
        }

        /// <inheritdoc />
        protected override bool IsInstanceOfType(StringValueNode valueSyntax)
        {
            return _validationRegex.IsMatch(valueSyntax.Value);
        }

        /// <inheritdoc />
        protected override string ParseLiteral(StringValueNode valueSyntax)
        {
            if(!_validationRegex.IsMatch(valueSyntax.Value))
            {
                throw ThrowHelper.EmailAddressType_ParseLiteral_IsInvalid(this);
            }

            return base.ParseLiteral(valueSyntax);
        }

        /// <inheritdoc />
        protected override StringValueNode ParseValue(string runtimeValue)
        {
            if(!_validationRegex.IsMatch(runtimeValue))
            {
                throw ThrowHelper.EmailAddressType_ParseValue_IsInvalid(this);
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
               _validationRegex.IsMatch(s))
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
               _validationRegex.IsMatch(s))
            {
                runtimeValue = s;
                return true;
            }

            runtimeValue = null;
            return false;
        }
    }
}
