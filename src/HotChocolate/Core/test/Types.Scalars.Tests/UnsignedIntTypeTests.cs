using HotChocolate.Language;

namespace HotChocolate.Types.Scalars
{
    /// <summary>
    /// The UnsignedInt scalar type represents a unsigned 32‐bit numeric non‐fractional
    /// value greater than or equal to 0.
    /// </summary>
    public class UnsignedIntType : IntegerTypeBase<uint>
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
            : base(name, uint.MinValue, uint.MaxValue, bind)
        {
            Description = description;
        }

        /// <inheritdoc />
        protected override bool IsInstanceOfType(uint runtimeValue)
        {
            return runtimeValue >= MinValue;
        }

        /// <inheritdoc />
        protected override bool IsInstanceOfType(IntValueNode valueSyntax)
        {
            return valueSyntax.ToUInt32() >= MinValue;
        }

        /// <inheritdoc />
        protected override uint ParseLiteral(IntValueNode valueSyntax)
        {
            if (valueSyntax.ToUInt32() < MinValue)
            {
                throw ThrowHelper.UnsignedIntType_ParseLiteral_IsNotUnsigned(this);
            }

            return valueSyntax.ToUInt32();
        }

        /// <inheritdoc />
        protected override IntValueNode ParseValue(uint runtimeValue)
        {
            if (runtimeValue < MinValue)
            {
                throw ThrowHelper.UnsignedIntType_ParseValue_IsNotUnsigned(this);
            }

            return new IntValueNode(runtimeValue);
        }
    }
}
