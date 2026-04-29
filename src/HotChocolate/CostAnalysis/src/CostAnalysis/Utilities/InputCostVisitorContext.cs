using HotChocolate.Types;

namespace HotChocolate.CostAnalysis.Utilities;

internal sealed class InputCostVisitorContext
{
    public List<IInputType> Types { get; } = [];

    public List<IInputValueDefinition> Fields { get; } = [];

    public HashSet<InputObjectType> Visiting { get; } = [];

    public Dictionary<InputObjectType, double> CostCache { get; } = [];

    public double Cost { get; set; }

    public bool SubtreeContainsCycle { get; set; }

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
        Visiting.Clear();
        CostCache.Clear();
        SubtreeContainsCycle = false;
        Cost = 0;
    }
}
