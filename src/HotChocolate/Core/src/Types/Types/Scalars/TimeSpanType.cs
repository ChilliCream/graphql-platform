using System;
using HotChocolate.Language;
using HotChocolate.Properties;

namespace HotChocolate.Types
{
    public sealed class TimeSpanType
        : ScalarType<TimeSpan, StringValueNode>
    {
        public TimeSpanType()
            : base(ScalarNames.TimeSpan, BindingBehavior.Implicit)
        {
            Description = TypeResources.TimeSpanType_Description;
        }

        public TimeSpanType(NameString name)
            : base(name, BindingBehavior.Implicit)
        {
        }

        public TimeSpanType(NameString name, string description)
            : base(name, BindingBehavior.Implicit)
        {
            Description = description;
        }

        protected override TimeSpan ParseLiteral(StringValueNode literal)
        {
            if (TryDeserializeFromString(literal.Value, out TimeSpan? value))
            {
                return value.Value;
            }

            throw new ScalarSerializationException(
                TypeResourceHelper.Scalar_Cannot_ParseLiteral(
                    Name, literal.GetType()));
        }

        protected override StringValueNode ParseValue(TimeSpan value)
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

            if (value is TimeSpan timeSpan)
            {
                serialized = Serialize(timeSpan);
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

            if (serialized is string s && TryDeserializeFromString(s, out TimeSpan? timeSpan))
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

        private static string Serialize(TimeSpan value)
        {
            return value.ToString();
        }

        private static bool TryDeserializeFromString(string serialized, out TimeSpan? value)
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
