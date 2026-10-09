using HotChocolate.Execution;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.AspNetCore;

public class HttpRequestExecutorProxyTests
{
    [Fact]
    public async Task GetOrCreateSessionAsync_Should_ReturnSameSession_When_ProxiesShareExecutor()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var services = CreateServices();
        using var first = HttpRequestExecutorProxy.Create(services, ISchemaDefinition.DefaultName);
        using var second = HttpRequestExecutorProxy.Create(services, ISchemaDefinition.DefaultName);

        // act
        var firstSession = await first.GetOrCreateSessionAsync(cancellationToken);
        var secondSession = await second.GetOrCreateSessionAsync(cancellationToken);

        // assert
        Assert.Same(firstSession, secondSession);
    }

    [Theory]
    [InlineData(2)]
    [InlineData(16)]
    public async Task GetOrCreateSessionAsync_Should_ReturnOneSession_When_ProxiesStartConcurrently(
        int proxyCount)
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var sessionCountsPerExecutor = new List<int>();

        // act
        for (var i = 0; i < 20; i++)
        {
            await using var services = CreateServices();
            await services
                .GetRequiredService<IRequestExecutorProvider>()
                .GetExecutorAsync(cancellationToken: cancellationToken);

            var proxies = new HttpRequestExecutorProxy[proxyCount];

            for (var j = 0; j < proxyCount; j++)
            {
                proxies[j] = HttpRequestExecutorProxy.Create(
                    services,
                    ISchemaDefinition.DefaultName);
            }

            var start = new TaskCompletionSource(
                TaskCreationOptions.RunContinuationsAsynchronously);
            var sessionTasks = proxies
                .Select(
                    async proxy =>
                    {
                        await start.Task;
                        return await proxy.GetOrCreateSessionAsync(cancellationToken);
                    })
                .ToArray();
            start.SetResult();
            var sessions = await Task.WhenAll(sessionTasks);

            sessionCountsPerExecutor.Add(sessions.Distinct().Count());

            foreach (var proxy in proxies)
            {
                proxy.Dispose();
            }
        }

        // assert
        Assert.All(sessionCountsPerExecutor, count => Assert.Equal(1, count));
    }

    private static ServiceProvider CreateServices()
        => new ServiceCollection()
            .AddGraphQLServer()
            .AddQueryType<Query>()
            .Services
            .BuildServiceProvider();

    public sealed class Query
    {
        public string Greeting => "hello";
    }
}
