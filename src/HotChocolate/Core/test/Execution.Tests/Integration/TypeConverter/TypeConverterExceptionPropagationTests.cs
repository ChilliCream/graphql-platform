using System.Diagnostics;
using HotChocolate.Execution.Configuration;
using HotChocolate.Types;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Execution.Integration.TypeConverter;

public class TypeConverterExceptionPropagationTests
{
    [Fact]
    public async Task ExceptionOfTypeConverter_ForScalarInputParameter_IsAvailableInErrorFilter()
    {
        // arrange
        var executor = await GetServiceCollection()
            .AddErrorFilter(AssertErrorFilter)
            .BuildRequestExecutorAsync();

        // act
        var result = await executor.ExecuteAsync("""{ inputParameter(arg: "foo") }""");

        // assert
        result.MatchSnapshot();
        static IError AssertErrorFilter(IError x)
        {
            Assert.IsType<CustomIdSerializationException>(x.Exception);
            return x.WithMessage(x.Exception.Message);
        }
    }

    [Fact]
    public async Task ExceptionOfTypeConverter_ForListInputParameter_IsAvailableInErrorFilter()
    {
        // arrange
        var executor = await GetServiceCollection()
            .AddErrorFilter(AssertErrorFilter)
            .BuildRequestExecutorAsync();

        // act
        var result = await executor.ExecuteAsync("""{ listInputParameter(arg: ["foo"]) }""");

        // assert
        result.MatchSnapshot();
        static IError AssertErrorFilter(IError x)
        {
            Assert.IsType<CustomIdSerializationException>(x.Exception);
            return x.WithMessage(x.Exception.Message);
        }
    }
    [Fact]
    public async Task ExceptionOfTypeConverter_ForObjectWithScalarInputParameter_IsAvailableInErrorFilter()
    {
        // arrange
        Exception? caughtException = null;
        var executor = await GetServiceCollection()
            .AddErrorFilter(AssertErrorFilter)
            .BuildRequestExecutorAsync();

        // act
        var result = await executor.ExecuteAsync("""{ inputObject(arg: { id: "foo" }) }""");

        // assert
        Assert.IsType<CustomIdSerializationException>(caughtException);
        result.MatchSnapshot();

        IError AssertErrorFilter(IError x)
        {
            caughtException = x.Exception;
            return x.WithMessage(x.Exception!.Message);
        }
    }

    [Fact]
    public async Task ExceptionOfTypeConverter_ForObjectWithScalarListInputParameter_IsAvailableInErrorFilter()
    {
        // arrange
        var executor = await GetServiceCollection()
            .AddErrorFilter(AssertErrorFilter)
            .BuildRequestExecutorAsync();

        // act
        var result = await executor.ExecuteAsync("""{ listInputObject(arg: { id: ["foo"] }) }""");

        // assert
        result.MatchSnapshot();
        static IError AssertErrorFilter(IError x)
        {
            Assert.IsType<CustomIdSerializationException>(x.Exception);
            return x.WithMessage(x.Exception.Message);
        }
    }

    private static IRequestExecutorBuilder GetServiceCollection()
    {
        return new ServiceCollection()
            .AddGraphQLServer()
            .AddQueryType<SomeQuery>()
            .AddTypeConverter<string, FailingId>(FailingId.Parse)
            .AddTypeConverter<FailingId, string>(id => id.Value.ToString())
            .BindRuntimeType<FailingId, StringType>()
            .ModifyRequestOptions(x => x.IncludeExceptionDetails = true);
    }

    public record FailingId(int Value)
    {
        public static FailingId Parse(string id) => throw new CustomIdSerializationException("Boom");
    }

    public record ObjectWithId(FailingId Id);
    public record ObjectWithListOfIds(List<FailingId> Id);

    public class SomeQuery
    {
        public string Boom() => throw new Exception("Boom");
        public string InputParameter(FailingId arg) => "";
        public string InputObject(ObjectWithId arg) => "";
        public string ListInputParameter(List<FailingId> arg) => "";
        public string ListInputObject(ObjectWithListOfIds arg) => "";
    }

    public class CustomIdSerializationException(string message) : Exception(message)
    {
        // Override stacktrace since otherwise the tests will be flaky
        public override string StackTrace => "Test";
    }
}
