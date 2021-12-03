using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate.Execution;
using HotChocolate.Tests;
using HotChocolate.Types.Relay;
using Microsoft.Extensions.DependencyInjection;
using Snapshooter;
using Snapshooter.Xunit;
using Xunit;

namespace HotChocolate.Types;

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
                .EnableMutationConventions()
                .BuildRequestExecutorAsync();

        // Act
        IExecutionResult res = await executor
            .ExecuteAsync(@"
                {
                    createFoo(input: {bar: ""A""}) {
                        bar
                    }
                }");

        // Assert
        res.ToJson().MatchSnapshot();
        SnapshotFullName fullName = Snapshot.FullName();
        SnapshotFullName snapshotName = new(fullName.Filename + "_schema", fullName.FolderPath);
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
                .EnableMutationConventions()
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
        SnapshotFullName snapshotName = new(fullName.Filename + "_schema", fullName.FolderPath);
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
                .EnableMutationConventions()
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
        SnapshotFullName snapshotName = new(fullName.Filename + "_schema", fullName.FolderPath);
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
                .EnableMutationConventions()
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
        SnapshotFullName snapshotName = new(fullName.Filename + "_schema", fullName.FolderPath);
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
                .EnableMutationConventions()
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
        SnapshotFullName snapshotName = new(fullName.Filename + "_schema", fullName.FolderPath);
        executor.Schema.Print().MatchSnapshot(snapshotName);
    }

    [Fact]
    public async Task InputArgumentMiddleware_Should_DefineTypeNames_When_OnlyOnMethod()
    {
        // Arrange
        IRequestExecutor executor =
            await new ServiceCollection()
                .AddGraphQL()
                .AddQueryType<QueryTypeNameOnRoot>()
                .EnableMutationConventions()
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
        SnapshotFullName snapshotName = new(fullName.Filename + "_schema", fullName.FolderPath);
        executor.Schema.Print().MatchSnapshot(snapshotName);
    }

    [Fact]
    public async Task InputArgumentMiddleware_Should_DefineTypeNames_When_OnParameter()
    {
        // Arrange
        IRequestExecutor executor =
            await new ServiceCollection()
                .AddGraphQL()
                .AddQueryType<QueryTypeNameOnRootAndOnParameters>()
                .EnableMutationConventions()
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
        SnapshotFullName snapshotName = new(fullName.Filename + "_schema", fullName.FolderPath);
        executor.Schema.Print().MatchSnapshot(snapshotName);
    }

    [Fact]
    public async Task InputArgumentMiddleware_Should_DefineTypeNames_When_JustOneTypeNameDefined()
    {
        // Arrange
        IRequestExecutor executor =
            await new ServiceCollection()
                .AddGraphQL()
                .AddQueryType<QueryTypeJustOne>()
                .EnableMutationConventions()
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
        SnapshotFullName snapshotName = new(fullName.Filename + "_schema", fullName.FolderPath);
        executor.Schema.Print().MatchSnapshot(snapshotName);
    }

    [Fact]
    public async Task InputArgumentMiddleware_Should_DefineTypeNames_When_MultipleInputs()
    {
        // Arrange
        IRequestExecutor executor =
            await new ServiceCollection()
                .AddGraphQL()
                .AddQueryType<QueryTypeMultiple>()
                .EnableMutationConventions()
                .BuildRequestExecutorAsync();

        // Act
        IExecutionResult res = await executor
            .ExecuteAsync(@"
                {
                    createFoo(a: {bar: ""A""},b: { baz: ""C""}) {
                        bar
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
    public async Task InputArgumentMiddleware_Should_MergeFormatter_When_UsedWithId()
    {
        // Arrange
        IRequestExecutor executor =
            await new ServiceCollection()
                .AddGraphQL()
                .AddQueryType<QueryId>()
                .EnableMutationConventions()
                .BuildRequestExecutorAsync();

        // Act
        string id = new IdSerializer()
            .Serialize("Item", Guid.Parse("5ea909ec-ae00-4d00-8f96-afe0e8bdcad2"));

        IExecutionResult res = await executor
            .ExecuteAsync(@"
                query Foo($id: ID!){
                    createFoo(input: {
                        bar: $id
                    })
                }
            ",
                new Dictionary<string, object?>() { ["id"] = id });

        // Assert
        res.ToJson().MatchSnapshot();
        SnapshotFullName fullName = Snapshot.FullName();
        SnapshotFullName snapshotName = new(fullName.Filename + "_schema", fullName.FolderPath);
        executor.Schema.Print().MatchSnapshot(snapshotName);
    }

    [Fact]
    public async Task InputArgumentMiddleware_Should_Throw_WhenTypeNamesCollide()
    {
        // Arrange

        // Act
        SchemaException exception = await Assert.ThrowsAsync<SchemaException>(async () =>
        {
            await new ServiceCollection()
                .AddGraphQL()
                .AddQueryType<QueryCollidingNames>()
                .EnableMutationConventions()
                .BuildRequestExecutorAsync();
        });

        // Assert
        exception.Message.MatchSnapshot();
    }

    [Fact]
    public async Task Ensure_That_CancellationToken_Are_Not_Inferred_As_Input()
    {
        Snapshot.FullName();

        await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType<QueryTypeWithCancellationToken>()
            .EnableMutationConventions()
            .BuildSchemaAsync()
            .MatchSnapshotAsync();
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

    public class QueryTypeNameOnRoot
    {
        [Input(TypeName = "ThisIsTheInputType")]
        public Foo CreateFoo(
            string bar,
            string baz) =>
            new(bar + baz);
    }

    public class QueryTypeNameOnRootAndOnParameters
    {
        [Input(TypeName = "ShouldNotBeInSnapshot")]
        public Foo CreateFoo(
            [Input(TypeName = "ThisIsTheInputType")] string bar,
            [Input(TypeName = "ThisIsTheInputType")]
            string baz) =>
            new(bar + baz);
    }

    public class QueryTypeWithCancellationToken
    {
        [Input]
        public Foo CreateFoo(
            string bar,
            string baz,
            CancellationToken cancellationToken = default) =>
            new(bar + baz);
    }

    public class QueryTypeJustOne
    {
        public Foo CreateFoo(
            [Input] string bar,
            [Input(TypeName = "ThisIsTheInputType")]
            string baz) =>
            new(bar + baz);
    }

    public class QueryTypeMultiple
    {
        public Foo CreateFoo(
            [Input("a", TypeName = "BInput")] string bar,
            [Input("b", TypeName = "AInput")] string baz) =>
            new(bar + baz);
    }

    public class QueryCollidingNames
    {
        public Foo CreateFoo(
            [Input(TypeName = "FirstOne")] string bar,
            [Input(TypeName = "SecondOne")] string baz) =>
            new(bar + baz);
    }

    public class QueryId
    {
        [ID("Item")]
        [Input]
        public Guid CreateFoo([ID("Item")] Guid bar) => bar;
    }
}
