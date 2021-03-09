using HotChocolate.Language;

namespace HotChocolate.Types.Scalars
{
    /// <summary>
    /// The `Port` scalar type represents a field whose value is a valid TCP port within the
    /// range of 0 to 65535 as defined here:
    /// <a>https://en.wikipedia.org/wiki/Transmission_Control_Protocol#TCP_ports</a>
    /// </summary>
    public class PortType : IntType
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PortType"/> class.
        /// </summary>
        public PortType()
            : this(
                WellKnownScalarTypes.Port,
                ScalarResources.PortType_Description)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PortType"/> class.
        /// </summary>
        public PortType(
            NameString name,
            string? description = null,
            BindingBehavior bind = BindingBehavior.Explicit)
            : base(name, description, 0, 65535, bind)
        {
            Description = description;
        }

        /// <inheritdoc />
        protected override bool IsInstanceOfType(int runtimeValue)
        {
            return runtimeValue >= MinValue && runtimeValue <= MaxValue;
        }

        /// <inheritdoc />
        protected override bool IsInstanceOfType(IntValueNode valueSyntax)
        {
            return valueSyntax.ToInt32() >= MinValue && valueSyntax.ToInt32() <= MaxValue;
        }

        /// <inheritdoc />
        protected override SerializationException CreateParseLiteralError(IValueNode valueSyntax)
        {
            throw ThrowHelper.PortType_ParseLiteral_OutOfRange(this);
        }

        /// <inheritdoc />
        protected override SerializationException CreateParseValueError(object runtimeValue)
        {
            throw ThrowHelper.PortType_ParseValue_OutOfRange(this);
        }

        /// <inheritdoc />
        protected override SerializationException CreateParseResultError(object runtimeValue)
        {
            throw ThrowHelper.PortType_ParseValue_OutOfRange(this);
        }
    }
}
