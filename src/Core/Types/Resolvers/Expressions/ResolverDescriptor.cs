using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;

namespace HotChocolate.Resolvers.Expressions
{
    /// <summary>
    /// Describes a resolver that is based on a resolver type.
    /// </summary>
    internal class ResolverDescriptor
    {
        public Type ResolverType { get; }

        public Type SourceType { get; }

        public FieldMember Field { get; }
    }
}
