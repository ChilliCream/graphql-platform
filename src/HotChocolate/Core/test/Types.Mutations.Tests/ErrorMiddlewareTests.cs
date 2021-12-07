using System;
using System.Threading.Tasks;
using HotChocolate.Execution;
using HotChocolate.Execution.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Snapshooter;
using Snapshooter.Xunit;
using Xunit;

namespace HotChocolate.Types;

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
        }";

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
        SnapshotFullName snapshotName = new(fullName.Filename + "_schema", fullName.FolderPath);
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
        SnapshotFullName snapshotName = new(fullName.Filename + "_schema", fullName.FolderPath);
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
        SnapshotFullName snapshotName = new(fullName.Filename + "_schema", fullName.FolderPath);
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
        SnapshotFullName snapshotName = new(fullName.Filename + "_schema", fullName.FolderPath);
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
        SnapshotFullName snapshotName = new(fullName.Filename + "_schema", fullName.FolderPath);
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
        SnapshotFullName snapshotName = new(fullName.Filename + "_schema", fullName.FolderPath);
        executor.Schema.Print().MatchSnapshot(snapshotName);
    }

    [Fact]
    public async Task ErrorMiddleware_Should_MapMultipleFactoriesOfDifferentType_FirstEx()
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
        SnapshotFullName snapshotName = new(fullName.Filename + "_schema", fullName.FolderPath);
        executor.Schema.Print().MatchSnapshot(snapshotName);
    }

    [Fact]
    public async Task ErrorMiddleware_Should_MapMultipleFactoriesOfDifferentType_SecondEx()
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
        SnapshotFullName snapshotName = new(fullName.Filename + "_schema", fullName.FolderPath);
        executor.Schema.Print().MatchSnapshot(snapshotName);
    }

    [Fact]
    public async Task ErrorMiddleware_Should_MapMultipleFactories_When_NotStatic()
    {
        // Arrange
        IRequestExecutor executor =
            await BuildSchemaAsync(
                () => throw new NullReferenceException(),
                field => field.Error<CustomErrorNonStatic>());

        // Act
        IExecutionResult res = await executor.ExecuteAsync(_query);

        // Assert
        res.ToJson().MatchSnapshot();
        SnapshotFullName fullName = Snapshot.FullName();
        SnapshotFullName snapshotName = new(fullName.Filename + "_schema", fullName.FolderPath);
        executor.Schema.Print().MatchSnapshot(snapshotName);
    }

    [Fact]
    public async Task ErrorMiddleware_Should_MapMultipleFactories_When_InterfaceIsUsed()
    {
        // Arrange
        IRequestExecutor executor =
            await BuildSchemaAsync(
                () => throw new NullReferenceException(),
                field => field.Error<CustomErrorPayloadErrorFactory>());

        // Act
        IExecutionResult res = await executor.ExecuteAsync(_query);

        // Assert
        res.ToJson().MatchSnapshot();
        SnapshotFullName fullName = Snapshot.FullName();
        SnapshotFullName snapshotName = new(fullName.Filename + "_schema", fullName.FolderPath);
        executor.Schema.Print().MatchSnapshot(snapshotName);
    }

    [Fact]
    public async Task ErrorMiddleware_Should_AllowToCustomizeErrorInterfaceType()
    {
        // Arrange
        IRequestExecutor executor =
            await BuildSchemaAsync(
                () => throw new NullReferenceException(),
                field => field.Error<CustomInterfaceError>(),
                b => b.AddErrorInterfaceType<CustomErrorInterfaceType>());

        // Act
        IExecutionResult res = await executor.ExecuteAsync(@"
             query {
                throw {
                    errors {
                        __typename
                        ... on CustomInterfaceError {
                            message
                            code
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
    public async Task ErrorMiddleware_Should_AllowToCustomizeErrorInterfaceRuntimeType()
    {
        // Arrange
        IRequestExecutor executor =
            await BuildSchemaAsync(
                () => throw new NullReferenceException(),
                field => field.Error<CustomInterfaceError>(),
                b => b.AddErrorInterfaceType<IUserError>());

        // Act
        IExecutionResult res = await executor.ExecuteAsync(@"
             query {
                throw {
                    errors {
                        __typename
                        ... on IUserError {
                            message
                            code
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

    private ValueTask<IRequestExecutor> BuildSchemaAsync(
        Action throwError,
        Action<IObjectFieldDescriptor> configureField,
        Action<IRequestExecutorBuilder>? configureSchema = null)
    {
        IRequestExecutorBuilder builder = new ServiceCollection()
            .AddGraphQL()
            .EnableMutationConventions()
            .AddQueryType(x =>
            {
                x.Name("Query");
                IObjectFieldDescriptor field = x.Field("throw")
                    .Type<ObjectType<Payload>>()
                    .Resolve(_ =>
                    {
                        throwError();
                        return new Payload();
                    });
                configureField(field);
            });

        configureSchema?.Invoke(builder);

        return builder.BuildRequestExecutorAsync();
    }

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
        public static CustomErrorWithMultipleFactory CreateErrorFrom(InvalidOperationException ex)
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

    public class CustomErrorNonStatic
    {
        public CustomError CreateErrorFrom(InvalidOperationException ex)
        {
            return new CustomError(ex);
        }

        public CustomNullRef CreateErrorFrom(NullReferenceException ex)
        {
            return new CustomNullRef(ex);
        }
    }

    public class CustomErrorPayloadErrorFactory
        : IPayloadErrorFactory<InvalidOperationException, CustomError>
        , IPayloadErrorFactory<NullReferenceException, CustomNullRef>
    {
        public CustomError CreateErrorFrom(InvalidOperationException exception)
        {
            return new CustomError(exception);
        }

        public CustomNullRef CreateErrorFrom(NullReferenceException exception)
        {
            return new CustomNullRef(exception);
        }
    }

    public class CustomErrorInterfaceType : InterfaceType
    {
        protected override void Configure(IInterfaceTypeDescriptor descriptor)
        {
            descriptor.Name("UserError");
            descriptor.Field("message").Type<NonNullType<StringType>>();
            descriptor.Field("code").Type<NonNullType<StringType>>();
        }
    }

    public class CustomInterfaceError
    {
        public CustomInterfaceError(NullReferenceException exception)
        {
            Message = "Did work";
        }

        public string Message { get; }

        public string Code => "CODE";
    }

    public interface IUserError
    {
        string? Message { get; }

        string? Code { get; }
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
