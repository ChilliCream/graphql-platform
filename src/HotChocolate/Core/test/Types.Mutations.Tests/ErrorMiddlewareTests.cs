using HotChocolate.Execution;
using HotChocolate.Execution.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Types;

public class ErrorMiddlewareTests
{
    private const string _query = @"
        mutation {
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
        var executor =
            await BuildSchemaAsync(
                () => throw new InvalidOperationException(),
                field => field.Error<InvalidOperationException>());

        // Act
        var res = await executor.ExecuteAsync(_query);

        // Assert
        await Snapshot.Create()
            .Add(res, "result:")
            .Add(executor.Schema, "schema:")
            .MatchAsync();
    }

    [Fact]
    public async Task ErrorMiddleware_Should_CatchCustomerException_WhenRegistered()
    {
        // Arrange
        var executor =
            await BuildSchemaAsync(
                () => throw new CustomException(),
                field => field.Error<CustomException>());

        // Act
        var res = await executor.ExecuteAsync(_query);

        // Assert
        await Snapshot.Create()
            .Add(res, "result:")
            .Add(executor.Schema, "schema:")
            .MatchAsync();
    }

    [Fact]
    public async Task ErrorMiddleware_Should_MapException_WhenRegistered()
    {
        // Arrange
        var executor =
            await BuildSchemaAsync(
                () => throw new InvalidOperationException(),
                field => field.Error<CustomError>());

        // Act
        var res = await executor.ExecuteAsync(_query);

        // Assert
        await Snapshot.Create()
            .Add(res, "result:")
            .Add(executor.Schema, "schema:")
            .MatchAsync();
    }

    [Fact]
    public async Task ErrorMiddleware_Should_MapAggregateException_WhenRegistered()
    {
        // Arrange
        var executor =
            await BuildSchemaAsync(
                () => throw new AggregateException(
                    new InvalidOperationException(),
                    new NullReferenceException(),
                    new ArgumentException()),
                field => field
                    .Error<CustomError>()
                    .Error<CustomNullRef>()
                    .Error<ArgumentException>());

        // Act
        var res = await executor.ExecuteAsync(_query);

        // Assert
        await Snapshot.Create()
            .Add(res, "result:")
            .Add(executor.Schema, "schema:")
            .MatchAsync();
    }

    [Fact]
    public async Task ErrorMiddleware_Should_MapFactoryMethodException_WhenRegistered()
    {
        // Arrange
        var executor =
            await BuildSchemaAsync(
                () => throw new InvalidOperationException(),
                field => field.Error<CustomErrorWithFactory>());

        // Act
        var res = await executor.ExecuteAsync(_query);

        // Assert
        await Snapshot.Create()
            .Add(res, "result:")
            .Add(executor.Schema, "schema:")
            .MatchAsync();
    }

    [Fact]
    public async Task ErrorMiddleware_Should_MapMultipleFactoryMethodException_WhenRegistered()
    {
        // Arrange
        var executor =
            await BuildSchemaAsync(
                () => throw new NullReferenceException(),
                field => field.Error<CustomErrorWithMultipleFactory>());

        // Act
        var res = await executor.ExecuteAsync(_query);

        // Assert
        await Snapshot.Create()
            .Add(res, "result:")
            .Add(executor.Schema, "schema:")
            .MatchAsync();
    }

    [Fact]
    public async Task ErrorMiddleware_Should_MapMultipleFactoriesOfDifferentType_FirstEx()
    {
        // Arrange
        var executor =
            await BuildSchemaAsync(
                () => throw new InvalidOperationException(),
                field => field.Error<CustomErrorWithMultipleFactoriesOfDifferentType>());

        // Act
        var res = await executor.ExecuteAsync(_query);

        // Assert
        await Snapshot.Create()
            .Add(res, "result:")
            .Add(executor.Schema, "schema:")
            .MatchAsync();
    }

    [Fact]
    public async Task ErrorMiddleware_Should_MapMultipleFactoriesOfDifferentType_SecondEx()
    {
        // Arrange
        var executor =
            await BuildSchemaAsync(
                () => throw new NullReferenceException(),
                field => field.Error<CustomErrorWithMultipleFactoriesOfDifferentType>());

        // Act
        var res = await executor.ExecuteAsync(_query);

        // Assert
        await Snapshot.Create()
            .Add(res, "result:")
            .Add(executor.Schema, "schema:")
            .MatchAsync();
    }

    [Fact]
    public async Task ErrorMiddleware_Should_MapMultipleFactories_When_NotStatic()
    {
        // Arrange
        var executor =
            await BuildSchemaAsync(
                () => throw new NullReferenceException(),
                field => field.Error<CustomErrorNonStatic>());

        // Act
        var res = await executor.ExecuteAsync(_query);

        // Assert
        await Snapshot.Create()
            .Add(res, "result:")
            .Add(executor.Schema, "schema:")
            .MatchAsync();
    }

    [Fact]
    public async Task ErrorMiddleware_Should_MapMultipleFactories_When_InterfaceIsUsed()
    {
        // Arrange
        var executor =
            await BuildSchemaAsync(
                () => throw new NullReferenceException(),
                field => field.Error<CustomErrorPayloadErrorFactory>());

        // Act
        var res = await executor.ExecuteAsync(_query);

        // Assert
        await Snapshot.Create()
            .Add(res, "result:")
            .Add(executor.Schema, "schema:")
            .MatchAsync();
    }

    [Fact]
    public async Task ErrorMiddleware_Should_AllowToCustomizeErrorInterfaceType()
    {
        // Arrange
        var executor =
            await BuildSchemaAsync(
                () => throw new NullReferenceException(),
                field => field.Error<CustomInterfaceError>(),
                b => b.AddErrorInterfaceType<CustomErrorInterfaceType>());

        // Act
        var res = await executor.ExecuteAsync(@"
            mutation {
                throw {
                    errors {
                        __typename
                        ... on CustomInterfaceError {
                            message
                            code
                        }
                    }
                }
            }");

        // Assert
        await Snapshot.Create()
            .Add(res, "result:")
            .Add(executor.Schema, "schema:")
            .MatchAsync();
    }

    [Fact]
    public async Task ErrorMiddleware_Should_AllowToCustomizeErrorInterfaceRuntimeType()
    {
        // Arrange
        var executor =
            await BuildSchemaAsync(
                () => throw new NullReferenceException(),
                field => field.Error<CustomInterfaceError>(),
                b => b.AddErrorInterfaceType<IUserError>());

        // Act
        var res = await executor.ExecuteAsync(@"
            mutation {
                throw {
                    errors {
                        __typename
                        ... on IUserError {
                            message
                            code
                        }
                    }
                }
            }");

        // Assert
        await Snapshot.Create()
            .Add(res, "result:")
            .Add(executor.Schema, "schema:")
            .MatchAsync();
    }

    private ValueTask<IRequestExecutor> BuildSchemaAsync(
        Action throwError,
        Action<IObjectFieldDescriptor> configureField,
        Action<IRequestExecutorBuilder>? configureSchema = null)
    {
        var builder = new ServiceCollection()
            .AddGraphQL()
            .AddMutationConventions(true)
            .AddMutationType(x =>
            {
                x.Name("Mutation");
                var field = x.Field("throw")
                    .Type<ObjectType<Payload>>()
                    .Resolve(_ =>
                    {
                        throwError();
                        return new Payload();
                    });
                configureField(field);
            })
            .ModifyOptions(o => o.StrictValidation = false);

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
        public override string Message => "Customer Exception";
    }

    public class Payload
    {
        public string Foo() => "Bar";
    }
}
