using System;
using System.Collections.Immutable;
using System.Globalization;
using HotChocolate.Language;

namespace HotChocolate.Types
{
    public class DateType
        : ScalarType
    {
        public DateType()
            : base("Date", "ISO-8601 compliant date type.")
        {
        }

        public override Type NativeType => typeof(DateTime);

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
                && DateTime.TryParse(
                    stringLiteral.Value, out DateTime dateTime))
            {
                return dateTime;
            }

            if (literal is NullValueNode)
            {
                return null;
            }

            throw new ArgumentException(
                "The date type can only parse string literals.",
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
            return value.ToString("yyyy-MM-dd");
        }

        private string Serialize(DateTimeOffset value)
        {
            return value.ToString("yyyy-MM-dd");
        }
    }
}
