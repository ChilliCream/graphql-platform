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
            : base(ScalarNames.ByteArray, BindingBehavior.Implicit)
        {
        }

        protected override byte[] ParseLiteral(StringValueNode literal)
        {
            return Convert.FromBase64String(literal.Value);
        }

        protected override StringValueNode ParseValue(byte[] value)
        {
            return new StringValueNode(Convert.ToBase64String(value));
        }

        public override bool TrySerialize(object value, out object serialized)
        {
            if (value is null)
            {
                serialized = null;
                return true;
            }

            if (value is byte[] b)
            {
                serialized = Convert.ToBase64String(b);
                return true;
            }

            serialized = null;
            return false;
        }

        public override bool TryDeserialize(object serialized, out object value)
        {
            if (serialized is null)
            {
                value = null;
                return true;
            }

            if (serialized is string s)
            {
                value = Convert.FromBase64String(s);
                return true;
            }

            if (serialized is byte[] b)
            {
                value = b;
                return true;
            }

            value = null;
            return false;
        }
    }
}
