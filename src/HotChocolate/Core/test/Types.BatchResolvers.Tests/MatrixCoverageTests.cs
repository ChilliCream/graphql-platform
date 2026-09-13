using System.Reflection;
using System.Text;
using Xunit.Sdk;
using Xunit.v3;

namespace HotChocolate.Types.BatchResolvers;

public sealed class MatrixCoverageTests
{
    [Fact]
    public async Task Matrix_Should_Cover_Every_Scenario_In_Every_Setting()
    {
        // arrange
        var families = DiscoverFamilies();
        var rows = new List<(string Family, string Scenario, DeclarationStyle Style, string Cell)>();

        foreach (var family in families)
        {
            var instance = (BatchScenarioTests)Activator.CreateInstance(family)!;

            try
            {
                var declarations = GetDeclarations(family, instance);

                foreach (var scenario in DiscoverScenarios(family))
                {
                    foreach (var style in Enum.GetValues<DeclarationStyle>())
                    {
                        rows.Add((family.Name, scenario.Name, style, DescribeCell(declarations[style])));
                    }
                }
            }
            finally
            {
                await instance.DisposeAsync();
            }
        }

        // act
        var markdown = RenderMarkdown(rows);

        // assert
        new Snapshot().Add(markdown, "Matrix coverage").MatchMarkdownSnapshot();
    }

    [Fact]
    public void Matrix_Should_Declare_Every_Scenario_As_BatchMatrix()
    {
        // arrange
        var violations = new List<string>();

        // act
        foreach (var family in DiscoverFamilies())
        {
            foreach (var method in family.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
            {
                if (method.GetCustomAttribute<TheoryAttribute>() is not { } theory)
                {
                    if (method.GetCustomAttribute<FactAttribute>() is not null)
                    {
                        violations.Add($"{family.Name}.{method.Name}");
                    }

                    continue;
                }

                var dataAttributes = method.GetCustomAttributes()
                    .OfType<DataAttribute>()
                    .ToArray();

                if (dataAttributes.Length != 1
                    || dataAttributes[0] is not BatchMatrixAttribute batchMatrix
                    || !string.IsNullOrEmpty(theory.Skip)
                    || !string.IsNullOrEmpty(theory.SkipWhen)
                    || !string.IsNullOrEmpty(theory.SkipUnless)
                    || !string.IsNullOrEmpty(batchMatrix.Skip)
                    || !string.IsNullOrEmpty(batchMatrix.SkipWhen)
                    || !string.IsNullOrEmpty(batchMatrix.SkipUnless))
                {
                    violations.Add($"{family.Name}.{method.Name}");
                }
            }
        }

        // assert
        Assert.Empty(violations);
    }

    private static IReadOnlyList<Type> DiscoverFamilies()
        => typeof(BatchScenarioTests).Assembly
            .GetTypes()
            .Where(t => !t.IsAbstract && typeof(BatchScenarioTests).IsAssignableFrom(t))
            .OrderBy(t => t.Name, StringComparer.Ordinal)
            .ToArray();

    private static IReadOnlyList<MethodInfo> DiscoverScenarios(Type family)
        => family.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .Where(m => m.GetCustomAttribute<TheoryAttribute>() is not null)
            .OrderBy(m => m.Name, StringComparer.Ordinal)
            .ToArray();

    private static BatchDeclarations GetDeclarations(Type family, BatchScenarioTests instance)
    {
        var property = family.GetProperty(
                "Declarations",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException(
                $"{family.Name} does not override BatchScenarioTests.Declarations.");

        return (BatchDeclarations)property.GetValue(instance)!;
    }

    private static string DescribeCell(Declaration declaration)
        => declaration.NotApplicableReason is { } reason
            ? $"NotApplicable({reason})"
            : "Declared";

    private static string RenderMarkdown(
        IReadOnlyList<(string Family, string Scenario, DeclarationStyle Style, string Cell)> rows)
    {
        var markdown = new StringBuilder();
        markdown.AppendLine("| Family | Scenario | Style | Result |");
        markdown.AppendLine("| --- | --- | --- | --- |");

        foreach (var row in rows)
        {
            markdown.AppendLine($"| {row.Family} | {row.Scenario} | {row.Style} | {row.Cell} |");
        }

        return markdown.ToString().TrimEnd('\n', '\r');
    }
}
