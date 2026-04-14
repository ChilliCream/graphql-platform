using HotChocolate.Types;

namespace HotChocolate.CostAnalysis.Utilities;

internal sealed class InputCostVisitorContext
{
    public List<IInputType> Types { get; } = [];

    public List<IInputValueDefinition> Fields { get; } = [];

    public HashSet<InputObjectType> Processed { get; } = [];

    public Dictionary<InputObjectType, double> CostCache { get; } = [];

    public double Cost { get; set; }

    public void Reset()
    {
        Types.Clear();
        Fields.Clear();
        Cost = 0;
    }

    public void Clear()
    {
        Types.Clear();
        Fields.Clear();
        Processed.Clear();
        CostCache.Clear();
        Cost = 0;
    }
}
