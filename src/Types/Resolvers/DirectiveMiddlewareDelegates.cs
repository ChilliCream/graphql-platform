using System;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate.Types;

namespace HotChocolate.Resolvers
{
    public delegate Task OnBeforeInvokeResolverAsync(
        IResolverContext resolverContext,
        IDirective directive,
        CancellationToken cancellationToken);

    public delegate Task<object> OnInvokeResolverAsync(
        IResolverContext resolverContext,
        IDirective directive,
        Func<Task<object>> resolveField,
        CancellationToken cancellationToken);

    public delegate Task<object> OnAfterInvokeResolverAsync(
        IResolverContext resolverContext,
        IDirective directive,
        object resolverResult,
        CancellationToken cancellationToken);
}
