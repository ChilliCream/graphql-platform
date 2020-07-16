using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using HotChocolate.Resolvers;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Types.Selections
{
    public class InMemoryProvider : IResolverProvider
    {
        public (IServiceCollection, Func<IResolverContext, IEnumerable<TResult>>)
            CreateResolver<TResult>(params TResult[] results) where TResult : class
        {
            var services = new ServiceCollection();
            return (services, _ => results);
        }

        public (IServiceCollection, Func<IResolverContext, IAsyncEnumerable<TResult>>)
            CreateAsyncResolver<TResult>(params TResult[] results) where TResult : class
        {
            var services = new ServiceCollection();
            return (services, _ => GenerateAsyncEnumerable(results));
        }

        private static async IAsyncEnumerable<T> GenerateAsyncEnumerable<T>(T[] results)
        {
            foreach (T element in results)
            {
                await Task.Delay(1).ConfigureAwait(false);
                yield return element;
            }
        }
    }
}
