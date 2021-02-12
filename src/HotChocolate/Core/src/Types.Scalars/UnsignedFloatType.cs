using HotChocolate.Language;

namespace HotChocolate.Types.Scalars
{
    /// <summary>
    /// The UnsignedFloatType scalar represents a double‐precision fractional value greater than
    /// or equal to 0.
    /// </summary>
    public class UnsignedFloatType : FloatType
    {
        /// <summary>
        /// Initializes a new instance of <see cref="UnsignedFloatType"/>
        /// </summary>
        public UnsignedFloatType()
            : this(
                WellKnownScalarTypes.UnsignedFloat,
                ScalarResources.UnsignedFloatType_Description)
        {
        }

        /// <summary>
        /// Initializes a new instance of <see cref="UnsignedFloatType"/>
        /// </summary>
        public UnsignedFloatType(
            NameString name,
            string? description = null,
            BindingBehavior bind = BindingBehavior.Explicit)
            : base(name, description, 0, double.MaxValue, bind)
        {
            Description = description;
        }

        /// <inheritdoc />
        protected override bool IsInstanceOfType(double runtimeValue)
        {
            return runtimeValue >= MinValue;
        }

        /// <inheritdoc />
        protected override bool IsInstanceOfType(IFloatValueLiteral valueSyntax)
        {
            return valueSyntax.ToDouble() >= MinValue;
        }

        /// <inheritdoc />
        protected override double ParseLiteral(IFloatValueLiteral valueSyntax)
        {
            if (valueSyntax.ToDouble() < MinValue)
            {
                throw ThrowHelper.UnsignedFloatType_ParseLiteral_IsNotNonNegative(this);
            }

            return base.ParseLiteral(valueSyntax);
        }

        protected override FloatValueNode ParseValue(double runtimeValue)
        {
            if (runtimeValue < MinValue)
            {
                throw ThrowHelper.UnsignedFloatType_ParseValue_IsNotNonNegative(this);
            }

            return base.ParseValue(runtimeValue);
        }
    }
}
