using System;

namespace HotChocolate.Configuration
{
    internal class ResolverBindingInfo
    {
        public Type ObjectType { get; set; }
        public NameString ObjectTypeName { get; set; }
    }
}
