using System.Text.RegularExpressions;
using HotChocolate.Language;

namespace HotChocolate.Types.Scalars
{
    /// <summary>
    /// The `Rgba` scalar type represents a valid CSS RGBA color
    /// <a href="https://developer.mozilla.org/en-US/docs/Web/CSS/color_value#rgb()_and_rgba()"></a>
    /// </summary>
    public class RgbaType : RegexType
    {
        private const string _validationPattern =
            "((?:rgba?)\\((?:\\d+%?(?:,|\\s)+){2,3}[\\s\\/]*[\\d\\.]+%?\\))";

        /// <summary>
        /// Initializes a new instance of the <see cref="RgbaType"/> class.
        /// </summary>
        public RgbaType()
            : this(
                WellKnownScalarTypes.Rgba,
                ScalarResources.RgbaType_Description)
        {
        }
        
         /// <summary>
        /// Initializes a new instance of the <see cref="RgbaType"/> class.
        /// </summary>
        public RgbaType(
            NameString name,
            string? description = null,
            BindingBehavior bind = BindingBehavior.Explicit)
            : base(name,
                _validationPattern,
                ScalarResources.RgbaType_Description,
                RegexOptions.Compiled | RegexOptions.IgnoreCase)
        {
            Description = description;
        }

        /// <inheritdoc />
        protected override SerializationException CreateParseLiteralError(IValueNode valueSyntax)
        {
            return ThrowHelper.RgbaType_ParseLiteral_IsInvalid(this);
        }

        /// <inheritdoc />
        protected override SerializationException CreateParseValueError(object runtimeValue)
        {
            return ThrowHelper.RgbaType_ParseValue_IsInvalid(this);
        }
    }
}
