using HotChocolate.Execution.Processing.Tasks;
using HotChocolate.Types;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.ObjectPool;

namespace HotChocolate.Execution.Processing;

public class WorkSchedulerTests
{
    [Fact]
    public async Task Execute_Should_Complete_When_SerialTaskIsReassignedBeforeItIsAwaited()
    {
        // arrange
        // the pool hands each returned task a new id, as a concurrent request re-registering it would;
        // the global field middleware keeps the resolver from compiling to a pure, parallel resolver
        var services = new ServiceCollection();
        services.AddSingleton<ObjectPool<ResolverTask>>(new ReassigningResolverTaskPool());
        var executor = await services
            .AddGraphQL()
            .AddQueryType<SerialQuery>()
            .UseField(next => next)
            .BuildRequestExecutorAsync(cancellationToken: TestContext.Current.CancellationToken);

        // act
        await using var result = await executor
            .ExecuteAsync("{ serial }", TestContext.Current.CancellationToken)
            .WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken);

        // assert
        result.MatchInlineSnapshot(
            """
            {
              "data": {
                "serial": "serial"
              }
            }
            """);
    }

    public sealed class SerialQuery
    {
        [Serial]
        public string GetSerial() => "serial";
    }

    private sealed class ReassigningResolverTaskPool : ObjectPool<ResolverTask>
    {
        private uint _nextForeignId = 1_000_000;

        public override ResolverTask Get() => new(this);

        public override void Return(ResolverTask obj)
            => obj.Id = Interlocked.Increment(ref _nextForeignId);
    }
}
