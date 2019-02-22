using System;
using HotChocolate.Properties;

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
                throw new ArgumentException(
                    AbstractionResources.DataLoader_KeyMustNotBeNullOrEmpty,
                    nameof(key));
            }

            Key = key;
        }

        public string Key { get; }
    }
}
