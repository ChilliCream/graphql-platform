using System.Threading.Tasks;
using HotChocolate.Execution;
using Microsoft.Extensions.DependencyInjection;
using Snapshooter;
using Snapshooter.Xunit;
using Xunit;

namespace HotChocolate.Types;

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
                .AddMutationType<Mutation>()
                .EnableMutationConventions()
                .BuildRequestExecutorAsync();

        // Act
        IExecutionResult res = await executor
            .ExecuteAsync(@"
                    mutation {
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
        SnapshotFullName snapshotName = new(fullName.Filename + "_schema", fullName.FolderPath);
        executor.Schema.Print().MatchSnapshot(snapshotName);
    }

    [Fact]
    public async Task PayloadMiddleware_Should_TransformPayload_When_QueryIsSpecified()
    {
        // Arrange
        IRequestExecutor executor =
            await new ServiceCollection()
                .AddGraphQL()
                .AddQueryType<Query>()
                .AddMutationType<Mutation>()
                .AddQueryFieldToMutationPayloads()
                .EnableMutationConventions()
                .BuildRequestExecutorAsync();

        // Act
        IExecutionResult res = await executor
            .ExecuteAsync(@"
                    mutation {
                        createFoo {
                            foo {
                                bar
                            }
                            query {
                                foo {
                                    bar
                                }
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
    public async Task PayloadAttribute_Should_AddTypeName_When_CustomTypeNameSpecified()
    {
        // Arrange
        IRequestExecutor executor =
            await new ServiceCollection()
                .AddGraphQL()
                .AddQueryType<Query>()
                .AddMutationType<CustomTypeName>()
                .EnableMutationConventions()
                .BuildRequestExecutorAsync();

        // Act

        // Assert
        executor.Schema.Print().MatchSnapshot();
    }

    [Fact]
    public async Task PayloadAttribute_Should_UserResultTypeForField_When_NoFieldNameSpecified()
    {
        // Arrange
        IRequestExecutor executor =
            await new ServiceCollection()
                .AddGraphQL()
                .AddQueryType<Query>()
                .AddMutationType<DefaultMutation>()
                .EnableMutationConventions()
                .BuildRequestExecutorAsync();

        // Act

        // Assert
        executor.Schema.Print().MatchSnapshot();
    }

    [Fact]
    public async Task PayloadMiddleware_Should_TransformPayload_When_ItIsATask()
    {
        // Arrange
        IRequestExecutor executor =
            await new ServiceCollection()
                .AddGraphQL()
                .AddQueryType<Query>()
                .AddMutationType<MutationTask>()
                .EnableMutationConventions()
                .BuildRequestExecutorAsync();

        // Act
        IExecutionResult res = await executor
            .ExecuteAsync(@"
                    mutation {
                        createTask {
                            foo {
                                bar
                            }
                        }
                        createValueTask {
                            foo {
                                bar
                            }
                        }
                        createTaskNoName {
                            foo {
                                bar
                            }
                        }
                        createValueTaskNoName {
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
    public async Task PayloadMiddleware_Should_TransformPayload_When_ItIsNullable()
    {
        // Arrange
        IRequestExecutor executor =
            await new ServiceCollection()
                .AddGraphQL()
                .AddQueryType<Query>()
                .AddMutationType<MutationNullable>()
                .EnableMutationConventions()
                .BuildRequestExecutorAsync();

        // Act
        IExecutionResult res = await executor
            .ExecuteAsync(@"
                    mutation {
                        nullableNumber {
                            foo
                        }
                        nullableNumberNoName {
                            int
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
    public async Task PayloadMiddleware_Should_TransformPayload_When_ObjectIsNamed()
    {
        // Arrange
        IRequestExecutor executor =
            await new ServiceCollection()
                .AddGraphQL()
                .AddQueryType<Query>()
                .AddMutationType<MutationRenamed>()
                .EnableMutationConventions()
                .BuildRequestExecutorAsync();

        // Act
        IExecutionResult res = await executor
            .ExecuteAsync(@"
                    mutation {
                        createBaz {
                            baz {
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

    public class Foo
    {
        public Foo(string bar)
        {
            Bar = bar;
        }

        public string Bar { get; set; }
    }

    public class MutationRenamed
    {
        [Payload]
        public BazDto CreateBaz() => new BazDto("Bar");
    }

    [GraphQLName("Baz")]
    public class BazDto
    {
        public BazDto(string bar)
        {
            Bar = bar;
        }

        public string Bar { get; set; }
    }

    public class Query
    {
        public Foo GetFoo() => new Foo("Bar");
    }

    public class DefaultMutation
    {
        [Payload]
        public Foo GetFoo() => new Foo("Bar");
    }

    public class CustomTypeName
    {
        [Payload(TypeName = "FooBarBazPayload")]
        public Foo GetFoo() => new Foo("Bar");
    }

    public class Mutation
    {
        [Payload("foo")]
        public Foo CreateFoo() => new Foo("Bar");
    }

    public class MutationTask
    {
        [Payload("foo")]
        public Task<Foo> CreateTask() => Task.FromResult(new Foo("Bar"));

        [Payload("foo")]
        public ValueTask<Foo> CreateValueTask() => new(new Foo("Bar"));

        [Payload]
        public Task<Foo> CreateTaskNoName() => Task.FromResult(new Foo("Bar"));

        [Payload]
        public ValueTask<Foo> CreateValueTaskNoName() => new(new Foo("Bar"));
    }

    public class MutationNullable
    {
        [Payload("foo")]
        public int? NullableNumber() => null;

        [Payload]
        public int? NullableNumberNoName() => null;
    }
}
