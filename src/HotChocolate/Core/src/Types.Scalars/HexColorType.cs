using System;
using System.Text.RegularExpressions;
using HotChocolate.Language;

namespace HotChocolate.Types.Scalars
{
    /// <summary>
    /// The `HexColor` scalar type represents a valid HEX color code as defined in
    /// <a href="https://www.w3.org/TR/css-color-4/#hex-notation">W3 HEX notation</a>
    /// </summary>
    public class HexColorType : RegexType
    {
        private const string _validationPattern =
            "^#([A-Fa-f0-9]{6}|[A-Fa-f0-9]{3}|[A-Fa-f0-9]{8})$";

        /// <summary>
        /// Initializes a new instance of the <see cref="HexColorType"/> class.
        /// </summary>
        public HexColorType()
            : base(
                WellKnownScalarTypes.HexColor,
                _validationPattern,
                ScalarResources.HexColorType_Description,
                RegexOptions.Compiled | RegexOptions.IgnoreCase)
        {
        }

        protected override SerializationException CreateParseLiteralError(IValueNode valueSyntax)
        {
            return ThrowHelper.HexColorType_ParseLiteral_IsInvalid(this);
        }

        protected override SerializationException CreateParseValueError(object runtimeValue)
        {
            return ThrowHelper.HexColorType_ParseValue_IsInvalid(this);
        }
    }
}
