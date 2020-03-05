using System;
using System.Collections.Generic;
using HotChocolate.Resolvers;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Types.Selection
{
    public class InMemoryProvider : IResolverProvider
    {
        public (IServiceCollection, Func<IResolverContext, IEnumerable<TResult>>)
            CreateResolver<TResult>(params TResult[] results) where TResult : class
        {
            var services = new ServiceCollection();
            return (services, ctx => results);
        }
    }
}
