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
        private static readonly string _validationPattern =
            "^(?:hsla?)\\((?:\\d+%?(?:deg|rad|grad|turn)?(?:,|\\s)+){2,3}[\\s\\/]*[\\d\\.]+%?\\)";

        /// <summary>
        /// Initializes a new instance of the <see cref="HslaType"/> class.
        /// </summary>
        public HslaType()
            : base(WellKnownScalarTypes.Hsla,
                _validationPattern,
                ScalarResources.HslaType_Description,
                RegexOptions.Compiled | RegexOptions.IgnoreCase)
        {
        }

        protected override Exception CreateParseLiteralError(StringValueNode valueSyntax)
        {
            return ThrowHelper.HslaType_ParseLiteral_IsInvalid(this);
        }

        protected override Exception CreateParseValueError(string runtimeValue)
        {
            return ThrowHelper.HslaType_ParseValue_IsInvalid(this);
        }
    }
}
