using System.Diagnostics.CodeAnalysis;
using HotChocolate.Types;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Execution.Integration.TypeConverter;

public partial class TypeConverterTests
{
    public record TestCase([StringSyntax("graphql")] string QueryString, string DisplayName)
    {
        public override string ToString() => DisplayName;
    }

    public static readonly TheoryData<TestCase> TestCases =
    [
         new("""{ scalarInput(arg: "foo") }""", "ScalarInput"),
         new("""{ objectInput(arg: { id: "foo" }) }""", "ObjectInput"),
         new("""{ listOfScalarsInput(arg: ["foo"]) }""", "ListOfScalarsInput"),
         new("""{ objectWithListOfScalarsInput(arg: { id: ["foo"] }) }""", "ObjectWithListOfScalarsInput"),
         new("""{ nestedObjectInput(arg: { inner: { id: "foo" } }) }""", "NestedObjectInput"),
         new("""{ listOfObjectsInput(arg: { items: [{ id: "foo" }] }) }""", "ListOfObjectsInput"),
         new("""{ nonNullScalarInput(arg: "foo") }""", "NonNullScalarInput"),
         new("""query($v: String!) { scalarInput(arg: $v) }""", "VariableInput"),
         new("""{ echo(arg: "foo") @boom(arg: "foo") }""", "DirectiveInput")
    ];

    public static readonly TheoryData<TestCase> TestCasesForMutationConventions =
    [
        new("""mutation{ scalarInput(input: { arg: "foo" }) { string } }""", "ScalarInput"),
        new("""mutation{ objectInput(input: { arg: { id: "foo" } }) { string } }""", "ObjectInput"),
        new("""mutation{ listOfScalarsInput(input: { arg: ["foo"] }) { string } }""", "ListOfScalarsInput"),
        new("""mutation{ objectWithListOfScalarsInput(input: { arg: { id: ["foo"] } }) { string } }""",
            "ObjectWithListOfScalarsInput"),
        new("""mutation{ nestedObjectInput(input: { arg: { inner: { id: "foo" } } }) { string } }""",
            "NestedObjectInput"),
        new("""mutation{ listOfObjectsInput(input: { arg: { items: [{ id: "foo" }] } }) { string } }""",
            "ListOfObjectsInput"),
        new("""mutation{ nonNullScalarInput(input: { arg: "foo" }) { string } }""", "NonNullScalarInput"),
        new("""mutation($v: String!) { scalarInput(input: { arg: $v }) { string } }""", "VariableInput"),
        new("""mutation{ echo(input: { arg: "foo"}) @boom(arg: "foo") { string }  }""", "DirectiveInput")
    ];

    public static readonly TheoryData<TestCase> TestCasesForQueryConventions =
    [
        new("""{ scalarInput(arg: "foo") { ... on ObjectWithId { id } } }""", "ScalarInput"),
        new("""{ objectInput(arg: { id: "foo" }) { ... on ObjectWithId { id } } }""", "ObjectInput"),
        new("""{ listOfScalarsInput(arg: ["foo"]) { ... on ObjectWithId { id } } }""", "ListOfScalarsInput"),
        new("""{ objectWithListOfScalarsInput(arg: { id: ["foo"] }) { ... on ObjectWithId { id } } }""",
            "ObjectWithListOfScalarsInput"),
        new("""{ nestedObjectInput(arg: { inner: { id: "foo" } }) { ... on ObjectWithId { id } } }""",
            "NestedObjectInput"),
        new("""{ listOfObjectsInput(arg: { items: [{ id: "foo" }] }) { ... on ObjectWithId { id } } }""",
            "ListOfObjectsInput"),
        new("""{ nonNullScalarInput(arg: "foo") { ... on ObjectWithId { id } } }""", "NonNullScalarInput"),
        new("""query($v: String!) { scalarInput(arg: $v) { ... on ObjectWithId { id } } }""",
            "VariableInput"),
        new("""{ echo(arg: "foo") @boom(arg: "foo") { ... on ObjectWithId { id } } }""", "DirectiveInput")
    ];

    [Theory]
    [MemberData(nameof(TestCases))]
    public async Task Exception_IsAvailableInErrorFilter(TestCase testCase)
    {
        // arrange
        Exception? caughtException = null;
        var executor = await new ServiceCollection()
            .AddGraphQLServer()
            .AddQueryType<SomeQuery>()
            .AddDirectiveType<BoomDirectiveType>()
            .AddTypeConverter<string, BrokenType>(_ => throw new CustomIdSerializationException("Boom"))
            .BindRuntimeType<BrokenType, StringType>()
            .ModifyRequestOptions(x => x.IncludeExceptionDetails = true)
            .AddErrorFilter(x =>
            {
                caughtException = x.Exception;
                return x;
            })
            .BuildRequestExecutorAsync();

        // act
        var variableValues =
            testCase.DisplayName == "VariableInput"
                ? new Dictionary<string, object?> { ["v"] = "foo" }
                : [];
        var result = await executor.ExecuteAsync(testCase.QueryString, variableValues: variableValues);

        // assert
        Assert.IsType<CustomIdSerializationException>(caughtException);
        result.MatchSnapshot(postFix: testCase.DisplayName);
    }

    [Theory]
    [MemberData(nameof(TestCases))]
    public async Task Exception_IsAvailableInErrorFilter_Mutation(TestCase testCase)
    {
        // arrange
        Exception? caughtException = null;
        var executor = await new ServiceCollection()
            .AddGraphQLServer()
            .AddMutationType<SomeQuery>()
            .AddDirectiveType<BoomDirectiveType>()
            .AddTypeConverter<string, BrokenType>(_ => throw new CustomIdSerializationException("Boom"))
            .BindRuntimeType<BrokenType, StringType>()
            .ModifyRequestOptions(x => x.IncludeExceptionDetails = true)
            .ModifyOptions(x => x.StrictValidation = false)
            .AddErrorFilter(x =>
            {
                caughtException = x.Exception;
                return x;
            })
            .BuildRequestExecutorAsync();

        // act
        var variableValues =
            testCase.DisplayName == "VariableInput"
                ? new Dictionary<string, object?> { ["v"] = "foo" }
                : [];

        var mutation = $"mutation{testCase.QueryString.TrimStart("query")}";
        var result = await executor.ExecuteAsync(mutation, variableValues: variableValues);

        // assert
        Assert.IsType<CustomIdSerializationException>(caughtException);
        result.MatchSnapshot(postFix: testCase.DisplayName);
    }

    [Theory]
    [MemberData(nameof(TestCasesForMutationConventions))]
    public async Task Exception_IsAvailableInErrorFilter_Mutation_WithMutationConventions(TestCase testCase)
    {
        // arrange
        Exception? caughtException = null;
        var executor = await new ServiceCollection()
            .AddGraphQLServer()
            .AddMutationType<SomeQuery>()
            .AddMutationConventions()
            .AddDirectiveType<BoomDirectiveType>()
            .AddTypeConverter<string, BrokenType>(_ => throw new CustomIdSerializationException("Boom"))
            .BindRuntimeType<BrokenType, StringType>()
            .ModifyRequestOptions(x => x.IncludeExceptionDetails = true)
            .ModifyOptions(x => x.StrictValidation = false)
            .AddErrorFilter(x =>
            {
                caughtException = x.Exception;
                return x;
            })
            .BuildRequestExecutorAsync();

        // act
        var variableValues =
            testCase.DisplayName == "VariableInput"
                ? new Dictionary<string, object?> { ["v"] = "foo" }
                : [];

        var result = await executor.ExecuteAsync(testCase.QueryString, variableValues: variableValues);

        // assert
        Assert.IsType<CustomIdSerializationException>(caughtException);
        result.MatchSnapshot(postFix: testCase.DisplayName);
    }

    [Theory]
    [MemberData(nameof(TestCasesForQueryConventions))]
    public async Task Exception_IsAvailableInErrorFilter_WithQueryConventions(TestCase testCase)
    {
        // arrange
        Exception? caughtException = null;
        var executor = await new ServiceCollection()
            .AddGraphQLServer()
            .AddQueryType<SomeQueryConventionFriendlyQueryType>()
            .AddQueryConventions()
            .AddDirectiveType<BoomDirectiveType>()
            .AddTypeConverter<string, BrokenType>(_ => throw new CustomIdSerializationException("Boom"))
            .BindRuntimeType<BrokenType, StringType>()
            .ModifyRequestOptions(x => x.IncludeExceptionDetails = true)
            .ModifyOptions(x => x.StrictValidation = false)
            .AddErrorFilter(x =>
            {
                caughtException = x.Exception;
                return x;
            })
            .BuildRequestExecutorAsync();

        // act
        var variableValues =
            testCase.DisplayName == "VariableInput"
                ? new Dictionary<string, object?> { ["v"] = "foo" }
                : [];

        var result = await executor.ExecuteAsync(testCase.QueryString, variableValues: variableValues);

        // assert
        Assert.IsType<CustomIdSerializationException>(caughtException);
        result.MatchSnapshot(postFix: testCase.DisplayName);
    }

    public record BrokenType(int Value)
    {
        public static BrokenType Parse(string id) => throw new CustomIdSerializationException("Boom");
    }

    public record ObjectWithId(BrokenType Id);
    public record ObjectWithListOfIds(List<BrokenType> Id);
    public record NestedObject(ObjectWithId Inner);
    public record ListOfObjectsInput(List<ObjectWithId> Items);

    public class BoomDirective
    {
        public BrokenType Arg { get; set; } = null!;
    }

    public class BoomDirectiveType : DirectiveType<BoomDirective>
    {
        protected override void Configure(IDirectiveTypeDescriptor<BoomDirective> descriptor)
        {
            descriptor.Name("boom");
            descriptor.Location(DirectiveLocation.Field);
            descriptor.BindArgumentsImplicitly();

            descriptor.Use((next, directive) => async context =>
            {
                await next.Invoke(context);
                var s = directive.ToValue<BoomDirective>().Arg;
                context.Result = context.Result?.ToString() + s;
            });
        }
    }

    public class SomeQuery
    {
        public string? ScalarInput(BrokenType arg) => null;
        public string? ObjectInput(ObjectWithId arg) => null;
        public string? ListOfScalarsInput(List<BrokenType> arg) => null;
        public string? ObjectWithListOfScalarsInput(ObjectWithListOfIds arg) => null;
        public string? NestedObjectInput(NestedObject arg) => null;
        // ReSharper disable once MemberHidesStaticFromOuterClass
        public string? ListOfObjectsInput(ListOfObjectsInput arg) => null;
        public string? NonNullScalarInput([GraphQLNonNullType] BrokenType arg) => null;
        public string? Echo(string arg) => null;
    }

    public class SomeQueryConventionFriendlyQueryType
    {
        [Error<CustomIdSerializationException>]
        public ObjectWithId? ScalarInput(BrokenType arg) => null;
        [Error<CustomIdSerializationException>]
        public ObjectWithId? ObjectInput(ObjectWithId arg) => null;
        [Error<CustomIdSerializationException>]
        public ObjectWithId? ListOfScalarsInput(List<BrokenType> arg) => null;
        [Error<CustomIdSerializationException>]
        public ObjectWithId? ObjectWithListOfScalarsInput(ObjectWithListOfIds arg) => null;
        [Error<CustomIdSerializationException>]
        public ObjectWithId? NestedObjectInput(NestedObject arg) => null;
        // ReSharper disable once MemberHidesStaticFromOuterClass
        [Error<CustomIdSerializationException>]
        public ObjectWithId? ListOfObjectsInput(ListOfObjectsInput arg) => null;
        [Error<CustomIdSerializationException>]
        public ObjectWithId? NonNullScalarInput([GraphQLNonNullType] BrokenType arg) => null;
        [Error<CustomIdSerializationException>]
        public ObjectWithId? Echo(string arg) => null;
    }

    public class CustomIdSerializationException(string message) : Exception(message)
    {
        // Override stacktrace since otherwise the tests will be flaky
        public override string StackTrace => "Test";
    }
}
