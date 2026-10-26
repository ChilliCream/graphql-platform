using System.Text;
using HotChocolate.Execution;
using HotChocolate.Fusion.Execution.Nodes;
using HotChocolate.Fusion.Types;
using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate.Fusion.Execution;

public class IncrementalPlanActivationTests : FusionTestBase
{
    [Fact]
    public void GetRunningPlans_Should_MarkOnlyPlansOfActiveGroups_When_PlanHasMoreThan64IncrementalPlans()
    {
        // arrange
        var schema = CreateSchema();
        var plan = PlanOperation(schema, CreateDeferDocument(conditionCount: 70));
        var variables = CreateVariables(schema, "d", 70, i => i is 3 or 65);
        var activeDeliveryGroups = DeliveryGroup.GetActive(plan.DeliveryGroups, variables);

        // act
        var running = IncrementalPlan.GetRunningPlans(plan.IncrementalPlans, activeDeliveryGroups);

        // assert
        Assert.Equal(70, plan.IncrementalPlans.Length);
        Assert.Equal(["d3", "d65"], DescribeRunning(plan, running));
    }

    [Fact]
    public void GetActive_Should_MarkOnlyActiveGroups_When_PlanHasMoreThan64DeliveryGroups()
    {
        // arrange
        var schema = CreateSchema();
        var plan = PlanOperation(schema, CreateDeferDocument(conditionCount: 130));
        var variables = CreateVariables(schema, "d", 130, i => i is 0 or 63 or 64 or 127 or 128);

        // act
        var active = DeliveryGroup.GetActive(plan.DeliveryGroups, variables);

        // assert
        Assert.Equal(
            ["d0", "d127", "d128", "d63", "d64"],
            plan.DeliveryGroups
                .Where(g => active.IsSet(g.Id))
                .Select(g => g.IfVariable!)
                .Order(StringComparer.Ordinal)
                .ToArray());
    }

    [Fact]
    public void DoesNotRun_Should_ReadTheRunningBitOfTheOperationsPlan_When_PlanHasMoreThan64IncrementalPlans()
    {
        // arrange
        var schema = CreateSchema();
        var plan = PlanOperation(schema, CreateDeferDocument(conditionCount: 70));
        var variables = CreateVariables(schema, "d", 70, i => i == 65);
        var activeDeliveryGroups = DeliveryGroup.GetActive(plan.DeliveryGroups, variables);
        var running = IncrementalPlan.GetRunningPlans(plan.IncrementalPlans, activeDeliveryGroups);

        // act
        var runningPlans = plan.IncrementalPlans
            .Where(p => !plan.DoesNotRun(running, p.Operation))
            .Select(p => p.DeliveryGroups[0].IfVariable!)
            .ToArray();
        var rootDoesNotRun = plan.DoesNotRun(running, plan.Operation);

        // assert
        Assert.Equal(["d65"], runningPlans);
        Assert.False(rootDoesNotRun);
    }

    [Theory]
    [InlineData(true, true, "a,b")]
    [InlineData(true, false, "a")]
    [InlineData(false, true, "")]
    [InlineData(false, false, "")]
    public void GetRunningPlans_Should_RequireTheParentPlanToRun_When_DefersAreNested(
        bool a,
        bool b,
        string expected)
    {
        // arrange
        var schema = CreateSchema();
        var plan = PlanOperation(
            schema,
            "query($a: Boolean! $b: Boolean!) "
            + "{ plain: foo ... @defer(if: $a) { x: foo ... @defer(if: $b) { y: foo } } }");
        var variables = CreateVariables(schema, ("a", a), ("b", b));
        var activeDeliveryGroups = DeliveryGroup.GetActive(plan.DeliveryGroups, variables);

        // act
        var running = IncrementalPlan.GetRunningPlans(plan.IncrementalPlans, activeDeliveryGroups);

        // assert
        Assert.Equal(expected, string.Join(",", DescribeRunning(plan, running)));
    }

    [Fact]
    public void GetRunningPlans_Should_NotAllocate_When_PlanHasAtMost64IncrementalPlans()
    {
        // arrange
        var schema = CreateSchema();
        var plan = PlanOperation(schema, CreateDeferDocument(conditionCount: 64));
        var variables = CreateVariables(schema, "d", 64, i => i % 2 == 0);

        for (var i = 0; i < 100; i++)
        {
            Evaluate(plan, variables);
        }

        // act
        var before = GC.GetAllocatedBytesForCurrentThread();
        var runningCount = Evaluate(plan, variables);
        var allocated = GC.GetAllocatedBytesForCurrentThread() - before;

        // assert
        Assert.Equal(32, runningCount);
        Assert.Equal(0, allocated);
    }

    private static int Evaluate(OperationPlan plan, IVariableValueCollection variables)
    {
        var activeDeliveryGroups = DeliveryGroup.GetActive(plan.DeliveryGroups, variables);
        var running = IncrementalPlan.GetRunningPlans(plan.IncrementalPlans, activeDeliveryGroups);
        var count = 0;

        for (var i = 0; i < plan.IncrementalPlans.Length; i++)
        {
            if (running.IsSet(i))
            {
                count++;
            }
        }

        return count;
    }

    private static string[] DescribeRunning(OperationPlan plan, ConditionFlags running)
    {
        var names = new List<string>();

        for (var i = 0; i < plan.IncrementalPlans.Length; i++)
        {
            if (running.IsSet(i))
            {
                names.Add(plan.IncrementalPlans[i].DeliveryGroups[0].IfVariable!);
            }
        }

        names.Sort(StringComparer.Ordinal);
        return [.. names];
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
        var values = new (string Name, bool Value)[count];

        for (var i = 0; i < count; i++)
        {
            values[i] = ($"{prefix}{i}", value(i));
        }

        return CreateVariables(schema, values);
    }

    private static VariableValueCollection CreateVariables(
        FusionSchemaDefinition schema,
        params (string Name, bool Value)[] values)
    {
        var nonNullBooleanType = new NonNullType(schema.Types.GetType<IScalarTypeDefinition>("Boolean"));
        var variables = new Dictionary<string, VariableValue>();

        foreach (var (name, value) in values)
        {
            variables.Add(
                name,
                new VariableValue(
                    name,
                    nonNullBooleanType,
                    value ? BooleanValueNode.True : BooleanValueNode.False));
        }

        return new VariableValueCollection(variables);
    }
}
