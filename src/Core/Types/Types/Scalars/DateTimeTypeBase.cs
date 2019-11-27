using System;
using HotChocolate.Language;
using HotChocolate.Properties;

namespace HotChocolate.Types
{
    public abstract class DateTimeTypeBase
        : ScalarType
    {
        protected DateTimeTypeBase(NameString name)
            : base(name)
        {
        }

        public sealed override bool IsInstanceOfType(IValueNode literal)
        {
            if (literal == null)
            {
                throw new ArgumentNullException(nameof(literal));
            }

            if (literal is NullValueNode)
            {
                return true;
            }

            return literal is StringValueNode stringLiteral
                && TryParseLiteral(stringLiteral, out _);
        }

        public sealed override object ParseLiteral(IValueNode literal)
        {
            if (literal == null)
            {
                throw new ArgumentNullException(nameof(literal));
            }

            if (literal is StringValueNode stringLiteral
                && TryParseLiteral(stringLiteral, out object obj))
            {
                return obj;
            }

            if (literal is NullValueNode)
            {
                return null;
            }

            throw new ScalarSerializationException(
                TypeResourceHelper.Scalar_Cannot_ParseLiteral(
                    Name, literal.GetType()));
        }

        public sealed override IValueNode ParseValue(object value)
        {
            if (TryParseValue(value, out IValueNode valueNode))
            {
                return valueNode;
            }

            throw new ScalarSerializationException(
                TypeResourceHelper.Scalar_Cannot_ParseValue(
                    Name, value.GetType()));
        }

        protected bool TryParseValue(
            object value,
            out IValueNode valueNode)
        {
            if (value == null)
            {
                valueNode = new NullValueNode(null);
                return true;
            }

            if (TrySerialize(value, out string serializedValue))
            {
                valueNode = new StringValueNode(serializedValue);
                return true;
            }

            valueNode = null;
            return false;
        }

        public sealed override object Serialize(object value)
        {
            if (value == null)
            {
                return null;
            }

            if (TrySerialize(value, out string serializedValue))
            {
                return serializedValue;
            }

            throw new ScalarSerializationException(
                TypeResourceHelper.Scalar_Cannot_Serialize(Name));
        }

        protected virtual bool TrySerialize(
            object value,
            out string serializedValue)
        {
            if (value == null)
            {
                serializedValue = null;
                return true;
            }

            if (value is DateTimeOffset dateTimeOffset)
            {
                serializedValue = Serialize(dateTimeOffset);
                return true;
            }

            if (value is DateTime dateTime)
            {
                serializedValue = Serialize(dateTime);
                return true;
            }

            serializedValue = null;
            return false;
        }

        private bool TryParseLiteral(StringValueNode literal, out object obj) =>
            TryDeserializeFromString(literal.Value, out obj);

        protected abstract bool TryDeserializeFromString(
            string serialized,
            out object obj);

        protected abstract string Serialize(DateTime value);

        protected abstract string Serialize(DateTimeOffset value);
    }
}
