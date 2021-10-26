using System;
using HotChocolate.Execution;
using Xunit;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Snapshooter;
using Snapshooter.Xunit;

namespace HotChocolate.Types.Errors.Tests
{
    public class ErrorMiddlewareTests
    {
        private const string _query = @"
             query {
                throw {
                    errors {
                        __typename
                        ... on Error {
                            message
                        }
                    }
                }
             }
            ";

        [Fact]
        public async Task ErrorMiddleware_Should_CatchException_WhenRegistered()
        {
            // Arrange
            IRequestExecutor executor =
                await BuildSchemaAsync(
                    () => throw new InvalidOperationException(),
                    field => field.Error<InvalidOperationException>());

            // Act
            IExecutionResult res = await executor.ExecuteAsync(_query);

            // Assert
            res.ToJson().MatchSnapshot();
            SnapshotFullName fullName = Snapshot.FullName();
            SnapshotFullName snapshotName =
                new SnapshotFullName(fullName.Filename + "_schema", fullName.FolderPath);
            executor.Schema.Print().MatchSnapshot(snapshotName);
        }

        [Fact]
        public async Task ErrorMiddleware_Should_CatchCustomerException_WhenRegistered()
        {
            // Arrange
            IRequestExecutor executor =
                await BuildSchemaAsync(
                    () => throw new CustomException(),
                    field => field.Error<CustomException>());

            // Act
            IExecutionResult res = await executor.ExecuteAsync(_query);

            // Assert
            res.ToJson().MatchSnapshot();
            SnapshotFullName fullName = Snapshot.FullName();
            SnapshotFullName snapshotName =
                new SnapshotFullName(fullName.Filename + "_schema", fullName.FolderPath);
            executor.Schema.Print().MatchSnapshot(snapshotName);
        }

        [Fact]
        public async Task ErrorMiddleware_Should_MapException_WhenRegistered()
        {
            // Arrange
            IRequestExecutor executor =
                await BuildSchemaAsync(
                    () => throw new InvalidOperationException(),
                    field => field.Error<CustomError>());

            // Act
            IExecutionResult res = await executor.ExecuteAsync(_query);

            // Assert
            res.ToJson().MatchSnapshot();
            SnapshotFullName fullName = Snapshot.FullName();
            SnapshotFullName snapshotName =
                new SnapshotFullName(fullName.Filename + "_schema", fullName.FolderPath);
            executor.Schema.Print().MatchSnapshot(snapshotName);
        }

        [Fact]
        public async Task ErrorMiddleware_Should_MapAggregateException_WhenRegistered()
        {
            // Arrange
            IRequestExecutor executor =
                await BuildSchemaAsync(
                    () => throw new AggregateException(
                              new InvalidOperationException(),
                              new NullReferenceException(),
                              new ArgumentException()),
                    field => field.Error<CustomError>()
                        .Error<CustomNullRef>()
                        .Error<ArgumentException>());

            // Act
            IExecutionResult res = await executor.ExecuteAsync(_query);

            // Assert
            res.ToJson().MatchSnapshot();
            SnapshotFullName fullName = Snapshot.FullName();
            SnapshotFullName snapshotName =
                new SnapshotFullName(fullName.Filename + "_schema", fullName.FolderPath);
            executor.Schema.Print().MatchSnapshot(snapshotName);
        }

        [Fact]
        public async Task ErrorMiddleware_Should_MapFactoryMethodException_WhenRegistered()
        {
            // Arrange
            IRequestExecutor executor =
                await BuildSchemaAsync(
                    () => throw new InvalidOperationException(),
                    field => field.Error<CustomErrorWithFactory>());

            // Act
            IExecutionResult res = await executor.ExecuteAsync(_query);

            // Assert
            res.ToJson().MatchSnapshot();
            SnapshotFullName fullName = Snapshot.FullName();
            SnapshotFullName snapshotName =
                new SnapshotFullName(fullName.Filename + "_schema", fullName.FolderPath);
            executor.Schema.Print().MatchSnapshot(snapshotName);
        }

        [Fact]
        public async Task ErrorMiddleware_Should_MapMultipleFactoryMethodException_WhenRegistered()
        {
            // Arrange
            IRequestExecutor executor =
                await BuildSchemaAsync(
                    () => throw new NullReferenceException(),
                    field => field.Error<CustomErrorWithMultipleFactory>());

            // Act
            IExecutionResult res = await executor.ExecuteAsync(_query);

            // Assert
            res.ToJson().MatchSnapshot();
            SnapshotFullName fullName = Snapshot.FullName();
            SnapshotFullName snapshotName =
                new SnapshotFullName(fullName.Filename + "_schema", fullName.FolderPath);
            executor.Schema.Print().MatchSnapshot(snapshotName);
        }

        [Fact]
        public async Task
            ErrorMiddleware_Should_MapMultipleFactoriesOfDifferentType_FirstEx()
        {
            // Arrange
            IRequestExecutor executor =
                await BuildSchemaAsync(
                    () => throw new InvalidOperationException(),
                    field => field.Error<CustomErrorWithMultipleFactoriesOfDifferentType>());

            // Act
            IExecutionResult res = await executor.ExecuteAsync(_query);

            // Assert
            res.ToJson().MatchSnapshot();
            SnapshotFullName fullName = Snapshot.FullName();
            SnapshotFullName snapshotName =
                new SnapshotFullName(fullName.Filename + "_schema", fullName.FolderPath);
            executor.Schema.Print().MatchSnapshot(snapshotName);
        }

        [Fact]
        public async Task
            ErrorMiddleware_Should_MapMultipleFactoriesOfDifferentType_SecondEx()
        {
            // Arrange
            IRequestExecutor executor =
                await BuildSchemaAsync(
                    () => throw new NullReferenceException(),
                    field => field.Error<CustomErrorWithMultipleFactoriesOfDifferentType>());

            // Act
            IExecutionResult res = await executor.ExecuteAsync(_query);

            // Assert
            res.ToJson().MatchSnapshot();
            SnapshotFullName fullName = Snapshot.FullName();
            SnapshotFullName snapshotName =
                new SnapshotFullName(fullName.Filename + "_schema", fullName.FolderPath);
            executor.Schema.Print().MatchSnapshot(snapshotName);
        }

        private ValueTask<IRequestExecutor> BuildSchemaAsync(
            Action throwError,
            Action<IObjectFieldDescriptor> configureField) =>
            new ServiceCollection()
                .AddGraphQL()
                .AddQueryType(x =>
                {
                    x.Name("Query");
                    IObjectFieldDescriptor field = x.Field("throw")
                        .Type<ObjectType<Payload>>()
                        .Resolver(ctx =>
                        {
                            throwError();
                            return new Payload();
                        });
                    configureField(field);
                })
                .BuildRequestExecutorAsync();

        public class CustomNullRef
        {
            public CustomNullRef(NullReferenceException exception)
            {
                Message = "This is a null ref";
            }

            public string Message { get; }
        }

        public class CustomErrorWithFactory
        {
            public static CustomErrorWithFactory CreateErrorFrom(InvalidOperationException ex)
            {
                return new CustomErrorWithFactory();
            }

            public string Message => "Foo";
        }

        public class CustomErrorWithMultipleFactory
        {
            public static CustomErrorWithMultipleFactory CreateErrorFrom(
                InvalidOperationException ex)
            {
                return new CustomErrorWithMultipleFactory();
            }

            public static CustomErrorWithMultipleFactory CreateErrorFrom(NullReferenceException ex)
            {
                return new CustomErrorWithMultipleFactory();
            }

            public string Message => "Foo";
        }

        public class CustomErrorWithMultipleFactoriesOfDifferentType
        {
            public static CustomError CreateErrorFrom(InvalidOperationException ex)
            {
                return new CustomError(ex);
            }

            public static CustomNullRef CreateErrorFrom(NullReferenceException ex)
            {
                return new CustomNullRef(ex);
            }

            public string Message => "Foo";
        }

        public class CustomError
        {
            public CustomError(InvalidOperationException exception)
            {
                Message = "Did work";
            }

            public string Message { get; }
        }

        public class CustomException : Exception
        {
            public string Message => "Customer Exception";
        }

        public class Payload
        {
            public string Foo() => "Bar";
        }
    }
}
