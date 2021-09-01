using System;

namespace GreenDonut
{
    public readonly struct TaskCacheKey : IEquatable<TaskCacheKey>
    {
        public TaskCacheKey(string type, object key)
        {
            Type = type ?? throw new ArgumentNullException(nameof(type));
            Key = key ?? throw new ArgumentNullException(nameof(key));
        }

        public string Type { get; }

        public object Key { get; }

        public bool Equals(TaskCacheKey other)
        {
            return Type == other.Type && Key.Equals(other.Key);
        }

        public override bool Equals(object? obj)
        {
            return obj is TaskCacheKey other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (Type.GetHashCode() * 397) ^ Key.GetHashCode();
            }
        }
    }
}
