using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate.Execution;
using HotChocolate.Subscriptions;
using Microsoft.Extensions.DependencyInjection;
using Snapshooter;
using Snapshooter.Xunit;
using Xunit;

#nullable enable

namespace HotChocolate.Types
{
    public class SubscriptionTypeTests
        : TypeTestBase
    {
        [Fact]
        public async Task Subscribe_With_Enumerable()
        {
            // arrange
            using var cts = new CancellationTokenSource(30000);

            // act
            ISchema schema = SchemaBuilder.New()
                .AddSubscriptionType(t => t
                    .Field("test")
                    .Type<StringType>()
                    .Resolver(ctx => ctx.CustomProperty<string>(WellKnownContextData.EventMessage))
                    .Subscribe(ctx => new List<string> { "a", "b", "c" }))
                .ModifyOptions(t => t.StrictValidation = false)
                .Create();

            // assert
            IQueryExecutor executor = schema.MakeExecutable();
            var stream = (IResponseStream)await executor.ExecuteAsync(
                "subscription { test }", cts.Token);

            var results = new List<IReadOnlyQueryResult>();
            await foreach (IReadOnlyQueryResult result in stream.WithCancellation(cts.Token))
            {
                results.Add(result);
            }

            results.MatchSnapshot();
        }

        [Fact]
        public async Task Subscribe_With_Enumerable_Async()
        {
            // arrange
            using var cts = new CancellationTokenSource(30000);

            // act
            ISchema schema = SchemaBuilder.New()
                .AddSubscriptionType(t => t
                    .Field("test")
                    .Type<StringType>()
                    .Resolver(ctx => ctx.CustomProperty<string>(WellKnownContextData.EventMessage))
                    .Subscribe(ctx => Task.FromResult<IEnumerable<string>>(
                        new List<string> { "a", "b", "c" })))
                .ModifyOptions(t => t.StrictValidation = false)
                .Create();

            // assert
            IQueryExecutor executor = schema.MakeExecutable();
            var stream = (IResponseStream)await executor.ExecuteAsync(
                "subscription { test }", cts.Token);

            var results = new List<IReadOnlyQueryResult>();
            await foreach (IReadOnlyQueryResult result in stream.WithCancellation(cts.Token))
            {
                results.Add(result);
            }

            results.MatchSnapshot();
        }

        [Fact]
        public async Task Subscribe_With_Observable()
        {
            // arrange
            using var cts = new CancellationTokenSource(30000);
            var observable = new TestObservable();

            // act
            ISchema schema = SchemaBuilder.New()
                .AddSubscriptionType(t => t
                    .Field("test")
                    .Type<StringType>()
                    .Resolver(ctx => ctx.CustomProperty<string>(WellKnownContextData.EventMessage))
                    .Subscribe(ctx => observable))
                .ModifyOptions(t => t.StrictValidation = false)
                .Create();

            // assert
            IQueryExecutor executor = schema.MakeExecutable();
            var stream = (IResponseStream)await executor.ExecuteAsync(
                "subscription { test }", cts.Token);

            var results = new List<IReadOnlyQueryResult>();
            await foreach (IReadOnlyQueryResult result in stream.WithCancellation(cts.Token))
            {
                results.Add(result);
            }

            Assert.True(observable.DisposeRaised);
            results.MatchSnapshot();
        }

        [Fact]
        public async Task Subscribe_With_Observable_Async()
        {
            // arrange
            using var cts = new CancellationTokenSource(30000);
            var observable = new TestObservable();

            // act
            ISchema schema = SchemaBuilder.New()
                .AddSubscriptionType(t => t
                    .Field("test")
                    .Type<StringType>()
                    .Resolver(ctx => ctx.CustomProperty<string>(WellKnownContextData.EventMessage))
                    .Subscribe(ctx => Task.FromResult<IObservable<string>>(observable)))
                .ModifyOptions(t => t.StrictValidation = false)
                .Create();

            // assert
            IQueryExecutor executor = schema.MakeExecutable();
            var stream = (IResponseStream)await executor.ExecuteAsync(
                "subscription { test }", cts.Token);

            var results = new List<IReadOnlyQueryResult>();
            await foreach (IReadOnlyQueryResult result in stream.WithCancellation(cts.Token))
            {
                results.Add(result);
            }

            Assert.True(observable.DisposeRaised);
            results.MatchSnapshot();
        }

        [Fact]
        public async Task Subscribe_With_AsyncEnumerable()
        {
            // arrange
            using var cts = new CancellationTokenSource(30000);

            // act
            ISchema schema = SchemaBuilder.New()
                .AddSubscriptionType(t => t
                    .Field("test")
                    .Type<StringType>()
                    .Resolver(ctx => ctx.CustomProperty<string>(WellKnownContextData.EventMessage))
                    .Subscribe(ctx => new TestAsyncEnumerable()))
                .ModifyOptions(t => t.StrictValidation = false)
                .Create();

            // assert
            IQueryExecutor executor = schema.MakeExecutable();
            var stream = (IResponseStream)await executor.ExecuteAsync(
                "subscription { test }", cts.Token);

            var results = new List<IReadOnlyQueryResult>();
            await foreach (IReadOnlyQueryResult result in stream.WithCancellation(cts.Token))
            {
                results.Add(result);
            }

            results.MatchSnapshot();
        }

        [Fact]
        public async Task Subscribe_With_AsyncEnumerable_Async()
        {
            // arrange
            using var cts = new CancellationTokenSource(30000);

            // act
            ISchema schema = SchemaBuilder.New()
                .AddSubscriptionType(t => t
                    .Field("test")
                    .Type<StringType>()
                    .Resolver(ctx => ctx.CustomProperty<string>(WellKnownContextData.EventMessage))
                    .Subscribe(ctx => Task.FromResult<IAsyncEnumerable<string>>(
                        new TestAsyncEnumerable())))
                .ModifyOptions(t => t.StrictValidation = false)
                .Create();

            // assert
            IQueryExecutor executor = schema.MakeExecutable();
            var stream = (IResponseStream)await executor.ExecuteAsync(
                "subscription { test }", cts.Token);

            var results = new List<IReadOnlyQueryResult>();
            await foreach (IReadOnlyQueryResult result in stream.WithCancellation(cts.Token))
            {
                results.Add(result);
            }

            results.MatchSnapshot();
        }

        [InlineData("onSomething")]
        [InlineData("onSomethingTask")]
        [InlineData("onSomethingValueTask")]
        [InlineData("onSomethingObj")]
        [InlineData("onSomethingObjTask")]
        [InlineData("onSomethingObjValueTask")]
        [Theory]
        public async Task Subscribe_Attribute_AsyncEnumerable(string field)
        {
            // arrange
            using var cts = new CancellationTokenSource(30000);

            // act
            ISchema schema = SchemaBuilder.New()
                .AddSubscriptionType<PureCodeFirstAsyncEnumerable>()
                .ModifyOptions(t => t.StrictValidation = false)
                .Create();

            // assert
            IQueryExecutor executor = schema.MakeExecutable();
            var stream = (IResponseStream)await executor.ExecuteAsync(
                "subscription { " + field + " }", cts.Token);

            var results = new List<IReadOnlyQueryResult>();
            await foreach (IReadOnlyQueryResult result in stream.WithCancellation(cts.Token))
            {
                results.Add(result);
            }

            results.MatchSnapshot(new SnapshotNameExtension(field));
        }

        [InlineData("onSomething")]
        [InlineData("onSomethingTask")]
        [InlineData("onSomethingValueTask")]
        [InlineData("onSomethingObj")]
        [InlineData("onSomethingObjTask")]
        [InlineData("onSomethingObjValueTask")]
        [Theory]
        public async Task Subscribe_Attribute_Enumerable(string field)
        {
            // arrange
            using var cts = new CancellationTokenSource(30000);

            // act
            ISchema schema = SchemaBuilder.New()
                .AddSubscriptionType<PureCodeFirstEnumerable>()
                .ModifyOptions(t => t.StrictValidation = false)
                .Create();

            // assert
            IQueryExecutor executor = schema.MakeExecutable();
            var stream = (IResponseStream)await executor.ExecuteAsync(
                "subscription { " + field + " }", cts.Token);

            var results = new List<IReadOnlyQueryResult>();
            await foreach (IReadOnlyQueryResult result in stream.WithCancellation(cts.Token))
            {
                results.Add(result);
            }

            results.MatchSnapshot(new SnapshotNameExtension(field));
        }

        [InlineData("onSomething")]
        [InlineData("onSomethingTask")]
        [InlineData("onSomethingValueTask")]
        [InlineData("onSomethingObj")]
        [InlineData("onSomethingObjTask")]
        [InlineData("onSomethingObjValueTask")]
        [Theory]
        public async Task Subscribe_Attribute_Queryable(string field)
        {
            // arrange
            using var cts = new CancellationTokenSource(30000);

            // act
            ISchema schema = SchemaBuilder.New()
                .AddSubscriptionType<PureCodeFirstQueryable>()
                .ModifyOptions(t => t.StrictValidation = false)
                .Create();

            // assert
            IQueryExecutor executor = schema.MakeExecutable();
            var stream = (IResponseStream)await executor.ExecuteAsync(
                "subscription { " + field + " }", cts.Token);

            var results = new List<IReadOnlyQueryResult>();
            await foreach (IReadOnlyQueryResult result in stream.WithCancellation(cts.Token))
            {
                results.Add(result);
            }

            results.MatchSnapshot(new SnapshotNameExtension(field));
        }

        [InlineData("onSomething")]
        [InlineData("onSomethingTask")]
        [InlineData("onSomethingValueTask")]
        [InlineData("onSomethingObj")]
        [InlineData("onSomethingObjTask")]
        [InlineData("onSomethingObjValueTask")]
        [Theory]
        public async Task Subscribe_Attribute_Observable(string field)
        {
            // arrange
            using var cts = new CancellationTokenSource(30000);

            // act
            ISchema schema = SchemaBuilder.New()
                .AddSubscriptionType<PureCodeFirstObservable>()
                .ModifyOptions(t => t.StrictValidation = false)
                .Create();

            // assert
            IQueryExecutor executor = schema.MakeExecutable();
            var stream = (IResponseStream)await executor.ExecuteAsync(
                "subscription { " + field + " }", cts.Token);

            var results = new List<IReadOnlyQueryResult>();
            await foreach (IReadOnlyQueryResult result in stream.WithCancellation(cts.Token))
            {
                results.Add(result);
            }

            results.MatchSnapshot(new SnapshotNameExtension(field));
        }

        [Fact]
        public async Task SubscribeAndResolveAttribute()
        {
            // arrange
            using var cts = new CancellationTokenSource(30000);

            // act
            ISchema schema = SchemaBuilder.New()
                .AddSubscriptionType<WithSubscribeAndResolve>()
                .ModifyOptions(t => t.StrictValidation = false)
                .Create();

            // assert
            IQueryExecutor executor = schema.MakeExecutable();
            var stream = (IResponseStream)await executor.ExecuteAsync(
                "subscription { onFoo }", cts.Token);

            var results = new List<IReadOnlyQueryResult>();
            await foreach (IReadOnlyQueryResult result in stream.WithCancellation(cts.Token))
            {
                results.Add(result);
            }

            results.MatchSnapshot();
        }

        [Fact]
        public async Task Subscribe_Attribute_With_Argument_Topic()
        {
            // arrange
            using var cts = new CancellationTokenSource(30000);

            var serviceCollection = new ServiceCollection();
            serviceCollection
                .AddInMemorySubscriptions()
                .AddGraphQLSchema(
                    SchemaBuilder.New()
                        .AddQueryType(c => c.Name("Query").Field("a").Resolver("b"))
                        .AddMutationType<MyMutation>()
                        .AddSubscriptionType<MySubscription>())
                .AddQueryExecutor();

            IQueryExecutor executor = serviceCollection
                .BuildServiceProvider()
                .GetRequiredService<IQueryExecutor>();

            // act
            var stream = (IResponseStream)await executor.ExecuteAsync(
                "subscription { onMessage(userId: \"abc\") }",
                cts.Token);

            // assert
            IExecutionResult mutationResult = await executor.ExecuteAsync(
                "mutation { writeMessage(userId: \"abc\" message: \"def\") }",
                cts.Token);
            Assert.Empty(mutationResult.Errors);

            var results = new StringBuilder();
            await foreach (IReadOnlyQueryResult queryResult in
                stream.WithCancellation(cts.Token))
            {
                results.AppendLine(queryResult.ToJson());
                break;
            }

            results.ToString().MatchSnapshot();
        }

        [Fact]
        public async Task Subscribe_Attribute_With_Static_Topic_Defined_On_Attribute()
        {
            // arrange
            using var cts = new CancellationTokenSource(30000);

            var serviceCollection = new ServiceCollection();
            serviceCollection
                .AddInMemorySubscriptions()
                .AddGraphQLSchema(
                    SchemaBuilder.New()
                        .AddQueryType(c => c.Name("Query").Field("a").Resolver("b"))
                        .AddMutationType<MyMutation>()
                        .AddSubscriptionType<MySubscription>())
                .AddQueryExecutor();

            IQueryExecutor executor = serviceCollection
                .BuildServiceProvider()
                .GetRequiredService<IQueryExecutor>();

            // act
            var stream = (IResponseStream)await executor.ExecuteAsync(
                "subscription { onFixedMessage }",
                cts.Token);

            // assert
            IExecutionResult mutationResult = await executor.ExecuteAsync(
                "mutation { writeFixedMessage(message: \"def\") }",
                cts.Token);
            Assert.Empty(mutationResult.Errors);

            var results = new StringBuilder();
            await foreach (IReadOnlyQueryResult queryResult in
                stream.WithCancellation(cts.Token))
            {
                results.AppendLine(queryResult.ToJson());
                break;
            }

            results.ToString().MatchSnapshot();
        }

        [Fact]
        public async Task Subscribe_Attribute_With_Static_Topic()
        {
            // arrange
            using var cts = new CancellationTokenSource(30000);

            var serviceCollection = new ServiceCollection();
            serviceCollection
                .AddInMemorySubscriptions()
                .AddGraphQLSchema(
                    SchemaBuilder.New()
                        .AddQueryType(c => c.Name("Query").Field("a").Resolver("b"))
                        .AddMutationType<MyMutation>()
                        .AddSubscriptionType<MySubscription>())
                .AddQueryExecutor();

            IQueryExecutor executor = serviceCollection
                .BuildServiceProvider()
                .GetRequiredService<IQueryExecutor>();

            // act
            var stream = (IResponseStream)await executor.ExecuteAsync(
                "subscription { onSysMessage }",
                cts.Token);

            // assert
            IExecutionResult mutationResult = await executor.ExecuteAsync(
                "mutation { writeSysMessage(message: \"def\") }",
                cts.Token);
            Assert.Empty(mutationResult.Errors);

            var results = new StringBuilder();
            await foreach (IReadOnlyQueryResult queryResult in
                stream.WithCancellation(cts.Token))
            {
                results.AppendLine(queryResult.ToJson());
                break;
            }

            results.ToString().MatchSnapshot();
        }

        [Fact]
        public async Task Subscribe_Attribute_With_Static_Topic_Infer_Topic()
        {
            // arrange
            using var cts = new CancellationTokenSource(30000);

            var serviceCollection = new ServiceCollection();
            serviceCollection
                .AddInMemorySubscriptions()
                .AddGraphQLSchema(
                    SchemaBuilder.New()
                        .AddQueryType(c => c.Name("Query").Field("a").Resolver("b"))
                        .AddMutationType<MyMutation>()
                        .AddSubscriptionType<MySubscription>())
                .AddQueryExecutor();

            IQueryExecutor executor = serviceCollection
                .BuildServiceProvider()
                .GetRequiredService<IQueryExecutor>();

            // act
            var stream = (IResponseStream)await executor.ExecuteAsync(
                "subscription { onInferTopic }",
                cts.Token);

            // assert
            IExecutionResult mutationResult = await executor.ExecuteAsync(
                "mutation { writeOnInferTopic(message: \"def\") }",
                cts.Token);
            Assert.Empty(mutationResult.Errors);

            var results = new StringBuilder();
            await foreach (IReadOnlyQueryResult queryResult in
                stream.WithCancellation(cts.Token))
            {
                results.AppendLine(queryResult.ToJson());
                break;
            }

            results.ToString().MatchSnapshot();
        }

        [Fact]
        public async Task Subscribe_Attribute_With_Explicitly_Defined_Subscribe()
        {
            // arrange
            using var cts = new CancellationTokenSource(30000);

            var serviceCollection = new ServiceCollection();
            serviceCollection
                .AddInMemorySubscriptions()
                .AddGraphQLSchema(
                    SchemaBuilder.New()
                        .AddQueryType(c => c.Name("Query").Field("a").Resolver("b"))
                        .AddMutationType<MyMutation>()
                        .AddSubscriptionType<MySubscription>())
                .AddQueryExecutor();

            IQueryExecutor executor = serviceCollection
                .BuildServiceProvider()
                .GetRequiredService<IQueryExecutor>();

            // act
            var stream = (IResponseStream)await executor.ExecuteAsync(
                "subscription { onExplicit }",
                cts.Token);

            // assert
            IExecutionResult mutationResult = await executor.ExecuteAsync(
                "mutation { writeOnExplicit(message: \"def\") }",
                cts.Token);
            Assert.Empty(mutationResult.Errors);

            var results = new StringBuilder();
            await foreach (IReadOnlyQueryResult queryResult in
                stream.WithCancellation(cts.Token))
            {
                results.AppendLine(queryResult.ToJson());
                break;
            }

            results.ToString().MatchSnapshot();
        }

        [Fact]
        public void Subscribe_Attribute_Schema_Is_Generated_Correctly()
        {
            // arrange
            using var cts = new CancellationTokenSource(30000);

            // act
            var serviceCollection = new ServiceCollection();
            serviceCollection
                .AddInMemorySubscriptions()
                .AddGraphQLSchema(
                    SchemaBuilder.New()
                        .AddQueryType(c => c.Name("Query").Field("a").Resolver("b"))
                        .AddMutationType<MyMutation>()
                        .AddSubscriptionType<MySubscription>())
                .AddQueryExecutor();

            IQueryExecutor executor = serviceCollection
                .BuildServiceProvider()
                .GetRequiredService<IQueryExecutor>();

            // assert
            executor.Schema.ToString().MatchSnapshot();
        }

        [Fact]
        public void Subscribe_Attribute_Schema_Is_Generated_Correctly_2()
        {
            // arrange
            using var cts = new CancellationTokenSource(30000);

            // act
            var serviceCollection = new ServiceCollection();
            serviceCollection
                .AddInMemorySubscriptions()
                .AddGraphQLSchema(
                    SchemaBuilder.New()
                        .AddQueryType(c => c.Name("Query").Field("a").Resolver("b"))
                        .AddSubscriptionType(d => d.Name("Subscription"))
                        .AddType<MySubscriptionExtension>())
                .AddQueryExecutor();

            IQueryExecutor executor = serviceCollection
                .BuildServiceProvider()
                .GetRequiredService<IQueryExecutor>();

            // assert
            executor.Schema.ToString().MatchSnapshot();
        }

        [Fact]
        public void Subscribe_Attribute_With_Two_Topic_Attributes_Error()
        {
            // arrange
            using var cts = new CancellationTokenSource(30000);

            var serviceCollection = new ServiceCollection();
            serviceCollection
                .AddInMemorySubscriptions()
                .AddGraphQLSchema(
                    SchemaBuilder.New()
                        .AddQueryType(c => c.Name("Query").Field("a").Resolver("b"))
                        .AddSubscriptionType<InvalidSubscription_TwoTopicAttributes>())
                .AddQueryExecutor();

            // act
            Action error = () => serviceCollection
                .BuildServiceProvider()
                .GetRequiredService<IQueryExecutor>();

            // assert
            Assert.Throws<SchemaException>(error).Message.MatchSnapshot();
        }

        public class WithSubscribeAndResolve
        {
            [SubscribeAndResolve]
            public IEnumerable<string> OnFoo()
            {
                yield return "a";
                yield return "b";
                yield return "c";
            }
        }

        public class TestObservable
            : IObservable<string>
            , IDisposable

        {
            public bool DisposeRaised { get; private set; }

            public IDisposable Subscribe(IObserver<string> observer)
            {
                Task.Run(async () =>
                {
                    await Task.Delay(250);

                    foreach (var s in new[] { "a", "b", "c" })
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
                return Task.FromResult<IAsyncEnumerable<string?>>(OnSomething());
            }

            [SubscribeAndResolve]
            public ValueTask<IAsyncEnumerable<string?>> OnSomethingValueTask()
            {
                return new ValueTask<IAsyncEnumerable<string?>>(OnSomething());
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
                return Task.FromResult<IAsyncEnumerable<object?>>(OnSomethingObj());
            }

            [GraphQLType(typeof(StringType))]
            [SubscribeAndResolve]
            public ValueTask<IAsyncEnumerable<object?>> OnSomethingObjValueTask()
            {
                return new ValueTask<IAsyncEnumerable<object?>>(OnSomethingObj());
            }
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
                return Task.FromResult<IEnumerable<string?>>(OnSomething());
            }

            [SubscribeAndResolve]
            public ValueTask<IEnumerable<string?>> OnSomethingValueTask()
            {
                return new ValueTask<IEnumerable<string?>>(OnSomething());
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
                return Task.FromResult<IEnumerable<object?>>(OnSomethingObj());
            }

            [GraphQLType(typeof(StringType))]
            [SubscribeAndResolve]
            public ValueTask<IEnumerable<object?>> OnSomethingObjValueTask()
            {
                return new ValueTask<IEnumerable<object?>>(OnSomethingObj());
            }
        }

        public class PureCodeFirstQueryable
        {
            private readonly List<string> _strings = new List<string>
            {
                "a",
                "b",
                "c"
            };

            [SubscribeAndResolve]
            public IQueryable<string?> OnSomething() => _strings.AsQueryable();

            [SubscribeAndResolve]
            public Task<IQueryable<string?>> OnSomethingTask()
            {
                return Task.FromResult<IQueryable<string?>>(OnSomething());
            }

            [SubscribeAndResolve]
            public ValueTask<IQueryable<string?>> OnSomethingValueTask()
            {
                return new ValueTask<IQueryable<string?>>(OnSomething());
            }

            [GraphQLType(typeof(StringType))]
            [SubscribeAndResolve]
            public IQueryable<object?> OnSomethingObj() => _strings.Cast<object>().AsQueryable();

            [GraphQLType(typeof(StringType))]
            [SubscribeAndResolve]
            public Task<IQueryable<object?>> OnSomethingObjTask()
            {
                return Task.FromResult<IQueryable<object?>>(OnSomethingObj());
            }

            [GraphQLType(typeof(StringType))]
            [SubscribeAndResolve]
            public ValueTask<IQueryable<object?>> OnSomethingObjValueTask()
            {
                return new ValueTask<IQueryable<object?>>(OnSomethingObj());
            }
        }

        public class PureCodeFirstObservable
        {
            [SubscribeAndResolve]
            public IObservable<string?> OnSomething() => new StringObservable();

            [SubscribeAndResolve]
            public Task<IObservable<string?>> OnSomethingTask()
            {
                return Task.FromResult<IObservable<string?>>(OnSomething());
            }

            [SubscribeAndResolve]
            public ValueTask<IObservable<string?>> OnSomethingValueTask()
            {
                return new ValueTask<IObservable<string?>>(OnSomething());
            }

            [GraphQLType(typeof(StringType))]
            [SubscribeAndResolve]
            public IObservable<object?> OnSomethingObj() => new StringObservable();

            [GraphQLType(typeof(StringType))]
            [SubscribeAndResolve]
            public Task<IObservable<object?>> OnSomethingObjTask()
            {
                return Task.FromResult<IObservable<object?>>(OnSomethingObj());
            }

            [GraphQLType(typeof(StringType))]
            [SubscribeAndResolve]
            public ValueTask<IObservable<object?>> OnSomethingObjValueTask()
            {
                return new ValueTask<IObservable<object?>>(OnSomethingObj());
            }

            private class StringObservable
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

                private class Subscription : IDisposable
                {
                    public Subscription(IObserver<string> observer)
                    {
                        new Thread(() =>
                        {
                            observer.OnNext("a");
                            observer.OnNext("b");
                            observer.OnNext("c");
                            observer.OnCompleted();
                        }).Start();
                    }

                    public Subscription(IObserver<object> observer)
                    {
                        new Thread(() =>
                        {
                            observer.OnNext("a");
                            observer.OnNext("b");
                            observer.OnNext("c");
                            observer.OnCompleted();
                        }).Start();
                    }

                    public void Dispose()
                    {

                    }
                }
            }
        }

        public class MyMutation
        {
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
            public string OnMessage(
                [Topic] string userId,
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

            public async ValueTask<IAsyncEnumerable<string>> SubscribeToOnExplicit(
                [Service] ITopicEventReceiver eventReceiver) =>
                await eventReceiver.SubscribeAsync<string, string>("explicit");

            [Subscribe(With = nameof(SubscribeToOnExplicit))]
            public string OnExplicit(
                [EventMessage] string message) =>
                message;

            public IAsyncEnumerable<string> SubscribeToOnExplicitSync(
                [Service] ITopicEventReceiver eventReceiver) =>
                default!;

            [Subscribe(With = nameof(SubscribeToOnExplicitSync))]
            public string OnExplicitSync(
                [EventMessage] string message) =>
                message;
        }

        [ExtendObjectType(Name = "Subscription")]
        public class MySubscriptionExtension
        {
            public async ValueTask<IAsyncEnumerable<string>> SubscribeToOnExplicit(
                [Service] ITopicEventReceiver eventReceiver) =>
                await eventReceiver.SubscribeAsync<string, string>("explicit");

            [Subscribe(With = nameof(SubscribeToOnExplicit))]
            public string OnExplicit(
                [EventMessage] string message) =>
                message;
        }

        public class InvalidSubscription_TwoTopicAttributes
        {
            [Subscribe]
            [Topic]
            public string OnMessage(
                [Topic] string userId,
                [EventMessage] string message) =>
                message;
        }
    }
}

