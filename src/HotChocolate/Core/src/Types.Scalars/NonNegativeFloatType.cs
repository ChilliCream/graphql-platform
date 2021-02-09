using System;
using HotChocolate.Language;

namespace HotChocolate.Types.Scalars
{
    /// <summary>
    /// The NonNegativeFloatType scalar represents a double‐precision fractional value greater than
    /// or equal to 0.
    /// </summary>
    public class NonNegativeFloatType : FloatType
    {
        public NonNegativeFloatType()
            : this(
                WellKnownScalarTypes.NonNegativeFloat,
                ScalarResources.NonNegativeFloatType_Description)
        {
        }

        /// <summary>
        /// Initializes a new instance of <see cref="NonNegativeFloatType"/>
        /// </summary>
        public NonNegativeFloatType(
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
                throw ThrowHelper.NonNegativeFloatType_ParseLiteral_IsEmpty(this);
            }

            return base.ParseLiteral(valueSyntax);
        }

        protected override FloatValueNode ParseValue(double runtimeValue)
        {
            if (runtimeValue < MinValue)
            {
                throw ThrowHelper.NonNegativeFloatType_ParseValue_IsEmpty(this);
            }

            return base.ParseValue(runtimeValue);
        }
    }
}
