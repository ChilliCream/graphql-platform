using System;
using System.Threading.Tasks;
using HotChocolate.Execution;
using Microsoft.Extensions.DependencyInjection;
using Snapshooter;
using Snapshooter.Xunit;
using Xunit;

namespace HotChocolate.Types
{
    public class CombineInputErrorPayloadTests
    {
        [Fact]
        public async Task InputArgumentMiddleware_Should_WorkInCombination()
        {
            // Arrange
            IRequestExecutor executor =
                await new ServiceCollection()
                    .AddGraphQL()
                    .AddQueryType<Query>()
                    .EnableMutationConventions()
                    .BuildRequestExecutorAsync();

            // Act
            IExecutionResult res = await executor
                .ExecuteAsync(@"
                    {
                        createFoo(input: {bar: ""A""}) {
                            foo {
                               bar
                            }
                        }
                    }
                ");

            // Assert
            res.ToJson().MatchSnapshot();
            SnapshotFullName fullName = Snapshot.FullName();
            SnapshotFullName snapshotName = new(fullName.Filename + "_schema", fullName.FolderPath);
            executor.Schema.Print().MatchSnapshot(snapshotName);
        }

        [Fact]
        public async Task InputArgumentMiddleware_Should_WorkWithException()
        {
            // Arrange
            IRequestExecutor executor =
                await new ServiceCollection()
                    .AddGraphQL()
                    .AddQueryType<QueryWithException>()
                    .EnableMutationConventions()
                    .BuildRequestExecutorAsync();

            // Act
            IExecutionResult res = await executor
                .ExecuteAsync(@"
                    {
                        createFoo(input: {bar: ""A""}) {
                            foo {
                               bar
                            }
                            errors { ... on Error { message __typename} }
                        }
                    }
                ");

            // Assert
            res.ToJson().MatchSnapshot();
            SnapshotFullName fullName = Snapshot.FullName();
            SnapshotFullName snapshotName = new(fullName.Filename + "_schema", fullName.FolderPath);
            executor.Schema.Print().MatchSnapshot(snapshotName);
        }

        public class Foo
        {
            public Foo(string bar)
            {
                Bar = bar;
            }

            public string Bar { get; set; }
        }

        public class Query
        {
            [Error(typeof(InvalidOperationException))]
            [Payload("foo")]
            public Foo CreateFoo([Input] string bar)
                => new Foo(bar);
        }

        public class QueryWithException
        {
            [Error(typeof(InvalidOperationException))]
            [Payload("foo")]
            public Foo CreateFoo([Input] string bar)
                => throw new InvalidOperationException();
        }
    }
}
