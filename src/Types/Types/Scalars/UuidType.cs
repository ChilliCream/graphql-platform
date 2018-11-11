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
                && Guid.TryParse(stringLiteral.Value, out Guid guid))
            {
                return guid;
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
            if (value == null)
            {
                return new NullValueNode(null);
            }

            if (value is Guid guid)
            {
                return new StringValueNode(Serialize(guid));
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

            if (value is Guid guid)
            {
                return Serialize(guid);
            }

            throw new ArgumentException(
                TypeResources.Scalar_Cannot_Serialize(Name));
        }

        private string Serialize(Guid value)
        {
            return value.ToString("N");
        }

        public override object Deserialize(object value)
        {
            if (value is null)
            {
                return null;
            }

            if (value is string s && Guid.TryParse(s, out Guid guid))
            {
                return guid;
            }

            throw new ArgumentException(
                TypeResources.Scalar_Cannot_Serialize(Name));
        }
    }
}
