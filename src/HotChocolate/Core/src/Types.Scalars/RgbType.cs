using System;
using System.Text.RegularExpressions;
using HotChocolate.Language;

namespace HotChocolate.Types
{
    /// <summary>
    /// The `Rgb` scalar type represents a valid CSS RGB color
    /// <a href="https://developer.mozilla.org/en-US/docs/Web/CSS/color_value#rgb()_and_rgba()">
    /// MDN CSS Color
    /// </a>
    /// </summary>
    public class RgbType : RegexType
    {
        private const string _validationPattern =
            "^rgb\\(\\s*(-?\\d+|-?\\d*\\.\\d+(?=%))(%?)\\s*,\\s*(-?\\d+|-?\\d*\\.\\d+(?=%))("+
            "\\2)\\s*,\\s*(-?\\d+|-?\\d*\\.\\d+(?=%))(\\2)\\s*\\)$";

        /// <summary>
        /// Initializes a new instance of the <see cref="RgbType"/> class.
        /// </summary>
        public RgbType()
            : base(
                WellKnownScalarTypes.Rgb,
                _validationPattern,
                ScalarResources.RgbType_Description,
                RegexOptions.Compiled | RegexOptions.IgnoreCase)
        {
        }

        /// <inheritdoc />
        protected override SerializationException CreateParseLiteralError(IValueNode valueSyntax)
        {
            return ThrowHelper.RgbType_ParseLiteral_IsInvalid(this);
        }

        /// <inheritdoc />
        protected override SerializationException CreateParseValueError(object runtimeValue)
        {
            return ThrowHelper.RgbType_ParseValue_IsInvalid(this);
        }
    }
}
