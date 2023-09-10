using System;
using System.Collections.Generic;
using HotChocolate.Execution.Processing;
using HotChocolate.Resolvers;
using HotChocolate.Types;

namespace HotChocolate.Data.Projections;

public interface ISelectionVisitorContext
{
    Stack<ISelection> Selection { get; }

    Stack<INamedType?> ResolvedType { get; }

    [Obsolete("Use ResolverContext")]
    IResolverContext Context => ResolverContext;

    IResolverContext ResolverContext { get; }
}
