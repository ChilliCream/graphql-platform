namespace HotChocolate.CostAnalysis.Fuzz;

internal static class ThrowHelper
{
    public static InvalidOperationException InvalidOperation(string message)
        => new(message);
}
