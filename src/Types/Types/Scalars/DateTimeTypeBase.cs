using System;
using HotChocolate.Language;

namespace HotChocolate.Types
{
    public abstract class DateTimeTypeBase
        : ScalarType
    {
        public DateTimeTypeBase(string name, string description)
            : base(name, description)
        {
        }

        public override bool IsInstanceOfType(IValueNode literal)
        {
            if (literal == null)
            {
                throw new ArgumentNullException(nameof(literal));
            }

            return literal is StringValueNode
                || literal is NullValueNode;
        }

        public override object ParseLiteral(IValueNode literal)
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
                "The date time type can only parse string literals.",
                nameof(literal));
        }

        public override IValueNode ParseValue(object value)
        {
            if (value == null)
            {
                return new NullValueNode(null);
            }

            if (value is DateTimeOffset dateTimeOffset)
            {
                return new StringValueNode(Serialize(dateTimeOffset));
            }

            if (value is DateTime dateTime)
            {
                return new StringValueNode(Serialize(dateTime));
            }

            throw new ArgumentException(
                "The specified value has to be a DateTime in order " +
                "to be parsed by the date time type.");
        }

        public override object Serialize(object value)
        {
            if (value == null)
            {
                return null;
            }

            if (value is DateTimeOffset dateTimeOffset)
            {
                return Serialize(dateTimeOffset);
            }

            if (value is DateTime dateTime)
            {
                return Serialize(dateTime);
            }

            throw new ArgumentException(
                "The specified value cannot be serialized by the DateTimeType.");
        }

        protected abstract bool TryParseLiteral(StringValueNode literal, out object obj);

        protected abstract string Serialize(DateTime value);

        protected abstract string Serialize(DateTimeOffset value);
    }
}
