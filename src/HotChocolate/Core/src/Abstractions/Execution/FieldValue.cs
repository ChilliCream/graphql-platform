using System;

#nullable enable

namespace HotChocolate.Execution
{
    public readonly struct FieldValue
        : IEquatable<FieldValue?>
    {
        internal FieldValue(string key, object value)
        {
            Key = key;
            Value = value;
            HasValue = true;
        }

        public string Key { get; }

        public object Value { get; }

        public bool HasValue { get; }

        public override bool Equals(object? obj)
        {
            return obj is FieldValue value &&
                HasValue == value.HasValue &&
                Key == value.Key &&
                Value == value.Value;
        }

        public bool Equals(FieldValue? other)
        {
            if (other is null)
            {
                return false;
            }

            if (HasValue != other.Value.HasValue)
            {
                return false;
            }

            if (HasValue == false)
            {
                return true;
            }

            return Key == other.Value.Key &&
                   Value == other.Value.Value;
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = (Key?.GetHashCode() ?? 0) * 3;
                hash = hash ^ ((Value?.GetHashCode() ?? 0) * 7);
                return hash;
            }
        }
    }
}
