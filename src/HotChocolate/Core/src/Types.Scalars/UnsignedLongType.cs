using HotChocolate.Language;
using HotChocolate.Types.Scalars;

namespace HotChocolate.Types
{
    /// <summary>
    /// The `UnsignedLong` scalar type represents a signed 64‐bit numeric non‐fractional
    /// value greater than or equal to 0.
    /// </summary>
    public class UnsignedLongType : IntegerTypeBase<ulong>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="UnsignedLongType"/> class.
        /// </summary>
        public UnsignedLongType()
            : this(
                WellKnownScalarTypes.UnsignedLong,
                ScalarResources.UnsignedLongType_Description)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="UnsignedLongType"/> class.
        /// </summary>
        public UnsignedLongType(
            NameString name,
            string? description = null,
            BindingBehavior bind = BindingBehavior.Explicit)
            : base(name, ulong.MinValue, ulong.MaxValue, bind)
        {
            Description = description;
        }

        /// <inheritdoc />
        protected override bool IsInstanceOfType(ulong runtimeValue)
        {
            return runtimeValue >= MinValue;
        }

        /// <inheritdoc />
        protected override bool IsInstanceOfType(IntValueNode valueSyntax)
        {
            return valueSyntax.ToUInt64() >= MinValue;
        }

        /// <inheritdoc />
        protected override ulong ParseLiteral(IntValueNode valueSyntax)
        {
            return valueSyntax.ToUInt64();
        }

        /// <inheritdoc />
        protected override IntValueNode ParseValue(ulong runtimeValue)
        {
            return new(runtimeValue);
        }

        /// <inheritdoc />
        protected override SerializationException CreateParseLiteralError(IValueNode valueSyntax)
        {
            throw ThrowHelper.UnsignedLongType_ParseLiteral_IsNotUnsigned(this);
        }

        /// <inheritdoc />
        protected override SerializationException CreateParseValueError(object runtimeValue)
        {
            throw ThrowHelper.UnsignedLongType_ParseValue_IsNotUnsigned(this);
        }

        /// <inheritdoc />
        protected override SerializationException CreateParseResultError(object runtimeValue)
        {
            throw ThrowHelper.UnsignedLongType_ParseValue_IsNotUnsigned(this);
        }
    }
}
