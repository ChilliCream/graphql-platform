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
          new("""{ fieldWithScalarInput(arg: "foo") }""", "ScalarInput"),
          new("""{ fieldWithNonNullScalarInput(arg: "foo") }""", "NonNullScalarInput"),
          new("""{ fieldWithObjectInput(arg: { id: "foo" }) }""", "ObjectInput"),
          new("""{ fieldWithNestedObjectInput(arg: { inner: { id: "foo" } }) }""", "NestedObjectInput"),
          new("""{ fieldWithListOfScalarsInput(arg: ["foo"]) }""", "ListOfScalarsInput"),
          new("""{ fieldWithListOfScalarsInput(arg: ["ok", "foo"]) }""", "ListOfScalarsInputWithOkValue"),
          new("""{ fieldWithObjectWithListOfScalarsInput(arg: { ids: ["foo"] }) }""", "ObjectWithListOfScalarsInput"),
          new("""{ fieldWithListOfObjectsInput(arg: { items: [{ id: "foo" }] }) }""", "ListOfObjectsInput"),
          new("""query($v: String!) { fieldWithScalarInput(arg: $v) }""", "VariableInput"),
          new("""query($v: String!) { fieldWithNestedObjectInput(arg: { inner: { id: $v } }) }""", "NestedVariableInput"),
          new("""{ echo(arg: "foo") @boom(arg: "foo") }""", "DirectiveInput"),
          new("""{ nestedObjectOutput { inner { id @boom(arg: "foo") } } }""", "NestedDirectiveInput")
    ];

    public static readonly TheoryData<TestCase> TestCasesForMutationConventions =
    [
        new("""mutation{ fieldWithScalarInput(input: { arg: "foo" }) { string } }""", "ScalarInput"),
        new("""mutation{ fieldWithNonNullScalarInput(input: { arg: "foo" }) { string } }""", "NonNullScalarInput"),
        new("""mutation{ fieldWithObjectInput(input: { arg: { id: "foo" } }) { string } }""", "ObjectInput"),
        new("""mutation{ fieldWithNestedObjectInput(input: { arg: { inner: { id: "foo" } } }) { string } }""",
            "NestedObjectInput"),
        new("""mutation{ fieldWithListOfScalarsInput(input: { arg: ["foo"] }) { string } }""", "ListOfScalarsInput"),
        new("""mutation{ fieldWithListOfScalarsInput(input: { arg: ["ok", "foo"] }) { string } }""",
            "ListOfScalarsInputWithOkValue"),
        new("""mutation{ fieldWithObjectWithListOfScalarsInput(input: { arg: { ids: ["foo"] } }) { string } }""",
            "ObjectWithListOfScalarsInput"),
        new("""mutation{ fieldWithListOfObjectsInput(input: { arg: { items: [{ id: "foo" }] } }) { string } }""",
            "ListOfObjectsInput"),
        new("""mutation($v: String!) { fieldWithScalarInput(input: { arg: $v }) { string } }""", "VariableInput"),
        new("""mutation($v: String!) { fieldWithNestedObjectInput(input: { arg: { inner: { id: $v } } })  { string } }""", "NestedVariableInput"),
        new("""mutation{ echo(input: { arg: "foo"}) @boom(arg: "foo") { string }  }""", "DirectiveInput"),
        new("""mutation { nestedObjectOutput { nestedObject { inner { id @boom(arg: "foo") } } } }""",
           "NestedDirectiveInput")
    ];

    public static readonly TheoryData<TestCase> TestCasesForQueryConventions =
    [
        new("""{ fieldWithScalarInput(arg: "foo") { ... on ObjectWithId { id } } }""", "ScalarInput"),
        new("""{ fieldWithNonNullScalarInput(arg: "foo") { ... on ObjectWithId { id } } }""", "NonNullScalarInput"),
        new("""{ fieldWithObjectInput(arg: { id: "foo" }) { ... on ObjectWithId { id } } }""", "ObjectInput"),
        new("""{ fieldWithNestedObjectInput(arg: { inner: { id: "foo" } }) { ... on ObjectWithId { id } } }""",
            "NestedObjectInput"),
        new("""{ fieldWithListOfScalarsInput(arg: ["foo"]) { ... on ObjectWithId { id } } }""", "ListOfScalarsInput"),
        new("""{ fieldWithListOfObjectsInput(arg: { items: [{ id: "foo" }] }) { ... on ObjectWithId { id } } }""",
            "ListOfObjectsInput"),
        new("""{ fieldWithObjectWithListOfScalarsInput(arg: { ids: ["foo"] }) { ... on ObjectWithId { id } } }""",
            "ObjectWithListOfScalarsInput"),
        new("""query($v: String!) { fieldWithScalarInput(arg: $v) { ... on ObjectWithId { id } } }""",
            "VariableInput"),
        new("""{ echo(arg: "foo") @boom(arg: "foo") { ... on ObjectWithId { id } } }""", "DirectiveInput"),
        new("""{ nestedObjectOutput { ... on NestedObject { inner { id @boom(arg: "foo") } } } }""", "NestedDirectiveInput")
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
            .AddTypeConverter<string, BrokenType>(x => x == "ok" ? new BrokenType(1) : throw new CustomIdSerializationException("Boom"))
            .BindRuntimeType<BrokenType, StringType>()
            .ModifyRequestOptions(x => x.IncludeExceptionDetails = true)
            .AddErrorFilter(x =>
            {
                caughtException = x.Exception;
                return x;
            })
            .BuildRequestExecutorAsync();

        // act
        var requestBuilder = OperationRequestBuilder
            .New()
            .SetDocument(testCase.QueryString);

        if (testCase.DisplayName.Contains("VariableInput"))
        {
            requestBuilder.SetVariableValues(
                """
                {
                  "v": "foo"
                }
                """);
        }

        var result = await executor.ExecuteAsync(requestBuilder.Build());

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
            .AddTypeConverter<string, BrokenType>(x => x == "ok" ? new BrokenType(1) : throw new CustomIdSerializationException("Boom"))
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
        var mutation = $"mutation{testCase.QueryString.TrimStart("query")}";

        var requestBuilder = OperationRequestBuilder
            .New()
            .SetDocument(mutation);

        if (testCase.DisplayName.Contains("VariableInput"))
        {
            requestBuilder.SetVariableValues(
                """
                {
                  "v": "foo"
                }
                """);
        }

        var result = await executor.ExecuteAsync(requestBuilder.Build());

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
        var requestBuilder = OperationRequestBuilder
            .New()
            .SetDocument(testCase.QueryString);

        if (testCase.DisplayName.Contains("VariableInput"))
        {
            requestBuilder.SetVariableValues(
                """
                {
                  "v": "foo"
                }
                """);
        }

        var result = await executor.ExecuteAsync(requestBuilder.Build());

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
        var requestBuilder = OperationRequestBuilder
            .New()
            .SetDocument(testCase.QueryString);

        if (testCase.DisplayName.Contains("VariableInput"))
        {
            requestBuilder.SetVariableValues(
                """
                {
                  "v": "foo"
                }
                """);
        }

        var result = await executor.ExecuteAsync(requestBuilder.Build());

        // assert
        Assert.IsType<CustomIdSerializationException>(caughtException);
        result.MatchSnapshot(postFix: testCase.DisplayName);
    }

    public record BrokenType(int Value)
    {
        public static BrokenType Parse(string id) => throw new CustomIdSerializationException("Boom");
    }

    public record ObjectWithId(BrokenType Id);
    public record ObjectWithListOfIds(List<BrokenType> Ids);
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
        public string? FieldWithScalarInput(BrokenType arg) => null;
        public string? FieldWithObjectInput(ObjectWithId arg) => null;
        public string? FieldWithListOfScalarsInput(List<BrokenType> arg) => null;
        public string? FieldWithObjectWithListOfScalarsInput(ObjectWithListOfIds arg) => null;
        public string? FieldWithNestedObjectInput(NestedObject arg) => null;
        // ReSharper disable once MemberHidesStaticFromOuterClass
        public string? FieldWithListOfObjectsInput(ListOfObjectsInput arg) => null;
        public string? FieldWithNonNullScalarInput([GraphQLNonNullType] BrokenType arg) => null;
        public string? Echo(string arg) => null;
        public NestedObject? NestedObjectOutput => null;
    }

    public class SomeQueryConventionFriendlyQueryType
    {
        [Error<CustomIdSerializationException>]
        public ObjectWithId? FieldWithScalarInput(BrokenType arg) => null;
        [Error<CustomIdSerializationException>]
        public ObjectWithId? FieldWithObjectInput(ObjectWithId arg) => null;
        [Error<CustomIdSerializationException>]
        public ObjectWithId? FieldWithListOfScalarsInput(List<BrokenType> arg) => null;
        [Error<CustomIdSerializationException>]
        public ObjectWithId? FieldWithObjectWithListOfScalarsInput(ObjectWithListOfIds arg) => null;
        [Error<CustomIdSerializationException>]
        public ObjectWithId? FieldWithNestedObjectInput(NestedObject arg) => null;
        // ReSharper disable once MemberHidesStaticFromOuterClass
        [Error<CustomIdSerializationException>]
        public ObjectWithId? FieldWithListOfObjectsInput(ListOfObjectsInput arg) => null;
        [Error<CustomIdSerializationException>]
        public ObjectWithId? FieldWithNonNullScalarInput([GraphQLNonNullType] BrokenType arg) => null;
        [Error<CustomIdSerializationException>]
        public ObjectWithId? Echo(string arg) => null;
        [Error<CustomIdSerializationException>]
        public NestedObject? NestedObjectOutput => null;
    }

    public class CustomIdSerializationException(string message) : Exception(message)
    {
        // Override stacktrace since otherwise the tests will be flaky
        public override string StackTrace => "Test";
    }
}
