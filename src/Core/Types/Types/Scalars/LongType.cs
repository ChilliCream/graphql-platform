using System;
using System.Globalization;
using HotChocolate.Language;

namespace HotChocolate.Types
{
    public sealed class LongType
        : ScalarType
    {
        public LongType()
            : base("Long")
        {
        }

        public override Type ClrType => typeof(long);

        public override bool IsInstanceOfType(IValueNode literal)
        {
            if (literal == null)
            {
                throw new ArgumentNullException(nameof(literal));
            }

            return literal is StringValueNode
                || literal is NullValueNode;
        }

        public override object ParseLiteral(IValueNode literal)
        {
            if (literal == null)
            {
                throw new ArgumentNullException(nameof(literal));
            }

            if (literal is StringValueNode stringLiteral)
            {
                return long.Parse(
                    stringLiteral.Value,
                    NumberStyles.Integer,
                    CultureInfo.InvariantCulture);
            }

            if (literal is NullValueNode)
            {
                return null;
            }

            throw new ArgumentException(
                TypeResources.Scalar_Cannot_ParseLiteral(
                    Name, literal.GetType()),
                nameof(literal));
        }

        public override IValueNode ParseValue(object value)
        {
            if (value is null)
            {
                return NullValueNode.Default;
            }

            if (value is long l)
            {
                return new StringValueNode(
                    l.ToString("D", CultureInfo.InvariantCulture));
            }

            throw new ArgumentException(
                TypeResources.Scalar_Cannot_ParseValue(
                    Name, value.GetType()),
                nameof(value));
        }

        public override object Serialize(object value)
        {
            if (value == null)
            {
                return null;
            }

            if (value is long l)
            {
                return l.ToString("D", CultureInfo.InvariantCulture);
            }

            throw new ArgumentException(
                TypeResources.Scalar_Cannot_Serialize(Name));
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
                value = long.Parse(
                    s,
                    NumberStyles.Integer,
                    CultureInfo.InvariantCulture);
                return true;
            }

            value = null;
            return false;
        }
    }
}
