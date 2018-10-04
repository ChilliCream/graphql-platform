using System;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate.Types;

namespace HotChocolate.Resolvers
{
    public delegate Task<object> OnInvokeResolver(
        IResolverContext resolverContext,
        IDirective directive,
        object previousResult,
        InvokeNext next);

    public delegate Task<object> InvokeNext(object result);
}
