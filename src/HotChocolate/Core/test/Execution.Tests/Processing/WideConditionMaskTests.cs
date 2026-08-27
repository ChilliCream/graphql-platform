using System.Text;
using System.Text.Json;
using HotChocolate.Language;
using HotChocolate.Types;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Execution.Processing;

public class WideConditionMaskTests
{
    [Fact]
    public void Compile_With_More_Than_64_Include_Conditions_Succeeds_By_Default()
    {
        // arrange
        var schema = CreateSchema();
        var document = Utf8GraphQLParser.Parse(CreateIncludeDocument(conditionCount: 100));

        // act
        var operation = OperationCompiler.Compile("opid", document, schema);

        // assert
        Assert.True(operation.HasWideIncludeFlags);
        Assert.False(operation.HasWideDeferFlags);
    }

    [Fact]
    public void Compile_With_More_Than_64_Defer_Conditions_Succeeds_By_Default()
    {
        // arrange
        var schema = CreateSchema();
        var document = Utf8GraphQLParser.Parse(CreateDeferDocument(conditionCount: 70));

        // act
        var operation = OperationCompiler.Compile("opid", document, schema);

        // assert
        Assert.True(operation.HasWideDeferFlags);
        Assert.False(operation.HasWideIncludeFlags);
    }

    [Fact]
    public async Task Include_Condition_Ceiling_Produces_Request_Error()
    {
        // arrange
        var executor = await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType(c => c.Name("Query").Field("foo").Resolve("abc"))
            .ModifyRequestOptions(o => o.MaxAllowedIncludeConditions = 4)
            .BuildRequestExecutorAsync(cancellationToken: TestContext.Current.CancellationToken);

        // act
        var result = await executor.ExecuteAsync(
            OperationRequestBuilder.New()
                .SetDocument(CreateIncludeDocument(conditionCount: 5))
                .SetVariableValues(CreateVariables("v", 5, _ => true))
                .Build(),
            TestContext.Current.CancellationToken);

        // assert
        var operationResult = Assert.IsType<OperationResult>(result);
        Assert.NotNull(operationResult.Errors);
        Assert.Contains(
            operationResult.Errors,
            e => e.Message.Contains(
                "The operation exceeds the maximum allowed number of include conditions (4)."));
    }

    [Fact]
    public async Task Defer_Condition_Ceiling_Produces_Request_Error()
    {
        // arrange
        var executor = await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType(c => c.Name("Query").Field("foo").Resolve("abc"))
            .ModifyOptions(o => o.EnableDefer = true)
            .ModifyRequestOptions(o => o.MaxAllowedDeferConditions = 4)
            .BuildRequestExecutorAsync(cancellationToken: TestContext.Current.CancellationToken);

        // act
        var result = await executor.ExecuteAsync(
            OperationRequestBuilder.New()
                .SetDocument(CreateDeferDocument(conditionCount: 5))
                .SetVariableValues(CreateVariables("d", 5, _ => false))
                .Build(),
            TestContext.Current.CancellationToken);

        // assert
        var operationResult = Assert.IsType<OperationResult>(result);
        Assert.NotNull(operationResult.Errors);
        Assert.Contains(
            operationResult.Errors,
            e => e.Message.Contains(
                "The operation exceeds the maximum allowed number of defer conditions (4)."));
    }

    [Fact]
    public void Narrow_IsIncluded_Throws_For_Conditional_Selection_Of_Wide_Operation()
    {
        // arrange
        var schema = CreateSchema();
        var document = Utf8GraphQLParser.Parse(CreateIncludeDocument(conditionCount: 100));
        var operation = OperationCompiler.Compile("opid", document, schema);

        Assert.True(operation.RootSelectionSet.TryGetSelection("f0", out var conditional));
        Assert.True(operation.RootSelectionSet.TryGetSelection("plain", out var nonConditional));

        // act & assert
        var exception = Assert.Throws<InvalidOperationException>(
            () => conditional!.IsIncluded(ulong.MaxValue));
        Assert.Contains("wide include flags", exception.Message);
        Assert.Throws<InvalidOperationException>(() => conditional!.IsSkipped(ulong.MaxValue));

        // non-conditional selections never consult the mask and must not throw.
        Assert.True(nonConditional!.IsIncluded(0));
    }

    [Fact]
    public void Narrow_IsDeferred_Throws_For_Deferrable_Selection_Of_Wide_Operation()
    {
        // arrange
        var schema = CreateSchema();
        var document = Utf8GraphQLParser.Parse(CreateDeferDocument(conditionCount: 70));
        var operation = OperationCompiler.Compile("opid", document, schema);

        Assert.True(operation.RootSelectionSet.TryGetSelection("f0", out var deferrable));
        Assert.True(operation.RootSelectionSet.TryGetSelection("plain", out var nonDeferrable));

        // act & assert
        var exception = Assert.Throws<InvalidOperationException>(
            () => deferrable!.IsDeferred(ulong.MaxValue));
        Assert.Contains("wide defer flags", exception.Message);
        Assert.Throws<InvalidOperationException>(
            () => deferrable!.GetActiveDeferUsages(ulong.MaxValue));
        Assert.Throws<InvalidOperationException>(
            () => deferrable!.GetPrimaryDeferUsage(ulong.MaxValue));

        // selections without defer usage never consult the mask and must not throw.
        Assert.False(nonDeferrable!.IsDeferred(ulong.MaxValue));
    }

    [Theory]
    [InlineData(63)]
    [InlineData(64)]
    [InlineData(65)]
    [InlineData(127)]
    [InlineData(128)]
    public void Wide_Include_Boundary_Bit_Is_Evaluated_Correctly(int index)
    {
        // arrange
        var schema = CreateSchema();
        var document = Utf8GraphQLParser.Parse(CreateIncludeDocument(conditionCount: 129));
        var operation = OperationCompiler.Compile("opid", document, schema);

        Assert.True(operation.RootSelectionSet.TryGetSelection($"f{index}", out var selection));

        // act & assert
        var word = index >> 6;
        var bit = 1ul << (index & 63);
        var word0 = word == 0 ? bit : 0ul;
        var wide = new ulong[2];

        if (word > 0)
        {
            wide[word - 1] = bit;
        }

        // only the condition bit of this selection set -> included.
        Assert.True(selection!.IsIncluded(word0, wide));

        // every bit except the condition bit set -> not included.
        var allWord0 = ~word0;
        var allWide = new[] { ~wide[0], ~wide[1] };
        Assert.False(selection.IsIncluded(allWord0, allWide));
    }

    [Fact]
    public void Wide_Include_Path_Requires_All_Words()
    {
        // arrange: "combined" sits below @include(if: $v0) and carries @include(if: $v128),
        // so its single path mask has bits in word 0 and word 2.
        var schema = CreateSchema();
        var sourceText = new StringBuilder();
        sourceText.Append("query(");

        for (var i = 0; i < 129; i++)
        {
            sourceText.Append($"$v{i}: Boolean! ");
        }

        sourceText.Append(") {");

        for (var i = 0; i < 129; i++)
        {
            sourceText.Append($" f{i}: foo @include(if: $v{i})");
        }

        sourceText.Append(" ... @include(if: $v0) { combined: foo @include(if: $v128) }");
        sourceText.Append(" }");

        var document = Utf8GraphQLParser.Parse(sourceText.ToString());
        var operation = OperationCompiler.Compile("opid", document, schema);

        Assert.True(operation.RootSelectionSet.TryGetSelection("combined", out var selection));

        // act & assert
        const ulong v0 = 1ul;
        var v128 = new ulong[] { 0, 1 };

        Assert.True(selection!.IsIncluded(v0, v128));
        Assert.False(selection.IsIncluded(v0, new ulong[2]));
        Assert.False(selection.IsIncluded(0, v128));
        Assert.False(selection.IsIncluded(0, new ulong[2]));
    }

    [Theory]
    [InlineData(63)]
    [InlineData(64)]
    [InlineData(65)]
    [InlineData(127)]
    [InlineData(128)]
    public void Wide_Defer_Boundary_Bit_Is_Evaluated_Correctly(int index)
    {
        // arrange
        var schema = CreateSchema();
        var document = Utf8GraphQLParser.Parse(CreateDeferDocument(conditionCount: 129));
        var operation = OperationCompiler.Compile("opid", document, schema);

        Assert.True(operation.RootSelectionSet.TryGetSelection($"f{index}", out var selection));

        // act & assert: deferred when ANY bit of the mask matches in any word.
        var word = index >> 6;
        var bit = 1ul << (index & 63);
        var word0 = word == 0 ? bit : 0ul;
        var wide = new ulong[2];

        if (word > 0)
        {
            wide[word - 1] = bit;
        }

        Assert.True(selection!.IsDeferred(word0, wide));
        Assert.False(selection.IsDeferred(0ul, new ulong[2]));

        var activeUsages = selection.GetActiveDeferUsages(word0, wide);
        Assert.NotNull(activeUsages);
        Assert.Equal(index, Assert.Single(activeUsages).DeferConditionIndex);
        Assert.Null(selection.GetActiveDeferUsages(0ul, new ulong[2]));
    }

    [Fact]
    public async Task Execute_With_More_Than_64_Include_Conditions()
    {
        // arrange
        const int conditionCount = 70;
        var executor = await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType(c => c.Name("Query").Field("foo").Resolve("abc"))
            .BuildRequestExecutorAsync(cancellationToken: TestContext.Current.CancellationToken);

        // act: every even condition is included, every odd condition is skipped.
        var result = await executor.ExecuteAsync(
            OperationRequestBuilder.New()
                .SetDocument(CreateIncludeDocument(conditionCount))
                .SetVariableValues(CreateVariables("v", conditionCount, i => i % 2 == 0))
                .Build(),
            TestContext.Current.CancellationToken);

        // assert
        var operationResult = Assert.IsType<OperationResult>(result);
        Assert.Empty(operationResult.Errors ?? []);

        using var json = JsonDocument.Parse(operationResult.ToJson());
        var data = json.RootElement.GetProperty("data");

        Assert.Equal("abc", data.GetProperty("plain").GetString());

        for (var i = 0; i < conditionCount; i++)
        {
            Assert.Equal(i % 2 == 0, data.TryGetProperty($"f{i}", out _));
        }
    }

    [Fact]
    public async Task Execute_With_More_Than_64_Defer_Conditions()
    {
        // arrange
        const int conditionCount = 66;
        var executor = await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType(c => c.Name("Query").Field("foo").Resolve("abc"))
            .ModifyOptions(o => o.EnableDefer = true)
            .BuildRequestExecutorAsync(cancellationToken: TestContext.Current.CancellationToken);

        // act: only condition 65 defers; all other fragments fold into the initial result.
        var result = await executor.ExecuteAsync(
            OperationRequestBuilder.New()
                .SetDocument(CreateDeferDocument(conditionCount))
                .SetVariableValues(CreateVariables("d", conditionCount, i => i == 65))
                .Build(),
            TestContext.Current.CancellationToken);

        // assert
        var stream = Assert.IsType<ResponseStream>(result);
        var payloads = new List<string>();

        await using (stream)
        {
            await foreach (var response in stream.ReadResultsAsync())
            {
                payloads.Add(response.ToJson());
            }
        }

        Assert.True(payloads.Count >= 2, "the deferred field must arrive in a later payload");

        using var initial = JsonDocument.Parse(payloads[0]);
        var data = initial.RootElement.GetProperty("data");

        Assert.Equal("abc", data.GetProperty("plain").GetString());

        for (var i = 0; i < conditionCount; i++)
        {
            Assert.Equal(i != 65, data.TryGetProperty($"f{i}", out _));
        }

        Assert.Contains(payloads.Skip(1), p => p.Contains("\"f65\""));
    }

    private static Schema CreateSchema()
        => SchemaBuilder.New()
            .AddQueryType(
                c => c
                    .Name("Query")
                    .Field("foo")
                    .Type<StringType>()
                    .Resolve("abc"))
            .Create();

    private static string CreateIncludeDocument(int conditionCount)
    {
        var sourceText = new StringBuilder();
        sourceText.Append("query(");

        for (var i = 0; i < conditionCount; i++)
        {
            sourceText.Append($"$v{i}: Boolean! ");
        }

        sourceText.Append(") { plain: foo");

        for (var i = 0; i < conditionCount; i++)
        {
            sourceText.Append($" f{i}: foo @include(if: $v{i})");
        }

        sourceText.Append(" }");
        return sourceText.ToString();
    }

    private static string CreateDeferDocument(int conditionCount)
    {
        var sourceText = new StringBuilder();
        sourceText.Append("query(");

        for (var i = 0; i < conditionCount; i++)
        {
            sourceText.Append($"$d{i}: Boolean! ");
        }

        sourceText.Append(") { plain: foo");

        for (var i = 0; i < conditionCount; i++)
        {
            sourceText.Append($" ... @defer(if: $d{i}) {{ f{i}: foo }}");
        }

        sourceText.Append(" }");
        return sourceText.ToString();
    }

    private static Dictionary<string, object?> CreateVariables(
        string prefix,
        int count,
        Func<int, bool> value)
    {
        var variables = new Dictionary<string, object?>();

        for (var i = 0; i < count; i++)
        {
            variables.Add($"{prefix}{i}", value(i));
        }

        return variables;
    }
}
