using HotChocolate.Language;
using HotChocolate.Types.Scalars;

namespace HotChocolate.Types
{
    /// <summary>
    /// The NegativeIntType scalar type represents a signed 32-bit numeric non-fractional with a
    /// maximum of -1.
    /// </summary>
    public class NegativeIntType : IntType
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="NegativeIntType"/> class.
        /// </summary>
        public NegativeIntType()
            : this(
                WellKnownScalarTypes.NegativeInt,
                ScalarResources.NegativeIntType_Description)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="NegativeIntType"/> class.
        /// </summary>
        public NegativeIntType(
            NameString name,
            string? description = null,
            BindingBehavior bind = BindingBehavior.Explicit)
            : base(name, description, int.MinValue, -1, bind)
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
                throw ThrowHelper.NegativeIntType_ParseLiteral_IsNotNegative(this);
            }

            return base.ParseLiteral(valueSyntax);
        }

        /// <inheritdoc />
        protected override IntValueNode ParseValue(int runtimeValue)
        {
            if (runtimeValue > MaxValue)
            {
                throw ThrowHelper.NegativeIntType_ParseValue_IsNotNegative(this);
            }

            return base.ParseValue(runtimeValue);
        }
    }
}
