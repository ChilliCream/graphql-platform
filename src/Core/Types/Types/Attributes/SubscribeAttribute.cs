using System;

#nullable enable

namespace HotChocolate.Types
{
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Property)]
    public sealed class SubscribeAttribute : Attribute
    {
        public SubscribeAttribute(string resolverName)
        {
            ResolverName = resolverName;
        }

        public string ResolverName { get; }
    }
}
