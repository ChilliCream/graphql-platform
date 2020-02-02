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
        public Type ClrType
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

            // TODO : resources
            throw new InvalidOperationException(
                "The specified type is not an input type.");
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

            // TODO : resources
            throw new InvalidOperationException(
                "The specified type is not an input type.");
        }

        protected abstract object ParseLiteral(IValueNode literal);

        bool IInputType.IsInstanceOfType(object value)
        {
            if (IsInputType)
            {
                return IsInstanceOfType(value);
            }

            // TODO : resources
            throw new InvalidOperationException(
                "The specified type is not an input type.");
        }

        protected abstract bool IsInstanceOfType(object value);

        IValueNode IInputType.ParseValue(object value)
        {
            if (IsInputType)
            {
                return ParseValue(value);
            }

            // TODO : resources
            throw new InvalidOperationException(
                "The specified type is not an input type.");
        }

        protected abstract IValueNode ParseValue(object value);

        object ISerializableType.Serialize(object value)
        {
            if (IsInputType)
            {
                if (TrySerialize(value, out var serialized))
                {
                    return serialized;
                }

                // TODO : resources
                throw new ScalarSerializationException(
                    TypeResourceHelper.Scalar_Cannot_Serialize(
                        this.Visualize()));
            }

            // TODO : resources
            throw new InvalidOperationException(
                "The specified type is not an input type.");
        }

        protected abstract bool TrySerialize(
            object value,
            out object serialized);

        object ISerializableType.Deserialize(object serialized)
        {
            if (IsInputType)
            {
                if (TryDeserialize(serialized, out var value))
                {
                    return value;
                }

                throw new ScalarSerializationException(
                    TypeResourceHelper.Scalar_Cannot_Deserialize(
                        this.Visualize()));
            }

            // TODO : resources
            throw new InvalidOperationException(
                "The specified type is not an input type.");
        }

        bool ISerializableType.TryDeserialize(
            object serialized, out object value)
        {
            if (IsInputType)
            {
                return TryDeserialize(serialized, out value);
            }

            // TODO : resources
            throw new InvalidOperationException(
                "The specified type is not an input type.");
        }

        protected abstract bool TryDeserialize(
            object serialized,
            out object value);
    }
}
