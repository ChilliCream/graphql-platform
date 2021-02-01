using System.Text.RegularExpressions;
using HotChocolate.Language;

namespace HotChocolate.Types.Scalars
{
    /// <summary>
    /// A field whose value conforms to the standard E.164 format as specified
    /// in: https://en.wikipedia.org/wiki/E.164.
    /// </summary>
    public class PhoneNumberType : StringType
    {
        private static readonly string _validationPattern = @"/^\+[1-9]\d{1,14}$/";

        private static readonly Regex _validationRegex =
            new Regex(
                _validationPattern,
                RegexOptions.Compiled | RegexOptions.IgnoreCase);

        public PhoneNumberType()
            : this(
                WellKnownScalarTypes.PhoneNumber,
                ScalarResources.PhoneNumberType_Description)
        {

        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PhoneNumberType"/> class.
        /// </summary>
        public PhoneNumberType(
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
                throw ThrowHelper.PhoneNumberType_ParseLiteral_IsInvalid(this);
            }

            return base.ParseLiteral(valueSyntax);
        }

        /// <inheritdoc />
        protected override StringValueNode ParseValue(string runtimeValue)
        {

            if(!_validationRegex.IsMatch(runtimeValue))
            {
                throw ThrowHelper.PhoneNumber_ParseValue_IsInvalid(this);
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
