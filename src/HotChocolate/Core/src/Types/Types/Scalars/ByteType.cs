using HotChocolate.Language;
using HotChocolate.Properties;

#nullable enable

namespace HotChocolate.Types
{
    public class ByteType : IntegerTypeBase<byte>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ByteType"/> class.
        /// </summary>
        public ByteType()
            : this(byte.MinValue, byte.MaxValue)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ByteType"/> class.
        /// </summary>
        public ByteType(byte min, byte max)
            : this(
                ScalarNames.Byte,
                TypeResources.ByteType_Description,
                min,
                max,
                BindingBehavior.Implicit)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ByteType"/> class.
        /// </summary>
        public ByteType(
            NameString name,
            string? description = null,
            byte min = byte.MinValue,
            byte max = byte.MaxValue,
            BindingBehavior bind = BindingBehavior.Explicit)
            : base(name, min, max, bind)
        {
            Description = description;
        }

        protected override byte ParseLiteral(IntValueNode valueSyntax) =>
            valueSyntax.ToByte();

        protected override IntValueNode ParseValue(byte runtimeValue) =>
            new(runtimeValue);
    }
}
