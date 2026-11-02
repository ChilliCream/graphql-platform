using System.Diagnostics.CodeAnalysis;

namespace HotChocolate.CostAnalysis;

internal static class HeadToHeadThrowHelper
{
    [DoesNotReturn]
    public static HeadToHeadCorpus CorpusCouldNotBeRead(string path)
        => throw new InvalidOperationException($"The benchmark corpus '{path}' could not be read.");

    [DoesNotReturn]
    public static HeadToHeadScenario ScenarioNotFound(string scenarioId)
        => throw new InvalidOperationException($"The benchmark scenario '{scenarioId}' was not found.");

    [DoesNotReturn]
    public static void InvalidDefaultListSize(int value)
        => throw new InvalidOperationException(
            $"The benchmark corpus default list size must be 1, but was {value}.");

    [DoesNotReturn]
    public static void InvalidSourceCommit(string value)
        => throw new InvalidOperationException(
            $"The benchmark corpus came from unexpected oracle revision '{value}'.");

    [DoesNotReturn]
    public static void InvalidTopologyExpectation(string id, double typeCost, double fieldCost)
        => throw new InvalidOperationException(
            $"The topology scenario '{id}' must have expected cost 2/2, but had {typeCost}/{fieldCost}.");

    [DoesNotReturn]
    public static void UnsupportedVariableValue(string name)
        => throw new InvalidOperationException(
            $"The benchmark variable '{name}' must have a Boolean value.");

    [DoesNotReturn]
    public static void CostMismatch(
        string id,
        double expectedTypeCost,
        double expectedFieldCost,
        double actualTypeCost,
        double actualFieldCost)
        => throw new InvalidOperationException(
            $"The benchmark scenario '{id}' expected {expectedTypeCost}/{expectedFieldCost}, "
            + $"but HotChocolate produced {actualTypeCost}/{actualFieldCost}.");
}
