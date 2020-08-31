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
            if (innerType == null)
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
                    _innerClrType = InnerType.ToClrType();
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
                    _clrType = this.ToClrType();
                }
                return _clrType;
            }
        }

        bool IInputType.IsInstanceOfType(IValueNode literal)
        {
            if (literal == null)
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

        object IInputType.ParseLiteral(IValueNode literal)
        {
            if (literal == null)
            {
                throw new ArgumentNullException(nameof(literal));
            }

            if (IsInputType)
            {
                return ParseLiteral(literal);
            }

            throw new InvalidOperationException(
                TypeResources.NonNamedType_IsInstanceOfType_NotAnInputType);
        }

        protected abstract object ParseLiteral(IValueNode literal);

        bool IInputType.IsInstanceOfType(object value)
        {
            if (IsInputType)
            {
                return IsInstanceOfType(value);
            }

            throw new InvalidOperationException(
                TypeResources.NonNamedType_IsInstanceOfType_NotAnInputType);
        }

        protected abstract bool IsInstanceOfType(object value);

        IValueNode IInputType.ParseValue(object value)
        {
            if (IsInputType)
            {
                return ParseValue(value);
            }

            throw new InvalidOperationException(
                TypeResources.NonNamedType_IsInstanceOfType_NotAnInputType);
        }

        protected abstract IValueNode ParseValue(object value);

        object? ISerializableType.Serialize(object? value)
        {
            if (IsInputType)
            {
                if (TrySerialize(value, out object? serialized))
                {
                    return serialized;
                }

                throw new ScalarSerializationException(
                    TypeResourceHelper.Scalar_Cannot_Serialize(
                        this.Visualize()));
            }

            throw new InvalidOperationException(
                TypeResources.NonNamedType_IsInstanceOfType_NotAnInputType);
        }

        protected abstract bool TrySerialize(
            object? value,
            out object? serialized);

        object? ISerializableType.Deserialize(object? serialized)
        {
            if (IsInputType)
            {
                if (TryDeserialize(serialized, out object? value))
                {
                    return value;
                }

                throw new ScalarSerializationException(
                    TypeResourceHelper.Scalar_Cannot_Deserialize(
                        this.Visualize()));
            }

            throw new InvalidOperationException(
                TypeResources.NonNamedType_IsInstanceOfType_NotAnInputType);
        }

        bool ISerializableType.TryDeserialize(
            object? serialized, out object? value)
        {
            if (IsInputType)
            {
                return TryDeserialize(serialized, out value);
            }

            throw new InvalidOperationException(
                TypeResources.NonNamedType_IsInstanceOfType_NotAnInputType);
        }

        protected abstract bool TryDeserialize(object? serialized, out object? value);
    }
}
