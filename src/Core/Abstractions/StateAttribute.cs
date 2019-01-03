using System;

namespace HotChocolate
{
    [AttributeUsage(AttributeTargets.Parameter)]
    public sealed class StateAttribute
        : Attribute
    {
        public StateAttribute(string key)
        {
            if (string.IsNullOrEmpty(key))
            {
                // TODO : Resources
                throw new ArgumentException(
                    "The state key cannot null or empty.",
                    nameof(key));
            }

            Key = key;
        }

        public string Key { get; }
    }
}
