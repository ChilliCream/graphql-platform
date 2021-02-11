using HotChocolate.Language;

namespace HotChocolate.Types.Scalars
{
    /// <summary>
    /// The NonPositiveIntType scalar type represents a signed 32-bit numeric non-fractional value
    /// less than or equal to 0.
    /// </summary>
    public class NonPositiveIntType : IntType
    {
        public NonPositiveIntType()
            : this(
                WellKnownScalarTypes.NonPositiveInt,
                ScalarResources.NonPositiveIntType_Description)
        {
        }

        /// <summary>
        /// Initializes a new instance of <see cref="NonPositiveIntType"/>
        /// </summary>
        public NonPositiveIntType(
            NameString name,
            string? description = null,
            BindingBehavior bind = BindingBehavior.Explicit)
            : base(name, description, int.MinValue, 0, bind)
        {
            Description = description;
        }

        /// <inheritdoc />
        protected override bool IsInstanceOfType(int runtimeValue)
        {
            return runtimeValue <= MaxValue;
        }

        /// <inheritdoc />
        protected override bool IsInstanceOfType(IntValueNode valueSyntax)
        {
            return valueSyntax.ToInt32() <= MaxValue;
        }

        /// <inheritdoc />
        protected override int ParseLiteral(IntValueNode valueSyntax)
        {
            if (valueSyntax.ToInt32() > MaxValue)
            {
                throw ThrowHelper.NonPositiveIntType_ParseLiteral_IsNotNonPositive(this);
            }

            return base.ParseLiteral(valueSyntax);
        }

        protected override IntValueNode ParseValue(int runtimeValue)
        {
            if (runtimeValue > MaxValue)
            {
                throw ThrowHelper.NonPositiveIntType_ParseValue_IsNotNonPositive(this);
            }

            return base.ParseValue(runtimeValue);
        }
    }
}
