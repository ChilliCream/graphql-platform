using System.Text;
using HotChocolate.Buffers;
using HotChocolate.Execution;
using HotChocolate.Fusion.Execution.Nodes;
using HotChocolate.Fusion.Execution.Nodes.Serialization;
using HotChocolate.Fusion.Types;
using HotChocolate.Language;
using HotChocolate.Types;
using Microsoft.Extensions.ObjectPool;

namespace HotChocolate.Fusion.Execution;

public class WideConditionMaskTests : FusionTestBase
{
    private readonly ObjectPool<OrderedDictionary<string, List<FieldSelectionNode>>> _fieldMapPool =
        new DefaultObjectPool<OrderedDictionary<string, List<FieldSelectionNode>>>(new FieldMapPooledObjectPolicy());

    [Fact]
    public void Compile_With_More_Than_64_Include_Conditions_Succeeds_By_Default()
    {
        // arrange
        var schema = CreateSchema();
        var operationDefinition = ParseOperation(CreateIncludeDocument(conditionCount: 100));

        // act
        var compiler = new OperationCompiler(schema, _fieldMapPool);
        var operation = compiler.Compile("1", "1", "1", operationDefinition);

        // assert
        Assert.True(operation.HasWideIncludeFlags);
        Assert.False(operation.HasWideDeferFlags);
    }

    [Fact]
    public void Compile_With_More_Than_64_Defer_Conditions_Succeeds_By_Default()
    {
        // arrange
        var schema = CreateSchema();
        var operationDefinition = ParseOperation(CreateDeferDocument(conditionCount: 70));

        // act
        var compiler = new OperationCompiler(schema, _fieldMapPool);
        var operation = compiler.Compile("1", "1", "1", operationDefinition);

        // assert
        Assert.True(operation.HasWideDeferFlags);
        Assert.False(operation.HasWideIncludeFlags);
    }

    [Fact]
    public void Include_Condition_Ceiling_Produces_GraphQL_Error()
    {
        // arrange
        var schema = CreateSchema();
        var operationDefinition = ParseOperation(CreateIncludeDocument(conditionCount: 5));

        // act
        var compiler = new OperationCompiler(schema, _fieldMapPool, maxAllowedIncludeConditions: 4);
        var exception = Assert.Throws<GraphQLException>(
            () => compiler.Compile("1", "1", "1", operationDefinition));

        // assert
        Assert.Contains(
            "The operation exceeds the maximum allowed number of include conditions (4).",
            exception.Errors[0].Message);
    }

    [Fact]
    public void Defer_Condition_Ceiling_Produces_GraphQL_Error()
    {
        // arrange
        var schema = CreateSchema();
        var operationDefinition = ParseOperation(CreateDeferDocument(conditionCount: 5));

        // act
        var compiler = new OperationCompiler(schema, _fieldMapPool, maxAllowedDeferConditions: 4);
        var exception = Assert.Throws<GraphQLException>(
            () => compiler.Compile("1", "1", "1", operationDefinition));

        // assert
        Assert.Contains(
            "The operation exceeds the maximum allowed number of defer conditions (4).",
            exception.Errors[0].Message);
    }

    [Fact]
    public void Narrow_IsIncluded_Throws_For_Conditional_Selection_Of_Wide_Operation()
    {
        // arrange
        var schema = CreateSchema();
        var operationDefinition = ParseOperation(CreateIncludeDocument(conditionCount: 100));
        var compiler = new OperationCompiler(schema, _fieldMapPool);
        var operation = compiler.Compile("1", "1", "1", operationDefinition);

        Assert.True(operation.RootSelectionSet.TryGetSelection("f0", out var conditional));
        Assert.True(operation.RootSelectionSet.TryGetSelection("plain", out var nonConditional));

        // act & assert
        var exception = Assert.Throws<InvalidOperationException>(
            () => conditional.IsIncluded(ulong.MaxValue));
        Assert.Contains("wide include flags", exception.Message);

        // non-conditional selections never consult the mask and must not throw.
        Assert.True(nonConditional.IsIncluded(0));
    }

    [Fact]
    public void Narrow_IsDeferred_Throws_For_Deferrable_Selection_Of_Wide_Operation()
    {
        // arrange
        var schema = CreateSchema();
        var operationDefinition = ParseOperation(CreateDeferDocument(conditionCount: 70));
        var compiler = new OperationCompiler(schema, _fieldMapPool);
        var operation = compiler.Compile("1", "1", "1", operationDefinition);

        Assert.True(operation.RootSelectionSet.TryGetSelection("f0", out var deferrable));
        Assert.True(operation.RootSelectionSet.TryGetSelection("plain", out var nonDeferrable));

        // act & assert
        var exception = Assert.Throws<InvalidOperationException>(
            () => deferrable.IsDeferred(ulong.MaxValue));
        Assert.Contains("wide defer flags", exception.Message);
        Assert.Throws<InvalidOperationException>(
            () => deferrable.GetActiveDeliveryGroups(ulong.MaxValue));
        Assert.Throws<InvalidOperationException>(
            () => deferrable.HasActiveDeliveryGroup(ulong.MaxValue, new DeliveryGroup(null, null, 0)));

        // selections without delivery groups never consult the mask and must not throw.
        Assert.False(nonDeferrable.IsDeferred(ulong.MaxValue));
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
        const int conditionCount = 129;
        var schema = CreateSchema();
        var operationDefinition = ParseOperation(CreateIncludeDocument(conditionCount));
        var compiler = new OperationCompiler(schema, _fieldMapPool);
        var operation = compiler.Compile("1", "1", "1", operationDefinition);

        Assert.True(operation.RootSelectionSet.TryGetSelection($"f{index}", out var selection));

        // act & assert: only the condition bit of this selection set -> included.
        var onlyIndex = CreateVariables(schema, "v", conditionCount, i => i == index);
        var flags = operation.CreateIncludeConditionFlags(onlyIndex);
        Assert.NotNull(flags.Overflow);
        Assert.True(selection.IsIncluded(flags));

        // every bit except the condition bit set -> not included.
        var allButIndex = CreateVariables(schema, "v", conditionCount, i => i != index);
        flags = operation.CreateIncludeConditionFlags(allButIndex);
        Assert.NotNull(flags.Overflow);
        Assert.False(selection.IsIncluded(flags));
    }

    [Fact]
    public void Wide_Include_Path_Requires_All_Words()
    {
        // arrange: "combined" sits below @include(if: $x) and carries @include(if: $v0).
        // The condition of $x registers after the 129 field conditions (index 129),
        // so the single path mask of "combined" has bits in word 0 and word 2.
        const int conditionCount = 129;
        var schema = CreateSchema();
        var sourceText = new StringBuilder();
        sourceText.Append("query(");

        for (var i = 0; i < conditionCount; i++)
        {
            sourceText.Append($"$v{i}: Boolean! ");
        }

        sourceText.Append("$x: Boolean! ) {");

        for (var i = 0; i < conditionCount; i++)
        {
            sourceText.Append($" f{i}: foo @include(if: $v{i})");
        }

        sourceText.Append(" ... @include(if: $x) { combined: foo @include(if: $v0) }");
        sourceText.Append(" }");

        var operationDefinition = ParseOperation(sourceText.ToString());
        var compiler = new OperationCompiler(schema, _fieldMapPool);
        var operation = compiler.Compile("1", "1", "1", operationDefinition);

        Assert.True(operation.RootSelectionSet.TryGetSelection("combined", out var selection));

        // act & assert: both words satisfied -> included.
        var bothWords = CreateVariables(schema, conditionCount, i => i == 0, x: true);
        var flags = operation.CreateIncludeConditionFlags(bothWords);
        Assert.NotNull(flags.Overflow);
        Assert.True(selection.IsIncluded(flags));

        // the same word 0, but the overflow words dropped -> the path is not satisfied.
        Assert.False(selection.IsIncluded(new ConditionFlags(flags.Word0, new ulong[flags.Overflow!.Length])));

        // only the overflow word satisfied -> the path is not satisfied.
        var overflowOnly = CreateVariables(schema, conditionCount, _ => false, x: true);
        flags = operation.CreateIncludeConditionFlags(overflowOnly);
        Assert.NotNull(flags.Overflow);
        Assert.False(selection.IsIncluded(flags));

        // only word 0 satisfied -> the path is not satisfied.
        var word0Only = CreateVariables(schema, conditionCount, i => i == 0, x: false);
        flags = operation.CreateIncludeConditionFlags(word0Only);
        Assert.NotNull(flags.Overflow);
        Assert.False(selection.IsIncluded(flags));
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
        const int conditionCount = 129;
        var schema = CreateSchema();
        var operationDefinition = ParseOperation(CreateDeferDocument(conditionCount));
        var compiler = new OperationCompiler(schema, _fieldMapPool);
        var operation = compiler.Compile("1", "1", "1", operationDefinition);

        Assert.True(operation.RootSelectionSet.TryGetSelection($"f{index}", out var selection));

        // act & assert: deferred when ANY bit of the mask matches in any word.
        var onlyIndex = CreateVariables(schema, "d", conditionCount, i => i == index);
        var flags = operation.CreateDeferConditionFlags(onlyIndex);
        Assert.NotNull(flags.Overflow);
        Assert.True(selection.IsDeferred(flags));

        var activeGroups = selection.GetActiveDeliveryGroups(flags);
        Assert.NotNull(activeGroups);
        Assert.Equal(index, Assert.Single(activeGroups).DeferConditionIndex);

        var none = CreateVariables(schema, "d", conditionCount, _ => false);
        flags = operation.CreateDeferConditionFlags(none);
        Assert.NotNull(flags.Overflow);
        Assert.False(selection.IsDeferred(flags));
        Assert.Null(selection.GetActiveDeliveryGroups(flags));
    }

    [Fact]
    public void Serialized_Plan_Round_Trips_Wide_Include_Operation()
    {
        // arrange: the plan format serializes the operation document, not the
        // compiled masks, so parsing recompiles the wide operation from source.
        const int conditionCount = 70;
        var schema = CreateSchema();
        var originalPlan = PlanOperation(schema, CreateIncludeDocument(conditionCount));
        Assert.True(originalPlan.Operation.HasWideIncludeFlags);

        using var buffer = new PooledArrayWriter();
        var formatter = new JsonOperationPlanFormatter();
        formatter.Format(buffer, originalPlan);

        // act
        var compiler = new OperationCompiler(schema, _fieldMapPool);
        var parser = new JsonOperationPlanParser(compiler);
        var parsedPlan = parser.Parse(buffer.WrittenMemory);

        // assert: the recompiled operation is wide and evaluates overflow bits.
        var operation = parsedPlan.Operation;
        Assert.True(operation.HasWideIncludeFlags);
        Assert.True(operation.RootSelectionSet.TryGetSelection("f65", out var selection));

        var only65 = CreateVariables(schema, "v", conditionCount, i => i == 65);
        var flags = operation.CreateIncludeConditionFlags(only65);
        Assert.NotNull(flags.Overflow);
        Assert.True(selection.IsIncluded(flags));

        var none = CreateVariables(schema, "v", conditionCount, _ => false);
        flags = operation.CreateIncludeConditionFlags(none);
        Assert.NotNull(flags.Overflow);
        Assert.False(selection.IsIncluded(flags));
    }

    private static FusionSchemaDefinition CreateSchema()
    {
        const string sourceText =
            """
            type Query {
                foo: String
            }

            scalar Boolean
            """;

        return ComposeSchema(sourceText);
    }

    private static OperationDefinitionNode ParseOperation(string sourceText)
        => Utf8GraphQLParser.Parse(sourceText)
            .Definitions
            .OfType<OperationDefinitionNode>()
            .First();

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

    private static VariableValueCollection CreateVariables(
        FusionSchemaDefinition schema,
        string prefix,
        int count,
        Func<int, bool> value)
    {
        var variables = new Dictionary<string, VariableValue>();
        AddVariables(schema, variables, prefix, count, value);
        return new VariableValueCollection(variables);
    }

    private static VariableValueCollection CreateVariables(
        FusionSchemaDefinition schema,
        int count,
        Func<int, bool> value,
        bool x)
    {
        var variables = new Dictionary<string, VariableValue>();
        AddVariables(schema, variables, "v", count, value);

        var booleanType = schema.Types.GetType<IScalarTypeDefinition>("Boolean");
        variables.Add(
            "x",
            new VariableValue(
                "x",
                new NonNullType(booleanType),
                x ? BooleanValueNode.True : BooleanValueNode.False));

        return new VariableValueCollection(variables);
    }

    private static void AddVariables(
        FusionSchemaDefinition schema,
        Dictionary<string, VariableValue> variables,
        string prefix,
        int count,
        Func<int, bool> value)
    {
        var booleanType = schema.Types.GetType<IScalarTypeDefinition>("Boolean");
        var nonNullBooleanType = new NonNullType(booleanType);

        for (var i = 0; i < count; i++)
        {
            var name = $"{prefix}{i}";
            variables.Add(
                name,
                new VariableValue(
                    name,
                    nonNullBooleanType,
                    value(i) ? BooleanValueNode.True : BooleanValueNode.False));
        }
    }
}
