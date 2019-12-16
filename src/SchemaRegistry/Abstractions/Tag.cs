using System;

namespace MarshmallowPie
{
    public class Tag
    {
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
}
