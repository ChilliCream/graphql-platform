using System;
using HotChocolate.Language;

namespace HotChocolate.Types.Scalars
{
    public class NegativeFloatType : FloatType
    {
        public NegativeFloatType()
            : this(
                WellKnownScalarTypes.NegativeFloat,
                ScalarResources.NegativeFloatType_Description)
        {
        }

        /// <summary>
        /// Initializes a new instance of <see cref="NegativeFloatType"/>
        /// </summary>
        public NegativeFloatType(
            NameString name,
            string? description = null,
            BindingBehavior bind = BindingBehavior.Explicit)
            : base(name, description, double.MinValue, -0, bind)
        {
            Description = description;
        }

        /// <inheritdoc />
        protected override bool IsInstanceOfType(double runtimeValue)
        {
            return runtimeValue < MaxValue;
        }

        /// <inheritdoc />
        protected override bool IsInstanceOfType(IFloatValueLiteral valueSyntax)
        {
            return valueSyntax.ToDouble() < MaxValue;
        }

        /// <inheritdoc />
        protected override double ParseLiteral(IFloatValueLiteral valueSyntax)
        {
            if (valueSyntax.ToDouble() > MaxValue)
            {
                throw ThrowHelper.NegativeFloatType_ParseLiteral_IsNotNegative(this);
            }

            return base.ParseLiteral(valueSyntax);
        }

        protected override FloatValueNode ParseValue(double runtimeValue)
        {
            if (runtimeValue > MaxValue)
            {
                throw ThrowHelper.NegativeFloatType_ParseValue_IsNotNegative(this);
            }

            return base.ParseValue(runtimeValue);
        }
    }
}
