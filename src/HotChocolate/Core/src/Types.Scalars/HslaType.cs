using System;
using System.Text.RegularExpressions;
using HotChocolate.Language;

namespace HotChocolate.Types.Scalars
{
    /// <summary>
    /// The `HSLA` scalar type represents a valid a CSS HSLA color as defined
    /// in <a href="https://www.w3.org/TR/css-color-3/#hsla-color">W3 HSLA Color</a>
    /// </summary>
    public class HslaType : RegexType
    {
        private const string _validationPattern =
            "^(?:hsla?)\\((?:\\d+%?(?:deg|rad|grad|turn)?(?:,|\\s)+){2,3}[\\s\\/]*[\\d\\.]+%?\\)";

        /// <summary>
        /// Initializes a new instance of the <see cref="HslaType"/> class.
        /// </summary>
        public HslaType()
            : base(
                WellKnownScalarTypes.Hsla,
                _validationPattern,
                ScalarResources.HslaType_Description,
                RegexOptions.Compiled | RegexOptions.IgnoreCase)
        {
        }

        /// <inheritdoc />
        protected override SerializationException CreateParseLiteralError(IValueNode valueSyntax)
        {
            return ThrowHelper.HslaType_ParseLiteral_IsInvalid(this);
        }

        /// <inheritdoc />
        protected override SerializationException CreateParseValueError(object runtimeValue)
        {
            return ThrowHelper.HslaType_ParseValue_IsInvalid(this);
        }
    }
}
