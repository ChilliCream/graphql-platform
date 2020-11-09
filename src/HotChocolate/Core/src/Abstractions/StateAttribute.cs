using System;
using HotChocolate.Properties;

namespace HotChocolate
{
    [Obsolete("Use GlobalStateAttribute or ScopedStateAttribute.")]
    [AttributeUsage(AttributeTargets.Parameter)]
    public class StateAttribute : Attribute
    {
        public StateAttribute(string key)
        {
            if (string.IsNullOrEmpty(key))
            {
                throw new ArgumentException(AbstractionResources
                    .State_KeyMustNotBeNullOrEmpty,
                    nameof(key));
            }

            Key = key;
        }

        public string Key { get; }

        public bool IsScoped { get; set; }

        public bool DefaultIfNotExists { get; set; }
    }
}
