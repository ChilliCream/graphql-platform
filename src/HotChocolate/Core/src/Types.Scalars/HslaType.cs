using System.Text.RegularExpressions;
using HotChocolate.Language;

namespace HotChocolate.Types.Scalars
{
    /// <summary>
    /// The `Hsla` scalar type represents a valid a Css Hsla color as defined
    /// here https://developer.mozilla.org/en-US/docs/Web/CSS/color_value#hsl_colors.
    /// </summary>
    public class HslaType : HslType
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="HslaType"/> class.
        /// </summary>
        public HslaType()
            : this(
                WellKnownScalarTypes.Hsla,
                ScalarResources.HslaType_Description)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="HslaType"/> class.
        /// </summary>
        public HslaType(
            NameString name,
            string? description = null,
            BindingBehavior bind = BindingBehavior.Explicit)
            : base(name, description, bind)
        {
            Description = description;
        }

        /// <inheritdoc />
        protected override string ParseLiteral(StringValueNode valueSyntax)
        {
            if (!ValidationRegex.IsMatch(valueSyntax.Value))
            {
                throw ThrowHelper.HslaType_ParseLiteral_IsInvalid(this);
            }

            return base.ParseLiteral(valueSyntax);
        }

        /// <inheritdoc />
        protected override StringValueNode ParseValue(string runtimeValue)
        {
            if (!ValidationRegex.IsMatch(runtimeValue))
            {
                throw ThrowHelper.HslaType_ParseValue_IsInvalid(this);
            }

            return base.ParseValue(runtimeValue);
        }
    }
}
