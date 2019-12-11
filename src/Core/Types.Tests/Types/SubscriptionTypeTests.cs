using System;
using System.Threading;
using System.Collections.Generic;
using System.Threading.Tasks;
using HotChocolate.Execution;
using Snapshooter.Xunit;
using Xunit;

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
    }
}

