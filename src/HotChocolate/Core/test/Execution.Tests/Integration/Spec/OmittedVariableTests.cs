using HotChocolate.Types;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Execution.Integration.Spec;

public class OmittedVariableTests
{
    [Theory]
    [InlineData("query($v: String) { result: field(input: { value: $v }) }")]
    [InlineData("query($v: String) { result: field(input: { count: 1, value: $v }) }")]
    [InlineData("query($v: String) { result: field(input: { value: $v, count: 1 }) }")]
    [InlineData("query($v: String) { result: nested(input: { inner: { value: $v } }) }")]
    [InlineData("query($v: String) { result: argument(value: $v) }")]
    public async Task ExecuteAsync_Should_ReportValueAsNotSet_When_VariableIsOmitted(
        string document)
    {
        // arrange
        var executor = await CreateExecutorAsync();

        // act
        var result = await executor.ExecuteAsync(document, TestContext.Current.CancellationToken);

        // assert
        Assert.Equal("""{"data":{"result":"not set"}}""", result.ToJson(false));
    }

    [Theory]
    [InlineData("query($v: String) { result: fieldWithDefault(input: { value: $v }) }")]
    [InlineData("query($v: String) { result: argumentWithDefault(value: $v) }")]
    public async Task ExecuteAsync_Should_ApplyDefaultAsNotSet_When_VariableIsOmitted(
        string document)
    {
        // arrange
        var executor = await CreateExecutorAsync();

        // act
        var result = await executor.ExecuteAsync(document, TestContext.Current.CancellationToken);

        // assert
        Assert.Equal("""{"data":{"result":"not set: default"}}""", result.ToJson(false));
    }

    [Theory]
    [InlineData(
        "query($v: String) { result: field(input: { value: $v }) }",
        """{ "v": null }""",
        "set: null")]
    [InlineData(
        "query($v: String = null) { result: field(input: { value: $v }) }",
        "{ }",
        "set: null")]
    [InlineData(
        "query($v: String = \"abc\") { result: field(input: { value: $v }) }",
        "{ }",
        "set: abc")]
    [InlineData(
        "query($v: String = null) { result: fieldWithDefault(input: { value: $v }) }",
        "{ }",
        "set: null")]
    [InlineData(
        "query($v: String) { result: argument(value: $v) }",
        """{ "v": null }""",
        "set: null")]
    [InlineData(
        "query($v: String = null) { result: argument(value: $v) }",
        "{ }",
        "set: null")]
    public async Task ExecuteAsync_Should_ReportValueAsSet_When_VariableHasValue(
        string document,
        string variables,
        string expected)
    {
        // arrange
        var executor = await CreateExecutorAsync();
        var request = OperationRequestBuilder.New()
            .SetDocument(document)
            .SetVariableValues(variables)
            .Build();

        // act
        var result = await executor.ExecuteAsync(request, TestContext.Current.CancellationToken);

        // assert
        Assert.Equal($$$"""{"data":{"result":"{{{expected}}}"}}""", result.ToJson(false));
    }

    private static ValueTask<IRequestExecutor> CreateExecutorAsync()
        => new ServiceCollection()
            .AddGraphQL()
            .AddQueryType<Query>()
            .BuildRequestExecutorAsync(cancellationToken: TestContext.Current.CancellationToken);

    public record ValueInput(Optional<string?> Value, int? Count);

    public record OuterInput(ValueInput Inner);

    public record DefaultedInput([property: DefaultValue("default")] Optional<string?> Value);

    public class Query
    {
        public string Argument(Optional<string?> value) => Describe(value);

        public string ArgumentWithDefault([DefaultValue("default")] Optional<string?> value)
            => Describe(value);

        public string Field(ValueInput input) => Describe(input.Value);

        public string FieldWithDefault(DefaultedInput input) => Describe(input.Value);

        public string Nested(OuterInput input) => Describe(input.Inner.Value);

        private static string Describe(Optional<string?> value)
            => value.HasValue
                ? $"set: {value.Value ?? "null"}"
                : value.Value is null ? "not set" : $"not set: {value.Value}";
    }
}
