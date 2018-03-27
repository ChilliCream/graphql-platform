using System;
using System.Collections.Generic;
using System.Linq;
using Prometheus.Resolvers;

namespace Prometheus.Types
{
    public delegate ObjectType ResolveType(
        IResolverContext context, 
        object resolverResult);
}