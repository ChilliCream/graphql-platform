using System.Diagnostics.CodeAnalysis;
using HotChocolate.Execution.Configuration;
using HotChocolate.Types;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Execution.Integration.TypeConverter;

public class TypeConverterExceptionPropagationTests
{
    public record TestCase([StringSyntax("graphql")] string Query, string DisplayName)
    {
        public override string ToString() => DisplayName;
    }

    public static readonly TheoryData<TestCase> TestCases =
    [
        new("""{ inputParameter(arg: "foo") }""", "ForScalarFieldInputParameter"),
        new("""{ inputObject(arg: { id: "foo" }) }""", "ForObjectWithScalarInputParameter"),
        new("""{ listInputParameter(arg: ["foo"]) }""", "ForListInputParameter"),
        new("""{ listInputObject(arg: { id: ["foo"] }) }""", "ForObjectWithScalarListInputParameter")
    ];

    [Theory]
    [MemberData(nameof(TestCases))]
    public async Task ExceptionOfTypeConverter_IsAvailableInErrorFilter(TestCase testCase)
    {
        // arrange
        Exception? caughtException = null;
        var executor = await new ServiceCollection()
            .AddGraphQLServer()
            .AddQueryType<SomeQuery>()
            .AddTypeConverter<string, FailingId>(FailingId.Parse)
            .AddTypeConverter<FailingId, string>(id => id.Value.ToString())
            .BindRuntimeType<FailingId, StringType>()
            .ModifyRequestOptions(x => x.IncludeExceptionDetails = true)
            .AddErrorFilter(x =>
            {
                caughtException = x.Exception;
                return x.WithMessage(x.Exception!.Message);
            })
            .BuildRequestExecutorAsync();

        // act
        var result = await executor.ExecuteAsync(testCase.Query);

        // assert
        Assert.IsType<CustomIdSerializationException>(caughtException);
        result.MatchSnapshot(postFix: testCase.DisplayName);
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
