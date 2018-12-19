using System;
using System.Collections.Generic;
using HotChocolate.Types;

namespace HotChocolate.Configuration
{
    internal class ResolverCollectionBindingInfo
        : ResolverBindingInfo
    {
        public Type ResolverType { get; set; }
        public BindingBehavior Behavior { get; set; }
        public List<FieldResolverBindungInfo> Fields { get; } =
            new List<FieldResolverBindungInfo>();
        public bool IsSourceResolver { get; set; }
    }
}
