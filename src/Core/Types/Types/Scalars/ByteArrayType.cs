using System;
using HotChocolate.Language;

namespace HotChocolate.Types
{
    public sealed class ByteArrayType
        : ScalarType<byte[], StringValueNode>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ByteArrayType"/> class.
        /// </summary>
        public ByteArrayType()
            : base(ScalarNames.ByteArray)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ByteArrayType"/> class.
        /// </summary>
        public ByteArrayType(NameString name)
            : base(name)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ByteArrayType"/> class.
        /// </summary>
        public ByteArrayType(NameString name, string description)
            : base(name)
        {
            Description = description;
        }

        protected override byte[] ParseLiteral(StringValueNode literal)
        {
            return Convert.FromBase64String(literal.Value);
        }

        protected override StringValueNode ParseValue(byte[] value)
        {
            return new StringValueNode(Convert.ToBase64String(value));
        }
    }
}
