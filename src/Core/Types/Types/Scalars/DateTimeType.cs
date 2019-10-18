using System;
using System.Globalization;
using HotChocolate.Language;
using HotChocolate.Properties;

namespace HotChocolate.Types
{
    public sealed class DateTimeType
        : ScalarType<DateTimeOffset, StringValueNode>
    {
        private const string _utcFormat = "yyyy-MM-ddTHH\\:mm\\:ss.fffZ";
        private const string _localFormat = "yyyy-MM-ddTHH\\:mm\\:ss.fffzzz";

        public DateTimeType() : base(ScalarNames.DateTime)
        {
            Description = TypeResources.DateTimeType_Description;
        }

        public DateTimeType(NameString name) : base(name)
        {
        }

        public DateTimeType(NameString name, string description) : base(name)
        {
            Description = description;
        }

        protected override DateTimeOffset ParseLiteral(StringValueNode literal)
        {
            if (TryDeserializeFromString(literal.Value, out DateTimeOffset? value))
            {
                return value.Value;
            }

            throw new ScalarSerializationException(
                TypeResourceHelper.Scalar_Cannot_ParseLiteral(
                    Name, literal.GetType()));
        }

        protected override StringValueNode ParseValue(DateTimeOffset value)
        {
            return new StringValueNode(Serialize(value));
        }

        public override bool TrySerialize(object value, out object serialized)
        {
            if (value is null)
            {
                serialized = null;
                return true;
            }

            if (value is DateTimeOffset dt)
            {
                serialized = Serialize(dt);
                return true;
            }

            serialized = null;
            return false;
        }

        public override bool TryDeserialize(object serialized, out object value)
        {
            if (serialized is null)
            {
                value = null;
                return true;
            }

            if (serialized is string s && TryDeserializeFromString(s, out DateTimeOffset? d))
            {
                value = d;
                return true;
            }

            if (serialized is DateTimeOffset)
            {
                value = serialized;
                return true;
            }

            if (serialized is DateTime dt)
            {
                value = new DateTimeOffset(
                    dt.ToUniversalTime(),
                    TimeSpan.Zero);
                return true;
            }

            value = null;
            return false;
        }

        private static string Serialize(DateTimeOffset value)
        {
            if (value.Offset == TimeSpan.Zero)
            {
                return value.ToString(
                    _utcFormat,
                    CultureInfo.InvariantCulture);
            }

            return value.ToString(
                _localFormat,
                CultureInfo.InvariantCulture);
        }

        private static bool TryDeserializeFromString(string serialized, out DateTimeOffset? value)
        {
            if (serialized != null
                && serialized.EndsWith("Z")
                && DateTime.TryParse(
                    serialized,
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.AssumeUniversal,
                    out DateTime zuluTime))
            {
                value = new DateTimeOffset(
                    zuluTime.ToUniversalTime(),
                    TimeSpan.Zero);
                return true;
            }

            if (DateTimeOffset.TryParse(
                serialized,
                out DateTimeOffset dt))
            {
                value = dt;
                return true;
            }

            value = null;
            return false;
        }
    }
}
