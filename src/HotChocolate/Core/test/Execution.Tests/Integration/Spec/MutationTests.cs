using HotChocolate.Types;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Execution.Integration.Spec;

public class MutationTests
{
    [Fact]
    public async Task Ensure_Mutations_Are_Executed_Serially()
    {
        var result =
            await new ServiceCollection()
                .AddGraphQLServer()
                .AddQueryType(d => d.Field("a").Resolve("b"))
                .AddMutationType<Mutation1>()
                .ExecuteRequestAsync(
                    """
                    mutation {
                        a
                        b
                    }
                    """,
                    cancellationToken: TestContext.Current.CancellationToken);

        result.MatchInlineSnapshot(
            """
            {
              "data": {
                "a": 1,
                "b": 2
              }
            }
            """);
    }

    [Fact]
    public async Task Ensure_Mutations_Child_Fields_Are_Scoped_To_Its_Parent()
    {
        using var cts = new CancellationTokenSource(5_000);

        var result =
            await new ServiceCollection()
                .AddGraphQLServer()
                .AddQueryType(d => d.Field("a").Resolve("b"))
                .AddMutationType<Mutation2>()
                .ExecuteRequestAsync(
                    """
                    mutation {
                        a { a b }
                        b { a b }
                    }
                    """,
                    cancellationToken: cts.Token);

        result.MatchInlineSnapshot(
            """
            {
              "data": {
                "a": {
                  "a": true,
                  "b": true
                },
                "b": {
                  "a": true,
                  "b": true
                }
              }
            }
            """);
    }

    [Fact]
    public async Task Execute_Should_CompleteAllMutations_When_ChildResolverYieldsUnderConcurrency()
    {
        // arrange
        // A mutation root field is executed as a serial task and awaited by the work
        // scheduler. Its payload exposes a single async child field that yields, so the
        // child resolver completes on a thread-pool thread while the scheduler loop is
        // waiting for the serial task. Firing many of these concurrently exercises the
        // narrow window where a completion signal can race the scheduler's wait, which
        // would leave a request hung forever.
        var executor =
            await new ServiceCollection()
                .AddGraphQL()
                .AddQueryType(d => d.Field("noop").Resolve("noop"))
                .AddMutationType<RaceMutation>()
                .BuildRequestExecutorAsync(cancellationToken: TestContext.Current.CancellationToken);

        const string mutation =
            """
            mutation {
                doWork {
                    value
                }
            }
            """;

        const int rounds = 40;
        const int requestsPerRound = 200;

        // act
        // assert
        // Each round must complete within the guard; a lost wakeup surfaces here as a
        // TimeoutException instead of hanging the whole test run.
        for (var round = 0; round < rounds; round++)
        {
            var requests = new Task<IExecutionResult>[requestsPerRound];
            for (var i = 0; i < requestsPerRound; i++)
            {
                requests[i] = executor.ExecuteAsync(mutation, TestContext.Current.CancellationToken);
            }

            var results = await Task.WhenAll(requests)
                .WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken);

            foreach (var result in results)
            {
                Assert.Empty(result.ExpectOperationResult().Errors);
            }
        }
    }

    public class RaceMutation
    {
        public RacePayload DoWork() => new();
    }

    public class RacePayload
    {
        public async Task<int> Value()
        {
            await Task.Yield();
            return 1;
        }
    }

    public class Mutation1
    {
        private int _order;
        private bool _a;
        private bool _b;
#if NET9_0_OR_GREATER
        private readonly Lock _sync = new();
#else
        private readonly object _sync = new();
#endif

        public async Task<int> A()
        {
            lock (_sync)
            {
                if (_b)
                {
                    throw new GraphQLException("A");
                }

                _a = true;
            }

            await Task.Delay(100);
            _a = false;
            return Interlocked.Increment(ref _order);
        }

        public async Task<int> B()
        {
            lock (_sync)
            {
                if (_a)
                {
                    throw new GraphQLException("B");
                }

                _b = true;
            }

            await Task.Delay(100);
            _b = false;
            return Interlocked.Increment(ref _order);
        }
    }

    public class Mutation2
    {
        private bool _a;
        private bool _b;
        // ReSharper disable once InconsistentNaming
        internal readonly object _sync = new();

        public bool IsExecutingA => _a;

        public bool IsExecutingB => _a;

        public async Task<SubA> A(CancellationToken cancellationToken)
        {
            lock (_sync)
            {
                if (_b)
                {
                    throw new GraphQLException("A");
                }

                _a = true;
            }

            await Task.Delay(100, cancellationToken);
            _a = false;
            return new SubA(this);
        }

        public async Task<SubB> B(CancellationToken cancellationToken)
        {
            lock (_sync)
            {
                if (_a)
                {
                    throw new GraphQLException("B");
                }

                _b = true;
            }

            await Task.Delay(100, cancellationToken);
            _b = false;
            return new SubB(this);
        }
    }

    public class SubA
    {
        private readonly Mutation2 _mutation;

        public SubA(Mutation2 mutation)
        {
            _mutation = mutation;
        }

        // will be executed in separate task.
        public async Task<bool> A()
        {
            lock (_mutation._sync)
            {
                if (_mutation.IsExecutingB)
                {
                    throw new GraphQLException("A");
                }
            }

            await Task.Delay(100);
            return true;
        }

        // will be folded in.
        public bool B()
        {
            lock (_mutation._sync)
            {
                if (_mutation.IsExecutingA)
                {
                    throw new GraphQLException("B");
                }
            }

            return true;
        }
    }

    public class SubB
    {
        private readonly Mutation2 _mutation;

        public SubB(Mutation2 mutation)
        {
            _mutation = mutation;
        }

        // will be executed in separate task.
        public async Task<bool> A()
        {
            lock (_mutation._sync)
            {
                if (_mutation.IsExecutingA)
                {
                    throw new GraphQLException("B");
                }
            }

            await Task.Delay(100);
            return true;
        }

        // will be folded in.
        public bool B()
        {
            lock (_mutation._sync)
            {
                if (_mutation.IsExecutingA)
                {
                    throw new GraphQLException("B");
                }
            }

            return true;
        }
    }
}
