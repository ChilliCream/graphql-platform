using System;

#nullable enable

namespace HotChocolate.Types
{
    public class SubscribeAttribute : Attribute
    {
        public SubscribeAttribute(string resolverName)
        {
            ResolverName = resolverName;
        }

        public string ResolverName { get; }
    }
}
