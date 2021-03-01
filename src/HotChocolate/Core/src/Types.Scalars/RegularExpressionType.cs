using System.Text.RegularExpressions;
using HotChocolate.Language;

namespace HotChocolate.Types.Scalars
{
    /// <summary>
    /// The Regular Expression scalar type represents textual data, represented as UTF‐8 character
    /// sequences following a pattern defined as a regular expression, as detailed here:
    /// https://en.wikipedia.org/wiki/Regular_expression.
    /// </summary>
    public class RegularExpressionType : StringType
    {
        private readonly string _validationPattern;
        private readonly Regex _validationRegex;

        /// <summary>
        /// Initializes a new instance of the <see cref="RegularExpressionType"/> class.
        /// </summary>
        public RegularExpressionType(
            NameString name,
            string pattern,
            string? description = null,
            RegexOptions regexOptions = RegexOptions.Compiled,
            BindingBehavior bind = BindingBehavior.Explicit)
            : this(
                name,
                new Regex(pattern, regexOptions),
                description,
                bind)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RegularExpressionType"/> class.
        /// </summary>
        public RegularExpressionType(
            NameString name,
            Regex regex,
            string? description = null,
            BindingBehavior bind = BindingBehavior.Explicit)
            : base(name, description, bind)
        {
            Description = description;
            _validationRegex = regex;
            _validationPattern = regex.ToString();
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
                throw ThrowHelper
                    .RegularExpressionType_ParseLiteral_IsInvalid(this, _validationPattern, Name);
            }

            return base.ParseLiteral(valueSyntax);
        }

        /// <inheritdoc />
        protected override StringValueNode ParseValue(string runtimeValue)
        {
            if (!_validationRegex.IsMatch(runtimeValue))
            {
                throw ThrowHelper
                    .RegularExpressionType_ParseValue_IsInvalid(this, _validationPattern, Name);
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
