using System;
using HotChocolate.Language;

namespace HotChocolate.Types
{
    public sealed class UuidType
        : ScalarType
    {
        public UuidType()
            : base("Uuid")
        {
        }

        public override Type ClrType => typeof(Guid);

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

            if (literal is StringValueNode stringLiteral
                && Guid.TryParse(stringLiteral.Value, out Guid obj))
            {
                return obj;
            }

            if (literal is NullValueNode)
            {
                return null;
            }

            throw new ArgumentException(
                "The Guid type can only parse string literals.",
                nameof(literal));
        }

        public override IValueNode ParseValue(object value)
        {
            if (value == null)
            {
                return new NullValueNode(null);
            }

            if (value is Guid guid)
            {
                return new StringValueNode(Serialize(guid));
            }

            throw new ArgumentException(
                "The specified value has to be a Guid in order " +
                "to be parsed by the Guid type.");
        }

        public override object Serialize(object value)
        {
            if (value == null)
            {
                return null;
            }

            if (value is Guid guid)
            {
                return Serialize(guid);
            }

            throw new ArgumentException(
                "The specified value cannot be serialized by the Guid type.");
        }

        private string Serialize(Guid value)
        {
            return value.ToString();
        }
    }
}
