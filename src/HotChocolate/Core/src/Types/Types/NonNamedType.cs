using System;
using HotChocolate.Language;
using HotChocolate.Properties;

#nullable enable

namespace HotChocolate.Types
{
    public abstract class NonNamedType
        : IOutputType
        , IInputType
    {
        private Type? _innerClrType;
        private Type? _clrType;

        protected NonNamedType(IType innerType)
        {
            if (innerType is null)
            {
                throw new ArgumentNullException(nameof(innerType));
            }

            IsInputType = innerType.IsInputType();
            InnerInputType = innerType as IInputType;
            InnerType = innerType;
        }

        public abstract TypeKind Kind { get; }

        protected bool IsInputType { get; }

        protected IInputType? InnerInputType { get; }

        protected IType InnerType { get; }

        protected Type InnerClrType
        {
            get
            {
                if (_innerClrType is null)
                {
                    _innerClrType = InnerType.ToRuntimeType();
                }
                return _innerClrType;
            }
        }
        public Type RuntimeType
        {
            get
            {
                if (_clrType is null)
                {
                    _clrType = this.ToRuntimeType();
                }
                return _clrType;
            }
        }

        bool IParsableType.IsInstanceOfType(IValueNode literal)
        {
            if (literal is null)
            {
                throw new ArgumentNullException(nameof(literal));
            }

            if (IsInputType)
            {
                return IsInstanceOfType(literal);
            }

            throw new InvalidOperationException(
                TypeResources.NonNamedType_IsInstanceOfType_NotAnInputType);
        }

        protected abstract bool IsInstanceOfType(IValueNode literal);

        object? IParsableType.ParseLiteral(IValueNode literal, bool withDefaults)
        {
            if (literal is null)
            {
                throw new ArgumentNullException(nameof(literal));
            }

            if (IsInputType)
            {
                return ParseLiteral(literal, withDefaults);
            }

            throw new InvalidOperationException(
                TypeResources.NonNamedType_IsInstanceOfType_NotAnInputType);
        }

        protected abstract object? ParseLiteral(IValueNode literal, bool withDefaults);

        bool IParsableType.IsInstanceOfType(object? runtimeValue)
        {
            if (IsInputType)
            {
                return IsInstanceOfType(runtimeValue);
            }

            throw new InvalidOperationException(
                TypeResources.NonNamedType_IsInstanceOfType_NotAnInputType);
        }

        protected abstract bool IsInstanceOfType(object? runtimeValue);

        IValueNode IParsableType.ParseValue(object? runtimeValue)
        {
            if (IsInputType)
            {
                return ParseValue(runtimeValue);
            }

            throw new InvalidOperationException(
                TypeResources.NonNamedType_IsInstanceOfType_NotAnInputType);
        }

        protected abstract IValueNode ParseValue(object? runtimeValue);

        IValueNode IParsableType.ParseResult(object? resultValue)
        {
            if (IsInputType)
            {
                return ParseResult(resultValue);
            }

            throw new InvalidOperationException(
                TypeResources.NonNamedType_IsInstanceOfType_NotAnInputType);
        }

        protected abstract IValueNode ParseResult(object? resultValue);

        object? ISerializableType.Serialize(object? runtimeValue)
        {
            if (IsInputType)
            {
                if (TrySerialize(runtimeValue, out object? serialized))
                {
                    return serialized;
                }

                throw new SerializationException(
                    TypeResourceHelper.Scalar_Cannot_Serialize(this.Visualize()),
                    this);
            }

            throw new InvalidOperationException(
                TypeResources.NonNamedType_IsInstanceOfType_NotAnInputType);
        }

        protected abstract bool TrySerialize(
            object? runtimeValue,
            out object? resultValue);

        object? ISerializableType.Deserialize(object? resultValue)
        {
            if (IsInputType)
            {
                if (TryDeserialize(resultValue, out object? runtimeValue))
                {
                    return runtimeValue;
                }

                throw new SerializationException(
                    TypeResourceHelper.Scalar_Cannot_Deserialize(this.Print()),
                    this);
            }

            throw new InvalidOperationException(
                TypeResources.NonNamedType_IsInstanceOfType_NotAnInputType);
        }

        bool ISerializableType.TryDeserialize(
            object? resultValue, out object? runtimeValue)
        {
            if (IsInputType)
            {
                return TryDeserialize(resultValue, out runtimeValue);
            }

            throw new InvalidOperationException(
                TypeResources.NonNamedType_IsInstanceOfType_NotAnInputType);
        }

        protected abstract bool TryDeserialize(object? resultValue, out object? runtimeValue);
    }
}
