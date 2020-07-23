using System;
using System.Xml;
using HotChocolate.Language;
using HotChocolate.Properties;

#nullable enable

namespace HotChocolate.Types
{
    public class TimeSpanType : ScalarType
    {
        private readonly TimeSpanFormat _format;

        public TimeSpanType()
            : this("TimeSpan")
        {
        }

        public TimeSpanType(
            NameString name,
            string? description = default,
            TimeSpanFormat format = TimeSpanFormat.Iso8601)
            : base(name)
        {
            _format = format;
            Description = description;
        }

        public override Type ClrType => typeof(TimeSpan);

        public override bool IsInstanceOfType(IValueNode literal)
        {
            return literal is NullValueNode || literal is StringValueNode;
        }

        public override object? ParseLiteral(IValueNode literal)
        {
            if (literal is null)
            {
                throw new ArgumentNullException(nameof(literal));
            }

            if (literal is NullValueNode)
            {
                return null;
            }

            if (literal is StringValueNode stringValue
                && TryDeserializeFromString(stringValue.Value, _format, out TimeSpan? value)
                && value != null)
            {
                return value.Value;
            }

            throw new ScalarSerializationException(
                TypeResourceHelper.Scalar_Cannot_ParseLiteral(
                    Name, literal.GetType()));
        }

        public override IValueNode ParseValue(object? value)
        {
            if (value is null)
            {
                return NullValueNode.Default;
            }

            if (value is TimeSpan timeSpan)
            {
                if (_format == TimeSpanFormat.Iso8601)
                {
                    return new StringValueNode(XmlConvert.ToString(timeSpan));
                }

                return new StringValueNode(timeSpan.ToString("c"));
            }

            throw new ScalarSerializationException(
                TypeResourceHelper.Scalar_Cannot_ParseValue(
                    Name, value.GetType()));
        }

        public override object? Serialize(object? value)
        {
            if (TrySerialize(value, out object? serialized))
            {
                return serialized;
            }

            throw new ScalarSerializationException(
                TypeResourceHelper.Scalar_Cannot_Serialize(Name));
        }

        public bool TrySerialize(object? value, out object? serialized)
        {
            if (value is null)
            {
                serialized = null;
                return true;
            }

            if (value is TimeSpan timeSpan)
            {
                if (_format == TimeSpanFormat.Iso8601)
                {
                    serialized = XmlConvert.ToString(timeSpan);
                    return true;
                }

                serialized = timeSpan.ToString("c");
                return true;
            }

            serialized = null;
            return false;
        }

        public override bool TryDeserialize(object? serialized, out object? value)
        {
            if (serialized is null)
            {
                value = null;
                return true;
            }

            if (serialized is string s &&
                TryDeserializeFromString(s, _format, out TimeSpan? timeSpan))
            {
                value = timeSpan;
                return true;
            }

            if (serialized is TimeSpan ts)
            {
                value = ts;
                return true;
            }

            value = null;
            return false;
        }

        private static bool TryDeserializeFromString(
            string serialized,
            TimeSpanFormat format,
            out TimeSpan? value)
        {
            if (format == TimeSpanFormat.Iso8601)
            {
                return TryDeserializeIso8601(serialized, out value);
            }

            return TryDeserializeDotNet(serialized, out value);
        }

        private static bool TryDeserializeIso8601(string serialized, out TimeSpan? value)
        {
            try
            {
                return Iso8601Duration.TryParse(serialized, out value);
            }
            catch (FormatException)
            {
                value = null;
                return false;
            }
        }

        private static bool TryDeserializeDotNet(string serialized, out TimeSpan? value)
        {
            if (TimeSpan.TryParse(serialized, out TimeSpan timeSpan))
            {
                value = timeSpan;
                return true;
            }

            value = null;
            return false;
        }
    }
}