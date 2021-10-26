using System.Threading.Tasks;
using HotChocolate.Execution;
using Microsoft.Extensions.DependencyInjection;
using Snapshooter;
using Snapshooter.Xunit;
using Xunit;

namespace HotChocolate.Types.Errors.Tests
{
    public class InputArgumentTests
    {
        [Fact]
        public async Task InputArgumentMiddleware_Should_HoistInput_Single()
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
                        createFoo(input: {bar: ""A""}) {
                            bar
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

        [Fact]
        public async Task InputArgumentMiddleware_Should_HoistInput_Multiple()
        {
            // Arrange
            IRequestExecutor executor =
                await new ServiceCollection()
                    .AddGraphQL()
                    .AddQueryType<QueryMultiple>()
                    .BuildRequestExecutorAsync();

            // Act
            IExecutionResult res = await executor
                .ExecuteAsync(@"
                    {
                        createFoo(input: {bar: ""A"", baz: ""C""}) {
                            bar
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

        [Fact]
        public async Task InputArgumentMiddleware_Should_HoistInputs_Different()
        {
            // Arrange
            IRequestExecutor executor =
                await new ServiceCollection()
                    .AddGraphQL()
                    .AddQueryType<QueryDifferentArgs>()
                    .BuildRequestExecutorAsync();

            // Act
            IExecutionResult res = await executor
                .ExecuteAsync(@"
                    {
                        createFoo(bar: {bar: ""A""} baz:{ baz: ""C""}) {
                            bar
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

        [Fact]
        public async Task InputArgumentMiddleware_Should_HoistInput_Member()
        {
            // Arrange
            IRequestExecutor executor =
                await new ServiceCollection()
                    .AddGraphQL()
                    .AddQueryType<QueryMember>()
                    .BuildRequestExecutorAsync();

            // Act
            IExecutionResult res = await executor
                .ExecuteAsync(@"
                    {
                        createFoo(input: {bar: ""A"", baz: ""C""}) {
                            bar
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

        [Fact]
        public async Task InputArgumentMiddleware_Should_HoistInputs_Mixed()
        {
            // Arrange
            IRequestExecutor executor =
                await new ServiceCollection()
                    .AddGraphQL()
                    .AddQueryType<QueryMixed>()
                    .BuildRequestExecutorAsync();

            // Act
            IExecutionResult res = await executor
                .ExecuteAsync(@"
                    {
                        createFoo(bar: {bar: ""A""} baz:{ baz: ""C""}) {
                            bar
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
            public Foo CreateFoo([Input("input")] string bar) => new(bar);
        }

        public class QueryMember
        {
            [Input]
            public Foo CreateFoo(string bar, string baz) => new(bar + baz);
        }

        public class QueryMultiple
        {
            public Foo CreateFoo(
                [Input] string bar,
                [Input] string baz) =>
                new(bar + baz);
        }

        public class QueryDifferentArgs
        {
            public Foo CreateFoo(
                [Input("bar")] string bar,
                [Input("baz")] string baz) =>
                new(bar + baz);
        }

        public class QueryMixed
        {
            [Input("bar")]
            public Foo CreateFoo(
                string bar,
                [Input("baz")] string baz) =>
                new(bar + baz);
        }
    }
}
