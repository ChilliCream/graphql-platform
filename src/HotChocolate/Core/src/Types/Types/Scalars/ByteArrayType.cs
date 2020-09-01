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

        protected override byte[] ParseLiteral(StringValueNode valueSyntax)
        {
            return Convert.FromBase64String(valueSyntax.Value);
        }

        protected override StringValueNode ParseValue(byte[] runtimeValue)
        {
            return new StringValueNode(Convert.ToBase64String(runtimeValue));
        }

        public override bool TrySerialize(object runtimeValue, out object resultValue)
        {
            if (runtimeValue is null)
            {
                resultValue = null;
                return true;
            }

            if (runtimeValue is byte[] b)
            {
                resultValue = Convert.ToBase64String(b);
                return true;
            }

            resultValue = null;
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
