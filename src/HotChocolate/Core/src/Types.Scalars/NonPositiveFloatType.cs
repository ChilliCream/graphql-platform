using HotChocolate.Language;

namespace HotChocolate.Types.Scalars
{
    /// <summary>
    /// The NonPositiveFloat scalar type represents a double‐precision fractional value less than or
    /// equal to 0.
    /// </summary>
    public class NonPositiveFloatType : FloatType
    {
        public NonPositiveFloatType()
            : this(
                WellKnownScalarTypes.NonPositiveFloat,
                ScalarResources.NonPositiveFloatType_Description)
        {
        }

        /// <summary>
        /// Initializes a new instance of <see cref="NonPositiveFloatType"/>
        /// </summary>
        public NonPositiveFloatType(
            NameString name,
            string? description = null,
            BindingBehavior bind = BindingBehavior.Explicit)
            : base(name, description, double.MinValue, 0, bind)
        {
            Description = description;
        }

        /// <inheritdoc />
        protected override bool IsInstanceOfType(double runtimeValue)
        {
            return runtimeValue <= MaxValue;
        }

        /// <inheritdoc />
        protected override bool IsInstanceOfType(IFloatValueLiteral valueSyntax)
        {
            return valueSyntax.ToDouble() <= MaxValue;
        }

        /// <inheritdoc />
        protected override double ParseLiteral(IFloatValueLiteral valueSyntax)
        {
            if (valueSyntax.ToDouble() > MaxValue)
            {
                throw ThrowHelper.NonPositiveFloatType_ParseLiteral_IsNotNonPositive(this);
            }

            return base.ParseLiteral(valueSyntax);
        }

        protected override FloatValueNode ParseValue(double runtimeValue)
        {
            if (runtimeValue > MaxValue)
            {
                throw ThrowHelper.NonPositiveFloatType_ParseValue_IsNotNonPositive(this);
            }

            return base.ParseValue(runtimeValue);
        }
    }
}
