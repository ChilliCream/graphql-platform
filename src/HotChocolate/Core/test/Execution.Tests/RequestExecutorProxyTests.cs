using System.Threading;
using System.Threading.Tasks;
using HotChocolate.StarWars;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace HotChocolate.Execution
{
    public class RequestExecutorProxyTests
    {
        [Fact]
        public async Task Ensure_Executor_Is_Cached()
        {
            // arrange
            IRequestExecutorResolver resolver =
                new ServiceCollection()
                    .AddGraphQL()
                    .AddStarWarsRepositories()
                    .AddStarWarsTypes()
                    .Services
                    .BuildServiceProvider()
                    .GetRequiredService<IRequestExecutorResolver>();

            // act
            var proxy = new RequestExecutorProxy(resolver, Schema.DefaultName);
            IRequestExecutor a = await proxy.GetRequestExecutorAsync(CancellationToken.None);
            IRequestExecutor b = await proxy.GetRequestExecutorAsync(CancellationToken.None);

            // assert
            Assert.Same(a, b);
        }

        [Fact]
        public async Task Ensure_Executor_Is_Correctly_Swapped_When_Evicted()
        {
            // arrange
            IRequestExecutorResolver resolver =
                new ServiceCollection()
                    .AddGraphQL()
                    .AddStarWarsRepositories()
                    .AddStarWarsTypes()
                    .Services
                    .BuildServiceProvider()
                    .GetRequiredService<IRequestExecutorResolver>();
            var evicted = false;
            var updated = false;

            var proxy = new RequestExecutorProxy(resolver, Schema.DefaultName);
            proxy.ExecutorEvicted += (sender, args) =>
            {
                evicted = true;
                updated = false;
            };
            proxy.ExecutorUpdated += (sender, args) => updated = true;

            // act
            IRequestExecutor a = await proxy.GetRequestExecutorAsync(CancellationToken.None);
            resolver.EvictRequestExecutor();
            IRequestExecutor b = await proxy.GetRequestExecutorAsync(CancellationToken.None);

            // assert
            Assert.NotSame(a, b);
            Assert.True(evicted);
            Assert.True(updated);
        }
    }
}
