using System;
using System.Xml;
using HotChocolate.Language;
using HotChocolate.Properties;

#nullable enable

namespace HotChocolate.Types
{
    /// <summary>
    /// The TimeSpan scalar type represented in two formats:
    /// <see cref="TimeSpanFormat.Iso8601"/> and <see cref="TimeSpanFormat.DotNet"/>
    /// </summary>
    public sealed class TimeSpanType
        : ScalarType<TimeSpan, StringValueNode>
    {
        private readonly TimeSpanFormat _format;

        public TimeSpanType(
            TimeSpanFormat format = TimeSpanFormat.Iso8601,
            BindingBehavior behavior = BindingBehavior.Implicit)
            : this(ScalarNames.TimeSpan, TypeResources.TimeSpanType_Description, format, behavior)
        {
        }

        public TimeSpanType(
            NameString name,
            string? description = default,
            TimeSpanFormat format = TimeSpanFormat.Iso8601,
            BindingBehavior behavior = BindingBehavior.Implicit)
            : base(name, behavior)
        {
            _format = format;
            Description = description;
        }

        protected override TimeSpan ParseLiteral(StringValueNode literal)
        {
            if (TryDeserializeFromString(literal.Value, _format, out TimeSpan? value) &&
                value != null)
            {
                return value.Value;
            }

            throw new ScalarSerializationException(
                TypeResourceHelper.Scalar_Cannot_ParseLiteral(
                    Name, literal.GetType()));
        }

        protected override StringValueNode ParseValue(TimeSpan value)
        {
            if (_format == TimeSpanFormat.Iso8601)
            {
                return new StringValueNode(XmlConvert.ToString(value));
            }

            return new StringValueNode(value.ToString("c"));
        }

        public override bool TrySerialize(object value, out object? serialized)
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

        public override bool TryDeserialize(object serialized, out object? value)
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
