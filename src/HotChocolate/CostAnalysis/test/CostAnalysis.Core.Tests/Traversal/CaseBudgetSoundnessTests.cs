using System.Globalization;
using System.Text;

namespace HotChocolate.CostAnalysis;

/// <summary>
/// For every Boolean assignment, the fallback the ExactCases backend
/// produces once its case budget is exhausted must be componentwise
/// greater than or equal to the unbudgeted exact result.
/// </summary>
public class CaseBudgetSoundnessTests
{
    private const int SampleCount = 200;
    private const int BaseSeed = 20260908;

    [Fact]
    public void Evaluate_Should_Overestimate_Or_Match_The_Unbudgeted_Result_When_The_Case_Budget_Is_Exhausted()
    {
        for (var sample = 0; sample < SampleCount; sample++)
        {
            var seed = BaseSeed + sample;
            var random = new Random(seed);
            var (sdl, operationText, variableNames) = GenerateOperation(random);
            var algebra = new TestCostAlgebra();

            var unbudgeted = TraversalTestHelpers.EvaluateOperation(sdl, operationText, algebra, caseBudget: int.MaxValue);
            var budgeted = TraversalTestHelpers.EvaluateOperation(sdl, operationText, algebra, caseBudget: 1);

            foreach (var assignment in EveryAssignment(variableNames))
            {
                var exact = unbudgeted.Resolve(name => assignment[name]);
                var bounded = budgeted.Resolve(name => assignment[name]);

                Assert.True(
                    bounded.TypeCost >= exact.TypeCost,
                    $"seed {seed}: typeCost {bounded.TypeCost} is below the unbudgeted {exact.TypeCost}");
                Assert.True(
                    bounded.FieldCost >= exact.FieldCost,
                    $"seed {seed}: fieldCost {bounded.FieldCost} is below the unbudgeted {exact.FieldCost}");
            }
        }
    }

    /// <summary>
    /// Generates a small schema (2-4 object types implementing one shared
    /// interface, signed weights in [-8, 8]) and an operation with 1-4
    /// correlated Boolean variables: each type selects, per variable, one
    /// field gated by <c>@include</c> and one by <c>@skip</c> on that same
    /// variable, so resolving it decides two fields at once. Each variable's
    /// field returns its own object type carrying a random signed
    /// <c>@cost</c> type weight over a cheap scalar child (the c5 shape),
    /// so collect-then-weigh combines a signed type weight with a signed
    /// field weight inside the fallback envelope.
    /// </summary>
    private static (string Sdl, string Operation, string[] VariableNames) GenerateOperation(Random random)
    {
        var typeCount = random.Next(2, 5);
        var variableCount = random.Next(1, 5);
        var variableNames = new string[variableCount];

        for (var j = 0; j < variableCount; j++)
        {
            variableNames[j] = $"b{j}";
        }

        var sdl = new StringBuilder();

        for (var j = 0; j < variableCount; j++)
        {
            var typeWeight = random.Next(-8, 9).ToString(CultureInfo.InvariantCulture);
            sdl.Append("type Leaf").Append(j).Append(" @cost(weight: \"").Append(typeWeight).Append("\") { x: Int }").AppendLine();
        }

        var interfaceFields = new StringBuilder();

        for (var j = 0; j < variableCount; j++)
        {
            interfaceFields.Append(" v").Append(j).Append(": Leaf").Append(j);
        }

        sdl.Append("interface Node {").Append(interfaceFields).AppendLine(" }");

        for (var i = 0; i < typeCount; i++)
        {
            var fields = new StringBuilder();

            for (var j = 0; j < variableCount; j++)
            {
                var weight = random.Next(-8, 9).ToString(CultureInfo.InvariantCulture);
                fields.Append(" v").Append(j).Append(": Leaf").Append(j).Append(" @cost(weight: \"").Append(weight).Append("\")");
            }

            sdl.Append("type T").Append(i).Append(" implements Node {").Append(fields).AppendLine(" }");
        }

        sdl.AppendLine("type Query { node: Node }");

        var variableDeclarations = string.Join(", ", variableNames.Select(v => $"${v}: Boolean!"));
        var operation = new StringBuilder();
        operation.Append("query(").Append(variableDeclarations).Append(") { node {");

        for (var i = 0; i < typeCount; i++)
        {
            operation.Append(" ... on T").Append(i).Append(" {");

            for (var j = 0; j < variableCount; j++)
            {
                var variable = variableNames[j];
                operation.Append(" r").Append(j).Append(": v").Append(j)
                    .Append(" @include(if: $").Append(variable).Append(") { x }");
                operation.Append(" s").Append(j).Append(": v").Append(j)
                    .Append(" @skip(if: $").Append(variable).Append(") { x }");
            }

            operation.Append(" }");
        }

        operation.Append(" } }");

        return (sdl.ToString(), operation.ToString(), variableNames);
    }

    private static IEnumerable<Dictionary<string, bool>> EveryAssignment(string[] variableNames)
    {
        var total = 1 << variableNames.Length;

        for (var mask = 0; mask < total; mask++)
        {
            var assignment = new Dictionary<string, bool>(variableNames.Length);

            for (var j = 0; j < variableNames.Length; j++)
            {
                assignment[variableNames[j]] = (mask & (1 << j)) != 0;
            }

            yield return assignment;
        }
    }
}
