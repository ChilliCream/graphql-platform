using System;
using System.Text.RegularExpressions;
using HotChocolate.Language;

namespace HotChocolate.Types.Scalars
{
    /// <summary>
    /// The Regular Expression scalar type represents textual data, represented as UTF‐8 character
    /// sequences following a pattern defined as a <see cref="Regex"/>
    /// </summary>
    public class RegexType : StringType
    {
        private const int _defaultRegexTimeoutInMs = 100;

        private readonly Regex _validationRegex;

        /// <summary>
        /// Initializes a new instance of the <see cref="RegexType"/> class.
        /// </summary>
        public RegexType(
            NameString name,
            string pattern,
            string? description = null,
            RegexOptions regexOptions = RegexOptions.Compiled,
            BindingBehavior bind = BindingBehavior.Explicit)
            : this(
                name,
                new Regex(
                    pattern,
                    regexOptions,
                    TimeSpan.FromMilliseconds(_defaultRegexTimeoutInMs)),
                description,
                bind)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RegexType"/> class.
        /// </summary>
        public RegexType(
            NameString name,
            Regex regex,
            string? description = null,
            BindingBehavior bind = BindingBehavior.Explicit)
            : base(name, description, bind)
        {
            Description = description;
            _validationRegex = regex;
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
                throw CreateParseLiteralError(valueSyntax);
            }

            return base.ParseLiteral(valueSyntax);
        }

        /// <inheritdoc />
        protected override StringValueNode ParseValue(string runtimeValue)
        {
            if (!_validationRegex.IsMatch(runtimeValue))
            {
                throw CreateParseValueError(runtimeValue);
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

        /// <inheritdoc />
        protected override SerializationException CreateParseLiteralError(IValueNode valueSyntax)
        {
            return ThrowHelper.RegexType_ParseLiteral_IsInvalid(this, Name);
        }

        /// <inheritdoc />
        protected override SerializationException CreateParseValueError(object runtimeValue)
        {
            return ThrowHelper.RegexType_ParseValue_IsInvalid(this, Name);
        }
    }
}
