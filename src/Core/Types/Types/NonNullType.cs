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
                // TODO : resources
                throw new ArgumentException(
                    "The inner type of non-null type must be a nullable type",
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

            // TODO : resources
            throw new InvalidOperationException(
                "The specified type is not an input type.");
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
                    // TODO : resources
                    throw new ArgumentException(
                        "A non null type cannot parse null value literals.");
                }

                return _inputType.ParseLiteral(literal);
            }

            // TODO : resources
            throw new InvalidOperationException(
                "The specified type is not an input type.");
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

            // TODO : resources
            throw new InvalidOperationException(
                "The specified type is not an input type.");
        }

        public IValueNode ParseValue(object value)
        {
            if (_isInputType)
            {
                if (value == null)
                {
                    // TODO : resources
                    throw new ArgumentException(
                        "A non null type cannot parse null values.");
                }

                return _inputType.ParseValue(value);
            }

            // TODO : resources
            throw new InvalidOperationException(
                "The specified type is not an input type.");
        }
    }
}
