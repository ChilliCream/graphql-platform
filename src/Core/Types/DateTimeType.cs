using System;
using HotChocolate.Language;

namespace HotChocolate.Types
{
    public class DateTimeType
        : ScalarType
    {
        public DateTimeType()
            : base("DateTime", "ISO-8601 compliant date time type.")
        {
        }

        public override Type NativeType => typeof(DateTimeOffset);

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
                && DateTimeOffset.TryParse(
                    stringLiteral.Value, out DateTimeOffset dateTime))
            {
                return dateTime;
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

        private string Serialize(DateTime value)
        {
            return value.ToString("yyyy-MM-ddTHH\\:mm\\:sszzz");
        }

        private string Serialize(DateTimeOffset value)
        {
            return value.ToString("yyyy-MM-ddTHH\\:mm\\:sszzz");
        }
    }
}
