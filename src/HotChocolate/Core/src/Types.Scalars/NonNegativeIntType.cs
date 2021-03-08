using HotChocolate.Language;

namespace HotChocolate.Types.Scalars
{
    /// <summary>
    /// The NonNegativeIntType scalar type represents a unsigned 32-bit numeric non-fractional value
    /// greater than or equal to 0.
    /// </summary>
    public class NonNegativeIntType : IntType
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="NonNegativeIntType"/> class.
        /// </summary>
        public NonNegativeIntType()
            : this(
                WellKnownScalarTypes.NonNegativeInt,
                ScalarResources.NonNegativeIntType_Description)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="NonNegativeIntType"/> class.
        /// </summary>
        public NonNegativeIntType(
            NameString name,
            string? description = null,
            BindingBehavior bind = BindingBehavior.Explicit)
            : base(name, description, 0, int.MaxValue, bind)
        {
            Description = description;
        }

        /// <inheritdoc />
        protected override bool IsInstanceOfType(int runtimeValue)
        {
            return runtimeValue >= MinValue;
        }

        /// <inheritdoc />
        protected override bool IsInstanceOfType(IntValueNode valueSyntax)
        {
            return valueSyntax.ToInt32() >= MinValue;
        }

        /// <inheritdoc />
        protected override SerializationException CreateParseLiteralError(IValueNode valueSyntax)
        {
            throw ThrowHelper.NonNegativeIntType_ParseLiteral_IsNotNonNegative(this);
        }

        /// <inheritdoc />
        protected override SerializationException CreateParseValueError(object runtimeValue)
        {
            throw ThrowHelper.NonNegativeIntType_ParseValue_IsNotNonNegative(this);
        }

        /// <inheritdoc />
        protected override SerializationException CreateParseResultError(object runtimeValue)
        {
            throw ThrowHelper.NonNegativeIntType_ParseValue_IsNotNonNegative(this);
        }
    }
}
