namespace HotChocolate.CostAnalysis;

internal sealed class CostByLocation(string path, double cost)
{
    public string Path { get; } = path;

    public double Cost { get; } = cost;
}
