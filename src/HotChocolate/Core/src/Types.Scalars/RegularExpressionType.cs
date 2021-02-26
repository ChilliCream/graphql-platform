using System;
using System.Text.RegularExpressions;
using HotChocolate.Language;

namespace HotChocolate.Types.Scalars
{
    /// <summary>
    /// The Regular Expression scalar type represents a String Type following 
    /// a regular expression pattern/format, as defined
    /// here https://en.wikipedia.org/wiki/Regular_expression.
    /// </summary>
    public class RegularExpressionType : StringType
    {
        private readonly string _validationPattern;
        private readonly Regex _validationRegex; 

        /// <summary>
        /// Initializes a new instance of the <see cref="RegularExpressionType"/> class.
        /// </summary>
        public RegularExpressionType()
            : this(
                WellKnownScalarTypes.RegularExpression,
                ScalarResources.RegularExpressionType_Description)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RegularExpressionType"/> class.
        /// </summary>
        public RegularExpressionType(
            NameString name,
            string? description = null,
            BindingBehavior bind = BindingBehavior.Explicit,
            string regEx = "" )
            : base(name, description, bind)
        {
            Description = description;
            _validationPattern = regEx;
            _validationRegex = new(_validationPattern, RegexOptions.Compiled | RegexOptions.IgnoreCase);
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
            if (!_validationRegex.IsMatch(valueSyntax.Value))
            {
                throw ThrowHelper.RegularExpressionType_ParseLiteral_IsInvalid(this);
            }

            return base.ParseLiteral(valueSyntax);
        }

        /// <inheritdoc />
        protected override StringValueNode ParseValue(string runtimeValue)
        {
            if (!_validationRegex.IsMatch(runtimeValue))
            {
                throw ThrowHelper.RegularExpressionType_ParseValue_IsInvalid(this);
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

            if (resultValue is string s &&
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
