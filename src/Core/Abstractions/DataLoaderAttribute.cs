using System;

namespace HotChocolate
{
    [AttributeUsage(AttributeTargets.Parameter)]
    public sealed class DataLoaderAttribute
        : Attribute
    {
        public DataLoaderAttribute()
        {
        }

        public DataLoaderAttribute(string key)
        {
            if (string.IsNullOrEmpty(key))
            {
                // TODO : Resources
                throw new ArgumentException(
                    "The DataLoader key cannot null or empty.",
                    nameof(key));
            }

            Key = key;
        }

        public string Key { get; }
    }
}
