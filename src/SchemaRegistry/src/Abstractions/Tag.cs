using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace MarshmallowPie
{
    public class Tag
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
            unchecked
            {
                return (obj.Key.GetHashCode() * 397) ^
                    (obj.Value.GetHashCode() * 397);
            }
        }

        public static TagComparer Default { get; } = new TagComparer();
    }
}
