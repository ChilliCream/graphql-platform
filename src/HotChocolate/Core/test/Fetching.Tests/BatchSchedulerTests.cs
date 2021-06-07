using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate.Fetching;
using Snapshooter.Xunit;
using Xunit;
using Xunit.Sdk;

namespace HotChocolate
{
    public class BatchSchedulerTests
    {
        [Fact]
        public void Dispatch_OneAction_ShouldDispatchOneAction()
        {
            // arrange
            var scheduler = new BatchScheduler();
            ValueTask Dispatch() => default;

            scheduler.Schedule(Dispatch);
            Assert.True(scheduler.HasTasks);

            // act
            scheduler.Dispatch(_ => { });

            // assert
            Assert.True(scheduler.HasTasks);
        }

        [Fact]
        public void Initialize_Nothing_ShouldMatchSnapshot()
        {
            // act
            var scheduler = new BatchScheduler();

            // assert
            scheduler.MatchSnapshot();
        }

        [Fact]
        public void Schedule_OneAction_HasTasksShouldReturnTrue()
        {
            // arrange
            var scheduler = new BatchScheduler();
            ValueTask Dispatch() => default;

            // act
            scheduler.Schedule(Dispatch);

            // assert
            Assert.True(scheduler.HasTasks);
        }

        [Fact]
        public void Schedule_OneAction_ShouldRaiseTaskEnqueued()
        {
            // arrange
            var hasBeenRaised = false;
            var scheduler = new BatchScheduler();
            ValueTask Dispatch() => default;

            scheduler.TaskEnqueued += (_, _) => hasBeenRaised = true;

            // act
            scheduler.Schedule(Dispatch);

            // assert
            Assert.True(hasBeenRaised);
        }
    }

    public partial class MyFooByIdDataLoader
    {
        private async Task<Dictionary<string, Foo>> GetFoosAsync(
            [Service] FooService fooService,
            IReadOnlyList<string> keys,
            CancellationToken cancellationToken)
        {
            var foos = await fooService.GetFoosByIds(keys, cancellationToken);
            return foos.ToDictionary(t => t.Id);
        }
    }

    // multiple dataloader in one static class
    public static class DataLoaders
    {
        // becomes FoosByIdDataLoader
        // return type of a batch dataloader must be something that implement IReadOnlyDictionary<TKey, TValue>
        public static async Task<Dictionary<string, Foo>> GetFoosByIdAsync(
            // services can be injected like with resolvers
            [Service] FooService fooService,
            // must be named keys and implement IEnumerable<TKey>
            IReadOnlyList<string> keys,
            // is optional
            CancellationToken cancellationToken)
        {
            var foos = await fooService.GetFoosByIds(keys, cancellationToken);
            return foos.ToDictionary(t => t.Id);
        }
    }

    public class Foo
    {
        public string Id { get; }
    }

    public class FooService
    {
        public Task< IReadOnlyList<Foo>> GetFoosByIds(
            IReadOnlyList<string> keys,
            CancellationToken cancellationToken) =>
            throw new NotImplementedException();
    }



    public class FooResolvers
    {
        // resolver
        public Task<Foo> GetFooAsync(
            string id,
            LoadOne<string, Foo> fooById) =>
            fooById(id);

        // dataloader
        [DataLoaderFetch]
        public async Task<Dictionary<string, Foo>> GetFoosAsync(
            [Service] FooService fooService,
            IReadOnlyList<string> keys,
            CancellationToken cancellationToken)
        {
            var foos = await fooService.GetFoosByIds(keys, cancellationToken);
            return foos.ToDictionary(t => t.Id);
        }
    }

    public class DataLoaderFetchAttribute : Attribute
    {
    }

    public delegate Task<TValue> LoadOne<TKey, TValue>(TKey key);
}
