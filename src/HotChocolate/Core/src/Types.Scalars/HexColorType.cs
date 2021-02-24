using System.Text.RegularExpressions;
using HotChocolate.Language;

namespace HotChocolate.Types.Scalars
{
    /// <summary>
    /// The `HexColor` scalar type represents a valid HEX color code.
    /// </summary>
    public class HexColorType : StringType
    {
        private static readonly string _validationPattern =
            ScalarResources.HexColorType_ValidationPattern;

        private static readonly Regex _validationRegex =
            new(_validationPattern, RegexOptions.Compiled | RegexOptions.IgnoreCase);

        /// <summary>
        /// Initializes a new instance of the <see cref="HexColorType"/> class.
        /// </summary>
        public HexColorType()
            : this(
                WellKnownScalarTypes.HexColor,
                ScalarResources.HexColorType_Description)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="HexColorType"/> class.
        /// </summary>
        public HexColorType(
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
                throw ThrowHelper.HexColorType_ParseLiteral_IsInvalid(this);
            }

            return base.ParseLiteral(valueSyntax);
        }

        /// <inheritdoc />
        protected override StringValueNode ParseValue(string runtimeValue)
        {
            if(!_validationRegex.IsMatch(runtimeValue))
            {
                throw ThrowHelper.HexColorType_ParseValue_IsInvalid(this);
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
