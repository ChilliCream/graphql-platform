using HotChocolate.Language;
using HotChocolate.Types.Scalars;

namespace HotChocolate.Types
{
    /// <summary>
    /// The NonPositiveIntType scalar type represents a signed 32-bit numeric non-fractional value
    /// less than or equal to 0.
    /// </summary>
    public class NonPositiveIntType : IntType
    {
        /// <summary>
        /// Initializes a new instance of <see cref="NonPositiveIntType"/>
        /// </summary>
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
        protected override SerializationException CreateParseLiteralError(IValueNode valueSyntax)
        {
            throw ThrowHelper.NonPositiveIntType_ParseLiteral_IsNotNonPositive(this);
        }

        /// <inheritdoc />
        protected override SerializationException CreateParseValueError(object runtimeValue)
        {
            throw ThrowHelper.NonPositiveIntType_ParseValue_IsNotNonPositive(this);
        }

        /// <inheritdoc />
        protected override SerializationException CreateParseResultError(object runtimeValue)
        {
            throw ThrowHelper.NonPositiveIntType_ParseValue_IsNotNonPositive(this);
        }
    }
}
