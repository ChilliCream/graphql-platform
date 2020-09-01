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

        public override bool TryDeserialize(object resultValue, out object runtimeValue)
        {
            if (resultValue is null)
            {
                runtimeValue = null;
                return true;
            }

            if (resultValue is string s)
            {
                runtimeValue = Convert.FromBase64String(s);
                return true;
            }

            if (resultValue is byte[] b)
            {
                runtimeValue = b;
                return true;
            }

            runtimeValue = null;
            return false;
        }
    }
}
