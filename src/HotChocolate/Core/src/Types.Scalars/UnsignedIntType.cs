using HotChocolate.Language;

namespace HotChocolate.Types.Scalars
{
    /// <summary>
    /// The UnsignedInt scalar type represents a unsigned 32‐bit numeric non‐fractional
    /// value greater than or equal to 0.
    /// </summary>
    public class UnsignedIntType : IntType
    {
        public UnsignedIntType()
            : this(
                WellKnownScalarTypes.UnsignedInt,
                ScalarResources.UnsignedIntType_Description)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="UnsignedIntType"/> class.
        /// </summary>
        public UnsignedIntType(
            NameString name,
            string? description = null,
            BindingBehavior bind = BindingBehavior.Explicit)
            : base(name, description, min: 0, int.MaxValue, bind)
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
                throw ThrowHelper.UnsignedIntType_ParseLiteral_IsNotUnsigned(this);
            }

            return base.ParseLiteral(valueSyntax);
        }

        /// <inheritdoc />
        protected override IntValueNode ParseValue(int runtimeValue)
        {
            if (runtimeValue < MinValue)
            {
                throw ThrowHelper.UnsignedIntType_ParseValue_IsNotUnsigned(this);
            }

            return base.ParseValue(runtimeValue);
        }
    }
}
