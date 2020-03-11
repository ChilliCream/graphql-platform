using System;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
using HotChocolate.Resolvers;

namespace HotChocolate.Types.Selections
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
