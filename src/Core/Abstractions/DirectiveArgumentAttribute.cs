using System;

namespace HotChocolate
{
    [AttributeUsage(AttributeTargets.Parameter)]
    public sealed class DirectiveArgumentAttribute
        : Attribute
    {
        public DirectiveArgumentAttribute()
        {
        }

        public DirectiveArgumentAttribute(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                // TODO : Resources
                throw new ArgumentException(
                    "The directive argument name be cannot null or empty.",
                    nameof(name));
            }

            Name = name;
        }

        public string Name { get; }
    }
}
