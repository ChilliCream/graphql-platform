using HotChocolate.Language;

namespace HotChocolate.Types
{
    /// <summary>
    /// The UnsignedInt scalar type represents a signed byte numeric non‐fractional
    /// value greater than or equal to -127.
    /// </summary>
    public class SignedByteType : IntegerTypeBase<sbyte>
    {
        public SignedByteType()
            : this(
                WellKnownScalarTypes.SignedByte,
                ScalarResources.SignedByteType_Description)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SignedByteType"/> class.
        /// </summary>
        public SignedByteType(
            NameString name,
            string? description = null,
            BindingBehavior bind = BindingBehavior.Explicit)
            : base(name, sbyte.MinValue, sbyte.MaxValue, bind)
        {
            Description = description;
        }

        /// <inheritdoc />
        protected override bool IsInstanceOfType(sbyte runtimeValue)
        {
            return runtimeValue >= MinValue;
        }

        /// <inheritdoc />
        protected override bool IsInstanceOfType(IntValueNode valueSyntax)
        {
            return valueSyntax.ToSByte() >= MinValue;
        }

        /// <inheritdoc />
        protected override sbyte ParseLiteral(IntValueNode valueSyntax)
        {
            return valueSyntax.ToSByte();
        }

        /// <inheritdoc />
        protected override IntValueNode ParseValue(sbyte runtimeValue)
        {
            return new(runtimeValue);
        }

        /// <inheritdoc />
        protected override SerializationException CreateParseLiteralError(IValueNode valueSyntax)
        {
            throw ThrowHelper.UnsignedIntType_ParseLiteral_IsNotUnsigned(this);
        }

        /// <inheritdoc />
        protected override SerializationException CreateParseValueError(object runtimeValue)
        {
            throw ThrowHelper.UnsignedIntType_ParseValue_IsNotUnsigned(this);
        }

        /// <inheritdoc />
        protected override SerializationException CreateParseResultError(object runtimeValue)
        {
            throw ThrowHelper.UnsignedIntType_ParseValue_IsNotUnsigned(this);
        }
    }
}
