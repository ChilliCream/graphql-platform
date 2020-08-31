using System;
using System.Collections.Generic;
using HotChocolate.Resolvers;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Types.Selections
{
    public interface IResolverProvider
    {
        public (IServiceCollection, Func<IResolverContext, IEnumerable<TResult>>)
            CreateResolver<TResult>(params TResult[] results)
            where TResult : class;

        public (IServiceCollection, Func<IResolverContext, IAsyncEnumerable<TResult>>)
            CreateAsyncResolver<TResult>(params TResult[] results)
            where TResult : class;
    }
}
