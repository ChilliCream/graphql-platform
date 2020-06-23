using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate.Execution;
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
            IRequestExecutor executor = schema.MakeExecutable();
            var stream = (IResponseStream)await executor.ExecuteAsync(
                "subscription { test }", cts.Token);

            var results = new StringBuilder();
            await foreach (IReadOnlyQueryResult result in
                stream.ReadResultsAsync().WithCancellation(cts.Token))
            {
                results.AppendLine(result.ToJson());
            }

            results.ToString().MatchSnapshot();
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
            IRequestExecutor executor = schema.MakeExecutable();
            var stream = (IResponseStream)await executor.ExecuteAsync(
                "subscription { test }", cts.Token);

            var results = new StringBuilder();
            await foreach (IReadOnlyQueryResult result in
                stream.ReadResultsAsync().WithCancellation(cts.Token))
            {
                results.AppendLine(result.ToJson());
            }

            results.ToString().MatchSnapshot();
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
            IRequestExecutor executor = schema.MakeExecutable();
            var stream = (IResponseStream)await executor.ExecuteAsync(
                "subscription { test }", cts.Token);

            var results = new StringBuilder();
            await foreach (IQueryResult queryResult in
                stream.ReadResultsAsync().WithCancellation(cts.Token))
            {
                var result = (IReadOnlyQueryResult) queryResult;
                results.AppendLine(result.ToJson());
            }

            Assert.True(observable.DisposeRaised);
            results.ToString().MatchSnapshot();
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
            IRequestExecutor executor = schema.MakeExecutable();
            var stream = (IResponseStream)await executor.ExecuteAsync(
                "subscription { test }", cts.Token);

            var results = new StringBuilder();
            await foreach (IReadOnlyQueryResult result in
                stream.ReadResultsAsync().WithCancellation(cts.Token))
            {
                results.AppendLine(result.ToJson());
            }

            Assert.True(observable.DisposeRaised);
            results.ToString().MatchSnapshot();
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
            IRequestExecutor executor = schema.MakeExecutable();
            var stream = (IResponseStream)await executor.ExecuteAsync(
                "subscription { test }", cts.Token);

            var results = new StringBuilder();
            await foreach (IReadOnlyQueryResult result in
                stream.ReadResultsAsync().WithCancellation(cts.Token))
            {
                results.AppendLine(result.ToJson());
            }

            results.ToString().MatchSnapshot();
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
            IRequestExecutor executor = schema.MakeExecutable();
            var stream = (IResponseStream)await executor.ExecuteAsync(
                "subscription { test }", cts.Token);

            var results = new StringBuilder();
            await foreach (IReadOnlyQueryResult result in
                stream.ReadResultsAsync().WithCancellation(cts.Token))
            {
                results.AppendLine(result.ToJson());
            }

            results.ToString().MatchSnapshot();
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
            IRequestExecutor executor = schema.MakeExecutable();
            var stream = (IResponseStream)await executor.ExecuteAsync(
                "subscription { " + field + " }", cts.Token);

            var results = new StringBuilder();
            await foreach (IReadOnlyQueryResult result in
                stream.ReadResultsAsync().WithCancellation(cts.Token))
            {
                results.AppendLine(result.ToJson());
            }

            results.ToString().MatchSnapshot(new SnapshotNameExtension(field));
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
            IRequestExecutor executor = schema.MakeExecutable();
            var stream = (IResponseStream)await executor.ExecuteAsync(
                "subscription { " + field + " }", cts.Token);

            var results = new StringBuilder();
            await foreach (IReadOnlyQueryResult result in
                stream.ReadResultsAsync().WithCancellation(cts.Token))
            {
                results.AppendLine(result.ToJson());
            }

            results.ToString().MatchSnapshot(new SnapshotNameExtension(field));
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
            IRequestExecutor executor = schema.MakeExecutable();
            var stream = (IResponseStream)await executor.ExecuteAsync(
                "subscription { " + field + " }", cts.Token);

            var results = new StringBuilder();
            await foreach (IReadOnlyQueryResult result in
                stream.ReadResultsAsync().WithCancellation(cts.Token))
            {
                results.AppendLine(result.ToJson());
            }

            results.ToString().MatchSnapshot(new SnapshotNameExtension(field));
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
            IRequestExecutor executor = schema.MakeExecutable();
            var stream = (IResponseStream)await executor.ExecuteAsync(
                "subscription { " + field + " }", cts.Token);

            var results = new StringBuilder();
            await foreach (IReadOnlyQueryResult result in
                stream.ReadResultsAsync().WithCancellation(cts.Token))
            {
                results.AppendLine(result.ToJson());
            }

            results.ToString().MatchSnapshot(new SnapshotNameExtension(field));
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
            [Subscribe]
            public async IAsyncEnumerable<string?> OnSomething()
            {
                await Task.Delay(50);
                yield return "a";
                yield return "b";
                yield return "c";
            }

            [Subscribe]
            public Task<IAsyncEnumerable<string?>> OnSomethingTask()
            {
                return Task.FromResult<IAsyncEnumerable<string?>>(OnSomething());
            }

            [Subscribe]
            public ValueTask<IAsyncEnumerable<string?>> OnSomethingValueTask()
            {
                return new ValueTask<IAsyncEnumerable<string?>>(OnSomething());
            }

            [GraphQLType(typeof(StringType))]
            [Subscribe]
            public async IAsyncEnumerable<object?> OnSomethingObj()
            {
                await Task.Delay(50);
                yield return "a";
                yield return "b";
                yield return "c";
            }

            [GraphQLType(typeof(StringType))]
            [Subscribe]
            public Task<IAsyncEnumerable<object?>> OnSomethingObjTask()
            {
                return Task.FromResult<IAsyncEnumerable<object?>>(OnSomethingObj());
            }

            [GraphQLType(typeof(StringType))]
            [Subscribe]
            public ValueTask<IAsyncEnumerable<object?>> OnSomethingObjValueTask()
            {
                return new ValueTask<IAsyncEnumerable<object?>>(OnSomethingObj());
            }
        }

        public class PureCodeFirstEnumerable
        {
            [Subscribe]
            public IEnumerable<string?> OnSomething()
            {
                yield return "a";
                yield return "b";
                yield return "c";
            }

            [Subscribe]
            public Task<IEnumerable<string?>> OnSomethingTask()
            {
                return Task.FromResult<IEnumerable<string?>>(OnSomething());
            }

            [Subscribe]
            public ValueTask<IEnumerable<string?>> OnSomethingValueTask()
            {
                return new ValueTask<IEnumerable<string?>>(OnSomething());
            }

            [GraphQLType(typeof(StringType))]
            [Subscribe]
            public IEnumerable<object?> OnSomethingObj()
            {
                yield return "a";
                yield return "b";
                yield return "c";
            }

            [GraphQLType(typeof(StringType))]
            [Subscribe]
            public Task<IEnumerable<object?>> OnSomethingObjTask()
            {
                return Task.FromResult<IEnumerable<object?>>(OnSomethingObj());
            }

            [GraphQLType(typeof(StringType))]
            [Subscribe]
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

            [Subscribe]
            public IQueryable<string?> OnSomething() => _strings.AsQueryable();

            [Subscribe]
            public Task<IQueryable<string?>> OnSomethingTask()
            {
                return Task.FromResult<IQueryable<string?>>(OnSomething());
            }

            [Subscribe]
            public ValueTask<IQueryable<string?>> OnSomethingValueTask()
            {
                return new ValueTask<IQueryable<string?>>(OnSomething());
            }

            [GraphQLType(typeof(StringType))]
            [Subscribe]
            public IQueryable<object?> OnSomethingObj() => _strings.Cast<object>().AsQueryable();

            [GraphQLType(typeof(StringType))]
            [Subscribe]
            public Task<IQueryable<object?>> OnSomethingObjTask()
            {
                return Task.FromResult<IQueryable<object?>>(OnSomethingObj());
            }

            [GraphQLType(typeof(StringType))]
            [Subscribe]
            public ValueTask<IQueryable<object?>> OnSomethingObjValueTask()
            {
                return new ValueTask<IQueryable<object?>>(OnSomethingObj());
            }
        }

        public class PureCodeFirstObservable
        {
            [Subscribe]
            public IObservable<string?> OnSomething() => new StringObservable();

            [Subscribe]
            public Task<IObservable<string?>> OnSomethingTask()
            {
                return Task.FromResult<IObservable<string?>>(OnSomething());
            }

            [Subscribe]
            public ValueTask<IObservable<string?>> OnSomethingValueTask()
            {
                return new ValueTask<IObservable<string?>>(OnSomething());
            }

            [GraphQLType(typeof(StringType))]
            [Subscribe]
            public IObservable<object?> OnSomethingObj() => new StringObservable();

            [GraphQLType(typeof(StringType))]
            [Subscribe]
            public Task<IObservable<object?>> OnSomethingObjTask()
            {
                return Task.FromResult<IObservable<object?>>(OnSomethingObj());
            }

            [GraphQLType(typeof(StringType))]
            [Subscribe]
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
    }
}

