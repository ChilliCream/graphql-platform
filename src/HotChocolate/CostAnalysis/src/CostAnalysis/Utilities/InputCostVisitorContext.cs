using HotChocolate.Types;

namespace HotChocolate.CostAnalysis.Utilities;

internal sealed class InputCostVisitorContext
{
    public List<IInputType> Types { get; } = new();

    public List<IInputField> Fields { get; } = new();

    public List<InputObjectType> Backlog { get; } = new();

    public HashSet<IInputType> Processed { get; } = new();

    public double Cost { get; set; }

    public void Reset()
    {
        Types.Clear();
        Fields.Clear();
        Cost = 0;
    }
}
