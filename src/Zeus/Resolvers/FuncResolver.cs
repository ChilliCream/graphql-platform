using System;
using System.Threading;
using System.Threading.Tasks;

namespace Zeus.Resolvers
{
    public class FuncResolver
        : IResolver
    {
        private readonly Func<IResolverContext, CancellationToken, Task<object>> _resolverFunc;

        public FuncResolver(Func<IResolverContext, CancellationToken, Task<object>> resolverFunc)
        {
            _resolverFunc = resolverFunc ?? throw new ArgumentNullException(nameof(resolverFunc));
        }

        public FuncResolver(Func<IResolverContext, Task<object>> resolverFunc)
        {
            if (resolverFunc == null)
            {
                throw new ArgumentNullException(nameof(resolverFunc));
            }

            _resolverFunc = new Func<IResolverContext, CancellationToken, Task<object>>((r, c) => resolverFunc(r));
        }

        public FuncResolver(Func<Task<object>> resolverFunc)
        {
            if (resolverFunc == null)
            {
                throw new ArgumentNullException(nameof(resolverFunc));
            }

            _resolverFunc = new Func<IResolverContext, CancellationToken, Task<object>>((r, c) => resolverFunc());
        }

        public FuncResolver(Func<IResolverContext, object> resolverFunc)
        {
            if (resolverFunc == null)
            {
                throw new ArgumentNullException(nameof(resolverFunc));
            }

            _resolverFunc = new Func<IResolverContext, CancellationToken, Task<object>>((r, c) => Task.FromResult(resolverFunc(r)));
        }

        public FuncResolver(Func<object> resolverFunc)
        {
            if (resolverFunc == null)
            {
                throw new ArgumentNullException(nameof(resolverFunc));
            }

            _resolverFunc = new Func<IResolverContext, CancellationToken, Task<object>>((r, c) => Task.FromResult(resolverFunc()));
        }

        public Task<object> ResolveAsync(IResolverContext context, CancellationToken cancellationToken)
        {
            return _resolverFunc(context, cancellationToken);
        }
    }
}
