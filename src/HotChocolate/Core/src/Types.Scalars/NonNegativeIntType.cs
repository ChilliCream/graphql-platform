using HotChocolate.Language;

namespace HotChocolate.Types.Scalars
{
    /// <summary>
    /// The NonNegativeIntType scalar type represents a signed 32-bit numeric non-fractional value
    /// equal to or greater than 0.
    /// </summary>
    public class NonNegativeIntType : IntType
    {
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
        protected override int ParseLiteral(IntValueNode valueSyntax)
        {
            if (valueSyntax.ToInt32() < MinValue)
            {
                throw ThrowHelper.NonNegativeIntType_ParseLiteral_IsNotNegative(this);
            }

            return base.ParseLiteral(valueSyntax);
        }

        /// <inheritdoc />
        protected override IntValueNode ParseValue(int runtimeValue)
        {
            if (runtimeValue < MinValue)
            {
                throw ThrowHelper.NonNegativeIntType_ParseValue_IsNotNegative(this);
            }

            return base.ParseValue(runtimeValue);
        }
    }
}
