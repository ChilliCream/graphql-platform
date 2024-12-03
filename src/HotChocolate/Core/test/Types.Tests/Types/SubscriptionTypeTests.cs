// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable AutoPropertyCanBeMadeGetOnly.Global
// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable MemberCanBePrivate.Global

#pragma warning disable CS0618 // Type or member is obsolete
#nullable enable

using System.Runtime.CompilerServices;
using HotChocolate.Execution;
using HotChocolate.Subscriptions;
using HotChocolate.Tests;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Types;

public class SubscriptionTypeTests : TypeTestBase
{
    [Fact]
    public async Task Subscribe_With_Enumerable()
        => await SnapshotTest
            .Create(
                async (snapshot, ct) =>
                {
                    var executor =
                        await new ServiceCollection()
                            .AddGraphQL()
                            .AddSubscriptionType(
                                t => t
                                    .Field("test")
                                    .Type<StringType>()
                                    .Resolve(ctx => ctx.GetEventMessage<string>())
                                    .Subscribe(_ => new List<string> { "a", "b", "c", }))
                            .ModifyOptions(t => t.StrictValidation = false)
                            .BuildRequestExecutorAsync(cancellationToken: ct);

                    var result = await executor.ExecuteAsync(
                        """
                        subscription {
                          test
                        }
                        """,
                        ct);

                    await foreach (var queryResult in result.ExpectResponseStream()
                        .ReadResultsAsync().WithCancellation(ct))
                    {
                        snapshot.Add(queryResult);
                    }
                })
            .MatchAsync();

    [Fact]
    public async Task Subscribe_With_Enumerable_Async()
        => await SnapshotTest
            .Create(
                async (snapshot, ct) =>
                {
                    var executor =
                        await new ServiceCollection()
                            .AddGraphQL()
                            .AddSubscriptionType(
                                t => t
                                    .Field("test")
                                    .Type<StringType>()
                                    .Resolve(ctx => ctx.GetEventMessage<string>())
                                    .Subscribe(
                                        _ => Task.FromResult<IEnumerable<string>>(
                                            new List<string> { "a", "b", "c", })))
                            .ModifyOptions(t => t.StrictValidation = false)
                            .BuildRequestExecutorAsync(cancellationToken: ct);

                    var result = await executor.ExecuteAsync(
                        """
                        subscription {
                          test
                        }
                        """,
                        ct);

                    await foreach (var queryResult in result.ExpectResponseStream()
                        .ReadResultsAsync().WithCancellation(ct))
                    {
                        snapshot.Add(queryResult);
                    }
                })
            .MatchAsync();

    [Fact]
    public async Task Subscribe_With_Observable()
        => await SnapshotTest
            .Create(
                async (snapshot, ct) =>
                {
                    var observable = new TestObservable();

                    var executor =
                        await new ServiceCollection()
                            .AddGraphQL()
                            .AddSubscriptionType(
                                t => t
                                    .Field("test")
                                    .Type<StringType>()
                                    .Resolve(ctx => ctx.GetEventMessage<string>())
                                    .Subscribe(_ => observable))
                            .ModifyOptions(t => t.StrictValidation = false)
                            .BuildRequestExecutorAsync(cancellationToken: ct);

                    var result = await executor.ExecuteAsync(
                        """
                        subscription {
                          test
                        }
                        """,
                        ct);

                    await foreach (var queryResult in result.ExpectResponseStream()
                        .ReadResultsAsync().WithCancellation(ct))
                    {
                        snapshot.Add(queryResult);
                    }

                    Assert.True(observable.DisposeRaised);
                })
            .MatchAsync();

    [Fact]
    public async Task Subscribe_With_Observable_Async()
        => await SnapshotTest
            .Create(
                async (snapshot, ct) =>
                {
                    var observable = new TestObservable();

                    var executor =
                        await new ServiceCollection()
                            .AddGraphQL()
                            .AddSubscriptionType(
                                t => t
                                    .Field("test")
                                    .Type<StringType>()
                                    .Resolve(ctx => ctx.GetEventMessage<string>())
                                    .Subscribe(_ => Task.FromResult<IObservable<string>>(observable)))
                            .ModifyOptions(t => t.StrictValidation = false)
                            .BuildRequestExecutorAsync(cancellationToken: ct);

                    var result = await executor.ExecuteAsync(
                        """
                        subscription {
                          test
                        }
                        """,
                        ct);

                    await foreach (var queryResult in result.ExpectResponseStream()
                        .ReadResultsAsync().WithCancellation(ct))
                    {
                        snapshot.Add(queryResult);
                    }

                    Assert.True(observable.DisposeRaised);
                })
            .MatchAsync();

    [Fact]
    public async Task Subscribe_With_AsyncEnumerable()
        => await SnapshotTest
            .Create(
                async (snapshot, ct) =>
                {
                    var executor =
                        await new ServiceCollection()
                            .AddGraphQL()
                            .AddSubscriptionType(
                                t => t
                                    .Field("test")
                                    .Type<StringType>()
                                    .Resolve(ctx => ctx.GetEventMessage<string>())
                                    .Subscribe(_ => new TestAsyncEnumerable()))
                            .ModifyOptions(t => t.StrictValidation = false)
                            .BuildRequestExecutorAsync(cancellationToken: ct);

                    var result = await executor.ExecuteAsync(
                        """
                        subscription {
                          test
                        }
                        """,
                        ct);

                    await foreach (var queryResult in result.ExpectResponseStream()
                        .ReadResultsAsync().WithCancellation(ct))
                    {
                        snapshot.Add(queryResult);
                    }
                })
            .MatchAsync();

    [Fact]
    public async Task Subscribe_With_AsyncEnumerable_Async()
        => await SnapshotTest
            .Create(
                async (snapshot, ct) =>
                {
                    var executor =
                        await new ServiceCollection()
                            .AddGraphQL()
                            .AddSubscriptionType(
                                t => t
                                    .Field("test")
                                    .Type<StringType>()
                                    .Resolve(ctx => ctx.GetEventMessage<string>())
                                    .Subscribe(
                                        _ => Task.FromResult<IAsyncEnumerable<string>>(
                                            new TestAsyncEnumerable())))
                            .ModifyOptions(t => t.StrictValidation = false)
                            .BuildRequestExecutorAsync(cancellationToken: ct);

                    var result = await executor.ExecuteAsync(
                        """
                        subscription {
                          test
                        }
                        """,
                        ct);

                    await foreach (var queryResult in result.ExpectResponseStream()
                        .ReadResultsAsync().WithCancellation(ct))
                    {
                        snapshot.Add(queryResult);
                    }
                })
            .MatchAsync();

    [InlineData("onSomething")]
    [InlineData("onSomethingTask")]
    [InlineData("onSomethingValueTask")]
    [InlineData("onSomethingObj")]
    [InlineData("onSomethingObjTask")]
    [InlineData("onSomethingObjValueTask")]
    [Theory]
    public async Task SubscribeAndResolve_Attribute_AsyncEnumerable(string field)
        => await SnapshotTest
            .Create(
                async (snapshot, ct) =>
                {
                    var executor =
                        await new ServiceCollection()
                            .AddGraphQL()
                            .AddSubscriptionType<PureCodeFirstAsyncEnumerable>()
                            .ModifyOptions(t => t.StrictValidation = false)
                            .BuildRequestExecutorAsync(cancellationToken: ct);

                    var result = await executor.ExecuteAsync(
                        $"subscription {{ {field} }}",
                        ct);

                    await foreach (var queryResult in result.ExpectResponseStream()
                        .ReadResultsAsync().WithCancellation(ct))
                    {
                        snapshot.Add(queryResult);
                    }
                },
                postFix: field)
            .MatchAsync();

    [InlineData("onSomething")]
    [Theory]
    public async Task SubscribeAndResolve_Attribute_ISourceStream(string field)
        => await SnapshotTest
            .Create(
                async (snapshot, ct) =>
                {
                    var executor = await TestHelper.CreateExecutorAsync(
                        r => r
                            .AddInMemorySubscriptions()
                            .AddQueryType(c => c.Name("Query").Field("a").Resolve("b"))
                            .AddMutationType<MyMutation>()
                            .AddSubscriptionType<PureCodeFirstSourceStream>());

                    var subscriptionResult = await executor.ExecuteAsync(
                        "subscription { " + field + " (userId: \"1\") }",
                        ct);

                    var mutationResult = await executor.ExecuteAsync(
                        "mutation { writeBoolean(userId: \"1\" message: true) }",
                        ct);
                    Assert.Null(mutationResult.ExpectOperationResult().Errors);

                    await foreach (var queryResult in subscriptionResult.ExpectResponseStream()
                        .ReadResultsAsync().WithCancellation(ct))
                    {
                        snapshot.Add(queryResult);
                        break;
                    }
                },
                postFix: field)
            .MatchAsync();

    [InlineData("onSomething")]
    [InlineData("onSomethingTask")]
    [InlineData("onSomethingValueTask")]
    [InlineData("onSomethingObj")]
    [InlineData("onSomethingObjTask")]
    [InlineData("onSomethingObjValueTask")]
    [Theory]
    public async Task SubscribeAndResolve_Attribute_Enumerable(string field)
        => await SnapshotTest
            .Create(
                async (snapshot, ct) =>
                {
                    var executor =
                        await new ServiceCollection()
                            .AddGraphQL()
                            .AddSubscriptionType<PureCodeFirstEnumerable>()
                            .ModifyOptions(t => t.StrictValidation = false)
                            .BuildRequestExecutorAsync(cancellationToken: ct);

                    var result = await executor.ExecuteAsync(
                        $"subscription {{ {field} }}",
                        ct);

                    await foreach (var queryResult in result.ExpectResponseStream()
                        .ReadResultsAsync().WithCancellation(ct))
                    {
                        snapshot.Add(queryResult);
                    }
                },
                postFix: field)
            .MatchAsync();

    [InlineData("onSomething")]
    [InlineData("onSomethingTask")]
    [InlineData("onSomethingValueTask")]
    [InlineData("onSomethingObj")]
    [InlineData("onSomethingObjTask")]
    [InlineData("onSomethingObjValueTask")]
    [Theory]
    public async Task SubscribeAndResolve_Attribute_Queryable(string field)
        => await SnapshotTest
            .Create(
                async (snapshot, ct) =>
                {
                    var executor =
                        await new ServiceCollection()
                            .AddGraphQL()
                            .AddSubscriptionType<PureCodeFirstQueryable>()
                            .ModifyOptions(t => t.StrictValidation = false)
                            .BuildRequestExecutorAsync(cancellationToken: ct);

                    var result = await executor.ExecuteAsync(
                        $"subscription {{ {field} }}",
                        ct);

                    await foreach (var queryResult in result.ExpectResponseStream()
                        .ReadResultsAsync().WithCancellation(ct))
                    {
                        snapshot.Add(queryResult);
                    }
                },
                postFix: field)
            .MatchAsync();

    [InlineData("onSomething")]
    [InlineData("onSomethingTask")]
    [InlineData("onSomethingValueTask")]
    [InlineData("onSomethingObj")]
    [InlineData("onSomethingObjTask")]
    [InlineData("onSomethingObjValueTask")]
    [Theory]
    public async Task SubscribeAndResolve_Attribute_Observable(string field)
        => await SnapshotTest
            .Create(
                async (snapshot, ct) =>
                {
                    var executor =
                        await new ServiceCollection()
                            .AddGraphQL()
                            .AddSubscriptionType<PureCodeFirstObservable>()
                            .ModifyOptions(t => t.StrictValidation = false)
                            .BuildRequestExecutorAsync(cancellationToken: ct);

                    var result = await executor.ExecuteAsync(
                        $"subscription {{ {field} }}",
                        ct);

                    await foreach (var queryResult in result.ExpectResponseStream()
                        .ReadResultsAsync().WithCancellation(ct))
                    {
                        snapshot.Add(queryResult);
                    }
                },
                postFix: field)
            .MatchAsync();

    [Fact]
    public async Task Subscribe_Attribute_With_Argument_Topic()
        => await SnapshotTest
            .Create(
                async (snapshot, ct) =>
                {
                    var executor = await TestHelper.CreateExecutorAsync(
                        r => r
                            .AddInMemorySubscriptions()
                            .AddQueryType(c => c.Name("Query").Field("a").Resolve("b"))
                            .AddMutationType<MyMutation>()
                            .AddSubscriptionType<MySubscription>());

                    // act
                    await using var subscriptionResult = await executor.ExecuteAsync(
                        "subscription { onMessage(userId: \"abc\") }",
                        ct);
                    var results = subscriptionResult.ExpectResponseStream().ReadResultsAsync();

                    // assert
                    var mutationResult = await executor.ExecuteAsync(
                        "mutation { writeMessage(userId: \"abc\" message: \"def\") }",
                        ct);
                    Assert.Null(mutationResult.ExpectOperationResult().Errors);

                    await foreach (var queryResult in
                        results.WithCancellation(ct).ConfigureAwait(false))
                    {
                        snapshot.Add(queryResult);
                        break;
                    }
                })
            .MatchAsync();

    [Fact]
    public async Task Subscribe_Attribute_With_Static_Topic_Defined_On_Attribute()
        => await SnapshotTest
            .Create(
                async (snapshot, ct) =>
                {
                    var executor = await TestHelper.CreateExecutorAsync(
                        r => r
                            .AddInMemorySubscriptions()
                            .AddQueryType(c => c.Name("Query").Field("a").Resolve("b"))
                            .AddMutationType<MyMutation>()
                            .AddSubscriptionType<MySubscription>());

                    // act
                    await using var subscriptionResult = await executor.ExecuteAsync(
                        "subscription { onFixedMessage }",
                        ct);
                    var results = subscriptionResult.ExpectResponseStream().ReadResultsAsync();

                    // assert
                    var mutationResult = await executor.ExecuteAsync(
                        "mutation { writeFixedMessage(message: \"def\") }",
                        ct);
                    Assert.Null(mutationResult.ExpectOperationResult().Errors);

                    await foreach (var queryResult in
                        results.WithCancellation(ct).ConfigureAwait(false))
                    {
                        snapshot.Add(queryResult);
                        break;
                    }
                })
            .MatchAsync();

    [Fact]
    public async Task Subscribe_Attribute_With_Static_Topic()
        => await SnapshotTest
            .Create(
                async (snapshot, ct) =>
                {
                    var executor = await TestHelper.CreateExecutorAsync(
                        r => r
                            .AddInMemorySubscriptions()
                            .AddQueryType(c => c.Name("Query").Field("a").Resolve("b"))
                            .AddMutationType<MyMutation>()
                            .AddSubscriptionType<MySubscription>());

                    // act
                    await using var subscriptionResult = await executor.ExecuteAsync(
                        "subscription { onSysMessage }",
                        ct);
                    var results = subscriptionResult.ExpectResponseStream().ReadResultsAsync();

                    // assert
                    var mutationResult = await executor.ExecuteAsync(
                        "mutation { writeSysMessage(message: \"def\") }",
                        ct);
                    Assert.Null(mutationResult.ExpectOperationResult().Errors);

                    await foreach (var queryResult in
                        results.WithCancellation(ct).ConfigureAwait(false))
                    {
                        snapshot.Add(queryResult);
                        break;
                    }
                })
            .MatchAsync();

    [Fact]
    public async Task Subscribe_Attribute_With_Static_Topic_Infer_Topic()
        => await SnapshotTest
            .Create(
                async (snapshot, ct) =>
                {
                    var executor = await TestHelper.CreateExecutorAsync(
                        r => r
                            .AddInMemorySubscriptions()
                            .AddQueryType(c => c.Name("Query").Field("a").Resolve("b"))
                            .AddMutationType<MyMutation>()
                            .AddSubscriptionType<MySubscription>());

                    // act
                    await using var subscriptionResult = await executor.ExecuteAsync(
                        "subscription { onInferTopic }",
                        ct);
                    var results = subscriptionResult.ExpectResponseStream().ReadResultsAsync();

                    // assert
                    var mutationResult = await executor.ExecuteAsync(
                        "mutation { writeOnInferTopic(message: \"def\") }",
                        ct);
                    Assert.Null(mutationResult.ExpectOperationResult().Errors);

                    await foreach (var queryResult in
                        results.WithCancellation(ct).ConfigureAwait(false))
                    {
                        snapshot.Add(queryResult);
                        break;
                    }
                })
            .MatchAsync();

    [Fact]
    public async Task Subscribe_Attribute_With_Explicitly_Defined_Subscribe()
        => await SnapshotTest
            .Create(
                async (snapshot, ct) =>
                {
                    var executor = await TestHelper.CreateExecutorAsync(
                        r => r
                            .AddInMemorySubscriptions()
                            .AddQueryType(c => c.Name("Query").Field("a").Resolve("b"))
                            .AddMutationType<MyMutation>()
                            .AddSubscriptionType<MySubscription>());

                    // act
                    await using var subscriptionResult = await executor.ExecuteAsync(
                        "subscription { onExplicit }",
                        ct);
                    var results = subscriptionResult.ExpectResponseStream().ReadResultsAsync();

                    // assert
                    var mutationResult = await executor.ExecuteAsync(
                        "mutation { writeOnExplicit(message: \"def\") }",
                        ct);
                    Assert.Null(mutationResult.ExpectOperationResult().Errors);

                    await foreach (var queryResult in
                        results.WithCancellation(ct).ConfigureAwait(false))
                    {
                        snapshot.Add(queryResult);
                        break;
                    }
                })
            .MatchAsync();

    [Fact]
    public async Task Subscribe_Attribute_Schema_Is_Generated_Correctly()
    {
        // arrange
        var snapshot = new Snapshot();

        // act
        var executor = await TestHelper.CreateExecutorAsync(
            r => r
                .AddInMemorySubscriptions()
                .AddQueryType(c => c.Name("Query").Field("a").Resolve("b"))
                .AddMutationType<MyMutation>()
                .AddSubscriptionType<MySubscription>());

        // assert
        snapshot
            .Add(executor.Schema)
            .MatchMarkdown();
    }

    [Fact]
    public async Task Subscribe_Attribute_Schema_Is_Generated_Correctly_2()
    {
        // arrange
        var snapshot = new Snapshot();

        // act
        var executor = await TestHelper.CreateExecutorAsync(
            r => r
                .AddInMemorySubscriptions()
                .AddQueryType(c => c.Name("Query").Field("a").Resolve("b"))
                .AddSubscriptionType(d => d.Name("Subscription"))
                .AddTypeExtension<MySubscriptionExtension>());

        // assert
        snapshot
            .Add(executor.Schema)
            .MatchMarkdown();
    }

    [Fact]
    public async Task Arguments_Can_Be_Declared_On_The_Stream_Schema()
    {
        // arrange
        var snapshot = new Snapshot();

        // act
        var executor = await TestHelper.CreateExecutorAsync(
            r => r
                .AddInMemorySubscriptions()
                .AddQueryType(c => c.Name("Query").Field("a").Resolve("b"))
                .AddSubscriptionType<MySubscription>());

        // assert
        snapshot
            .Add(executor.Schema)
            .MatchMarkdown();
    }

    [Fact]
    public async Task Arguments_Can_Be_Declared_On_The_Stream()
    {
        // arrange
        // act
        var executor = await TestHelper.CreateExecutorAsync(
            r => r
                .AddInMemorySubscriptions()
                .AddQueryType(c => c.Name("Query").Field("a").Resolve("b"))
                .AddSubscriptionType<MySubscription>());

        var result = await executor.ExecuteAsync("subscription { onArguments(arg: \"abc\") }");

        // assert
        result.MatchInlineSnapshot(
            """
            {
              "data": {
                "onArguments": "abc"
              }
            }

            """);
    }

    [Fact]
    public async Task Subscription_Directives_Are_Allowed()
    {
        // arrange
        // act
        var executor = await new ServiceCollection()
            .AddGraphQLServer()
            .AddDocumentFromString(
                """
                type Subscription {
                  bookAdded: String!
                }

                directive @bug(test: Int!) on SUBSCRIPTION
                """)
            .BindRuntimeType<SubscriptionWithDirective>("Subscription")
            .ModifyOptions(o => o.StrictValidation = false)
            .BuildRequestExecutorAsync();

        var result = await executor.ExecuteAsync(
            """
            subscription test @bug(test: 123) {
              bookAdded
            }
            """);

        // assert
        result.MatchInlineSnapshot(
            """
            {
              "data": {
                "bookAdded": "foo"
              }
            }

            """);
    }

    public class TestObservable : IObservable<string>, IDisposable
    {
        public bool DisposeRaised { get; private set; }

        public IDisposable Subscribe(IObserver<string> observer)
        {
            Task.Run(
                async () =>
                {
                    await Task.Delay(250);

                    foreach (var s in new[] { "a", "b", "c", })
                    {
                        observer.OnNext(s);
                    }

                    observer.OnCompleted();
                });

            return this;
        }

        public void Dispose()
        {
            DisposeRaised = true;
        }
    }

    public class TestAsyncEnumerable
        : IAsyncEnumerable<string>
    {
        public async IAsyncEnumerator<string> GetAsyncEnumerator(
            CancellationToken cancellationToken = default)
        {
            await Task.Delay(50, cancellationToken);
            yield return "a";
            yield return "b";
            yield return "c";
        }
    }

    public class PureCodeFirstAsyncEnumerable
    {
        [SubscribeAndResolve]
        public async IAsyncEnumerable<string?> OnSomething()
        {
            await Task.Delay(50);
            yield return "a";
            yield return "b";
            yield return "c";
        }

        [SubscribeAndResolve]
        public Task<IAsyncEnumerable<string?>> OnSomethingTask()
        {
            return Task.FromResult(OnSomething());
        }

        [SubscribeAndResolve]
        public ValueTask<IAsyncEnumerable<string?>> OnSomethingValueTask()
        {
            return new(OnSomething());
        }

        [GraphQLType(typeof(StringType))]
        [SubscribeAndResolve]
        public async IAsyncEnumerable<object?> OnSomethingObj()
        {
            await Task.Delay(50);
            yield return "a";
            yield return "b";
            yield return "c";
        }

        [GraphQLType(typeof(StringType))]
        [SubscribeAndResolve]
        public Task<IAsyncEnumerable<object?>> OnSomethingObjTask()
        {
            return Task.FromResult(OnSomethingObj());
        }

        [GraphQLType(typeof(StringType))]
        [SubscribeAndResolve]
        public ValueTask<IAsyncEnumerable<object?>> OnSomethingObjValueTask()
        {
            return new(OnSomethingObj());
        }
    }

    public class PureCodeFirstSourceStream
    {
        [SubscribeAndResolve]
        public ValueTask<ISourceStream<bool>> OnSomething(
            string userId,
            [Service] ITopicEventReceiver receiver)
            => receiver.SubscribeAsync<bool>(userId);
    }

    public class PureCodeFirstEnumerable
    {
        [SubscribeAndResolve]
        public IEnumerable<string?> OnSomething()
        {
            yield return "a";
            yield return "b";
            yield return "c";
        }

        [SubscribeAndResolve]
        public Task<IEnumerable<string?>> OnSomethingTask()
        {
            return Task.FromResult(OnSomething());
        }

        [SubscribeAndResolve]
        public ValueTask<IEnumerable<string?>> OnSomethingValueTask()
        {
            return new(OnSomething());
        }

        [GraphQLType(typeof(StringType))]
        [SubscribeAndResolve]
        public IEnumerable<object?> OnSomethingObj()
        {
            yield return "a";
            yield return "b";
            yield return "c";
        }

        [GraphQLType(typeof(StringType))]
        [SubscribeAndResolve]
        public Task<IEnumerable<object?>> OnSomethingObjTask()
        {
            return Task.FromResult(OnSomethingObj());
        }

        [GraphQLType(typeof(StringType))]
        [SubscribeAndResolve]
        public ValueTask<IEnumerable<object?>> OnSomethingObjValueTask()
        {
            return new(OnSomethingObj());
        }
    }

    public class PureCodeFirstQueryable
    {
        private readonly List<string> _strings = ["a", "b", "c",];

        [SubscribeAndResolve]
        public IQueryable<string?> OnSomething() => _strings.AsQueryable();

        [SubscribeAndResolve]
        public Task<IQueryable<string?>> OnSomethingTask()
        {
            return Task.FromResult(OnSomething());
        }

        [SubscribeAndResolve]
        public ValueTask<IQueryable<string?>> OnSomethingValueTask()
        {
            return new(OnSomething());
        }

        [GraphQLType(typeof(StringType))]
        [SubscribeAndResolve]
        public IQueryable<object?> OnSomethingObj() => _strings.Cast<object>().AsQueryable();

        [GraphQLType(typeof(StringType))]
        [SubscribeAndResolve]
        public Task<IQueryable<object?>> OnSomethingObjTask()
        {
            return Task.FromResult(OnSomethingObj());
        }

        [GraphQLType(typeof(StringType))]
        [SubscribeAndResolve]
        public ValueTask<IQueryable<object?>> OnSomethingObjValueTask()
        {
            return new(OnSomethingObj());
        }
    }

    public class PureCodeFirstObservable
    {
        [SubscribeAndResolve]
        public IObservable<string?> OnSomething() => new StringObservable();

        [SubscribeAndResolve]
        public Task<IObservable<string?>> OnSomethingTask()
        {
            return Task.FromResult(OnSomething());
        }

        [SubscribeAndResolve]
        public ValueTask<IObservable<string?>> OnSomethingValueTask()
        {
            return new(OnSomething());
        }

        [GraphQLType(typeof(StringType))]
        [SubscribeAndResolve]
        public IObservable<object?> OnSomethingObj() => new StringObservable();

        [GraphQLType(typeof(StringType))]
        [SubscribeAndResolve]
        public Task<IObservable<object?>> OnSomethingObjTask()
        {
            return Task.FromResult(OnSomethingObj());
        }

        [GraphQLType(typeof(StringType))]
        [SubscribeAndResolve]
        public ValueTask<IObservable<object?>> OnSomethingObjValueTask()
        {
            return new(OnSomethingObj());
        }

        private sealed class StringObservable
            : IObservable<string>
                , IObservable<object>
        {
            public IDisposable Subscribe(IObserver<string> observer)
            {
                return new Subscription(observer);
            }

            public IDisposable Subscribe(IObserver<object> observer)
            {
                return new Subscription(observer);
            }

            private sealed class Subscription : IDisposable
            {
                public Subscription(IObserver<string> observer)
                {
                    new Thread(
                        () =>
                        {
                            observer.OnNext("a");
                            observer.OnNext("b");
                            observer.OnNext("c");
                            observer.OnCompleted();
                        }).Start();
                }

                public Subscription(IObserver<object> observer)
                {
                    new Thread(
                        () =>
                        {
                            observer.OnNext("a");
                            observer.OnNext("b");
                            observer.OnNext("c");
                            observer.OnCompleted();
                        }).Start();
                }

                public void Dispose() { }
            }
        }
    }

    public class MyMutation
    {
        public bool WriteBoolean(
            string userId,
            bool message,
            [Service] ITopicEventSender eventSender)
        {
            eventSender.SendAsync(userId, message);
            return message;
        }

        public string WriteMessage(
            string userId,
            string message,
            [Service] ITopicEventSender eventSender)
        {
            eventSender.SendAsync(userId, message);
            return message;
        }

        public string WriteSysMessage(
            string message,
            [Service] ITopicEventSender eventSender)
        {
            eventSender.SendAsync("OnSysMessage", message);
            return message;
        }

        public string WriteFixedMessage(
            string message,
            [Service] ITopicEventSender eventSender)
        {
            eventSender.SendAsync("Fixed", message);
            return message;
        }

        public string WriteOnInferTopic(
            string message,
            [Service] ITopicEventSender eventSender)
        {
            eventSender.SendAsync("OnInferTopic", message);
            return message;
        }

        public string WriteOnExplicit(
            string message,
            [Service] ITopicEventSender eventSender)
        {
            eventSender.SendAsync("explicit", message);
            return message;
        }
    }

    public class MySubscription
    {
        [Subscribe]
        [Topic("{userId}")]
        public string OnMessage(
            string userId,
            [EventMessage] string message) =>
            message;

        [Subscribe]
        [Topic]
        public string OnSysMessage(
            [EventMessage] string message) =>
            message;

        [Subscribe]
        [Topic("Fixed")]
        public string OnFixedMessage(
            [EventMessage] string message) =>
            message;

        [Subscribe]
        public string OnInferTopic(
            [EventMessage] string message) =>
            message;

        public ValueTask<ISourceStream<string>> SubscribeToOnExplicit(
            [Service] ITopicEventReceiver eventReceiver) =>
            eventReceiver.SubscribeAsync<string>("explicit");

        [Subscribe(With = nameof(SubscribeToOnExplicit))]
        public string OnExplicit(
            [EventMessage] string message) =>
            message;

        public ValueTask<ISourceStream> SubscribeToOnExplicitNonGeneric(
            [Service] ITopicEventReceiver eventReceiver) =>
            default;

        [Subscribe(With = nameof(SubscribeToOnExplicitNonGeneric))]
        public string OnExplicitNonGeneric(
            [EventMessage] string message) =>
            message;

        public ISourceStream SubscribeToOnExplicitNonGenericSync(
            [Service] ITopicEventReceiver eventReceiver) =>
            default!;

        [Subscribe(With = nameof(SubscribeToOnExplicitNonGenericSync))]
        public string OnExplicitNonGenericSync(
            [EventMessage] string message) =>
            message;

        public ISourceStream<string> SubscribeToOnExplicitSync(
            [Service] ITopicEventReceiver eventReceiver) =>
            default!;

        [Subscribe(With = nameof(SubscribeToOnExplicitSync))]
        public string OnExplicitSync(
            [EventMessage] string message) =>
            message;

        public async IAsyncEnumerable<string> CreateOnArguments(string arg)
        {
            await Task.Delay(1);

            yield return arg;
        }

        [Subscribe(With = nameof(CreateOnArguments))]
        public string OnArguments([EventMessage] string s) => s;
    }

    [ExtendObjectType("Subscription")]
    public class MySubscriptionExtension
    {
        public async ValueTask<ISourceStream<string>> SubscribeToOnExplicit(
            ITopicEventReceiver eventReceiver) =>
            await eventReceiver.SubscribeAsync<string>("explicit");

        [Subscribe(With = nameof(SubscribeToOnExplicit))]
        public string OnExplicit(
            [EventMessage] string message) =>
            message;
    }


    public class SubscriptionWithDirective
    {
        [Subscribe(With = nameof(GetStream))]
        public string BookAdded([EventMessage] string foo) => foo;

        private async IAsyncEnumerable<string> GetStream(
            [EnumeratorCancellation] CancellationToken ct = default)
        {
            await Task.Delay(200, ct);
            yield return "foo";
        }
    }
}
