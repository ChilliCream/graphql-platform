using System;
using HotChocolate.Language;

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
                    "The inner type of non-null type must be a nullable type",
                    nameof(type));
            }

            _isInputType = type.InnerType().IsInputType();
            _inputType = type.InnerType() as IInputType;
            Type = type;
        }

        public IType Type { get; }

        public bool IsInstanceOfType(IValueNode literal)
        {
            if (_isInputType)
            {
                if (literal is NullValueNode)
                {
                    return false;
                }

                _inputType.IsInstanceOfType(literal);
            }

            throw new InvalidOperationException(
                "The specified type is not an input type.");
        }

        public object ParseLiteral(IValueNode literal, Type targetType)
        {
            if (literal == null)
            {
                throw new ArgumentNullException(nameof(literal));
            }

            if (targetType == null)
            {
                throw new ArgumentNullException(nameof(targetType));
            }

            if (_isInputType)
            {
                if (literal is NullValueNode)
                {
                    throw new ArgumentException(
                        "A non null type cannot parse null value literals.");
                }

                return _inputType.ParseLiteral(literal, targetType);
            }

            throw new InvalidOperationException(
                "The specified type is not an input type.");
        }
    }
}
