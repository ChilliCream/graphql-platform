using System;
using System.Diagnostics;
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

            _isInputType = type.IsInputType();
            _inputType = type as IInputType;

            Type = type;
            NativeType = _inputType?.NativeType;
        }

        public TypeKind Kind { get; } = TypeKind.NonNull;

        public IType Type { get; }

        public Type NativeType { get; }

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
                    throw new ArgumentException(
                        "A non null type cannot parse null value literals.");
                }

                return _inputType.ParseLiteral(literal);
            }

            throw new InvalidOperationException(
                "The specified type is not an input type.");
        }

        public IValueNode ParseValue(object value)
        {
            if (_isInputType)
            {
                if (value == null)
                {
                    throw new ArgumentException(
                        "A non null type cannot parse null values.");
                }

                return _inputType.ParseValue(value);
            }

            throw new InvalidOperationException(
                "The specified type is not an input type.");
        }
    }

    // this is just a marker type for the fluent code-first api.
    public sealed class NonNullType<T>
        : IOutputType
        , IInputType
        where T : IType
    {
        private NonNullType()
        {
        }

        public Type NativeType => throw new NotImplementedException();

        public TypeKind Kind => throw new NotImplementedException();

        public bool IsInstanceOfType(IValueNode literal)
        {
            throw new NotImplementedException();
        }

        public object ParseLiteral(IValueNode literal)
        {
            throw new NotImplementedException();
        }

        public IValueNode ParseValue(object value)
        {
            throw new NotImplementedException();
        }
    }
}
