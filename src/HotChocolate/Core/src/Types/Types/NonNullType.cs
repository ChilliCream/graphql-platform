using System;
using HotChocolate.Language;
using HotChocolate.Properties;

#nullable enable

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

            return InnerInputType!.IsInstanceOfType(literal);
        }

        protected sealed override object? ParseLiteral(IValueNode literal, bool withDefaults)
        {
            if (literal is NullValueNode)
            {
                throw new ArgumentException(
                    TypeResources.NonNullType_ValueIsNull,
                    nameof(literal));
            }

            return InnerInputType!.ParseLiteral(literal, withDefaults);
        }

        protected sealed override bool IsInstanceOfType(object? runtimeValue)
        {
            if (runtimeValue is null)
            {
                return false;
            }

            return InnerInputType!.IsInstanceOfType(runtimeValue);
        }

        protected sealed override IValueNode ParseValue(object? runtimeValue)
        {
            if (runtimeValue is null)
            {
                throw new ArgumentException(
                    TypeResources.NonNullType_ValueIsNull,
                    nameof(runtimeValue));
            }

            return InnerInputType!.ParseValue(runtimeValue);
        }

        protected override IValueNode ParseResult(object? resultValue)
        {
            if (resultValue is null)
            {
                throw new ArgumentException(
                    TypeResources.NonNullType_ValueIsNull,
                    nameof(resultValue));
            }

            return InnerInputType!.ParseResult(resultValue);
        }

        protected sealed override bool TrySerialize(object? runtimeValue, out object? resultValue)
        {
            if (runtimeValue != null)
            {
                resultValue = InnerInputType!.Serialize(runtimeValue);
                return true;
            }

            resultValue = null;
            return false;
        }

        protected sealed override bool TryDeserialize(object? resultValue, out object? runtimeValue)
        {
            if (resultValue != null)
            {
                return InnerInputType!.TryDeserialize(resultValue, out runtimeValue);
            }

            runtimeValue = null;
            return false;
        }
    }
}
