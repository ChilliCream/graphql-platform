using System;
using HotChocolate.Language;
using HotChocolate.Properties;

namespace HotChocolate.Types
{
    public class NonNullType
        : NonNamedType
    {
        public NonNullType(IType type)
            : base(type)
        {
            if (!(type is INullableType))
            {
                throw new ArgumentException(
                    TypeResources.NonNullType_TypeIsNunNullType,
                    nameof(type));
            }
        }

        public override TypeKind Kind => TypeKind.NonNull;

        public IType Type => InnerType;

        protected sealed override bool IsInstanceOfType(IValueNode literal)
        {
            if (literal is NullValueNode)
            {
                return false;
            }

            return InnerInputType.IsInstanceOfType(literal);
        }

        protected sealed override object ParseLiteral(IValueNode literal)
        {
            if (literal is NullValueNode)
            {
                throw new ArgumentException(
                    TypeResources.NonNullType_ValueIsNull,
                    nameof(literal));
            }

            return InnerInputType.ParseLiteral(literal);
        }

        protected sealed override bool IsInstanceOfType(object value)
        {
            if (value is null)
            {
                return false;
            }

            return InnerInputType.IsInstanceOfType(value);
        }

        protected sealed override IValueNode ParseValue(object value)
        {
            if (value == null)
            {
                throw new ArgumentException(
                    TypeResources.NonNullType_ValueIsNull,
                    nameof(value));
            }

            return InnerInputType.ParseValue(value);
        }

        protected sealed override bool TrySerialize(
            object value, out object serialized)
        {
            if (value != null)
            {
                serialized = InnerInputType.Serialize(value);
                return true;
            }

            serialized = null;
            return false;
        }

        protected sealed override bool TryDeserialize(
            object serialized, out object value)
        {
            if (serialized != null)
            {
                return InnerInputType.TryDeserialize(serialized, out value);
            }

            value = null;
            return false;
        }
    }
}
