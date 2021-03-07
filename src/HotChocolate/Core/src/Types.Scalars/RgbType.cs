using System;
using System.Text.RegularExpressions;
using HotChocolate.Language;

namespace HotChocolate.Types.Scalars
{
    /// <summary>
    /// The `Rgb` scalar type represents a valid CSS RGB color
    /// <a href="https://developer.mozilla.org/en-US/docs/Web/CSS/color_value#rgb()_and_rgba()">MDN CSS Color</a>
    /// </summary>
    public class RgbType : RegexType
    {
        private const string _validationPattern =
            "/^rgb\\(\\s*(-?\\d+|-?\\d*\\.\\d+(?=%))(%?)\\s*,\\s*(-?\\d+|-?\\d*\\.\\d+(?=%))" +
            "(\\2)\\s*,\\s*(-?\\d+|-?\\d*\\.\\d+(?=%))(\\2)\\s*\\)$/";

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

        protected override Exception CreateParseLiteralError(StringValueNode valueSyntax)
        {
            return ThrowHelper.RgbType_ParseLiteral_IsInvalid(this);
        }

        protected override Exception CreateParseValueError(string runtimeValue)
        {
            return ThrowHelper.RgbType_ParseValue_IsInvalid(this);
        }
    }
}
