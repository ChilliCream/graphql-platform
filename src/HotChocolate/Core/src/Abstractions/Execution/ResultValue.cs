using System;

#nullable enable

namespace HotChocolate.Execution
{
    public readonly struct ResultValue : IEquatable<ResultValue?>
    {
        public ResultValue(string name, object? value, bool isNullable)
        {
            Name = name;
            Value = value;
            IsNullable = isNullable;
            HasValue = true;
        }

        public string Name { get; }

        public object? Value { get; }

        public bool IsNullable { get; }

        public bool HasValue { get; }

        public override bool Equals(object? obj)
        {
            return obj is FieldValue value &&
                HasValue == value.HasValue &&
                Name == value.Key &&
                Value == value.Value;
        }

        public bool Equals(ResultValue? other)
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

            return Name == other.Value.Name &&
                   Value == other.Value.Value;
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = (Name?.GetHashCode() ?? 0) * 3;
                hash = hash ^ ((Value?.GetHashCode() ?? 0) * 7);
                return hash;
            }
        }
    }
}
