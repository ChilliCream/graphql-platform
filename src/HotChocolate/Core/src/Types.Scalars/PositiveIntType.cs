using HotChocolate.Language;
using HotChocolate.Types.Scalars;

namespace HotChocolate.Types.Scalars
{
    /// <summary>
    /// The PositiveInt scalar type represents a signed 32‐bit numeric non‐fractional
    /// value of at least the value 1.
    /// </summary>
    public class PositiveIntType : IntType
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PositiveIntType"/> class.
        /// </summary>
        public PositiveIntType()
            : this(
                WellKnownScalarTypes.PositiveInt,
                ScalarResources.PositiveIntType_Description)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PositiveIntType"/> class.
        /// </summary>
        public PositiveIntType(
            NameString name,
            string? description = null,
            BindingBehavior bind = BindingBehavior.Explicit)
            : base(name, description, min: 1, bind: bind)
        {
            Description = description;
        }

        /// <inheritdoc />
        protected override bool IsInstanceOfType(IntValueNode valueSyntax)
        {
            return IsInstanceOfType(base.ParseLiteral(valueSyntax));
        }

        /// <inheritdoc />
        protected override SerializationException CreateParseLiteralError(IValueNode valueSyntax)
        {
            throw ThrowHelper.PositiveIntType_ParseLiteral_ZeroOrLess(this);
        }

        /// <inheritdoc />
        protected override SerializationException CreateParseValueError(object runtimeValue)
        {
            throw ThrowHelper.PositiveIntType_ParseValue_ZeroOrLess(this);
        }
    }
}
