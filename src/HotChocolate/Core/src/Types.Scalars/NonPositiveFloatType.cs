using HotChocolate.Language;
using HotChocolate.Types.Scalars;

namespace HotChocolate.Types
{
    /// <summary>
    /// The NonPositiveFloat scalar type represents a double‐precision fractional value less than or
    /// equal to 0.
    /// </summary>
    public class NonPositiveFloatType : FloatType
    {
        /// <summary>
        /// Initializes a new instance of <see cref="NonPositiveFloatType"/>
        /// </summary>
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
        protected override SerializationException CreateParseLiteralError(IValueNode valueSyntax)
        {
            throw ThrowHelper.NonPositiveFloatType_ParseLiteral_IsNotNonPositive(this);
        }

        /// <inheritdoc />
        protected override SerializationException CreateParseValueError(object runtimeValue)
        {
            throw ThrowHelper.NonPositiveFloatType_ParseValue_IsNotNonPositive(this);
        }

        /// <inheritdoc />
        protected override SerializationException CreateParseResultError(object runtimeValue)
        {
            throw ThrowHelper.NonPositiveFloatType_ParseValue_IsNotNonPositive(this);
        }
    }
}
