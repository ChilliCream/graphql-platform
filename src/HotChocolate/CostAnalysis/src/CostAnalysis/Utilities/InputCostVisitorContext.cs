using HotChocolate.Types;

namespace HotChocolate.CostAnalysis.Utilities;

internal sealed class InputCostVisitorContext
{
    public List<IInputType> Types { get; } = [];

    public List<IInputValueDefinition> Fields { get; } = [];

    public List<InputObjectType> Backlog { get; } = [];

    public HashSet<IInputType> Processed { get; } = [];

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
        Backlog.Clear();
        Processed.Clear();
        Cost = 0;
    }
}
