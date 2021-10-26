using System.Threading.Tasks;
using HotChocolate;
using HotChocolate.Execution;
using HotChocolate.Types;
using Microsoft.Extensions.DependencyInjection;
using Snapshooter;
using Snapshooter.Xunit;
using Xunit;

namespace HotChocolate.Types.Payload.Tests
{
    public class PayloadTests
    {
        [Fact]
        public async Task PayloadMiddleware_Should_TransformPayload()
        {
            // Arrange
            IRequestExecutor executor =
                await new ServiceCollection()
                    .AddGraphQL()
                    .AddQueryType<Query>()
                    .BuildRequestExecutorAsync();

            // Act
            IExecutionResult res = await executor
                .ExecuteAsync(@"
                    {
                        createFoo {
                            foo {
                                bar
                            }
                        }
                    }
                ");

            // Assert
            res.ToJson().MatchSnapshot();
            SnapshotFullName fullName = Snapshot.FullName();
            SnapshotFullName snapshotName =
                new SnapshotFullName(fullName.Filename + "_schema", fullName.FolderPath);
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
            [Payload("foo")]
            public Foo CreateFoo() => new Foo("Bar");
        }
    }
}
