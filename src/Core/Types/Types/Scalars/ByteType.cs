using HotChocolate.Language;
using HotChocolate.Properties;

namespace HotChocolate.Types
{
    public sealed class ByteType
        : IntegerTypeBase<byte>
    {
        public ByteType()
            : this(byte.MinValue, byte.MaxValue)
        {
        }

        public ByteType(byte min, byte max)
            : this(ScalarNames.Byte, min, max)
        {
            Description = TypeResources.ByteType_Description;
        }

        public ByteType(NameString name)
            : this(name, byte.MinValue, byte.MaxValue)
        {
        }

        public ByteType(NameString name, byte min, byte max)
            : base(name, min, max)
        {
        }

        public ByteType(NameString name, string description, byte min, byte max)
            : base(name, min, max)
        {
            Description = description;
        }

        protected override byte ParseLiteral(IntValueNode literal)
        {
            return literal.ToByte();
        }

        protected override IntValueNode ParseValue(byte value)
        {
            return new IntValueNode(value);
        }
    }
}
