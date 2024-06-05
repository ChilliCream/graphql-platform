namespace HotChocolate.CostAnalysis;

internal sealed class CostCountType(string name, int value)
{
    public string Name { get; } = name;

    public int Value { get; } = value;
}
