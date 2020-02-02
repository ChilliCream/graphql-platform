using System;
using System.Collections.Generic;

namespace MarshmallowPie
{
    public sealed class Tag
        : IEquatable<Tag>
    {
        public Tag(string key, string value)
            : this(key, value, DateTime.UtcNow)
        {
        }

        public Tag(string key, string value, DateTime published)
        {
            Key = key;
            Value = value;
            Published = published;
        }

        public string Key { get; }

        public string Value { get; }

        public DateTime Published { get; }

        public override bool Equals(object? obj)
        {
            return obj is Tag tag
                && Key.Equals(tag.Key, StringComparison.Ordinal)
                && Value.Equals(tag.Value, StringComparison.Ordinal)
                && Published.Equals(tag.Published);
        }

        public bool Equals(Tag? other)
        {
            return other is { }
                && Key.Equals(other.Key, StringComparison.Ordinal)
                && Value.Equals(other.Value, StringComparison.Ordinal)
                && Published.Equals(other.Published);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Key, Value, Published);
        }
    }

    public sealed class TagComparer : IEqualityComparer<Tag>
    {
        public bool Equals(Tag? x, Tag? y)
        {
            if (x is null)
            {
                if (y is null)
                {
                    return true;
                }
                return false;
            }

            if (y is null)
            {
                return false;
            }

            return string.Equals(x.Key, y.Key, StringComparison.Ordinal)
                && string.Equals(x.Value, y.Value, StringComparison.Ordinal);
        }

        public int GetHashCode(Tag obj)
        {
            return obj.GetHashCode();
        }

        public static TagComparer Default { get; } = new TagComparer();
    }
}
