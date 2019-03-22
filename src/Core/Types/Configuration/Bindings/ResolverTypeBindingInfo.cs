using System;
using System.Collections.Generic;
using HotChocolate.Types;

namespace HotChocolate.Configuration
{
    internal class ResolverTypeBindingInfo
    {
        public Type ResolverType { get; set; }
        public Type SourceType { get; set; }
        public NameString TypeName { get; set; }
        public BindingBehavior Behavior { get; set; }
        public ICollection<FieldResolverBindungInfo> Fields { get; } =
            new List<FieldResolverBindungInfo>();
    }
}
