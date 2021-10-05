using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using GreenDonut;
using HotChocolate.StarWars;
using HotChocolate.Tests;
using Microsoft.Extensions.DependencyInjection;
using Snapshooter.Xunit;
using Xunit;

namespace HotChocolate.Execution
{
    public class StreamTests
    {
        [Fact]
        public async Task Stream_Nodes()
        {
            IExecutionResult result =
                await new ServiceCollection()
                    .AddStarWarsRepositories()
                    .AddGraphQL()
                    .AddStarWarsTypes()
                    .ExecuteRequestAsync(
                        @"{
                            hero(episode: NEW_HOPE) {
                                id
                                ... @defer(label: ""friends"") {
                                    friends {
                                        nodes @stream(initialCount: 1) {
                                            id
                                            name
                                        }
                                    }
                                }
                            }
                        }");

            IResponseStream stream = Assert.IsType<DeferredQueryResult>(result);

            var results = new StringBuilder();

            await foreach (IQueryResult payload in stream.ReadResultsAsync())
            {
                results.AppendLine(payload.ToJson());
                results.AppendLine();
            }

            results.ToString().MatchSnapshot();
        }

        [Fact]
        public async Task Do_Not_Stream_Nodes()
        {
            IExecutionResult result =
                await new ServiceCollection()
                    .AddStarWarsRepositories()
                    .AddGraphQL()
                    .AddStarWarsTypes()
                    .ExecuteRequestAsync(
                        QueryRequestBuilder.New()
                            .SetQuery(
                                @"query($stream: Boolean) {
                                    hero(episode: NEW_HOPE) {
                                        id
                                        ... @defer(label: ""friends"") {
                                            friends {
                                                nodes @stream(initialCount: 1, if: $stream) {
                                                    id
                                                    name
                                                }
                                            }
                                        }
                                    }
                                }")
                            .SetVariableValue("stream", false)
                            .Create());

            IResponseStream stream = Assert.IsType<DeferredQueryResult>(result);

            var results = new StringBuilder();

            await foreach (IQueryResult payload in stream.ReadResultsAsync())
            {
                results.AppendLine(payload.ToJson());
                results.AppendLine();
            }

            results.ToString().MatchSnapshot();
        }

        [Fact]
        public async Task Stream_Nested_Nodes()
        {
            IExecutionResult result =
                await new ServiceCollection()
                    .AddStarWarsRepositories()
                    .AddGraphQL()
                    .AddStarWarsTypes()
                    .ExecuteRequestAsync(
                        @"{
                            hero(episode: NEW_HOPE) {
                                id
                                ... @defer(label: ""friends"") {
                                    friends {
                                        nodes @stream(initialCount: 1) {
                                            id
                                            name
                                            friends {
                                                nodes @stream(initialCount: 1) {
                                                    id
                                                    name
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }");

            IResponseStream stream = Assert.IsType<DeferredQueryResult>(result);

            var results = new StringBuilder();

            await foreach (IQueryResult payload in stream.ReadResultsAsync())
            {
                results.AppendLine(payload.ToJson());
                results.AppendLine();
            }

            results.ToString().MatchSnapshot();
        }

        [Fact]
        public async Task Stream_With_AsyncEnumerable_Schema()
        {
            await new ServiceCollection()
                .AddGraphQL()
                .AddQueryType<Query>()
                .BuildSchemaAsync()
                .MatchSnapshotAsync();
        }

        [Fact]
        public async Task List_With_AsyncEnumerable()
        {
            IExecutionResult result =
                await new ServiceCollection()
                    .AddGraphQL()
                    .AddQueryType<Query>()
                    .ExecuteRequestAsync(
                        @"{
                            persons {
                                name
                            }
                        }");

            Assert.IsType<QueryResult>(result).MatchSnapshot();
        }

        [Fact]
        public async Task Stream_With_AsyncEnumerable()
        {
            IExecutionResult result =
                await new ServiceCollection()
                    .AddGraphQL()
                    .AddQueryType<Query>()
                    .ExecuteRequestAsync(
                        @"{
                            persons @stream(initialCount: 0) {
                                name
                            }
                        }");

            IResponseStream stream = Assert.IsType<DeferredQueryResult>(result);

            var results = new StringBuilder();

            await foreach (IQueryResult payload in stream.ReadResultsAsync())
            {
                results.AppendLine(payload.ToJson());
                results.AppendLine();
            }

            results.ToString().MatchSnapshot();
        }

        [Fact]
        public async Task Stream_With_AsyncEnumerable_InitialCount_1()
        {
            IExecutionResult result =
                await new ServiceCollection()
                    .AddGraphQL()
                    .AddQueryType<Query>()
                    .ExecuteRequestAsync(
                        @"{
                            persons @stream(initialCount: 1) {
                                name
                            }
                        }");

            IResponseStream stream = Assert.IsType<DeferredQueryResult>(result);

            var results = new StringBuilder();

            await foreach (IQueryResult payload in stream.ReadResultsAsync())
            {
                results.AppendLine(payload.ToJson());
                results.AppendLine();
            }

            results.ToString().MatchSnapshot();
        }

        [Fact]
        public async Task Stream_With_DataLoader()
        {
            IExecutionResult result =
                await new ServiceCollection()
                    .AddGraphQL()
                    .AddQueryType<QueryWithDataLoader>()
                    .AddDataLoader<PersonsGroupDataLoader>()
                    .ExecuteRequestAsync(
                        @"{
                            persons @stream(initialCount: 0) {
                                name
                            }
                        }");

            IResponseStream stream = Assert.IsType<DeferredQueryResult>(result);

            var results = new StringBuilder();

            await foreach (IQueryResult payload in stream.ReadResultsAsync())
            {
                results.AppendLine(payload.ToJson());
                results.AppendLine();
            }

            results.ToString().MatchSnapshot();
        }

        [Fact]
        public async Task Stream_With_DataLoader_InitialCount_1()
        {
            IExecutionResult result =
                await new ServiceCollection()
                    .AddGraphQL()
                    .AddQueryType<QueryWithDataLoader>()
                    .AddDataLoader<PersonsGroupDataLoader>()
                    .ExecuteRequestAsync(
                        @"{
                            persons @stream(initialCount: 1) {
                                name
                            }
                        }");

            IResponseStream stream = Assert.IsType<DeferredQueryResult>(result);

            var results = new StringBuilder();

            await foreach (IQueryResult payload in stream.ReadResultsAsync())
            {
                results.AppendLine(payload.ToJson());
                results.AppendLine();
            }

            results.ToString().MatchSnapshot();
        }

        public class Query
        {
            public async IAsyncEnumerable<Person> GetPersonsAsync()
            {
                await Task.Delay(1);
                yield return new Person { Name = "Foo" };
                await Task.Delay(1);
                yield return new Person { Name = "Bar" };
            }
        }

        public class QueryWithDataLoader
        {
            public async IAsyncEnumerable<Person> GetPersonsAsync(PersonsGroupDataLoader dl)
            {
                Person[] persons = await dl.LoadAsync("abc");

                foreach (Person person in persons)
                {
                    yield return person;
                }
            }
        }

        public class PersonsGroupDataLoader : GroupedDataLoader<string, Person>
        {
            public PersonsGroupDataLoader(
                IBatchScheduler batchScheduler,
                DataLoaderOptions options = null)
                : base(batchScheduler, options)
            {
            }

            protected override Task<ILookup<string, Person>> LoadGroupedBatchAsync(
                IReadOnlyList<string> keys,
                CancellationToken cancellationToken)
            {
                return Task.FromResult(new List<Person>
                {
                    new() { GroupId = keys[0], Name = "Foo" },
                    new() { GroupId = keys[0], Name = "Bar" }
                }.ToLookup(t => t.GroupId));
            }
        }

        public class Person
        {
            public string GroupId { get; set; }

            public string Name { get; set; }
        }
    }
}
