using System;
using HotChocolate.Language;
using HotChocolate.Properties;

namespace HotChocolate.Types
{
    public class NonNullType
        : IOutputType
        , IInputType
    {
        private readonly bool _isInputType;
        private readonly IInputType _inputType;

        public NonNullType(IType type)
        {
            if (type == null)
            {
                throw new ArgumentNullException(nameof(type));
            }

            if (!(type is INullableType))
            {
                throw new ArgumentException(
                    TypeResources.NonNullType_TypeIsNunNullType,
                    nameof(type));
            }

            _isInputType = type.IsInputType();
            _inputType = type as IInputType;

            Type = type;
            ClrType = this.ToClrType();
        }

        public TypeKind Kind => TypeKind.NonNull;

        public IType Type { get; }

        public Type ClrType { get; }

        public bool IsInstanceOfType(IValueNode literal)
        {
            if (_isInputType)
            {
                if (literal is NullValueNode)
                {
                    return false;
                }

                return _inputType.IsInstanceOfType(literal);
            }

            throw new InvalidOperationException(
                TypeResources.NonNullType_NotAnInputType);
        }

        public object ParseLiteral(IValueNode literal)
        {
            if (literal == null)
            {
                throw new ArgumentNullException(nameof(literal));
            }

            if (_isInputType)
            {
                if (literal is NullValueNode)
                {
                    throw new ArgumentException(
                        TypeResources.NonNullType_ValueIsNull,
                        nameof(literal));
                }

                return _inputType.ParseLiteral(literal);
            }

            throw new InvalidOperationException(
                TypeResources.NonNullType_NotAnInputType);
        }

        public bool IsInstanceOfType(object value)
        {
            if (_isInputType && Type is IInputType it)
            {
                if (value is null)
                {
                    return false;
                }

                return it.IsInstanceOfType(value);
            }

            throw new InvalidOperationException(
                TypeResources.NonNullType_NotAnInputType);
        }

        public IValueNode ParseValue(object value)
        {
            if (_isInputType)
            {
                if (value == null)
                {
                    throw new ArgumentException(
                        TypeResources.NonNullType_ValueIsNull,
                        nameof(value));
                }

                return _inputType.ParseValue(value);
            }

            throw new InvalidOperationException(
                TypeResources.NonNullType_NotAnInputType);
        }
    }
}
