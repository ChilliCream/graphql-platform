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
                .EnableMutationConvention()
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
                .EnableMutationConvention()
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
                .EnableMutationConvention()
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
                .EnableMutationConvention()
                .BuildRequestExecutorAsync();

        // Act

        // Assert
        executor.Schema.Print().MatchSnapshot();
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
}
