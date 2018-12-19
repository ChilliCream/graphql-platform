using System;
using System.Collections.Generic;
using System.Linq;
using HotChocolate.Resolvers;

namespace HotChocolate.Types
{
    public delegate bool IsOfType(
        IResolverContext context,
        object resolverResult);
}
