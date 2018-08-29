using System;
using HotChocolate.Language;

namespace HotChocolate.Types
{
    public abstract class DateTimeTypeBase
        : ScalarType
    {
        protected DateTimeTypeBase(string name, string description)
            : base(name, description)
        {
        }

        public sealed override bool IsInstanceOfType(IValueNode literal)
        {
            if (literal == null)
            {
                throw new ArgumentNullException(nameof(literal));
            }

            return literal is StringValueNode
                || literal is NullValueNode;
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

            throw new ArgumentException(
                $"The {Name} can only parse string literals.",
                nameof(literal));
        }

        public sealed override IValueNode ParseValue(object value)
        {
            if (TryParseValue(value, out IValueNode valueNode))
            {
                return valueNode;
            }

            throw new ArgumentException(
                $"The specified value has to be a valid {Name} " +
                $"in order to be parsed by the {Name}.");
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

            throw new ArgumentException(
                $"The specified value cannot be serialized by {Name}.");
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

        protected abstract bool TryParseLiteral(StringValueNode literal, out object obj);

        protected abstract string Serialize(DateTime value);

        protected abstract string Serialize(DateTimeOffset value);
    }
}
