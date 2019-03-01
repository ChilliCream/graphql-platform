using System;
using HotChocolate.Properties;

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
                throw new ArgumentException(AbstractionResources
                    .DirectiveArgument_NameMustNotBeNullOrempty,
                    nameof(name));
            }

            Name = name;
        }

        public string Name { get; }
    }
}
