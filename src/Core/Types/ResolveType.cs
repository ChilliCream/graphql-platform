using System;
using System.Collections.Generic;
using System.Linq;
using HotChocolate.Resolvers;

namespace HotChocolate.Types
{
    public delegate ObjectType ResolveAbstractType(
        IResolverContext context,
        object resolverResult);
}
