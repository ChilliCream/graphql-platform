using HotChocolate.Language;
using HotChocolate.Types.Scalars;

namespace HotChocolate.Types
{
    /// <summary>
    /// The NonNegativeFloatType scalar represents a double‐precision fractional value greater than
    /// or equal to 0.
    /// </summary>
    public class NonNegativeFloatType : FloatType
    {
        /// <summary>
        /// Initializes a new instance of <see cref="NonNegativeFloatType"/>
        /// </summary>
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
        protected override SerializationException CreateParseLiteralError(IValueNode valueSyntax)
        {
            throw ThrowHelper.NonNegativeFloatType_ParseLiteral_IsNotNonNegative(this);
        }

        /// <inheritdoc />
        protected override SerializationException CreateParseValueError(object runtimeValue)
        {
            throw ThrowHelper.NonNegativeFloatType_ParseValue_IsNotNonNegative(this);
        }

        /// <inheritdoc />
        protected override SerializationException CreateParseResultError(object runtimeValue)
        {
            throw ThrowHelper.NonNegativeFloatType_ParseValue_IsNotNonNegative(this);
        }
    }
}
